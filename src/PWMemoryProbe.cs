using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
                for (int index = 0; index < args.Length; index++)
                {
                    if (args[index] == "--layout" && index + 1 < args.Length)
                        layoutPath = args[++index];
                    else if (args[index] == "--profile" && index + 1 < args.Length)
                        profileId = args[++index];
                }

                MemoryLayoutFile layouts = MemoryLayoutFile.Load(layoutPath);
                MemoryLayoutProfile profile = layouts.Find(profileId);
                if (profile == null)
                    throw new InvalidDataException("Perfil desconhecido: " + profileId);
                profile.Validate();

                Process[] games = Process.GetProcessesByName("elementclient");
                if (games.Length == 0)
                {
                    Console.WriteLine("Nenhum elementclient aberto.");
                    return 2;
                }

                int failures = 0;
                foreach (Process game in games)
                {
                    using (game)
                    {
                        try { Probe(game, profile); }
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

        private static void Probe(Process game, MemoryLayoutProfile profile)
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

                ProbeTasks(memory, profile, player);
            }
        }

        private static void ProbeTasks(
            ProcessMemoryReader memory,
            MemoryLayoutProfile profile,
            long player)
        {
            QuestMemorySnapshot snapshot = new QuestMemoryReader(
                memory, profile.quests).Read(player);
            Console.WriteLine(
                "  TaskInterface 0x{0:X8} | ativas: {1} | concluídas: {2}",
                snapshot.TaskInterfaceAddress,
                JoinActive(snapshot.ActiveTasks),
                snapshot.FinishedTaskIds.Count == 0
                    ? "nenhuma"
                    : string.Join(", ", snapshot.FinishedTaskIds));
        }

        private static string JoinActive(List<ActiveQuestMemoryEntry> entries)
        {
            if (entries.Count == 0) return "nenhuma";
            var ids = new List<string>();
            foreach (ActiveQuestMemoryEntry entry in entries)
                ids.Add(entry.TaskId.ToString());
            return string.Join(", ", ids);
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
    }
}
