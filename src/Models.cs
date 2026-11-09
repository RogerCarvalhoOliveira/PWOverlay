using System;
using System.Collections.Generic;
using System.IO;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;

namespace PWQuestOverlay
{
    public sealed class RoutePayload
    {
        public int schemaVersion { get; set; }
        public string generatedFrom { get; set; }
        public List<QuestMission> missions { get; set; }
    }

    public sealed class QuestMission
    {
        public string id { get; set; }
        public string routeKey { get; set; }
        public int order { get; set; }
        public string classScope { get; set; }
        public int minLevel { get; set; }
        public int expectedLevel { get; set; }
        public string maxLevel { get; set; }
        public string name { get; set; }
        public string npc { get; set; }
        public string location { get; set; }
        public int? baseXp { get; set; }
        public int? xp3 { get; set; }
        public string priority { get; set; }
        public string benefit { get; set; }
        public string instruction { get; set; }
        public string cultivationPath { get; set; }
        public bool cultivationChoice { get; set; }
        public bool repeatable { get; set; }
        public string availabilityNote { get; set; }
        public List<QuestStep> steps { get; set; }
    }

    public sealed class QuestStep
    {
        public string id { get; set; }
        public string title { get; set; }
        public string action { get; set; }
        public int? markerX { get; set; }
        public int? markerY { get; set; }
        public int routeOrder { get; set; }
        public uint memoryTaskId { get; set; }
        public uint[] memoryItemIds { get; set; }
    }

    public sealed class CharacterProgress
    {
        public string CharacterName { get; set; }
        public string ClassCode { get; set; }
        public string CultivationPath { get; set; }
        public bool ManualClass { get; set; }
        public bool HasOverlayMarker { get; set; }
        public int OverlayMarkerCount { get; set; }
        public Dictionary<string, string> MissionStates { get; set; }

