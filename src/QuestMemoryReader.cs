using System;
using System.Collections.Generic;

namespace PWQuestOverlay.Memory
{
    internal sealed class QuestMemorySnapshot
    {
        internal QuestMemorySnapshot()
        {
            ActiveTasks = new List<ActiveQuestMemoryEntry>();
            FinishedTaskIds = new List<uint>();
        }

        internal long TaskInterfaceAddress { get; set; }
        internal List<ActiveQuestMemoryEntry> ActiveTasks { get; private set; }
        internal List<uint> FinishedTaskIds { get; private set; }
    }

    internal sealed class ActiveQuestMemoryEntry
    {
        internal uint TaskId { get; set; }
        internal int ParentIndex { get; set; }
        internal byte Flags { get; set; }
        internal byte StateCode { get { return (byte)(Flags & 0x0F); } }
        internal bool SuccessFlag { get { return (StateCode & 0x01) != 0; } }
        internal bool FinishedFlag { get { return (StateCode & 0x02) != 0; } }
        internal long Address { get; set; }
        internal byte[] RawData { get; set; }
    }

    internal sealed class QuestMemoryReader
    {
        private readonly ProcessMemoryReader _memory;
        private readonly QuestMemoryLayout _layout;

        internal QuestMemoryReader(
            ProcessMemoryReader memory,
            QuestMemoryLayout layout)
        {
            if (memory == null) throw new ArgumentNullException("memory");
            if (layout == null) throw new ArgumentNullException("layout");
            _memory = memory;
            _layout = layout;
        }

        internal QuestMemorySnapshot Read(long player)
        {
            long taskInterface = FindTaskInterface(player);
            long activeList = ReadRequiredPointer(
                taskInterface + _layout.ActiveListPointerOffset,
                "lista de missões ativas");
            long finishedList = ReadRequiredPointer(
                taskInterface + _layout.FinishedListPointerOffset,
                "histórico de missões");

            int activeCount = _memory.ReadByte(
                activeList + _layout.ActiveCountOffset);
            if (activeCount > _layout.ActiveMaximum)
                throw new InvalidOperationException(
                    "Quantidade de missões ativas inválida: " + activeCount);

            int finishedCount = _memory.ReadUInt16(
                finishedList + _layout.FinishedCountOffset);
            if (finishedCount > _layout.FinishedMaximum)
                throw new InvalidOperationException(
                    "Quantidade de missões concluídas inválida: " + finishedCount);

            var snapshot = new QuestMemorySnapshot
            {
                TaskInterfaceAddress = taskInterface
            };
            for (int index = 0; index < activeCount; index++)
            {
                long entryAddress = activeList + _layout.ActiveEntriesOffset +
                    index * _layout.ActiveEntryStride;
                byte[] raw = _memory.ReadBytes(
                    entryAddress, _layout.ActiveEntryStride);
                uint taskId = BitConverter.ToUInt16(
                    raw, _layout.ActiveIdOffset);
                if (taskId == 0)
                    throw new InvalidOperationException(
                        "Entrada ativa sem ID no índice " + index + ".");
                snapshot.ActiveTasks.Add(new ActiveQuestMemoryEntry
                {
                    TaskId = taskId,
                    ParentIndex = unchecked((sbyte)raw[_layout.ActiveParentIndexOffset]),
                    Flags = raw[_layout.ActiveFlagsOffset],
                    Address = entryAddress,
                    RawData = raw
                });
            }
            for (int index = 0; index < finishedCount; index++)
            {
                long idAddress = finishedList + _layout.FinishedEntriesOffset +
                    index * _layout.FinishedEntryStride;
                uint taskId = _memory.ReadUInt32(idAddress);
                if (taskId != 0) snapshot.FinishedTaskIds.Add(taskId);
            }
            return snapshot;
        }

        private long FindTaskInterface(long player)
        {
            List<uint> vtables = RttiResolver.FindVTables(
                _memory, _layout.className);
            if (vtables.Count == 0)
                throw new InvalidOperationException(
                    "RTTI não localizado: " + _layout.className);

            byte[] playerBytes = _memory.ReadBytes(
                player, _layout.PlayerSearchSize);
            foreach (uint vtable in vtables)
            {
                for (int offset = 0; offset <= playerBytes.Length - 4; offset += 4)
                {
                    uint value = BitConverter.ToUInt32(playerBytes, offset);
                    if (value == vtable) return player + offset;
                    if (value < 0x10000 || value >= 0x7FFF0000) continue;
                    uint targetVTable;
                    if (_memory.TryReadUInt32(value, out targetVTable) &&
                        targetVTable == vtable) return value;
                }
            }
            throw new InvalidOperationException(
                _layout.className + " não está associado ao jogador atual.");
        }

        private long ReadRequiredPointer(long address, string description)
        {
            uint value = _memory.ReadUInt32(address);
            if (value < 0x10000 || value >= 0x7FFF0000)
                throw new InvalidOperationException(
                    "Ponteiro inválido para " + description + ".");
            return value;
        }
    }
}
