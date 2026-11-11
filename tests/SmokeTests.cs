using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using PWQuestOverlay;
using PWQuestOverlay.Memory;

internal static class SmokeTests
{
    private static void TestMemoryIdentity(MemoryLayoutProfile profile)
    {
        PlayerIdentityLayout layout = profile.identity;
        IntPtr player = Marshal.AllocHGlobal(0x800), text = Marshal.AllocHGlobal(144);
        try
        {
            string name = "Ação[EP]";
            Marshal.Copy(new byte[0x800], 0, player, 0x800);
            Marshal.WriteInt32(player, (int)layout.RoleIdOffset, 1234);
            Marshal.WriteInt32(player, (int)layout.NamePointerOffset, text.ToInt32() + 12);
            Marshal.WriteInt32(text, 0, 1);
            Marshal.WriteInt32(text, 4, name.Length);
            Marshal.WriteInt32(text, 8, 64);
            byte[] encoded = Encoding.Unicode.GetBytes(name + "\0");
            Marshal.Copy(encoded, 0, IntPtr.Add(text, 12), encoded.Length);
            using (Process process = Process.GetCurrentProcess())
            using (ProcessMemoryReader reader = ProcessMemoryReader.Open(process, process.MainModule.ModuleName))
            {
                string[] codes = { "WR", "MG", "ES", "WF", "WB", "MC", "EA", "EP", "SK", "MS" };
                for (int i = 0; i < codes.Length; i++)
                {
                    Marshal.WriteInt32(player, (int)layout.ClassOffset, i);
                    PlayerIdentityMemory result = PlayerIdentityReader.Read(reader, player.ToInt64(), layout);
                    if (result.Name != name || result.RoleId != 1234 || result.ClassCode != codes[i]) throw new Exception("Nome UTF-16/classe incorretos.");
                }
                Action reject = delegate {
                    bool failed = false;
                    try { PlayerIdentityReader.Read(reader, player.ToInt64(), layout); } catch { failed = true; }
                    if (!failed) throw new Exception("Identidade inválida foi aceita.");
                };
                Marshal.WriteInt32(player, (int)layout.ClassOffset, 10); reject();
                Marshal.WriteInt32(player, (int)layout.ClassOffset, 7);
                Marshal.WriteInt32(text, 4, 65); reject();
                Marshal.WriteInt32(text, 4, name.Length);
                Marshal.WriteInt32(text, 8, 1); reject();
                Marshal.WriteInt32(text, 8, 64);
                Marshal.WriteInt16(text, 12 + name.Length * 2, 65); reject();
                Marshal.WriteInt16(text, 12 + name.Length * 2, 0);
                Marshal.WriteInt16(text, 12, 0); reject();
                Marshal.Copy(encoded, 0, IntPtr.Add(text, 12), encoded.Length);
                uint original = 1234;
                bool raceRejected = false;
                try {
                    PlayerIdentityReader.Read(address => address == player.ToInt64() + layout.RoleIdOffset ? original++ : reader.ReadUInt32(address),
                        reader.ReadBytes, player.ToInt64(), layout);
                } catch (InvalidDataException) { raceRejected = true; }
                if (!raceRejected) throw new Exception("Troca de personagem durante leitura foi aceita.");

                // Exercise the real read-only session against this x86 test process, with quests disabled.
                IntPtr root = Marshal.AllocHGlobal(4);
                string path = Path.Combine(Path.GetTempPath(), "PWOverlay-identity-" + Guid.NewGuid().ToString("N") + ".json");
                try
                {
                    var json = new System.Web.Script.Serialization.JavaScriptSerializer();
                    MemoryLayoutProfile fixture = json.Deserialize<MemoryLayoutProfile>(json.Serialize(profile));
                    fixture.module = process.MainModule.ModuleName;
                    fixture.player.baseRva = "0x" + checked((uint)(root.ToInt64() - process.MainModule.BaseAddress.ToInt64())).ToString("X");
                    fixture.player.chain = new List<string>();
                    Marshal.WriteInt32(root, player.ToInt32());
                    foreach (string attribute in new[] { "level", "hp", "hpMax", "mp", "mpMax" })
                        Marshal.WriteInt32(player, (int)fixture.AttributeOffset(attribute), 10);
                    File.WriteAllText(path, json.Serialize(new MemoryLayoutFile { schemaVersion = 1, profiles = new List<MemoryLayoutProfile> { fixture } }));
                    using (var monitor = new QuestMemoryMonitor(path, fixture.id, null))
                    {
                        monitor.Poll(new[] { process.Id }, false);
                        if (monitor.Find(process.Id).Identity != null) throw new Exception("Monitor publicou uma leitura isolada.");
                        monitor.Poll(new[] { process.Id }, false);
                        QuestMemoryClientState observed = monitor.Find(process.Id);
                        if (observed.Identity == null || observed.Identity.Name != name || observed.Ready || observed.Snapshot != null)
                            throw new Exception("Nome deve funcionar independentemente da leitura de missões.");
                        string token = observed.SessionToken;
                        Marshal.WriteInt32(player, (int)layout.RoleIdOffset, 5678);
                        monitor.Poll(new[] { process.Id }, false);
                        if (observed.Identity != null) throw new Exception("Monitor manteve o personagem anterior durante a troca.");
                        monitor.Poll(new[] { process.Id }, false);
                        if (observed.Identity == null || observed.Identity.RoleId != 5678 || observed.SessionToken == token)
                            throw new Exception("Monitor não detectou outro personagem no mesmo objeto.");
                        Marshal.WriteInt32(root, 0);
                        monitor.Poll(new[] { process.Id }, false);
                        if (observed.Identity != null) throw new Exception("Logout não descartou identidade.");
                    }
                    Marshal.WriteInt32(root, player.ToInt32());
                    TestBackgroundMemory(path, fixture.id, process.Id);
                }
                finally { Marshal.FreeHGlobal(root); if (File.Exists(path)) File.Delete(path); }
            }
        }
        finally { Marshal.FreeHGlobal(player); Marshal.FreeHGlobal(text); }

        var state = new QuestMemoryClientState { SessionToken = "first" };
        var first = new PlayerIdentityMemory { RoleId = 1, Name = "Primeiro", ClassCode = "WB" };
        var second = new PlayerIdentityMemory { RoleId = 2, Name = "Segundo", ClassCode = "EP" };
        if (state.ObserveIdentity(first) || !state.ObserveIdentity(first) || state.Identity != first) throw new Exception("Identidade exige duas leituras.");
        state.Ready = true; state.Snapshot = new QuestMemorySnapshot();
        state.Record(new[] { new QuestMemoryEvent { Kind = QuestMemoryEventKind.Activated, TaskId = 42 } });
        state.Record(new[] { new QuestMemoryEvent { Kind = QuestMemoryEventKind.DisappearedAfterObservation, TaskId = 42 } });
        QuestMemoryClientState published = state.CopyForUi();
        if (state.ObserveIdentity(second) || state.Identity != null || state.Ready || state.Snapshot != null || state.HasObservedDisappearance(42))
            throw new Exception("Troca de personagem deve descartar evidências imediatamente.");
        if (published.Identity != first || !published.Ready || !published.HasObservedDisappearance(42))
            throw new Exception("O trabalhador modificou evidências já publicadas para a interface.");
        if (!state.ObserveIdentity(second) || state.SessionToken == "first") throw new Exception("Nova identidade precisa de novo token.");
        state.InvalidateQuests();
        if (state.Identity != second) throw new Exception("Falha nas missões não deve apagar uma identidade validada.");
        state.ObserveIdentity(null);
        if (state.Identity != null || state.ObserveIdentity(second)) throw new Exception("Falha de identidade exige nova confirmação.");
    }