        public CharacterProgress()
        {
            MissionStates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public sealed class SavedPosition
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    internal static class OverlayPlacement
    {
        internal static string ResolveMonitor(
            string preferredMonitor, string gameMonitor, IEnumerable<string> availableMonitors)
        {
            if (!string.IsNullOrWhiteSpace(preferredMonitor) && availableMonitors.Any(name =>
                string.Equals(name, preferredMonitor, StringComparison.OrdinalIgnoreCase)))
                return preferredMonitor;
            return gameMonitor;
        }

        internal static Point Clamp(Point location, Size size, Rectangle workingArea)
        {
            return new Point(
                Math.Max(workingArea.Left, Math.Min(location.X, workingArea.Right - size.Width)),
                Math.Max(workingArea.Top, Math.Min(location.Y, workingArea.Bottom - size.Height)));
        }
    }

    public sealed class MarkerPoint
    {
        public int X { get; set; }
        public int Y { get; set; }

        public MarkerPoint() { }
        public MarkerPoint(int x, int y) { X = x; Y = y; }
    }

    public sealed class MarkerSettings
    {
        public string DeleteMode { get; set; }
        public bool Calibrated { get; set; }
        public MarkerPoint TrackerButton { get; set; }
        public MarkerPoint DeleteAll { get; set; }
        public MarkerPoint DeleteSelected { get; set; }
        public MarkerPoint MarkerList { get; set; }
        public MarkerPoint DeleteConfirmYes { get; set; }
        public MarkerPoint CoordinateInput { get; set; }
        public MarkerPoint TrackCoordinates { get; set; }
        public MarkerPoint NameInput { get; set; }
        public MarkerPoint NameOk { get; set; }
        public MarkerPoint PanelClose { get; set; }

        public static MarkerSettings CreateDefault()
        {
            return new MarkerSettings
            {
                DeleteMode = "selected",
                TrackerButton = new MarkerPoint(1760, 158),
                DeleteAll = new MarkerPoint(291, 382),
                DeleteSelected = new MarkerPoint(260, 382),
                MarkerList = new MarkerPoint(230, 70),
                DeleteConfirmYes = new MarkerPoint(970, 560),
                CoordinateInput = new MarkerPoint(218, 41),
                TrackCoordinates = new MarkerPoint(291, 41),
                NameInput = new MarkerPoint(953, 536),
                NameOk = new MarkerPoint(970, 560),
                PanelClose = new MarkerPoint(294, 16)
            };
        }
    }

    public sealed class QuestMemorySettings
    {
        public bool AutoAdvanceEnabled { get; set; }
        public string ProfileId { get; set; }

        public static QuestMemorySettings CreateDefault()
        {
            return new QuestMemorySettings
            {
                ProfileId = "the_classic_144"
            };
        }
    }

    public sealed class AppState
    {
        public int SchemaVersion { get; set; }
        public bool PositionLocked { get; set; }
        public string OverlayMonitorDeviceName { get; set; }
        public bool UserHidden { get; set; }
        public bool CompactMode { get; set; }
        public double Opacity { get; set; }
        public Dictionary<string, CharacterProgress> Characters { get; set; }
        public Dictionary<string, SavedPosition> Positions { get; set; }
        public Dictionary<string, string> ClassAssociations { get; set; }
        public MarkerSettings Marker { get; set; }
        public QuestMemorySettings Memory { get; set; }

        public AppState()
        {
            SchemaVersion = 11;
            Opacity = 0.82;
            Characters = new Dictionary<string, CharacterProgress>(StringComparer.OrdinalIgnoreCase);
            Positions = new Dictionary<string, SavedPosition>(StringComparer.OrdinalIgnoreCase);
            ClassAssociations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            Marker = MarkerSettings.CreateDefault();
            Memory = QuestMemorySettings.CreateDefault();
        }
    }

    public sealed class RouteRepository
    {
        private readonly List<QuestMission> _missions;
        private readonly RoutePayload _payload;

        private RouteRepository(RoutePayload payload)
        {
            _payload = payload;
            _missions = payload.missions ?? new List<QuestMission>();
        }

        public List<QuestMission> AllMissions { get { return _missions; } }

        public static RouteRepository Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Arquivo de rotas não encontrado.", path);

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            RoutePayload payload = serializer.Deserialize<RoutePayload>(File.ReadAllText(path, Encoding.UTF8));
            if (payload == null || payload.missions == null || payload.missions.Count == 0)
                throw new InvalidDataException("O arquivo de rotas está vazio ou inválido.");
            foreach (QuestMission mission in payload.missions)
            {
                if (mission.expectedLevel <= 0) mission.expectedLevel = mission.minLevel;
                if (mission.steps == null || mission.steps.Count == 0)
                {
                    mission.steps = new List<QuestStep>
                    {
                        new QuestStep { id = "e1", action = mission.instruction }
                    };
                }
                for (int index = 0; index < mission.steps.Count; index++)
                {
                    QuestStep step = mission.steps[index];
                    if (string.IsNullOrWhiteSpace(step.id)) step.id = "e" + (index + 1);
                    if (string.IsNullOrWhiteSpace(step.action)) step.action = mission.instruction;
                    if (string.IsNullOrWhiteSpace(step.title)) step.title = StepTitleFormatter.FromAction(step.action);
                    if (step.routeOrder <= 0) step.routeOrder = mission.order * 100 + index;
                }
            }
            return new RouteRepository(payload);
        }

        public void Save(string path)
        {
            _payload.schemaVersion = Math.Max(8, _payload.schemaVersion);
            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            string temporary = path + ".tmp";
            File.WriteAllText(temporary, serializer.Serialize(_payload), new UTF8Encoding(false));
            File.Copy(temporary, path, true);
            File.Delete(temporary);
        }

        public List<QuestMission> GetForClass(string classCode)
        {
            return GetForClass(classCode, null);
        }

        public List<QuestMission> GetForClass(string classCode, string cultivationPath)
        {
            classCode = (classCode ?? string.Empty).Trim().ToUpperInvariant();
            IEnumerable<QuestMission> race;
            switch (classCode)
            {
                case "WR":
                case "MG":
                    race = _missions.Where(m => m.routeKey == "WR");
                    break;
                case "WB":
                    race = _missions.Where(m => m.routeKey == "WBWF" && m.classScope == "WB/WF");
                    break;
                case "WF":
                    race = _missions.Where(m => m.routeKey == "WBWF" &&
                        (m.classScope == "WB/WF" || m.classScope == "WF"));
                    break;
                case "EA":
                case "EP":
                    race = _missions.Where(m => m.routeKey == "EAEP");
                    break;
                case "MC":
                case "ES":
                    race = _missions.Where(m => m.routeKey == "MC");
                    break;
                case "SK":
                case "MS":
                    race = _missions.Where(m => m.routeKey == "SKMS" &&
                        (m.classScope == "SK/MS" || m.classScope == classCode));
                    break;
                default:
                    return new List<QuestMission>();
            }
            IEnumerable<QuestMission> common = _missions.Where(m => m.routeKey == "COMMON" && m.classScope == "ALL");
            return race.Concat(common).Where(m => CultivationRouting.IsVisible(m, cultivationPath))
                .OrderBy(m => m.order).ToList();
        }
    }

