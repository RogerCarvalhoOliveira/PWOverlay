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
            TaskItemCounts = new Dictionary<uint, int>();
        }

        internal long TaskInterfaceAddress { get; set; }
        internal List<ActiveQuestMemoryEntry> ActiveTasks { get; private set; }
        internal List<uint> FinishedTaskIds { get; private set; }
        internal Dictionary<uint, int> TaskItemCounts { get; private set; }
    }

    internal sealed class ActiveQuestMemoryEntry
    {
        internal uint TaskId { get; set; }
        internal int ParentIndex { get; set; }
        internal byte Flags { get; set; }
        internal byte StateCode { get { return (byte)(Flags & 0x0F); } }
        internal long Address { get; set; }
        internal byte[] RawData { get; set; }
    }

    internal sealed class QuestMemoryReader
    {
        private readonly ProcessMemoryReader _memory;
        private readonly QuestMemoryLayout _layout;
        private readonly TaskInventoryMemoryLayout _inventory;

        internal QuestMemoryReader(
            ProcessMemoryReader memory,
            QuestMemoryLayout layout,
            TaskInventoryMemoryLayout inventory)
        {
            if (memory == null) throw new ArgumentNullException("memory");
            if (layout == null) throw new ArgumentNullException("layout");
            if (inventory == null) throw new ArgumentNullException("inventory");
            _memory = memory;
            _layout = layout;
            _inventory = inventory;
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
            ReadTaskInventory(player, snapshot);
            return snapshot;
        }

        private void ReadTaskInventory(long player, QuestMemorySnapshot snapshot)
        {
            long inventories = ReadRequiredPointer(
                player + _inventory.PlayerPointerOffset,
                "inventários do personagem");
            long taskInventory = inventories + _inventory.TaskInventoryOffset;
            int slotCount = _memory.ReadInt32(
                taskInventory + _inventory.SlotCountOffset);
            if (slotCount < 0 || slotCount > _inventory.SlotMaximum)
                throw new InvalidOperationException(
                    "Quantidade de espaços do inventário de missão inválida: " + slotCount);
            if (slotCount == 0) return;
            long slots = ReadRequiredPointer(
                taskInventory + _inventory.SlotsPointerOffset,
                "espaços do inventário de missão");
            for (int index = 0; index < slotCount; index++)
            {
                uint itemPointer = _memory.ReadUInt32(slots + index * 4L);
                if (itemPointer == 0) continue;
                if (itemPointer < 0x10000 || itemPointer >= 0x7FFF0000)
                    throw new InvalidOperationException(
                        "Ponteiro inválido no inventário de missão.");
                uint itemId = _memory.ReadUInt32(itemPointer + _inventory.ItemIdOffset);
                int itemCount = _memory.ReadInt32(itemPointer + _inventory.ItemCountOffset);
                if (itemId == 0 || itemCount <= 0) continue;
                int existing;
                snapshot.TaskItemCounts.TryGetValue(itemId, out existing);
                snapshot.TaskItemCounts[itemId] = checked(existing + itemCount);
            }
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
