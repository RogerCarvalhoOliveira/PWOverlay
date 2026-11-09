using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class ClientProfilesForm : Form
    {
        private readonly DataGridView _grid;
        private readonly ComboBox _saved;
        private readonly List<CharacterProgress> _profiles;
        internal Dictionary<string, ClientIdentity> Selections { get; private set; }

        internal ClientProfilesForm(List<ClientWindowInfo> windows, ClientBindings bindings,
            IDictionary<string, string> aliases, IEnumerable<CharacterProgress> profiles, IntPtr active)
        {
            Text = "Personagens por cliente — PW Quest Overlay";
            Icon = PWOverlayShared.AppBranding.Icon;
            ClientSize = new Size(790, 420); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            ShowInTaskbar = false; TopMost = true; Font = new Font("Segoe UI", 9);
            var description = new Label { Text = "Cada linha é uma janela do jogo. Digite nome e classe ou use um perfil salvo. PW Helper é opcional.\nA linha selecionada é o último cliente em foco. Reassocie após reiniciar ou trocar de personagem.", Location = new Point(12, 10), Size = new Size(766, 44) };
            _profiles = profiles.Where(p => !string.IsNullOrWhiteSpace(p.CharacterName) && ClientIdentities.IsValidClass(p.ClassCode))
                .OrderBy(p => p.CharacterName).ThenBy(p => p.ClassCode).ToList();
            _saved = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 62), Size = new Size(550, 26) };
            foreach (CharacterProgress p in _profiles) _saved.Items.Add(p.CharacterName + " [" + p.ClassCode + "]");
            if (_saved.Items.Count > 0) _saved.SelectedIndex = 0;
            var use = new Button { Text = "Usar na linha selecionada", Location = new Point(574, 60), Size = new Size(204, 28), Enabled = _profiles.Count > 0 };
            _grid = new DataGridView { Location = new Point(12, 100), Size = new Size(766, 260), AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoGenerateColumns = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Window", HeaderText = "Cliente / título da janela", ReadOnly = true, Width = 365 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Character", HeaderText = "Nome do personagem", Width = 235 });
            var classes = new DataGridViewComboBoxColumn { Name = "Class", HeaderText = "Classe", Width = 140, FlatStyle = FlatStyle.Flat };
            classes.Items.AddRange("Automático", "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS"); _grid.Columns.Add(classes);
            foreach (ClientWindowInfo window in windows)
            {
                bool unique = windows.Count(w => string.Equals(w.Title, window.Title, StringComparison.OrdinalIgnoreCase)) == 1;
                ClientIdentity manual = bindings.Find(window.SessionKey);
                ClientIdentity detected = manual ?? ClientIdentities.FromTitle(window.Title, unique, aliases);
                int index = _grid.Rows.Add("PID " + window.ProcessId + " · " + window.Title, detected == null ? "" : detected.Name,
                    manual == null ? "Automático" : manual.ClassCode);
                _grid.Rows[index].Tag = window.SessionKey;
                if (window.Handle == active) _grid.CurrentCell = _grid.Rows[index].Cells["Character"];
            }
            _grid.CurrentCellDirtyStateChanged += delegate {
                if (_grid.IsCurrentCellDirty && _grid.CurrentCell != null && _grid.CurrentCell.OwningColumn.Name == "Class")
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args) {
                if (args.RowIndex < 0 || args.ColumnIndex != _grid.Columns["Class"].Index) return;
                var row = _grid.Rows[args.RowIndex];
                if (Convert.ToString(row.Cells["Class"].Value) != "Automático") return;
                var window = windows.Find(w => w.SessionKey == (string)row.Tag);
                ClientIdentity automatic = ClientIdentities.FromTitle(window.Title,
                    windows.Count(w => string.Equals(w.Title, window.Title, StringComparison.OrdinalIgnoreCase)) == 1, aliases);
                row.Cells["Character"].Value = automatic == null ? "" : automatic.Name;
            };
            use.Click += delegate {
                if (_grid.CurrentRow == null || _saved.SelectedIndex < 0) return;
                CharacterProgress p = _profiles[_saved.SelectedIndex];
                _grid.CurrentRow.Cells["Character"].Value = p.CharacterName; _grid.CurrentRow.Cells["Class"].Value = p.ClassCode;
            };
            var save = new Button { Text = "Salvar", Location = new Point(592, 376), Size = new Size(88, 30), Enabled = windows.Count > 0 };
            save.Click += delegate {
                _grid.EndEdit(); var result = new Dictionary<string, ClientIdentity>();
                try {
                    foreach (DataGridViewRow row in _grid.Rows) {
                        string code = Convert.ToString(row.Cells["Class"].Value);
                        var window = windows.Find(w => w.SessionKey == (string)row.Tag);
                        string name = Convert.ToString(row.Cells["Character"].Value).Trim();
                        ClientIdentity auto = ClientIdentities.FromTitle(window.Title,
                            windows.Count(other => string.Equals(other.Title, window.Title, StringComparison.OrdinalIgnoreCase)) == 1, aliases);
                        if (code == "Automático" && name.Length > 0 && (auto == null || !string.Equals(auto.Name, name, StringComparison.OrdinalIgnoreCase)))
                            throw new ArgumentException("Selecione também a classe do personagem digitado ou use um perfil salvo.");
                        result[(string)row.Tag] = code == "Automático" ? null : new ClientIdentity(Convert.ToString(row.Cells["Character"].Value), code, true);
                    }
                    var resolved = windows.Select(w => result[w.SessionKey] ?? ClientIdentities.FromTitle(w.Title,
                        windows.Count(other => string.Equals(other.Title, w.Title, StringComparison.OrdinalIgnoreCase)) == 1, aliases)).Where(i => i != null);
                    if (resolved.GroupBy(i => i.ProfileKey).Any(group => group.Count() > 1))
                        throw new ArgumentException("O mesmo perfil foi associado a dois clientes. Use nomes/identificadores diferentes para personagens distintos.");
                    Selections = result; DialogResult = DialogResult.OK;
                } catch (ArgumentException error) { MessageBox.Show(this, error.Message, "Confira nome e classe", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(690, 376), Size = new Size(88, 30) };
            Controls.AddRange(new Control[] { description, _saved, use, _grid, save, cancel }); AcceptButton = save; CancelButton = cancel;
        }
    }
}
