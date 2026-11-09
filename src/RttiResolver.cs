using System;
using System.Collections.Generic;
using System.Text;

namespace PWQuestOverlay.Memory
{
    internal static class RttiResolver
    {
        internal static List<uint> FindVTables(
            ProcessMemoryReader memory,
            string className)
        {
            if (memory == null) throw new ArgumentNullException("memory");
            if (string.IsNullOrWhiteSpace(className))
                throw new ArgumentException("Classe RTTI vazia.", "className");

            byte[] module = memory.ReadBytes(
                memory.ModuleBase.ToInt64(), memory.ModuleSize);
            byte[] typeName = Encoding.ASCII.GetBytes(".?AV" + className + "@@\0");
            List<int> names = FindAll(module, typeName);
            var vtables = new List<uint>();
            foreach (int nameOffset in names)
            {
                if (nameOffset < 8) continue;
                uint typeDescriptor = checked((uint)(
                    memory.ModuleBase.ToInt64() + nameOffset - 8));
                foreach (int typeReference in FindAll(
                    module, BitConverter.GetBytes(typeDescriptor)))
                {
                    if (typeReference < 12) continue;
                    uint locator = checked((uint)(
                        memory.ModuleBase.ToInt64() + typeReference - 12));
                    foreach (int locatorReference in FindAll(
                        module, BitConverter.GetBytes(locator)))
                    {
                        uint vtable = checked((uint)(
                            memory.ModuleBase.ToInt64() + locatorReference + 4));
                        if (!vtables.Contains(vtable)) vtables.Add(vtable);
                    }
                }
            }
            return vtables;
        }

        private static List<int> FindAll(byte[] haystack, byte[] needle)
        {
            var matches = new List<int>();
            if (haystack == null || needle == null || needle.Length == 0 ||
                needle.Length > haystack.Length) return matches;
            for (int index = 0; index <= haystack.Length - needle.Length; index++)
            {
                int part = 0;
                while (part < needle.Length &&
                    haystack[index + part] == needle[part]) part++;
                if (part == needle.Length) matches.Add(index);
            }
            return matches;
        }
    }
}
