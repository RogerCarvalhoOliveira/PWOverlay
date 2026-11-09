using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

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
    }

    internal sealed class TaskCatalog
    {
        private readonly Dictionary<uint, TaskCatalogEntry> _entries;

        private TaskCatalog(Dictionary<uint, TaskCatalogEntry> entries)
        {
            _entries = entries;
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
                    if (entry.Id != 0 && !entries.ContainsKey(entry.Id))
                        entries.Add(entry.Id, entry);
                }
            }
            if (entries.Count == 0)
                throw new InvalidDataException("Catálogo de missões vazio.");
            return new TaskCatalog(entries);
        }

        internal TaskCatalogEntry Find(uint id)
        {
            TaskCatalogEntry entry;
            return _entries.TryGetValue(id, out entry) ? entry : null;
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
    }
}
