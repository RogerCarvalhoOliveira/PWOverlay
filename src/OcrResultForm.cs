using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class OcrResultForm : Form
    {
        internal OcrResultForm(QuestOcrResult result, string matchSummary)
        {
            Text = "Teste de OCR — PW Quest Overlay";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(680, 480);
            MinimumSize = new Size(560, 380);
            BackColor = Color.FromArgb(15, 23, 42);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9.0f);

            var summary = new Label
            {
                Location = new Point(16, 14),
                Size = new Size(648, 54),
                ForeColor = Color.FromArgb(186, 230, 253),
                Text = string.Format(
                    "Leitura em {0} ms · imagem {1}×{2}\r\n{3}",
                    result.ElapsedMilliseconds,
                    result.Width,
                    result.Height,
                    matchSummary)
            };
            var textBox = new TextBox
            {
                Location = new Point(16, 76),
                Size = new Size(648, 330),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                BackColor = Color.FromArgb(30, 41, 59),
                ForeColor = Color.White,
                Text = string.IsNullOrWhiteSpace(result.Text)
                    ? "(nenhum texto reconhecido)"
                    : result.Text
            };
            var imageButton = new Button
            {
                Location = new Point(16, 422),
                Size = new Size(190, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Left,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(3, 105, 161),
                ForeColor = Color.White,
                Text = "Abrir imagem capturada"
            };
            imageButton.FlatAppearance.BorderSize = 0;
            imageButton.Click += delegate
            {
                try
                {
                    Process.Start(new ProcessStartInfo(result.DiagnosticImagePath)
                    {
                        UseShellExecute = true
                    });
                }
                catch (Exception error)
                {
                    MessageBox.Show(error.Message, "OCR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            };
            var closeButton = new Button
            {
                Location = new Point(544, 422),
                Size = new Size(120, 34),
                Anchor = AnchorStyles.Bottom | AnchorStyles.Right,
                DialogResult = DialogResult.OK,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(71, 85, 105),
                ForeColor = Color.White,
                Text = "Fechar"
            };
            closeButton.FlatAppearance.BorderSize = 0;

            Controls.Add(summary);
            Controls.Add(textBox);
            Controls.Add(imageButton);
            Controls.Add(closeButton);
            AcceptButton = closeButton;
        }
    }
}
