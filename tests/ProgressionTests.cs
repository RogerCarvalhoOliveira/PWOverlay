using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using PWQuestOverlay;
using PWQuestOverlay.Memory;

internal static class ProgressionTests
{
    internal static void TestHistoryReader(MemoryLayoutProfile profile)
    {
        IntPtr player = Marshal.AllocHGlobal(0x1800);
        IntPtr taskInterface = Marshal.AllocHGlobal(32);
        IntPtr active = Marshal.AllocHGlobal(64);
        IntPtr finished = Marshal.AllocHGlobal(32);
        try
        {
            Marshal.Copy(new byte[0x1800], 0, player, 0x1800);
            Marshal.Copy(new byte[32], 0, taskInterface, 32);
            Marshal.Copy(new byte[64], 0, active, 64);
            Marshal.Copy(new byte[32], 0, finished, 32);
            var layout = profile.quests;
            Marshal.WriteInt32(taskInterface, layout.ActiveListPointerOffset, active.ToInt32());
            Marshal.WriteInt32(taskInterface, layout.FinishedListPointerOffset, finished.ToInt32());
            Marshal.WriteByte(active, layout.ActiveCountOffset, 1);
            Marshal.WriteInt16(active, layout.ActiveEntriesOffset + layout.ActiveIdOffset, 1173);
            Marshal.WriteInt16(finished, layout.FinishedCountOffset, 2);
            Marshal.WriteInt32(finished, layout.FinishedEntriesOffset, 35);
            Marshal.WriteInt32(finished, layout.FinishedEntriesOffset + layout.FinishedEntryStride, 1174);
            using (Process process = Process.GetCurrentProcess())
            using (ProcessMemoryReader memory = ProcessMemoryReader.Open(process, process.MainModule.ModuleName))
            {
                var reader = new QuestMemoryReader(memory, layout, profile.inventory);
                QuestMemorySnapshot result = reader.ReadFromInterface(player.ToInt64(), taskInterface.ToInt64(), false);
                Check(result.ActiveTasks.Count == 1 && result.ActiveTasks[0].TaskId == 1173 &&
                    result.FinishedTaskIds.SequenceEqual(new uint[] { 35, 1174 }),
                    "History-only reads must decode active and finished tasks without accessing the inventory.");
                if (layout.FinishedIdBytes == 2)
                {
                    Marshal.WriteInt16(finished, layout.FinishedCountOffset, 3);
                    int third = layout.FinishedEntriesOffset + 2 * layout.FinishedEntryStride;
                    Marshal.WriteInt16(finished, third, unchecked((short)61531));
                    Marshal.WriteByte(finished, third + 3, 7);
                    Marshal.WriteByte(finished, layout.FinishedEntriesOffset + layout.finishedFailureFlagOffset, 1);
                    result = reader.ReadFromInterface(player.ToInt64(), taskInterface.ToInt64(), false);
                    Check(result.FinishedTaskIds.SequenceEqual(new uint[] { 1174, 61531 }),
                        "1.2.6 history must ignore failed tasks and keep IDs separate from metadata.");
                }
                bool inventoryRejected = false;
                try { reader.ReadFromInterface(player.ToInt64(), taskInterface.ToInt64(), true); }
                catch (InvalidOperationException) { inventoryRejected = true; }
                Check(inventoryRejected, "The fixture's unavailable inventory must be accessed only for live objective reads.");
                Marshal.WriteInt16(finished, layout.FinishedCountOffset, (short)(layout.FinishedMaximum + 1));
                bool countRejected = false;
                try { reader.ReadFromInterface(player.ToInt64(), taskInterface.ToInt64(), false); }
                catch (InvalidOperationException) { countRejected = true; }
                Check(countRejected, "An invalid history count must be rejected before reading the list.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(player);
            Marshal.FreeHGlobal(taskInterface);
            Marshal.FreeHGlobal(active);
            Marshal.FreeHGlobal(finished);
        }
    }

    internal static void TestClassic126Mapping(string path, TaskCatalog catalog)
    {
        var local = new QuestMemorySnapshot();
        local.FinishedTaskIds.AddRange(new uint[] { 60313, 60312, 61531, 61534, 65535 });
        local.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 61534 });
        QuestMemorySnapshot translated = QuestProgression.TranslateHistory126(path, local);
        Check(new HashSet<uint>(translated.FinishedTaskIds).SetEquals(new uint[] { 2566, 2569, 934 }),
            "The EA aliases must resolve to EA roots. Unknown IDs and completed children are not proof of a completed chain.");
        Check(QuestProgression.ActiveRoots(catalog, translated).SetEquals(new uint[] { 934 }),
            "An active child must prevent importing the whole chain.");

        local.PlayerLevel = 20;
        translated = QuestProgression.TranslateHistory126(path, local);
        Check(translated.Availability[2714] == QuestAvailability.Available,
            "Confiança dos Guardas is still available at 20 in the 1.2.6 catalog.");
        local.PlayerLevel = 40;
        local.PlayerReputation = 500;
        translated = QuestProgression.TranslateHistory126(path, local);
        Check(translated.Availability[2714] == QuestAvailability.Expired &&
            !translated.FinishedTaskIds.Contains(2714), "An expired mission is not completed history.");
        Check(translated.Availability[6517] == QuestAvailability.Blocked,
            "A rank event without an offer cannot be suggested just because the reputation requirement is met.");
        Check(translated.Availability[990] == QuestAvailability.Blocked,
            "A missing prerequisite must block automatic selection.");
        local.FinishedTaskIds.Add(923);
        translated = QuestProgression.TranslateHistory126(path, local);
        Check(translated.Availability[990] == QuestAvailability.Available,
            "Confirmed prerequisites must unlock eligible missions.");

        var activeMission = Mission("active", 1, 100, false);
        activeMission.historyTaskGroups = new[] { new uint[] { 934 } };
        activeMission.steps[0].action = "Aceite a missão";
        activeMission.steps.Add(new QuestStep { id = "deliver", title = "Aldeões Feridos", action = "Entregue a missão" });
        var progress = new CharacterProgress();
        Check(QuestProgression.ImportHistory(new[] { activeMission }, progress, catalog, translated) == 1 &&
            progress.MissionStates.ContainsKey(QuestProgression.StageKey(activeMission, activeMission.steps[0])) &&
            !progress.MissionStates.ContainsKey(activeMission.id),
            "An active quest confirms its acceptance without recording the delivery.");
    }

