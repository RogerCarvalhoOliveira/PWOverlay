using System;
using System.Drawing;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class OcrRegionSelectionForm : Form
    {
        private Point _dragStart;
        private Rectangle _selection;
        private bool _dragging;

        internal Rectangle SelectedClientRegion { get; private set; }

        internal OcrRegionSelectionForm(IntPtr gameWindow, Rectangle initialRegion)
        {
            NativeMethods.RECT client;
            if (!NativeMethods.GetClientRect(gameWindow, out client))
                throw new InvalidOperationException("Não foi possível medir a janela do jogo.");
            var origin = new NativeMethods.POINT { X = 0, Y = 0 };
            if (!NativeMethods.ClientToScreen(gameWindow, ref origin))
                throw new InvalidOperationException("Não foi possível localizar a janela do jogo.");

            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = new Rectangle(origin.X, origin.Y, client.Width, client.Height);
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            BackColor = Color.Black;
            Opacity = 0.48;
            Cursor = Cursors.Cross;
            KeyPreview = true;

            _selection = Rectangle.Intersect(ClientRectangle, initialRegion);
            SelectedClientRegion = Rectangle.Empty;

            MouseDown += OnSelectionMouseDown;
            MouseMove += OnSelectionMouseMove;
            MouseUp += OnSelectionMouseUp;
            KeyDown += delegate(object sender, KeyEventArgs e)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    DialogResult = DialogResult.Cancel;
                    Close();
                }
            };
        }

        protected override bool ShowWithoutActivation { get { return false; } }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var instructionBackground = new SolidBrush(Color.FromArgb(220, 15, 23, 42)))
            using (var border = new Pen(Color.FromArgb(56, 189, 248), 3.0f))
            using (var fill = new SolidBrush(Color.FromArgb(70, 56, 189, 248)))
            using (var textBrush = new SolidBrush(Color.White))
            using (var font = new Font("Segoe UI", 12.0f, FontStyle.Bold))
            {
                e.Graphics.FillRectangle(instructionBackground, 12, 12, 610, 42);
                e.Graphics.DrawString(
                    "Arraste sobre a lista de missões. Esc cancela.",
                    font,
                    textBrush,
                    new PointF(24, 22));
                if (_selection.Width > 0 && _selection.Height > 0)
                {
                    e.Graphics.FillRectangle(fill, _selection);
                    e.Graphics.DrawRectangle(border, _selection);
                }
            }
        }

        private void OnSelectionMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            _dragStart = e.Location;
            _selection = new Rectangle(e.X, e.Y, 0, 0);
            _dragging = true;
            Invalidate();
        }

        private void OnSelectionMouseMove(object sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            int left = Math.Min(_dragStart.X, e.X);
            int top = Math.Min(_dragStart.Y, e.Y);
            int right = Math.Max(_dragStart.X, e.X);
            int bottom = Math.Max(_dragStart.Y, e.Y);
            _selection = Rectangle.Intersect(
                ClientRectangle,
                Rectangle.FromLTRB(left, top, right, bottom));
            Invalidate();
        }

        private void OnSelectionMouseUp(object sender, MouseEventArgs e)
        {
            if (!_dragging || e.Button != MouseButtons.Left) return;
            _dragging = false;
            if (_selection.Width < 20 || _selection.Height < 20)
            {
                System.Media.SystemSounds.Exclamation.Play();
                return;
            }
            SelectedClientRegion = _selection;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