    private static void TestBackgroundMemory(string path, string profileId, int pid)
    {
        using (var entered = new ManualResetEventSlim())
        using (var release = new ManualResetEventSlim())
        using (var polling = new QuestMemoryPolling(path, profileId, message =>
        {
            if (!message.Contains("sessão aberta") || entered.IsSet) return;
            entered.Set();
            release.Wait(3000);
        }))
        {
            var window = new ClientWindowInfo { ProcessId = pid, SessionKey = "first-window" };
            var windows = new[] { window };
            polling.Poll(windows, false);
            if (!entered.Wait(2000) || release.IsSet)
                throw new Exception("A leitura deveria continuar em segundo plano.");
            var watch = Stopwatch.StartNew();
            polling.Poll(windows, false);
            if (watch.ElapsedMilliseconds > 500 || polling.Find(pid) != null)
                throw new Exception("A interface aguardou uma leitura bloqueada.");
            window.SessionKey = "reopened-window";
            polling.Poll(windows, false);
            release.Set();

            QuestMemoryClientState state = null;
            watch.Restart();
            while (watch.ElapsedMilliseconds < 3000)
            {
                polling.Poll(windows, false);
                state = polling.Find(pid);
                if (state != null && state.Identity != null) break;
                Thread.Sleep(20);
            }
            if (state == null || state.Identity == null || state.Identity.RoleId != 5678)
                throw new Exception("A leitura nova não foi publicada após a troca de janela.");
            polling.Forget(pid);
            if (polling.Find(pid) != null)
                throw new Exception("Evidência antiga sobreviveu à invalidação do cliente.");
            polling.Poll(new ClientWindowInfo[0], false);
        }
    }

