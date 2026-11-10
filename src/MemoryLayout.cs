using System;
using System.Collections.Generic;
using System.IO;
using System.Web.Script.Serialization;

namespace PWQuestOverlay.Memory
{
    internal sealed class MemoryLayoutFile
    {
        public int schemaVersion { get; set; }
        public List<MemoryLayoutProfile> profiles { get; set; }

        internal static MemoryLayoutFile Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Arquivo de offsets não encontrado.", path);
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            MemoryLayoutFile file = serializer.Deserialize<MemoryLayoutFile>(
                File.ReadAllText(path));
            if (file == null || file.profiles == null || file.profiles.Count == 0)
                throw new InvalidDataException("Nenhum perfil de memória foi configurado.");
            return file;
        }

        internal MemoryLayoutProfile Find(string id)
        {
            return profiles.Find(profile => string.Equals(
                profile.id, id, StringComparison.OrdinalIgnoreCase));
        }
    }

    internal sealed class MemoryLayoutProfile
    {
        public string id { get; set; }
        public string label { get; set; }
        public string module { get; set; }
        public int pointerSize { get; set; }
        public PointerRoot player { get; set; }
        public Dictionary<string, string> attributes { get; set; }
        public QuestMemoryLayout quests { get; set; }
        public TaskInventoryMemoryLayout inventory { get; set; }
        public PlayerIdentityLayout identity { get; set; }

        internal uint PlayerBaseRva
        {
            get { return HexValue.Parse(player.baseRva); }
        }

        internal uint[] PlayerChain
        {
            get
            {
                if (player == null || player.chain == null) return new uint[0];
                return player.chain.ConvertAll(HexValue.Parse).ToArray();
            }
        }

        internal uint AttributeOffset(string name)
        {
            string value;
            if (attributes == null || !attributes.TryGetValue(name, out value))
                throw new InvalidDataException("Atributo sem offset: " + name);
            return HexValue.Parse(value);
        }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(module))
                throw new InvalidDataException("Perfil de memória sem id ou módulo.");
            if (pointerSize != 4)
                throw new InvalidDataException(
                    "A leitura de memória aceita somente clientes x86.");
            if (player == null || string.IsNullOrWhiteSpace(player.baseRva))
                throw new InvalidDataException("Perfil sem raiz do personagem.");
            AttributeOffset("level");
            AttributeOffset("hp");
            AttributeOffset("hpMax");
            AttributeOffset("mp");
            AttributeOffset("mpMax");
            if (quests == null) throw new InvalidDataException(
                "Perfil sem layout de missões.");
            quests.Validate();
            if (inventory == null) throw new InvalidDataException(
                "Perfil sem layout do inventário de missão.");
            inventory.Validate();
            if (identity != null) identity.Validate();
        }
    }

    internal sealed class PlayerIdentityLayout
    {
        public string roleIdOffset { get; set; }
        public string namePointerOffset { get; set; }
        public string classOffset { get; set; }
        public int maximumCharacters { get; set; }
        internal uint RoleIdOffset { get { return HexValue.Parse(roleIdOffset); } }
        internal uint NamePointerOffset { get { return HexValue.Parse(namePointerOffset); } }
        internal uint ClassOffset { get { return HexValue.Parse(classOffset); } }
        internal int MaximumCharacters { get { return maximumCharacters; } }
        internal void Validate()
        {
            if (RoleIdOffset > 0x10000 || NamePointerOffset > 0x10000 || ClassOffset > 0x10000 ||
                maximumCharacters < 1 || maximumCharacters > 64)
                throw new InvalidDataException("Layout da identidade inválido.");
        }
    }

    internal sealed class TaskInventoryMemoryLayout
    {
        public string playerPointerOffset { get; set; }
        public string taskInventoryOffset { get; set; }
        public string slotsPointerOffset { get; set; }
        public string slotCountOffset { get; set; }
        public int slotMaximum { get; set; }
        public string itemIdOffset { get; set; }
        public string itemCountOffset { get; set; }

        internal int PlayerPointerOffset { get { return ToInt(playerPointerOffset); } }
        internal int TaskInventoryOffset { get { return ToInt(taskInventoryOffset); } }
        internal int SlotsPointerOffset { get { return ToInt(slotsPointerOffset); } }
        internal int SlotCountOffset { get { return ToInt(slotCountOffset); } }
        internal int SlotMaximum { get { return slotMaximum; } }
        internal int ItemIdOffset { get { return ToInt(itemIdOffset); } }
        internal int ItemCountOffset { get { return ToInt(itemCountOffset); } }

        internal void Validate()
        {
            if (PlayerPointerOffset < 0 || TaskInventoryOffset < 0 ||
                SlotsPointerOffset < 0 || SlotCountOffset < 0 ||
                SlotMaximum <= 0 || SlotMaximum > 256 ||
                ItemIdOffset < 0 || ItemCountOffset < 0)
                throw new InvalidDataException("Layout do inventário de missão inválido.");
        }

        private static int ToInt(string value)
        {
            return checked((int)HexValue.Parse(value));
        }
    }

    internal sealed class QuestMemoryLayout
    {
        public string className { get; set; }
        public string playerSearchSize { get; set; }
        public string activeListPointerOffset { get; set; }
        public string activeCountOffset { get; set; }
        public string activeEntriesOffset { get; set; }
        public string activeEntryStride { get; set; }
        public string activeIdOffset { get; set; }
        public string activeParentIndexOffset { get; set; }
        public string activeFlagsOffset { get; set; }
        public string activeFirstMonsterCountOffset { get; set; }
        public int activeMaximum { get; set; }
        public string finishedListPointerOffset { get; set; }
        public string finishedCountOffset { get; set; }
        public string finishedEntriesOffset { get; set; }
        public string finishedEntryStride { get; set; }
        public int finishedMaximum { get; set; }

        internal int PlayerSearchSize { get { return ToInt(playerSearchSize); } }
        internal int ActiveListPointerOffset { get { return ToInt(activeListPointerOffset); } }
        internal int ActiveCountOffset { get { return ToInt(activeCountOffset); } }
        internal int ActiveEntriesOffset { get { return ToInt(activeEntriesOffset); } }
        internal int ActiveEntryStride { get { return ToInt(activeEntryStride); } }
        internal int ActiveIdOffset { get { return ToInt(activeIdOffset); } }
        internal int ActiveParentIndexOffset { get { return ToInt(activeParentIndexOffset); } }
        internal int ActiveFlagsOffset { get { return ToInt(activeFlagsOffset); } }
        internal int ActiveFirstMonsterCountOffset
        {
            get { return string.IsNullOrWhiteSpace(activeFirstMonsterCountOffset)
                ? 0x15 : ToInt(activeFirstMonsterCountOffset); }
        }
        internal int ActiveMaximum { get { return activeMaximum; } }
        internal int FinishedListPointerOffset { get { return ToInt(finishedListPointerOffset); } }
        internal int FinishedCountOffset { get { return ToInt(finishedCountOffset); } }
        internal int FinishedEntriesOffset { get { return ToInt(finishedEntriesOffset); } }
        internal int FinishedEntryStride { get { return ToInt(finishedEntryStride); } }
        internal int FinishedMaximum { get { return finishedMaximum; } }

        internal void Validate()
        {
            if (string.IsNullOrWhiteSpace(className))
                throw new InvalidDataException("Classe RTTI de missões vazia.");
            if (PlayerSearchSize <= 0 || ActiveEntryStride < 4 ||
                ActiveIdOffset < 0 || ActiveIdOffset + 2 > ActiveEntryStride ||
                ActiveParentIndexOffset < 0 || ActiveParentIndexOffset >= ActiveEntryStride ||
                ActiveFlagsOffset < 0 || ActiveFlagsOffset >= ActiveEntryStride ||
                ActiveFirstMonsterCountOffset < 0 ||
                ActiveFirstMonsterCountOffset + 2 > ActiveEntryStride ||
                ActiveMaximum <= 0 || FinishedEntryStride < 4 ||
                FinishedMaximum <= 0)
                throw new InvalidDataException("Layout de missões inválido.");
        }

        private static int ToInt(string value)
        {
            return checked((int)HexValue.Parse(value));
        }
    }

    internal sealed class PointerRoot
    {
        public string baseRva { get; set; }
        public List<string> chain { get; set; }
        public string className { get; set; }
    }

    internal static class HexValue
    {
        internal static uint Parse(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new FormatException("Offset vazio.");
            string normalized = value.Trim();
            if (normalized.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                normalized = normalized.Substring(2);
            uint parsed;
            if (!uint.TryParse(normalized,
                System.Globalization.NumberStyles.HexNumber,
                System.Globalization.CultureInfo.InvariantCulture,
                out parsed))
                throw new FormatException("Offset inválido: " + value);
            return parsed;
        }
    }
}
