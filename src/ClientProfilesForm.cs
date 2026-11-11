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
        internal Dictionary<string, ClientServer> Servers { get; private set; }
        internal Dictionary<string, CharacterProgress> LegacyProfiles { get; private set; }

        internal void AddElevatedReadAction(Action request)
        {
            var allowRead = new Button { Text = "Permitir leitura do PW elevado...", Location = new Point(12, 376), Size = new Size(238, 30) };
            allowRead.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                request();
            };
            Controls.Add(allowRead);
        }

        internal ClientProfilesForm(List<ClientWindowInfo> windows, ClientBindings bindings,
            IDictionary<string, string> aliases, IEnumerable<CharacterProgress> profiles, IntPtr active)
        {
            Text = "Personagens por cliente — PW Quest Overlay";
            Icon = PWOverlayShared.AppBranding.Icon;
            ClientSize = new Size(870, 420); StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false; MinimizeBox = false;
            ShowInTaskbar = false; TopMost = true; Font = new Font("Segoe UI", 9);
            var description = new Label { Text = "Confira o servidor de cada cliente. Se uma instalação atende vários servidores, informe o mundo da janela.\nPara vincular o progresso antigo, selecione o perfil marcado como Sem servidor e clique em Usar.", Location = new Point(12, 10), Size = new Size(846, 44) };
            _profiles = profiles.Where(p => !string.IsNullOrWhiteSpace(p.CharacterName) && ClientIdentities.IsValidClass(p.ClassCode))
                .OrderBy(p => p.CharacterName).ThenBy(p => p.ServerName).ThenBy(p => p.ClassCode).ToList();
            _saved = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Location = new Point(12, 62), Size = new Size(550, 26) };
            foreach (CharacterProgress p in _profiles)
                _saved.Items.Add(p.CharacterName + " [" + p.ClassCode + "] · " + (p.ServerName ?? "Sem servidor"));
            if (_saved.Items.Count > 0) _saved.SelectedIndex = 0;
            var use = new Button { Text = "Usar na linha selecionada", Location = new Point(574, 60), Size = new Size(204, 28), Enabled = _profiles.Count > 0 };
            _grid = new DataGridView { Location = new Point(12, 100), Size = new Size(846, 260), AllowUserToAddRows = false,
                AllowUserToDeleteRows = false, RowHeadersVisible = false, AutoGenerateColumns = false, MultiSelect = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect, BackgroundColor = Color.White };
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Window", HeaderText = "Cliente / título da janela", ReadOnly = true, Width = 280 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Server", HeaderText = "Servidor", Width = 210 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Character", HeaderText = "Nome do personagem", Width = 215 });
            var classes = new DataGridViewComboBoxColumn { Name = "Class", HeaderText = "Classe", Width = 115, FlatStyle = FlatStyle.Flat };
            classes.Items.AddRange("Automático", "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS"); _grid.Columns.Add(classes);
            var selectedLegacy = new Dictionary<string, CharacterProgress>();
            foreach (ClientWindowInfo window in windows)
            {
                ClientServer server = bindings.ServerFor(window);
                ClientIdentity manual = bindings.ManualFor(window);
                ClientIdentity detected = manual ?? ClientIdentities.Automatic(window, windows, bindings, aliases, _profiles);
                int index = _grid.Rows.Add("PID " + window.ProcessId + " · " + window.Title, server == null ? "" : server.Name,
                    detected == null ? "" : detected.Name,
                    manual == null ? "Automático" : manual.ClassCode);
                _grid.Rows[index].Tag = window.SessionKey;
                if (detected != null) _grid.Rows[index].Cells["Class"].ToolTipText = "Classe: " + detected.ClassCode +
                    (detected.FromMemory ? " (memória)" : detected.Manual ? " (manual)" : " (título)");
                if (window.Handle == active) _grid.CurrentCell = _grid.Rows[index].Cells["Character"];
            }
            _grid.CurrentCellDirtyStateChanged += delegate {
                if (_grid.IsCurrentCellDirty && _grid.CurrentCell != null && _grid.CurrentCell.OwningColumn.Name == "Class")
                    _grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };
            _grid.CellValueChanged += delegate(object sender, DataGridViewCellEventArgs args) {
                bool serverChanged = args.ColumnIndex == _grid.Columns["Server"].Index;
                if (args.RowIndex < 0 || (!serverChanged && args.ColumnIndex != _grid.Columns["Class"].Index)) return;
                var row = _grid.Rows[args.RowIndex];
                var window = windows.Find(w => w.SessionKey == (string)row.Tag);
                if (serverChanged)
                {
                    selectedLegacy.Remove(window.SessionKey);
                    row.Cells["Class"].Value = "Automático";
                }
                if (Convert.ToString(row.Cells["Class"].Value) != "Automático") return;
                try
                {
                    ClientServer server = ClientServer.ForSelection(window.Server, Convert.ToString(row.Cells["Server"].Value));
                    ClientIdentity automatic = ClientIdentities.Automatic(window, windows, bindings, aliases, _profiles, server);
                    row.Cells["Character"].Value = automatic == null ? "" : automatic.Name;
                }
                catch (ArgumentException) { row.Cells["Character"].Value = ""; }
            };
            use.Click += delegate {
                if (_grid.CurrentRow == null || _saved.SelectedIndex < 0) return;
                _grid.EndEdit();
                DataGridViewRow row = _grid.CurrentRow;
                var window = windows.Find(w => w.SessionKey == (string)row.Tag);
                CharacterProgress p = _profiles[_saved.SelectedIndex];
                try
                {
                    if (!string.IsNullOrEmpty(p.ServerKey))
                    {
                        ClientServer server = ClientServer.ForSelection(window.Server, p.ServerName);
                        if (!ClientServer.Matches(server, p.ServerKey))
                            throw new ArgumentException("Este perfil pertence a outra instalação do PW.");
                        row.Cells["Server"].Value = p.ServerName;
                    }
                    selectedLegacy.Remove(window.SessionKey);
                    if (string.IsNullOrEmpty(p.ServerKey)) selectedLegacy[window.SessionKey] = p;
                    row.Cells["Character"].Value = p.CharacterName;
                    row.Cells["Class"].Value = p.ClassCode;
                }
                catch (ArgumentException error) { MessageBox.Show(this, error.Message, "Confira o servidor"); }
            };
            var save = new Button { Text = "Salvar", Location = new Point(672, 376), Size = new Size(88, 30), Enabled = windows.Count > 0 };
            save.Click += delegate {
                _grid.EndEdit();
                var result = new Dictionary<string, ClientIdentity>();
                var servers = new Dictionary<string, ClientServer>();
                var legacy = new Dictionary<string, CharacterProgress>();
                var resolved = new List<ClientIdentity>();
                try {
                    foreach (DataGridViewRow row in _grid.Rows) {
                        string code = Convert.ToString(row.Cells["Class"].Value);
                        var window = windows.Find(w => w.SessionKey == (string)row.Tag);
                        ClientServer server = ClientServer.ForSelection(window.Server, Convert.ToString(row.Cells["Server"].Value));
                        servers.Add(window.SessionKey, server);
                        string name = Convert.ToString(row.Cells["Character"].Value).Trim();
                        if (window.ServerRequired && server == null && name.Length > 0)
                            throw new ArgumentException("Informe o servidor deste personagem.");
                        ClientIdentity auto = ClientIdentities.Automatic(window, windows, bindings, aliases, _profiles, server);
                        ClientIdentity selection = null;
                        if (code == "Automático" && name.Length > 0 && (auto == null || !string.Equals(auto.Name, name, StringComparison.OrdinalIgnoreCase)))
                        {
                            selection = ClientIdentities.FromSavedProfile(name, _profiles, true, server);
                            if (selection == null)
                                throw new ArgumentException("Selecione a classe do personagem digitado ou use um perfil salvo.");
                        }
                        else if (code != "Automático") selection = new ClientIdentity(name, code, true).OnServer(server);
                        result.Add(window.SessionKey, selection);
                        if (selection != null || auto != null) resolved.Add(selection ?? auto);
                        CharacterProgress previous;
                        if (server != null && selection != null && selectedLegacy.TryGetValue(window.SessionKey, out previous) &&
                            string.Equals(previous.CharacterName, selection.Name, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(previous.ClassCode, selection.ClassCode, StringComparison.OrdinalIgnoreCase))
                            legacy.Add(window.SessionKey, previous);
                    }
                    if (resolved.GroupBy(i => i.ProfileKey).Any(group => group.Count() > 1))
                        throw new ArgumentException("O mesmo personagem e servidor foram associados a dois clientes.");
                    if (legacy.Values.Distinct().Count() != legacy.Count)
                        throw new ArgumentException("Vincule cada perfil antigo a um único servidor.");
                    Selections = result; Servers = servers; LegacyProfiles = legacy; DialogResult = DialogResult.OK;
                } catch (ArgumentException error) { MessageBox.Show(this, error.Message, "Confira nome e classe", MessageBoxButtons.OK, MessageBoxIcon.Information); }
            };
            var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Location = new Point(770, 376), Size = new Size(88, 30) };
            Controls.AddRange(new Control[] { description, _saved, use, _grid, save, cancel }); AcceptButton = save; CancelButton = cancel;
        }
    }
}
