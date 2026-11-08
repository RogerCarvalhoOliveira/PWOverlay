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
            internal Func<MarkerPoint> Read;
            internal Action<MarkerPoint> Write;
        }

        private readonly IntPtr _gameWindow;
        private readonly MarkerSettings _settings;
        private readonly DataGridView _grid;
        private readonly Timer _captureTimer;
        private readonly List<CalibrationItem> _items;
        private bool _f8Down;

        internal MarkerCalibrationForm(IntPtr gameWindow, MarkerSettings settings)
        {
            _gameWindow = gameWindow;
            _settings = settings;
            _items = BuildItems();

            Text = "Calibrar marcador — PW Quest Overlay";
            FormBorderStyle = FormBorderStyle.SizableToolWindow;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(650, 455);
            MinimumSize = new Size(600, 420);
            TopMost = true;
            Font = new Font("Segoe UI", 9.0f);

            var instructions = new Label
            {
                Location = new Point(12, 10),
                Size = new Size(626, 62),
                Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right,
                Text = "Selecione uma linha, coloque o mouse sobre o controle correspondente no jogo e pressione F8. " +
                    "As posições são salvas em escala 1920×1080 e adaptadas à resolução do cliente. " +
                    "Abra o painel Marcadores manualmente para capturar seus controles."
            };

            _grid = new DataGridView
            {
                Location = new Point(12, 78),
                Size = new Size(626, 315),
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
                MarkerPoint point = item.Read();
                _grid.Rows.Add(item.Name, point.X, point.Y);
            }
            if (_grid.Rows.Count > 0) _grid.Rows[0].Selected = true;

            var resetButton = new Button { Text = "Restaurar padrão", Size = new Size(120, 32), Location = new Point(12, 405), Anchor = AnchorStyles.Left | AnchorStyles.Bottom };
            var saveButton = new Button { Text = "Salvar", Size = new Size(92, 32), Location = new Point(446, 405), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, DialogResult = DialogResult.OK };
            var cancelButton = new Button { Text = "Cancelar", Size = new Size(92, 32), Location = new Point(546, 405), Anchor = AnchorStyles.Right | AnchorStyles.Bottom, DialogResult = DialogResult.Cancel };
            resetButton.Click += delegate { ResetDefaults(); };
            saveButton.Click += delegate { if (!SaveGrid()) DialogResult = DialogResult.None; };

            Controls.AddRange(new Control[] { instructions, _grid, resetButton, saveButton, cancelButton });
            AcceptButton = saveButton;
            CancelButton = cancelButton;

            _captureTimer = new Timer { Interval = 40 };
            _captureTimer.Tick += CaptureTimerTick;
            _captureTimer.Start();
            FormClosed += delegate { _captureTimer.Stop(); _captureTimer.Dispose(); };
        }

        private List<CalibrationItem> BuildItems()
        {
            return new List<CalibrationItem>
            {
                new CalibrationItem { Name = "Botão Marcadores ao lado do minimapa", Read = () => _settings.TrackerButton, Write = value => _settings.TrackerButton = value },
                new CalibrationItem { Name = "Excluir todos os marcadores", Read = () => _settings.DeleteAll, Write = value => _settings.DeleteAll = value },
                new CalibrationItem { Name = "Excluir marcador selecionado", Read = () => _settings.DeleteSelected, Write = value => _settings.DeleteSelected = value },
                new CalibrationItem { Name = "Área da lista de marcadores", Read = () => _settings.MarkerList, Write = value => _settings.MarkerList = value },
                new CalibrationItem { Name = "Campo de coordenadas", Read = () => _settings.CoordinateInput, Write = value => _settings.CoordinateInput = value },
                new CalibrationItem { Name = "Botão rastrear coordenadas (>>)", Read = () => _settings.TrackCoordinates, Write = value => _settings.TrackCoordinates = value },
                new CalibrationItem { Name = "Campo Nome da segunda janela", Read = () => _settings.NameInput, Write = value => _settings.NameInput = value },
                new CalibrationItem { Name = "Botão OK da segunda janela", Read = () => _settings.NameOk, Write = value => _settings.NameOk = value },
                new CalibrationItem { Name = "Fechar painel Marcadores", Read = () => _settings.PanelClose, Write = value => _settings.PanelClose = value },
            };
        }

        private void CaptureTimerTick(object sender, EventArgs e)
        {
            bool down = (NativeMethods.GetAsyncKeyState(0x77) & 0x8000) != 0;
            if (down && !_f8Down && _grid.SelectedRows.Count > 0)
            {
                NativeMethods.POINT point;
                NativeMethods.RECT client;
                if (NativeMethods.GetCursorPos(out point) && NativeMethods.GetClientRect(_gameWindow, out client))
                {
                    NativeMethods.ScreenToClient(_gameWindow, ref point);
                    if (point.X >= 0 && point.Y >= 0 && point.X < client.Width && point.Y < client.Height)
                    {
                        int row = _grid.SelectedRows[0].Index;
                        _grid.Rows[row].Cells[1].Value = (int)Math.Round(point.X * 1920.0 / client.Width);
                        _grid.Rows[row].Cells[2].Value = (int)Math.Round(point.Y * 1080.0 / client.Height);
                        if (row + 1 < _grid.Rows.Count)
                        {
                            _grid.ClearSelection();
                            _grid.Rows[row + 1].Selected = true;
                            _grid.FirstDisplayedScrollingRowIndex = Math.Max(0, row);
                        }
                    }
                }
            }
            _f8Down = down;
        }

        private bool SaveGrid()
        {
            try
            {
                for (int index = 0; index < _items.Count; index++)
                {
                    int x = Convert.ToInt32(_grid.Rows[index].Cells[1].Value);
                    int y = Convert.ToInt32(_grid.Rows[index].Cells[2].Value);
                    if (x < 0 || x > 1920 || y < 0 || y > 1080) throw new InvalidOperationException("Posição fora da área de referência.");
                    _items[index].Write(new MarkerPoint(x, y));
                }
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
            MarkerPoint[] points = { defaults.TrackerButton, defaults.DeleteAll, defaults.DeleteSelected, defaults.MarkerList,
                defaults.CoordinateInput, defaults.TrackCoordinates, defaults.NameInput, defaults.NameOk, defaults.PanelClose };
            for (int index = 0; index < points.Length; index++)
            {
                _grid.Rows[index].Cells[1].Value = points[index].X;
                _grid.Rows[index].Cells[2].Value = points[index].Y;
            }
        }
    }
}