    private static void TestIdentityOnlyMemory(MemoryLayoutProfile profile)
    {
        profile.Validate();
        IntPtr fixtureMemory = Marshal.AllocHGlobal(0x900);
        string path = Path.Combine(Path.GetTempPath(), "PWOverlay-identity126-" + Guid.NewGuid().ToString("N") + ".json");
        try
        {
            Marshal.Copy(new byte[0x900], 0, fixtureMemory, 0x900);
            IntPtr game = IntPtr.Add(fixtureMemory, 0x10), run = IntPtr.Add(fixtureMemory, 0x40);
            IntPtr player = IntPtr.Add(fixtureMemory, 0x80), name = IntPtr.Add(fixtureMemory, 0x800);
            using (Process process = Process.GetCurrentProcess())
            {
                var json = new System.Web.Script.Serialization.JavaScriptSerializer();
                MemoryLayoutProfile fixture = json.Deserialize<MemoryLayoutProfile>(json.Serialize(profile));
                int imageBase = process.MainModule.BaseAddress.ToInt32();
                fixture.module = process.MainModule.ModuleName;
                fixture.player.baseRva = "0x" + checked((uint)(fixtureMemory.ToInt64() - imageBase)).ToString("X");
                fixture.player.vtableRva = "0x1000";
                using (var file = File.OpenRead(process.MainModule.FileName))
                using (var hash = SHA256.Create())
                    fixture.moduleSha256 = BitConverter.ToString(hash.ComputeHash(file)).Replace("-", "");
                Marshal.WriteInt32(fixtureMemory, game.ToInt32());
                Marshal.WriteInt32(game, 0x1C, run.ToInt32());
                Marshal.WriteInt32(run, 0x2C, player.ToInt32());
                Marshal.WriteInt32(player, imageBase + 0x1000);
                Marshal.WriteInt32(player, (int)fixture.identity.RoleIdOffset, 126);
                Marshal.WriteInt32(player, (int)fixture.identity.ClassOffset, 7);
                Marshal.WriteInt32(player, (int)fixture.identity.NamePointerOffset, name.ToInt32() + 12);
                Marshal.WriteInt32(name, 0, 1);
                Marshal.WriteInt32(name, 4, 5);
                Marshal.WriteInt32(name, 8, 63);
                byte[] encoded = Encoding.Unicode.GetBytes("Pluma\0");
                Marshal.Copy(encoded, 0, IntPtr.Add(name, 12), encoded.Length);
                File.WriteAllText(path, json.Serialize(new MemoryLayoutFile
                { schemaVersion = 1, profiles = new List<MemoryLayoutProfile> { fixture } }));
                using (var monitor = new QuestMemoryMonitor(path, fixture.id, null))
                {
                    var ids = new[] { process.Id };
                    monitor.Poll(ids, true, true);
                    if (monitor.Find(process.Id).Identity != null)
                        throw new Exception("O 1.2.6 exige duas leituras para confirmar a identidade.");
                    monitor.Poll(ids, true, true);
                    QuestMemoryClientState state = monitor.Find(process.Id);
                    if (state.Identity == null || state.Identity.Name != "Pluma" || state.Identity.ClassCode != "EP" ||
                        state.Identity.RoleId != 126 || state.Ready || state.Snapshot != null)
                        throw new Exception("O perfil do 1.2.6 deve identificar nome e classe sem ler missões.");
                    Marshal.WriteInt32(player, imageBase + 0x2000);
                    monitor.Poll(ids, true, true);
                    if (state.Identity != null)
                        throw new Exception("A identidade deve ser invalidada quando a tabela virtual muda.");
                }
                fixture.moduleSha256 = new string('0', 64);
                bool rejected = false;
                try { fixture.VerifyModule(process.MainModule.FileName); }
                catch (InvalidDataException) { rejected = true; }
                if (!rejected) throw new Exception("Um executável diferente deve ser rejeitado.");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(fixtureMemory);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private static void TestStandaloneClients()
    {
        TestServerProfiles();
        var aliases = new Dictionary<string, string> { { "THE CLASSIC PW 1.4.4", "EP" }, { "CAUTON", "EP" } };
        var windows = new List<ClientWindowInfo> {
            new ClientWindowInfo { Handle = new IntPtr(11), ProcessId = 11, SessionKey = "11:100:11", Title = "The Classic PW 1.4.4" },
            new ClientWindowInfo { Handle = new IntPtr(12), ProcessId = 12, SessionKey = "12:100:12", Title = "The Classic PW 1.4.4" }
        };
        var bindings = new ClientBindings();
        var profiles = new List<CharacterProgress> { new CharacterProgress { CharacterName = "SemSigla", ClassCode = "WF" } };
        windows[0].Title = "SemSigla";
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases, profiles).ProfileKey != "SEMSIGLA|WF")
            throw new Exception("Título com nome salvo deve recuperar a classe sem sigla.");
        profiles.Add(new CharacterProgress { CharacterName = "SemSigla", ClassCode = "WB" });
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases, profiles) != null)
            throw new Exception("Classes diferentes para o mesmo nome exigem seleção manual.");
        profiles.RemoveAt(1);
        windows[1].Title = windows[0].Title;
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases, profiles) != null)
            throw new Exception("Títulos repetidos não podem recuperar o mesmo perfil.");
        windows[0].Title = windows[1].Title = "The Classic PW 1.4.4";
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases) != null || ClientIdentities.FromTitle("The Classic PW 1.4.4", true, aliases) != null)
            throw new Exception("Generic/duplicate titles must never create a shared profile.");
        bindings.Set(windows[0].SessionKey, new ClientIdentity("Primeiro", "WB", true));
        bindings.Set(windows[1].SessionKey, new ClientIdentity("Segundo", "EP", true));
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases).ProfileKey != "PRIMEIRO|WB" ||
            ClientIdentities.Resolve(windows[1], windows, bindings, aliases).ProfileKey != "SEGUNDO|EP")
            throw new Exception("Standalone clients must have independent persistent profile keys.");
        windows.Reverse();
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases).Name != "Segundo") throw new Exception("Changing focus/order must not change the binding.");
        bindings.Set(windows[0].SessionKey, new ClientIdentity("Primeiro", "WB", true));
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases) != null) throw new Exception("Two windows cannot silently share one character profile.");
        bindings.Set(windows[0].SessionKey, new ClientIdentity("Segundo", "EP", true));
        bindings.ObserveMemorySession(windows[0].SessionKey, "player-a");
        bindings.ObserveMemorySession(windows[0].SessionKey, "player-b");
        if (bindings.Find(windows[0].SessionKey) != null || bindings.Find(windows[1].SessionKey) == null) throw new Exception("A new player session must invalidate only its own binding.");
        bindings.Prune(new[] { "11:200:11" });
        if (bindings.Find("11:100:11") != null || bindings.Find("11:200:11") != null) throw new Exception("Recycled PID/HWND cannot inherit a session association.");
        foreach (string code in new[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS" })
            if (ClientIdentities.FromTitle("Teste[" + code + "]", true, aliases).ClassCode != code) throw new Exception("Helper/title compatibility: " + code);
        if (ClientIdentities.FromTitle("Cauton", true, aliases).ClassCode != "EP" || ClientIdentities.FromTitle("Wrex", true, aliases) != null)
            throw new Exception("Manual title aliases and class token boundaries must remain compatible.");
        bool invalid = false; try { new ClientIdentity("Wrong|Name", "WB", true); } catch (ArgumentException) { invalid = true; }
        if (!invalid) throw new Exception("Invalid profile delimiter accepted.");
        bindings.Clear();
        bindings.SetMemory(windows[0].SessionKey, new ClientIdentity("Memória", "EP", false, true));
        bindings.SetMemory(windows[1].SessionKey, new ClientIdentity("Outro", "WB", false, true));
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases).Name != "Memória") throw new Exception("Memory identity must work with duplicate default titles.");
        windows[0].Title = "TítuloAntigo[WR]";
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases).ClassCode != "EP") throw new Exception("Memory must precede stale Helper titles.");
        bindings.Set(windows[0].SessionKey, new ClientIdentity("Alias", "EP", true));
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases).Name != "Alias") throw new Exception("Manual profiles must remain available.");
        bindings.ObserveMemorySession(windows[0].SessionKey, "a");
        bindings.ObserveMemorySession(windows[0].SessionKey, "b");
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases).Name != "Memória") throw new Exception("New player must release old manual binding.");
        bindings.SetMemory(windows[1].SessionKey, new ClientIdentity("Memória", "EP", false, true));
        if (ClientIdentities.Resolve(windows[0], windows, bindings, aliases) != null) throw new Exception("Duplicate memory profiles must be blocked.");
        bindings.Prune(new string[0]);
        if (bindings.FindMemory(windows[0].SessionKey) != null) throw new Exception("Closed clients must discard identity.");
        var oldProfiles = new Dictionary<string, CharacterProgress> { { "AÇÃO|EP", new CharacterProgress { CharacterName = "Ação", ClassCode = "EP" } } };
        if (ClientIdentities.LegacyMemoryProfileKey(new ClientIdentity("Ação[EP]", "EP", false, true), oldProfiles) != "AÇÃO|EP" ||
            ClientIdentities.LegacyMemoryProfileKey(new ClientIdentity("Ação[WB]", "EP", false, true), oldProfiles) != null)
            throw new Exception("Real names with class suffix must preserve only the matching legacy profile.");
        var state = new QuestMemoryClientState { Ready = true, Snapshot = new QuestMemorySnapshot() };
        state.Record(new[] { new QuestMemoryEvent { Kind = QuestMemoryEventKind.Activated, TaskId = 42 } });
        state.Record(new[] { new QuestMemoryEvent { Kind = QuestMemoryEventKind.DisappearedAfterObservation, TaskId = 42 } });
        state.Invalidate();
        if (state.Ready || state.Snapshot != null || state.HasObservedDisappearance(42)) throw new Exception("Failed reads must discard stale memory evidence.");
    }

    private static void TestServerProfiles()
    {
        ClientServer oldServer = ClientServer.ForInstallation(@"C:\PW\The Classic PW 1.2.6\ElementClient.exe", "Personagem");
        ClientServer newServer = ClientServer.ForInstallation(@"D:\PW\The Classic PW 1.4.4\ElementClient.exe", "Personagem");
        if (oldServer.Key == newServer.Key || !oldServer.IdentityMemoryOnly || newServer.IdentityMemoryOnly)
            throw new Exception("A edição precisa vir do executável, inclusive com janela renomeada.");
        var windows = new List<ClientWindowInfo>
        {
            new ClientWindowInfo { SessionKey = "old", Title = "Personagem", Server = oldServer, ServerRequired = true },
            new ClientWindowInfo { SessionKey = "new", Title = "Personagem", Server = newServer, ServerRequired = true }
        };
        var oldProgress = new CharacterProgress { CharacterName = "Personagem", ClassCode = "EP", ServerKey = oldServer.Key };
        var newProgress = new CharacterProgress { CharacterName = "Personagem", ClassCode = "EP", ServerKey = newServer.Key, CultivationPath = "evil" };
        var bindings = new ClientBindings();
        var aliases = new Dictionary<string, string>
        {
            { "PERSONAGEM", "WB" }, { ClientIdentities.TitleKey("Personagem", oldServer), "WF" }
        };
        ClientIdentity oldIdentity = ClientIdentities.Resolve(windows[0], windows, bindings, aliases, new[] { oldProgress, newProgress });
        ClientIdentity newIdentity = ClientIdentities.Resolve(windows[1], windows, bindings, aliases, new[] { oldProgress, newProgress });
        if (oldIdentity.ClassCode != "WF" || newIdentity.ClassCode != "EP")
            throw new Exception("Classe e associações por título precisam ser limitadas ao servidor.");
        aliases.Clear();
        oldIdentity = ClientIdentities.Resolve(windows[0], windows, bindings, aliases, new[] { oldProgress, newProgress });
        if (oldIdentity.ProfileKey == newIdentity.ProfileKey)
            throw new Exception("Homônimos da mesma classe em servidores diferentes devem ter chaves distintas.");
        bindings.Set(windows[1], newIdentity);
        windows[1].Title = "Outro[WB]";
        if (ClientIdentities.Resolve(windows[1], windows, bindings, aliases).Name != "Outro" || bindings.Find("new") != null)
            throw new Exception("Uma associação antiga não pode acompanhar outro personagem na mesma janela.");
        ClientServer world = ClientServer.ForSelection(newServer, "Mundo B");
        if (!bindings.SetServer(windows[1], world) || bindings.ServerFor(windows[0]).Key != oldServer.Key)
            throw new Exception("A troca de mundo deve ser individual por cliente.");
        if (ClientIdentities.Resolve(windows[1], windows, bindings, aliases).Server.Key != world.Key)
            throw new Exception("O mundo selecionado deve prevalecer para a identidade da janela.");
        bindings.SetMemory("new", new ClientIdentity("Outro", "WB", false, true));
        bindings.Set(windows[1], new ClientIdentity("Outro", "WB", true));
        bindings.ObserveMemoryIdentity(windows[1], null);
        if (bindings.Find("new") != null || ClientIdentities.Resolve(windows[1], windows, bindings, aliases) != null)
            throw new Exception("Ao perder a identidade confirmada, nem a associação manual nem o título antigo devem reabrir o perfil.");
        windows[1].Title = "Próximo[EP]";
        if (ClientIdentities.Resolve(windows[1], windows, bindings, aliases).Name != "Próximo")
            throw new Exception("Um título novo pode identificar o próximo personagem.");

        var legacy = new CharacterProgress { CharacterName = "Personagem", ClassCode = "EP", CultivationPath = "god", HasOverlayMarker = true };
        legacy.MissionStates["a"] = "done";
        newProgress.MissionStates["a"] = "skipped";
        newProgress.MissionStates["b"] = "done";
        var saved = new Dictionary<string, CharacterProgress>
        { { "PERSONAGEM|EP", legacy }, { oldIdentity.ProfileKey, oldProgress }, { newIdentity.ProfileKey, newProgress } };
        ClientIdentities.AdoptLegacyProgress(saved, "PERSONAGEM|EP", newIdentity);
        if (saved.ContainsKey("PERSONAGEM|EP") || oldProgress.MissionStates.Count != 0 ||
            saved[newIdentity.ProfileKey].MissionStates.Count != 2 || newProgress.MissionStates["a"] != "done" ||
            newProgress.CultivationPath != "evil" || newProgress.HasOverlayMarker)
            throw new Exception("Vincular um perfil antigo deve somar progresso somente no servidor escolhido.");
    }

    private static int Main(string[] args)
    {
        try
        {
            if (args.Length != 4) throw new ArgumentException(
                "Informe quest_routes.json, memory_offsets.json, task_catalog_144.tsv e task_history_126.tsv.");
            RouteRepository routes = RouteRepository.Load(args[0]);
            QuestMission retiredCultivation = routes.AllMissions.Single(mission => mission.id == "ENDGAME|ALL|925");
            var activeCultivation = routes.GetForClass("EP");
            QuestMission currentCultivation = activeCultivation.Single(mission => mission.id == retiredCultivation.aliasOf);
            var legacyCultivation = new CharacterProgress();
            legacyCultivation.MissionStates[retiredCultivation.id] = "done-memory";
            if (activeCultivation.Contains(retiredCultivation) || !routes.RestoreAliasProgress(activeCultivation, legacyCultivation) ||
                !QuestProgression.MissionDone(currentCultivation, legacyCultivation) ||
                routes.RestoreAliasProgress(activeCultivation, legacyCultivation))
                throw new Exception("Cultivo 59 deve aparecer uma vez e reaproveitar a conclusão anterior.");
            TestStandaloneClients();
            string maintenanceRoot = Path.Combine(Path.GetTempPath(), "PWOverlay-scope-test");
            if (!PWOverlayShared.ToolMaintenance.BelongsToDirectory(Path.Combine(maintenanceRoot, "PW Quest Overlay.exe"), maintenanceRoot) ||
                PWOverlayShared.ToolMaintenance.BelongsToDirectory(Path.Combine(maintenanceRoot + "-other", "PW Quest Overlay.exe"), maintenanceRoot) ||
                PWOverlayShared.ToolMaintenance.BelongsToDirectory(Path.Combine(maintenanceRoot, "elementclient.exe"), maintenanceRoot) ||
                PWOverlayShared.ToolMaintenance.BelongsToDirectory(Path.Combine(maintenanceRoot, "PW Helper.exe"), maintenanceRoot) ||
                PWOverlayShared.AppShutdown.EventName("overlay", maintenanceRoot) != PWOverlayShared.AppShutdown.EventName("overlay", maintenanceRoot.ToUpperInvariant() + "\\") ||
                PWOverlayShared.AppShutdown.EventName("overlay", maintenanceRoot) == PWOverlayShared.AppShutdown.EventName("overlay", maintenanceRoot + "-other"))
                throw new Exception("Maintenance must be scoped to our executables in the exact installation directory.");
            var expected = new Dictionary<string, int>
            {
                { "WR", 116 }, { "WB", 116 }, { "WF", 121 },
                { "EA", 117 }, { "EP", 117 }, { "MC", 115 },
                { "MG", 116 }, { "ES", 115 }, { "SK", 106 }, { "MS", 126 }
            };
            foreach (KeyValuePair<string, int> pair in expected)
            {
                int actual = routes.GetForClass(pair.Key).Count(m => !m.id.StartsWith("ENDGAME|", StringComparison.Ordinal));
                if (actual != pair.Value)
                    throw new InvalidDataException(pair.Key + ": esperado " + pair.Value + ", obtido " + actual);
            }

            List<QuestMission> wr = routes.GetForClass("WR");
            List<QuestMission> mc = routes.GetForClass("MC");
            if (!wr.Select(m => m.id).SequenceEqual(routes.GetForClass("MG").Select(m => m.id)) ||
                !mc.Select(m => m.id).SequenceEqual(routes.GetForClass("ES").Select(m => m.id)))
                throw new InvalidDataException("MG/ES não compartilham a rota correta de sua raça.");
            foreach (string guardian in new[] { "SK", "MS" })
            {
                List<QuestMission> guardianRoute = routes.GetForClass(guardian);
                string other = guardian == "SK" ? "MS" : "SK";
                if (guardianRoute.Any(m => m.classScope == other))
                    throw new InvalidDataException("Etapa da outra classe na rota " + guardian);
                if (!guardianRoute.Any(m => m.id == "SKMS|SK/MS|23757") ||
                    !guardianRoute.Any(m => m.id == "SKMS|SK/MS|23215"))
                    throw new InvalidDataException("Treinador ou chegada à Cidade do Dragão ausente em " + guardian);
            }
            foreach (string guardian in new[] { "SK", "MS" })
                if (!routes.GetForClass(guardian).Any(m => m.steps.Any(s => s.memoryTaskId == 23760)) ||
                    !routes.GetForClass(guardian).Any(m => m.steps.Any(s => s.memoryTaskId == 23761)))
                    throw new InvalidDataException("Maestria de Habilidades deve incluir os dois filhos obrigatórios.");
            foreach (uint summon in new uint[] { 23029, 23030, 23031, 23032, 23033, 23034, 23035, 23036, 23037, 23038 })
                if (!routes.GetForClass("MS").Any(m => m.id == "SKMS|MS|" + summon))
                    throw new InvalidDataException("Invocação/planta do Místico ausente: " + summon);
            if (wr[2].name != "Revitalizando a Pátria" || wr[2].steps.Count != 7)
                throw new InvalidDataException("Etapas de Revitalizando a Pátria não foram carregadas.");
            if (mc[1].name != "Cidade das Tormentas" || mc[1].steps.Count != 10)
                throw new InvalidDataException("Etapas de Cidade das Tormentas não foram carregadas.");
            foreach (QuestMission mission in routes.AllMissions)
            {
                if (mission.steps == null || mission.steps.Count == 0)
                    throw new InvalidDataException("Missão sem etapas: " + mission.name);
                if (mission.expectedLevel < mission.minLevel)
                    throw new InvalidDataException("Nível previsto abaixo do mínimo: " + mission.name);
                int previousRouteOrder = 0;
                foreach (QuestStep step in mission.steps)
                {
                    if (string.IsNullOrWhiteSpace(step.title))
                        throw new InvalidDataException("Etapa sem título de submissão: " + mission.name + " / " + step.id);
                    if (step.routeOrder <= 0)
                        throw new InvalidDataException("Etapa sem ordem global: " + mission.name + " / " + step.id);
                    if (step.routeOrder <= previousRouteOrder)
                        throw new InvalidDataException("Etapas fora de ordem dentro da missão: " + mission.name);
                    previousRouteOrder = step.routeOrder;
                }
            }

            MemoryLayoutFile memoryLayouts = MemoryLayoutFile.Load(args[1]);
            TestMemoryIdentity(memoryLayouts.profiles[0]);
            TestIdentityOnlyMemory(memoryLayouts.Find("the_classic_126_identity"));
            ProgressionTests.TestHistoryReader(memoryLayouts.profiles[0]);
            ProgressionTests.TestHistoryReader(memoryLayouts.Find("the_classic_126_identity"));
            MemoryLayoutProfile memoryProfile = memoryLayouts.Find("the_classic_144");
            if (memoryProfile == null)
                throw new InvalidDataException("Perfil de memória 1.4.4 ausente.");
            memoryProfile.Validate();
            if (memoryProfile.pointerSize != 4 ||
                memoryProfile.player.className != "CECHostPlayer" ||
                memoryProfile.quests.className != "CECTaskInterface" ||
                memoryProfile.quests.ActiveEntryStride != 0x20 ||
                memoryProfile.quests.ActiveParentIndexOffset != 0x2 ||
                memoryProfile.quests.ActiveFlagsOffset != 0x7 ||
                memoryProfile.inventory.PlayerPointerOffset != 0xFDC ||
                memoryProfile.inventory.TaskInventoryOffset != 0x78 ||
                memoryProfile.inventory.ItemIdOffset != 0xC ||
                memoryProfile.inventory.ItemCountOffset != 0x14)
                throw new InvalidDataException("Layout de memória 1.4.4 inesperado.");

            TaskCatalog taskCatalog = TaskCatalog.Load(args[2]);
            ProgressionTests.TestClassic126Mapping(args[3], taskCatalog);
            ProgressionTests.TestMineHistory(args[3], taskCatalog, routes);
            ProgressionTests.Run(routes, taskCatalog);
            TaskCatalogEntry bamboo = taskCatalog.Find(2646);
            TaskCatalogEntry bambooFlight = taskCatalog.Find(2655);
            TaskCatalogEntry beetleBlood = taskCatalog.Find(936);
            int beetleBloodCount = 0;
            if (beetleBlood != null)
                beetleBlood.RequiredTaskItems.TryGetValue(3502, out beetleBloodCount);
            if (taskCatalog.Count != 24277 || bamboo == null ||
                bamboo.Name != "Vila do Bambu" || bambooFlight == null ||
                bambooFlight.Name != "Voe para a Vila do Bam" ||
                bambooFlight.ParentId != bamboo.Id ||
                beetleBloodCount != 10 ||
                taskCatalog.FindByName("Voe para a Vila do Bam").Count != 1 ||
                taskCatalog.FindByName("Moeda Sangrenta de Fer").Count < 2)
                throw new InvalidDataException("Catálogo dataminado de missões está divergente.");

            var tracker = new QuestStateTracker();
            var reconnectedWb = new QuestMemorySnapshot();
            reconnectedWb.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1177 });
            reconnectedWb.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1179 });
            if (!taskCatalog.HasActiveOrderedSuccessor(1178, 1179, reconnectedWb))
                throw new InvalidDataException("Sucessor de Primeira Viagem não recuperou abates concluídos antes da conexão.");
            reconnectedWb.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 1178 });
            if (taskCatalog.HasActiveOrderedSuccessor(1178, 1179, reconnectedWb))
                throw new InvalidDataException("Objetivo ainda ativo foi dado como concluído pelo sucessor.");
            var parallelChildren = new QuestMemorySnapshot();
            parallelChildren.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 805 });
            if (taskCatalog.HasActiveOrderedSuccessor(804, 805, parallelChildren) ||
                taskCatalog.HasActiveOrderedSuccessor(1178, 805, parallelChildren))
                throw new InvalidDataException("Filhos paralelos ou de outra cadeia provaram conclusão.");
            var deliveredBamboo = new QuestMemorySnapshot();
            if (taskCatalog.IsChainRecordedComplete(938, deliveredBamboo))
                throw new InvalidDataException("Ausência sem histórico foi tratada como entrega.");
            deliveredBamboo.FinishedTaskIds.Add(938);
            if (!taskCatalog.IsChainRecordedComplete(938, deliveredBamboo))
                throw new InvalidDataException("Histórico de Bambu dos Selvagens não foi reconhecido.");
            deliveredBamboo.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 938 });
            if (taskCatalog.IsChainRecordedComplete(938, deliveredBamboo))
                throw new InvalidDataException("Missão repetida ativa foi confundida com entrega anterior.");
            var conversations = new QuestMemoryClientState();
            conversations.Observe(deliveredBamboo);
            conversations.Record(new[] {new QuestMemoryEvent {
                TaskId = 938, Kind = QuestMemoryEventKind.DisappearedAfterObservation }});
            if (!conversations.HasObservedDisappearance(938) ||
                !conversations.HasObservedDisappearance(938) ||
                conversations.HasObservedDisappearance(939))
                throw new InvalidDataException("Conversa/entrega repetida perdeu a evidência ou usou outra missão.");
            conversations.Record(new[] {new QuestMemoryEvent {
                TaskId = 938, Kind = QuestMemoryEventKind.Activated }});
            if (conversations.HasObservedDisappearance(938))
                throw new InvalidDataException("Reaceite não limpou a evidência antiga.");
            var materials = new QuestMemorySnapshot();
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 80 });
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 82 });
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 86 });
            materials.TaskItemCounts[2377] = 10;
            materials.TaskItemCounts[2375] = 2;
            if (taskCatalog.Find(85).ParentId != 82 || taskCatalog.Find(85).TopId != 80 ||
                !taskCatalog.AreTaskItemsComplete(85, materials) ||
                taskCatalog.AreTaskItemsComplete(86, materials) ||
                taskCatalog.AreTaskItemsComplete(82, materials))
                throw new InvalidDataException("Coletas filhas não distinguem lenha 10/10 de ferro 2/10.");
            materials.TaskItemCounts[2377] = 9;
            if (taskCatalog.AreTaskItemsComplete(85, materials))
                throw new InvalidDataException("Lenha incompleta avançou.");
            materials.TaskItemCounts[2377] = 10;
            materials.TaskItemCounts[2375] = 10;
            materials.ActiveTasks.RemoveAll(entry => entry.TaskId != 80);
            if (!taskCatalog.AreTaskItemsComplete(85, materials) ||
                !taskCatalog.AreTaskItemsComplete(86, materials))
                throw new InvalidDataException("Coletas completas não sobreviveram ao desaparecimento dos filhos.");
            materials.ActiveTasks.Clear();
            materials.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 934 });
            if (taskCatalog.AreTaskItemsComplete(85, materials) ||
                taskCatalog.AreTaskItemsComplete(86, materials))
                throw new InvalidDataException("Itens sem cadeia ativa foram tratados como missão completa.");
            QuestMission foundry = routes.GetForClass("EA").Find(m => m.name == "Livro de Fundição");
            if (foundry.steps[2].title != taskCatalog.Find(85).Name ||
                foundry.steps[3].title != taskCatalog.Find(86).Name)
                throw new InvalidDataException("Rota de matérias-primas aponta para o pai em vez das submissões.");
            TaskCatalogEntry hedgehogs = taskCatalog.Find(2714);
            int boundObjectives = 0;
            foreach (QuestMission mission in routes.AllMissions)
            foreach (QuestStep step in mission.steps)
            {
                if (step.memoryTaskId == 0) continue;
                boundObjectives++;
                TaskCatalogEntry boundTask = taskCatalog.Find(step.memoryTaskId);
                if (boundTask == null || boundTask.Name != step.title)
                    throw new InvalidDataException("Associação explícita divergente: " + mission.name + "/" + step.id);
                if (step.memoryItemIds == null || step.memoryItemIds.Length == 0) continue;
                var partial = new QuestMemorySnapshot();
                partial.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = boundTask.TopId });
                foreach (uint itemId in step.memoryItemIds)
                {
                    int requirement;
                    if (!boundTask.RequiredTaskItems.TryGetValue(itemId, out requirement))
                        throw new InvalidDataException("Item vinculado a outra missão.");
                    partial.TaskItemCounts[itemId] = requirement;
                }
                if (!taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, step.memoryItemIds) ||
                    taskCatalog.AreTaskItemsComplete(boundTask.Id, partial) ||
                    taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, new uint[] { uint.MaxValue }))
                    throw new InvalidDataException("Materiais independentes não foram separados: " + mission.name + "/" + step.id);
                foreach (uint itemId in step.memoryItemIds) partial.TaskItemCounts[itemId]--;
                if (taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, step.memoryItemIds))
                    throw new InvalidDataException("Material abaixo da quantidade avançou.");
                partial.ActiveTasks.Clear();
                foreach (uint itemId in step.memoryItemIds)
                    partial.TaskItemCounts[itemId] = boundTask.RequiredTaskItems[itemId];
                if (taskCatalog.AreTaskItemsComplete(boundTask.Id, partial, step.memoryItemIds))
                    throw new InvalidDataException("Coleta avançou sem cadeia ativa.");
            }
            if (boundObjectives < 62)
                throw new InvalidDataException("Associações das cadeias aninhadas incompletas.");
            var automaticChild = new QuestMemorySnapshot();
            automaticChild.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 736 });
            if (!taskCatalog.CanConfirmAutomaticKillCompletion(804, "Matar (5) Gato-Rei Sentinela", automaticChild) ||
                taskCatalog.CanConfirmAutomaticKillCompletion(804, "Entregue ao guarda", automaticChild) ||
                taskCatalog.CanConfirmAutomaticKillCompletion(6440, "Matar (6) Espinhos do Orgulho", automaticChild))
                throw new InvalidDataException("Transição de objetivo automático confundiu entrega ou outra cadeia.");
            automaticChild.ActiveTasks.Add(new ActiveQuestMemoryEntry { TaskId = 804 });
            if (taskCatalog.CanConfirmAutomaticKillCompletion(804, "Matar (5) Gato-Rei Sentinela", automaticChild))
                throw new InvalidDataException("Filho ainda ativo foi tratado como desaparecido.");
            automaticChild.ActiveTasks.Clear();
            if (taskCatalog.CanConfirmAutomaticKillCompletion(804, "Matar (5) Gato-Rei Sentinela", automaticChild))
                throw new InvalidDataException("Abandono da cadeia foi tratado como objetivo concluído.");
            string killEvidence;
            var killEntry = new ActiveQuestMemoryEntry { TaskId = 2714 };
            if (hedgehogs == null || hedgehogs.RequiredMonsterId != 1037 ||
                hedgehogs.RequiredMonsterKills != 10)
                throw new InvalidDataException("Requisitos dos filhotes de ouriço divergentes.");
            foreach (int count in new[] { 0, 1, 9, 10, 11, 256 })
            {
                killEntry.FirstMonsterKillCount = count;
                bool expectedComplete = count >= 10 && count <= 255;
                if (hedgehogs.TryGetKillProgress(killEntry, "Matar (10) Filhote de Ouriço", out killEvidence) != expectedComplete)
                    throw new InvalidDataException("Limiar de abates incorreto: " + count);
            }
            killEntry.FirstMonsterKillCount = 10;
            foreach (string action in new[] {
                "Aceite a missão com Guarda", "Entregue a missão para Guarda",
                "Mate 10 filhotes e entregue ao Guarda", "Pegar 10 itens matando filhotes" })
                if (hedgehogs.TryGetKillProgress(killEntry, action, out killEvidence))
                    throw new InvalidDataException("Contador de abates avançou uma etapa incompatível.");
            if (hedgehogs.TryGetKillProgress(null, "Matar (10) Filhote de Ouriço", out killEvidence) ||
                beetleBlood.TryGetKillProgress(new ActiveQuestMemoryEntry {
                    TaskId = 936, FirstMonsterKillCount = 10 }, "Matar besouros", out killEvidence))
                throw new InvalidDataException("Contador avançou uma missão inativa ou uma coleta.");
            killEntry.TaskId = 694;
            if (hedgehogs.TryGetKillProgress(killEntry, "Matar (10) Filhote de Ouriço", out killEvidence))
                throw new InvalidDataException("Contador de outra missão foi reutilizado.");
            var baseline = new QuestMemorySnapshot();
            baseline.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 10, Flags = 0xA1 });
            baseline.FinishedTaskIds.Add(20);
            tracker.SetBaseline(baseline);
            var changed = new QuestMemorySnapshot();
            changed.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 10, Flags = 0xA3 });
            changed.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 11, Flags = 0xA1 });
            changed.FinishedTaskIds.Add(20);
            changed.FinishedTaskIds.Add(10);
            List<QuestMemoryEvent> changes = tracker.Update(changed);
            if (changes.Count != 3 ||
                changes.Count(item => item.Kind == QuestMemoryEventKind.StateChanged) != 1 ||
                changes.Count(item => item.Kind == QuestMemoryEventKind.Activated) != 1 ||
                changes.Count(item => item.Kind == QuestMemoryEventKind.AddedToHistory) != 1)
                throw new InvalidDataException("Rastreador não distinguiu as transições de missão.");
            var disappeared = new QuestMemorySnapshot();
            disappeared.ActiveTasks.Add(new ActiveQuestMemoryEntry
                { TaskId = 11, Flags = 0xA1 });
            disappeared.FinishedTaskIds.Add(20);
            disappeared.FinishedTaskIds.Add(10);
            List<QuestMemoryEvent> removals = tracker.Update(disappeared);
            if (removals.Count != 1 ||
                removals[0].Kind != QuestMemoryEventKind.DisappearedAfterObservation ||
                removals[0].TaskId != 10)
                throw new InvalidDataException("Rastreador confundiu ausência com missão nunca aceita.");

            List<QuestMission> common = routes.AllMissions.Where(m => m.routeKey == "COMMON").ToList();
            int lastLevelTwenty = common.Where(m => m.minLevel == 20)
                .SelectMany(m => m.steps).Max(step => step.routeOrder);
            int firstLaterLevel = common.Where(m => m.minLevel > 20)
                .SelectMany(m => m.steps).Min(step => step.routeOrder);
            if (firstLaterLevel >= lastLevelTwenty)
                throw new InvalidDataException("A rota comum 20-60 não intercala missões entre regiões e retornos.");

            QuestMission cultivo29 = common.Find(m => m.id == "COMMON|ALL|29|Cultivo 29 — O Infante Primordial");
            QuestMission moeda29 = common.Find(m => m.id == "COMMON|ALL|29|DG29 — Moeda Sangrenta de Ferro 2 Peão");
            if (cultivo29 == null || cultivo29.name != "Princípio Lúcido." ||
                moeda29 == null || moeda29.name != "Moeda Sangrenta de Fer")
                throw new InvalidDataException("Nomes dataminados de Cultivo 29 ou DG29 estão ausentes.");
            if (cultivo29.steps[0].routeOrder >= moeda29.steps[moeda29.steps.Count - 1].routeOrder)
                throw new InvalidDataException("Cultivo 29 e Moeda da DG29 não foram agrupados corretamente.");
            if (!(moeda29.steps[1].routeOrder < cultivo29.steps[2].routeOrder &&
                  cultivo29.steps[2].routeOrder < moeda29.steps[2].routeOrder &&
                  moeda29.steps[2].routeOrder < cultivo29.steps[3].routeOrder &&
                  moeda29.steps[3].routeOrder < cultivo29.steps[4].routeOrder))
                throw new InvalidDataException("DG29 separou objetivos de chefes ou retornos ao Imortal.");
            QuestMission dg39 = common.Find(m => m.id.Contains("DG39"));
            QuestMission dg59 = common.Find(m => m.id.Contains("DG59"));
            QuestMission cultivo59 = common.Find(m => m.id.Contains("Cultivo 59"));
            QuestMission flightCleanup = common.Find(m => m.id.Contains("Remover missão de voo"));
            if (dg39.steps.Last().routeOrder >= common.Where(m => m.minLevel >= 40)
                    .SelectMany(m => m.steps).Min(s => s.routeOrder) ||
                dg59.steps[1].routeOrder >= cultivo59.steps[2].routeOrder ||
                flightCleanup.steps.Last().routeOrder >= common.Where(m => m.minLevel == 31)
                    .SelectMany(m => m.steps).Min(s => s.routeOrder))
                throw new InvalidDataException("DG39, DG59 ou remoção do voo foram adiadas para fora da visita planejada.");

            QuestMission forgeWr = wr.Find(m => m.name == "Habilidades Produção");
            QuestMission hedgehogWr = wr.Find(m => m.name == "Espinho do Ouriço");
            if (forgeWr.steps[1].routeOrder >= hedgehogWr.steps[0].routeOrder)
                throw new InvalidDataException("Rota WR ainda manda sair da cidade antes de falar com o Ancião no nível 5.");
            QuestMission weaponsWr = wr.Find(m => m.name == "Fazer Armas");
            QuestMission jumpWr = wr.Find(m => m.name == "Teste de Salto");
            if (!(weaponsWr.steps[0].routeOrder < jumpWr.steps[0].routeOrder &&
                  jumpWr.steps[jumpWr.steps.Count - 1].routeOrder < weaponsWr.steps[1].routeOrder))
                throw new InvalidDataException("Rota WR não intercala Teste de Salto e Fazer Armas.");

            QuestMission pearl = mc.Find(m => m.name == "Pérola de Lágrimas");
            QuestStep pearlReport = pearl.steps.Find(step => step.id == "e9");
            if (pearlReport == null || pearlReport.title != "Reportar ao Lu Fu")
                throw new InvalidDataException("Submissão final de Pérola de Lágrimas não está identificada.");
            if (pearl.steps.Find(step => step.id == "e8").action.IndexOf("Sapo", StringComparison.OrdinalIgnoreCase) < 0)
                throw new InvalidDataException("Objetivo do Sapo está ausente em Pérola de Lágrimas.");

            foreach (string classCode in expected.Keys)
            {
                QuestMission forgePopup = routes.GetForClass(classCode).Find(m =>
                    m.name == "Habilidades Produção");
                bool guardian = classCode == "SK" || classCode == "MS";
                if (forgePopup == null || forgePopup.minLevel != (guardian ? 6 : 5) ||
                    (!guardian && forgePopup.expectedLevel != 5) || forgePopup.steps.Count != 2)
                    throw new InvalidDataException("Orientação de forja ausente ou inválida em " + classCode);
                QuestMission firstLevelFive = routes.GetForClass(classCode).Find(m => m.expectedLevel >= 5);
                if (!guardian && firstLevelFive != forgePopup)
                    throw new InvalidDataException("Orientação de forja não é a primeira missão prevista do nível 5 em " + classCode);

                List<QuestMission> route = routes.GetForClass(classCode);
                if (!route[route.Count - 1].cultivationChoice || route[route.Count - 1].expectedLevel != 89 ||
                    route.Any(m => m.expectedLevel >= 89 && !m.cultivationChoice))
                    throw new InvalidDataException("A rota avançou além da escolha de cultivo pendente em " + classCode);
                foreach (string path in new[] { "GOD", "EVIL" })
                {
                    List<QuestMission> selectedRoute = routes.GetForClass(classCode, path);
                    if (selectedRoute[selectedRoute.Count - 1].expectedLevel != 105 ||
                        selectedRoute[selectedRoute.Count - 1].id != "ENDGAME|ALL|LEVEL105")
                        throw new InvalidDataException("Rota não termina em 105: " + classCode + "/" + path);
                    if (selectedRoute.Any(m => !string.IsNullOrWhiteSpace(m.cultivationPath) && m.cultivationPath != path))
                        throw new InvalidDataException("Cultivo oposto presente na rota: " + classCode + "/" + path);
                    foreach (int cultivation in (path == "GOD" ? new[] { 2804, 3227, 14161, 14015 } : new[] { 2818, 3204, 14016 }))
                        if (!selectedRoute.Any(m => m.id == "ENDGAME|" + path + "|" + cultivation))
                            throw new InvalidDataException("Cadeia de cultivo ausente: " + cultivation);
                    var pathOrders = new HashSet<int>();
                    foreach (QuestStep stage in selectedRoute.SelectMany(m => m.steps))
                        if (!pathOrders.Add(stage.routeOrder)) throw new InvalidDataException("Ordem duplicada no caminho " + path);
                }
                var required = new Dictionary<string, string>
                {
                    { "COMMON|ALL|29|DG29 — Moeda Sangrenta de Ferro 2 Peão", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|39|DG39 — Moeda Sangrenta de Ferro 3 Fantasma", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|51|DG49 — Moeda Sangrenta de Ferro 4 Wanjun", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|59|DG59 — Moeda Sangrenta de Ferro 5 Fera Sagrada", "Moeda Sangrenta de Fer" },
                    { "COMMON|ALL|50|Pilar Esquisito — cadeia completa", "Pilar Esquisito" },
                    { "COMMON|ALL|30|Remover missão de voo do questlog", "Remover missão de voo do questlog" }
                };
                foreach (KeyValuePair<string, string> item in required)
                {
                    QuestMission requiredMission = route.Find(m => m.id == item.Key);
                    if (requiredMission == null || requiredMission.name != item.Value)
                        throw new InvalidDataException(item.Value + " ausente ou com nome divergente em " + classCode);
                }

                var globalOrders = new HashSet<int>();
                foreach (QuestMission mission in route)
                    foreach (QuestStep step in mission.steps)
                        if (!globalOrders.Add(step.routeOrder))
                            throw new InvalidDataException("Ordem global repetida em " + classCode + ": " + step.routeOrder);

                QuestMission forge = route.Find(m => m.name == "Habilidades Produção");
                int lastLevelSix = route.Where(m => m.minLevel == 6).SelectMany(m => m.steps).Max(step => step.routeOrder);
                if (forge.steps[forge.steps.Count - 1].routeOrder >= lastLevelSix)
                    throw new InvalidDataException("Orientação de forja ficou atrasada na rota " + classCode);
            }

            QuestMission conspiracy = mc.Find(m => m.name == "Conspiração");
            QuestMission enemyInformation = mc.Find(m => m.name == "Informação do Inimigo");
            QuestMission celestialBalance = mc.Find(m => m.name == "O Equilíbrio Celestial");
            if (conspiracy == null || conspiracy.steps.Find(step => step.title == "Reportar para Tormenta") == null)
                throw new InvalidDataException("Nome dataminado de Conspiração ou de suas submissões está ausente.");
            if (enemyInformation == null || enemyInformation.steps.Find(step => step.title == "Informe o Sacerdote") == null)
                throw new InvalidDataException("Nome dataminado de Informação do Inimigo está ausente.");
            if (celestialBalance == null || celestialBalance.steps.Find(step => step.title == "Celestial em Perigo") == null)
                throw new InvalidDataException("Submissões dataminadas de O Equilíbrio Celestial estão ausentes.");

            List<QuestMission> ea = routes.GetForClass("EA");
            if (ea.Find(m => m.name == "Mão e Coração de Ouro") == null ||
                ea.Find(m => m.name == "Matar Cobras Nagas") == null)
                throw new InvalidDataException("Correções dataminadas da rota EA/EP estão ausentes.");
            QuestMission unknownChain = common.Find(m => m.id == "COMMON|ALL|55|Quest sem Tradução");
            if (unknownChain == null || unknownChain.name != "Segredo da Família" ||
                unknownChain.steps.Find(step => step.id == "e10").title != "Salvação")
                throw new InvalidDataException("Cadeia Segredo da Família não recebeu os nomes dataminados.");
            QuestMission pillar = common.Find(m => m.id == "COMMON|ALL|50|Pilar Esquisito — cadeia completa");
            if (pillar == null || pillar.steps.Find(step => step.id == "e9").title != "Problema" ||
                pillar.steps.Find(step => step.id == "e11").title.IndexOf("Conversão do Cristal", StringComparison.Ordinal) < 0)
                throw new InvalidDataException("Submissões dataminadas de Pilar Esquisito estão ausentes.");
            foreach (QuestMission mission in routes.AllMissions)
            {
                if (mission.name.IndexOf('^') >= 0)
                    throw new InvalidDataException("Código de cor do cliente permaneceu no nome: " + mission.name);
            }

            string[] dualMonitors = { "primary", "secondary" };
            if (OverlayPlacement.ResolveMonitor("secondary", "primary", dualMonitors) != "secondary" ||
                OverlayPlacement.ResolveMonitor(null, "primary", dualMonitors) != "primary" ||
                OverlayPlacement.ResolveMonitor("secondary", "primary", new[] { "primary" }) != "primary" ||
                OverlayPlacement.ResolveMonitor("secondary", "secondary", dualMonitors) != "secondary")
                throw new InvalidDataException("Seleção do monitor fixo, automático ou desconectado incorreta.");
            Size overlaySize = new Size(570, 180);
            Rectangle leftScreen = new Rectangle(-1920, 0, 1920, 1032);
            Rectangle rightScreen = new Rectangle(1920, 0, 1920, 1032);
            Rectangle upperScreen = new Rectangle(0, -1080, 1920, 1080);
            if (OverlayPlacement.Clamp(new Point(-1700, 70), overlaySize, leftScreen) != new Point(-1700, 70) ||
                OverlayPlacement.Clamp(new Point(2100, 70), overlaySize, rightScreen) != new Point(2100, 70) ||
                OverlayPlacement.Clamp(new Point(100, -1000), overlaySize, upperScreen) != new Point(100, -1000) ||
                OverlayPlacement.Clamp(new Point(-100, 1000), overlaySize, leftScreen) != new Point(-570, 852))
                throw new InvalidDataException("Overlay foi trazido para o monitor errado ou saiu da área útil.");
            var positionState = new AppState { OverlayMonitorDeviceName = "secondary", PositionLocked = true };
            positionState.Positions["secondary"] = new SavedPosition { X = -1700, Y = 70 };
            var stateSerializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            var paths = new AppState();
            paths.Characters["TESTE|WR"] = new CharacterProgress { CharacterName = "Teste", ClassCode = "WR", CultivationPath = "GOD" };
            paths.Characters["TESTE|EP"] = new CharacterProgress { CharacterName = "Teste", ClassCode = "EP", CultivationPath = "EVIL" };
            AppState restoredPaths = stateSerializer.Deserialize<AppState>(stateSerializer.Serialize(paths));
            if (restoredPaths.Characters["TESTE|WR"].CultivationPath != "GOD" ||
                restoredPaths.Characters["TESTE|EP"].CultivationPath != "EVIL" ||
                new CharacterProgress().CultivationPath != null ||
                CultivationRouting.Normalize(" god ") != "GOD" || CultivationRouting.Normalize("inválido") != null)
                throw new InvalidDataException("Escolha de cultivo misturada entre personagens ou sem persistência.");
            string olderState = "{\"SchemaVersion\":10,\"RemovedFeature\":{\"Enabled\":true},\"Memory\":{\"AutoAdvanceEnabled\":true,\"ProfileId\":\"the_classic_144\"}}";
            AppState compatible = stateSerializer.Deserialize<AppState>(olderState);
            if (!compatible.Memory.AutoAdvanceEnabled || compatible.Characters == null ||
                stateSerializer.Serialize(compatible).Contains("RemovedFeature"))
                throw new InvalidDataException("Preferência atual ou compatibilidade de estado antigo foi perdida.");
            AppState restoredPosition = stateSerializer.Deserialize<AppState>(stateSerializer.Serialize(positionState));
            if (restoredPosition.OverlayMonitorDeviceName != "secondary" || !restoredPosition.PositionLocked ||
                restoredPosition.Positions["secondary"].X != -1700 ||
                !string.IsNullOrEmpty(new AppState().OverlayMonitorDeviceName))
                throw new InvalidDataException("Monitor escolhido não sobreviveu à gravação ou alterou o padrão antigo.");

            AppState state = new StateStore().Load();
            if (state.Characters == null || state.Positions == null || state.ClassAssociations == null)
                throw new InvalidDataException("Estado carregado sem coleções obrigatórias.");
            if (state.SchemaVersion < 11)
                throw new InvalidDataException("Estado não foi migrado para a versão atual.");
            if (state.Marker == null || state.Marker.MarkerList == null)
                throw new InvalidDataException("Calibração do marcador incompleta.");
            if (state.Memory == null || string.IsNullOrWhiteSpace(state.Memory.ProfileId))
                throw new InvalidDataException("Configuração de memória não foi inicializada.");
            if (!state.Marker.Calibrated && state.Marker.MarkerList.Y != 70)
                throw new InvalidDataException("Ponto padrão da lista de marcadores não foi migrado para uma linha selecionável.");
            foreach (CharacterProgress progress in state.Characters.Values)
                if (progress.HasOverlayMarker && progress.OverlayMarkerCount <= 0)
                    throw new InvalidDataException("Contagem de marcadores não foi migrada para " + progress.CharacterName);

            Console.WriteLine("Smoke tests aprovados para rotas, estado, clientes independentes e isolamento de evidências.");
            return 0;
        }
        catch (Exception error)
        {
            Console.Error.WriteLine(error);
            return 1;
        }
    }
}
