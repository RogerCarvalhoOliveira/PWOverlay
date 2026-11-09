using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace PWOverlayCentral
{
    internal sealed class LauncherSettings
    {
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
            Icon = PWOverlayShared.AppBranding.Icon;
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            ClientSize = new Size(560, 294);
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
                Text = "Abra o PW manualmente ou pelo PW Helper e escolha o que ativar.",
                Location = new Point(20, 48),
                Size = new Size(520, 22),
                ForeColor = Color.FromArgb(203, 213, 225)
            };

            var optionsPanel = new Panel
            {
                Location = new Point(20, 84),
                Size = new Size(520, 76),
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
                Text = "Ativar Windowed Fullscreen (janela sem bordas)",
                Location = new Point(14, 40),
                Size = new Size(490, 24),
                Checked = _settings.UseBorderless,
                ForeColor = Color.White
            };
            optionsPanel.Controls.Add(_overlayBox);
            optionsPanel.Controls.Add(_borderlessBox);

            var applyButton = MakeButton("Aplicar ao PW já aberto", 20, 174, 250, 34, Color.FromArgb(21, 128, 61));
            applyButton.Click += delegate { StartSelected(); };
            var stopButton = MakeButton("Parar Overlay e Borderless", 290, 174, 250, 34, Color.FromArgb(153, 27, 27));
            stopButton.Click += delegate { StopTools(); };

            _statusLabel = new Label
            {
                Location = new Point(20, 220),
                Size = new Size(520, 36),
                ForeColor = Color.FromArgb(148, 163, 184),
                Text = "Marque as funções e clique em Aplicar. As opções ficam salvas."
            };

            Controls.AddRange(new Control[] { title, subtitle,
                optionsPanel, applyButton, stopButton, _statusLabel });
            var uninstall = new LinkLabel { Text = "Desinstalar programa...", Location = new Point(400, 266), Size = new Size(140, 20),
                LinkColor = Color.FromArgb(125, 211, 252), Enabled = Program.HasUninstaller };
            uninstall.LinkClicked += delegate {
                try { Program.Uninstall(); Close(); }
                catch (Exception error) { ShowError(error.Message); }
            };
            Controls.Add(uninstall);
            if (!Program.HasUninstaller) Controls.Add(new Label { Text = "Versão portátil: não há desinstalador.", Location = new Point(20, 266), Size = new Size(365, 20), ForeColor = Color.FromArgb(148, 163, 184) });
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
            return new LauncherSettings();
        }

        private void SaveSettings()
        {
            _settings.UseOverlay = _overlayBox.Checked;
            _settings.UseBorderless = _borderlessBox.Checked;
            File.WriteAllText(_settingsPath, _serializer.Serialize(_settings), new UTF8Encoding(false));
        }

        private void StartSelected()
        {
            SaveSettings();
            try
            {
                if (_overlayBox.Checked) StartTool("PW Quest Overlay.exe");
                else StopTool("PW Quest Overlay");
                if (_borderlessBox.Checked) StartTool("PW Borderless.exe");
                else StopBorderlessTool();
                _statusLabel.Text = "Opções aplicadas. As ferramentas também acompanharão os próximos clientes abertos.";
                _statusLabel.ForeColor = Color.FromArgb(134, 239, 172);
            }
            catch (Exception error)
            {
                ShowError(error.Message);
            }
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

        private static void StopBorderlessTool()
        {
            Process[] processes = Process.GetProcessesByName("PW Borderless");
            if (processes.Length == 0) return;

            try
            {
                using (EventWaitHandle disableEvent = EventWaitHandle.OpenExisting(
                    @"Local\PWBorderlessDisableEvent"))
                {
                    disableEvent.Set();
                }
            }
            catch (WaitHandleCannotBeOpenedException)
            {
                // Compatibilidade com versões antigas que ainda não restauravam as janelas.
            }

            foreach (Process process in processes)
            {
                try
                {
                    if (!process.WaitForExit(2500))
                    {
                        process.Kill();
                        process.WaitForExit(2000);
                    }
                }
                catch { }
                finally { process.Dispose(); }
            }
        }

        private void StopTools()
        {
            StopTool("PW Quest Overlay");
            StopBorderlessTool();
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
        internal static bool HasUninstaller { get { return File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unins000.exe")); } }
        internal static void Uninstall()
        {
            string root = AppDomain.CurrentDomain.BaseDirectory;
            if (!HasUninstaller) throw new InvalidOperationException("Esta é a versão portátil. Feche as ferramentas e remova a pasta extraída; seus perfis ficam em %LOCALAPPDATA%\\PWQuestOverlay.");
            if (!PWOverlayShared.ToolMaintenance.Stop(root, false, delegate { }))
                throw new InvalidOperationException("Não foi possível fechar todas as ferramentas desta instalação. Feche-as e tente novamente.");
            Process.Start(new ProcessStartInfo(Path.Combine(root, "unins000.exe")) { WorkingDirectory = root, UseShellExecute = true });
        }

        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            if (args.Length == 1 && args[0] == "--uninstall")
            {
                try { Uninstall(); }
                catch (Exception error) { MessageBox.Show(error.Message, "Desinstalar PW Quest Overlay", MessageBoxButtons.OK, MessageBoxIcon.Information); }
                return;
            }
            using (var shutdown = new EventWaitHandle(false, EventResetMode.AutoReset,
                PWOverlayShared.AppShutdown.EventName("central", AppDomain.CurrentDomain.BaseDirectory)))
            using (var timer = new System.Windows.Forms.Timer { Interval = 100 })
            using (var form = new LauncherForm())
            {
                timer.Tick += delegate { if (shutdown.WaitOne(0)) form.Close(); };
                timer.Start(); Application.Run(form);
            }
        }
    }
}
