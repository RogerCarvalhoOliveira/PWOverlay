using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using PWQuestOverlay.Memory;

namespace PWQuestOverlay.Tools
{
    internal static class PWMemoryProbe
    {
        private static int Main(string[] args)
        {
            try
            {
                string layoutPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "memory_offsets.json");
                string profileId = "the_classic_144";
                bool watch = false;
                int watchInterval = 500;
                string catalogPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "task_catalog_144.tsv");
                for (int index = 0; index < args.Length; index++)
                {
                    if (args[index] == "--layout" && index + 1 < args.Length)
                        layoutPath = args[++index];
                    else if (args[index] == "--profile" && index + 1 < args.Length)
                        profileId = args[++index];
                    else if (args[index] == "--catalog" && index + 1 < args.Length)
                        catalogPath = args[++index];
                    else if (args[index] == "--watch")
                        watch = true;
                    else if (args[index] == "--interval" && index + 1 < args.Length)
                        watchInterval = ParseInterval(args[++index]);
                }

                MemoryLayoutFile layouts = MemoryLayoutFile.Load(layoutPath);
                MemoryLayoutProfile profile = layouts.Find(profileId);
                if (profile == null)
                    throw new InvalidDataException("Perfil desconhecido: " + profileId);
                profile.Validate();
                TaskCatalog catalog = TaskCatalog.Load(catalogPath);

                Process[] games = Process.GetProcessesByName("elementclient");
                if (games.Length == 0)
                {
                    Console.WriteLine("Nenhum elementclient aberto.");
                    return 2;
                }

                if (watch)
                    return Watch(games, profile, catalog, watchInterval);

