using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using PWQuestOverlay.Memory;

namespace PWQuestOverlay
{
    internal sealed class OverlayForm : Form
    {
        private const int HotkeyDone = 1001;
        private const int HotkeyPrevious = 1002;
        private const int HotkeySkip = 1003;
        private const int HotkeyMarker = 1004;
        private const int HotkeyMemoryAutoAdvance = 1007;
        private static readonly Size FullClientSize = new Size(570, 180);
        private static readonly Size CompactClientSize = new Size(470, 122);

        private readonly RouteRepository _routes;
        private readonly StateStore _store;
        private readonly AppState _state;
        private readonly string _routePath;
        private readonly string _workbookPath;
        private readonly Timer _timer;
        private readonly Panel _topBar;
        private readonly Label _profileLabel;
        private readonly Label _questLabel;
        private readonly Label _npcLabel;
        private readonly Label _stepTypeLabel;
        private readonly Label _locationLabel;
        private readonly Label _statusLabel;
        private readonly Button _doneButton;
        private readonly Button _previousButton;
        private readonly Button _skipButton;
        private readonly Button _markerButton;
        private readonly Button _profilesButton;
        private readonly Button _stagesButton;
        private readonly Button _lockButton;
        private readonly Button _compactButton;
        private readonly Button _hideButton;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolStripMenuItem _showMenuItem;
        private readonly ToolStripMenuItem _lockMenuItem;
        private readonly ToolStripMenuItem _followGameMonitorItem;
        private readonly ToolStripMenuItem _compactMenuItem;
        private readonly ToolStripMenuItem _classMenuItem;
        private readonly ToolStripMenuItem _markerHotkeyItem;
        private readonly ToolStripMenuItem _memoryAutoAdvanceItem;
        private readonly Dictionary<IntPtr, DateTime> _titleRefresh;
        private readonly TaskCatalog _taskCatalog;
        private readonly QuestMemoryPolling _memoryMonitor;
        private readonly QuestMemoryPolling _identityMonitor126;
        private readonly Dictionary<uint, uint> _classic126Tasks;
        private readonly ElevatedIdentityReader _elevatedIdentity = new ElevatedIdentityReader();
        private readonly System.Threading.EventWaitHandle _shutdown = new System.Threading.EventWaitHandle(false,
            System.Threading.EventResetMode.AutoReset, PWOverlayShared.AppShutdown.EventName("overlay", AppDomain.CurrentDomain.BaseDirectory));

        private IntPtr _activeGame;
        private IntPtr _lastGameWindow;
        private DateTime _helperNeutralUntil;
        private DateTime _nextRaiseAt;
        private DateTime _nextMemoryPollAt;
        private DateTime _nextWindowScanAt;
        private string _characterKey;
        private string _windowTitleKey;
        private string _characterName;
        private string _classCode;
        private ClientServer _characterServer;
        private CharacterProgress _progress;
        private List<QuestMission> _currentRoute;
        private List<QuestMission> _classMissions = new List<QuestMission>();
        private QuestMemorySnapshot _routeSnapshot;
        private QuestMemorySnapshot _savedImportedHistory;
        private readonly Dictionary<string, Tuple<string, QuestMemorySnapshot>> _importedHistories =
            new Dictionary<string, Tuple<string, QuestMemorySnapshot>>();
        private RouteStage _selectedStage;
        private ToolStripMenuItem _reducedRouteItem;
        private QuestMission _currentMission;
        private QuestStep _currentStep;
        private int _currentStepIndex;
        private List<RouteStage> _orderedStages;
        private Screen _currentScreen;
        private string _currentMonitorKey;
        private bool _dragging;
        private Point _dragCursorStart;
        private Point _dragFormStart;
        private bool _allowExit;
        private bool _markerBusy;
        private bool _markerCalibrationOpen;
        private string _memoryDiagnosticSignature;
        private bool _cultivationDialogOpen;
        private bool _profileDialogOpen;
        private bool _historyImportBusy;
        private readonly ClientBindings _clientBindings = new ClientBindings();
        private List<ClientWindowInfo> _clientWindows = new List<ClientWindowInfo>();
        private readonly HashSet<string> _cultivationPrompted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private sealed class RouteStage
        {
            internal QuestMission Mission;
            internal QuestStep Step;
            internal int StepIndex;
        }

