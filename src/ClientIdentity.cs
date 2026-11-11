using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace PWQuestOverlay
{
    internal sealed class ClientIdentity
    {
        internal string Name { get; private set; }
        internal string ClassCode { get; private set; }
        internal bool Manual { get; private set; }
        internal bool FromMemory { get; private set; }
        internal ClientServer Server { get; private set; }
        internal string ProfileKey { get { return ClientServer.Scope(Server, Name.ToUpperInvariant() + "|" + ClassCode); } }

        internal ClientIdentity(string name, string code, bool manual, bool fromMemory = false)
        {
            name = (name ?? "").Trim();
            if (name.Length == 0 || name.Length > 64 || name.Contains("|") || name.Any(char.IsControl) ||
                !ClientIdentities.IsValidClass(code))
                throw new ArgumentException("Informe o nome do personagem (até 64 caracteres, sem |) e uma classe válida.");
            Name = name; ClassCode = code.Trim().ToUpperInvariant(); Manual = manual; FromMemory = fromMemory;
        }

        internal ClientIdentity OnServer(ClientServer server)
        {
            if (ReferenceEquals(Server, server)) return this;
            var identity = (ClientIdentity)MemberwiseClone();
            identity.Server = server;
            return identity;
        }
    }

    internal sealed class ClientServer
    {
        internal string Key { get; private set; }
        internal string Name { get; private set; }
        internal bool IdentityMemoryOnly { get; private set; }

        private ClientServer(string key, string name, bool identityMemoryOnly = false)
        { Key = key; Name = name; IdentityMemoryOnly = identityMemoryOnly; }

        internal static ClientServer ForInstallation(string executablePath, string title)
        {
            string directory = string.IsNullOrEmpty(executablePath) ? null : Path.GetDirectoryName(executablePath);
            string name = directory == null ? "" : Path.GetFileName(directory);
            string edition = string.IsNullOrEmpty(name) ? title : name;
            if (string.Equals(edition, "The Classic PW 1.4.4", StringComparison.OrdinalIgnoreCase))
                return new ClientServer("CLASSIC:1.4.4", "The Classic 1.4.4");
            if (string.Equals(edition, "The Classic PW 1.2.6", StringComparison.OrdinalIgnoreCase))
                return new ClientServer("CLASSIC:1.2.6", "The Classic 1.2.6", true);
            if (directory == null) return null;
            using (var hash = SHA256.Create())
            {
                string key = Convert.ToBase64String(hash.ComputeHash(Encoding.UTF8.GetBytes(directory.ToUpperInvariant())));
                return new ClientServer("INSTALL:" + key, string.IsNullOrWhiteSpace(name) ? directory : name);
            }
        }

        internal static ClientServer ForSelection(ClientServer installation, string label)
        {
            label = (label ?? "").Trim();
            if (label.Length == 0 || (installation != null && string.Equals(installation.Name, label, StringComparison.OrdinalIgnoreCase)))
                return installation;
            if (label.Length > 80 || label.Contains("|") || label.Any(char.IsControl))
                throw new ArgumentException("Informe um servidor com até 80 caracteres, sem |.");
            return new ClientServer((installation == null ? "MANUAL" : installation.Key) + ":WORLD:" + label.ToUpperInvariant(),
                label, installation != null && installation.IdentityMemoryOnly);
        }

        internal static string Scope(ClientServer server, string key)
        { return server == null ? key : server.Key + "|" + key; }

        internal static bool Matches(ClientServer server, string key)
        { return string.Equals(server == null ? null : server.Key, string.IsNullOrEmpty(key) ? null : key, StringComparison.OrdinalIgnoreCase); }
    }

    internal sealed class ClientWindowInfo
    {
        internal IntPtr Handle { get; set; }
        internal int ProcessId { get; set; }
        internal string SessionKey { get; set; }
        internal string Title { get; set; }
        internal ClientServer Server { get; set; }
        internal bool ServerRequired { get; set; }
    }

    internal sealed class ClientBindings
    {
        private readonly Dictionary<string, ClientIdentity> _bindings = new Dictionary<string, ClientIdentity>();
        private readonly Dictionary<string, string> _memorySessions = new Dictionary<string, string>();
        private readonly Dictionary<string, ClientIdentity> _memoryIdentities = new Dictionary<string, ClientIdentity>();
        private readonly Dictionary<string, ClientServer> _servers = new Dictionary<string, ClientServer>();
        private readonly Dictionary<string, string> _manualTitles = new Dictionary<string, string>();
        private readonly Dictionary<string, string> _titlesAtLogout = new Dictionary<string, string>();
        internal void Clear() { _bindings.Clear(); _memorySessions.Clear(); _memoryIdentities.Clear(); _servers.Clear(); _manualTitles.Clear(); _titlesAtLogout.Clear(); }
        internal ClientIdentity Find(string key) { ClientIdentity value; return key != null && _bindings.TryGetValue(key, out value) ? value : null; }
        internal ClientIdentity FindMemory(string key) { ClientIdentity value; return key != null && _memoryIdentities.TryGetValue(key, out value) ? value : null; }
        internal void SetMemory(string key, ClientIdentity identity)
        {
            ClientIdentity previous = FindMemory(key);
            if (previous != null && (identity == null || previous.ProfileKey != identity.ProfileKey))
                Set(key, null);
            if (identity == null) _memoryIdentities.Remove(key);
            else _memoryIdentities[key] = identity;
        }
        internal void ObserveMemoryIdentity(ClientWindowInfo window, ClientIdentity identity)
        {
            if (identity != null) _titlesAtLogout.Remove(window.SessionKey);
            else if (FindMemory(window.SessionKey) != null) _titlesAtLogout[window.SessionKey] = window.Title;
            SetMemory(window.SessionKey, identity);
        }
        internal bool CanUseTitle(ClientWindowInfo window)
        {
            string previous;
            if (!_titlesAtLogout.TryGetValue(window.SessionKey, out previous)) return true;
            if (string.Equals(previous, window.Title, StringComparison.OrdinalIgnoreCase)) return false;
            _titlesAtLogout.Remove(window.SessionKey);
            return true;
        }
        internal void Set(string key, ClientIdentity identity)
        { if (string.IsNullOrEmpty(key)) throw new ArgumentException("Cliente indisponível."); if (identity == null) { _bindings.Remove(key); _manualTitles.Remove(key); } else _bindings[key] = identity; }
        internal void Set(ClientWindowInfo window, ClientIdentity identity)
        {
            Set(window.SessionKey, identity);
            if (identity != null) _manualTitles[window.SessionKey] = window.Title;
        }
        internal ClientIdentity ManualFor(ClientWindowInfo window)
        {
            string title;
            if (_manualTitles.TryGetValue(window.SessionKey, out title) &&
                !string.Equals(title, window.Title, StringComparison.OrdinalIgnoreCase))
                Set(window.SessionKey, null);
            ClientIdentity identity = Find(window.SessionKey);
            ClientServer server = ServerFor(window);
            return identity == null || (window.ServerRequired && server == null) ? null : identity.OnServer(server);
        }
        internal ClientServer ServerFor(ClientWindowInfo window)
        {
            ClientServer server;
            return _servers.TryGetValue(window.SessionKey, out server) ? server : window.Server;
        }
        internal bool SetServer(ClientWindowInfo window, ClientServer server)
        {
            if (ClientServer.Matches(ServerFor(window), server == null ? null : server.Key)) return false;
            if (ClientServer.Matches(window.Server, server == null ? null : server.Key)) _servers.Remove(window.SessionKey);
            else _servers[window.SessionKey] = server;
            Set(window.SessionKey, null);
            _memorySessions.Remove(window.SessionKey);
            _memoryIdentities.Remove(window.SessionKey);
            return true;
        }
        internal void Prune(IEnumerable<string> liveKeys)
        {
            var live = new HashSet<string>(liveKeys);
            foreach (string key in _bindings.Keys.Where(k => !live.Contains(k)).ToList()) _bindings.Remove(key);
            foreach (string key in _memorySessions.Keys.Where(k => !live.Contains(k)).ToList()) _memorySessions.Remove(key);
            foreach (string key in _memoryIdentities.Keys.Where(k => !live.Contains(k)).ToList()) _memoryIdentities.Remove(key);
            foreach (string key in _servers.Keys.Where(k => !live.Contains(k)).ToList()) _servers.Remove(key);
            foreach (string key in _manualTitles.Keys.Where(k => !live.Contains(k)).ToList()) _manualTitles.Remove(key);
            foreach (string key in _titlesAtLogout.Keys.Where(k => !live.Contains(k)).ToList()) _titlesAtLogout.Remove(key);
        }
        internal void ObserveMemorySession(string key, string token)
        {
            if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(token)) return;
            string previous;
            if (_memorySessions.TryGetValue(key, out previous) && previous != token) Set(key, null);
            _memorySessions[key] = token;
        }
    }

    internal static class ClientIdentities
    {
        private static readonly Regex Primary = new Regex(@"^\s*(?<name>.+?)\s*[\[\(\{]\s*(?<class>WB|WF|EA|EP|WR|MG|MC|ES|SK|MS)\s*[\]\)\}]\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly Regex Token = new Regex(@"(?:^|[\s\[\](){}_\-])(?<class>WB|WF|EA|EP|WR|MG|MC|ES|SK|MS)(?=$|[\s\[\](){}_\-])", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        private static readonly HashSet<string> Classes = new HashSet<string>(
            new[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS" }, StringComparer.OrdinalIgnoreCase);
        internal static bool IsValidClass(string code)
        { return !string.IsNullOrWhiteSpace(code) && Classes.Contains(code.Trim()); }
        internal static void ParseTitle(string title, out string name, out string code)
        {
            name = (title ?? "").Trim(); code = null;
            Match m = Primary.Match(name);
            if (m.Success) { name = m.Groups["name"].Value.Trim(); code = m.Groups["class"].Value.ToUpperInvariant(); return; }
            m = Token.Match(name); if (!m.Success) return;
            code = m.Groups["class"].Value.ToUpperInvariant();
            name = (name.Substring(0, m.Index) + " " + name.Substring(m.Index + m.Length)).Trim(' ', '-', '_', '[', ']', '(', ')', '{', '}');
        }
        internal static ClientIdentity FromTitle(string title, bool uniqueTitle, IDictionary<string, string> associations, ClientServer server = null)
        {
            if (!uniqueTitle) return null;
            string name, code; ParseTitle(title, out name, out code);
            string manual = null;
            bool associated = associations != null && associations.TryGetValue(TitleKey(title, server), out manual) && IsValidClass(manual);
            if (associated) code = manual;
            if (!IsValidClass(code) || string.IsNullOrWhiteSpace(name)) return null;
            if (IsGenericTitle(name)) return null;
            try { return new ClientIdentity(name, code, associated).OnServer(server); } catch (ArgumentException) { return null; }
        }

        internal static string TitleKey(string title, ClientServer server)
        { return ClientServer.Scope(server, (title ?? "").Trim().ToUpperInvariant()); }

        private static bool IsGenericTitle(string name)
        {
            string normalized = Regex.Replace(name, @"[\s._\-]", "").ToUpperInvariant();
            return normalized == "ELEMENTCLIENT" || normalized.StartsWith("PERFECTWORLD", StringComparison.Ordinal) ||
                normalized.StartsWith("THECLASSICPW", StringComparison.Ordinal);
        }

        internal static ClientIdentity FromSavedProfile(string name, IEnumerable<CharacterProgress> profiles, bool manual, ClientServer server = null)
        {
            if (string.IsNullOrWhiteSpace(name) || profiles == null || IsGenericTitle(name)) return null;
            CharacterProgress match = null;
            foreach (CharacterProgress profile in profiles)
            {
                if (!string.Equals(profile.CharacterName, name, StringComparison.OrdinalIgnoreCase) ||
                    !ClientServer.Matches(server, profile.ServerKey) || !IsValidClass(profile.ClassCode)) continue;
                if (match != null && !string.Equals(match.ClassCode, profile.ClassCode, StringComparison.OrdinalIgnoreCase))
                    return null;
                match = profile;
            }
            if (match == null) return null;
            try { return new ClientIdentity(match.CharacterName, match.ClassCode, manual).OnServer(server); }
            catch (ArgumentException) { return null; }
        }

        internal static ClientIdentity Resolve(ClientWindowInfo window, IList<ClientWindowInfo> windows, ClientBindings bindings,
            IDictionary<string, string> aliases, IEnumerable<CharacterProgress> profiles = null)
        {
            if (window == null) return null;
            var identities = new Dictionary<string, ClientIdentity>();
            var currentWindows = windows.Select(w => w.SessionKey == window.SessionKey ? window : w).ToList();
            foreach (ClientWindowInfo w in currentWindows)
                identities[w.SessionKey] = bindings.ManualFor(w) ?? Automatic(w, currentWindows, bindings, aliases, profiles);
            ClientIdentity result;
            if (!identities.TryGetValue(window.SessionKey, out result) || result == null) return null;
            return identities.Values.Count(i => i != null && i.ProfileKey == result.ProfileKey) == 1 ? result : null;
        }

        internal static ClientIdentity Automatic(ClientWindowInfo window, IList<ClientWindowInfo> windows,
            ClientBindings bindings, IDictionary<string, string> aliases, IEnumerable<CharacterProgress> profiles = null)
        { return Automatic(window, windows, bindings, aliases, profiles, bindings.ServerFor(window)); }

        internal static ClientIdentity Automatic(ClientWindowInfo window, IList<ClientWindowInfo> windows,
            ClientBindings bindings, IDictionary<string, string> aliases, IEnumerable<CharacterProgress> profiles, ClientServer server)
        {
            if (window.ServerRequired && server == null) return null;
            ClientIdentity memory = bindings.FindMemory(window.SessionKey);
            if (memory != null) return memory.OnServer(server);
            if (!bindings.CanUseTitle(window)) return null;
            if (windows.Any(other => other.SessionKey != window.SessionKey &&
                ClientServer.Matches(bindings.ServerFor(other), server == null ? null : server.Key) &&
                string.Equals(other.Title, window.Title, StringComparison.OrdinalIgnoreCase)))
                return null;
            return FromTitle(window.Title, true, aliases, server) ?? FromSavedProfile(window.Title, profiles, false, server);
        }

        internal static string LegacyMemoryProfileKey(ClientIdentity identity, IDictionary<string, CharacterProgress> profiles)
        {
            if (identity == null || !identity.FromMemory || profiles.ContainsKey(identity.ProfileKey)) return null;
            Match match = Primary.Match(identity.Name);
            if (!match.Success || !string.Equals(match.Groups["class"].Value, identity.ClassCode, StringComparison.OrdinalIgnoreCase)) return null;
            string key = ClientServer.Scope(identity.Server, match.Groups["name"].Value.Trim().ToUpperInvariant() + "|" + identity.ClassCode);
            CharacterProgress old;
            return profiles.TryGetValue(key, out old) && string.Equals(old.ClassCode, identity.ClassCode, StringComparison.OrdinalIgnoreCase) ? key : null;
        }

        internal static void AdoptLegacyProgress(IDictionary<string, CharacterProgress> profiles, string sourceKey, ClientIdentity identity)
        {
            CharacterProgress source = profiles[sourceKey];
            CharacterProgress target;
            if (identity.Server == null || !string.IsNullOrEmpty(source.ServerKey) ||
                !string.Equals(source.CharacterName, identity.Name, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(source.ClassCode, identity.ClassCode, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Confira o personagem e o servidor do perfil antigo.");
            if (!profiles.TryGetValue(identity.ProfileKey, out target))
                target = new CharacterProgress
                {
                    CharacterName = identity.Name, ClassCode = identity.ClassCode, ManualClass = true,
                    ServerKey = identity.Server.Key, ServerName = identity.Server.Name
                };
            foreach (var stage in source.MissionStates)
            {
                string existing;
                if (!target.MissionStates.TryGetValue(stage.Key, out existing) ||
                    (QuestProgression.IsDone(stage.Value) && !QuestProgression.IsDone(existing)))
                    target.MissionStates[stage.Key] = stage.Value;
            }
            if (string.IsNullOrEmpty(target.CultivationPath)) target.CultivationPath = source.CultivationPath;
            profiles[identity.ProfileKey] = target;
            profiles.Remove(sourceKey);
        }
    }
}
