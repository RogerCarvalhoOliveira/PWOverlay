using System;
using System.Collections.Generic;

namespace PWQuestOverlay.Memory
{
    internal enum QuestMemoryEventKind
    {
        Activated,
        StateChanged,
        DisappearedAfterObservation,
        AddedToHistory
    }

    internal sealed class QuestMemoryEvent
    {
        internal QuestMemoryEventKind Kind { get; set; }
        internal uint TaskId { get; set; }
        internal byte PreviousState { get; set; }
        internal byte CurrentState { get; set; }
    }

    internal sealed class QuestStateTracker
    {
        private Dictionary<uint, byte> _active;
        private HashSet<uint> _history;

        internal void SetBaseline(QuestMemorySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            _active = ActiveStates(snapshot);
            _history = new HashSet<uint>(snapshot.FinishedTaskIds);
        }

        internal List<QuestMemoryEvent> Update(QuestMemorySnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException("snapshot");
            if (_active == null || _history == null)
            {
                SetBaseline(snapshot);
                return new List<QuestMemoryEvent>();
            }

            var events = new List<QuestMemoryEvent>();
            Dictionary<uint, byte> current = ActiveStates(snapshot);
            foreach (KeyValuePair<uint, byte> pair in current)
            {
                byte previous;
                if (!_active.TryGetValue(pair.Key, out previous))
                {
                    events.Add(new QuestMemoryEvent
                    {
                        Kind = QuestMemoryEventKind.Activated,
                        TaskId = pair.Key,
                        CurrentState = pair.Value
                    });
                }
                else if (previous != pair.Value)
                {
                    events.Add(new QuestMemoryEvent
                    {
                        Kind = QuestMemoryEventKind.StateChanged,
                        TaskId = pair.Key,
                        PreviousState = previous,
                        CurrentState = pair.Value
                    });
                }
            }
            foreach (KeyValuePair<uint, byte> pair in _active)
            {
                if (!current.ContainsKey(pair.Key))
                    events.Add(new QuestMemoryEvent
                    {
                        Kind = QuestMemoryEventKind.DisappearedAfterObservation,
                        TaskId = pair.Key,
                        PreviousState = pair.Value
                    });
            }
            foreach (uint taskId in snapshot.FinishedTaskIds)
            {
                if (!_history.Contains(taskId))
                    events.Add(new QuestMemoryEvent
                    {
                        Kind = QuestMemoryEventKind.AddedToHistory,
                        TaskId = taskId
                    });
            }

            _active = current;
            _history = new HashSet<uint>(snapshot.FinishedTaskIds);
            return events;
        }

        private static Dictionary<uint, byte> ActiveStates(
            QuestMemorySnapshot snapshot)
        {
            var result = new Dictionary<uint, byte>();
            foreach (ActiveQuestMemoryEntry entry in snapshot.ActiveTasks)
                result[entry.TaskId] = entry.StateCode;
            return result;
        }
    }
}
