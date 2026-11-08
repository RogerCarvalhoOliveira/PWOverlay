using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace PWQuestOverlay
{
    internal sealed class RouteEditorForm : Form
    {
        private readonly RouteRepository _repository;
        private readonly string _routePath;
        private readonly string _workbookPath;
        private readonly ComboBox _routeBox;
        private readonly ComboBox _missionBox;
        private readonly TextBox _nameBox;
        private readonly TextBox _npcBox;
        private readonly TextBox _locationBox;
        private readonly TextBox _instructionBox;
        private readonly NumericUpDown _minLevel;
        private readonly NumericUpDown _expectedLevel;
        private readonly ListBox _stepsBox;
        private readonly TextBox _actionBox;
        private readonly NumericUpDown _markerX;
        private readonly NumericUpDown _markerY;
        private List<QuestMission> _missions;
        private QuestMission _mission;

        internal RouteEditorForm(RouteRepository repository, string routePath, string workbookPath)
        {
            _repository = repository;
            _routePath = routePath;
            _workbookPath = workbookPath;
            Text = "Editor de rotas — PW Quest Overlay";
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(980, 650);
            MinimumSize = new Size(900, 600);
            Font = new Font("Segoe UI", 9.0f);
            TopMost = true;

            Controls.Add(MakeLabel("Rota", 12, 12));
            _routeBox = new ComboBox { Location = new Point(12, 34), Size = new Size(170, 26), DropDownStyle = ComboBoxStyle.DropDownList };
            _routeBox.Items.AddRange(new object[] { "WR", "WBWF", "EAEP", "MC", "COMMON" });
            Controls.Add(_routeBox);
            Controls.Add(MakeLabel("Missão", 194, 12));
            _missionBox = new ComboBox { Location = new Point(194, 34), Size = new Size(774, 26), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            Controls.Add(_missionBox);

            Controls.Add(MakeLabel("Nome", 12, 72));
            _nameBox = MakeTextBox(12, 94, 460);
            Controls.Add(_nameBox);
            Controls.Add(MakeLabel("NPC inicial", 484, 72));
            _npcBox = MakeTextBox(484, 94, 484);
            Controls.Add(_npcBox);
            Controls.Add(MakeLabel("Localização", 12, 130));
            _locationBox = MakeTextBox(12, 152, 460);
            Controls.Add(_locationBox);
            Controls.Add(MakeLabel("Como encaixar na rota", 484, 130));
            _instructionBox = MakeTextBox(484, 152, 484);
            Controls.Add(_instructionBox);
            Controls.Add(MakeLabel("Nível mínimo", 12, 188));
            _minLevel = MakeLevelBox(12, 210);
            Controls.Add(_minLevel);
            Controls.Add(MakeLabel("Nível previsto", 142, 188));
            _expectedLevel = MakeLevelBox(142, 210);
            Controls.Add(_expectedLevel);
            var applyMission = new Button { Text = "Aplicar dados da missão", Location = new Point(484, 204), Size = new Size(170, 32) };
            applyMission.Click += delegate { ApplyMissionFields(); ReloadMissionCaption(); };
            Controls.Add(applyMission);

            Controls.Add(MakeLabel("Etapas", 12, 252));
            _stepsBox = new ListBox { Location = new Point(12, 274), Size = new Size(440, 300), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Bottom };
            Controls.Add(_stepsBox);
            Controls.Add(MakeLabel("Texto da etapa", 466, 252));
            _actionBox = new TextBox { Location = new Point(466, 274), Size = new Size(502, 90), Anchor = AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right, Multiline = true, ScrollBars = ScrollBars.Vertical };
            Controls.Add(_actionBox);
            Controls.Add(MakeLabel("Marcador X (0 = sem coordenada)", 466, 376));
            _markerX = MakeCoordinateBox(466, 398);
            Controls.Add(_markerX);
            Controls.Add(MakeLabel("Marcador Y", 632, 376));
            _markerY = MakeCoordinateBox(632, 398);
            Controls.Add(_markerY);

            var applyStep = new Button { Text = "Aplicar etapa", Location = new Point(466, 444), Size = new Size(112, 32) };
            var addStep = new Button { Text = "Adicionar", Location = new Point(586, 444), Size = new Size(92, 32) };
            var removeStep = new Button { Text = "Remover", Location = new Point(686, 444), Size = new Size(92, 32) };
            var upStep = new Button { Text = "↑", Location = new Point(786, 444), Size = new Size(52, 32) };
            var downStep = new Button { Text = "↓", Location = new Point(846, 444), Size = new Size(52, 32) };
            applyStep.Click += delegate { ApplyStepFields(); };
            addStep.Click += delegate { AddStep(); };
            removeStep.Click += delegate { RemoveStep(); };
            upStep.Click += delegate { MoveStep(-1); };
            downStep.Click += delegate { MoveStep(1); };
            Controls.AddRange(new Control[] { applyStep, addStep, removeStep, upStep, downStep });

            var saveButton = new Button { Text = "Salvar JSON + Excel", Location = new Point(774, 602), Size = new Size(194, 36), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            var closeButton = new Button { Text = "Fechar", Location = new Point(666, 602), Size = new Size(100, 36), Anchor = AnchorStyles.Right | AnchorStyles.Bottom };
            saveButton.Click += delegate { SaveAll(); };
            closeButton.Click += delegate { Close(); };
            Controls.Add(saveButton);
            Controls.Add(closeButton);

            _routeBox.SelectedIndexChanged += delegate { LoadRoute(); };
            _missionBox.SelectedIndexChanged += delegate { LoadMission(); };
            _stepsBox.SelectedIndexChanged += delegate { LoadStep(); };
            _routeBox.SelectedIndex = 0;
        }

        private static Label MakeLabel(string text, int x, int y) { return new Label { Text = text, Location = new Point(x, y), AutoSize = true }; }
        private TextBox MakeTextBox(int x, int y, int width) { return new TextBox { Location = new Point(x, y), Size = new Size(width, 26), Anchor = x > 400 ? AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right : AnchorStyles.Left | AnchorStyles.Top }; }
        private static NumericUpDown MakeLevelBox(int x, int y) { return new NumericUpDown { Location = new Point(x, y), Size = new Size(110, 26), Minimum = 1, Maximum = 150 }; }
        private static NumericUpDown MakeCoordinateBox(int x, int y) { return new NumericUpDown { Location = new Point(x, y), Size = new Size(140, 26), Minimum = 0, Maximum = 9999 }; }

        private void LoadRoute()
        {
            string key = Convert.ToString(_routeBox.SelectedItem);
            _missions = _repository.AllMissions.Where(item => item.routeKey == key).OrderBy(item => item.order).ToList();
            _missionBox.Items.Clear();
            foreach (QuestMission mission in _missions) _missionBox.Items.Add(mission.order + ". " + mission.name);
            if (_missionBox.Items.Count > 0) _missionBox.SelectedIndex = 0;
        }

        private void LoadMission()
        {
            if (_missionBox.SelectedIndex < 0) return;
            _mission = _missions[_missionBox.SelectedIndex];
            _nameBox.Text = _mission.name;
            _npcBox.Text = _mission.npc;
            _locationBox.Text = _mission.location;
            _instructionBox.Text = _mission.instruction;
            _minLevel.Value = Math.Max(1, Math.Min(150, _mission.minLevel));
            _expectedLevel.Value = Math.Max(1, Math.Min(150, _mission.expectedLevel <= 0 ? _mission.minLevel : _mission.expectedLevel));
            ReloadSteps();
        }

        private void ApplyMissionFields()
        {
            if (_mission == null) return;
            _mission.name = _nameBox.Text.Trim();
            _mission.npc = _npcBox.Text.Trim();
            _mission.location = _locationBox.Text.Trim();
            _mission.instruction = _instructionBox.Text.Trim();
            _mission.minLevel = (int)_minLevel.Value;
            _mission.expectedLevel = (int)_expectedLevel.Value;
        }

        private void ReloadMissionCaption()
        {
            if (_missionBox.SelectedIndex >= 0) _missionBox.Items[_missionBox.SelectedIndex] = _mission.order + ". " + _mission.name;
        }

        private void ReloadSteps()
        {
            _stepsBox.Items.Clear();
            foreach (QuestStep step in _mission.steps) _stepsBox.Items.Add(step.id + " — " + step.action);
            if (_stepsBox.Items.Count > 0) _stepsBox.SelectedIndex = 0;
        }

        private void LoadStep()
        {
            if (_mission == null || _stepsBox.SelectedIndex < 0) return;
            QuestStep step = _mission.steps[_stepsBox.SelectedIndex];
            _actionBox.Text = step.action;
            _markerX.Value = step.markerX ?? 0;
            _markerY.Value = step.markerY ?? 0;
        }

        private void ApplyStepFields()
        {
            if (_mission == null || _stepsBox.SelectedIndex < 0) return;
            int index = _stepsBox.SelectedIndex;
            QuestStep step = _mission.steps[index];
            step.action = _actionBox.Text.Trim();
            step.markerX = _markerX.Value == 0 ? (int?)null : (int)_markerX.Value;
            step.markerY = _markerY.Value == 0 ? (int?)null : (int)_markerY.Value;
            ReloadSteps();
            _stepsBox.SelectedIndex = index;
        }

        private void AddStep()
        {
            if (_mission == null) return;
            _mission.steps.Add(new QuestStep { id = "e" + (_mission.steps.Count + 1), action = "Nova etapa" });
            ReloadSteps();
            _stepsBox.SelectedIndex = _stepsBox.Items.Count - 1;
        }

        private void RemoveStep()
        {
            if (_mission == null || _stepsBox.SelectedIndex < 0 || _mission.steps.Count <= 1) return;
            if (MessageBox.Show("Remover esta etapa?", "Editor de rotas", MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            _mission.steps.RemoveAt(_stepsBox.SelectedIndex);
            RenumberSteps();
            ReloadSteps();
        }

        private void MoveStep(int direction)
        {
            if (_mission == null || _stepsBox.SelectedIndex < 0) return;
            int oldIndex = _stepsBox.SelectedIndex;
            int newIndex = oldIndex + direction;
            if (newIndex < 0 || newIndex >= _mission.steps.Count) return;
            QuestStep step = _mission.steps[oldIndex];
            _mission.steps.RemoveAt(oldIndex);
            _mission.steps.Insert(newIndex, step);
            RenumberSteps();
            ReloadSteps();
            _stepsBox.SelectedIndex = newIndex;
        }

        private void RenumberSteps()
        {
            for (int index = 0; index < _mission.steps.Count; index++) _mission.steps[index].id = "e" + (index + 1);
        }

        private void SaveAll()
        {
            ApplyMissionFields();
            ApplyStepFields();
            try
            {
                string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
                if (File.Exists(_routePath)) File.Copy(_routePath, _routePath + "." + stamp + ".bak", true);
                if (File.Exists(_workbookPath)) File.Copy(_workbookPath, _workbookPath + "." + stamp + ".bak", true);
                _repository.Save(_routePath);
                WorkbookSynchronizer.Sync(_workbookPath, _repository.AllMissions);
                MessageBox.Show("JSON e planilha atualizados. Backups .bak foram criados.", "Editor de rotas");
            }
            catch (Exception error)
            {
                MessageBox.Show("O JSON pode ter sido salvo, mas a sincronização falhou:\n\n" + error.Message,
                    "Falha ao salvar", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    internal static class WorkbookSynchronizer
    {
        internal static void Sync(string path, List<QuestMission> missions)
        {
            if (!File.Exists(path)) throw new FileNotFoundException("Planilha de rotas não encontrada.", path);
            Type excelType = Type.GetTypeFromProgID("Excel.Application");
            if (excelType == null) throw new InvalidOperationException("Microsoft Excel não está instalado.");
            dynamic excel = null;
            dynamic workbook = null;
            try
            {
                excel = Activator.CreateInstance(excelType);
                excel.Visible = false;
                excel.DisplayAlerts = false;
                workbook = excel.Workbooks.Open(path);
                List<QuestMission> common = missions.Where(m => m.routeKey == "COMMON").ToList();
                SyncSheet(workbook, "Humanos - WR", missions.Where(m => m.routeKey == "WR").Concat(common).OrderBy(m => m.order).ToList());
                SyncSheet(workbook, "Selvagens - WB-WF", missions.Where(m => m.routeKey == "WBWF").Concat(common).OrderBy(m => m.order).ToList());
                SyncSheet(workbook, "Alados - EA-EP", missions.Where(m => m.routeKey == "EAEP").Concat(common).OrderBy(m => m.order).ToList());
                SyncSheet(workbook, "Abissais - MC", missions.Where(m => m.routeKey == "MC").Concat(common).OrderBy(m => m.order).ToList());
                workbook.Save();
            }
            finally
            {
                try { if (workbook != null) workbook.Close(false); } catch { }
                try { if (excel != null) excel.Quit(); } catch { }
                if (workbook != null) Marshal.FinalReleaseComObject(workbook);
                if (excel != null) Marshal.FinalReleaseComObject(excel);
            }
        }

        private static void SyncSheet(dynamic workbook, string sheetName, List<QuestMission> missions)
        {
            dynamic sheet = null;
            dynamic table = null;
            try
            {
                sheet = workbook.Worksheets[sheetName];
                int lastRow = 7 + missions.Count;
                if (missions.Count > 0)
                {
                    dynamic template = sheet.Range["A8:O8"];
                    dynamic target = sheet.Range["A8:O" + lastRow];
                    template.Copy();
                    target.PasteSpecial(-4122);
                    Marshal.FinalReleaseComObject(template);
                    Marshal.FinalReleaseComObject(target);
                }
                sheet.Cells[7, 15].Value2 = "Nível previsto";
                for (int index = 0; index < missions.Count; index++)
                {
                    int row = 8 + index;
                    QuestMission mission = missions[index];
                    sheet.Cells[row, 1].Value2 = "Não";
                    sheet.Cells[row, 2].Value2 = mission.order;
                    sheet.Cells[row, 3].Value2 = mission.classScope;
                    sheet.Cells[row, 4].Value2 = mission.minLevel;
                    sheet.Cells[row, 5].Value2 = mission.maxLevel;
                    sheet.Cells[row, 6].Value2 = mission.name;
                    sheet.Cells[row, 7].Value2 = mission.npc;
                    sheet.Cells[row, 8].Value2 = mission.location;
                    sheet.Cells[row, 9].Value2 = mission.baseXp;
                    sheet.Cells[row, 10].Formula = "=I" + row + "*3";
                    sheet.Cells[row, 11].Value2 = mission.priority;
                    sheet.Cells[row, 12].Value2 = mission.benefit;
                    sheet.Cells[row, 13].Value2 = mission.instruction;
                    sheet.Cells[row, 14].Value2 = string.Join(Environment.NewLine,
                        mission.steps.Select((step, stepIndex) => (stepIndex + 1) + ". " + step.action));
                    sheet.Cells[row, 15].Value2 = mission.expectedLevel <= 0 ? mission.minLevel : mission.expectedLevel;
                }
                table = sheet.ListObjects[1];
                table.Resize(sheet.Range["A7:O" + lastRow]);
                sheet.Range["A8:O" + lastRow].Rows.AutoFit();
                sheet.Columns[15].ColumnWidth = 12;
            }
            finally
            {
                if (table != null) Marshal.FinalReleaseComObject(table);
                if (sheet != null) Marshal.FinalReleaseComObject(sheet);
            }
        }
    }
}
