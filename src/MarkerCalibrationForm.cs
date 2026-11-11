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
        private ClientWindowInfo _captureClient;
        private Size _captureSize;
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
                Text = "Coloque o PW em foco. Posicione o mouse sobre o controle indicado e pressione F8. " +
                    "Abra Marcadores manualmente para capturar os controles internos. Depois, volte pelo Alt+Tab e clique em Salvar."
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
            var saveButton = new Button { Text = "Salvar", Size = new Size(92, 32), Location = new Point(446, 430), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var cancelButton = new Button { Text = "Cancelar", Size = new Size(92, 32), Location = new Point(546, 430), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, DialogResult = DialogResult.Cancel };
            resetButton.Click += delegate { ResetDefaults(); };
            saveButton.Click += delegate { if (SaveGrid()) DialogResult = DialogResult.OK; };

            Controls.AddRange(new Control[] { instructions, _targetLabel, _grid, resetButton, saveButton, cancelButton });
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
            if (disposing && _captureTimer != null) _captureTimer.Dispose();
            base.Dispose(disposing);
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
            _targetLabel.Text = error ?? ("Cliente PID " + client.ProcessId + " · " + size.Width + " × " + size.Height +
                " · " + (_feedback ?? "Aponte para o controle selecionado e pressione F8."));
        }

        private void CaptureAtCursor()
        {
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

        private bool TryCapture(ClientWindowInfo client, Size size, Point point)
        {
            if (CaptureError(client, size) != null || _grid.SelectedRows.Count == 0) return false;
            if (point.X < 0 || point.Y < 0 || point.X >= size.Width || point.Y >= size.Height)
            {
                _feedback = "Posicione o mouse dentro da área do jogo e pressione F8.";
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
                _feedback = "Ponto capturado. Pressione F8 sobre o próximo controle.";
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
