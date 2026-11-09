using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace PWQuestOverlay
{
    internal sealed class ClientIdentity
    {
        internal string Name { get; private set; }
        internal string ClassCode { get; private set; }
        internal bool Manual { get; private set; }
        internal string ProfileKey { get { return Name.ToUpperInvariant() + "|" + ClassCode; } }

        internal ClientIdentity(string name, string code, bool manual)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 64 || name.Contains("|") || name.Any(char.IsControl) ||
                !ClientIdentities.IsValidClass(code))
                throw new ArgumentException("Informe o nome do personagem (até 64 caracteres, sem |) e uma classe válida.");
            Name = name; ClassCode = code.Trim().ToUpperInvariant(); Manual = manual;
        }
    }

    internal sealed class ClientWindowInfo
    {
        internal IntPtr Handle { get; set; }
        internal int ProcessId { get; set; }
        internal string SessionKey { get; set; }
        internal string Title { get; set; }
    }

    internal sealed class ClientBindings
    {
        private readonly Dictionary<string, ClientIdentity> _bindings = new Dictionary<string, ClientIdentity>();
        private readonly Dictionary<string, string> _memorySessions = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _profiles = new Dictionary<string, string>();
        internal void Clear() { _bindings.Clear(); _memorySessions.Clear(); _profiles.Clear(); }
        internal ClientIdentity Find(string key) { ClientIdentity value; return key != null && _bindings.TryGetValue(key, out value) ? value : null; }
        internal void Set(string key, ClientIdentity identity)
        { if (string.IsNullOrEmpty(key)) throw new ArgumentException("Cliente indisponível."); _memorySessions.Remove(key); if (identity == null) _bindings.Remove(key); else _bindings[key] = identity; }
        internal void Prune(IEnumerable<string> liveKeys)
        {
            var live = new HashSet<string>(liveKeys);
            foreach (string key in _bindings.Keys.Where(k => !live.Contains(k)).ToList()) _bindings.Remove(key);
            foreach (string key in _memorySessions.Keys.Where(k => !live.Contains(k)).ToList()) _memorySessions.Remove(key);
            foreach (string key in _profiles.Keys.Where(k => !live.Contains(k)).ToList()) _profiles.Remove(key);
        }
        internal bool ObserveProfile(string key, string profileKey)
        {
            string previous;
            bool changed = _profiles.TryGetValue(key, out previous) && previous != profileKey;
            _profiles[key] = profileKey;
            if (changed) _memorySessions.Remove(key);
            return changed;
        }
        internal void ObserveMemorySession(string key, string token)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(token)) return;
            string previous;
            if (_memorySessions.TryGetValue(key, out previous) && previous != token) _bindings.Remove(key);
            _memorySessions[key] = token;
        }
    }

    internal static class ClientIdentities
    {
        private static readonly Regex Primary = new Regex(@"^\s*(?<name>.+?)\s*[\[\(\{]\s*(?<class>WB|WF|EA|EP|WR|MG|MC|ES|SK|MS)\s*[\]\)\}]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Token = new Regex(@"(?:^|[\s\[\](){}_\-])(?<class>WB|WF|EA|EP|WR|MG|MC|ES|SK|MS)(?=$|[\s\[\](){}_\-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        internal static bool IsValidClass(string code)
        { return new[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS" }.Contains((code ?? "").Trim().ToUpperInvariant()); }
        internal static void ParseTitle(string title, out string name, out string code)
        {
            name = (title ?? "").Trim(); code = null;
            Match m = Primary.Match(name);
            if (m.Success) { name = m.Groups["name"].Value.Trim(); code = m.Groups["class"].Value.ToUpperInvariant(); return; }
            m = Token.Match(name); if (!m.Success) return;
            code = m.Groups["class"].Value.ToUpperInvariant();
            name = (name.Substring(0, m.Index) + " " + name.Substring(m.Index + m.Length)).Trim(' ', '-', '_', '[', ']', '(', ')', '{', '}');
        }
        internal static ClientIdentity FromTitle(string title, bool uniqueTitle, IDictionary<string, string> associations)
        {
            if (!uniqueTitle) return null;
            string name, code; ParseTitle(title, out name, out code);
            string manual;
            bool associated = associations != null && associations.TryGetValue((title ?? "").Trim().ToUpperInvariant(), out manual) && IsValidClass(manual);
            if (associated) code = associations[(title ?? "").Trim().ToUpperInvariant()];
            if (!IsValidClass(code) || string.IsNullOrWhiteSpace(name)) return null;
            string generic = Regex.Replace(name, @"[\s._\-]", "").ToUpperInvariant();
            if (generic == "ELEMENTCLIENT" || generic.StartsWith("PERFECTWORLD") || generic.StartsWith("THECLASSICPW")) return null;
            try { return new ClientIdentity(name, code, associated); } catch (ArgumentException) { return null; }
        }

        internal static ClientIdentity Resolve(ClientWindowInfo window, IList<ClientWindowInfo> windows, ClientBindings bindings, IDictionary<string, string> aliases)
        {
            if (window == null) return null;
            var identities = new Dictionary<string, ClientIdentity>();
            var currentWindows = windows.Select(w => w.SessionKey == window.SessionKey ? window : w).ToList();
            foreach (ClientWindowInfo w in currentWindows)
                identities[w.SessionKey] = bindings.Find(w.SessionKey) ?? FromTitle(w.Title,
                    currentWindows.Count(other => string.Equals(other.Title, w.Title, StringComparison.OrdinalIgnoreCase)) == 1, aliases);
            ClientIdentity result;
            if (!identities.TryGetValue(window.SessionKey, out result) || result == null) return null;
            return identities.Values.Count(i => i != null && i.ProfileKey == result.ProfileKey) == 1 ? result : null;
        }
    }
}
