using System;
using System.Collections.Generic;
using System.Linq;
using PWQuestOverlay.Memory;

namespace PWQuestOverlay
{
    internal static class QuestProgression
    {
        internal static string StageKey(QuestMission mission, QuestStep step)
        {
            return mission.id + "|STEP|" + step.id;
        }

        internal static bool IsDone(string value)
        {
            return value == "done" || value == "done-memory" || value == "done-ocr";
        }

        internal static bool MissionDone(QuestMission mission, CharacterProgress progress)
        {
            if (mission.cultivationChoice) return false;
            var states = progress.MissionStates;
            string value;
            if (mission.steps.All(step => states.TryGetValue(StageKey(mission, step), out value) && IsDone(value)))
                return true;
            return states.TryGetValue(mission.id, out value) && IsDone(value) &&
                !mission.steps.Any(step => states.ContainsKey(StageKey(mission, step)));
        }

        internal static long RewardXp(QuestMission mission)
        {
            return Math.Max(0L, mission.xp3 ?? (3L * (mission.baseXp ?? 0)));
        }

        internal static long CompletedXp(IEnumerable<QuestMission> missions, CharacterProgress progress)
        {
            var credited = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (QuestMission mission in missions)
            {
                long reward = RewardXp(mission);
                if (reward == 0 || !MissionDone(mission, progress)) continue;
                uint[][] groups = mission.historyTaskGroups;
                string key = groups == null || groups.Length == 0 ? mission.id :
                    string.Join(";", groups.Select(group => string.Join(",", group.OrderBy(id => id))).OrderBy(value => value));
                long existing;
                credited[key] = credited.TryGetValue(key, out existing) ? Math.Min(existing, reward) : reward;
            }
            return credited.Values.Sum();
        }

        internal static int ExpectedLevel(int[] xpToNextLevel, long completedXp)
        {
            if (xpToNextLevel == null || xpToNextLevel.Length != 104) return 0;
            int level = 1;
            foreach (int required in xpToNextLevel)
            {
                if (required <= 0) return 0;
                if (completedXp < required) break;
                completedXp -= required;
                level++;
            }
            return level;
        }

        internal static HashSet<uint> ActiveRoots(TaskCatalog catalog, QuestMemorySnapshot snapshot)
        {
            var roots = new HashSet<uint>();
            if (catalog == null || snapshot == null) return roots;
            foreach (ActiveQuestMemoryEntry active in snapshot.ActiveTasks)
            {
                TaskCatalogEntry task = catalog.Find(active.TaskId);
                if (task != null) roots.Add(task.TopId);
            }
            return roots;
        }

        internal static QuestMission NextMission(IList<QuestMission> pending, CharacterProgress progress,
            int level, HashSet<uint> activeRoots)
        {
            QuestMission required = pending.FirstOrDefault(mission => !mission.xpOptional ||
                mission.steps.Any(step => HasDoneStep(progress, mission, step)) ||
                (mission.historyTaskGroups != null &&
                    mission.historyTaskGroups.Any(group => group.Any(activeRoots.Contains))));
            if (required == null || required.minLevel <= level) return required;

            // Use the largest reward available before the next protected level gate.
            // Candidates are independent XP missions, verified against the client catalog.
            return pending.Where(mission => mission.xpOptional && IsAvailable(mission, level))
                .OrderByDescending(RewardXp).FirstOrDefault() ?? required;
        }

        private static bool HasDoneStep(CharacterProgress progress, QuestMission mission, QuestStep step)
        {
            string status;
            return progress.MissionStates.TryGetValue(StageKey(mission, step), out status) && IsDone(status);
        }

        private static bool IsAvailable(QuestMission mission, int level)
        {
            int maximum;
            return mission.minLevel <= level &&
                (!int.TryParse(mission.maxLevel, out maximum) || maximum <= 0 || level <= maximum);
        }

        internal static uint ResolveStageTaskId(TaskCatalog catalog, QuestMission mission, QuestStep step)
        {
            if (step == null || string.IsNullOrWhiteSpace(step.title)) return 0;
            string[] titles = step.title.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries);
            if (step.memoryTaskId != 0)
            {
                TaskCatalogEntry bound = catalog.Find(step.memoryTaskId);
                return bound != null && titles.Contains(bound.Name, StringComparer.Ordinal) ? bound.Id : 0;
            }

            var matches = titles.SelectMany(catalog.FindByName).Distinct().ToList();
            if (mission != null && mission.historyTaskGroups != null && mission.historyTaskGroups.Length > 0)
            {
                var roots = new HashSet<uint>(mission.historyTaskGroups.SelectMany(group => group));
                matches = matches.Where(entry => roots.Contains(entry.TopId)).ToList();
            }
            else if (mission != null)
            {
                var roots = new HashSet<uint>(catalog.FindByName(mission.name).Select(entry => entry.TopId));
                var sameChain = matches.Where(entry => roots.Contains(entry.TopId)).ToList();
                if (sameChain.Count > 0) matches = sameChain;
            }
            return matches.Count == 1 ? matches[0].Id : 0;
        }

        internal static int ImportHistory(IEnumerable<QuestMission> missions, CharacterProgress progress,
            TaskCatalog catalog, QuestMemorySnapshot snapshot)
        {
            var finished = new HashSet<uint>(snapshot.FinishedTaskIds);
            if (finished.Count == 0) return 0;
            HashSet<uint> activeRoots = ActiveRoots(catalog, snapshot);
            finished.ExceptWith(activeRoots);
            int changed = 0;
            foreach (QuestMission mission in missions)
            {
                if (mission.cultivationChoice || mission.repeatable) continue;
                uint[][] groups = mission.historyTaskGroups;
                bool full = groups != null && groups.Length > 0 &&
                    groups.All(group => group.Any(finished.Contains)) &&
                    !groups.Any(group => group.Any(activeRoots.Contains));
                bool modified = false;
                foreach (QuestStep step in mission.steps)
                {
                    if (!full)
                    {
                        uint taskId = ResolveStageTaskId(catalog, mission, step);
                        TaskCatalogEntry task = catalog.Find(taskId);
                        if (task == null || !finished.Contains(task.TopId)) continue;
                    }
                    modified |= RecordDone(progress, StageKey(mission, step));
                }
                if (full || mission.steps.All(step => HasDoneStep(progress, mission, step)))
                    modified |= RecordDone(progress, mission.id);
                if (modified) changed++;
            }
            return changed;
        }

        private static bool RecordDone(CharacterProgress progress, string key)
        {
            string status;
            if (progress.MissionStates.TryGetValue(key, out status) && IsDone(status)) return false;
            progress.MissionStates[key] = "done-memory";
            return true;
        }
    }
}
