using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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
        public string maxLevel { get; set; }
        public string name { get; set; }
        public string npc { get; set; }
        public string location { get; set; }
        public int? baseXp { get; set; }
        public int? xp3 { get; set; }
        public string priority { get; set; }
        public string benefit { get; set; }
        public string instruction { get; set; }
    }

    public sealed class CharacterProgress
    {
        public string CharacterName { get; set; }
        public string ClassCode { get; set; }
        public bool ManualClass { get; set; }
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

    public sealed class AppState
    {
        public int SchemaVersion { get; set; }
        public bool PositionLocked { get; set; }
        public bool UserHidden { get; set; }
        public double Opacity { get; set; }
        public Dictionary<string, CharacterProgress> Characters { get; set; }
        public Dictionary<string, SavedPosition> Positions { get; set; }
        public Dictionary<string, string> ClassAssociations { get; set; }

        public AppState()
        {
            SchemaVersion = 1;
            Opacity = 0.92;
            Characters = new Dictionary<string, CharacterProgress>(StringComparer.OrdinalIgnoreCase);
            Positions = new Dictionary<string, SavedPosition>(StringComparer.OrdinalIgnoreCase);
            ClassAssociations = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public sealed class RouteRepository
    {
        private readonly List<QuestMission> _missions;

        private RouteRepository(List<QuestMission> missions)
        {
            _missions = missions ?? new List<QuestMission>();
        }

        public static RouteRepository Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException("Arquivo de rotas não encontrado.", path);

            var serializer = new JavaScriptSerializer { MaxJsonLength = int.MaxValue };
            RoutePayload payload = serializer.Deserialize<RoutePayload>(File.ReadAllText(path, Encoding.UTF8));
            if (payload == null || payload.missions == null || payload.missions.Count == 0)
                throw new InvalidDataException("O arquivo de rotas está vazio ou inválido.");
            return new RouteRepository(payload.missions);
        }

        public List<QuestMission> GetForClass(string classCode)
        {
            classCode = (classCode ?? string.Empty).Trim().ToUpperInvariant();
            IEnumerable<QuestMission> query;
            switch (classCode)
            {
                case "WR":
                    query = _missions.Where(m => m.routeKey == "WR");
                    break;
                case "WB":
                    query = _missions.Where(m => m.routeKey == "WBWF" && m.classScope == "WB/WF");
                    break;
                case "WF":
                    query = _missions.Where(m => m.routeKey == "WBWF" &&
                        (m.classScope == "WB/WF" || m.classScope == "WF"));
                    break;
                case "EA":
                case "EP":
                    query = _missions.Where(m => m.routeKey == "EAEP");
                    break;
                case "MC":
                    query = _missions.Where(m => m.routeKey == "MC");
                    break;
                default:
                    return new List<QuestMission>();
            }
            return query.OrderBy(m => m.order).ToList();
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
                foreach (CharacterProgress progress in state.Characters.Values)
                {
                    if (progress.MissionStates == null)
                        progress.MissionStates = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                }
                if (state.Opacity < 0.65 || state.Opacity > 1.0) state.Opacity = 0.92;
                return state;
            }
            catch (Exception error)
            {
                Log("Falha ao carregar estado: " + error.Message);
                return new AppState();
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