    public static class CultivationRouting
    {
        public static string Normalize(string value)
        {
            value = (value ?? string.Empty).Trim().ToUpperInvariant();
            return value == "GOD" || value == "EVIL" ? value : null;
        }

        public static bool IsVisible(QuestMission mission, string selectedPath)
        {
            string path = Normalize(selectedPath);
            if (path == null)
                return mission.cultivationChoice || (mission.expectedLevel < 89 &&
                    string.IsNullOrWhiteSpace(mission.cultivationPath));
            return string.IsNullOrWhiteSpace(mission.cultivationPath) ||
                string.Equals(mission.cultivationPath, path, StringComparison.OrdinalIgnoreCase);
        }
    }

    internal static class StepTitleFormatter
    {
        private static readonly Regex Coordinate = new Regex(
            @"\s*\(\s*\d+\s*,\s*\d+(?:\s*,?\s*[A-Z]\d+|\s+\d+)?\s*\)",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        internal static string FromAction(string action)
        {
            string value = Coordinate.Replace((action ?? string.Empty).Trim(), string.Empty).Trim();
            if (value.StartsWith("Relatório de ", StringComparison.OrdinalIgnoreCase))
                value = "Reportar ao " + value.Substring("Relatório de ".Length);
            else if (value.StartsWith("NPC Final: ", StringComparison.OrdinalIgnoreCase))
                value = "Entregar a " + value.Substring("NPC Final: ".Length);
            else if (value.StartsWith("Fale novamente com ", StringComparison.OrdinalIgnoreCase))
                value = "Falar novamente com " + value.Substring("Fale novamente com ".Length);
            else if (value.StartsWith("Fale com ", StringComparison.OrdinalIgnoreCase))
                value = "Falar com " + value.Substring("Fale com ".Length);
            else if (value.StartsWith("Aguarde ", StringComparison.OrdinalIgnoreCase))
                value = "Aguardar " + value.Substring("Aguarde ".Length);
            else if (value.StartsWith("Espere ", StringComparison.OrdinalIgnoreCase))
                value = "Aguardar " + value.Substring("Espere ".Length);
            else if (value.StartsWith("Pegar ", StringComparison.OrdinalIgnoreCase))
                value = "Coletar " + value.Substring("Pegar ".Length);
            else if (value.StartsWith("Mate ", StringComparison.OrdinalIgnoreCase))
                value = "Matar " + value.Substring("Mate ".Length);
            else if (value.StartsWith("Receba ", StringComparison.OrdinalIgnoreCase))
                value = "Receber " + value.Substring("Receba ".Length);
            else if (value.StartsWith("Confirme ", StringComparison.OrdinalIgnoreCase))
                value = "Confirmar " + value.Substring("Confirme ".Length);
            else if (value.StartsWith("Complete ", StringComparison.OrdinalIgnoreCase))
                value = "Completar " + value.Substring("Complete ".Length);

            if (string.IsNullOrWhiteSpace(value)) return "Próxima ação";
            if (value.Length <= 72) return value;
            int cut = value.LastIndexOf(' ', 72);
            return value.Substring(0, cut > 40 ? cut : 72).TrimEnd() + "…";
        }
    }

    public sealed class StateStore
    {
        private readonly string _directory;
        private readonly string _statePath;
        private readonly string _logPath;
        private readonly JavaScriptSerializer _serializer;

        public string StatePath { get { return _statePath; } }
        public string LogPath { get { return _logPath; } }