        internal OverlayForm(RouteRepository routes, StateStore store, AppState state,
            string routePath, string workbookPath)
        {
            _routes = routes;
            _store = store;
            _state = state;
            _routePath = routePath;
            _workbookPath = workbookPath;
            _currentRoute = new List<QuestMission>();
            _orderedStages = new List<RouteStage>();
            _titleRefresh = new Dictionary<IntPtr, DateTime>();
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _taskCatalog = TaskCatalog.Load(Path.Combine(
                    baseDirectory, "task_catalog_144.tsv"));
                _memoryMonitor = new QuestMemoryPolling(
                    Path.Combine(baseDirectory, "memory_offsets.json"),
                    _state.Memory.ProfileId,
                    _store.Log);
            }
            catch (Exception error)
            {
                _store.Log("Leitura de memória indisponível: " + error.Message);
            }
            try
            {
                _classic126Tasks = QuestProgression.LoadClassic126Tasks(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "task_history_126.tsv"));
                _identityMonitor126 = new QuestMemoryPolling(
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "memory_offsets.json"),
                    ElevatedIdentityReader.ProfileId, _store.Log);
            }
            catch (Exception error)
            {
                _store.Log("Identificação do 1.2.6 indisponível: " + error.Message);
            }

            Text = "PW Quest Overlay";
            Icon = PWOverlayShared.AppBranding.Icon;
            Name = "PWQuestOverlay";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            ClientSize = FullClientSize;
            MinimumSize = ClientSize;
            MaximumSize = ClientSize;
            BackColor = Color.FromArgb(10, 18, 31);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.0f, FontStyle.Regular, GraphicsUnit.Point);
            TopMost = true;
            Opacity = _state.Opacity;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.Dpi;

            _topBar = new Panel
            {
                Location = new Point(0, 0),
                Size = new Size(ClientSize.Width, 30),
                BackColor = Color.FromArgb(24, 59, 91),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };

            _profileLabel = new Label
            {
                Location = new Point(10, 6),
                Size = new Size(245, 19),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Text = "PW Quest Overlay — aguardando cliente",
                AutoEllipsis = true
            };

            _profilesButton = MakeTopButton("Perfis", 258, 56);
            _stagesButton = MakeTopButton("Etapas", 318, 62);
            _lockButton = MakeTopButton("Mover", 384, 78);
            _compactButton = MakeTopButton("−", 498, 30);
            _compactButton.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);
            _hideButton = MakeTopButton("×", 532, 30);
            _hideButton.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);

            _questLabel = new Label
            {
                Location = new Point(12, 36),
                Size = new Size(546, 28),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 12.0f, FontStyle.Bold),
                Text = "Abra ou selecione um cliente do Perfect World.",
                AutoEllipsis = true
            };

            _npcLabel = new Label
            {
                Location = new Point(88, 67),
                Size = new Size(470, 38),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Regular),
                AutoEllipsis = true
            };

            _stepTypeLabel = new Label
            {
                Location = new Point(12, 69),
                Size = new Size(68, 22),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(71, 85, 105)
            };

            _locationLabel = new Label
            {
                Location = new Point(12, 108),
                Size = new Size(546, 16),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoEllipsis = true
            };

            _previousButton = MakeActionButton("Voltar", 12, 137, 72, Color.FromArgb(51, 65, 85));
            _doneButton = MakeActionButton("Etapa feita", 90, 137, 102, Color.FromArgb(21, 128, 61));
            _skipButton = MakeActionButton("Pular etapa", 198, 137, 92, Color.FromArgb(154, 52, 18));
            _markerButton = MakeActionButton("Marcar", 296, 137, 82, Color.FromArgb(29, 78, 216));

            _statusLabel = new Label
            {
                Location = new Point(384, 142),
                Size = new Size(174, 22),
                ForeColor = Color.FromArgb(203, 213, 225),
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            };

            _topBar.Controls.Add(_profileLabel);
            _topBar.Controls.Add(_profilesButton);
            _topBar.Controls.Add(_stagesButton);
            _topBar.Controls.Add(_lockButton);
            _topBar.Controls.Add(_compactButton);
            _topBar.Controls.Add(_hideButton);
            Controls.Add(_topBar);
            Controls.Add(_questLabel);
            Controls.Add(_stepTypeLabel);
            Controls.Add(_npcLabel);
            Controls.Add(_locationLabel);
            Controls.Add(_previousButton);
            Controls.Add(_doneButton);
            Controls.Add(_skipButton);
            Controls.Add(_markerButton);
            Controls.Add(_statusLabel);

            AttachDragHandlers(_topBar);
            AttachDragHandlers(_profileLabel);
            _profilesButton.Click += delegate { OpenProfileManager(); };
            _stagesButton.Click += delegate { OpenStageSelector(); };
            _lockButton.Click += delegate { ToggleLock(); };
            _compactButton.Click += delegate { SetCompactMode(!_state.CompactMode, true); };
            _hideButton.Click += delegate { SetUserHidden(true); };
            _doneButton.Click += delegate { CompleteCurrent(); };
            _previousButton.Click += delegate { GoBack(); };
            _skipButton.Click += delegate { SkipCurrent(); };
            _markerButton.Click += delegate { MarkCurrentLocation(); };

            _trayMenu = new ContextMenuStrip();
            _showMenuItem = new ToolStripMenuItem("Mostrar overlay");
            _showMenuItem.Click += delegate { SetUserHidden(false); };
            _trayMenu.Items.Add(_showMenuItem);
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Concluir etapa", null, delegate { CompleteCurrent(); });
            _trayMenu.Items.Add("Voltar uma etapa", null, delegate { GoBack(); });
            _trayMenu.Items.Add("Pular etapa", null, delegate { SkipCurrent(); });
            _trayMenu.Items.Add(new ToolStripSeparator());

            _trayMenu.Items.Add("Gerenciar perfis...", null, delegate { OpenProfileManager(); });
            _trayMenu.Items.Add("Permitir leitura do PW elevado...", null, async delegate { await RequestElevatedIdentity(); });
            _trayMenu.Items.Add("Encerrar leitor elevado", null, delegate
            {
                PWOverlayShared.AppShutdown.Signal("identity", AppDomain.CurrentDomain.BaseDirectory);
            });
            _trayMenu.Items.Add("Associações por título (compatibilidade)...", null, delegate { OpenTitleAssociations(); });
            _trayMenu.Items.Add("Selecionar missão/etapa...", null, delegate { OpenStageSelector(); });
            _trayMenu.Items.Add("Escolher/alterar cultivo God ou Evil...", null, delegate { ChooseCultivation(); });
            _trayMenu.Items.Add("Editar rotas...", null, delegate { OpenRouteEditor(); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Exportar backup de perfis...", null, delegate { ExportBackup(); });
            _trayMenu.Items.Add("Restaurar backup de perfis...", null, delegate { RestoreBackup(); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Calibrar marcador...", null, delegate { OpenMarkerCalibration(); });
            _reducedRouteItem = new ToolStripMenuItem("Rota reduzida (XP ×3)")
            {
                CheckOnClick = true,
                Checked = _state.OptimizeQuestXp
            };
            _reducedRouteItem.Click += delegate
            {
                _state.OptimizeQuestXp = _reducedRouteItem.Checked;
                _store.Save(_state);
                RefreshMissionDisplay();
            };
            _trayMenu.Items.Add(_reducedRouteItem);
            ToolStripMenuItem memoryMenu = new ToolStripMenuItem("Estado das missões por memória");
            _memoryAutoAdvanceItem = new ToolStripMenuItem(
                "Avançar etapas em andamento automaticamente")
            {
                CheckOnClick = true,
                Checked = _state.Memory.AutoAdvanceEnabled,
                Enabled = _memoryMonitor != null && _taskCatalog != null
            };
            _memoryAutoAdvanceItem.Click += delegate
            {
                SetMemoryAutoAdvance(_memoryAutoAdvanceItem.Checked, true);
            };
            memoryMenu.DropDownItems.Add(_memoryAutoAdvanceItem);
            memoryMenu.DropDownItems.Add(new ToolStripSeparator());
            memoryMenu.DropDownItems.Add("Importar missões concluídas...", null, async delegate { await ImportQuestHistory(); });
            _trayMenu.Items.Add(memoryMenu);
            _trayMenu.Items.Add("Abrir guia de configuração...", null, delegate { OpenConfigurationGuide(); });
            _markerHotkeyItem = new ToolStripMenuItem("Atalho do marcador: verificando...") { Enabled = false };
            _trayMenu.Items.Add(_markerHotkeyItem);

            ToolStripMenuItem cleanupMenu = new ToolStripMenuItem("Ao criar marcador");
            ToolStripMenuItem deleteAllItem = new ToolStripMenuItem("Apagar todos") { CheckOnClick = true };
            ToolStripMenuItem deleteSelectedItem = new ToolStripMenuItem("Substituir somente PWOverlay (recomendado)") { CheckOnClick = true };
            Action refreshCleanupChecks = delegate
            {
                deleteAllItem.Checked = !string.Equals(_state.Marker.DeleteMode, "selected", StringComparison.OrdinalIgnoreCase);
                deleteSelectedItem.Checked = !deleteAllItem.Checked;
            };
            deleteAllItem.Click += delegate { _state.Marker.DeleteMode = "all"; refreshCleanupChecks(); _store.Save(_state); };
            deleteSelectedItem.Click += delegate { _state.Marker.DeleteMode = "selected"; refreshCleanupChecks(); _store.Save(_state); };
            refreshCleanupChecks();
            cleanupMenu.DropDownItems.Add(deleteAllItem);
            cleanupMenu.DropDownItems.Add(deleteSelectedItem);
            _trayMenu.Items.Add(cleanupMenu);

            _classMenuItem = new ToolStripMenuItem("Definir classe manualmente");
            foreach (string code in new[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS" })
            {
                string captured = code;
                _classMenuItem.DropDownItems.Add(code, null, delegate { SetManualClass(captured); });
            }
            _classMenuItem.DropDownItems.Add(new ToolStripSeparator());
            _classMenuItem.DropDownItems.Add("Usar detecção automática", null, delegate { ClearManualClass(); });
            _trayMenu.Items.Add(_classMenuItem);

            _lockMenuItem = new ToolStripMenuItem("Bloquear posição") { CheckOnClick = true };
            _lockMenuItem.Checked = _state.PositionLocked;
            _lockMenuItem.Click += delegate
            {
                _state.PositionLocked = _lockMenuItem.Checked;
                UpdateLockUi();
                _store.Save(_state);
            };
            _trayMenu.Items.Add(_lockMenuItem);
            _followGameMonitorItem = new ToolStripMenuItem("Acompanhar monitor do PW")
            {
                CheckOnClick = true,
                Checked = string.IsNullOrWhiteSpace(_state.OverlayMonitorDeviceName)
            };
            _followGameMonitorItem.Click += delegate
            {
                SetFollowGameMonitor(_followGameMonitorItem.Checked);
            };
            _trayMenu.Items.Add(_followGameMonitorItem);
            _compactMenuItem = new ToolStripMenuItem("Modo compacto");
            _compactMenuItem.Click += delegate { SetCompactMode(!_state.CompactMode, true); };
            _trayMenu.Items.Add(_compactMenuItem);
            _trayMenu.Items.Add("Restaurar posição", null, delegate { ResetPosition(); });

            ToolStripMenuItem opacityMenu = new ToolStripMenuItem("Transparência");
            AddOpacityItem(opacityMenu, "65%", 0.65);
            AddOpacityItem(opacityMenu, "75%", 0.75);
            AddOpacityItem(opacityMenu, "82% (recomendado)", 0.82);
            AddOpacityItem(opacityMenu, "90%", 0.90);
            AddOpacityItem(opacityMenu, "100%", 1.0);
            _trayMenu.Items.Add(opacityMenu);
            _trayMenu.Items.Add("Reiniciar progresso deste personagem", null, delegate { ResetProgress(); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Sair", null, delegate { ExitApplication(); });
            _trayMenu.Items.Insert(_trayMenu.Items.Count - 1, new ToolStripMenuItem("Desinstalar programa...", null, delegate {
                Process.Start(new ProcessStartInfo(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PW Overlay Central.exe"), "--uninstall") { UseShellExecute = true });
            }) { Enabled = File.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "unins000.exe")) });
            _trayMenu.Items.Insert(_trayMenu.Items.Count - 1, new ToolStripMenuItem(
                "Versão " + Application.ProductVersion) { Enabled = false });

            _trayIcon = new NotifyIcon
            {
                Icon = PWOverlayShared.AppBranding.Icon,
                Text = "PW Quest Overlay",
                Visible = true,
                ContextMenuStrip = _trayMenu
            };
            _trayIcon.DoubleClick += delegate { SetUserHidden(false); };

            SetCompactMode(_state.CompactMode, false);
            UpdateLockUi();
            UpdateActionAvailability();

            _timer = new Timer { Interval = 100 };
            _timer.Tick += delegate { MonitorTick(); };
            _timer.Start();

            Shown += delegate
            {
                _elevatedIdentity.SetAutoAdvance(_state.Memory.AutoAdvanceEnabled);
                _elevatedIdentity.Listen(AppDomain.CurrentDomain.BaseDirectory, _store.Log);
                RegisterHotkeys();
                BeginInvoke((MethodInvoker)delegate
                {
                    if (_activeGame == IntPtr.Zero || _state.UserHidden) Hide();
                });
            };
            FormClosing += OnFormClosing;
            FormClosed += delegate
            {
                _timer.Stop();
                if (_memoryMonitor != null) _memoryMonitor.Dispose();
                if (_identityMonitor126 != null) _identityMonitor126.Dispose();
                _elevatedIdentity.Dispose();
                UnregisterHotkeys();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
                _shutdown.Dispose();
            };
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams parameters = base.CreateParams;
                parameters.ExStyle |= (int)(NativeMethods.WS_EX_TOOLWINDOW | NativeMethods.WS_EX_NOACTIVATE);
                return parameters;
            }
        }

        protected override bool ShowWithoutActivation { get { return true; } }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NativeMethods.WM_MOUSEACTIVATE)
            {
                message.Result = new IntPtr(NativeMethods.MA_NOACTIVATE);
                return;
            }
            if (message.Msg == NativeMethods.WM_HOTKEY)
            {
                if (_markerCalibrationOpen) return;
                int id = message.WParam.ToInt32();
                if (id == HotkeyDone) CompleteCurrent();
                else if (id == HotkeyPrevious) GoBack();
                else if (id == HotkeySkip) SkipCurrent();
                else if (id == HotkeyMarker) MarkCurrentLocation();
                else if (id == HotkeyMemoryAutoAdvance)
                    SetMemoryAutoAdvance(!_state.Memory.AutoAdvanceEnabled, true);
                return;
            }
            base.WndProc(ref message);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(Color.FromArgb(71, 85, 105)))
                e.Graphics.DrawRectangle(pen, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }

        private Button MakeTopButton(string text, int x, int width)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, 3),
                Size = new Size(width, 24),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(31, 76, 113),
                ForeColor = Color.White,
                TabStop = false,
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private Button MakeActionButton(string text, int x, int y, int width, Color color)
        {
            return new Button
            {
                Text = text,
                Location = new Point(x, y),
                Size = new Size(width, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = color,
                ForeColor = Color.White,
                TabStop = false,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                FlatAppearance = { BorderSize = 0 }
            };
        }

        private void AttachDragHandlers(Control control)
        {
            control.MouseDown += delegate(object sender, MouseEventArgs e)
            {
                if (_state.PositionLocked || e.Button != MouseButtons.Left) return;
                _dragging = true;
                _dragCursorStart = Cursor.Position;
                _dragFormStart = Location;
                // The same control must receive both the press and release.
                // Capturing on the form redirects MouseUp away from this handler.
                control.Capture = true;
            };
            control.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                if (!_dragging || _state.PositionLocked) return;
                if ((e.Button & MouseButtons.Left) == 0)
                {
                    FinishDrag(control);
                    return;
                }
                Point cursor = Cursor.Position;
                Location = new Point(
                    _dragFormStart.X + cursor.X - _dragCursorStart.X,
                    _dragFormStart.Y + cursor.Y - _dragCursorStart.Y);
            };
            control.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (e.Button == MouseButtons.Left) FinishDrag(control);
            };
            control.MouseCaptureChanged += delegate
            {
                if (!control.Capture) FinishDrag(control);
            };
        }

        private void FinishDrag(Control control)
        {
            if (!_dragging) return;
            _dragging = false;
            control.Capture = false;
            // Retain the monitor selected by the user, including secondary screens.
            _currentScreen = Screen.FromRectangle(Bounds);
            _currentMonitorKey = GetMonitorKey(_currentScreen);
            _state.OverlayMonitorDeviceName = _currentScreen.DeviceName;
            _followGameMonitorItem.Checked = false;
            ClampToCurrentScreen();
            SaveCurrentPosition();
        }

        private void MonitorTick()
        {
            if (_shutdown.WaitOne(0)) { ExitApplication(); return; }
            if (_cultivationDialogOpen || _profileDialogOpen || _markerCalibrationOpen) return;
            try
            {
                IntPtr foreground = NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GA_ROOT);
                DateTime now = DateTime.UtcNow;
                if (now >= _nextWindowScanAt)
                {
                    _nextWindowScanAt = now.AddMilliseconds(400);
                    _clientWindows = NativeMethods.FindGameWindows().Select(NativeMethods.DescribeGameWindow)
                        .Where(w => w != null).ToList();
                    _clientBindings.Prune(_clientWindows.Select(w => w.SessionKey));
                    foreach (string stale in _importedHistories.Keys.Where(key => !_clientWindows.Any(window => window.SessionKey == key)).ToList())
                        _importedHistories.Remove(stale);
                    foreach (IntPtr stale in _titleRefresh.Keys.Where(w => !_clientWindows.Any(game => game.Handle == w)).ToList())
                        _titleRefresh.Remove(stale);
                }
                bool gameForeground = _clientWindows.Any(window => window.Handle == foreground);
                bool helperForeground = NativeMethods.WindowBelongsToProcess(foreground, "PW Helper");
                bool ownForeground = NativeMethods.WindowBelongsToCurrentProcess(foreground);

                if ((_memoryMonitor != null || _identityMonitor126 != null || _elevatedIdentity.IsRunning) && now >= _nextMemoryPollAt)
                {
                    _nextMemoryPollAt = now.AddMilliseconds(250);
                    if (_memoryMonitor != null)
                        _memoryMonitor.Poll(_clientWindows.Where(window => MemoryMonitorFor(window) == _memoryMonitor),
                            _state.Memory.AutoAdvanceEnabled);
                    if (_identityMonitor126 != null)
                        _identityMonitor126.Poll(_clientWindows.Where(window => MemoryMonitorFor(window) == _identityMonitor126),
                            _state.Memory.AutoAdvanceEnabled);
                    foreach (ClientWindowInfo window in _clientWindows)
                    {
                        QuestMemoryPolling monitor = MemoryMonitorFor(window);
                        QuestMemoryClientState memory = monitor == null ? null : monitor.Find(window.ProcessId);
                        ElevatedIdentityReader.Sample elevated = window.Server != null && window.Server.IdentityMemoryOnly
                            ? _elevatedIdentity.Find(window.SessionKey) : null;
                        string session = elevated != null ? elevated.SessionToken : memory == null ? null : memory.SessionToken;
                        if (!string.IsNullOrEmpty(session)) _clientBindings.ObserveMemorySession(window.SessionKey, session);
                        PlayerIdentityMemory player = memory == null ? null : memory.Identity;
                        string name = elevated != null ? elevated.Name : player == null ? null : player.Name;
                        string code = elevated != null ? elevated.ClassCode : player == null ? null : player.ClassCode;
                        _clientBindings.ObserveMemoryIdentity(window, string.IsNullOrEmpty(name) ? null :
                            new ClientIdentity(name, code, false, true));
                    }
                }

                if (gameForeground)
                {
                    _lastGameWindow = foreground;
                    if (_activeGame != foreground)
                    {
                        _activeGame = foreground;
                        _store.Log("Cliente ativo hwnd=0x" + _activeGame.ToInt64().ToString("X"));
                        RefreshActiveProfile(true);
                    }
                }
                else if (helperForeground || ownForeground)
                {
                    _helperNeutralUntil = now.AddMilliseconds(900);
                }
                else if (foreground == IntPtr.Zero && now < _helperNeutralUntil)
                {
                    // Transição breve observada entre um painel do PW Helper e o jogo.
                }
                else
                {
                    _activeGame = IntPtr.Zero;
                    if (Visible) Hide();
                    return;
                }

                if (_activeGame == IntPtr.Zero || !NativeMethods.IsWindow(_activeGame) ||
                    !_clientWindows.Any(window => window.Handle == _activeGame))
                {
                    _activeGame = IntPtr.Zero;
                    if (Visible) Hide();
                    return;
                }

                DateTime refreshAt;
                if (!_titleRefresh.TryGetValue(_activeGame, out refreshAt) || now >= refreshAt)
                {
                    _titleRefresh[_activeGame] = now.AddMilliseconds(500);
                    RefreshActiveProfile(false);
                }

                Screen gameScreen = Screen.FromHandle(_activeGame);
                string targetMonitor = OverlayPlacement.ResolveMonitor(
                    _state.OverlayMonitorDeviceName, gameScreen.DeviceName,
                    Screen.AllScreens.Select(candidate => candidate.DeviceName));
                Screen screen = Screen.AllScreens.FirstOrDefault(candidate =>
                    string.Equals(candidate.DeviceName, targetMonitor, StringComparison.OrdinalIgnoreCase))
                    ?? gameScreen;
                string monitorKey = GetMonitorKey(screen);
                if (!_dragging && (_currentScreen == null ||
                    !string.Equals(_currentMonitorKey, monitorKey, StringComparison.Ordinal)))
                {
                    _currentScreen = screen;
                    _currentMonitorKey = monitorKey;
                    ApplySavedOrDefaultPosition();
                }

                if (!_state.UserHidden)
                {
                    if (!Visible)
                    {
                        NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNOACTIVATE);
                        _showMenuItem.Text = "Ocultar overlay";
                    }
                    if (now >= _nextRaiseAt)
                    {
                        RaiseOverlayStack();
                        _nextRaiseAt = now.AddMilliseconds(150);
                    }
                }

                RefreshRouteSnapshot();
                if (_state.Memory.AutoAdvanceEnabled)
                {
                    ClientWindowInfo window = _clientWindows.Find(item => item.Handle == _activeGame);
                    if (window != null && window.Server != null && window.Server.IdentityMemoryOnly)
                        ApplyClassic126Progress(window);
                    else ApplyMemoryProgress();
                }
                if (gameForeground && !_state.UserHidden && !_dragging)
                    PromptCultivationWhenNeeded();
            }
            catch (Exception error)
            {
                _store.Log("Erro no monitor: " + error.GetType().Name + ": " + error.Message);
            }
        }

        private QuestMemoryPolling MemoryMonitorFor(ClientWindowInfo window)
        {
            ClientServer server = _clientBindings.ServerFor(window);
            return server == null ? null : server.IdentityMemoryOnly ? _identityMonitor126 : _memoryMonitor;
        }

        private void RefreshActiveProfile(bool force)
        {
            if (_activeGame == IntPtr.Zero) return;
            string title = NativeMethods.GetWindowTitle(_activeGame).Trim();
            ClientWindowInfo window = NativeMethods.DescribeGameWindow(_activeGame);
            ClientServer server = window == null ? null : _clientBindings.ServerFor(window);
            string titleKey = ClientIdentities.TitleKey(title, server);
            ClientIdentity identity = ClientIdentities.Resolve(window, _clientWindows, _clientBindings,
                _state.ClassAssociations, _state.Characters.Values);
            if (identity == null)
            {
                _characterKey = null; _characterName = null; _classCode = null; _characterServer = null; _progress = null;
                _routeSnapshot = null; _savedImportedHistory = null; _selectedStage = null; _classMissions.Clear();
                _windowTitleKey = titleKey; _currentRoute.Clear(); _orderedStages.Clear(); RefreshMissionDisplay(); return;
            }
            string parsedName = identity.Name, parsedClass = identity.ClassCode;
            bool hasAssociation = identity.Manual;
            string key = identity.ProfileKey;
            _windowTitleKey = titleKey;

            if (!force && string.Equals(_characterKey, key, StringComparison.OrdinalIgnoreCase))
            {
                if (_progress != null)
                {
                    bool changed = _progress.ManualClass != hasAssociation ||
                        !string.Equals(_progress.ClassCode, parsedClass, StringComparison.OrdinalIgnoreCase);
                    _progress.ManualClass = hasAssociation;
                    _progress.ClassCode = parsedClass;
                    if (changed)
                    {
                        _store.Save(_state);
                        LoadRouteAndRefresh();
                    }
                }
                return;
            }

            CharacterProgress progress;
            bool foundProgress = _state.Characters.TryGetValue(key, out progress);
            string memoryAlias = ClientIdentities.LegacyMemoryProfileKey(identity, _state.Characters);
            if (!foundProgress && memoryAlias != null)
            {
                progress = _state.Characters[memoryAlias];
                _state.Characters.Remove(memoryAlias);
                _state.Characters[key] = progress;
                progress.CharacterName = parsedName;
                foundProgress = true;
                _store.Log("Perfil do título preservado com o nome real: " + memoryAlias + " -> " + key);
                _store.Save(_state);
            }
            if (!foundProgress && server == null)
            {
                string baseKey = NormalizeCharacterKey(parsedName);
                CharacterProgress legacy;
                if (_state.Characters.TryGetValue(baseKey, out legacy) &&
                    string.Equals(legacy.ClassCode, parsedClass, StringComparison.OrdinalIgnoreCase))
                {
                    _state.Characters.Remove(baseKey);
                    _state.Characters[key] = legacy;
                    progress = legacy;
                    foundProgress = true;
                    _store.Log("Perfil migrado para chave por classe: " + key);
                    _store.Save(_state);
                }
            }

            if (!foundProgress)
            {
                progress = new CharacterProgress
                {
                    CharacterName = parsedName,
                    ClassCode = parsedClass,
                    ServerKey = server == null ? null : server.Key,
                    ServerName = server == null ? null : server.Name,
                    ManualClass = hasAssociation
                };
                _state.Characters[key] = progress;
                _store.Save(_state);
                _store.Log("Perfil criado: " + parsedName + " [" + (parsedClass ?? "?") + "]");
            }
            else
            {
                progress.ClassCode = parsedClass;
                progress.ManualClass = hasAssociation;
            }

            _characterKey = key;
            _characterName = parsedName;
            _characterServer = server;
            _classCode = progress.ClassCode;
            _progress = progress;
            RemoveEmptyPlaceholderProfiles();
            LoadRouteAndRefresh();
        }

        private void RemoveEmptyPlaceholderProfiles()
        {
            var staleKeys = _state.Characters
                .Where(pair => !object.ReferenceEquals(pair.Value, _progress) &&
                    string.IsNullOrWhiteSpace(pair.Value.ClassCode) &&
                    (pair.Value.MissionStates == null || pair.Value.MissionStates.Count == 0) &&
                    string.Equals(pair.Value.CharacterName, "The Classic PW 1.4.4", StringComparison.OrdinalIgnoreCase))
                .Select(pair => pair.Key)
                .ToList();
            if (staleKeys.Count == 0) return;
            foreach (string staleKey in staleKeys) _state.Characters.Remove(staleKey);
            _store.Save(_state);
        }

        private static void ParseTitle(string title, out string characterName, out string classCode)
        {
            ClientIdentities.ParseTitle(title, out characterName, out classCode);
        }

        private static string NormalizeCharacterKey(string name)
        {
            return (name ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool IsValidClass(string code)
        {
            return ClientIdentities.IsValidClass(code);
        }

        private static string BuildCharacterKey(string name, string classCode, ClientServer server)
        {
            string key = NormalizeCharacterKey(name);
            if (!string.IsNullOrWhiteSpace(classCode))
                key += "|" + classCode.Trim().ToUpperInvariant();
            return ClientServer.Scope(server, key);
        }

        private void LoadRouteAndRefresh()
        {
            _classCode = _progress == null ? null : _progress.ClassCode;
            _routeSnapshot = null;
            _savedImportedHistory = QuestProgression.RestoreImport(_progress == null ? null : _progress.ImportedRoute);
            _selectedStage = null;
            _classMissions = _routes.GetClassMissions(_classCode);
            if (_progress != null && _routes.RestoreAliasProgress(_classMissions, _progress)) _store.Save(_state);
            string path = _progress == null ? null : _progress.CultivationPath;
            _currentRoute = _classMissions.Where(mission => CultivationRouting.IsVisible(mission, path)).ToList();
            _orderedStages = _currentRoute
                .SelectMany(mission => mission.steps.Select((step, index) => new RouteStage
                {
                    Mission = mission,
                    Step = step,
                    StepIndex = index
                }))
                .OrderBy(stage => stage.Step.routeOrder)
                .ThenBy(stage => stage.Mission.order)
                .ThenBy(stage => stage.StepIndex)
                .ToList();
            RefreshMissionDisplay();
        }

        private void RefreshMissionDisplay()
        {
            if (_progress == null)
            {
                _profileLabel.Text = _activeGame == IntPtr.Zero ? "PW Quest Overlay — aguardando cliente" : "Cliente PID " + NativeMethods.GetProcessId(_activeGame) + " — sem associação";
                _questLabel.Text = _activeGame == IntPtr.Zero ? "Abra ou selecione um cliente do Perfect World." : "Defina o personagem e a classe em Perfis.";
                _npcLabel.Text = _activeGame == IntPtr.Zero ? "" : "Use um perfil salvo para retomar seu progresso. PW Helper é opcional.";
                ClientWindowInfo window = _clientWindows.Find(candidate => candidate.Handle == _activeGame);
                if (NeedsElevatedIdentity(window))
                    _npcLabel.Text = "PW 1.2.6 elevado. Em Perfis, permita a leitura para detectar nome e classe.";
                _locationLabel.Text = _activeGame == IntPtr.Zero ? "" : "Uma janela sem associação não altera o progresso.";
                _statusLabel.Text = string.Empty;
                _currentMission = null;
                _currentStep = null;
                _stepTypeLabel.Text = string.Empty;
                UpdateActionAvailability();
                return;
            }

            string classText = string.IsNullOrWhiteSpace(_classCode) ? "classe não definida" : _classCode;
            string cultivationText = CultivationRouting.Normalize(_progress.CultivationPath);
            int finished = _currentRoute.Count(m => _progress.MissionStates.ContainsKey(m.id) ||
                (m.cultivationChoice && CultivationRouting.Normalize(_progress.CultivationPath) != null));
            _profileLabel.Text = _characterName + " [" + classText + (cultivationText == null ? "" : "/" + cultivationText) + "]  ·  " + finished + "/" + _currentRoute.Count;

            if (_currentRoute.Count == 0)
            {
                _questLabel.Text = "Classe não identificada";
                _npcLabel.Text = "Abra Perfis para associar WR, MG, WB, WF, EA, EP, MC, ES, SK ou MS.";
                _locationLabel.Text = "Nome/classe pela memória; use Perfis se a leitura estiver indisponível.";
                _statusLabel.Text = "Seleção manual disponível";
                _currentMission = null;
                _currentStep = null;
                _stepTypeLabel.Text = string.Empty;
                UpdateActionAvailability();
                return;
            }

            int expectedLevel = QuestProgression.ExpectedLevel(_routes.XpToNextLevel,
                QuestProgression.CompletedXp(_classMissions, _progress));
            string levelText = "Nv. previsto " + (expectedLevel > 0 ? expectedLevel.ToString() : "—");
            QuestMemorySnapshot imported = CurrentImportedHistory();
            QuestMemorySnapshot routeSnapshot = _routeSnapshot ?? imported;
            int availableLevel = routeSnapshot == null || routeSnapshot.PlayerLevel == 0 ? expectedLevel : routeSnapshot.PlayerLevel;
            var pending = _orderedStages.Where(stage => !IsStageComplete(stage)).ToList();
            bool optimized = _state.OptimizeQuestXp && expectedLevel > 0;
            RouteStage activeStage;
            if (_selectedStage != null && pending.Contains(_selectedStage))
                activeStage = _selectedStage;
            else if (imported != null)
                activeStage = SelectImportedStage(pending, routeSnapshot, optimized);
            else if (optimized)
            {
                var missions = pending.Select(stage => stage.Mission).Distinct().ToList();
                QuestMission next = QuestProgression.NextMission(missions, _progress, availableLevel,
                    QuestProgression.ActiveRoots(_taskCatalog, routeSnapshot));
                activeStage = pending.FirstOrDefault(stage => stage.Mission == next);
            }
            else
                activeStage = pending.FirstOrDefault();

            if (activeStage == null)
            {
                bool awaitingAvailability = imported != null && pending.Count > 0;
                _questLabel.Text = optimized && pending.Count > 0 ? "Rota reduzida concluída" : "Rota concluída";
                _npcLabel.Text = optimized && pending.Count > 0
                    ? "As missões de XP restantes estão disponíveis em Etapas."
                    : "Todas as etapas da rota foram resolvidas.";
                if (awaitingAvailability)
                {
                    _questLabel.Text = "Nenhuma próxima missão confirmada";
                    _npcLabel.Text = "Abra Etapas para escolher uma pendência ou atualizar o histórico.";
                }
                _locationLabel.Text = string.Empty;
                _statusLabel.Text = levelText;
                _currentMission = null;
                _currentStep = null;
                _stepTypeLabel.Text = awaitingAvailability ? "PAUSA" : "FIM";
                _stepTypeLabel.BackColor = awaitingAvailability ? Color.FromArgb(71, 85, 105) : Color.FromArgb(22, 163, 74);
                UpdateActionAvailability();
                return;
            }

            _currentMission = activeStage.Mission;
            _currentStep = activeStage.Step;
            _currentStepIndex = activeStage.StepIndex;
            ApplyStepType(_currentStep.action);

            _questLabel.Text = _currentMission.name + "  ›  " + _currentStep.title;
            _npcLabel.Text = _currentStep.action;
            _locationLabel.Text = _currentStep.markerX.HasValue && _currentStep.markerY.HasValue
                ? "Marcador: " + _currentStep.markerX.Value + ", " + _currentStep.markerY.Value
                : "Sem coordenada para esta etapa";
            if (imported == null && optimized && _currentMission.minLevel > availableLevel)
                _locationLabel.Text = "Disponível a partir do nível " + _currentMission.minLevel + ".";
            _statusLabel.Text = levelText + " · " + (_currentStepIndex + 1) + "/" + _currentMission.steps.Count;
            _statusLabel.ForeColor = string.Equals(_currentMission.priority, "Opcional", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(251, 191, 36)
                : Color.FromArgb(203, 213, 225);
            UpdateActionAvailability();
        }

        private QuestMemorySnapshot CurrentImportedHistory()
        {
            ClientWindowInfo window = _clientWindows.Find(item => item.Handle == _activeGame);
            Tuple<string, QuestMemorySnapshot> result;
            return window != null && _importedHistories.TryGetValue(window.SessionKey, out result) &&
                result.Item1 == _characterKey ? result.Item2 : _savedImportedHistory;
        }

        private RouteStage SelectImportedStage(List<RouteStage> pending, QuestMemorySnapshot snapshot, bool optimized)
        {
            HashSet<uint> activeRoots = QuestProgression.ActiveRoots(_taskCatalog, snapshot);
            RouteStage available = null;
            RouteStage optional = null;
            bool waitingForRequired = false;
            foreach (RouteStage stage in pending.GroupBy(item => item.Mission).Select(group => group.First()))
            {
                uint[] roots = QuestProgression.StageRoots(_taskCatalog, stage.Mission, stage.Step);
                if (roots.Length == 1 && activeRoots.Contains(roots[0])) return stage;
                QuestAvailability status = QuestProgression.StageAvailability(stage.Mission, roots, snapshot);
                if (status != QuestAvailability.Available)
                {
                    if (!stage.Mission.xpOptional && status == QuestAvailability.Blocked) waitingForRequired = true;
                    continue;
                }
                if (!optimized || !stage.Mission.xpOptional) available = available ?? stage;
                else if (optional == null || QuestProgression.RewardXp(stage.Mission) > QuestProgression.RewardXp(optional.Mission))
                    optional = stage;
            }
            return available ?? (waitingForRequired ? optional : null);
        }

        private void CompleteCurrent()
        {
            if (!CanChangeProgress() || _currentMission == null || _currentStep == null) return;
            if (_currentMission.cultivationChoice) { ChooseCultivation(); return; }
            _progress.MissionStates[StageKey(_currentMission, _currentStep)] = "done";
            if (MissionStagesComplete(_currentMission))
                _progress.MissionStates[_currentMission.id] = QuestProgression.MissionDone(_currentMission, _progress) ? "done" : "skipped";
            _store.Save(_state);
            _store.Log(_characterName + " concluiu etapa " + (_currentStepIndex + 1) + "/" +
                _currentMission.steps.Count + ": " + _currentMission.name);
            RefreshMissionDisplay();
        }

        private void SkipCurrent()
        {
            if (!CanChangeProgress() || _currentMission == null || _currentStep == null) return;
            if (_currentMission.cultivationChoice) { ChooseCultivation(); return; }
            _progress.MissionStates[StageKey(_currentMission, _currentStep)] = "skipped";
            if (MissionStagesComplete(_currentMission)) _progress.MissionStates[_currentMission.id] = "skipped";
            _store.Save(_state);
            _store.Log(_characterName + " pulou etapa " + (_currentStepIndex + 1) + "/" +
                _currentMission.steps.Count + ": " + _currentMission.name);
            RefreshMissionDisplay();
        }

        private void GoBack()
        {
            if (!CanChangeProgress() || _progress == null) return;
            RouteStage previousStage = _orderedStages.LastOrDefault(stage =>
                _progress.MissionStates.ContainsKey(StageKey(stage.Mission, stage.Step)));
            if (previousStage != null)
            {
                _progress.MissionStates.Remove(StageKey(previousStage.Mission, previousStage.Step));
                _progress.MissionStates.Remove(previousStage.Mission.id);
                _selectedStage = previousStage;
                _store.Save(_state);
                _store.Log(_characterName + " voltou uma ação em: " + previousStage.Mission.name);
                RefreshMissionDisplay();
                return;
            }
            QuestMission previous = _currentRoute.LastOrDefault(m => _progress.MissionStates.ContainsKey(m.id));
            if (previous == null) return;
            _progress.MissionStates.Remove(previous.id);
            QuestStep previousStep = previous.steps.LastOrDefault(step =>
                _progress.MissionStates.ContainsKey(StageKey(previous, step)));
            if (previousStep != null)
            {
                _progress.MissionStates.Remove(StageKey(previous, previousStep));
            }
            else
            {
                // Estado 0.1: havia somente a marca da missão. Voltar deve abrir a última etapa.
                for (int index = 0; index < previous.steps.Count - 1; index++)
                    _progress.MissionStates[StageKey(previous, previous.steps[index])] = "done";
            }
            _store.Save(_state);
            _store.Log(_characterName + " voltou uma etapa para: " + previous.name);
            RefreshMissionDisplay();
        }

        private static string StageKey(QuestMission mission, QuestStep step)
        {
            return QuestProgression.StageKey(mission, step);
        }

        private bool IsStageComplete(RouteStage stage)
        {
            if (stage.Mission.cultivationChoice)
                return CultivationRouting.Normalize(_progress.CultivationPath) != null;
            return _progress.MissionStates.ContainsKey(StageKey(stage.Mission, stage.Step)) ||
                (_progress.MissionStates.ContainsKey(stage.Mission.id) &&
                 !stage.Mission.steps.Any(step => _progress.MissionStates.ContainsKey(StageKey(stage.Mission, step))));
        }

        private bool MissionStagesComplete(QuestMission mission)
        {
            return mission.steps.All(step => _progress.MissionStates.ContainsKey(StageKey(mission, step)));
        }

        private void ApplyStepType(string action)
        {
            string text = (action ?? string.Empty).Trim().ToUpperInvariant();
            string type = "AÇÃO";
            Color color = Color.FromArgb(71, 85, 105);
            if (text.StartsWith("ACEITE") || text.StartsWith("PEGUE A MISSÃO")) { type = "PEGAR"; color = Color.FromArgb(37, 99, 235); }
            else if (text.StartsWith("FALE") || text.StartsWith("CONVERSE") || text.StartsWith("REPORTE") || text.StartsWith("RELATÓRIO") || text.StartsWith("PESSOA")) { type = "FALAR"; color = Color.FromArgb(124, 58, 237); }
            else if (text.StartsWith("MATAR") || text.StartsWith("MATE") || text.StartsWith("MATA ")) { type = "MATAR"; color = Color.FromArgb(185, 28, 28); }
            else if (text.StartsWith("COLETE") || text.StartsWith("COLETAR") || text.StartsWith("RECOLHA") || text.StartsWith("PEGAR")) { type = "COLETAR"; color = Color.FromArgb(5, 150, 105); }
            else if (text.StartsWith("ESPERE") || text.StartsWith("AGUARDE")) { type = "ESPERAR"; color = Color.FromArgb(180, 83, 9); }
            else if (text.StartsWith("ENTREGUE") || text.StartsWith("RETORNE")) { type = "ENTREGAR"; color = Color.FromArgb(14, 116, 144); }
            else if (text.StartsWith("VISITE") || text.StartsWith("IR ") || text.StartsWith("VÁ ") || text.StartsWith("VAI ")) { type = "IR"; color = Color.FromArgb(79, 70, 229); }
            _stepTypeLabel.Text = type;
            _stepTypeLabel.BackColor = color;
        }

        private void MarkCurrentLocation()
        {
            if (_markerBusy || _activeGame == IntPtr.Zero || _currentStep == null ||
                !_currentStep.markerX.HasValue || !_currentStep.markerY.HasValue) return;

            _markerBusy = true;
            string originalText = _markerButton.Text;
            NativeMethods.POINT originalCursor;
            NativeMethods.GetCursorPos(out originalCursor);
            try
            {
                NativeMethods.SetForegroundWindow(_activeGame);
                System.Threading.Thread.Sleep(100);

                // Abre Marcadores. No modo recomendado, clica na primeira linha real da
                // lista (não na área vazia), usa End para selecionar o último PWOverlay
                // criado e remove somente ele.
                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.TrackerButton);
                System.Threading.Thread.Sleep(180);

                bool selectedMode = string.Equals(_state.Marker.DeleteMode, "selected", StringComparison.OrdinalIgnoreCase);
                bool shouldDelete = !selectedMode || (_progress != null && _progress.HasOverlayMarker);
                if (shouldDelete)
                {
                    int deleteCount = selectedMode && _progress != null
                        ? Math.Max(1, _progress.OverlayMarkerCount)
                        : 1;
                    for (int deletion = 0; deletion < deleteCount; deletion++)
                    {
                        if (selectedMode)
                        {
                            NativeMethods.ClickClientPoint(_activeGame, _state.Marker.MarkerList);
                            System.Threading.Thread.Sleep(60);
                            NativeMethods.PressKey(NativeMethods.VK_END);
                            System.Threading.Thread.Sleep(120);
                            NativeMethods.ClickClientPoint(_activeGame, _state.Marker.DeleteSelected);
                        }
                        else
                        {
                            NativeMethods.ClickClientPoint(_activeGame, _state.Marker.DeleteAll);
                        }
                        System.Threading.Thread.Sleep(220);

                        // O cliente 1.4.4 sempre pede confirmação antes de excluir waypoints.
                        // A caixa muda alguns pixels conforme escala/resolução, portanto confirma
                        // pelo teclado: Esquerda garante o botão afirmativo e Enter o aciona.
                        NativeMethods.PressKey((byte)NativeMethods.VK_LEFT);
                        System.Threading.Thread.Sleep(80);
                        NativeMethods.PressKey(NativeMethods.VK_RETURN);
                        System.Threading.Thread.Sleep(360);
                        if (!selectedMode) break;
                    }
                    _store.Log(_characterName + " removeu marcador anterior no modo " +
                        (selectedMode ? "PWOverlay" : "todos") + " (quantidade: " + deleteCount + ")");

                    // Excluir altera o estado do painel no cliente. Fechar e reabrir garante
                    // que a criação aconteça nesta mesma chamada, sem um segundo clique.
                    NativeMethods.ClickClientPoint(_activeGame, _state.Marker.PanelClose);
                    System.Threading.Thread.Sleep(180);
                    NativeMethods.ClickClientPoint(_activeGame, _state.Marker.TrackerButton);
                    System.Threading.Thread.Sleep(260);
                }

                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.CoordinateInput);
                NativeMethods.PressKey(NativeMethods.VK_END);
                for (int index = 0; index < 24; index++) NativeMethods.PressKey(NativeMethods.VK_BACK);
                SendKeys.SendWait(_currentStep.markerX.Value + ", " + _currentStep.markerY.Value);
                System.Threading.Thread.Sleep(80);
                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.TrackCoordinates);
                System.Threading.Thread.Sleep(180);

                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.NameInput);
                SendKeys.SendWait("PWOverlay");
                System.Threading.Thread.Sleep(60);
                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.NameOk);
                System.Threading.Thread.Sleep(160);
                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.PanelClose);

                if (_progress != null)
                {
                    _progress.HasOverlayMarker = true;
                    _progress.OverlayMarkerCount = 1;
                    _store.Save(_state);
                }

                _markerButton.Text = "Marcado ✓";
                _store.Log(_characterName + " marcou " + _currentStep.markerX.Value + ", " +
                    _currentStep.markerY.Value + " em " + _currentMission.name);
            }
            catch (Exception error)
            {
                _markerButton.Text = "Falhou";
                _store.Log("Falha ao criar marcador: " + error.GetType().Name + ": " + error.Message);
            }
            finally
            {
                NativeMethods.SetCursorPos(originalCursor.X, originalCursor.Y);
                _markerBusy = false;
                var resetTimer = new Timer { Interval = 1200 };
                resetTimer.Tick += delegate
                {
                    resetTimer.Stop();
                    resetTimer.Dispose();
                    _markerButton.Text = originalText;
                    UpdateActionAvailability();
                };
                resetTimer.Start();
            }
        }

        private bool CanChangeProgress()
        {
            return HasCurrentIdentity() && !_state.UserHidden;
        }

        private bool HasCurrentIdentity()
        {
            if (_activeGame == IntPtr.Zero || _progress == null) return false;
            ClientIdentity identity = ClientIdentities.Resolve(NativeMethods.DescribeGameWindow(_activeGame), _clientWindows,
                _clientBindings, _state.ClassAssociations, _state.Characters.Values);
            return identity != null && identity.ProfileKey == _characterKey;
        }

        private void UpdateActionAvailability()
        {
            bool hasRoute = _progress != null && _currentRoute != null && _currentRoute.Count > 0;
            _doneButton.Text = _currentMission != null && _currentMission.cultivationChoice ? "Escolher" : "Etapa feita";
            _doneButton.Enabled = hasRoute && _currentMission != null;
            _skipButton.Enabled = hasRoute && _currentMission != null;
            _previousButton.Enabled = hasRoute && _progress.MissionStates.Count > 0;
            _markerButton.Enabled = hasRoute && _currentStep != null &&
                _currentStep.markerX.HasValue && _currentStep.markerY.HasValue;
        }

        private void SetManualClass(string code)
        {
            ClientWindowInfo window = NativeMethods.DescribeGameWindow(_activeGame);
            if (_progress == null || window == null) { OpenProfileManager(); return; }
            if (_clientBindings.Find(window.SessionKey) != null || _clientBindings.FindMemory(window.SessionKey) != null)
            {
                _clientBindings.Set(window, new ClientIdentity(_characterName, code, true));
                RefreshActiveProfile(true); return;
            }
            if (!string.IsNullOrWhiteSpace(_windowTitleKey))
                _state.ClassAssociations[_windowTitleKey] = code;
            ReKeyCurrentProgress(code);
            _progress.ClassCode = code;
            _progress.ManualClass = true;
            _classCode = code;
            _store.Save(_state);
            _store.Log("Classe manual: " + _characterName + " = " + code);
            LoadRouteAndRefresh();
        }

        private void ClearManualClass()
        {
            ClientWindowInfo window = NativeMethods.DescribeGameWindow(_activeGame);
            if (window == null) return;
            _clientBindings.Set(window, null);
            if (!string.IsNullOrWhiteSpace(_windowTitleKey))
                _state.ClassAssociations.Remove(_windowTitleKey);
            _store.Save(_state);
            RefreshActiveProfile(true);
        }

        private static bool NeedsElevatedIdentity(ClientWindowInfo window)
        {
            return window != null && window.Server != null && window.Server.IdentityMemoryOnly &&
                NativeMethods.MemoryAccessDenied(window.ProcessId);
        }

        private async System.Threading.Tasks.Task RequestElevatedIdentity()
        {
            if (_elevatedIdentity.IsRunning) return;
            if (!NativeMethods.FindGameWindows().Select(NativeMethods.DescribeGameWindow).Any(NeedsElevatedIdentity))
            {
                _trayIcon.ShowBalloonTip(4000, "Identificação do PW", "Abra um cliente PW 1.2.6 que precise da leitura elevada.", ToolTipIcon.Info);
                return;
            }
            try
            {
                await _elevatedIdentity.Start(Application.ExecutablePath, _store.Log);
                if (!IsDisposed)
                    _trayIcon.ShowBalloonTip(4000, "Identificação do PW", "Leitor ativo. Selecione o personagem no jogo.", ToolTipIcon.Info);
            }
            catch (Exception error)
            {
                var windowsError = error as System.ComponentModel.Win32Exception;
                if (windowsError != null && windowsError.NativeErrorCode == 1223) return;
                if (!IsDisposed) MessageBox.Show(error.Message, "Não foi possível iniciar o leitor do PW");
            }
        }

        private void OpenProfileManager()
        {
            if (_profileDialogOpen) return;
            _profileDialogOpen = true;
            try
            {
                var windows = NativeMethods.FindGameWindows().Select(NativeMethods.DescribeGameWindow).Where(w => w != null).ToList();
                using (var dialog = new ClientProfilesForm(windows, _clientBindings, _state.ClassAssociations, _state.Characters.Values,
                    _activeGame != IntPtr.Zero ? _activeGame : _lastGameWindow))
                {
                    if (!_elevatedIdentity.IsRunning && windows.Any(NeedsElevatedIdentity))
                        dialog.AddElevatedReadAction(delegate
                        {
                            BeginInvoke(new Action(async delegate { await RequestElevatedIdentity(); }));
                        });
                    if (dialog.ShowDialog(this) != DialogResult.OK) return;
                    var live = NativeMethods.FindGameWindows().Select(NativeMethods.DescribeGameWindow).Where(w => w != null).ToList();
                    if (!ApplyLegacyProfiles(dialog, windows, live)) return;
                    foreach (ClientWindowInfo window in live)
                    {
                        ClientWindowInfo original = windows.Find(candidate => candidate.SessionKey == window.SessionKey);
                        if (original == null || !string.Equals(original.Title, window.Title, StringComparison.OrdinalIgnoreCase)) continue;
                        ClientIdentity identity;
                        if (!dialog.Selections.TryGetValue(window.SessionKey, out identity)) continue;
                        bool serverChanged = _clientBindings.SetServer(window, dialog.Servers[window.SessionKey]);
                        if (serverChanged)
                        {
                            QuestMemoryPolling monitor = MemoryMonitorFor(window);
                            if (monitor != null) monitor.Forget(window.ProcessId);
                        }
                        ClientIdentity previous = _clientBindings.ManualFor(window);
                        if (!serverChanged && ((previous == null && identity == null) ||
                            (previous != null && identity != null && previous.ProfileKey == identity.ProfileKey))) continue;
                        _clientBindings.Set(window, identity);
                        _store.Log("Cliente PID " + window.ProcessId + ": associação " + (identity == null ? "automática" : identity.ProfileKey));
                        if (identity != null && !_state.Characters.ContainsKey(identity.ProfileKey))
                            _state.Characters[identity.ProfileKey] = new CharacterProgress
                            {
                                CharacterName = identity.Name, ClassCode = identity.ClassCode, ManualClass = true,
                                ServerKey = identity.Server == null ? null : identity.Server.Key,
                                ServerName = identity.Server == null ? null : identity.Server.Name
                            };
                    }
                    _store.Save(_state); _clientWindows = live; _characterKey = null;
                    if (_activeGame != IntPtr.Zero) RefreshActiveProfile(true);
                }
            }
            finally { _profileDialogOpen = false; }
        }

        private bool ApplyLegacyProfiles(ClientProfilesForm dialog, List<ClientWindowInfo> original, List<ClientWindowInfo> live)
        {
            foreach (var pair in dialog.LegacyProfiles)
            {
                ClientWindowInfo current = live.Find(window => window.SessionKey == pair.Key);
                ClientWindowInfo previous = original.Find(window => window.SessionKey == pair.Key);
                if (current == null || previous == null || !string.Equals(current.Title, previous.Title, StringComparison.OrdinalIgnoreCase))
                {
                    MessageBox.Show("O cliente mudou. Abra Perfis novamente para vincular o progresso.", "PW Quest Overlay");
                    return false;
                }
            }
            foreach (var pair in dialog.LegacyProfiles)
            {
                string oldKey = _state.Characters.First(entry => object.ReferenceEquals(entry.Value, pair.Value)).Key;
                ClientIdentities.AdoptLegacyProgress(_state.Characters, oldKey, dialog.Selections[pair.Key]);
            }
            return true;
        }

        private void OpenTitleAssociations()
        {
            if (_profileDialogOpen) return;
            _profileDialogOpen = true;
            try { ShowTitleAssociations(); }
            finally { _profileDialogOpen = false; }
        }

        private void ShowTitleAssociations()
        {
            ClientWindowInfo active = NativeMethods.DescribeGameWindow(_activeGame != IntPtr.Zero ? _activeGame : _lastGameWindow);
            ClientServer server = active == null ? null : _clientBindings.ServerFor(active);
            if (server == null) { BeginInvoke(new Action(OpenProfileManager)); return; }
            string prefix = ClientServer.Scope(server, "");
            var titles = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (ClientWindowInfo window in NativeMethods.FindGameWindows().Select(NativeMethods.DescribeGameWindow).Where(w => w != null))
            {
                if (ClientServer.Matches(_clientBindings.ServerFor(window), server.Key) && !string.IsNullOrWhiteSpace(window.Title))
                    titles[ClientIdentities.TitleKey(window.Title, server)] = window.Title;
            }
            foreach (string savedTitle in _state.ClassAssociations.Keys)
                if (savedTitle.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !titles.ContainsKey(savedTitle))
                    titles[savedTitle] = savedTitle.Substring(prefix.Length);

            using (var dialog = new Form())
            using (var grid = new DataGridView())
            using (var description = new Label())
            using (var saveButton = new Button())
            using (var cancelButton = new Button())
            {
                dialog.Text = "Associações por título — " + server.Name;
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(650, 390);
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = true;
                dialog.Font = new Font("Segoe UI", 9.0f);

                description.Text = "Compatibilidade com títulos identificáveis de personagem. Títulos genéricos/repetidos não são usados. " +
                    "Nome e classe são detectados pela memória; use Perfis para uma associação manual.";
                description.Location = new Point(12, 10);
                description.Size = new Size(626, 36);

                grid.Location = new Point(12, 52);
                grid.Size = new Size(626, 285);
                grid.AllowUserToAddRows = true;
                grid.AllowUserToDeleteRows = true;
                grid.RowHeadersVisible = false;
                grid.AutoGenerateColumns = false;
                grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
                grid.BackgroundColor = Color.White;
                grid.BorderStyle = BorderStyle.FixedSingle;

                var titleColumn = new DataGridViewTextBoxColumn
                {
                    Name = "TitleColumn",
                    HeaderText = "Título ou nome do personagem",
                    Width = 330
                };
                var detectedColumn = new DataGridViewTextBoxColumn
                {
                    Name = "DetectedColumn",
                    HeaderText = "Detectada",
                    Width = 90,
                    ReadOnly = true
                };
                var classColumn = new DataGridViewComboBoxColumn
                {
                    Name = "ClassColumn",
                    HeaderText = "Associação manual",
                    Width = 175,
                    FlatStyle = FlatStyle.Flat
                };
                classColumn.Items.AddRange("Automático", "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS");
                grid.Columns.Add(titleColumn);
                grid.Columns.Add(detectedColumn);
                grid.Columns.Add(classColumn);

                foreach (KeyValuePair<string, string> pair in titles)
                {
                    string parsedName;
                    string detectedClass;
                    ParseTitle(pair.Value, out parsedName, out detectedClass);
                    string associated;
                    string selection = _state.ClassAssociations.TryGetValue(pair.Key, out associated)
                        ? associated.ToUpperInvariant()
                        : "Automático";
                    grid.Rows.Add(pair.Value, detectedClass ?? "—", selection);
                }

                saveButton.Text = "Salvar";
                saveButton.Location = new Point(462, 348);
                saveButton.Size = new Size(84, 30);
                saveButton.DialogResult = DialogResult.OK;
                cancelButton.Text = "Cancelar";
                cancelButton.Location = new Point(554, 348);
                cancelButton.Size = new Size(84, 30);
                cancelButton.DialogResult = DialogResult.Cancel;

                dialog.Controls.Add(description);
                dialog.Controls.Add(grid);
                dialog.Controls.Add(saveButton);
                dialog.Controls.Add(cancelButton);
                dialog.AcceptButton = saveButton;
                dialog.CancelButton = cancelButton;

                if (dialog.ShowDialog() != DialogResult.OK) return;

                foreach (DataGridViewRow row in grid.Rows)
                {
                    if (row.IsNewRow) continue;
                    string rowTitle = Convert.ToString(row.Cells["TitleColumn"].Value).Trim();
                    if (string.IsNullOrWhiteSpace(rowTitle)) continue;
                    string rowKey = ClientIdentities.TitleKey(rowTitle, server);
                    string selected = Convert.ToString(row.Cells["ClassColumn"].Value).Trim();
                    if (string.IsNullOrWhiteSpace(selected) ||
                        string.Equals(selected, "Automático", StringComparison.OrdinalIgnoreCase))
                        _state.ClassAssociations.Remove(rowKey);
                    else if (IsValidClass(selected))
                        _state.ClassAssociations[rowKey] = selected.ToUpperInvariant();
                }

                _store.Save(_state);
                _characterKey = null;
                if (_activeGame != IntPtr.Zero) RefreshActiveProfile(true);
            }
        }

        private void OpenStageSelector()
        {
            if (_progress == null || _currentRoute == null || _currentRoute.Count == 0) return;
            using (var dialog = new Form())
            using (var missionBox = new ComboBox())
            using (var stepBox = new ComboBox())
            using (var description = new Label())
            using (var applyButton = new Button())
            using (var cancelButton = new Button())
            using (var importButton = new Button())
            {
                dialog.Text = "Selecionar missão e etapa";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(620, 205);
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.TopMost = true;
                dialog.Font = new Font("Segoe UI", 9.0f);

                description.Text = "Escolha o ponto da rota. Etapas anteriores sem conclusão confirmada serão puladas. As posteriores serão reabertas.";
                description.Location = new Point(12, 12);
                description.Size = new Size(596, 38);
                missionBox.Location = new Point(12, 58);
                missionBox.Size = new Size(596, 26);
                missionBox.DropDownStyle = ComboBoxStyle.DropDownList;
                stepBox.Location = new Point(12, 96);
                stepBox.Size = new Size(596, 26);
                stepBox.DropDownStyle = ComboBoxStyle.DropDownList;
                foreach (QuestMission mission in _currentRoute)
                    missionBox.Items.Add(mission.order + ". " + mission.name);

                Action reloadSteps = delegate
                {
                    stepBox.Items.Clear();
                    if (missionBox.SelectedIndex < 0) return;
                    QuestMission mission = _currentRoute[missionBox.SelectedIndex];
                    for (int index = 0; index < mission.steps.Count; index++)
                        stepBox.Items.Add((index + 1) + "/" + mission.steps.Count + " — " + mission.steps[index].action);
                    stepBox.SelectedIndex = mission == _currentMission ? Math.Max(0, _currentStepIndex) : 0;
                };
                missionBox.SelectedIndexChanged += delegate { reloadSteps(); };
                int missionIndex = _currentMission == null ? 0 : _currentRoute.IndexOf(_currentMission);
                missionBox.SelectedIndex = Math.Max(0, missionIndex);

                applyButton.Text = "Ir para etapa";
                applyButton.Location = new Point(400, 153);
                applyButton.Size = new Size(104, 32);
                applyButton.DialogResult = DialogResult.OK;
                cancelButton.Text = "Cancelar";
                cancelButton.Location = new Point(512, 153);
                cancelButton.Size = new Size(96, 32);
                cancelButton.DialogResult = DialogResult.Cancel;
                importButton.Text = "Importar missões concluídas";
                importButton.Location = new Point(12, 153);
                importButton.Size = new Size(220, 32);
                importButton.DialogResult = DialogResult.Retry;
                importButton.Enabled = !_historyImportBusy;
                dialog.Controls.AddRange(new Control[] { description, missionBox, stepBox, applyButton, cancelButton, importButton });
                dialog.AcceptButton = applyButton;
                dialog.CancelButton = cancelButton;
                DialogResult result = dialog.ShowDialog();
                if (result == DialogResult.Retry)
                {
                    BeginInvoke(new Action(async delegate { await ImportQuestHistory(); }));
                    return;
                }
                if (result != DialogResult.OK || missionBox.SelectedIndex < 0 || stepBox.SelectedIndex < 0) return;

                if (MessageBox.Show("Reposicionar o progresso neste ponto da rota?", "PW Quest Overlay",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                SetProgressPoint(missionBox.SelectedIndex, stepBox.SelectedIndex);
            }
        }

        private void SetProgressPoint(int missionIndex, int stepIndex)
        {
            QuestMission targetMission = _currentRoute[missionIndex];
            QuestStep targetStep = targetMission.steps[stepIndex];
            RouteStage target = _orderedStages.First(stage => stage.Mission == targetMission && stage.Step == targetStep);
            int targetIndex = _orderedStages.IndexOf(target);

            var completed = new HashSet<QuestMission>(_currentRoute.Where(mission => QuestProgression.MissionDone(mission, _progress)));
            foreach (QuestMission mission in _currentRoute) _progress.MissionStates.Remove(mission.id);
            for (int index = 0; index < _orderedStages.Count; index++)
            {
                RouteStage stage = _orderedStages[index];
                string key = StageKey(stage.Mission, stage.Step);
                string existing;
                if (index >= targetIndex) _progress.MissionStates.Remove(key);
                else if (completed.Contains(stage.Mission))
                {
                    if (!_progress.MissionStates.ContainsKey(key)) _progress.MissionStates[key] = "done";
                }
                else if (!_progress.MissionStates.TryGetValue(key, out existing) || !QuestProgression.IsDone(existing))
                    _progress.MissionStates[key] = "skipped";
            }
            foreach (QuestMission mission in _currentRoute)
                if (MissionStagesComplete(mission))
                    _progress.MissionStates[mission.id] = QuestProgression.MissionDone(mission, _progress) ? "done" : "skipped";
            _selectedStage = target;
            _store.Save(_state);
            _store.Log(_characterName + " reposicionou a rota na ação global " + (targetIndex + 1) +
                ": " + targetMission.name + " / etapa " + (stepIndex + 1));
            RefreshMissionDisplay();
        }

        private void OpenMarkerCalibration()
        {
            if (_markerCalibrationOpen || _markerBusy || _cultivationDialogOpen || _profileDialogOpen) return;
            _markerCalibrationOpen = true;
            try
            {
                if (Visible) Hide();
                using (var dialog = new MarkerCalibrationForm(_state.Marker) { PreferredGame = _lastGameWindow })
                {
                    if (dialog.ShowDialog() != DialogResult.OK) return;
                    _state.Marker.Calibrated = true;
                    _store.Save(_state);
                }
            }
            finally
            {
                _markerCalibrationOpen = false;
            }
        }

        private void OpenRouteEditor()
        {
            using (var dialog = new RouteEditorForm(_routes, _routePath, _workbookPath))
                dialog.ShowDialog();
            LoadRouteAndRefresh();
        }

        private void ExportBackup()
        {
            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar perfis e progresso";
                dialog.Filter = "Backup JSON (*.json)|*.json";
                dialog.FileName = "PWOverlay-backup-" + DateTime.Now.ToString("yyyy-MM-dd-HHmm") + ".json";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                _store.Export(_state, dialog.FileName);
                MessageBox.Show("Backup exportado com sucesso.", "PW Quest Overlay");
            }
        }

        private void RestoreBackup()
        {
            if (_profileDialogOpen) return;
            _profileDialogOpen = true;
            try { RestoreBackupCore(); }
            finally { _profileDialogOpen = false; }
        }

        private void RestoreBackupCore()
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Restaurar perfis e progresso";
                dialog.Filter = "Backup JSON (*.json)|*.json";
                if (dialog.ShowDialog() != DialogResult.OK) return;
                AppState imported;
                try { imported = _store.LoadFrom(dialog.FileName); }
                catch (Exception error) { MessageBox.Show(error.Message, "Backup inválido", MessageBoxButtons.OK, MessageBoxIcon.Error); return; }
                if (MessageBox.Show("Substituir os perfis e o progresso atuais pelo backup?", "PW Quest Overlay",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
                _state.Characters = imported.Characters;
                _clientBindings.Clear();
                foreach (ClientWindowInfo window in _clientWindows)
                {
                    if (_memoryMonitor != null) _memoryMonitor.Forget(window.ProcessId);
                    if (_identityMonitor126 != null) _identityMonitor126.Forget(window.ProcessId);
                }
                _cultivationPrompted.Clear();
                _state.Positions = imported.Positions;
                _state.ClassAssociations = imported.ClassAssociations;
                _state.Marker = imported.Marker;
                _state.PositionLocked = imported.PositionLocked;
                _state.OverlayMonitorDeviceName = imported.OverlayMonitorDeviceName;
                _followGameMonitorItem.Checked = string.IsNullOrWhiteSpace(_state.OverlayMonitorDeviceName);
                _currentScreen = null;
                _state.CompactMode = imported.CompactMode;
                _state.OptimizeQuestXp = imported.OptimizeQuestXp;
                _reducedRouteItem.Checked = _state.OptimizeQuestXp;
                _state.Opacity = imported.Opacity;
                _store.Save(_state);
                Opacity = _state.Opacity;
                SetCompactMode(_state.CompactMode, false);
                _lockMenuItem.Checked = _state.PositionLocked;
                UpdateLockUi();
                _characterKey = null;
                if (_activeGame != IntPtr.Zero) RefreshActiveProfile(true);
                MessageBox.Show("Backup restaurado.", "PW Quest Overlay");
            }
        }

        private void ReKeyCurrentProgress(string classCode)
        {
            if (_progress == null || string.IsNullOrWhiteSpace(_characterName)) return;
            string newKey = BuildCharacterKey(_characterName, classCode, _characterServer);
            if (string.Equals(newKey, _characterKey, StringComparison.OrdinalIgnoreCase)) return;

            CharacterProgress existing;
            if (_state.Characters.TryGetValue(newKey, out existing) && !object.ReferenceEquals(existing, _progress))
            {
                _progress = existing;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(_characterKey))
                    _state.Characters.Remove(_characterKey);
                _state.Characters[newKey] = _progress;
            }
            _characterKey = newKey;
        }

        private void ResetProgress()
        {
            if (_progress == null) return;
            DialogResult result = MessageBox.Show(
                "Apagar as marcações de " + _characterName + "?",
                "PW Quest Overlay",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question,
                MessageBoxDefaultButton.Button2);
            if (result != DialogResult.Yes) return;
            _progress.MissionStates.Clear();
            _cultivationPrompted.Remove(_characterKey);
            _store.Save(_state);
            RefreshMissionDisplay();
        }

        private void ToggleLock()
        {
            _state.PositionLocked = !_state.PositionLocked;
            _lockMenuItem.Checked = _state.PositionLocked;
            UpdateLockUi();
            _store.Save(_state);
        }

        private void SetCompactMode(bool compact, bool persist)
        {
            _state.CompactMode = compact;
            _compactButton.Text = compact ? "+" : "−";
            _compactMenuItem.Checked = compact;

            _profilesButton.Visible = !compact;
            _stagesButton.Visible = !compact;
            _lockButton.Visible = !compact;
            _locationLabel.Visible = !compact;
            _skipButton.Visible = !compact;

            MinimumSize = Size.Empty;
            MaximumSize = Size.Empty;
            ClientSize = compact ? CompactClientSize : FullClientSize;
            MinimumSize = Size;
            MaximumSize = Size;
            _topBar.Size = new Size(ClientSize.Width, 30);

            if (compact)
            {
                _profileLabel.Size = new Size(385, 19);
                _compactButton.Location = new Point(404, 3);
                _hideButton.Location = new Point(438, 3);
                _questLabel.Location = new Point(10, 34);
                _questLabel.Size = new Size(450, 22);
                _questLabel.Font = new Font("Segoe UI", 10.5f, FontStyle.Bold);
                _stepTypeLabel.Location = new Point(10, 59);
                _stepTypeLabel.Size = new Size(62, 20);
                _npcLabel.Location = new Point(79, 57);
                _npcLabel.Size = new Size(381, 35);
                _npcLabel.Font = new Font("Segoe UI", 8.75f, FontStyle.Regular);
                _previousButton.Location = new Point(10, 94);
                _previousButton.Size = new Size(60, 24);
                _doneButton.Location = new Point(76, 94);
                _doneButton.Size = new Size(84, 24);
                _markerButton.Location = new Point(166, 94);
                _markerButton.Size = new Size(68, 24);
                _statusLabel.Location = new Point(240, 95);
                _statusLabel.Size = new Size(220, 22);
            }
            else
            {
                _profileLabel.Size = new Size(245, 19);
                _compactButton.Location = new Point(498, 3);
                _hideButton.Location = new Point(532, 3);
                _questLabel.Location = new Point(12, 36);
                _questLabel.Size = new Size(546, 28);
                _questLabel.Font = new Font("Segoe UI", 12.0f, FontStyle.Bold);
                _stepTypeLabel.Location = new Point(12, 69);
                _stepTypeLabel.Size = new Size(68, 22);
                _npcLabel.Location = new Point(88, 67);
                _npcLabel.Size = new Size(470, 38);
                _npcLabel.Font = new Font("Segoe UI", 9.5f, FontStyle.Regular);
                _previousButton.Location = new Point(12, 137);
                _previousButton.Size = new Size(72, 32);
                _doneButton.Location = new Point(90, 137);
                _doneButton.Size = new Size(102, 32);
                _markerButton.Location = new Point(296, 137);
                _markerButton.Size = new Size(82, 32);
                _statusLabel.Location = new Point(384, 142);
                _statusLabel.Size = new Size(174, 22);
            }

            ClampToCurrentScreen();
            if (persist)
            {
                SaveCurrentPosition();
                _store.Save(_state);
            }
            Invalidate();
        }

        private void UpdateLockUi()
        {
            _lockButton.Text = _state.PositionLocked ? "Fixado" : "Mover";
            _topBar.Cursor = _state.PositionLocked ? Cursors.Default : Cursors.SizeAll;
            _profileLabel.Cursor = _topBar.Cursor;
        }

        private void SetUserHidden(bool hidden)
        {
            _state.UserHidden = hidden;
            _store.Save(_state);
            _showMenuItem.Text = hidden ? "Mostrar overlay" : "Ocultar overlay";
            if (hidden) Hide();
            else if (_activeGame != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNOACTIVATE);
                RaiseOverlayStack();
            }
        }

        private void OpenConfigurationGuide()
        {
            string path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "INSTALACAO.txt");
            if (System.IO.File.Exists(path))
            {
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                return;
            }
            MessageBox.Show(
                "O guia INSTALACAO.txt não foi encontrado ao lado do executável.",
                "PW Quest Overlay",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private static bool IsTalkStage(string action)
        {
            return Regex.IsMatch(
                action ?? string.Empty,
                @"\b(?:(?:fale|falar|converse)\s+com|visite\s+(?:o|a))\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool IsTurnInStage(QuestMission mission, QuestStep step)
        {
            string action = step == null ? string.Empty : step.action ?? string.Empty;
            if (Regex.IsMatch(
                action,
                @"\b(?:entregue|reporte|retorne|volte|relat[oó]rio)\b|\bNPC\s+Final\s*:",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
                return true;
            return mission != null && step != null &&
                mission.steps.IndexOf(step) == mission.steps.Count - 1 &&
                IsTalkStage(action);
        }

        private static bool IsCollectionStage(string action)
        {
            return Regex.IsMatch(
                action ?? string.Empty,
                @"\b(?:peg(?:ar|ue)|colet(?:ar|e)|obte(?:r|nha)|consig(?:a|uir)|re[uú]na|junte)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private bool CanUseMemoryForProfile(QuestMemoryClientState client)
        {
            return client != null && client.Ready && client.Snapshot != null && client.Identity != null &&
                string.Equals(client.Identity.Name, _characterName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(client.Identity.ClassCode, _classCode, StringComparison.OrdinalIgnoreCase);
        }

        private async System.Threading.Tasks.Task ImportQuestHistory()
        {
            if (_historyImportBusy) return;
            ClientWindowInfo window = NativeMethods.DescribeGameWindow(_activeGame);
            if (window == null || _progress == null || _taskCatalog == null || !HasCurrentIdentity())
            {
                MessageBox.Show("Selecione no jogo o personagem que deseja importar e abra Etapas novamente.", "Importar missões concluídas");
                return;
            }

            CharacterProgress progress = _progress;
            var missions = _classMissions.ToArray();
            string name = _characterName, classCode = _classCode;
            string directory = AppDomain.CurrentDomain.BaseDirectory;
            bool classic126 = window.Server != null && window.Server.IdentityMemoryOnly;
            _historyImportBusy = true;
            try
            {
                QuestMemorySnapshot snapshot;
                if (classic126 && NativeMethods.MemoryAccessDenied(window.ProcessId))
                {
                    if (!_elevatedIdentity.IsRunning)
                        await _elevatedIdentity.Start(Application.ExecutablePath, _store.Log);
                    snapshot = await _elevatedIdentity.ReadHistory(window, name, classCode);
                }
                else
                {
                    snapshot = await System.Threading.Tasks.Task.Run(delegate
                    {
                        var file = MemoryLayoutFile.Load(Path.Combine(directory, "memory_offsets.json"));
                        return QuestMemoryMonitor.ReadHistory(window.ProcessId,
                            file.Find(classic126 ? ElevatedIdentityReader.ProfileId : "the_classic_144"), name, classCode);
                    });
                }
                if (classic126)
                    snapshot = await System.Threading.Tasks.Task.Run(() =>
                        QuestProgression.TranslateHistory126(Path.Combine(directory, "task_history_126.tsv"), snapshot));
                if (IsDisposed) return;
                ClientWindowInfo current = NativeMethods.DescribeGameWindow(window.Handle);
                if (current == null || current.SessionKey != window.SessionKey ||
                    !object.ReferenceEquals(_progress, progress) || !HasCurrentIdentity())
                    throw new InvalidOperationException("O cliente ou personagem mudou. Abra Etapas e tente novamente.");

                int changed = QuestProgression.ImportHistory(missions, progress, _taskCatalog, snapshot);
                progress.ImportedRoute = QuestProgression.RememberImport(missions, _taskCatalog, snapshot);
                _savedImportedHistory = QuestProgression.RestoreImport(progress.ImportedRoute);
                _importedHistories[window.SessionKey] = Tuple.Create(_characterKey, snapshot);
                _selectedStage = null;
                _store.Save(_state);
                RefreshMissionDisplay();
                string next = _currentMission == null ? "sem próxima missão confirmada" : _currentMission.name;
                _store.Log(name + ": importação solicitada atualizou " + changed + " missões do guia. Próxima: " + next + ".");
                string message = changed == 0 ? "O histórico não trouxe novas conclusões para esta rota." :
                    "Histórico aplicado. Missões do guia atualizadas: " + changed + ".";
                message += _currentMission == null ? "\nNenhuma próxima missão foi confirmada pelo guia." :
                    "\nPróxima missão do guia — " + _currentMission.name + ".";
                MessageBox.Show(this, message, "Importar missões concluídas", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception error)
            {
                var windowsError = error as System.ComponentModel.Win32Exception;
                if (windowsError != null && windowsError.NativeErrorCode == 1223) return;
                _store.Log("Falha na importação solicitada: " + error.Message);
                if (!IsDisposed) MessageBox.Show(this, error.Message, "Histórico indisponível", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally { _historyImportBusy = false; }
        }

        private void RefreshRouteSnapshot()
        {
            if (_memoryMonitor == null || _taskCatalog == null || _activeGame == IntPtr.Zero || _progress == null)
                return;
            QuestMemoryClientState client = _memoryMonitor.Find(NativeMethods.GetProcessId(_activeGame));
            QuestMemorySnapshot snapshot = CanUseMemoryForProfile(client) ? client.Snapshot : null;
            if (object.ReferenceEquals(_routeSnapshot, snapshot) || !HasCurrentIdentity()) return;
            QuestMemorySnapshot previous = _routeSnapshot;
            _routeSnapshot = snapshot;
            bool activeRootsChanged = !QuestProgression.ActiveRoots(_taskCatalog, previous)
                .SetEquals(QuestProgression.ActiveRoots(_taskCatalog, snapshot));
            if (activeRootsChanged || (previous == null ? 0 : previous.PlayerLevel) !=
                (snapshot == null ? 0 : snapshot.PlayerLevel))
                RefreshMissionDisplay();
        }

        private void ApplyMemoryProgress()
        {
            if (_memoryMonitor == null || _taskCatalog == null ||
                _activeGame == IntPtr.Zero || _progress == null ||
                _currentMission == null || _currentStep == null)
                return;

            if (!HasCurrentIdentity()) return;

            if (_currentMission.cultivationChoice) return;

            int processId = NativeMethods.GetProcessId(_activeGame);
            QuestMemoryClientState client = _memoryMonitor.Find(processId);
            if (!CanUseMemoryForProfile(client)) return;

            for (int attempt = 0; attempt < 8 && _currentStep != null; attempt++)
            {
                uint taskId;
                if (_currentMission.cultivationChoice) return;
                if (!TryResolveUniqueTaskId(
                    _currentMission, _currentStep, out taskId))
                {
                    LogMemoryDiagnostic("sem ID único: " + _currentMission.name +
                        " / " + _currentStep.title);
                    return;
                }

                ActiveQuestMemoryEntry active = client.FindActive(taskId);
                string action = _currentStep.action ?? string.Empty;
                bool accepted = QuestProgression.IsAcceptStage(action) && active != null;
                TaskCatalogEntry currentTask = _taskCatalog.Find(taskId);
                string killEvidence = string.Empty;
                bool killsComplete = currentTask != null &&
                    currentTask.TryGetKillProgress(active, action, out killEvidence);
                bool taskItemsComplete = IsCollectionStage(action) &&
                    _taskCatalog.AreTaskItemsComplete(
                        taskId, client.Snapshot, _currentStep.memoryItemIds);
                bool automaticChildComplete = _taskCatalog.CanConfirmAutomaticKillCompletion(
                    taskId, action, client.Snapshot) &&
                    client.HasObservedDisappearance(taskId);
                bool nextTaskResolved = false;
                uint nextTaskId = 0;
                if (_currentStepIndex + 1 < _currentMission.steps.Count)
                {
                    nextTaskResolved = TryResolveUniqueTaskId(
                        _currentMission,
                        _currentMission.steps[_currentStepIndex + 1],
                        out nextTaskId);
                }

                bool waitsForDisappearance = IsTurnInStage(
                    _currentMission, _currentStep) ||
                    IsTalkStage(action);
                bool disappeared = waitsForDisappearance &&
                    client.HasObservedDisappearance(taskId);
                bool recordedComplete = !_currentMission.repeatable && currentTask != null &&
                    client.HasNewlyFinished(currentTask.TopId) &&
                    _taskCatalog.IsChainRecordedComplete(taskId, client.Snapshot);
                bool orderedSuccessor = nextTaskResolved &&
                    (IsCollectionStage(action) || (currentTask != null && currentTask.IsAutomaticKillStage(action))) &&
                    _taskCatalog.HasActiveOrderedSuccessor(taskId, nextTaskId, client.Snapshot);
                TaskCatalogEntry nextTask = nextTaskResolved
                    ? _taskCatalog.Find(nextTaskId) : null;
                bool successorActive = waitsForDisappearance && active == null &&
                    nextTaskResolved && nextTaskId != taskId &&
                    client.FindActive(nextTaskId) != null &&
                    currentTask != null && nextTask != null &&
                    currentTask.TopId == nextTask.TopId;
                if (!accepted && !taskItemsComplete && !killsComplete && !automaticChildComplete && !recordedComplete && !orderedSuccessor &&
                    !disappeared && !successorActive)
                {
                    LogMemoryDiagnostic("aguardando: " + _currentMission.name +
                        " / " + _currentStep.title + " ID=" + taskId +
                        " ativo=" + (active != null) +
                        " espera-sumir=" + waitsForDisappearance +
                        " próximo=" + (nextTaskResolved ? nextTaskId.ToString() : "-") +
                        " próximo-ativo=" + (nextTaskResolved &&
                            client.FindActive(nextTaskId) != null) +
                        " itens=" + TaskItemEvidence(currentTask, client.Snapshot, _currentStep.memoryItemIds) +
                        " abates=" + killEvidence);
                    return;
                }

                string evidence = recordedComplete
                    ? "cadeia ID " + currentTask.TopId + " confirmada no histórico de entregas"
                    : orderedSuccessor
                        ? "sucessor ID " + nextTaskId + " ativo em cadeia sequencial confirmada no catálogo"
                    : accepted
                    ? "ID " + taskId + " apareceu na lista ativa"
                    : automaticChildComplete
                        ? "objetivo filho automático ID " + taskId +
                            " desapareceu após observação, com sua cadeia ainda ativa"
                    : killsComplete
                        ? "abates completos: " + killEvidence
                    : taskItemsComplete
                        ? "itens de missão completos: " +
                            TaskItemEvidence(currentTask, client.Snapshot, _currentStep.memoryItemIds)
                    : disappeared
                        ? "ID " + taskId + " foi observado e depois desapareceu"
                        : "a próxima submissão da mesma cadeia, ID " +
                            nextTaskId + ", já está ativa";
                MarkCurrentStageFromMemory(evidence);
            }
        }

        private void ApplyClassic126Progress(ClientWindowInfo window)
        {
            if (_classic126Tasks == null || _taskCatalog == null || _currentMission == null || _currentStep == null ||
                !HasCurrentIdentity()) return;
            var elevated = _elevatedIdentity.Find(window.SessionKey);
            IEnumerable<uint> active;
            if (elevated != null)
            {
                if (elevated.Active == null || !string.Equals(elevated.Name, _characterName, StringComparison.OrdinalIgnoreCase) ||
                    !string.Equals(elevated.ClassCode, _classCode, StringComparison.OrdinalIgnoreCase)) return;
                active = elevated.Active;
            }
            else
            {
                QuestMemoryClientState client = _identityMonitor126 == null ? null : _identityMonitor126.Find(window.ProcessId);
                if (!CanUseMemoryForProfile(client)) return;
                active = client.Snapshot.ActiveTasks.Select(task => task.TaskId);
            }
            HashSet<uint> canonical = QuestProgression.TranslateActive126(_classic126Tasks, active);
            for (int attempt = 0; attempt < 8 && _currentMission != null && _currentStep != null; attempt++)
            {
                string evidence = QuestProgression.Classic126Evidence(_currentMission, _currentStep, _taskCatalog, canonical);
                if (evidence == null) return;
                MarkCurrentStageFromMemory(evidence);
            }
        }

        private static string TaskItemEvidence(
            TaskCatalogEntry task,
            QuestMemorySnapshot snapshot,
            uint[] selectedItems)
        {
            if (task == null || task.RequiredTaskItems == null ||
                task.RequiredTaskItems.Count == 0 || snapshot == null)
                return "-";
            return string.Join(", ", task.RequiredTaskItems
                .Where(requirement => selectedItems == null || selectedItems.Length == 0 ||
                    selectedItems.Contains(requirement.Key)).Select(requirement =>
            {
                int current;
                snapshot.TaskItemCounts.TryGetValue(requirement.Key, out current);
                return requirement.Key + "=" + current + "/" + requirement.Value;
            }));
        }

        private void LogMemoryDiagnostic(string message)
        {
            if (string.Equals(message, _memoryDiagnosticSignature, StringComparison.Ordinal))
                return;
            _memoryDiagnosticSignature = message;
            _store.Log("Diagnóstico de memória: " + message);
        }

        private bool TryResolveUniqueTaskId(
            QuestMission mission,
            QuestStep step,
            out uint taskId)
        {
            taskId = QuestProgression.ResolveStageTaskId(_taskCatalog, mission, step);
            return taskId != 0;
        }

        private void MarkCurrentStageFromMemory(string evidence)
        {
            QuestMission mission = _currentMission;
            QuestStep step = _currentStep;
            int stepIndex = _currentStepIndex;
            _progress.MissionStates[StageKey(mission, step)] = "done-memory";
            if (MissionStagesComplete(mission))
                _progress.MissionStates[mission.id] = QuestProgression.MissionDone(mission, _progress) ? "done-memory" : "skipped";
            _store.Save(_state);
            _store.Log(_characterName + " avançou automaticamente por memória na etapa " +
                (stepIndex + 1) + "/" + mission.steps.Count + ": " + mission.name +
                "; evidência=" + evidence);
            RefreshMissionDisplay();
        }

        private void SetMemoryAutoAdvance(bool enabled, bool notify)
        {
            if (enabled && (_memoryMonitor == null || _taskCatalog == null))
            {
                enabled = false;
                if (notify)
                    MessageBox.Show(
                        "O leitor de memória não pôde ser iniciado. Consulte o overlay.log.",
                        "Estado por memória",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
            }
            _state.Memory.AutoAdvanceEnabled = enabled;
            _elevatedIdentity.SetAutoAdvance(enabled);
            _memoryAutoAdvanceItem.Checked = enabled;
            _nextMemoryPollAt = DateTime.MinValue;
            _store.Save(_state);
            _store.Log("Sincronização e avanço automáticos por memória " +
                (enabled ? "ativados" : "desativados"));
            if (!notify) return;
            _trayIcon.ShowBalloonTip(
                1800,
                "PW Quest Overlay",
                "Sincronização por memória " + (enabled ? "ATIVADA" : "DESATIVADA"),
                enabled ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private void PromptCultivationWhenNeeded()
        {
            if (_progress == null || _currentMission == null || !_currentMission.cultivationChoice ||
                (_routeSnapshot != null && _routeSnapshot.PlayerLevel < _currentMission.minLevel) ||
                CultivationRouting.Normalize(_progress.CultivationPath) != null ||
                _cultivationDialogOpen || !IsHandleCreated || _cultivationPrompted.Contains(_characterKey)) return;
            string profileKey = _characterKey;
            _cultivationPrompted.Add(profileKey);
            BeginInvoke((MethodInvoker)delegate
            {
                if (_characterKey == profileKey && _currentMission != null && _currentMission.cultivationChoice &&
                    NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GA_ROOT) == _activeGame)
                    ChooseCultivation();
                else _cultivationPrompted.Remove(profileKey);
            });
        }

        private void ChooseCultivation()
        {
            if (_progress == null || _cultivationDialogOpen) return;
            CharacterProgress selected = _progress;
            _cultivationDialogOpen = true;
            try
            {
                using (var dialog = new CultivationChoiceForm(_characterName + " [" + _classCode + "]", selected.CultivationPath))
                {
                    if (dialog.ShowDialog(this) != DialogResult.OK || dialog.SelectedPath == null) return;
                    selected.CultivationPath = dialog.SelectedPath;
                    _store.Save(_state);
                    _store.Log("Cultivo do guia de " + selected.CharacterName + ": " + selected.CultivationPath);
                    if (object.ReferenceEquals(selected, _progress)) LoadRouteAndRefresh();
                }
            }
            finally { _cultivationDialogOpen = false; }
        }

        private void RaiseOverlayStack()
        {
            uint flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER |
                NativeMethods.SWP_SHOWWINDOW;

            // Primeiro o tracker; por último todos os painéis flutuantes do PW Helper,
            // para que permaneçam visíveis mesmo quando o jogo ativo volta ao topo.
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, flags);
            foreach (IntPtr helperWindow in NativeMethods.FindPwHelperOverlayWindows())
            {
                NativeMethods.SetWindowPos(helperWindow, NativeMethods.HWND_TOPMOST,
                    0, 0, 0, 0, flags | NativeMethods.SWP_ASYNCWINDOWPOS);
            }
        }

        private void AddOpacityItem(ToolStripMenuItem parent, string text, double value)
        {
            parent.DropDownItems.Add(text, null, delegate
            {
                _state.Opacity = value;
                Opacity = value;
                _store.Save(_state);
            });
        }

        private string GetMonitorKey(Screen screen)
        {
            Rectangle bounds = screen.Bounds;
            return screen.DeviceName + "|" + bounds.X + "," + bounds.Y + "|" + bounds.Width + "x" + bounds.Height;
        }

        private void ApplySavedOrDefaultPosition()
        {
            if (_currentScreen == null) return;
            SavedPosition saved;
            if (_state.Positions.TryGetValue(_currentMonitorKey, out saved))
                Location = new Point(saved.X, saved.Y);
            else
                Location = FindDefaultPosition(_currentScreen.Bounds);
            ClampToCurrentScreen();
        }

        private Point FindDefaultPosition(Rectangle bounds)
        {
            Point[] candidates =
            {
                new Point(bounds.Right - Width - 20, bounds.Top + 20),
                new Point(bounds.Left + 20, bounds.Top + 20),
                new Point(bounds.Left + 20, bounds.Bottom - Height - 20),
                new Point(bounds.Right - Width - 20, bounds.Bottom - Height - 20)
            };
            var helperRects = new List<NativeMethods.RECT>();
            foreach (IntPtr helperWindow in NativeMethods.FindPwHelperOverlayWindows())
            {
                NativeMethods.RECT helperRect;
                if (NativeMethods.GetWindowRect(helperWindow, out helperRect))
                    helperRects.Add(helperRect);
            }
            foreach (Point point in candidates)
            {
                Rectangle candidate = new Rectangle(point, Size);
                bool intersectsHelper = helperRects.Any(helper => candidate.IntersectsWith(
                    new Rectangle(helper.Left, helper.Top, helper.Width, helper.Height)));
                if (!intersectsHelper)
                    return point;
            }
            return candidates[0];
        }

        private void ClampToCurrentScreen()
        {
            if (_currentScreen == null) return;
            Location = OverlayPlacement.Clamp(Location, Size, _currentScreen.WorkingArea);
        }

        private void SetFollowGameMonitor(bool follow)
        {
            if (follow)
            {
                _state.OverlayMonitorDeviceName = null;
                _currentScreen = null;
                _currentMonitorKey = null;
                _store.Save(_state);
            }
            else
            {
                _currentScreen = Screen.FromRectangle(Bounds);
                _currentMonitorKey = GetMonitorKey(_currentScreen);
                _state.OverlayMonitorDeviceName = _currentScreen.DeviceName;
                SaveCurrentPosition();
            }
            _followGameMonitorItem.Checked = follow;
        }

        private void SaveCurrentPosition()
        {
            if (string.IsNullOrWhiteSpace(_currentMonitorKey)) return;
            _state.Positions[_currentMonitorKey] = new SavedPosition { X = Left, Y = Top };
            _store.Save(_state);
        }

        private void ResetPosition()
        {
            if (!string.IsNullOrWhiteSpace(_currentMonitorKey))
                _state.Positions.Remove(_currentMonitorKey);
            ApplySavedOrDefaultPosition();
            SaveCurrentPosition();
        }

        private void RegisterHotkeys()
        {
            bool done = NativeMethods.RegisterHotKey(Handle, HotkeyDone,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_RIGHT);
            bool previous = NativeMethods.RegisterHotKey(Handle, HotkeyPrevious,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_LEFT);
            bool skip = NativeMethods.RegisterHotKey(Handle, HotkeySkip,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_DOWN);
            bool markerPrimary = NativeMethods.RegisterHotKey(Handle, HotkeyMarker,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_M);
            bool markerFallback = false;
            if (!markerPrimary)
            {
                markerFallback = NativeMethods.RegisterHotKey(Handle, HotkeyMarker,
                    NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, NativeMethods.VK_M);
            }
            string markerShortcut = markerPrimary ? "Ctrl + Alt + M"
                : markerFallback ? "Ctrl + Shift + M" : "indisponível (use o botão Marcar)";
            bool memoryAutoAdvance = NativeMethods.RegisterHotKey(Handle, HotkeyMemoryAutoAdvance,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, NativeMethods.VK_A);
            _markerHotkeyItem.Text = "Atalho do marcador: " + markerShortcut;
            _store.Log("Atalhos registrados: concluir=" + done + " voltar=" + previous +
                " pular=" + skip + " marcador=" + markerShortcut +
                " memoria-auto=" + memoryAutoAdvance);
        }

        private void UnregisterHotkeys()
        {
            if (!IsHandleCreated) return;
            NativeMethods.UnregisterHotKey(Handle, HotkeyDone);
            NativeMethods.UnregisterHotKey(Handle, HotkeyPrevious);
            NativeMethods.UnregisterHotKey(Handle, HotkeySkip);
            NativeMethods.UnregisterHotKey(Handle, HotkeyMarker);
            NativeMethods.UnregisterHotKey(Handle, HotkeyMemoryAutoAdvance);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowExit || e.CloseReason == CloseReason.WindowsShutDown ||
                e.CloseReason == CloseReason.TaskManagerClosing || e.CloseReason == CloseReason.ApplicationExitCall) return;
            e.Cancel = true;
            SetUserHidden(true);
        }

        private void ExitApplication()
        {
            _allowExit = true;
            Close();
        }
    }
}
