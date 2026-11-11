using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
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

        internal static uint[] StageRoots(TaskCatalog catalog, QuestMission mission, QuestStep step)
        {
            uint[] review = CompoundReviewRoots(catalog, step);
            if (review.Length > 0) return review;
            TaskCatalogEntry task = catalog.Find(ResolveStageTaskId(catalog, mission, step));
            if (task != null) return new[] { task.TopId };
            return mission.historyTaskGroups == null ? new uint[0] :
                mission.historyTaskGroups.SelectMany(group => group).Distinct().ToArray();
        }

        private static uint[] CompoundReviewRoots(TaskCatalog catalog, QuestStep step)
        {
            if (step.title == null || step.action == null ||
                !step.action.TrimStart().StartsWith("Complete ", StringComparison.OrdinalIgnoreCase) ||
                !step.title.Contains(" / ")) return new uint[0];
            var roots = new HashSet<uint>();
            foreach (string title in step.title.Split(new[] { " / " }, StringSplitOptions.RemoveEmptyEntries))
            {
                uint[] candidates = catalog.FindByName(title).Select(task => task.TopId).Distinct().ToArray();
                if (candidates.Length != 1) return new uint[0];
                roots.Add(candidates[0]);
            }
            return roots.ToArray();
        }

        internal static QuestAvailability StageAvailability(QuestMission mission, uint[] roots, QuestMemorySnapshot snapshot)
        {
            if (snapshot.UsesClassic126Catalog && !mission.cultivationChoice)
            {
                return roots.Select(root =>
                {
                    QuestAvailability status;
                    return snapshot.Availability.TryGetValue(root, out status) ? status : QuestAvailability.Unknown;
                }).DefaultIfEmpty(QuestAvailability.Unknown).Max();
            }
            int maximum;
            if (int.TryParse(mission.maxLevel, out maximum) && maximum > 0 && maximum < snapshot.PlayerLevel)
                return QuestAvailability.Expired;
            return snapshot.PlayerLevel < mission.minLevel ? QuestAvailability.Blocked : QuestAvailability.Available;
        }

        internal static bool IsAcceptStage(string action)
        {
            return Regex.IsMatch(action ?? string.Empty,
                @"\b(?:aceite|pegue\s+a\s+miss[aã]o|receba\s+a\s+miss[aã]o|inicie\s+a\s+miss[aã]o)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        internal static Dictionary<uint, uint> LoadClassic126Tasks(string path)
        {
            var roots = new Dictionary<uint, uint>();
            foreach (string line in File.ReadLines(path).Skip(1))
            {
                string[] fields = line.Split('\t');
                uint local, root;
                if (fields.Length != 10 || !uint.TryParse(fields[0], out local) || !uint.TryParse(fields[9], out root))
                    throw new InvalidDataException("O mapa de missões do 1.2.6 está inválido.");
                roots.Add(local, root);
            }
            return roots;
        }

        internal static HashSet<uint> TranslateActive126(Dictionary<uint, uint> localTasks, IEnumerable<uint> localActive)
        {
            var active = new HashSet<uint>();
            foreach (uint local in localActive)
            {
                uint task;
                if (localTasks.TryGetValue(local, out task)) active.Add(task);
            }
            return active;
        }

        internal static string Classic126Evidence(QuestMission mission, QuestStep step, TaskCatalog catalog, HashSet<uint> active)
        {
            if (mission.cultivationChoice) return null;
            if (IsAcceptStage(step.action))
            {
                uint[] roots = StageRoots(catalog, mission, step);
                if (roots.Length == 1 && active.Select(catalog.Find).Any(task => task != null && task.TopId == roots[0]))
                    return "aceite confirmado na lista ativa do PW 1.2.6";
            }
            if (catalog.HasActiveLaterOrderedChild(ResolveStageTaskId(catalog, mission, step), active))
                return "submissão posterior confirmada na mesma cadeia do PW 1.2.6";
            return null;
        }

        internal static ImportedRouteState RememberImport(IEnumerable<QuestMission> missions, TaskCatalog catalog,
            QuestMemorySnapshot snapshot)
        {
            var roots = new HashSet<uint>(missions.SelectMany(mission => mission.steps
                .SelectMany(step => StageRoots(catalog, mission, step))));
            return new ImportedRouteState
            {
                PlayerLevel = snapshot.PlayerLevel, UsesClassic126Catalog = snapshot.UsesClassic126Catalog,
                Availability = snapshot.Availability.Where(pair => roots.Contains(pair.Key))
                    .ToDictionary(pair => pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture), pair => (int)pair.Value)
            };
        }

        internal static QuestMemorySnapshot RestoreImport(ImportedRouteState saved)
        {
            if (saved == null || saved.PlayerLevel < 1 || saved.PlayerLevel > 150) return null;
            var result = new QuestMemorySnapshot { PlayerLevel = saved.PlayerLevel, UsesClassic126Catalog = saved.UsesClassic126Catalog };
            if (saved.Availability == null) return result;
            foreach (var pair in saved.Availability)
            {
                uint id;
                if (uint.TryParse(pair.Key, out id) && pair.Value >= (int)QuestAvailability.Unknown &&
                    pair.Value <= (int)QuestAvailability.Available)
                    result.Availability[id] = (QuestAvailability)pair.Value;
            }
            return result;
        }

        internal static int ImportHistory(IEnumerable<QuestMission> missions, CharacterProgress progress,
            TaskCatalog catalog, QuestMemorySnapshot snapshot)
        {
            var finished = new HashSet<uint>(snapshot.FinishedTaskIds);
            HashSet<uint> activeRoots = ActiveRoots(catalog, snapshot);
            finished.ExceptWith(activeRoots);
            int changed = 0;
            foreach (QuestMission mission in missions)
            {
                if (mission.cultivationChoice || mission.repeatable) continue;
                uint[][] groups = mission.historyTaskGroups;
                var boundRoots = mission.steps.Where(step => step.memoryTaskId != 0)
                    .Select(step => catalog.Find(ResolveStageTaskId(catalog, mission, step)))
                    .Where(task => task != null).Select(task => task.TopId).Distinct();
                var groupedRoots = groups == null ? new HashSet<uint>() : new HashSet<uint>(groups.SelectMany(group => group));
                bool full = groups != null && groups.Length > 0 &&
                    groups.All(group => group.Any(finished.Contains)) &&
                    !groups.Any(group => group.Any(activeRoots.Contains)) &&
                    boundRoots.All(root => groupedRoots.Contains(root) || finished.Contains(root));
                bool modified = false;
                foreach (QuestStep step in mission.steps)
                {
                    if (!full)
                    {
                        uint taskId = ResolveStageTaskId(catalog, mission, step);
                        TaskCatalogEntry task = catalog.Find(taskId);
                        uint[] review = CompoundReviewRoots(catalog, step);
                        bool recorded = (task != null && finished.Contains(task.TopId)) ||
                            (review.Length > 0 && review.All(finished.Contains));
                        if (!recorded)
                        {
                            uint[] roots = StageRoots(catalog, mission, step);
                            if (roots.Length != 1 || !activeRoots.Contains(roots[0]) || !IsAcceptStage(step.action)) continue;
                        }
                    }
                    modified |= RecordDone(progress, StageKey(mission, step));
                }
                if (full || mission.steps.All(step => HasDoneStep(progress, mission, step)))
                    modified |= RecordDone(progress, mission.id);
                if (modified) changed++;
            }
            return changed;
        }

        internal static QuestMemorySnapshot TranslateHistory126(string path, QuestMemorySnapshot source)
        {
            var active = new HashSet<uint>(source.ActiveTasks.Select(task => task.TaskId));
            var finished = new HashSet<uint>(source.FinishedTaskIds);
            var snapshot = new QuestMemorySnapshot
            {
                UsesClassic126Catalog = true, PlayerLevel = source.PlayerLevel, PlayerReputation = source.PlayerReputation
            };
            foreach (string line in File.ReadLines(path).Skip(1))
            {
                string[] fields = line.Split('\t');
                uint local, root, canonicalTask;
                if (fields.Length != 10 || !uint.TryParse(fields[0], out local) || !uint.TryParse(fields[1], out root) ||
                    !uint.TryParse(fields[9], out canonicalTask))
                    throw new InvalidDataException("O mapa de missões do 1.2.6 está inválido.");
                if (active.Contains(local))
                    snapshot.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = canonicalTask });
                if (fields[2] != "1") continue;
                if (finished.Contains(local)) snapshot.FinishedTaskIds.Add(root);
                int minimum, maximum, reputation, required;
                if (!int.TryParse(fields[3], out minimum) || !int.TryParse(fields[4], out maximum) ||
                    !int.TryParse(fields[5], out reputation) || !int.TryParse(fields[6], out required)) continue;
                QuestAvailability status = QuestAvailability.Available;
                if (maximum > 0 && maximum < source.PlayerLevel) status = QuestAvailability.Expired;
                else if (minimum > source.PlayerLevel || fields[8] == "1" ||
                    (reputation > 0 && source.PlayerReputation.GetValueOrDefault() < reputation) ||
                    (required > 0 && fields[7].Split(',').Count(id => finished.Contains(uint.Parse(id))) < required))
                    status = QuestAvailability.Blocked;
                QuestAvailability previous;
                if (!snapshot.Availability.TryGetValue(root, out previous) || previous < status)
                    snapshot.Availability[root] = status;
            }
            return snapshot;
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
