using System;
using System.Drawing;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class CultivationChoiceForm : Form
    {
        private readonly RadioButton _god;
        private readonly RadioButton _evil;
        private readonly Button _confirm;

        internal CultivationChoiceForm(string character, string currentPath)
        {
            Text = "Cultivo de " + character;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(460, 228);
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            TopMost = true;
            Font = new Font("Segoe UI", 9.0f);
            var question = new Label { Text = "Qual cultivo este personagem seguirá?", AutoSize = false,
                Location = new Point(16, 16), Size = new Size(428, 28), Font = new Font(Font, FontStyle.Bold) };
            _god = new RadioButton { Text = "God — caminho Celestial", Location = new Point(20, 52), Size = new Size(400, 28) };
            _evil = new RadioButton { Text = "Evil — caminho Demoníaco", Location = new Point(20, 88), Size = new Size(400, 28) };
            var explanation = new Label { Text = "Esta escolha configura apenas o guia. Escolha o mesmo caminho com o NPC no jogo. A preferência fica salva somente para este personagem.",
                Location = new Point(16, 126), Size = new Size(428, 43) };
            _confirm = new Button { Text = "Salvar escolha", Location = new Point(188, 183), Size = new Size(124, 30),
                DialogResult = DialogResult.OK, Enabled = false };
            var later = new Button { Text = "Decidir depois", Location = new Point(320, 183), Size = new Size(124, 30), DialogResult = DialogResult.Cancel };
            _god.CheckedChanged += delegate { _confirm.Enabled = SelectedPath != null; };
            _evil.CheckedChanged += delegate { _confirm.Enabled = SelectedPath != null; };
            currentPath = CultivationRouting.Normalize(currentPath);
            _god.Checked = currentPath == "GOD";
            _evil.Checked = currentPath == "EVIL";
            Controls.AddRange(new Control[] { question, _god, _evil, explanation, _confirm, later });
            AcceptButton = _confirm;
            CancelButton = later;
            ActiveControl = later;
        }

        internal string SelectedPath { get { return _god.Checked ? "GOD" : _evil.Checked ? "EVIL" : null; } }
    }
}
