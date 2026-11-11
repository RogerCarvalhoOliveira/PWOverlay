using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace PWQuestOverlay.Memory
{
    internal sealed class QuestMemoryClientState
    {
        private readonly HashSet<uint> _observedActive;
        private readonly HashSet<uint> _disappeared;

        internal QuestMemoryClientState()
        {
            _observedActive = new HashSet<uint>();
            _disappeared = new HashSet<uint>();
        }

        internal QuestMemorySnapshot Snapshot { get; set; }
        internal bool Ready { get; set; }
        internal string SessionToken { get; set; }
        internal PlayerIdentityMemory Identity { get; private set; }
        private string _candidateIdentity, _lastIdentity;
        private int _identityReads;

        internal bool ObserveIdentity(PlayerIdentityMemory identity)
        {
            if (identity != null && Identity != null && identity.Key == Identity.Key) return true;
            Identity = null;
            InvalidateQuests();
            string key = identity == null ? null : identity.Key;
            _identityReads = key != null && key == _candidateIdentity ? _identityReads + 1 : 1;
            _candidateIdentity = key;
            if (key == null || _identityReads < 2) return false;
            if (_lastIdentity != null && _lastIdentity != key) SessionToken = Guid.NewGuid().ToString("N");
            _lastIdentity = key;
            Identity = identity;
            return true;
        }

        internal void Invalidate()
        {
            Identity = null; _candidateIdentity = null; _identityReads = 0;
            InvalidateQuests();
        }

        internal void InvalidateQuests()
        { Ready = false; Snapshot = null; _observedActive.Clear(); _disappeared.Clear(); }

        internal void Observe(QuestMemorySnapshot snapshot)
        {
            foreach (ActiveQuestMemoryEntry entry in snapshot.ActiveTasks)
                _observedActive.Add(entry.TaskId);
        }

        internal void Record(IEnumerable<QuestMemoryEvent> changes)
        {
            foreach (QuestMemoryEvent change in changes)
            {
                if (change.Kind == QuestMemoryEventKind.Activated)
                {
                    _observedActive.Add(change.TaskId);
                    _disappeared.Remove(change.TaskId);
                }
                else if (change.Kind == QuestMemoryEventKind.DisappearedAfterObservation &&
                    _observedActive.Contains(change.TaskId))
                    _disappeared.Add(change.TaskId);
            }
        }

        internal ActiveQuestMemoryEntry FindActive(uint taskId)
        {
            return Snapshot == null
                ? null
                : Snapshot.ActiveTasks.Find(entry => entry.TaskId == taskId);
        }

        internal bool HasObservedDisappearance(uint taskId)
        {
            // One real transition may satisfy consecutive talk/turn-in guide steps.
            // A new activation clears this evidence for a repeated quest.
            return _disappeared.Contains(taskId);
        }
    }

    internal sealed class QuestMemoryMonitor : IDisposable
    {
        private readonly MemoryLayoutProfile _profile;
        private readonly Dictionary<int, Session> _sessions;
        private readonly HashSet<int> _reportedOpenFailures;
        private readonly Dictionary<int, DateTime> _openRetryAt = new Dictionary<int, DateTime>();
        private readonly Action<string> _log;

        internal QuestMemoryMonitor(
            string layoutPath,
            string profileId,
            Action<string> log)
        {
            MemoryLayoutFile layouts = MemoryLayoutFile.Load(layoutPath);
            _profile = layouts.Find(profileId);
            if (_profile == null)
                throw new InvalidDataException("Perfil de memória desconhecido: " + profileId);
            _profile.Validate();
            _sessions = new Dictionary<int, Session>();
            _reportedOpenFailures = new HashSet<int>();
            _log = log ?? delegate { };
        }

        internal void Poll(IEnumerable<int> processIds, bool readQuests = true, bool readHistory = false)
        {
            var live = new HashSet<int>(processIds.Where(id => id > 0));
            _reportedOpenFailures.RemoveWhere(id => !live.Contains(id));
            foreach (int stale in _openRetryAt.Keys.Where(id => !live.Contains(id)).ToList()) _openRetryAt.Remove(stale);
            foreach (int stale in _sessions.Keys.Where(id => !live.Contains(id)).ToList())
            {
                _sessions[stale].Dispose();
                _sessions.Remove(stale);
            }

            foreach (int processId in live)
            {
                Session session;
                if (!_sessions.TryGetValue(processId, out session))
                {
                    DateTime retry;
                    if (_openRetryAt.TryGetValue(processId, out retry) && DateTime.UtcNow < retry) continue;
                    try
                    {
                        session = Session.Open(processId, _profile, _log);
                        _sessions.Add(processId, session);
                        _reportedOpenFailures.Remove(processId);
                        _openRetryAt.Remove(processId);
                        _log("Memória PID " + processId + ": sessão aberta.");
                    }
                    catch (Exception error)
                    {
                        _openRetryAt[processId] = DateTime.UtcNow.AddSeconds(2);
                        if (_reportedOpenFailures.Add(processId))
                            _log("Memória PID " + processId +
                                ": falha ao abrir sessão: " + error.Message);
                        continue;
                    }
                }
                if (!session.Poll(readQuests, readHistory)) Forget(processId);
            }
        }

        internal QuestMemoryClientState Find(int processId)
        {
            Session session;
            return _sessions.TryGetValue(processId, out session)
                ? session.State : null;
        }

        internal void Forget(int processId)
        {
            Session session;
            if (_sessions.TryGetValue(processId, out session)) { session.State.Invalidate(); session.Dispose(); _sessions.Remove(processId); }
            _reportedOpenFailures.Remove(processId);
            _openRetryAt.Remove(processId);
        }

        public void Dispose()
        {
            foreach (Session session in _sessions.Values) session.Dispose();
            _sessions.Clear();
            _openRetryAt.Clear();
        }

        private sealed class Session : IDisposable
        {
            private readonly Process _process;
            private readonly ProcessMemoryReader _memory;
            private readonly QuestMemoryReader _reader;
            private readonly QuestStateTracker _tracker;
            private readonly long _player;
            private readonly MemoryLayoutProfile _profile;
            private readonly bool _configuredRoot;
            private int _readFailures;
            private readonly Action<string> _log;
            private string _candidateSignature;
            private string _committedSignature;
            private int _candidateCount;
            private bool? _lastReadQuests;
            private DateTime _questRetryAt;
            private string _lastQuestError;

            private Session(
                Process process,
                ProcessMemoryReader memory,
                QuestMemoryReader reader,
                long player,
                MemoryLayoutProfile profile,
                Action<string> log)
            {
                _process = process;
                _memory = memory;
                _reader = reader;
                _player = player;
                _profile = profile;
                try { _configuredRoot = _memory.ResolvePointerChain(profile.PlayerBaseRva, profile.PlayerChain) == player; }
                catch { _configuredRoot = false; }
                _log = log;
                _tracker = new QuestStateTracker();
                State = new QuestMemoryClientState { SessionToken = Guid.NewGuid().ToString("N") };
            }

            internal QuestMemoryClientState State { get; private set; }

            internal static Session Open(
                int processId,
                MemoryLayoutProfile profile,
                Action<string> log)
            {
                Process process = Process.GetProcessById(processId);
                ProcessMemoryReader memory = null;
                try
                {
                    memory = ProcessMemoryReader.Open(process, profile.module);
                    long player = ResolvePlayer(memory, profile);
                    return new Session(
                        process,
                        memory,
                        new QuestMemoryReader(memory, profile.quests, profile.inventory),
                        player,
                        profile,
                        log);
                }
                catch
                {
                    if (memory != null) memory.Dispose();
                    process.Dispose();
                    throw;
                }
            }

            internal bool Poll(bool readQuests, bool readHistory)
            {
                if (_process.HasExited) { State.Invalidate(); return false; }
                bool identityValidated = false;
                try
                {
                    // Login/logout can replace the player object without replacing the process.
                    // Never keep reading the previous character through a stale object pointer.
                    if (_configuredRoot)
                    {
                        long current = _memory.ResolvePointerChain(_profile.PlayerBaseRva, _profile.PlayerChain);
                        if (current != _player) { State.Invalidate(); return false; }
                    }
                    else if (!IsValidPlayer(_memory, _profile, _player)) { State.Invalidate(); return false; }
                    if (_profile.identity != null)
                    {
                        PlayerIdentityMemory identity;
                        try { identity = PlayerIdentityReader.Read(_memory, _player, _profile.identity); }
                        catch { State.ObserveIdentity(null); throw; }
                        if (!State.ObserveIdentity(identity))
                        { _candidateSignature = null; _committedSignature = null; _candidateCount = 0; return true; }
                    }
                    identityValidated = true;
                    if (_lastReadQuests != readQuests || (!readQuests && !readHistory))
                    {
                        State.InvalidateQuests();
                        _candidateSignature = null;
                        _committedSignature = null;
                        _candidateCount = 0;
                        _lastReadQuests = readQuests;
                        _questRetryAt = DateTime.MinValue;
                    }
                    if (!readQuests && !readHistory) { _readFailures = 0; return true; }
                    if (DateTime.UtcNow < _questRetryAt) return true;
                    QuestMemorySnapshot snapshot = _reader.Read(_player, readQuests);
                    int level = _memory.ReadInt32(_player + _profile.AttributeOffset("level"));
                    if (level < 1 || level > 150) throw new InvalidDataException("Nível do personagem inválido.");
                    snapshot.PlayerLevel = level;
                    _lastQuestError = null;
                    _readFailures = 0;
                    string signature = Signature(snapshot);
                    if (string.Equals(signature, _candidateSignature, StringComparison.Ordinal))
                        _candidateCount = Math.Min(2, _candidateCount + 1);
                    else
                    {
                        _candidateSignature = signature;
                        _candidateCount = 1;
                    }
                    if (_candidateCount < 2 ||
                        string.Equals(signature, _committedSignature, StringComparison.Ordinal))
                        return true;

                    if (!State.Ready)
                    {
                        _tracker.SetBaseline(snapshot);
                        State.Ready = true;
                        _log("Memória PID " + _process.Id + ": baseline confirmado: " +
                            signature);
                    }
                    else
                    {
                        List<QuestMemoryEvent> changes = _tracker.Update(snapshot);
                        State.Record(changes);
                        foreach (QuestMemoryEvent change in changes)
                            _log("Memória PID " + _process.Id + ": " +
                                change.Kind + " ID " + change.TaskId + ".");
                    }
                    State.Snapshot = snapshot;
                    State.Observe(snapshot);
                    _committedSignature = signature;
                    return true;
                }
                catch (Exception error)
                {
                    // Failed reads cannot leave old quest evidence available to the UI.
                    _candidateSignature = null; _committedSignature = null; _candidateCount = 0;
                    if (identityValidated)
                    {
                        State.InvalidateQuests();
                        _questRetryAt = DateTime.UtcNow.AddSeconds(2);
                        if (_lastQuestError != error.Message)
                        {
                            _lastQuestError = error.Message;
                            _log("Memória PID " + _process.Id + ": histórico temporariamente indisponível: " + error.Message);
                        }
                        return true;
                    }
                    State.Invalidate();
                    return ++_readFailures < 3;
                }
            }

            public void Dispose()
            {
                _memory.Dispose();
                _process.Dispose();
            }

            private static string Signature(QuestMemorySnapshot snapshot)
            {
                return snapshot.PlayerLevel + ":" + string.Join(",", snapshot.ActiveTasks
                    .Select(entry => entry.TaskId + ":" + entry.StateCode +
                        ":abates=" + entry.FirstMonsterKillCount)
                    .OrderBy(value => value)) + "|" +
                    string.Join(",", snapshot.FinishedTaskIds.OrderBy(id => id)) + "|" +
                    string.Join(",", snapshot.TaskItemCounts
                        .OrderBy(pair => pair.Key)
                        .Select(pair => pair.Key + ":" + pair.Value));
            }

            private static long ResolvePlayer(
                ProcessMemoryReader memory,
                MemoryLayoutProfile profile)
            {
                try
                {
                    long configured = memory.ResolvePointerChain(
                        profile.PlayerBaseRva,
                        profile.PlayerChain);
                    if (IsValidPlayer(memory, profile, configured)) return configured;
                }
                catch { }

                foreach (uint vtable in RttiResolver.FindVTables(
                    memory, profile.player.className))
                {
                    long candidate = memory.FindObjectByVTable(
                        vtable,
                        address => IsValidPlayer(memory, profile, address));
                    if (candidate != 0) return candidate;
                }
                throw new InvalidDataException("Personagem não localizado por memória.");
            }

            private static bool IsValidPlayer(
                ProcessMemoryReader memory,
                MemoryLayoutProfile profile,
                long player)
            {
                if (player <= 0) return false;
                int level = memory.ReadInt32(player + profile.AttributeOffset("level"));
                int hp = memory.ReadInt32(player + profile.AttributeOffset("hp"));
                int hpMax = memory.ReadInt32(player + profile.AttributeOffset("hpMax"));
                int mp = memory.ReadInt32(player + profile.AttributeOffset("mp"));
                int mpMax = memory.ReadInt32(player + profile.AttributeOffset("mpMax"));
                return level >= 1 && level <= 150 && hp >= 0 && hp <= hpMax &&
                    hpMax > 0 && hpMax < 100000000 && mp >= 0 && mp <= mpMax &&
                    mpMax > 0 && mpMax < 100000000;
            }
        }
    }
}
