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
                    _observedActive.Add(change.TaskId);
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

        internal bool TryConsumeDisappearance(uint taskId)
        {
            return _disappeared.Remove(taskId);
        }
    }

    internal sealed class QuestMemoryMonitor : IDisposable
    {
        private readonly MemoryLayoutProfile _profile;
        private readonly Dictionary<int, Session> _sessions;
        private readonly HashSet<int> _reportedOpenFailures;
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

        internal void Poll(IEnumerable<int> processIds)
        {
            var live = new HashSet<int>(processIds.Where(id => id > 0));
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
                    try
                    {
                        session = Session.Open(processId, _profile, _log);
                        _sessions.Add(processId, session);
                        _reportedOpenFailures.Remove(processId);
                        _log("Memória PID " + processId + ": sessão aberta.");
                    }
                    catch (Exception error)
                    {
                        if (_reportedOpenFailures.Add(processId))
                            _log("Memória PID " + processId +
                                ": falha ao abrir sessão: " + error.Message);
                        continue;
                    }
                }
                session.Poll();
            }
        }

        internal QuestMemoryClientState Find(int processId)
        {
            Session session;
            return _sessions.TryGetValue(processId, out session)
                ? session.State : null;
        }

        public void Dispose()
        {
            foreach (Session session in _sessions.Values) session.Dispose();
            _sessions.Clear();
        }

        private sealed class Session : IDisposable
        {
            private readonly Process _process;
            private readonly ProcessMemoryReader _memory;
            private readonly QuestMemoryReader _reader;
            private readonly QuestStateTracker _tracker;
            private readonly long _player;
            private readonly Action<string> _log;
            private string _candidateSignature;
            private string _committedSignature;
            private int _candidateCount;

            private Session(
                Process process,
                ProcessMemoryReader memory,
                QuestMemoryReader reader,
                long player,
                Action<string> log)
            {
                _process = process;
                _memory = memory;
                _reader = reader;
                _player = player;
                _log = log;
                _tracker = new QuestStateTracker();
                State = new QuestMemoryClientState();
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
                        log);
                }
                catch
                {
                    if (memory != null) memory.Dispose();
                    process.Dispose();
                    throw;
                }
            }

            internal void Poll()
            {
                if (_process.HasExited) return;
                try
                {
                    QuestMemorySnapshot snapshot = _reader.Read(_player);
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
                        return;

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
                }
                catch
                {
                    // Uma leitura isolada nunca altera o estado confirmado.
                }
            }

            public void Dispose()
            {
                _memory.Dispose();
                _process.Dispose();
            }

            private static string Signature(QuestMemorySnapshot snapshot)
            {
                return string.Join(",", snapshot.ActiveTasks
                    .Select(entry => entry.TaskId + ":" + entry.StateCode)
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
