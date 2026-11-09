using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace PWQuestOverlay.Memory
{
    internal sealed class TaskCatalogEntry
    {
        internal uint Id { get; set; }
        internal uint ParentId { get; set; }
        internal uint TopId { get; set; }
        internal int Depth { get; set; }
        internal string Name { get; set; }
        internal int Method { get; set; }
        internal int FinishType { get; set; }
        internal int MinimumLevel { get; set; }
        internal int MaximumLevel { get; set; }
        internal bool NeedRecord { get; set; }
        internal bool ReadyToNotify { get; set; }
        internal bool AutoDeliver { get; set; }
        internal Dictionary<uint, int> RequiredTaskItems { get; set; }
        internal uint RequiredMonsterId { get; set; }
        internal int RequiredMonsterKills { get; set; }
        internal bool ChildrenInOrder { get; set; }
        internal int CatalogOrder { get; set; }

        internal bool TryGetKillProgress(
            ActiveQuestMemoryEntry active, string action, out string evidence)
        {
            evidence = string.Empty;
            if (active == null || active.TaskId != Id || Method != 1 ||
                RequiredMonsterId == 0 || RequiredMonsterKills <= 0 ||
                RequiredMonsterKills > 255 || RequiredTaskItems.Count != 0 ||
                active.FirstMonsterKillCount < 0 || active.FirstMonsterKillCount > 255 ||
                !Regex.IsMatch(action ?? string.Empty, @"^\s*(?:matar|mate)\b", RegexOptions.IgnoreCase) ||
                Regex.IsMatch(action ?? string.Empty, @"\b(?:entreg|report|retorn|volt|fale)", RegexOptions.IgnoreCase))
                return false;
            evidence = RequiredMonsterId + "=" + active.FirstMonsterKillCount +
                "/" + RequiredMonsterKills;
            return active.FirstMonsterKillCount >= RequiredMonsterKills;
        }

        internal bool IsAutomaticKillStage(string action)
        {
            string ignored;
            return ParentId != 0 && FinishType == 0 && TryGetKillProgress(
                new ActiveQuestMemoryEntry { TaskId = Id,
                    FirstMonsterKillCount = RequiredMonsterKills }, action, out ignored);
        }
    }

    internal sealed class TaskCatalog
    {
        private readonly Dictionary<uint, TaskCatalogEntry> _entries;
        private readonly Dictionary<string, List<TaskCatalogEntry>> _byName;

        private TaskCatalog(
            Dictionary<uint, TaskCatalogEntry> entries,
            Dictionary<string, List<TaskCatalogEntry>> byName)
        {
            _entries = entries;
            _byName = byName;
        }

        internal int Count { get { return _entries.Count; } }

        internal static TaskCatalog Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Catálogo de missões não encontrado.", path);

            var entries = new Dictionary<uint, TaskCatalogEntry>();
            using (var reader = new StreamReader(path, Encoding.UTF8, true))
            {
                string header = reader.ReadLine();
                if (header == null || !header.StartsWith("id\tparent_id\t", StringComparison.Ordinal))
                    throw new InvalidDataException("Cabeçalho inválido no catálogo de missões.");

                string line;
                int lineNumber = 1;
                while ((line = reader.ReadLine()) != null)
                {
                    lineNumber++;
                    if (line.Length == 0) continue;
                    string[] fields = line.Split('\t');
                    if (fields.Length < 17)
                        throw new InvalidDataException(
                            "Linha incompleta no catálogo de missões: " + lineNumber);

                    var entry = new TaskCatalogEntry
                    {
                        Id = ParseUInt(fields[0], lineNumber),
                        ParentId = ParseUInt(fields[1], lineNumber),
                        TopId = ParseUInt(fields[2], lineNumber),
                        Depth = ParseInt(fields[3], lineNumber),
                        Name = fields[4],
                        Method = ParseInt(fields[5], lineNumber),
                        FinishType = ParseInt(fields[6], lineNumber),
                        MinimumLevel = ParseInt(fields[12], lineNumber),
                        MaximumLevel = ParseInt(fields[13], lineNumber),
                        NeedRecord = fields[14] == "1",
                        ReadyToNotify = fields[15] == "1",
                        AutoDeliver = fields[16] == "1"
                    };
                    entry.CatalogOrder = lineNumber;
                    entry.ChildrenInOrder = fields.Length >= 20 && fields[19] == "1";
                    entry.RequiredTaskItems = fields.Length >= 18
                        ? ParseItemRequirements(fields[17], lineNumber)
                        : new Dictionary<uint, int>();
                    if (fields.Length >= 19 && fields[18] != "-")
                    {
                        var kills = ParseItemRequirements(fields[18], lineNumber);
                        if (kills.Count != 1) throw new InvalidDataException(
                            "Objetivo de abate único inválido na linha " + lineNumber + ".");
                        foreach (var kill in kills)
                        {
                            entry.RequiredMonsterId = kill.Key;
                            entry.RequiredMonsterKills = kill.Value;
                        }
                    }
                    if (entry.Id != 0 && !entries.ContainsKey(entry.Id))
                        entries.Add(entry.Id, entry);
                }
            }
            if (entries.Count == 0)
                throw new InvalidDataException("Catálogo de missões vazio.");
            var byName = new Dictionary<string, List<TaskCatalogEntry>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (TaskCatalogEntry entry in entries.Values)
            {
                string name = (entry.Name ?? string.Empty).Trim();
                if (name.Length == 0) continue;
                List<TaskCatalogEntry> matches;
                if (!byName.TryGetValue(name, out matches))
                {
                    matches = new List<TaskCatalogEntry>();
                    byName.Add(name, matches);
                }
                matches.Add(entry);
            }
            return new TaskCatalog(entries, byName);
        }

        internal TaskCatalogEntry Find(uint id)
        {
            TaskCatalogEntry entry;
            return _entries.TryGetValue(id, out entry) ? entry : null;
        }

        internal List<TaskCatalogEntry> FindByName(string name)
        {
            List<TaskCatalogEntry> matches;
            if (string.IsNullOrWhiteSpace(name) ||
                !_byName.TryGetValue(name.Trim(), out matches))
                return new List<TaskCatalogEntry>();
            return new List<TaskCatalogEntry>(matches);
        }

        internal bool AreTaskItemsComplete(uint taskId, QuestMemorySnapshot snapshot)
        {
            return AreTaskItemsComplete(taskId, snapshot, null);
        }

        internal bool HasActiveTaskContext(uint taskId, QuestMemorySnapshot snapshot)
        {
            TaskCatalogEntry task = Find(taskId);
            if (task == null || snapshot == null)
                return false;
            // Completed automatic children may disappear before the parent is delivered.
            // Require the objective itself or a real ancestor of this chain to be active.
            var visited = new HashSet<uint>();
            TaskCatalogEntry context = task;
            while (context != null && visited.Add(context.Id))
            {
                if (snapshot.ActiveTasks.Exists(entry => entry.TaskId == context.Id))
                {
                    return true;
                }
                context = context.ParentId == 0 ? null : Find(context.ParentId);
            }
            return false;
        }

        internal bool AreTaskItemsComplete(
            uint taskId, QuestMemorySnapshot snapshot, uint[] selectedItems)
        {
            TaskCatalogEntry task = Find(taskId);
            if (task == null || task.RequiredTaskItems.Count == 0 ||
                !HasActiveTaskContext(taskId, snapshot)) return false;
            var selected = selectedItems == null || selectedItems.Length == 0
                ? null : new HashSet<uint>(selectedItems);
            if (selected != null)
                foreach (uint itemId in selected)
                    if (!task.RequiredTaskItems.ContainsKey(itemId)) return false;
            foreach (var requirement in task.RequiredTaskItems)
            {
                if (selected != null && !selected.Contains(requirement.Key)) continue;
                int count;
                if (!snapshot.TaskItemCounts.TryGetValue(requirement.Key, out count) ||
                    count < requirement.Value) return false;
            }
            return true;
        }

        internal bool CanConfirmAutomaticKillCompletion(
            uint taskId, string action, QuestMemorySnapshot snapshot)
        {
            TaskCatalogEntry task = Find(taskId);
            return task != null && snapshot != null &&
                task.IsAutomaticKillStage(action) &&
                !snapshot.ActiveTasks.Exists(entry => entry.TaskId == taskId) &&
                HasActiveTaskContext(taskId, snapshot);
        }

        internal bool IsChainRecordedComplete(uint taskId, QuestMemorySnapshot snapshot)
        {
            TaskCatalogEntry task = Find(taskId);
            if (task == null || snapshot == null ||
                !snapshot.FinishedTaskIds.Contains(task.TopId)) return false;
            foreach (var active in snapshot.ActiveTasks)
            {
                TaskCatalogEntry entry = Find(active.TaskId);
                if (entry != null && entry.TopId == task.TopId) return false;
            }
            return true;
        }

        internal bool HasActiveOrderedSuccessor(
            uint taskId, uint nextTaskId, QuestMemorySnapshot snapshot)
        {
            TaskCatalogEntry task = Find(taskId), next = Find(nextTaskId);
            if (task == null || next == null || snapshot == null ||
                task.ParentId == 0 || task.ParentId != next.ParentId ||
                task.Method != 1 || task.FinishType != 0 ||
                next.CatalogOrder <= task.CatalogOrder ||
                snapshot.ActiveTasks.Exists(entry => entry.TaskId == taskId) ||
                !snapshot.ActiveTasks.Exists(entry => entry.TaskId == nextTaskId)) return false;
            TaskCatalogEntry parent = Find(task.ParentId);
            return parent != null && parent.ChildrenInOrder;
        }

        private static uint ParseUInt(string value, int lineNumber)
        {
            uint parsed;
            if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidDataException("Número inválido na linha " + lineNumber + ".");
            return parsed;
        }

        private static int ParseInt(string value, int lineNumber)
        {
            int parsed;
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
                throw new InvalidDataException("Número inválido na linha " + lineNumber + ".");
            return parsed;
        }

        private static Dictionary<uint, int> ParseItemRequirements(
            string value, int lineNumber)
        {
            var result = new Dictionary<uint, int>();
            if (string.IsNullOrWhiteSpace(value) || value == "-") return result;
            foreach (string requirement in value.Split(';'))
            {
                string[] parts = requirement.Split(':');
                if (parts.Length != 2)
                    throw new InvalidDataException(
                        "Item de missão inválido na linha " + lineNumber + ".");
                uint itemId = ParseUInt(parts[0], lineNumber);
                int count = ParseInt(parts[1], lineNumber);
                if (itemId == 0 || count <= 0)
                    throw new InvalidDataException(
                        "Item de missão inválido na linha " + lineNumber + ".");
                int existing;
                result.TryGetValue(itemId, out existing);
                result[itemId] = Math.Max(existing, count);
            }
            return result;
        }
    }
}
