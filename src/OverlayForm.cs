using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class OverlayForm : Form
    {
        private const int HotkeyDone = 1001;
        private const int HotkeyPrevious = 1002;
        private const int HotkeySkip = 1003;
        private const int HotkeyMarker = 1004;

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
        private readonly Button _hideButton;
        private readonly NotifyIcon _trayIcon;
        private readonly ContextMenuStrip _trayMenu;
        private readonly ToolStripMenuItem _showMenuItem;
        private readonly ToolStripMenuItem _lockMenuItem;
        private readonly ToolStripMenuItem _classMenuItem;
        private readonly ToolStripMenuItem _markerHotkeyItem;
        private readonly Dictionary<IntPtr, DateTime> _titleRefresh;

        private IntPtr _activeGame;
        private DateTime _helperNeutralUntil;
        private DateTime _nextRaiseAt;
        private string _characterKey;
        private string _windowTitleKey;
        private string _characterName;
        private string _classCode;
        private CharacterProgress _progress;
        private List<QuestMission> _currentRoute;
        private QuestMission _currentMission;
        private QuestStep _currentStep;
        private int _currentStepIndex;
        private Screen _currentScreen;
        private string _currentMonitorKey;
        private bool _dragging;
        private Point _dragCursorStart;
        private Point _dragFormStart;
        private bool _allowExit;
        private bool _markerBusy;

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
            _titleRefresh = new Dictionary<IntPtr, DateTime>();

            Text = "PW Quest Overlay";
            Name = "PWQuestOverlay";
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            ClientSize = new Size(620, 210);
            MinimumSize = ClientSize;
            MaximumSize = ClientSize;
            BackColor = Color.FromArgb(17, 24, 39);
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
                BackColor = Color.FromArgb(32, 74, 112),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right
            };

            _profileLabel = new Label
            {
                Location = new Point(10, 6),
                Size = new Size(285, 19),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.0f, FontStyle.Bold),
                Text = "PW Quest Overlay — aguardando cliente",
                AutoEllipsis = true
            };

            _profilesButton = MakeTopButton("Perfis", 294, 65);
            _stagesButton = MakeTopButton("Etapas", 363, 72);
            _lockButton = MakeTopButton("Mover", 439, 96);
            _hideButton = MakeTopButton("×", 580, 30);
            _hideButton.Font = new Font("Segoe UI", 11.0f, FontStyle.Bold);

            _questLabel = new Label
            {
                Location = new Point(12, 38),
                Size = new Size(596, 34),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 13.0f, FontStyle.Bold),
                Text = "Abra ou selecione um cliente do Perfect World.",
                AutoEllipsis = true
            };

            _npcLabel = new Label
            {
                Location = new Point(94, 75),
                Size = new Size(514, 45),
                ForeColor = Color.FromArgb(226, 232, 240),
                Font = new Font("Segoe UI", 10.0f, FontStyle.Regular),
                AutoEllipsis = true
            };

            _stepTypeLabel = new Label
            {
                Location = new Point(12, 78),
                Size = new Size(74, 24),
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font("Segoe UI", 8.0f, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(71, 85, 105)
            };

            _locationLabel = new Label
            {
                Location = new Point(12, 122),
                Size = new Size(596, 18),
                ForeColor = Color.FromArgb(148, 163, 184),
                AutoEllipsis = true
            };

            _previousButton = MakeActionButton("Voltar", 12, 151, 82, Color.FromArgb(71, 85, 105));
            _doneButton = MakeActionButton("Etapa feita", 101, 151, 112, Color.FromArgb(22, 163, 74));
            _skipButton = MakeActionButton("Pular etapa", 220, 151, 100, Color.FromArgb(180, 83, 9));
            _markerButton = MakeActionButton("Marcar", 327, 151, 92, Color.FromArgb(37, 99, 235));

            _statusLabel = new Label
            {
                Location = new Point(426, 157),
                Size = new Size(182, 22),
                ForeColor = Color.FromArgb(203, 213, 225),
                TextAlign = ContentAlignment.MiddleRight,
                AutoEllipsis = true
            };

            _topBar.Controls.Add(_profileLabel);
            _topBar.Controls.Add(_profilesButton);
            _topBar.Controls.Add(_stagesButton);
            _topBar.Controls.Add(_lockButton);
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
            _trayMenu.Items.Add("Restaurar posição", null, delegate { ResetPosition(); });

            ToolStripMenuItem opacityMenu = new ToolStripMenuItem("Transparência");
            AddOpacityItem(opacityMenu, "75%", 0.75);
            AddOpacityItem(opacityMenu, "85%", 0.85);
            AddOpacityItem(opacityMenu, "92%", 0.92);
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
                BackColor = Color.FromArgb(45, 94, 135),
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
                    // Transição breve observada entre o painel TODOS e o jogo.
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

            _currentMission = _currentRoute.FirstOrDefault(m => !_progress.MissionStates.ContainsKey(m.id));
            if (_currentMission == null)
            {
                _questLabel.Text = "Rota concluída até a Cidade do Dragão";
                _npcLabel.Text = "Todas as missões da rota foram marcadas.";
                _locationLabel.Text = string.Empty;
                _statusLabel.Text = finished + "/" + _currentRoute.Count;
                _currentStep = null;
                _stepTypeLabel.Text = "FIM";
                _stepTypeLabel.BackColor = Color.FromArgb(22, 163, 74);
                UpdateActionAvailability();
                return;
            }

            _currentStepIndex = _currentMission.steps.FindIndex(step =>
                !_progress.MissionStates.ContainsKey(StageKey(_currentMission, step)));
            if (_currentStepIndex < 0) _currentStepIndex = Math.Max(0, _currentMission.steps.Count - 1);
            _currentStep = _currentMission.steps[_currentStepIndex];
            ApplyStepType(_currentStep.action);

            _questLabel.Text = _currentMission.name + "  ·  Etapa " + (_currentStepIndex + 1) + "/" + _currentMission.steps.Count;
            _npcLabel.Text = _currentStep.action;
            _locationLabel.Text = _currentStep.markerX.HasValue && _currentStep.markerY.HasValue
                ? "Marcador: " + _currentStep.markerX.Value + ", " + _currentStep.markerY.Value
                : "Sem coordenada para esta etapa";
            _statusLabel.Text = _currentMission.priority + " · Prev. Nv. " + _currentMission.expectedLevel +
                " · " + (finished + 1) + "/" + _currentRoute.Count;
            _statusLabel.ForeColor = string.Equals(_currentMission.priority, "Opcional", StringComparison.OrdinalIgnoreCase)
                ? Color.FromArgb(251, 191, 36)
                : Color.FromArgb(203, 213, 225);
            UpdateActionAvailability();
        }

        private void CompleteCurrent()
        {
            if (!CanChangeProgress() || _currentMission == null || _currentStep == null) return;
            _progress.MissionStates[StageKey(_currentMission, _currentStep)] = "done";
            bool lastStep = _currentStepIndex >= _currentMission.steps.Count - 1;
            if (lastStep) _progress.MissionStates[_currentMission.id] = "done";
            _store.Save(_state);
            _store.Log(_characterName + " concluiu etapa " + (_currentStepIndex + 1) + "/" +
                _currentMission.steps.Count + ": " + _currentMission.name);
            RefreshMissionDisplay();
        }

        private void SkipCurrent()
        {
            if (!CanChangeProgress() || _currentMission == null || _currentStep == null) return;
            _progress.MissionStates[StageKey(_currentMission, _currentStep)] = "skipped";
            bool lastStep = _currentStepIndex >= _currentMission.steps.Count - 1;
            if (lastStep) _progress.MissionStates[_currentMission.id] = "skipped";
            _store.Save(_state);
            _store.Log(_characterName + " pulou etapa " + (_currentStepIndex + 1) + "/" +
                _currentMission.steps.Count + ": " + _currentMission.name);
            RefreshMissionDisplay();
        }

        private void GoBack()
        {
            if (!CanChangeProgress() || _progress == null) return;
            if (_currentMission != null)
            {
                QuestStep completedStep = _currentMission.steps.LastOrDefault(step =>
                    _progress.MissionStates.ContainsKey(StageKey(_currentMission, step)));
                if (completedStep != null)
                {
                    _progress.MissionStates.Remove(StageKey(_currentMission, completedStep));
                    _progress.MissionStates.Remove(_currentMission.id);
                    _store.Save(_state);
                    _store.Log(_characterName + " voltou uma etapa em: " + _currentMission.name);
                    RefreshMissionDisplay();
                    return;
                }
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

                // Abre Marcadores. No modo recomendado, o PWOverlay criado anteriormente
                // é o último item da lista: seleciona-o com End e remove somente ele.
                NativeMethods.ClickClientPoint(_activeGame, _state.Marker.TrackerButton);
                System.Threading.Thread.Sleep(180);

                bool selectedMode = string.Equals(_state.Marker.DeleteMode, "selected", StringComparison.OrdinalIgnoreCase);
                bool shouldDelete = !selectedMode || (_progress != null && _progress.HasOverlayMarker);
                if (shouldDelete)
                {
                    if (selectedMode)
                    {
                        NativeMethods.ClickClientPoint(_activeGame, _state.Marker.MarkerList);
                        NativeMethods.PressKey(NativeMethods.VK_END);
                        System.Threading.Thread.Sleep(80);
                        NativeMethods.ClickClientPoint(_activeGame, _state.Marker.DeleteSelected);
                    }
                    else
                    {
                        NativeMethods.ClickClientPoint(_activeGame, _state.Marker.DeleteAll);
                    }
                    System.Threading.Thread.Sleep(180);

                    // Excluir altera o estado do painel no cliente. Fechar e reabrir garante
                    // que a criação aconteça nesta mesma chamada, sem um segundo clique.
                    NativeMethods.ClickClientPoint(_activeGame, _state.Marker.PanelClose);
                    System.Threading.Thread.Sleep(100);
                    NativeMethods.ClickClientPoint(_activeGame, _state.Marker.TrackerButton);
                    System.Threading.Thread.Sleep(180);
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
            for (int index = 0; index < _currentRoute.Count; index++)
            {
                QuestMission mission = _currentRoute[index];
                _progress.MissionStates.Remove(mission.id);
                foreach (QuestStep step in mission.steps) _progress.MissionStates.Remove(StageKey(mission, step));
                if (index < missionIndex)
                {
                    foreach (QuestStep step in mission.steps) _progress.MissionStates[StageKey(mission, step)] = "done";
                    _progress.MissionStates[mission.id] = "done";
                }
                else if (index == missionIndex)
                {
                    for (int done = 0; done < stepIndex; done++)
                        _progress.MissionStates[StageKey(mission, mission.steps[done])] = "done";
                }
            }
            _store.Save(_state);
            _store.Log(_characterName + " reposicionou a rota em missão " + (missionIndex + 1) +
                ", etapa " + (stepIndex + 1));
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
                _state.PositionLocked = imported.PositionLocked;
                _state.Opacity = imported.Opacity;
                _store.Save(_state);
                Opacity = _state.Opacity;
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

        private void RaiseOverlayStack()
        {
            uint flags = NativeMethods.SWP_NOMOVE | NativeMethods.SWP_NOSIZE |
                NativeMethods.SWP_NOACTIVATE | NativeMethods.SWP_NOOWNERZORDER |
                NativeMethods.SWP_SHOWWINDOW;

            // Primeiro o tracker; por último o painel TODOS, para que o Helper
            // permaneça visível mesmo quando o jogo ativo volta ao topo.
            NativeMethods.SetWindowPos(Handle, NativeMethods.HWND_TOPMOST, 0, 0, 0, 0, flags);
            IntPtr helperWindow;
            NativeMethods.RECT helperRect;
            if (NativeMethods.TryFindPwHelperOverlay(out helperWindow, out helperRect) &&
                helperWindow != IntPtr.Zero)
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
            NativeMethods.RECT helper;
            bool hasHelper = NativeMethods.TryFindPwHelperOverlay(out helper);
            foreach (Point point in candidates)
            {
                Rectangle candidate = new Rectangle(point, Size);
                if (!hasHelper || !candidate.IntersectsWith(new Rectangle(
                    helper.Left, helper.Top, helper.Width, helper.Height)))
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
            _markerHotkeyItem.Text = "Atalho do marcador: " + markerShortcut;
            _store.Log("Atalhos registrados: concluir=" + done + " voltar=" + previous +
                " pular=" + skip + " marcador=" + markerShortcut);
        }

        private void UnregisterHotkeys()
        {
            if (!IsHandleCreated) return;
            NativeMethods.UnregisterHotKey(Handle, HotkeyDone);
            NativeMethods.UnregisterHotKey(Handle, HotkeyPrevious);
            NativeMethods.UnregisterHotKey(Handle, HotkeySkip);
            NativeMethods.UnregisterHotKey(Handle, HotkeyMarker);
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