        public StateStore()
        {
            _directory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PWQuestOverlay");
            _statePath = Path.Combine(_directory, "state.json");
            _logPath = Path.Combine(_directory, "overlay.log");
            _serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
        }

        public AppState Load()
        {
            try
            {
                Directory.CreateDirectory(_directory);
                if (!File.Exists(_statePath)) return new AppState();
                AppState state = _serializer.Deserialize<AppState>(File.ReadAllText(_statePath, Encoding.UTF8));
                if (state == null) return new AppState();
                if (state.Characters == null)
                    state.Characters = new Dictionary<string, CharacterProgress>(StringComparer.OrdinalIgnoreCase);
                if (state.Positions == null)
                    state.Positions = new Dictionary<string, SavedPosition>(StringComparer.OrdinalIgnoreCase);
                if (state.ClassAssociations == null)
                    state.ClassAssociations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (state.Marker == null) state.Marker = MarkerSettings.CreateDefault();
                if (state.Memory == null) state.Memory = QuestMemorySettings.CreateDefault();
                if (state.SchemaVersion < 2)
                {
                    state.Marker.DeleteMode = "selected";
                    state.SchemaVersion = 2;
                }
                if (state.SchemaVersion < 3)
                {
                    MarkerSettings defaults = MarkerSettings.CreateDefault();
                    state.Marker.MarkerList = defaults.MarkerList;
                    state.Marker.DeleteConfirmYes = defaults.DeleteConfirmYes;
                    state.SchemaVersion = 3;
                }
                if (state.SchemaVersion < 4)
                {
                    if (Math.Abs(state.Opacity - 0.92) < 0.001) state.Opacity = 0.82;
                    state.CompactMode = false;
                    state.SchemaVersion = 4;
                }
                if (state.SchemaVersion < 5)
                {
                    if (state.Marker.MarkerList != null && state.Marker.MarkerList.X == 230 && state.Marker.MarkerList.Y == 200)
                        state.Marker.MarkerList = new MarkerPoint(230, 70);
                    state.SchemaVersion = 5;
                }
                if (state.SchemaVersion < 6)
                {
                    foreach (CharacterProgress progress in state.Characters.Values)
                        if (progress.HasOverlayMarker && progress.OverlayMarkerCount <= 0)
                            progress.OverlayMarkerCount = 1;
                    state.SchemaVersion = 6;
                }
                if (state.SchemaVersion < 10)
                {
                    state.Memory = QuestMemorySettings.CreateDefault();
                    state.SchemaVersion = 10;
                }
                NormalizeMarkerSettings(state.Marker);
                foreach (CharacterProgress progress in state.Characters.Values)
                {
                    progress.CultivationPath = CultivationRouting.Normalize(progress.CultivationPath);
                    if (progress.MissionStates == null)
                        progress.MissionStates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                RestoreLegacyMarkerFlags(state);
                foreach (CharacterProgress progress in state.Characters.Values)
                    if (progress.HasOverlayMarker && progress.OverlayMarkerCount <= 0)
                        progress.OverlayMarkerCount = 1;
                if (state.Opacity < 0.60 || state.Opacity > 1.0) state.Opacity = 0.82;
                state.SchemaVersion = Math.Max(11, state.SchemaVersion);
                return state;
            }
            catch (Exception error)
            {
                Log("Falha ao carregar estado: " + error.Message);
                return new AppState();
            }
        }

        public AppState LoadFrom(string path)
        {
            AppState state = _serializer.Deserialize<AppState>(File.ReadAllText(path, Encoding.UTF8));
            if (state == null) throw new InvalidDataException("Backup de progresso inválido.");
            if (state.Characters == null) state.Characters = new Dictionary<string, CharacterProgress>();
            if (state.Positions == null) state.Positions = new Dictionary<string, SavedPosition>();
            if (state.ClassAssociations == null) state.ClassAssociations = new Dictionary<string, string>();
            if (state.Marker == null) state.Marker = MarkerSettings.CreateDefault();
            if (state.Memory == null) state.Memory = QuestMemorySettings.CreateDefault();
            if (state.SchemaVersion < 2)
            {
                state.Marker.DeleteMode = "selected";
                state.SchemaVersion = 2;
            }
            if (state.SchemaVersion < 3)
            {
                MarkerSettings defaults = MarkerSettings.CreateDefault();
                state.Marker.MarkerList = defaults.MarkerList;
                state.Marker.DeleteConfirmYes = defaults.DeleteConfirmYes;
                state.SchemaVersion = 3;
            }
            if (state.SchemaVersion < 4)
            {
                if (Math.Abs(state.Opacity - 0.92) < 0.001) state.Opacity = 0.82;
                state.CompactMode = false;
                state.SchemaVersion = 4;
            }
            if (state.SchemaVersion < 5)
            {
                if (state.Marker.MarkerList != null && state.Marker.MarkerList.X == 230 && state.Marker.MarkerList.Y == 200)
                    state.Marker.MarkerList = new MarkerPoint(230, 70);
                state.SchemaVersion = 5;
            }
            if (state.SchemaVersion < 6)
            {
                foreach (CharacterProgress progress in state.Characters.Values)
                    if (progress.HasOverlayMarker && progress.OverlayMarkerCount <= 0)
                        progress.OverlayMarkerCount = 1;
                state.SchemaVersion = 6;
            }
            if (state.SchemaVersion < 10)
            {
                state.Memory = QuestMemorySettings.CreateDefault();
                state.SchemaVersion = 10;
            }
            NormalizeMarkerSettings(state.Marker);
            foreach (CharacterProgress progress in state.Characters.Values)
            {
                progress.CultivationPath = CultivationRouting.Normalize(progress.CultivationPath);
                if (progress.MissionStates == null) progress.MissionStates = new Dictionary<string, string>();
            }
            state.SchemaVersion = Math.Max(11, state.SchemaVersion);
            return state;
        }

        public void Export(AppState state, string path)
        {
            File.WriteAllText(path, _serializer.Serialize(state), new UTF8Encoding(false));
        }

        private static void NormalizeMarkerSettings(MarkerSettings marker)
        {
            MarkerSettings defaults = MarkerSettings.CreateDefault();
            if (string.IsNullOrWhiteSpace(marker.DeleteMode)) marker.DeleteMode = defaults.DeleteMode;
            if (marker.TrackerButton == null) marker.TrackerButton = defaults.TrackerButton;
            if (marker.DeleteAll == null) marker.DeleteAll = defaults.DeleteAll;
            if (marker.DeleteSelected == null) marker.DeleteSelected = defaults.DeleteSelected;
            if (marker.MarkerList == null) marker.MarkerList = defaults.MarkerList;
            if (marker.DeleteConfirmYes == null) marker.DeleteConfirmYes = defaults.DeleteConfirmYes;
            if (marker.CoordinateInput == null) marker.CoordinateInput = defaults.CoordinateInput;
            if (marker.TrackCoordinates == null) marker.TrackCoordinates = defaults.TrackCoordinates;
            if (marker.NameInput == null) marker.NameInput = defaults.NameInput;
            if (marker.NameOk == null) marker.NameOk = defaults.NameOk;
            if (marker.PanelClose == null) marker.PanelClose = defaults.PanelClose;
        }

        private void RestoreLegacyMarkerFlags(AppState state)
        {
            if (!File.Exists(_logPath)) return;
            string log = File.ReadAllText(_logPath, Encoding.UTF8);
            foreach (CharacterProgress progress in state.Characters.Values)
            {
                if (!progress.HasOverlayMarker && !string.IsNullOrWhiteSpace(progress.CharacterName) &&
                    log.IndexOf(" " + progress.CharacterName + " marcou ", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    progress.HasOverlayMarker = true;
                }
            }
        }

        public void Save(AppState state)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                string temporary = _statePath + ".tmp";
                File.WriteAllText(temporary, _serializer.Serialize(state), new UTF8Encoding(false));
                File.Copy(temporary, _statePath, true);
                File.Delete(temporary);
            }
            catch (Exception error)
            {
                Log("Falha ao salvar estado: " + error.Message);
            }
        }

        public void Log(string message)
        {
            try
            {
                Directory.CreateDirectory(_directory);
                File.AppendAllText(_logPath,
                    DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }
    }
}