    internal static void TestMineHistory(string path, TaskCatalog catalog, RouteRepository routes)
    {
        QuestMission mine = routes.GetClassMissions("EA").Single(mission => mission.name == "A Batalha da Mina");
        var local = new QuestMemorySnapshot { PlayerLevel = 35 };
        local.FinishedTaskIds.Add(824);
        var progress = new CharacterProgress();
        QuestMemorySnapshot translated = QuestProgression.TranslateHistory126(path, local);
        QuestProgression.ImportHistory(new[] { mine }, progress, catalog, translated);
        Check(progress.MissionStates.ContainsKey(QuestProgression.StageKey(mine, mine.steps[0])) &&
            !progress.MissionStates.ContainsKey(QuestProgression.StageKey(mine, mine.steps[4])) &&
            !QuestProgression.MissionDone(mine, progress), "The first mine delivery must not complete its continuations.");
        local.FinishedTaskIds.AddRange(new uint[] { 61608, 61613 });
        translated = QuestProgression.TranslateHistory126(path, local);
        QuestProgression.ImportHistory(new[] { mine }, progress, catalog, translated);
        Check(QuestProgression.MissionDone(mine, progress), "All three mine records, across verified IDs, complete the chain.");
        QuestMission hero = routes.GetClassMissions("EA").Single(mission => mission.name == "Memória do Herói");
        var localTasks = QuestProgression.LoadClassic126Tasks(path);
        var heroActive = QuestProgression.TranslateActive126(localTasks, new uint[] { 60919, 60920 });
        var unrelated = QuestProgression.TranslateActive126(localTasks, new uint[] { 60934 });
        var bones = QuestProgression.TranslateActive126(localTasks, new uint[] { 60919, 60921 });
        Check(QuestProgression.Classic126Evidence(hero, hero.steps[0], catalog, heroActive) != null &&
            QuestProgression.Classic126Evidence(hero, hero.steps[0], catalog, unrelated) == null &&
            QuestProgression.Classic126Evidence(hero, hero.steps[1], catalog, heroActive) == null,
            "The 1.2.6 EA acceptance must match the right quest, and not advance talk steps.");
        Check(QuestProgression.Classic126Evidence(hero, hero.steps[1], catalog, bones) != null &&
            QuestProgression.Classic126Evidence(hero, hero.steps[2], catalog, bones) == null &&
            QuestProgression.Classic126Evidence(hero, hero.steps[1], catalog, unrelated) == null,
            "Bones appearing after the old blacksmith must advance only the completed step of the same ordered chain.");
        translated.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1953 });
        var saved = QuestProgression.RestoreImport(QuestProgression.RememberImport(new[] { hero }, catalog, translated));
        Check(saved.PlayerLevel == 35 && saved.Availability.ContainsKey(1953) &&
            saved.ActiveTasks.Count == 0 && saved.FinishedTaskIds.Count == 0,
            "Reopening restores eligibility without replaying stale active or finished quest observations.");
    }

    internal static void Run(RouteRepository routes, TaskCatalog catalog)
    {
        var first = Mission("first", 1, 225, false);
        var optional = Mission("optional", 1, 50, true);
        var high = Mission("high", 1, 500, true);
        var locked = Mission("locked", 5, 100, false);
        var progress = new CharacterProgress();

        Check(QuestProgression.ExpectedLevel(routes.XpToNextLevel, 0) == 1, "New profile starts at expected level 1.");
        Check(QuestProgression.ExpectedLevel(routes.XpToNextLevel, 274) == 2, "Level boundary before 3 is wrong.");
        Check(QuestProgression.ExpectedLevel(routes.XpToNextLevel, 275) == 3, "Level 3 requires 275 cumulative XP.");
        Check(QuestProgression.ExpectedLevel(routes.XpToNextLevel, 147605) == 20, "Classic level 20 curve is wrong.");
        Check(QuestProgression.ExpectedLevel(routes.XpToNextLevel, long.MaxValue) == 105, "Expected level must stop at 105.");
        progress.MissionStates[first.id] = "done";
        Check(QuestProgression.CompletedXp(new[] { first }, progress) == 225, "Completed reward must use quest XP x3 once.");
        progress.MissionStates[optional.id] = "skipped";
        Check(QuestProgression.CompletedXp(new[] { first, optional }, progress) == 225, "Skipped quests must not grant forecast XP.");
        progress.MissionStates[first.id] = "done-memory";
        progress.MissionStates[QuestProgression.StageKey(first, first.steps[0])] = "done-memory";
        Check(QuestProgression.CompletedXp(new[] { first }, progress) == 225, "Mission and step completion must not duplicate XP.");
        var alias = Mission("alias", 1, 225, false);
        first.historyTaskGroups = alias.historyTaskGroups = new[] { new uint[] { 1173 } };
        progress.MissionStates[alias.id] = "done";
        Check(QuestProgression.CompletedXp(new[] { first, alias }, progress) == 225, "Duplicate presentations of one quest must not duplicate XP.");
        progress.MissionStates[QuestProgression.StageKey(first, first.steps[0])] = "skipped";
        Check(QuestProgression.CompletedXp(new[] { first }, progress) == 0, "A skipped objective must not credit a completed quest reward.");

        progress = new CharacterProgress();
        var pending = new[] { optional, high, locked };
        Check(QuestProgression.NextMission(pending, progress, 1, new HashSet<uint>()) == high,
            "Before a level gate, choose the largest eligible independent XP reward.");
        Check(QuestProgression.NextMission(pending, progress, 5, new HashSet<uint>()) == locked,
            "Once the protected mission is unlocked, unnecessary XP quests should be omitted.");
        progress.MissionStates[QuestProgression.StageKey(optional, optional.steps[0])] = "done";
        optional.steps.Add(new QuestStep { id = "turn-in", title = "Deliver", action = "Entregue" });
        Check(QuestProgression.NextMission(pending, progress, 5, new HashSet<uint>()) == optional,
            "An accepted/started quest must remain available to finish.");
        progress.MissionStates.Clear();
        optional.historyTaskGroups = new[] { new uint[] { 1173 } };
        Check(QuestProgression.NextMission(pending, progress, 5, new HashSet<uint> { 1173 }) == optional,
            "An active quest must remain available even without local acceptance marks.");
        high.maxLevel = "2";
        Check(QuestProgression.NextMission(pending, progress, 3, new HashSet<uint>()) == optional,
            "Expired XP quests cannot be selected.");
        Check(QuestProgression.NextMission(new[] { optional }, progress, 20, new HashSet<uint>()) == null,
            "Unneeded XP quests must not be forced after protected work is finished.");

        foreach (string code in new[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS" })
        {
            List<QuestMission> missions = routes.GetClassMissions(code);
            Check(missions.Any(m => m.xpOptional), "No reduced-route candidates for " + code);
            foreach (QuestMission mission in missions.Where(m => m.xpOptional))
                Check(mission.priority == "Alta" || mission.priority == "Opcional", "Protected priority became optional.");
            var snapshot = new QuestMemorySnapshot { PlayerLevel = 60 };
            foreach (QuestMission mission in missions.Where(m => !m.repeatable && m.historyTaskGroups != null))
                foreach (uint[] group in mission.historyTaskGroups)
                    snapshot.FinishedTaskIds.Add(group[0]);
            var restored = new CharacterProgress { ClassCode = code };
            int imported = QuestProgression.ImportHistory(missions, restored, catalog, snapshot);
            Check(imported > 100, "History must reconcile the complete route for " + code);
            Check(QuestProgression.ImportHistory(missions, restored, catalog, snapshot) == 0, "History import must be idempotent.");
            Check(QuestProgression.ExpectedLevel(routes.XpToNextLevel, QuestProgression.CompletedXp(missions, restored)) > 1,
                "Restored completed quests must contribute to the expected level.");
        }

        var unknown = Mission("unmapped", 1, 10, false);
        var known = routes.GetForClass("WR").First(m => m.id.Contains("Primeiro Teste"));
        var later = routes.GetForClass("WR").First(m => m.id.Contains("Revitalizando"));
        var history = new QuestMemorySnapshot { PlayerLevel = 20 };
        history.FinishedTaskIds.Add(known.historyTaskGroups[0][0]);
        history.FinishedTaskIds.Add(later.historyTaskGroups[0][0]);
        progress = new CharacterProgress();
        QuestProgression.ImportHistory(new[] { unknown, known, later }, progress, catalog, history);
        Check(!progress.MissionStates.ContainsKey(unknown.id) && QuestProgression.MissionDone(later, progress),
            "An unresolved early stage cannot block importing later confirmed quests.");
        var active = new QuestMemorySnapshot { PlayerLevel = 20 };
        active.FinishedTaskIds.Add(known.historyTaskGroups[0][0]);
        active.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = known.historyTaskGroups[0][0] });
        progress = new CharacterProgress();
        QuestProgression.ImportHistory(new[] { known }, progress, catalog, active);
        Check(progress.MissionStates.Count == 0, "An active chain cannot be imported as complete.");
        history.FinishedTaskIds.Clear();
        QuestProgression.ImportHistory(new[] { known }, progress, catalog, history);
        Check(progress.MissionStates.Count == 0, "Level or absence alone cannot prove a quest was completed.");
        var repeated = Mission("repeated", 1, 100, false);
        repeated.repeatable = true;
        repeated.historyTaskGroups = known.historyTaskGroups;
        history.FinishedTaskIds.Add(known.historyTaskGroups[0][0]);
        QuestProgression.ImportHistory(new[] { repeated }, progress, catalog, history);
        Check(progress.MissionStates.Count == 0, "Old history cannot complete a new repeatable cycle.");
        QuestMission dungeon = routes.GetForClass("WR").First(m => m.id.Contains("DG29"));
        var coinHistory = new QuestMemorySnapshot();
        coinHistory.FinishedTaskIds.Add(8741);
        progress = new CharacterProgress();
        QuestProgression.ImportHistory(new[] { dungeon }, progress, catalog, coinHistory);
        Check(!QuestProgression.MissionDone(dungeon, progress), "Receiving a dungeon coin cannot prove the bosses were killed.");

        foreach (QuestMission mission in routes.AllMissions.Where(m => m.historyTaskGroups != null))
            foreach (uint root in mission.historyTaskGroups.SelectMany(group => group))
                Check(catalog.Find(root) != null && catalog.Find(root).TopId == root, "History metadata must refer to real root tasks.");
    }

    private static QuestMission Mission(string id, int level, int xp, bool optional)
    {
        return new QuestMission
        {
            id = id, name = id, minLevel = level, xp3 = xp, xpOptional = optional,
            steps = new List<QuestStep> { new QuestStep { id = "step", title = "Unknown " + id, action = "Fale" } }
        };
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
}
