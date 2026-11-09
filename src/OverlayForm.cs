using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
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
        private const int HotkeyOcrSelect = 1005;
        private const int HotkeyOcrScan = 1006;
        private const int HotkeyOcrAutoAdvance = 1007;
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
        private readonly ToolStripMenuItem _compactMenuItem;
        private readonly ToolStripMenuItem _classMenuItem;
        private readonly ToolStripMenuItem _markerHotkeyItem;
        private readonly ToolStripMenuItem _ocrHotkeyItem;
        private readonly ToolStripMenuItem _ocrAutoAdvanceItem;
        private readonly ToolStripMenuItem _memoryAutoAdvanceItem;
        private readonly Dictionary<IntPtr, DateTime> _titleRefresh;
        private readonly Dictionary<string, OcrStabilityState> _ocrStability;
        private readonly Dictionary<string, OcrStepObservation> _ocrStepObservations;
        private readonly TaskCatalog _taskCatalog;
        private readonly QuestMemoryMonitor _memoryMonitor;

        private IntPtr _activeGame;
        private DateTime _helperNeutralUntil;
        private DateTime _nextRaiseAt;
        private DateTime _nextAutomaticOcrAt;
        private DateTime _nextMemoryPollAt;
        private string _characterKey;
        private string _windowTitleKey;
        private string _characterName;
        private string _classCode;
        private CharacterProgress _progress;
        private List<QuestMission> _currentRoute;
        private QuestMission _currentMission;
        private QuestStep _currentStep;
        private int _currentStepIndex;
        private int _currentRouteStageIndex;
        private List<RouteStage> _orderedStages;
        private Screen _currentScreen;
        private string _currentMonitorKey;
        private bool _dragging;
        private Point _dragCursorStart;
        private Point _dragFormStart;
        private bool _allowExit;
        private bool _markerBusy;
        private bool _ocrBusy;
        private string _memoryDiagnosticSignature;

        private sealed class RouteStage
        {
            internal QuestMission Mission;
            internal QuestStep Step;
            internal int StepIndex;
        }

        private sealed class OcrStabilityState
        {
            internal string Signature;
            internal int ConsecutiveReadings;
            internal DateTime LastReadAt;
        }

        private sealed class OcrQuestBlock
        {
            internal QuestMission Mission;
            internal string ObservedTitle;
            internal bool Completed;
            internal int? CurrentCount;
            internal int? TotalCount;
        }

        private sealed class OcrStepObservation
        {
            internal bool VisibleInLastConfirmedReading;
        }

        private static readonly Regex PrimaryTitlePattern = new Regex(
            @"^\s*(?<name>.+?)\s*[\[\(\{]\s*(?<class>WB|WF|EA|EP|WR|MC)\s*[\]\)\}]\s*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static readonly Regex TokenClassPattern = new Regex(
            @"(?:^|[\s\[\](){}_\-])(?<class>WB|WF|EA|EP|WR|MC)(?=$|[\s\[\](){}_\-])",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

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
            _ocrStability = new Dictionary<string, OcrStabilityState>(StringComparer.OrdinalIgnoreCase);
            _ocrStepObservations = new Dictionary<string, OcrStepObservation>(
                StringComparer.OrdinalIgnoreCase);
            try
            {
                string baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
                _taskCatalog = TaskCatalog.Load(Path.Combine(
                    baseDirectory, "task_catalog_144.tsv"));
                _memoryMonitor = new QuestMemoryMonitor(
                    Path.Combine(baseDirectory, "memory_offsets.json"),
                    _state.Memory.ProfileId,
                    _store.Log);
            }
            catch (Exception error)
            {
                _store.Log("Leitura de memória indisponível: " + error.Message);
            }

            Text = "PW Quest Overlay";
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
            _trayMenu.Items.Add("Selecionar missão/etapa...", null, delegate { OpenStageSelector(); });
            _trayMenu.Items.Add("Editar rotas...", null, delegate { OpenRouteEditor(); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Exportar backup de perfis...", null, delegate { ExportBackup(); });
            _trayMenu.Items.Add("Restaurar backup de perfis...", null, delegate { RestoreBackup(); });
            _trayMenu.Items.Add(new ToolStripSeparator());
            _trayMenu.Items.Add("Calibrar marcador...", null, delegate { OpenMarkerCalibration(); });
            ToolStripMenuItem memoryMenu = new ToolStripMenuItem("Estado das missões por memória");
            _memoryAutoAdvanceItem = new ToolStripMenuItem(
                "Sincronizar e avançar automaticamente")
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
            memoryMenu.DropDownItems.Add(new ToolStripMenuItem(
                "Somente nomes com ID único são avançados") { Enabled = false });
            _trayMenu.Items.Add(memoryMenu);
            ToolStripMenuItem ocrMenu = new ToolStripMenuItem("OCR experimental");
            ocrMenu.DropDownItems.Add(
                "Selecionar área e testar...",
                null,
                delegate { SelectAndTestOcr(); });
            ocrMenu.DropDownItems.Add(
                "Testar área salva",
                null,
                delegate { TestSavedOcrRegion(); });
            _ocrAutoAdvanceItem = new ToolStripMenuItem(
                "Avanço por OCR desativado nesta branch")
            {
                CheckOnClick = false,
                Checked = false,
                Enabled = false
            };
            _state.Ocr.AutoAdvanceEnabled = false;
            ocrMenu.DropDownItems.Add(_ocrAutoAdvanceItem);
            _ocrHotkeyItem = new ToolStripMenuItem("Atalhos do OCR: verificando...") { Enabled = false };
            ocrMenu.DropDownItems.Add(new ToolStripSeparator());
            ocrMenu.DropDownItems.Add(_ocrHotkeyItem);
            _trayMenu.Items.Add(ocrMenu);
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
            foreach (string code in new[] { "WR", "WB", "WF", "EA", "EP", "MC" })
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

            _trayIcon = new NotifyIcon
            {
                Icon = SystemIcons.Information,
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
                UnregisterHotkeys();
                _trayIcon.Visible = false;
                _trayIcon.Dispose();
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
                int id = message.WParam.ToInt32();
                if (id == HotkeyDone) CompleteCurrent();
                else if (id == HotkeyPrevious) GoBack();
                else if (id == HotkeySkip) SkipCurrent();
                else if (id == HotkeyMarker) MarkCurrentLocation();
                else if (id == HotkeyOcrSelect) SelectAndTestOcr();
                else if (id == HotkeyOcrScan) TestSavedOcrRegion();
                else if (id == HotkeyOcrAutoAdvance)
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
                Capture = true;
            };
            control.MouseMove += delegate(object sender, MouseEventArgs e)
            {
                if (!_dragging || _state.PositionLocked) return;
                Point cursor = Cursor.Position;
                Location = new Point(
                    _dragFormStart.X + cursor.X - _dragCursorStart.X,
                    _dragFormStart.Y + cursor.Y - _dragCursorStart.Y);
            };
            control.MouseUp += delegate(object sender, MouseEventArgs e)
            {
                if (!_dragging) return;
                _dragging = false;
                Capture = false;
                ClampToCurrentScreen();
                SaveCurrentPosition();
            };
        }

        private void MonitorTick()
        {
            try
            {
                List<IntPtr> games = NativeMethods.FindGameWindows();
                IntPtr foreground = NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GA_ROOT);
                DateTime now = DateTime.UtcNow;
                bool gameForeground = games.Contains(foreground);
                bool helperForeground = NativeMethods.WindowBelongsToProcess(foreground, "PW Helper");
                bool ownForeground = NativeMethods.WindowBelongsToCurrentProcess(foreground);

                if (_state.Memory.AutoAdvanceEnabled && _memoryMonitor != null &&
                    now >= _nextMemoryPollAt)
                {
                    _nextMemoryPollAt = now.AddMilliseconds(250);
                    _memoryMonitor.Poll(games
                        .Select(NativeMethods.GetProcessId)
                        .Where(id => id > 0)
                        .Distinct());
                }

                if (gameForeground)
                {
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

                if (_activeGame == IntPtr.Zero || !NativeMethods.IsWindow(_activeGame) || !games.Contains(_activeGame))
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

                Screen screen = Screen.FromHandle(_activeGame);
                string monitorKey = GetMonitorKey(screen);
                if (_currentScreen == null || !string.Equals(_currentMonitorKey, monitorKey, StringComparison.Ordinal))
                {
                    _currentScreen = screen;
                    _currentMonitorKey = monitorKey;
                    ApplySavedOrDefaultPosition();
                }

                if (!_state.UserHidden && !_ocrBusy)
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

                if (_state.Ocr.AutoAdvanceEnabled && _state.Ocr.Calibrated &&
                    gameForeground && !_ocrBusy && now >= _nextAutomaticOcrAt)
                {
                    _nextAutomaticOcrAt = now.AddSeconds(2);
                    BeginAutomaticOcrScan();
                }

                if (_state.Memory.AutoAdvanceEnabled)
                    ApplyMemoryProgress();
            }
            catch (Exception error)
            {
                _store.Log("Erro no monitor: " + error.GetType().Name + ": " + error.Message);
            }
        }

        private void RefreshActiveProfile(bool force)
        {
            if (_activeGame == IntPtr.Zero) return;
            string title = NativeMethods.GetWindowTitle(_activeGame).Trim();
            string titleKey = NormalizeWindowTitle(title);
            string parsedName;
            string parsedClass;
            ParseTitle(title, out parsedName, out parsedClass);
            string associatedClass;
            bool hasAssociation = _state.ClassAssociations.TryGetValue(titleKey, out associatedClass) &&
                IsValidClass(associatedClass);
            if (hasAssociation) parsedClass = associatedClass.ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(parsedName))
            {
                int processId = NativeMethods.GetProcessId(_activeGame);
                parsedName = string.IsNullOrWhiteSpace(title) ? "Cliente " + processId : title;
            }
            string baseKey = NormalizeCharacterKey(parsedName);
            string key = BuildCharacterKey(parsedName, parsedClass);
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
            if (!foundProgress && !string.IsNullOrWhiteSpace(parsedClass))
            {
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
            characterName = (title ?? string.Empty).Trim();
            classCode = null;
            Match primary = PrimaryTitlePattern.Match(characterName);
            if (primary.Success)
            {
                characterName = primary.Groups["name"].Value.Trim();
                classCode = primary.Groups["class"].Value.ToUpperInvariant();
                return;
            }

            Match token = TokenClassPattern.Match(characterName);
            if (!token.Success) return;
            classCode = token.Groups["class"].Value.ToUpperInvariant();
            characterName = (characterName.Substring(0, token.Index) + " " +
                characterName.Substring(token.Index + token.Length)).Trim(' ', '-', '_', '[', ']', '(', ')', '{', '}');
        }

        private static string NormalizeCharacterKey(string name)
        {
            return (name ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static string NormalizeWindowTitle(string title)
        {
            return (title ?? string.Empty).Trim().ToUpperInvariant();
        }

        private static bool IsValidClass(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            string normalized = code.Trim().ToUpperInvariant();
            return normalized == "WR" || normalized == "WB" || normalized == "WF" ||
                normalized == "EA" || normalized == "EP" || normalized == "MC";
        }

        private static string BuildCharacterKey(string name, string classCode)
        {
            string key = NormalizeCharacterKey(name);
            if (!string.IsNullOrWhiteSpace(classCode))
                key += "|" + classCode.Trim().ToUpperInvariant();
            return key;
        }

        private void LoadRouteAndRefresh()
        {
            _classCode = _progress == null ? null : _progress.ClassCode;
            _currentRoute = _routes.GetForClass(_classCode);
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
                _profileLabel.Text = "PW Quest Overlay — aguardando cliente";
                _questLabel.Text = "Abra ou selecione um cliente do Perfect World.";
                _npcLabel.Text = string.Empty;
                _locationLabel.Text = string.Empty;
                _statusLabel.Text = string.Empty;
                _currentMission = null;
                _currentStep = null;
                _stepTypeLabel.Text = string.Empty;
                UpdateActionAvailability();
                return;
            }

            string classText = string.IsNullOrWhiteSpace(_classCode) ? "classe não definida" : _classCode;
            int finished = _currentRoute.Count(m => _progress.MissionStates.ContainsKey(m.id));
            _profileLabel.Text = _characterName + " [" + classText + "]  ·  " + finished + "/" + _currentRoute.Count;

            if (_currentRoute.Count == 0)
            {
                _questLabel.Text = "Classe não identificada";
                _npcLabel.Text = "Use o ícone ao lado do relógio para selecionar WR, WB, WF, EA, EP ou MC.";
                _locationLabel.Text = "A detecção automática usa o título Personagem[CLASSE].";
                _statusLabel.Text = "Seleção manual disponível";
                _currentMission = null;
                _currentStep = null;
                _stepTypeLabel.Text = string.Empty;
                UpdateActionAvailability();
                return;
            }

            RouteStage activeStage = _orderedStages.FirstOrDefault(stage => !IsStageComplete(stage));
            if (activeStage == null)
            {
                _questLabel.Text = "Rota concluída até o nível 60";
                _npcLabel.Text = "Todas as missões da rota foram marcadas.";
                _locationLabel.Text = string.Empty;
                _statusLabel.Text = finished + "/" + _currentRoute.Count;
                _currentStep = null;
                _stepTypeLabel.Text = "FIM";
                _stepTypeLabel.BackColor = Color.FromArgb(22, 163, 74);
                UpdateActionAvailability();
                return;
            }

            _currentMission = activeStage.Mission;
            _currentStep = activeStage.Step;
            _currentStepIndex = activeStage.StepIndex;
            _currentRouteStageIndex = _orderedStages.IndexOf(activeStage);
            ApplyStepType(_currentStep.action);

            _questLabel.Text = _currentMission.name + "  ›  " + _currentStep.title;
            _npcLabel.Text = _currentStep.action;
            _locationLabel.Text = _currentStep.markerX.HasValue && _currentStep.markerY.HasValue
                ? "Marcador: " + _currentStep.markerX.Value + ", " + _currentStep.markerY.Value
                : "Sem coordenada para esta etapa";
            _statusLabel.Text = _currentMission.priority + " · Nv. " + _currentMission.expectedLevel +
                " · Etapa " + (_currentStepIndex + 1) + "/" + _currentMission.steps.Count +
                " · Ação " + (_currentRouteStageIndex + 1) + "/" + _orderedStages.Count;
            _statusLabel.ForeColor = string.Equals(_currentMission.priority, "Opcional", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(251, 191, 36)
                : Color.FromArgb(203, 213, 225);
            UpdateActionAvailability();
        }

        private void CompleteCurrent()
        {
            if (!CanChangeProgress() || _currentMission == null || _currentStep == null) return;
            _progress.MissionStates[StageKey(_currentMission, _currentStep)] = "done";
            if (MissionStagesComplete(_currentMission)) _progress.MissionStates[_currentMission.id] = "done";
            _store.Save(_state);
            _store.Log(_characterName + " concluiu etapa " + (_currentStepIndex + 1) + "/" +
                _currentMission.steps.Count + ": " + _currentMission.name);
            RefreshMissionDisplay();
        }

        private void SkipCurrent()
        {
            if (!CanChangeProgress() || _currentMission == null || _currentStep == null) return;
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
            return mission.id + "|STEP|" + step.id;
        }

        private bool IsStageComplete(RouteStage stage)
        {
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
            return _activeGame != IntPtr.Zero && _progress != null && !_state.UserHidden;
        }

        private void UpdateActionAvailability()
        {
            bool hasRoute = _progress != null && _currentRoute != null && _currentRoute.Count > 0;
            _doneButton.Enabled = hasRoute && _currentMission != null;
            _skipButton.Enabled = hasRoute && _currentMission != null;
            _previousButton.Enabled = hasRoute && _progress.MissionStates.Count > 0;
            _markerButton.Enabled = hasRoute && _currentStep != null &&
                _currentStep.markerX.HasValue && _currentStep.markerY.HasValue;
        }

        private void SetManualClass(string code)
        {
            if (_progress == null) return;
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
            if (_progress == null) return;
            if (!string.IsNullOrWhiteSpace(_windowTitleKey))
                _state.ClassAssociations.Remove(_windowTitleKey);
            _progress.ManualClass = false;
            string parsedName;
            string parsedClass;
            ParseTitle(NativeMethods.GetWindowTitle(_activeGame), out parsedName, out parsedClass);
            ReKeyCurrentProgress(parsedClass);
            _progress.ClassCode = parsedClass;
            _store.Save(_state);
            LoadRouteAndRefresh();
        }

        private void OpenProfileManager()
        {
            var titles = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (IntPtr game in NativeMethods.FindGameWindows())
            {
                string title = NativeMethods.GetWindowTitle(game).Trim();
                if (!string.IsNullOrWhiteSpace(title))
                    titles[NormalizeWindowTitle(title)] = title;
            }
            foreach (string savedTitle in _state.ClassAssociations.Keys)
                if (!titles.ContainsKey(savedTitle)) titles[savedTitle] = savedTitle;

            using (var dialog = new Form())
            using (var grid = new DataGridView())
            using (var description = new Label())
            using (var saveButton = new Button())
            using (var cancelButton = new Button())
            {
                dialog.Text = "Perfis e classes — PW Quest Overlay";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(650, 390);
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.ShowInTaskbar = false;
                dialog.TopMost = true;
                dialog.Font = new Font("Segoe UI", 9.0f);

                description.Text = "Associe o título ou nome exibido pelo PW Helper a uma classe. " +
                    "Você também pode digitar um novo título na última linha.";
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
                classColumn.Items.AddRange("Automático", "WR", "WB", "WF", "EA", "EP", "MC");
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
                    string rowKey = NormalizeWindowTitle(rowTitle);
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
            {
                dialog.Text = "Selecionar missão e etapa";
                dialog.FormBorderStyle = FormBorderStyle.FixedDialog;
                dialog.StartPosition = FormStartPosition.CenterScreen;
                dialog.ClientSize = new Size(620, 205);
                dialog.MaximizeBox = false;
                dialog.MinimizeBox = false;
                dialog.TopMost = true;
                dialog.Font = new Font("Segoe UI", 9.0f);

                description.Text = "Escolha o ponto exato da rota. Missões e etapas anteriores serão marcadas; as posteriores serão reabertas.";
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
                dialog.Controls.AddRange(new Control[] { description, missionBox, stepBox, applyButton, cancelButton });
                dialog.AcceptButton = applyButton;
                dialog.CancelButton = cancelButton;
                if (dialog.ShowDialog() != DialogResult.OK || missionBox.SelectedIndex < 0 || stepBox.SelectedIndex < 0) return;

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

            foreach (QuestMission mission in _currentRoute)
            {
                _progress.MissionStates.Remove(mission.id);
                foreach (QuestStep step in mission.steps) _progress.MissionStates.Remove(StageKey(mission, step));
            }
            for (int index = 0; index < targetIndex; index++)
                _progress.MissionStates[StageKey(_orderedStages[index].Mission, _orderedStages[index].Step)] = "done";
            foreach (QuestMission mission in _currentRoute)
                if (MissionStagesComplete(mission)) _progress.MissionStates[mission.id] = "done";
            _store.Save(_state);
            _store.Log(_characterName + " reposicionou a rota na ação global " + (targetIndex + 1) +
                ": " + targetMission.name + " / etapa " + (stepIndex + 1));
            RefreshMissionDisplay();
        }

        private void OpenMarkerCalibration()
        {
            if (_activeGame == IntPtr.Zero)
            {
                MessageBox.Show("Coloque o cliente que deseja calibrar em foco primeiro.", "PW Quest Overlay");
                return;
            }
            using (var dialog = new MarkerCalibrationForm(_activeGame, _state.Marker))
            {
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _state.Marker.Calibrated = true;
                    _store.Save(_state);
                }
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
                _state.Positions = imported.Positions;
                _state.ClassAssociations = imported.ClassAssociations;
                _state.Marker = imported.Marker;
                _state.Ocr = imported.Ocr;
                _state.PositionLocked = imported.PositionLocked;
                _state.CompactMode = imported.CompactMode;
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
            string newKey = BuildCharacterKey(_characterName, classCode);
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

        private async void SelectAndTestOcr()
        {
            if (!CanStartOcr()) return;
            _ocrBusy = true;
            Hide();
            try
            {
                Rectangle initial = GetSavedOcrRegion();
                using (var selector = new OcrRegionSelectionForm(_activeGame, initial))
                {
                    if (selector.ShowDialog() != DialogResult.OK) return;
                    SaveOcrRegion(selector.SelectedClientRegion);
                    await ScanOcrRegion(selector.SelectedClientRegion);
                }
            }
            catch (Exception error)
            {
                _store.Log("Erro no OCR: " + error.GetType().Name + ": " + error.Message);
                MessageBox.Show(error.Message, "OCR experimental", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _ocrBusy = false;
                RestoreOverlayAfterOcr();
            }
        }

        private async void TestSavedOcrRegion()
        {
            if (!CanStartOcr()) return;
            Rectangle region = GetSavedOcrRegion();
            if (region.Width < 20 || region.Height < 20)
            {
                MessageBox.Show(
                    "Use primeiro 'Selecionar área e testar...'.",
                    "OCR experimental",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            _ocrBusy = true;
            Hide();
            try
            {
                await ScanOcrRegion(region);
            }
            catch (Exception error)
            {
                _store.Log("Erro no OCR: " + error.GetType().Name + ": " + error.Message);
                MessageBox.Show(error.Message, "OCR experimental", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _ocrBusy = false;
                RestoreOverlayAfterOcr();
            }
        }

        private bool CanStartOcr()
        {
            if (_ocrBusy) return false;
            if (_activeGame == IntPtr.Zero || !NativeMethods.IsWindow(_activeGame))
            {
                MessageBox.Show(
                    "Coloque um cliente do Perfect World em foco antes de iniciar o teste.",
                    "OCR experimental",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return false;
            }
            string reason;
            if (!QuestOcrService.IsAvailable(out reason))
            {
                MessageBox.Show(
                    "O Tesseract OCR não está disponível.\r\n\r\n" + reason,
                    "OCR experimental",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                return false;
            }
            return true;
        }

        private async Task ScanOcrRegion(
            Rectangle region,
            bool showResult = true,
            bool bringGameToFront = true)
        {
            IntPtr targetWindow = _activeGame;
            string targetCharacterKey = _characterKey;
            if (bringGameToFront)
            {
                NativeMethods.SetForegroundWindow(targetWindow);
                await Task.Delay(180);
            }
            string directory = Path.GetDirectoryName(_store.LogPath);
            QuestOcrResult result = await QuestOcrService.ScanAsync(targetWindow, region, directory);
            if (targetWindow != _activeGame ||
                !string.Equals(targetCharacterKey, _characterKey, StringComparison.OrdinalIgnoreCase))
            {
                _store.Log("OCR descartado: o cliente ativo mudou durante a captura.");
                return;
            }
            result.Text = InterpretOcrText(result.Text);
            string stabilitySummary = UpdateOcrStability(result);
            string comparisonSummary = BuildOcrMatchSummary(result.Text);
            string advanceSummary = ApplyConfirmedOcrProgress(result);
            string matchSummary = comparisonSummary + " " + stabilitySummary + " " + advanceSummary;
            _store.Log(string.Format(
                "OCR concluído em {0} ms, caracteres={1}, região={2}",
                result.ElapsedMilliseconds,
                result.Text == null ? 0 : result.Text.Length,
                region));
            if (showResult)
            {
                using (var resultForm = new OcrResultForm(result, matchSummary))
                    resultForm.ShowDialog();
            }
        }

        private async void BeginAutomaticOcrScan()
        {
            if (_ocrBusy || !_state.Ocr.AutoAdvanceEnabled ||
                _activeGame == IntPtr.Zero || !NativeMethods.IsWindow(_activeGame))
                return;
            Rectangle region = GetSavedOcrRegion();
            if (region.Width < 20 || region.Height < 20) return;

            _ocrBusy = true;
            bool hiddenForCapture = OverlayIntersectsOcrRegion(region) && Visible;
            if (hiddenForCapture) Hide();
            try
            {
                await ScanOcrRegion(region, false, false);
            }
            catch (Exception error)
            {
                _store.Log("Erro no OCR automático: " + error.GetType().Name + ": " + error.Message);
            }
            finally
            {
                _ocrBusy = false;
                _nextAutomaticOcrAt = DateTime.UtcNow.AddSeconds(2);
                if (hiddenForCapture) RestoreOverlayAfterOcr();
            }
        }

        private bool OverlayIntersectsOcrRegion(Rectangle clientRegion)
        {
            if (_activeGame == IntPtr.Zero) return false;
            var origin = new NativeMethods.POINT { X = 0, Y = 0 };
            if (!NativeMethods.ClientToScreen(_activeGame, ref origin)) return false;
            Rectangle screenRegion = new Rectangle(
                origin.X + clientRegion.X,
                origin.Y + clientRegion.Y,
                clientRegion.Width,
                clientRegion.Height);
            return Bounds.IntersectsWith(screenRegion);
        }

        private void SaveOcrRegion(Rectangle region)
        {
            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(_activeGame, out client)) return;
            _state.Ocr.Calibrated = true;
            _state.Ocr.X = region.X;
            _state.Ocr.Y = region.Y;
            _state.Ocr.Width = region.Width;
            _state.Ocr.Height = region.Height;
            _state.Ocr.ReferenceWidth = client.Width;
            _state.Ocr.ReferenceHeight = client.Height;
            _store.Save(_state);
        }

        private Rectangle GetSavedOcrRegion()
        {
            if (_state.Ocr == null || !_state.Ocr.Calibrated ||
                _state.Ocr.ReferenceWidth <= 0 || _state.Ocr.ReferenceHeight <= 0)
                return Rectangle.Empty;

            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(_activeGame, out client))
                return Rectangle.Empty;
            double scaleX = (double)client.Width / _state.Ocr.ReferenceWidth;
            double scaleY = (double)client.Height / _state.Ocr.ReferenceHeight;
            return Rectangle.FromLTRB(
                (int)Math.Round(_state.Ocr.X * scaleX),
                (int)Math.Round(_state.Ocr.Y * scaleY),
                (int)Math.Round((_state.Ocr.X + _state.Ocr.Width) * scaleX),
                (int)Math.Round((_state.Ocr.Y + _state.Ocr.Height) * scaleY));
        }

        private string BuildOcrMatchSummary(string recognizedText)
        {
            if (_currentMission == null || _currentStep == null)
                return "Sem etapa de rota ativa para comparar.";

            string expected = _currentMission.name + " " + _currentStep.title;
            string normalizedOcr = NormalizeOcrText(recognizedText);
            string[] expectedWords = NormalizeOcrText(expected)
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Where(word => word.Length >= 3)
                .Distinct()
                .ToArray();
            int found = expectedWords.Count(word => normalizedOcr.Contains(word));
            int confidence = expectedWords.Length == 0
                ? 0
                : (int)Math.Round(100.0 * found / expectedWords.Length);
            string summary = string.Format(
                "Comparação com a etapa atual: {0}% ({1}/{2} palavras).",
                confidence,
                found,
                expectedWords.Length);
            int? expectedTotal = GetExpectedCounterTotal();
            if (!expectedTotal.HasValue) return summary;

            Match counter = Regex.Match(
                recognizedText ?? string.Empty,
                @"\(\s*(?<current>\d{1,4})\s*/\s*(?<total>\d{1,4})\s*\)");
            if (!counter.Success)
                return summary + " Contador esperado: ?/" + expectedTotal.Value + ".";
            return summary + " Contador lido: " + counter.Groups["current"].Value + "/" +
                counter.Groups["total"].Value + "; esperado: " + expectedTotal.Value + ".";
        }

        private string UpdateOcrStability(QuestOcrResult result)
        {
            string profileKey = !string.IsNullOrWhiteSpace(_characterKey)
                ? _characterKey
                : "hwnd:" + _activeGame.ToInt64().ToString("X");
            string signature = BuildRelevantOcrSignature(result.Text);
            DateTime now = DateTime.UtcNow;
            OcrStabilityState state;
            if (!_ocrStability.TryGetValue(profileKey, out state))
            {
                state = new OcrStabilityState();
                _ocrStability[profileKey] = state;
            }

            bool sameReading = !string.IsNullOrWhiteSpace(signature) &&
                string.Equals(state.Signature, signature, StringComparison.Ordinal) &&
                now - state.LastReadAt <= TimeSpan.FromSeconds(30);
            state.ConsecutiveReadings = sameReading
                ? Math.Min(2, state.ConsecutiveReadings + 1)
                : 1;
            state.Signature = signature;
            state.LastReadAt = now;
            result.StabilityCount = state.ConsecutiveReadings;
            result.StateConfirmed = state.ConsecutiveReadings >= 2;

            string temporal = result.CaptureFrameCount + " quadros × " +
                result.CaptureIntervalMilliseconds + " ms";
            return result.StateConfirmed
                ? "Estado confirmado (2/2; " + temporal + ")."
                : "Aguardando segunda leitura igual (1/2; " + temporal + ").";
        }

        private string BuildRelevantOcrSignature(string recognizedText)
        {
            var parts = new List<string>();
            foreach (OcrQuestBlock block in ParseOcrQuestBlocks(recognizedText))
            {
                string alias = FindBestKnownAlias(block.ObservedTitle);
                if (alias == null) continue;
                string normalizedAlias = NormalizeOcrText(alias);
                parts.Add("entry:" + normalizedAlias);
                if (block.Completed)
                    parts.Add("entry:" + normalizedAlias + ":status:concluida");
                if (block.TotalCount.HasValue)
                    parts.Add("entry:" + normalizedAlias + ":counter:" +
                        (block.CurrentCount.HasValue ? block.CurrentCount.Value.ToString() : "?") +
                        "/" + block.TotalCount.Value);
            }
            return string.Join("|", parts.Distinct().OrderBy(part => part));
        }

        private string ApplyConfirmedOcrProgress(QuestOcrResult result)
        {
            if (!_state.Ocr.AutoAdvanceEnabled)
                return "Avanço automático desativado (Ctrl + Shift + A).";
            if (!result.StateConfirmed)
                return "Nenhuma etapa alterada antes da confirmação 2/2.";
            if (_progress == null || _currentMission == null || _currentStep == null)
                return "Sem etapa ativa para avançar.";

            List<OcrQuestBlock> blocks = ParseOcrQuestBlocks(result.Text);
            RecordVisibleStepObservations(blocks);
            var advances = new List<string>();
            for (int attempt = 0; attempt < 8; attempt++)
            {
                string action = _currentStep.action ?? string.Empty;
                bool acceptStage = IsAcceptStage(action);
                bool turnInStage = IsTurnInStage(_currentMission, _currentStep);
                bool talkStage = IsTalkStage(action) && !turnInStage;
                OcrQuestBlock missionBlock = FindBestBlock(
                    blocks,
                    GetOcrAliases(_currentMission, _currentStep, true));
                OcrQuestBlock stepBlock = FindBestBlock(
                    blocks,
                    GetOcrAliases(_currentMission, _currentStep, false));

                if (turnInStage || talkStage)
                {
                    OcrStepObservation observation = GetStepObservation(
                        _currentMission, _currentStep);
                    if (!observation.VisibleInLastConfirmedReading &&
                        turnInStage && _currentStepIndex > 0)
                    {
                        QuestStep previousStep = _currentMission.steps[_currentStepIndex - 1];
                        string previousState;
                        if (_progress.MissionStates.TryGetValue(
                            StageKey(_currentMission, previousStep), out previousState) &&
                            string.Equals(previousState, "done-ocr", StringComparison.OrdinalIgnoreCase))
                            observation.VisibleInLastConfirmedReading = true;
                    }
                    bool stepStillVisible = stepBlock != null ||
                        (turnInStage && missionBlock != null);
                    if (stepStillVisible)
                    {
                        observation.VisibleInLastConfirmedReading = true;
                        break;
                    }
                    if (!observation.VisibleInLastConfirmedReading || blocks.Count == 0)
                        break;

                    string completedMissionName = _currentMission.name;
                    int completedStepNumber = _currentStepIndex + 1;
                    MarkCurrentStageFromOcr(
                        "submissão foi vista e depois desapareceu do quest log");
                    advances.Add(completedMissionName + " etapa " + completedStepNumber +
                        " (conversa/entrega confirmada por presença e desaparecimento)");
                    observation.VisibleInLastConfirmedReading = false;
                    continue;
                }

                if (acceptStage)
                {
                    if (!HasAcceptedMissionEvidence(_currentMission, blocks)) break;
                    string acceptedMission = _currentMission.name;
                    int acceptedStep = _currentStepIndex + 1;
                    MarkCurrentStageFromOcr("missão ou submissão apareceu no quest log");
                    advances.Add(acceptedMission + " etapa " + acceptedStep +
                        " (missão/submissão visível)");
                    continue;
                }

                int? expectedTotal = GetExpectedCounterTotal();
                int objectiveCountRequirements = Regex.Matches(
                    action,
                    @"\(\s*\d{1,4}\s*\)").Count;
                bool counterFinished = expectedTotal.HasValue &&
                    objectiveCountRequirements == 1 &&
                    missionBlock != null && missionBlock.CurrentCount.HasValue &&
                    missionBlock.TotalCount.HasValue &&
                    missionBlock.CurrentCount.Value >= expectedTotal.Value &&
                    missionBlock.TotalCount.Value == expectedTotal.Value;
                bool objectiveFinished = counterFinished;
                if (!objectiveFinished) break;

                string missionName = _currentMission.name;
                int stepNumber = _currentStepIndex + 1;
                string evidence = counterFinished
                    ? "contador " + missionBlock.CurrentCount.Value + "/" +
                        missionBlock.TotalCount.Value
                    : "objetivo numérico confirmado";
                MarkCurrentStageFromOcr(evidence);
                advances.Add(missionName + " etapa " + stepNumber + " (" + evidence + ")");
            }

            return advances.Count == 0
                ? "Nenhuma etapa tinha evidência suficiente para avanço automático."
                : "Avanço automático: " + string.Join("; ", advances) + ".";
        }

        private List<OcrQuestBlock> ParseOcrQuestBlocks(string recognizedText)
        {
            Dictionary<string, QuestMission> knownMissions = _currentRoute
                .GroupBy(mission => NormalizeOcrText(mission.name))
                .ToDictionary(group => group.Key, group => group.First(),
                    StringComparer.OrdinalIgnoreCase);
            var blocks = new List<OcrQuestBlock>();
            OcrQuestBlock activeBlock = null;
            string[] lines = (recognizedText ?? string.Empty)
                .Replace("\r\n", "\n")
                .Split('\n');
            foreach (string line in lines)
            {
                Match numberedTitle = Regex.Match(line, @"^\s*\d+\s+(?<title>.+?)\s*$");
                string observedTitle = numberedTitle.Success
                    ? numberedTitle.Groups["title"].Value.Trim()
                    : line.TrimStart(' ', '\t', '&', 'S', 'E', '-').Trim();
                bool knownUnnumberedTitle = !numberedTitle.Success &&
                    FindBestKnownAlias(observedTitle) != null;
                if (numberedTitle.Success || knownUnnumberedTitle)
                {
                    QuestMission mission;
                    knownMissions.TryGetValue(
                        NormalizeOcrText(observedTitle),
                        out mission);
                    activeBlock = new OcrQuestBlock
                    {
                        Mission = mission,
                        ObservedTitle = observedTitle
                    };
                    blocks.Add(activeBlock);
                    continue;
                }
                if (activeBlock == null) continue;

                string normalizedLine = NormalizeOcrText(line);
                if (normalizedLine.Contains("missao concluida"))
                    activeBlock.Completed = true;
                Match counter = Regex.Match(
                    line,
                    @"\(\s*(?<current>\d{1,4}|\?)\s*/\s*(?<total>\d{1,4})\s*\)");
                int current;
                int total;
                if (counter.Success &&
                    int.TryParse(counter.Groups["total"].Value, out total))
                {
                    activeBlock.TotalCount = total;
                    if (int.TryParse(counter.Groups["current"].Value, out current))
                        activeBlock.CurrentCount = current;
                }
            }
            return blocks;
        }

        private List<string> GetOcrAliases(
            QuestMission mission,
            QuestStep step,
            bool includeMission)
        {
            var aliases = new List<string>();
            if (includeMission && mission != null && !string.IsNullOrWhiteSpace(mission.name))
                aliases.Add(mission.name);
            if (step != null && !string.IsNullOrWhiteSpace(step.title) &&
                (mission == null || !string.Equals(step.title, mission.name,
                    StringComparison.OrdinalIgnoreCase)))
                aliases.Add(step.title);
            if (step != null) aliases.AddRange(GetDerivedStepAliases(step.action));
            return aliases
                .Where(alias => !string.IsNullOrWhiteSpace(alias))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static IEnumerable<string> GetDerivedStepAliases(string action)
        {
            string normalized = NormalizeOcrText(action);
            if (normalized.Contains("ferreiro")) yield return "Visite o Ferreiro";
            if (normalized.Contains("comerciante")) yield return "Visite o Negociante";
            if (normalized.Contains("boticario")) yield return "Visite o Boticário";
            if (normalized.Contains("artesa") || normalized.Contains("artesao"))
                yield return "Visite o Artesão";
            if (normalized.Contains("alfaiate")) yield return "Visite o Alfaiate";
            if (normalized.Contains("ancia") || normalized.Contains("anciao"))
            {
                yield return "Visitando o Ancião";
                yield return "Informar ao Ancião";
            }
        }

        private OcrQuestBlock FindBestBlock(
            IEnumerable<OcrQuestBlock> blocks,
            IEnumerable<string> aliases)
        {
            OcrQuestBlock bestBlock = null;
            double bestSimilarity = 0.0;
            foreach (OcrQuestBlock block in blocks)
            {
                string observed = NormalizeOcrText(block.ObservedTitle);
                foreach (string alias in aliases)
                {
                    double similarity = CalculateBestTitleSimilarity(
                        observed,
                        NormalizeOcrText(alias));
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestBlock = block;
                    }
                }
            }
            return bestSimilarity >= 0.72 ? bestBlock : null;
        }

        private string FindBestKnownAlias(string observedTitle)
        {
            string bestAlias = null;
            double bestSimilarity = 0.0;
            foreach (QuestMission mission in _currentRoute)
            {
                foreach (QuestStep step in mission.steps)
                {
                    foreach (string alias in GetOcrAliases(mission, step, true))
                    {
                        double similarity = CalculateBestTitleSimilarity(
                            NormalizeOcrText(observedTitle),
                            NormalizeOcrText(alias));
                        if (similarity > bestSimilarity)
                        {
                            bestSimilarity = similarity;
                            bestAlias = alias;
                        }
                    }
                }
            }
            return bestSimilarity >= 0.72 ? bestAlias : null;
        }

        private bool HasAcceptedMissionEvidence(
            QuestMission mission,
            List<OcrQuestBlock> blocks)
        {
            if (FindBestBlock(blocks, new[] { mission.name }) != null) return true;
            List<QuestStep> objectiveSteps = mission.steps
                .Where(step => !IsAcceptStage(step.action) && !IsTurnInStage(mission, step))
                .ToList();
            int matched = objectiveSteps.Count(step =>
                FindBestBlock(blocks, GetOcrAliases(mission, step, false)) != null);
            int distinctAliasCount = objectiveSteps
                .SelectMany(step => GetOcrAliases(mission, step, false))
                .Select(NormalizeOcrText)
                .Distinct()
                .Count();
            return matched >= (distinctAliasCount >= 3 ? 2 : 1);
        }

        private bool HasMissionEvidence(QuestMission mission, List<OcrQuestBlock> blocks)
        {
            if (FindBestBlock(blocks, new[] { mission.name }) != null) return true;
            List<QuestStep> evidenceSteps = mission.steps
                .Where(step => !IsAcceptStage(step.action))
                .ToList();
            int matched = evidenceSteps.Count(step =>
                FindBestBlock(blocks, GetOcrAliases(mission, step, false)) != null);
            int distinctAliasCount = evidenceSteps
                .SelectMany(step => GetOcrAliases(mission, step, false))
                .Select(NormalizeOcrText)
                .Distinct()
                .Count();
            return matched >= (distinctAliasCount >= 3 ? 2 : 1);
        }

        private bool HasAnyKnownRouteEvidence(List<OcrQuestBlock> blocks)
        {
            return blocks.Any(block => FindBestKnownAlias(block.ObservedTitle) != null);
        }

        private OcrStepObservation GetStepObservation(QuestMission mission, QuestStep step)
        {
            string profileKey = !string.IsNullOrWhiteSpace(_characterKey)
                ? _characterKey
                : "hwnd:" + _activeGame.ToInt64().ToString("X");
            string key = profileKey + "|" + StageKey(mission, step);
            OcrStepObservation observation;
            if (!_ocrStepObservations.TryGetValue(key, out observation))
            {
                observation = new OcrStepObservation();
                _ocrStepObservations[key] = observation;
            }
            return observation;
        }

        private void RecordVisibleStepObservations(List<OcrQuestBlock> blocks)
        {
            foreach (QuestMission mission in _currentRoute)
            {
                foreach (QuestStep step in mission.steps)
                {
                    bool turnIn = IsTurnInStage(mission, step);
                    bool conversationOrTurnIn = IsTalkStage(step.action) || turnIn;
                    if (!conversationOrTurnIn) continue;
                    OcrQuestBlock block = FindBestBlock(
                        blocks,
                        GetOcrAliases(mission, step, turnIn));
                    if (block != null)
                        GetStepObservation(mission, step).VisibleInLastConfirmedReading = true;
                }
            }
        }

        private void MarkCurrentStageFromOcr(string evidence)
        {
            QuestMission mission = _currentMission;
            QuestStep step = _currentStep;
            int stepIndex = _currentStepIndex;
            _progress.MissionStates[StageKey(mission, step)] = "done-ocr";
            if (MissionStagesComplete(mission))
                _progress.MissionStates[mission.id] = "done-ocr";
            _store.Save(_state);
            _store.Log(_characterName + " avançou automaticamente por OCR na etapa " +
                (stepIndex + 1) + "/" + mission.steps.Count + ": " + mission.name +
                "; evidência=" + evidence);
            RefreshMissionDisplay();
        }

        private static bool IsAcceptStage(string action)
        {
            return Regex.IsMatch(
                action ?? string.Empty,
                @"\b(?:aceite|pegue\s+a\s+miss[aã]o|receba\s+a\s+miss[aã]o|inicie\s+a\s+miss[aã]o)\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool IsTalkStage(string action)
        {
            return Regex.IsMatch(
                action ?? string.Empty,
                @"\bfale\s+com\b",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        private static bool IsTurnInStage(QuestMission mission, QuestStep step)
        {
            string action = step == null ? string.Empty : step.action ?? string.Empty;
            if (Regex.IsMatch(
                action,
                @"\b(?:entregue|reporte|retorne|volte)\b",
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

        private void ApplyMemoryProgress()
        {
            if (_memoryMonitor == null || _taskCatalog == null ||
                _activeGame == IntPtr.Zero || _progress == null ||
                _currentMission == null || _currentStep == null)
                return;

            int processId = NativeMethods.GetProcessId(_activeGame);
            QuestMemoryClientState client = _memoryMonitor.Find(processId);
            if (client == null || !client.Ready || client.Snapshot == null) return;

            for (int attempt = 0; attempt < 8 && _currentStep != null; attempt++)
            {
                uint taskId;
                if (!TryResolveUniqueTaskId(
                    _currentMission, _currentStep, out taskId))
                {
                    LogMemoryDiagnostic("sem ID único: " + _currentMission.name +
                        " / " + _currentStep.title);
                    return;
                }

                ActiveQuestMemoryEntry active = client.FindActive(taskId);
                string action = _currentStep.action ?? string.Empty;
                bool accepted = IsAcceptStage(action) && active != null;
                TaskCatalogEntry currentTask = _taskCatalog.Find(taskId);
                string killEvidence = string.Empty;
                bool killsComplete = currentTask != null &&
                    currentTask.TryGetKillProgress(active, action, out killEvidence);
                bool taskItemsComplete = IsCollectionStage(action) &&
                    _taskCatalog.AreTaskItemsComplete(taskId, client.Snapshot);
                bool nextUsesSameTask = false;
                bool nextTaskResolved = false;
                uint nextTaskId = 0;
                if (_currentStepIndex + 1 < _currentMission.steps.Count)
                {
                    nextTaskResolved = TryResolveUniqueTaskId(
                        _currentMission,
                        _currentMission.steps[_currentStepIndex + 1],
                        out nextTaskId);
                    nextUsesSameTask = nextTaskResolved && nextTaskId == taskId;
                }

                bool waitsForDisappearance = IsTurnInStage(
                    _currentMission, _currentStep) ||
                    (IsTalkStage(action) && !nextUsesSameTask);
                bool disappeared = waitsForDisappearance &&
                    client.TryConsumeDisappearance(taskId);
                TaskCatalogEntry nextTask = nextTaskResolved
                    ? _taskCatalog.Find(nextTaskId) : null;
                bool successorActive = waitsForDisappearance && active == null &&
                    nextTaskResolved && nextTaskId != taskId &&
                    client.FindActive(nextTaskId) != null &&
                    currentTask != null && nextTask != null &&
                    currentTask.TopId == nextTask.TopId;
                if (!accepted && !taskItemsComplete && !killsComplete &&
                    !disappeared && !successorActive)
                {
                    LogMemoryDiagnostic("aguardando: " + _currentMission.name +
                        " / " + _currentStep.title + " ID=" + taskId +
                        " ativo=" + (active != null) +
                        " espera-sumir=" + waitsForDisappearance +
                        " próximo=" + (nextTaskResolved ? nextTaskId.ToString() : "-") +
                        " próximo-ativo=" + (nextTaskResolved &&
                            client.FindActive(nextTaskId) != null) +
                        " itens=" + TaskItemEvidence(currentTask, client.Snapshot) +
                        " abates=" + killEvidence);
                    return;
                }

                string evidence = accepted
                    ? "ID " + taskId + " apareceu na lista ativa"
                    : killsComplete
                        ? "abates completos: " + killEvidence
                    : taskItemsComplete
                        ? "itens de missão completos: " +
                            TaskItemEvidence(currentTask, client.Snapshot)
                    : disappeared
                        ? "ID " + taskId + " foi observado e depois desapareceu"
                        : "a próxima submissão da mesma cadeia, ID " +
                            nextTaskId + ", já está ativa";
                MarkCurrentStageFromMemory(evidence);
            }
        }

        private static string TaskItemEvidence(
            TaskCatalogEntry task,
            QuestMemorySnapshot snapshot)
        {
            if (task == null || task.RequiredTaskItems == null ||
                task.RequiredTaskItems.Count == 0 || snapshot == null)
                return "-";
            return string.Join(", ", task.RequiredTaskItems.Select(requirement =>
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
            taskId = 0;
            if (step == null || string.IsNullOrWhiteSpace(step.title)) return false;
            List<TaskCatalogEntry> matches = _taskCatalog.FindByName(step.title);
            List<TaskCatalogEntry> missionMatches = mission == null
                ? new List<TaskCatalogEntry>()
                : _taskCatalog.FindByName(mission.name);
            HashSet<uint> topIds = new HashSet<uint>(
                missionMatches.Select(entry => entry.TopId));
            if (topIds.Count > 0)
            {
                List<TaskCatalogEntry> sameChain = matches
                    .Where(entry => topIds.Contains(entry.TopId))
                    .ToList();
                if (sameChain.Count > 0) matches = sameChain;
            }
            if (matches.Count != 1) return false;
            taskId = matches[0].Id;
            return taskId != 0;
        }

        private void MarkCurrentStageFromMemory(string evidence)
        {
            QuestMission mission = _currentMission;
            QuestStep step = _currentStep;
            int stepIndex = _currentStepIndex;
            _progress.MissionStates[StageKey(mission, step)] = "done-memory";
            if (MissionStagesComplete(mission))
                _progress.MissionStates[mission.id] = "done-memory";
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

        private void SetOcrAutoAdvance(bool enabled, bool notify)
        {
            _state.Ocr.AutoAdvanceEnabled = enabled;
            _ocrAutoAdvanceItem.Checked = enabled;
            _store.Save(_state);
            _nextAutomaticOcrAt = DateTime.UtcNow;
            _store.Log("Sincronização e avanço automáticos por OCR " +
                (enabled ? "ativados" : "desativados"));
            if (!notify) return;
            _trayIcon.ShowBalloonTip(
                1800,
                "PW Quest Overlay",
                "Sincronização e avanço por OCR " + (enabled ? "ATIVADOS" : "DESATIVADOS"),
                enabled ? ToolTipIcon.Info : ToolTipIcon.Warning);
        }

        private string InterpretOcrText(string recognizedText)
        {
            if (string.IsNullOrWhiteSpace(recognizedText)) return string.Empty;
            List<string> knownTitles = _currentRoute
                .SelectMany(mission => new[] { mission.name }
                    .Concat(mission.steps.Select(step => step.title)))
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            string[] lines = recognizedText.Replace("\r\n", "\n").Split('\n');
            for (int index = 0; index < lines.Length; index++)
            {
                Match numberedTitle = Regex.Match(lines[index], @"^(?<prefix>\s*\d+\s+)(?<title>.+?)\s*$");
                if (!numberedTitle.Success) continue;

                string observed = numberedTitle.Groups["title"].Value;
                string normalizedObserved = NormalizeOcrText(observed);
                string bestTitle = null;
                double bestSimilarity = 0.0;
                foreach (string title in knownTitles)
                {
                    double similarity = CalculateBestTitleSimilarity(
                        normalizedObserved,
                        NormalizeOcrText(title));
                    if (similarity > bestSimilarity)
                    {
                        bestSimilarity = similarity;
                        bestTitle = title;
                    }
                }
                if (bestTitle != null && bestSimilarity >= 0.78)
                    lines[index] = numberedTitle.Groups["prefix"].Value + bestTitle;
            }

            string interpreted = string.Join(Environment.NewLine, lines);
            int? expectedTotal = GetExpectedCounterTotal();
            if (expectedTotal.HasValue && !Regex.IsMatch(
                interpreted,
                @"\(\s*\d{1,4}\s*/\s*\d{1,4}\s*\)"))
            {
                interpreted = Regex.Replace(
                    interpreted,
                    @"\((?![^\r\n)]*,)[^\r\n)]*\d[^\r\n)]*\)",
                    "(?/" + expectedTotal.Value + ")",
                    RegexOptions.None,
                    TimeSpan.FromMilliseconds(100));
            }
            return interpreted;
        }

        private int? GetExpectedCounterTotal()
        {
            if (_currentStep == null || string.IsNullOrWhiteSpace(_currentStep.action))
                return null;
            Match match = Regex.Match(
                _currentStep.action,
                @"\b(?:matar|derrotar|pegar|coletar|obter|recolher)\b[^()\r\n]{0,50}\(\s*(?<total>\d{1,4})\s*\)",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
            int total;
            return match.Success && int.TryParse(match.Groups["total"].Value, out total)
                ? (int?)total
                : null;
        }

        private static double CalculateSimilarity(string left, string right)
        {
            if (string.Equals(left, right, StringComparison.Ordinal)) return 1.0;
            if (string.IsNullOrEmpty(left) || string.IsNullOrEmpty(right)) return 0.0;
            int[] previous = Enumerable.Range(0, right.Length + 1).ToArray();
            int[] current = new int[right.Length + 1];
            for (int leftIndex = 1; leftIndex <= left.Length; leftIndex++)
            {
                current[0] = leftIndex;
                for (int rightIndex = 1; rightIndex <= right.Length; rightIndex++)
                {
                    int substitution = previous[rightIndex - 1] +
                        (left[leftIndex - 1] == right[rightIndex - 1] ? 0 : 1);
                    current[rightIndex] = Math.Min(
                        Math.Min(previous[rightIndex] + 1, current[rightIndex - 1] + 1),
                        substitution);
                }
                int[] swap = previous;
                previous = current;
                current = swap;
            }
            int maximumLength = Math.Max(left.Length, right.Length);
            return 1.0 - (double)previous[right.Length] / maximumLength;
        }

        private static double CalculateBestTitleSimilarity(string observed, string expected)
        {
            double best = CalculateSimilarity(observed, expected);
            if (string.IsNullOrWhiteSpace(observed) || string.IsNullOrWhiteSpace(expected))
                return best;
            if (observed.Contains(expected)) return 1.0;

            string[] observedWords = observed.Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            string[] expectedWords = expected.Split(
                new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (observedWords.Length <= expectedWords.Length) return best;
            for (int start = 0; start + expectedWords.Length <= observedWords.Length; start++)
            {
                string window = string.Join(" ",
                    observedWords.Skip(start).Take(expectedWords.Length));
                best = Math.Max(best, CalculateSimilarity(window, expected));
            }
            return best;
        }

        private static string NormalizeOcrText(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;
            string decomposed = value.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(decomposed.Length);
            foreach (char character in decomposed)
            {
                UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
                if (category == UnicodeCategory.NonSpacingMark) continue;
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
            }
            return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
        }

        private void RestoreOverlayAfterOcr()
        {
            if (_state.UserHidden || _activeGame == IntPtr.Zero || !NativeMethods.IsWindow(_activeGame))
                return;
            NativeMethods.ShowWindow(Handle, NativeMethods.SW_SHOWNOACTIVATE);
            RaiseOverlayStack();
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
                    0, 0, 0, 0, flags);
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
            Rectangle bounds = _currentScreen.Bounds;
            int x = Math.Max(bounds.Left, Math.Min(Location.X, bounds.Right - Width));
            int y = Math.Max(bounds.Top, Math.Min(Location.Y, bounds.Bottom - Height));
            Location = new Point(x, y);
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
            bool ocrSelect = NativeMethods.RegisterHotKey(Handle, HotkeyOcrSelect,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, NativeMethods.VK_O);
            bool ocrScan = NativeMethods.RegisterHotKey(Handle, HotkeyOcrScan,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT, NativeMethods.VK_O);
            bool ocrAutoAdvance = NativeMethods.RegisterHotKey(Handle, HotkeyOcrAutoAdvance,
                NativeMethods.MOD_CONTROL | NativeMethods.MOD_SHIFT, NativeMethods.VK_A);
            _markerHotkeyItem.Text = "Atalho do marcador: " + markerShortcut;
            _ocrHotkeyItem.Text = ocrSelect && ocrScan
                ? "OCR: Ctrl + Shift + O / Ctrl + Alt + O  ·  Memória: Ctrl + Shift + A"
                : "Atalhos OCR indisponíveis; use este menu";
            _store.Log("Atalhos registrados: concluir=" + done + " voltar=" + previous +
                " pular=" + skip + " marcador=" + markerShortcut +
                " ocr-selecionar=" + ocrSelect + " ocr-ler=" + ocrScan +
                " memoria-auto=" + ocrAutoAdvance);
        }

        private void UnregisterHotkeys()
        {
            if (!IsHandleCreated) return;
            NativeMethods.UnregisterHotKey(Handle, HotkeyDone);
            NativeMethods.UnregisterHotKey(Handle, HotkeyPrevious);
            NativeMethods.UnregisterHotKey(Handle, HotkeySkip);
            NativeMethods.UnregisterHotKey(Handle, HotkeyMarker);
            NativeMethods.UnregisterHotKey(Handle, HotkeyOcrSelect);
            NativeMethods.UnregisterHotKey(Handle, HotkeyOcrScan);
            NativeMethods.UnregisterHotKey(Handle, HotkeyOcrAutoAdvance);
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (_allowExit) return;
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
