using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class MarkerCalibrationForm : Form
    {
        private sealed class CalibrationItem
        {
            internal string Name;
            internal Func<MarkerSettings, MarkerPoint> Read;
            internal Action<MarkerSettings, MarkerPoint> Write;
        }

        private const int CaptureHotkey = 1;
        private readonly MarkerSettings _settings;
        private readonly DataGridView _grid;
        private readonly Timer _captureTimer;
        private readonly List<CalibrationItem> _items;
        private readonly Label _targetLabel;
        private readonly Button _selectButton;
        private ClientWindowInfo _captureClient;
        private Size _captureSize;
        private ClientWindowInfo _selectionClient;
        private PointPicker _picker;
        private DateTime _pickerReadyAt;
        private DateTime _focusDeadline;
        private int _selectionRow;
        internal IntPtr PreferredGame { get; set; }
        private IntPtr _hotkeyWindow;
        private string _feedback;
        private bool _f8Down;

        internal MarkerCalibrationForm(MarkerSettings settings)
        {
            _settings = settings;
            _items = BuildItems();

            Text = "Calibrar marcador — PW Quest Overlay";
            FormBorderStyle = FormBorderStyle.Sizable;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(650, 480);
            MinimumSize = new Size(600, 440);
            TopMost = true;
            Font = new Font("Segoe UI", 9.0f);

            var instructions = new Label
            {
                Location = new Point(12, 10),
                Size = new Size(626, 55),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
                Text = "Selecione uma linha e clique em Selecionar ponto. Dê um clique duplo sobre o controle no PW e confirme. " +
                    "Abra os painéis do jogo antes de selecionar seus controles. Você também pode capturar com F8."
            };
            _targetLabel = new Label
            {
                Location = new Point(12, 64),
                Size = new Size(626, 34),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
                Text = "Aguardando uma janela do PW em foco."
            };

            _grid = new DataGridView
            {
                Location = new Point(12, 104),
                Size = new Size(626, 314),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                AutoGenerateColumns = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Controle", Width = 360, ReadOnly = true });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "X", Width = 110 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Y", Width = 110 });
            foreach (CalibrationItem item in _items)
            {
                MarkerPoint point = item.Read(_settings);
                _grid.Rows.Add(item.Name, point.X, point.Y);
            }
            if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;

            var resetButton = new Button { Text = "Restaurar padrão", Size = new Size(120, 32), Location = new Point(12, 430), Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            _selectButton = new Button { Text = "Selecionar ponto", Size = new Size(135, 32), Location = new Point(140, 430), Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            var saveButton = new Button { Text = "Salvar", Size = new Size(92, 32), Location = new Point(446, 430), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var cancelButton = new Button { Text = "Cancelar", Size = new Size(92, 32), Location = new Point(546, 430), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, DialogResult = DialogResult.Cancel };
            resetButton.Click += delegate { ResetDefaults(); };
            _selectButton.Click += delegate { StartPointSelection(); };
            saveButton.Click += delegate { if (SaveGrid()) DialogResult = DialogResult.OK; };

            Controls.AddRange(new Control[] { instructions, _targetLabel, _grid, resetButton, _selectButton, saveButton, cancelButton });
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            _captureTimer = new Timer { Interval = 40 };
            _captureTimer.Tick += CaptureTimerTick;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            if (NativeMethods.RegisterHotKey(Handle, CaptureHotkey, NativeMethods.MOD_NOREPEAT, NativeMethods.VK_F8))
                _hotkeyWindow = Handle;
            _f8Down = IsF8Down();
            _captureTimer.Start();
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            if (_hotkeyWindow != IntPtr.Zero)
            {
                NativeMethods.UnregisterHotKey(_hotkeyWindow, CaptureHotkey);
                _hotkeyWindow = IntPtr.Zero;
            }
            base.OnHandleDestroyed(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_captureTimer != null) _captureTimer.Dispose();
                if (_picker != null) _picker.Dispose();
            }
            base.Dispose(disposing);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (_selectionClient != null) EndPointSelection();
            base.OnFormClosing(e);
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == NativeMethods.WM_HOTKEY && message.WParam.ToInt32() == CaptureHotkey)
            {
                CaptureAtCursor();
                return;
            }
            base.WndProc(ref message);
        }

        private static List<CalibrationItem> BuildItems()
        {
            return new List<CalibrationItem>
            {
                new CalibrationItem { Name = "Botão Marcadores ao lado do minimapa", Read = s => s.TrackerButton, Write = (s, p) => s.TrackerButton = p },
                new CalibrationItem { Name = "Excluir todos os marcadores", Read = s => s.DeleteAll, Write = (s, p) => s.DeleteAll = p },
                new CalibrationItem { Name = "Excluir marcador selecionado", Read = s => s.DeleteSelected, Write = (s, p) => s.DeleteSelected = p },
                new CalibrationItem { Name = "Lista de waypoints criados (painel direito)", Read = s => s.MarkerList, Write = (s, p) => s.MarkerList = p },
                new CalibrationItem { Name = "Campo de coordenadas", Read = s => s.CoordinateInput, Write = (s, p) => s.CoordinateInput = p },
                new CalibrationItem { Name = "Botão rastrear coordenadas (>>)", Read = s => s.TrackCoordinates, Write = (s, p) => s.TrackCoordinates = p },
                new CalibrationItem { Name = "Campo Nome da segunda janela", Read = s => s.NameInput, Write = (s, p) => s.NameInput = p },
                new CalibrationItem { Name = "Botão OK da segunda janela", Read = s => s.NameOk, Write = (s, p) => s.NameOk = p },
                new CalibrationItem { Name = "Fechar painel Marcadores", Read = s => s.PanelClose, Write = (s, p) => s.PanelClose = p },
            };
        }

        private void CaptureTimerTick(object sender, EventArgs e)
        {
            if (_selectionClient != null)
            {
                UpdatePointSelection();
                return;
            }
            Size size;
            ClientWindowInfo client = ForegroundClient(out size);
            UpdateTargetStatus(client, size);
            if (_hotkeyWindow != IntPtr.Zero) return;
            bool down = IsF8Down();
            if (down && !_f8Down) CaptureAtCursor();
            _f8Down = down;
        }

        private static bool IsF8Down()
        {
            return (NativeMethods.GetAsyncKeyState(NativeMethods.VK_F8) & 0x8000) != 0;
        }

        private static ClientWindowInfo ForegroundClient(out Size size)
        {
            IntPtr window = NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GA_ROOT);
            ClientWindowInfo client = NativeMethods.DescribeGameWindow(window);
            NativeMethods.RECT rect;
            size = client != null && NativeMethods.GetClientRect(window, out rect)
                ? new Size(rect.Width, rect.Height) : Size.Empty;
            return client;
        }

        private string CaptureError(ClientWindowInfo client, Size size)
        {
            if (client == null || size.Width <= 0 || size.Height <= 0)
                return _captureClient == null ? "Aguardando uma janela do PW em foco."
                    : "Volte ao PW que está sendo calibrado ou clique em Salvar.";
            if (_captureClient != null && client.SessionKey != _captureClient.SessionKey)
                return "Volte ao cliente PID " + _captureClient.ProcessId + " para continuar a calibração.";
            if (_captureClient != null && size != _captureSize)
                return "A resolução mudou. Restaure o padrão ou reabra a calibração para capturar novamente.";
            return null;
        }

        private void UpdateTargetStatus(ClientWindowInfo client, Size size)
        {
            string error = CaptureError(client, size);
            string status = error == null
                ? "Cliente PID " + client.ProcessId + " · " + size.Width + " × " + size.Height
                : error;
            _targetLabel.Text = _feedback == null ? status :
                (error == null ? status + Environment.NewLine + _feedback : _feedback);
        }

        private void CaptureAtCursor()
        {
            if (_selectionClient != null) return;
            Size size;
            ClientWindowInfo client = ForegroundClient(out size);
            if (CaptureError(client, size) == null)
            {
                NativeMethods.POINT point;
                if (NativeMethods.GetCursorPos(out point) &&
                    NativeMethods.GetAncestor(NativeMethods.WindowFromPoint(point), NativeMethods.GA_ROOT) == client.Handle &&
                    NativeMethods.ScreenToClient(client.Handle, ref point))
                    TryCapture(client, size, new Point(point.X, point.Y));
                else
                    _feedback = "Posicione o mouse sobre a janela do PW e pressione F8.";
            }
            UpdateTargetStatus(client, size);
        }

        private void StartPointSelection()
        {
            if (_selectionClient != null || _grid.SelectedRows.Count == 0) return;
            ClientWindowInfo client;
            if (_captureClient != null)
            {
                client = NativeMethods.DescribeGameWindow(_captureClient.Handle);
                if (client == null || client.SessionKey != _captureClient.SessionKey)
                {
                    _feedback = "O cliente usado saiu. Restaure o padrão ou reabra a calibração.";
                    return;
                }
            }
            else
            {
                client = NativeMethods.DescribeGameWindow(PreferredGame);
                if (client == null)
                {
                    foreach (IntPtr window in NativeMethods.FindGameWindows())
                    {
                        client = NativeMethods.DescribeGameWindow(window);
                        if (client != null) break;
                    }
                }
            }
            if (client == null)
            {
                _feedback = "Abra um cliente do PW para selecionar o ponto.";
                return;
            }

            _selectionRow = _grid.SelectedRows[0].Index;
            _selectionClient = client;
            _selectButton.Enabled = false;
            _targetLabel.Text = "Dê um clique duplo no controle. Clique com o botão direito para cancelar.";
            TopMost = false;
            DateTime now = DateTime.UtcNow;
            _pickerReadyAt = now.AddMilliseconds(180);
            _focusDeadline = now.AddMilliseconds(1500);
            if (NativeMethods.IsIconic(client.Handle))
                NativeMethods.ShowWindowAsync(client.Handle, NativeMethods.SW_RESTORE);
            NativeMethods.SetForegroundWindow(client.Handle);
        }

        private void UpdatePointSelection()
        {
            IntPtr target = _selectionClient.Handle;
            IntPtr foreground = NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GA_ROOT);
            Rectangle bounds;
            if (foreground != target || !TryGetClientBounds(target, out bounds))
            {
                if (_picker == null && DateTime.UtcNow < _focusDeadline)
                {
                    if (foreground == Handle && !NativeMethods.IsIconic(target))
                        NativeMethods.SetForegroundWindow(target);
                    return;
                }
                CancelPointSelection("O foco saiu do cliente do PW. Selecione o ponto novamente.");
                return;
            }
            if (_picker != null)
            {
                if (bounds != _picker.Bounds)
                    CancelPointSelection("A janela mudou de posição ou tamanho. Selecione o ponto novamente.");
                return;
            }
            if (DateTime.UtcNow < _pickerReadyAt) return;

            ClientWindowInfo client = NativeMethods.DescribeGameWindow(target);
            string error = CaptureError(client, bounds.Size);
            if (error != null || client.SessionKey != _selectionClient.SessionKey)
            {
                CancelPointSelection(error ?? "O cliente mudou. Selecione o ponto novamente.");
                return;
            }
            _picker = new PointPicker(bounds, ConfirmPointSelection,
                delegate { CancelPointSelection("Seleção cancelada."); });
            _picker.Show();
        }

        private static bool TryGetClientBounds(IntPtr window, out Rectangle bounds)
        {
            NativeMethods.RECT rect;
            var origin = new NativeMethods.POINT();
            if (NativeMethods.GetClientRect(window, out rect) && rect.Width > 0 && rect.Height > 0 &&
                NativeMethods.ClientToScreen(window, ref origin))
            {
                bounds = new Rectangle(origin.X, origin.Y, rect.Width, rect.Height);
                return true;
            }
            bounds = Rectangle.Empty;
            return false;
        }

        private void ConfirmPointSelection(Point point)
        {
            Rectangle bounds;
            ClientWindowInfo client = NativeMethods.DescribeGameWindow(_selectionClient.Handle);
            if (client == null || client.SessionKey != _selectionClient.SessionKey ||
                NativeMethods.GetAncestor(NativeMethods.GetForegroundWindow(), NativeMethods.GA_ROOT) != client.Handle ||
                !TryGetClientBounds(client.Handle, out bounds) || bounds != _picker.Bounds)
            {
                CancelPointSelection("O cliente ou a posição da janela mudou. Selecione o ponto novamente.");
                return;
            }
            EndPointSelection();
            Activate();
            _grid.CurrentCell = _grid.Rows[_selectionRow].Cells[0];
            if (MessageBox.Show(this, _items[_selectionRow].Name + "\nPonto no jogo (" + point.X + ", " + point.Y + ").",
                "Confirmar posição", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) != DialogResult.OK)
            {
                _feedback = "Seleção cancelada.";
                return;
            }
            Rectangle currentBounds;
            ClientWindowInfo current = NativeMethods.DescribeGameWindow(client.Handle);
            if (current == null || current.SessionKey != client.SessionKey ||
                !TryGetClientBounds(current.Handle, out currentBounds) || currentBounds != bounds)
                _feedback = "A janela mudou durante a confirmação. Selecione o ponto novamente.";
            else
                TryCapture(current, bounds.Size, point);
        }

        private void CancelPointSelection(string message)
        {
            EndPointSelection();
            _feedback = message;
            Activate();
        }

        private void EndPointSelection()
        {
            if (_picker != null) _picker.Dispose();
            _picker = null;
            _selectionClient = null;
            _selectButton.Enabled = true;
            TopMost = true;
        }

        private sealed class PointPicker : Form
        {
            private readonly Action<Point> _confirm;
            private readonly Action _cancel;

            internal PointPicker(Rectangle bounds, Action<Point> confirm, Action cancel)
            {
                _confirm = confirm;
                _cancel = cancel;
                FormBorderStyle = FormBorderStyle.None;
                StartPosition = FormStartPosition.Manual;
                AutoScaleMode = AutoScaleMode.None;
                ShowInTaskbar = false;
                TopMost = true;
                BackColor = Color.Black;
                Opacity = 0.08;
                Cursor = Cursors.Cross;
                Bounds = bounds;
                SetStyle(ControlStyles.StandardClick | ControlStyles.StandardDoubleClick, true);
            }

            protected override bool ShowWithoutActivation { get { return true; } }

            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams parameters = base.CreateParams;
                    parameters.ExStyle |= (int)(NativeMethods.WS_EX_NOACTIVATE | NativeMethods.WS_EX_TOOLWINDOW);
                    return parameters;
                }
            }

            protected override void WndProc(ref Message message)
            {
                if (message.Msg == NativeMethods.WM_MOUSEACTIVATE)
                {
                    message.Result = new IntPtr(NativeMethods.MA_NOACTIVATE);
                    return;
                }
                base.WndProc(ref message);
            }

            protected override void OnMouseDoubleClick(MouseEventArgs e)
            {
                base.OnMouseDoubleClick(e);
                if (e.Button == MouseButtons.Left)
                    BeginInvoke(new Action(delegate { if (!IsDisposed) _confirm(e.Location); }));
            }

            protected override void OnMouseUp(MouseEventArgs e)
            {
                base.OnMouseUp(e);
                if (e.Button == MouseButtons.Right)
                    BeginInvoke(new Action(delegate { if (!IsDisposed) _cancel(); }));
            }
        }

        private bool TryCapture(ClientWindowInfo client, Size size, Point point)
        {
            if (CaptureError(client, size) != null || _grid.SelectedRows.Count == 0) return false;
            if (point.X < 0 || point.Y < 0 || point.X >= size.Width || point.Y >= size.Height)
            {
                _feedback = "Aponte dentro da área do jogo.";
                return false;
            }
            _captureClient = client;
            _captureSize = size;
            int row = _grid.SelectedRows[0].Index;
            _grid.Rows[row].Cells[1].Value = (int)Math.Round(point.X * 1920.0 / size.Width);
            _grid.Rows[row].Cells[2].Value = (int)Math.Round(point.Y * 1080.0 / size.Height);
            if (row + 1 < _grid.Rows.Count)
            {
                _grid.CurrentCell = _grid.Rows[row + 1].Cells[0];
                _feedback = "Ponto capturado. Selecione o próximo controle ou pressione F8.";
            }
            else
                _feedback = "Último ponto capturado. Volte e clique em Salvar.";
            return true;
        }

        private bool SaveGrid()
        {
            try
            {
                var points = new List<MarkerPoint>();
                for (int index = 0; index < _items.Count; index++)
                {
                    int x, y;
                    if (!int.TryParse(Convert.ToString(_grid.Rows[index].Cells[1].Value), out x) ||
                        !int.TryParse(Convert.ToString(_grid.Rows[index].Cells[2].Value), out y) ||
                        x < 0 || x > 1920 || y < 0 || y > 1080)
                        throw new InvalidOperationException("Confira as coordenadas de " + _items[index].Name + ".");
                    points.Add(new MarkerPoint(x, y));
                }
                for (int index = 0; index < _items.Count; index++)
                    _items[index].Write(_settings, points[index]);
                return true;
            }
            catch (Exception error)
            {
                MessageBox.Show(error.Message, "Calibração inválida", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        private void ResetDefaults()
        {
            MarkerSettings defaults = MarkerSettings.CreateDefault();
            for (int index = 0; index < _items.Count; index++)
            {
                MarkerPoint point = _items[index].Read(defaults);
                _grid.Rows[index].Cells[1].Value = point.X;
                _grid.Rows[index].Cells[2].Value = point.Y;
            }
            _grid.CurrentCell = _grid.Rows[0].Cells[0];
            _captureClient = null;
            _captureSize = Size.Empty;
            _feedback = null;
        }
    }
}