                int failures = 0;
                foreach (Process game in games)
                {
                    using (game)
                    {
                        try { Probe(game, profile, catalog); }
                        catch (Exception error)
                        {
                            failures++;
                            Console.Error.WriteLine(
                                "PID {0}: FALHA: {1}", game.Id, error.Message);
                        }
                    }
                }
                return failures == 0 ? 0 : 1;
            }
            catch (Exception error)
            {
                Console.Error.WriteLine("FALHA: " + error.Message);
                return 1;
            }
        }

        private static int ParseInterval(string value)
        {
            int parsed;
            if (!int.TryParse(value, out parsed) || parsed < 100 || parsed > 10000)
                throw new ArgumentOutOfRangeException(
                    "value", "O intervalo deve ficar entre 100 e 10000 ms.");
            return parsed;
        }

        private static int Watch(
            Process[] games,
            MemoryLayoutProfile profile,
            TaskCatalog catalog,
            int interval)
        {
            var sessions = new List<WatchSession>();
            try
            {
                foreach (Process game in games)
                {
                    try
                    {
                        var session = new WatchSession(game, profile, catalog);
                        sessions.Add(session);
                        session.PrintBaseline();
                    }
                    catch (Exception error)
                    {
                        Console.Error.WriteLine(
                            "PID {0}: FALHA: {1}", game.Id, error.Message);
                        game.Dispose();
                    }
                }
                if (sessions.Count == 0) return 1;

                bool cancel = false;
                Console.CancelKeyPress += delegate(object sender, ConsoleCancelEventArgs eventArgs)
                {
                    eventArgs.Cancel = true;
                    cancel = true;
                };
                Console.WriteLine(
                    "Monitorando alterações a cada {0} ms. Pressione Ctrl+C para sair.",
                    interval);
                while (!cancel)
                {
                    Thread.Sleep(interval);
                    foreach (WatchSession session in sessions)
                        session.Poll();
                }
                return 0;
            }
            finally
            {
                foreach (WatchSession session in sessions) session.Dispose();
            }
        }

        private static void Probe(
            Process game,
            MemoryLayoutProfile profile,
            TaskCatalog catalog)
        {
            using (ProcessMemoryReader memory = ProcessMemoryReader.Open(
                game, profile.module))
            {
                long player = ResolvePlayer(memory, profile);
                int level = memory.ReadInt32(player + profile.AttributeOffset("level"));
                int hp = memory.ReadInt32(player + profile.AttributeOffset("hp"));
                int hpMax = memory.ReadInt32(player + profile.AttributeOffset("hpMax"));
                int mp = memory.ReadInt32(player + profile.AttributeOffset("mp"));
                int mpMax = memory.ReadInt32(player + profile.AttributeOffset("mpMax"));

                Validate("nível", level, 1, 150);
                ValidatePair("HP", hp, hpMax);
                ValidatePair("MP", mp, mpMax);

                Console.WriteLine(
                    "PID {0} | {1} | base 0x{2:X8} | player 0x{3:X8} | Nv. {4} | HP {5}/{6} | MP {7}/{8}",
                    game.Id,
                    string.IsNullOrWhiteSpace(game.MainWindowTitle)
                        ? "sem título" : game.MainWindowTitle,
                    memory.ModuleBase.ToInt64(),
                    player,
                    level,
                    hp,
                    hpMax,
                    mp,
                    mpMax);

                ProbeTasks(memory, profile, player, catalog);
            }
        }

        private static void ProbeTasks(
            ProcessMemoryReader memory,
            MemoryLayoutProfile profile,
            long player,
            TaskCatalog catalog)
        {
            QuestMemorySnapshot snapshot = new QuestMemoryReader(
                memory, profile.quests).Read(player);
            Console.WriteLine(
                "  TaskInterface 0x{0:X8} | catálogo: {1} | ativas: {2} | histórico: {3}",
                snapshot.TaskInterfaceAddress,
                catalog.Count,
                snapshot.ActiveTasks.Count,
                snapshot.FinishedTaskIds.Count == 0
                    ? "nenhuma"
                    : string.Join(", ", snapshot.FinishedTaskIds));
            foreach (ActiveQuestMemoryEntry entry in snapshot.ActiveTasks)
                Console.WriteLine("    " + FormatActive(entry, catalog));
        }

        private static string FormatActive(
            ActiveQuestMemoryEntry entry,
            TaskCatalog catalog)
        {
            TaskCatalogEntry task = catalog.Find(entry.TaskId);
            string name = task == null || string.IsNullOrWhiteSpace(task.Name)
                ? "<sem nome>" : task.Name;
            string parent = entry.ParentIndex < 0
                ? "raiz" : "índice-pai " + entry.ParentIndex;
            return string.Format(
                "ID {0} | {1} | {2} | estado {3} (sucesso={4}, finalizada={5}) | flags 0x{6:X2} | catálogo-pai {7}",
                entry.TaskId,
                name,
                parent,
                entry.StateCode,
                entry.SuccessFlag ? "sim" : "não",
                entry.FinishedFlag ? "sim" : "não",
                entry.Flags,
                task == null || task.ParentId == 0 ? "-" : task.ParentId.ToString());
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

            string className = profile.player.className;
            if (string.IsNullOrWhiteSpace(className))
                throw new InvalidDataException(
                    "Ponteiro estático inválido e classe RTTI não configurada.");
            foreach (uint vtable in RttiResolver.FindVTables(memory, className))
            {
                long candidate = memory.FindObjectByVTable(
                    vtable,
                    address => IsValidPlayer(memory, profile, address));
                if (candidate != 0) return candidate;
            }
            throw new InvalidDataException(
                "Não foi possível localizar " + className + " por RTTI.");
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

        private static void Validate(string name, int value, int minimum, int maximum)
        {
            if (value < minimum || value > maximum)
                throw new InvalidDataException(
                    name + " fora do intervalo esperado: " + value);
        }

        private static void ValidatePair(string name, int current, int maximum)
        {
            if (current < 0 || maximum <= 0 || current > maximum)
                throw new InvalidDataException(
                    name + " inválido: " + current + "/" + maximum);
        }

        private sealed class WatchSession : IDisposable
        {
            private readonly Process _game;
            private readonly ProcessMemoryReader _memory;
            private readonly QuestMemoryReader _reader;
            private readonly QuestStateTracker _tracker;
            private readonly TaskCatalog _catalog;
            private readonly long _player;

            internal WatchSession(
                Process game,
                MemoryLayoutProfile profile,
                TaskCatalog catalog)
            {
                _game = game;
                _catalog = catalog;
                _memory = ProcessMemoryReader.Open(game, profile.module);
                try
                {
                    _player = ResolvePlayer(_memory, profile);
                    _reader = new QuestMemoryReader(_memory, profile.quests);
                    _tracker = new QuestStateTracker();
                }
                catch
                {
                    _memory.Dispose();
                    throw;
                }
            }

            internal void PrintBaseline()
            {
                QuestMemorySnapshot snapshot = _reader.Read(_player);
                _tracker.SetBaseline(snapshot);
                Console.WriteLine(
                    "PID {0} | {1} | baseline com {2} entradas ativas",
                    _game.Id,
                    string.IsNullOrWhiteSpace(_game.MainWindowTitle)
                        ? "sem título" : _game.MainWindowTitle,
                    snapshot.ActiveTasks.Count);
                foreach (ActiveQuestMemoryEntry entry in snapshot.ActiveTasks)
                    Console.WriteLine("    " + FormatActive(entry, _catalog));
            }

            internal void Poll()
            {
                try
                {
                    foreach (QuestMemoryEvent change in _tracker.Update(
                        _reader.Read(_player)))
                    {
                        TaskCatalogEntry task = _catalog.Find(change.TaskId);
                        string name = task == null || string.IsNullOrWhiteSpace(task.Name)
                            ? "<sem nome>" : task.Name;
                        string detail = change.Kind == QuestMemoryEventKind.StateChanged
                            ? change.PreviousState + " -> " + change.CurrentState
                            : change.Kind == QuestMemoryEventKind.Activated
                                ? "estado " + change.CurrentState
                                : change.Kind == QuestMemoryEventKind.DisappearedAfterObservation
                                    ? "último estado " + change.PreviousState
                                    : "registrada no histórico";
                        Console.WriteLine(
                            "{0:HH:mm:ss.fff} | PID {1} | {2} | ID {3} | {4} | {5}",
                            DateTime.Now,
                            _game.Id,
                            change.Kind,
                            change.TaskId,
                            name,
                            detail);
                    }
                }
                catch (Exception error)
                {
                    Console.Error.WriteLine(
                        "PID {0}: leitura temporariamente inválida: {1}",
                        _game.Id, error.Message);
                }
            }

            public void Dispose()
            {
                _memory.Dispose();
                _game.Dispose();
            }
        }
    }
}
