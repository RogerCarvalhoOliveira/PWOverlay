using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PWOverlayCentral
{
    internal sealed class LauncherSettings
    {
        public string GamePath { get; set; }
        public bool UseOverlay { get; set; }
        public bool UseBorderless { get; set; }

        public LauncherSettings()
        {
            UseOverlay = true;
            UseBorderless = true;
        }
    }

    internal sealed class LauncherForm : Form
    {
        private readonly string _appDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private readonly string _settingsPath;
        private readonly JavaScriptSerializer _serializer = new JavaScriptSerializer();
        private readonly TextBox _gamePathBox;
        private readonly CheckBox _overlayBox;
        private readonly CheckBox _borderlessBox;
        private readonly Label _statusLabel;
        private LauncherSettings _settings;

        internal LauncherForm()
        {
            string settingsDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "PWQuestOverlay");
            Directory.CreateDirectory(settingsDirectory);
            _settingsPath = Path.Combine(settingsDirectory, "launcher.json");
            _settings = LoadSettings();

            Text = "PW Overlay Central";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(560, 356);
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.0f);

            var title = new Label
            {
                Text = "PW Overlay Central",
                Location = new Point(18, 16),
                Size = new Size(524, 30),
                Font = new Font("Segoe UI", 16.0f, FontStyle.Bold),
                ForeColor = Color.FromArgb(125, 211, 252)
            };
            var subtitle = new Label
            {
                Text = "Escolha como deseja abrir o Perfect World hoje.",
                Location = new Point(20, 48),
                Size = new Size(520, 22),
                ForeColor = Color.FromArgb(203, 213, 225)
            };

            var pathLabel = new Label { Text = "Launcher do Perfect World", Location = new Point(20, 82), AutoSize = true };
            _gamePathBox = new TextBox
            {
                Location = new Point(20, 104),
                Size = new Size(424, 26),
                ReadOnly = true,
                Text = _settings.GamePath ?? string.Empty
            };
            var browseButton = MakeButton("Procurar...", 452, 102, 88, 30, Color.FromArgb(51, 65, 85));
            browseButton.Click += delegate { ChooseGamePath(); };

            var optionsPanel = new Panel
            {
                Location = new Point(20, 144),
                Size = new Size(520, 72),
                BackColor = Color.FromArgb(30, 41, 59)
            };
            _overlayBox = new CheckBox
            {
                Text = "Ativar PW Quest Overlay",
                Location = new Point(14, 10),
                Size = new Size(480, 24),
                Checked = _settings.UseOverlay,
                ForeColor = Color.White
            };
            _borderlessBox = new CheckBox
            {
                Text = "Ativar Windowed Fullscreen e manter o PW Helper acima",
                Location = new Point(14, 38),
                Size = new Size(490, 24),
                Checked = _settings.UseBorderless,
                ForeColor = Color.White
            };
            optionsPanel.Controls.Add(_overlayBox);
            optionsPanel.Controls.Add(_borderlessBox);

            var startAllButton = MakeButton("Iniciar selecionados + PW", 20, 230, 250, 42, Color.FromArgb(3, 105, 161));
            startAllButton.Font = new Font("Segoe UI", 10.0f, FontStyle.Bold);
            startAllButton.Click += delegate { StartSelected(true); };
            var onlyGameButton = MakeButton("Abrir somente o PW", 290, 230, 250, 42, Color.FromArgb(71, 85, 105));
            onlyGameButton.Font = new Font("Segoe UI", 10.0f, FontStyle.Bold);
            onlyGameButton.Click += delegate { StartGameOnly(); };

            var applyButton = MakeButton("Aplicar ao PW já aberto", 20, 282, 250, 34, Color.FromArgb(21, 128, 61));
            applyButton.Click += delegate { StartSelected(false); };
            var stopButton = MakeButton("Parar Overlay e Borderless", 290, 282, 250, 34, Color.FromArgb(153, 27, 27));
            stopButton.Click += delegate { StopTools(); };

            _statusLabel = new Label
            {
                Location = new Point(20, 326),
                Size = new Size(520, 22),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = "As opções ficam salvas. O botão 'somente PW' encerra apenas nossas ferramentas."
            };

            Controls.AddRange(new Control[] { title, subtitle, pathLabel, _gamePathBox, browseButton,
                optionsPanel, startAllButton, onlyGameButton, applyButton, stopButton, _statusLabel });
            FormClosing += delegate { SaveSettings(); };
        }

        private static Button MakeButton(string text, int x, int y, int width, int height, Color color)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, height),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private LauncherSettings LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsPath))
                    return _serializer.Deserialize<LauncherSettings>(File.ReadAllText(_settingsPath, Encoding.UTF8)) ?? new LauncherSettings();
            }
            catch { }
            var settings = new LauncherSettings { GamePath = FindKnownLauncher() };
            return settings;
        }

        private static string FindKnownLauncher()
        {
            string[] candidates =
            {
                @"C:\The Classic Games\The Classic Games Launcher.exe",
                @"C:\The Classic Games\The Classic PW 1.4.4\launcher.exe",
                @"C:\Level Up\Perfect World\launcher\Launcher.exe"
            };
            return candidates.FirstOrDefault(File.Exists) ?? string.Empty;
        }

        private void SaveSettings()
        {
            _settings.GamePath = _gamePathBox.Text.Trim();
            _settings.UseOverlay = _overlayBox.Checked;
            _settings.UseBorderless = _borderlessBox.Checked;
            File.WriteAllText(_settingsPath, _serializer.Serialize(_settings), new UTF8Encoding(false));
        }

        private bool ChooseGamePath()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Selecione o launcher do Perfect World";
                dialog.Filter = "Aplicativo (*.exe)|*.exe";
                dialog.CheckFileExists = true;
                if (dialog.ShowDialog() != DialogResult.OK) return false;
                _gamePathBox.Text = dialog.FileName;
                SaveSettings();
                return true;
            }
        }

        private void StartSelected(bool launchGame)
        {
            SaveSettings();
            try
            {
                if (_overlayBox.Checked) StartTool("PW Quest Overlay.exe");
                else StopTool("PW Quest Overlay");
                if (_borderlessBox.Checked) StartTool("PW Borderless.exe");
                else StopTool("PW Borderless");
                if (launchGame && !LaunchGame()) return;
                _statusLabel.Text = launchGame ? "Perfect World iniciado com as opções selecionadas." : "Opções aplicadas aos clientes já abertos.";
                _statusLabel.ForeColor = Color.FromArgb(134, 239, 172);
            }
            catch (Exception error)
            {
                ShowError(error.Message);
            }
        }

        private void StartGameOnly()
        {
            try
            {
                StopTool("PW Quest Overlay");
                StopTool("PW Borderless");
                if (!LaunchGame()) return;
                _statusLabel.Text = "Perfect World iniciado sem Overlay e sem Windowed Fullscreen automático.";
                _statusLabel.ForeColor = Color.FromArgb(253, 224, 71);
            }
            catch (Exception error)
            {
                ShowError(error.Message);
            }
        }

        private bool LaunchGame()
        {
            string path = _gamePathBox.Text.Trim();
            if (!File.Exists(path))
            {
                if (!ChooseGamePath()) return false;
                path = _gamePathBox.Text.Trim();
            }
            Process.Start(new ProcessStartInfo(path)
            {
                WorkingDirectory = Path.GetDirectoryName(path),
                UseShellExecute = true
            });
            return true;
        }

        private void StartTool(string fileName)
        {
            string processName = Path.GetFileNameWithoutExtension(fileName);
            if (Process.GetProcessesByName(processName).Length > 0) return;
            string path = Path.Combine(_appDirectory, fileName);
            if (!File.Exists(path)) throw new FileNotFoundException("Ferramenta não encontrada: " + fileName, path);
            Process.Start(new ProcessStartInfo(path) { WorkingDirectory = _appDirectory, UseShellExecute = true });
        }

        private static void StopTool(string processName)
        {
            foreach (Process process in Process.GetProcessesByName(processName))
            {
                try { process.Kill(); process.WaitForExit(2000); }
                catch { }
                finally { process.Dispose(); }
            }
        }

        private void StopTools()
        {
            StopTool("PW Quest Overlay");
            StopTool("PW Borderless");
            _statusLabel.Text = "Overlay e PW Borderless encerrados. Os clientes do jogo continuam abertos.";
            _statusLabel.ForeColor = Color.FromArgb(253, 224, 71);
        }

        private void ShowError(string message)
        {
            _statusLabel.Text = message;
            _statusLabel.ForeColor = Color.FromArgb(252, 165, 165);
            MessageBox.Show(message, "PW Overlay Central", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new LauncherForm());
        }
    }
}
