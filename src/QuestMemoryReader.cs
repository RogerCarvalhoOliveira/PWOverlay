using System;
using System.Collections.Generic;

namespace PWQuestOverlay.Memory
{
    internal enum QuestAvailability { Unknown, Expired, Blocked, Available }

    internal sealed class QuestMemorySnapshot
    {
        internal QuestMemorySnapshot()
        {
            ActiveTasks = new List<ActiveQuestMemoryEntry>();
            FinishedTaskIds = new List<uint>();
            TaskItemCounts = new Dictionary<uint, int>();
            Availability = new Dictionary<uint, QuestAvailability>();
        }

        internal long TaskInterfaceAddress { get; set; }
        internal int PlayerLevel { get; set; }
        internal int? PlayerReputation { get; set; }
        internal bool UsesClassic126Catalog { get; set; }
        internal Dictionary<uint, QuestAvailability> Availability { get; private set; }
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
        internal int FirstMonsterKillCount { get; set; }
        internal long Address { get; set; }
        internal byte[] RawData { get; set; }
    }

    internal sealed class QuestMemoryReader
    {
        private readonly ProcessMemoryReader _memory;
        private readonly QuestMemoryLayout _layout;
        private readonly TaskInventoryMemoryLayout _inventory;
        private List<uint> _taskVTables;
        private long _cachedPlayer;
        private long _cachedSlot;
        private long _cachedInterface;
        private uint _cachedVTable;

        internal QuestMemoryReader(
            ProcessMemoryReader memory,
            QuestMemoryLayout layout,
            TaskInventoryMemoryLayout inventory)
        {
            if (memory == null) throw new ArgumentNullException("memory");
            if (layout == null) throw new ArgumentNullException("layout");
            _memory = memory;
            _layout = layout;
            _inventory = inventory;
        }

        internal QuestMemorySnapshot Read(long player, bool readItems = true)
        {
            long taskInterface = FindTaskInterface(player);
            return ReadFromInterface(player, taskInterface, readItems);
        }

        internal QuestMemorySnapshot ReadFromInterface(long player, long taskInterface, bool readItems)
        {
            QuestMemorySnapshot snapshot = ReadActiveFromInterface(taskInterface);
            long finishedList = ReadRequiredPointer(taskInterface + _layout.FinishedListPointerOffset, "histórico de missões");
            int finishedCount = _memory.ReadUInt16(finishedList + _layout.FinishedCountOffset);
            if (finishedCount > _layout.FinishedMaximum)
                throw new InvalidOperationException("Quantidade de missões concluídas inválida: " + finishedCount);
            if (finishedCount > 0)
            {
                byte[] entries = _memory.ReadBytes(finishedList + _layout.FinishedEntriesOffset,
                    checked(finishedCount * _layout.FinishedEntryStride));
                for (int index = 0; index < finishedCount; index++)
                {
                    int entry = index * _layout.FinishedEntryStride;
                    if (_layout.FinishedIdBytes == 2 && (entries[entry + _layout.finishedFailureFlagOffset] & 1) != 0)
                        continue;
                    uint taskId = _layout.FinishedIdBytes == 2
                        ? BitConverter.ToUInt16(entries, entry)
                        : BitConverter.ToUInt32(entries, entry);
                    if (taskId != 0) snapshot.FinishedTaskIds.Add(taskId);
                }
            }
            if (readItems) ReadTaskInventory(player, snapshot);
            return snapshot;
        }

        internal QuestMemorySnapshot ReadActive(long player)
        {
            return ReadActiveFromInterface(FindTaskInterface(player));
        }

        private QuestMemorySnapshot ReadActiveFromInterface(long taskInterface)
        {
            long activeList = ReadRequiredPointer(
                taskInterface + _layout.ActiveListPointerOffset,
                "lista de missões ativas");

            int activeCount = _memory.ReadByte(
                activeList + _layout.ActiveCountOffset);
            if (activeCount > _layout.ActiveMaximum)
                throw new InvalidOperationException(
                    "Quantidade de missões ativas inválida: " + activeCount);

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
                    FirstMonsterKillCount = BitConverter.ToUInt16(
                        raw, _layout.ActiveFirstMonsterCountOffset),
                    Address = entryAddress,
                    RawData = raw
                });
            }
            return snapshot;
        }

        private void ReadTaskInventory(long player, QuestMemorySnapshot snapshot)
        {
            if (_inventory == null) throw new InvalidOperationException("Layout do inventário de missão indisponível.");
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
            uint value;
            if (!string.IsNullOrEmpty(_layout.playerInterfacePointerOffset))
            {
                long pointer = ReadRequiredPointer(player + HexValue.Parse(_layout.playerInterfacePointerOffset), "interface de missões");
                if (_memory.ReadUInt32(pointer) != _memory.ModuleBase.ToInt64() + HexValue.Parse(_layout.interfaceVtableRva) ||
                    _memory.ReadUInt32(pointer + 4) != player)
                    throw new InvalidOperationException("A interface de missões não corresponde ao personagem atual.");
                return pointer;
            }
            if (_cachedPlayer == player && _cachedSlot != 0 &&
                _memory.TryReadUInt32(_cachedSlot, out value))
            {
                if (_cachedSlot == _cachedInterface && value == _cachedVTable)
                    return _cachedInterface;
                uint vtable;
                if (value == _cachedInterface && _memory.TryReadUInt32(value, out vtable) &&
                    vtable == _cachedVTable) return _cachedInterface;
            }

            if (_taskVTables == null)
                _taskVTables = RttiResolver.FindVTables(_memory, _layout.className);
            if (_taskVTables.Count == 0)
                throw new InvalidOperationException(
                    "RTTI não localizado: " + _layout.className);

            byte[] playerBytes = _memory.ReadBytes(
                player, _layout.PlayerSearchSize);
            foreach (uint vtable in _taskVTables)
            {
                for (int offset = 0; offset <= playerBytes.Length - 4; offset += 4)
                {
                    value = BitConverter.ToUInt32(playerBytes, offset);
                    if (value == vtable) return CacheTaskInterface(player, offset, player + offset, vtable);
                    if (value < 0x10000 || value >= 0x7FFF0000) continue;
                    uint targetVTable;
                    if (_memory.TryReadUInt32(value, out targetVTable) &&
                        targetVTable == vtable) return CacheTaskInterface(player, offset, value, vtable);
                }
            }
            throw new InvalidOperationException(
                _layout.className + " não está associado ao jogador atual.");
        }

        private long CacheTaskInterface(long player, int offset, long address, uint vtable)
        {
            _cachedPlayer = player;
            _cachedSlot = player + offset;
            _cachedInterface = address;
            _cachedVTable = vtable;
            return address;
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
