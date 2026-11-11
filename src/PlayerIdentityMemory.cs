using System;
using System.IO;
using System.Text;

namespace PWQuestOverlay.Memory
{
    internal sealed class PlayerIdentityMemory
    {
        internal uint RoleId { get; set; }
        internal string Name { get; set; }
        internal string ClassCode { get; set; }
        internal string Key { get { return RoleId + "|" + Name + "|" + ClassCode; } }
    }

    internal static class PlayerIdentityReader
    {
        // IDs de profissão do cliente; a ordem difere da lista de classes da interface.
        private static readonly string[] Classes = { "WR", "MG", "ES", "WF", "WB", "MC", "EA", "EP", "SK", "MS" };
        internal static long ResolveConfiguredPlayer(ProcessMemoryReader memory, MemoryLayoutProfile profile)
        {
            long player = memory.ResolvePointerChain(profile.PlayerBaseRva, profile.PlayerChain);
            long vtable = memory.ModuleBase.ToInt64() + HexValue.Parse(profile.player.vtableRva);
            if (player <= 0 || memory.ReadUInt32(player) != vtable)
                throw new InvalidDataException("O cliente ainda não tem um personagem conectado.");
            return player;
        }
        internal static PlayerIdentityMemory Read(ProcessMemoryReader memory, long player, PlayerIdentityLayout layout)
        { return Read(memory.ReadUInt32, memory.ReadBytes, player, layout); }

        internal static PlayerIdentityMemory Read(Func<long, uint> readWord,
            Func<long, int, byte[]> readBytes, long player, PlayerIdentityLayout layout)
        {
            if (layout == null) return null;
            uint role = readWord(player + layout.RoleIdOffset);
            uint profession = readWord(player + layout.ClassOffset);
            uint pointer = readWord(player + layout.NamePointerOffset);
            if (role == 0 || profession >= Classes.Length || pointer < 0x10000 || pointer >= 0x7FFF0000)
                throw new InvalidDataException("Identidade do personagem ainda indisponível.");
            // AWString usa UTF-16 e guarda comprimento/capacidade imediatamente antes dos dados.
            int length = checked((int)readWord(pointer - 8));
            uint capacity = readWord(pointer - 4);
            if (length < 1 || length > layout.MaximumCharacters || capacity < length || capacity > 4096)
                throw new InvalidDataException("Comprimento do nome inválido.");
            byte[] bytes = readBytes(pointer, (length + 1) * 2);
            if (bytes.Length != (length + 1) * 2 || bytes[length * 2] != 0 || bytes[length * 2 + 1] != 0)
                throw new InvalidDataException("Nome sem terminador UTF-16.");
            string name = new UnicodeEncoding(false, false, true).GetString(bytes, 0, length * 2);
            if (name.Trim() != name || string.IsNullOrWhiteSpace(name) || name.IndexOf('|') >= 0)
                throw new InvalidDataException("Nome de personagem inválido.");
            foreach (char character in name)
                if (char.IsControl(character) || character == '\uFFFD') throw new InvalidDataException("Nome de personagem inválido.");
            if (role != readWord(player + layout.RoleIdOffset) || profession != readWord(player + layout.ClassOffset) ||
                pointer != readWord(player + layout.NamePointerOffset) || length != readWord(pointer - 8))
                throw new InvalidDataException("Personagem mudou durante a leitura.");
            return new PlayerIdentityMemory { RoleId = role, Name = name, ClassCode = Classes[profession] };
        }
    }
}
