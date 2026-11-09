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
                    "Esta fase experimental aceita somente clientes x86.");
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
