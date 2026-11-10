using System;
using System.Collections;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using PWQuestOverlay;

// Synthetic control events only: no game input, cursor movement or real profiles.
internal static class OverlayInteractionTests
{
    const BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;
    static object Field(object instance, string name)
    { return instance.GetType().GetField(name, Hidden).GetValue(instance); }
    static void Raise(Control control, string name, MouseButtons buttons)
    { typeof(Control).GetMethod(name, Hidden).Invoke(control, new object[] { new MouseEventArgs(buttons, 1, 3, 3, 0) }); }
    static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); }

    static void TestClientProfiles(Assembly assembly)
    {
        var windowType = assembly.GetType("PWQuestOverlay.ClientWindowInfo");
        var windows = (IList)Activator.CreateInstance(typeof(System.Collections.Generic.List<>).MakeGenericType(windowType));
        for (int i = 1; i <= 2; i++)
        {
            object window = Activator.CreateInstance(windowType, true);
            foreach (var pair in new System.Collections.Generic.Dictionary<string, object> {
                { "Handle", new IntPtr(i) }, { "ProcessId", i }, { "SessionKey", "client" + i }, { "Title", "Perfect World" } })
                windowType.GetProperty(pair.Key, Hidden).SetValue(window, pair.Value, null);
            windows.Add(window);
        }
        var bindings = Activator.CreateInstance(assembly.GetType("PWQuestOverlay.ClientBindings"), true);
        var type = assembly.GetType("PWQuestOverlay.ClientProfilesForm");
        var profiles = new System.Collections.Generic.List<CharacterProgress> { new CharacterProgress { CharacterName = "Segundo", ClassCode = "EP" } };
        using (var form = (Form)Activator.CreateInstance(type, Hidden, null, new object[] {
            windows, bindings, new System.Collections.Generic.Dictionary<string, string>(), profiles, new IntPtr(2) }, null))
        {
            var grid = (DataGridView)Field(form, "_grid");
            Check(grid.Rows.Count == 2 && grid.CurrentRow.Index == 1, "Identical window titles need separate rows; active client must be selected.");
            grid.Rows[0].Cells["Character"].Value = "Primeiro"; grid.Rows[0].Cells["Class"].Value = "WB";
            var use = form.Controls.OfType<Button>().Single(b => b.Text.StartsWith("Usar"));
            var click = typeof(Button).GetMethod("OnClick", Hidden);
            click.Invoke(use, new object[] { EventArgs.Empty });
            Check(Convert.ToString(grid.Rows[0].Cells["Character"].Value) == "Primeiro" && Convert.ToString(grid.Rows[1].Cells["Character"].Value) == "Segundo", "A saved profile must affect only the selected client.");
            grid.Rows[1].Cells["Class"].Value = "Automático";
            Check(Convert.ToString(grid.Rows[1].Cells["Character"].Value) == "", "Automatic must release a manual association on a generic title.");
            click.Invoke(use, new object[] { EventArgs.Empty });
            click.Invoke(form.Controls.OfType<Button>().Single(b => b.Text == "Salvar"), new object[] { EventArgs.Empty });
            var result = (IDictionary)type.GetProperty("Selections", Hidden).GetValue(form, null);
            var identity = assembly.GetType("PWQuestOverlay.ClientIdentity").GetProperty("ProfileKey", Hidden);
            Check((string)identity.GetValue(result["client1"], null) == "PRIMEIRO|WB" && (string)identity.GetValue(result["client2"], null) == "SEGUNDO|EP", "Saving must preserve independent per-client identities.");
        }
        var identityType = assembly.GetType("PWQuestOverlay.ClientIdentity");
        object automaticIdentity = Activator.CreateInstance(identityType, Hidden, null, new object[] { "Detectado", "MC", false, true }, null);
        bindings.GetType().GetMethod("SetMemory", Hidden).Invoke(bindings, new object[] { "client1", automaticIdentity });
        using (var form = (Form)Activator.CreateInstance(type, Hidden, null, new object[] {
            windows, bindings, new System.Collections.Generic.Dictionary<string, string>(), profiles, new IntPtr(1) }, null))
        {
            var grid = (DataGridView)Field(form, "_grid");
            Check(Convert.ToString(grid.Rows[0].Cells["Character"].Value) == "Detectado" &&
                grid.Rows[0].Cells["Class"].ToolTipText.Contains("MC (memória)"), "Profiles must show the memory name and detected class with generic titles.");
            grid.Rows[0].Cells["Class"].Value = "WB";
            grid.Rows[0].Cells["Character"].Value = "Alias";
            grid.Rows[0].Cells["Class"].Value = "Automático";
            Check(Convert.ToString(grid.Rows[0].Cells["Character"].Value) == "Detectado", "Automatic must restore the name detected from memory.");
            typeof(Button).GetMethod("OnClick", Hidden).Invoke(form.Controls.OfType<Button>().Single(b => b.Text == "Salvar"), new object[] { EventArgs.Empty });
            var result = (IDictionary)type.GetProperty("Selections", Hidden).GetValue(form, null);
            Check(result != null && result["client1"] == null, "Saving an automatic memory profile must not create a manual override.");
        }
    }

    [STAThread]
    static int Main(string[] args)
    {
        try
        {
            string root = Path.GetFullPath(args[0]);
            string temporary = Path.Combine(root, "work", "ui-regression-state");
            Directory.CreateDirectory(temporary);
            var store = new StateStore();
            typeof(StateStore).GetField("_directory", Hidden).SetValue(store, temporary);
            typeof(StateStore).GetField("_statePath", Hidden).SetValue(store, Path.Combine(temporary, "state.json"));
            typeof(StateStore).GetField("_logPath", Hidden).SetValue(store, Path.Combine(temporary, "overlay.log"));
            var state = new AppState();
            var type = typeof(RouteRepository).Assembly.GetType("PWQuestOverlay.OverlayForm");
            TestClientProfiles(type.Assembly);
            var native = type.Assembly.GetType("PWQuestOverlay.NativeMethods");
            var startToken = native.GetMethod("ProcessStartToken", BindingFlags.Static | BindingFlags.NonPublic);
            int ownPid = System.Diagnostics.Process.GetCurrentProcess().Id;
            long token = (long)startToken.Invoke(null, new object[] { ownPid });
            Check(token > 0 && token == (long)startToken.Invoke(null, new object[] { ownPid }), "Process lifetime token must be stable without reading game memory.");
            var form = (Form)Activator.CreateInstance(type, Hidden, null, new object[] {
                RouteRepository.Load(Path.Combine(root, "data", "quest_routes.json")), store, state,
                Path.Combine(root, "data", "quest_routes.json"), Path.Combine(root, "data", "rota_leveling_pw_144.xlsx") }, null);
            ((Timer)Field(form, "_timer")).Stop();
            try
            {
                // No Show/Application.Run: timers, hotkeys and game polling stay idle.
                IntPtr handle = form.Handle;
                foreach (string name in new[] { "_topBar", "_profileLabel" })
                {
                    var control = (Control)Field(form, name);
                    handle = control.Handle;
                    form.Location = new Point(80, 80);
                    state.PositionLocked = false;
                    Raise(control, "OnMouseDown", MouseButtons.Left);
                    Check((bool)Field(form, "_dragging") && control.Capture, "Press must capture on the originating control: " + name);
                    type.GetField("_dragCursorStart", Hidden).SetValue(form, new Point(Cursor.Position.X - 40, Cursor.Position.Y - 20));
                    Raise(control, "OnMouseMove", MouseButtons.Left);
                    Check(form.Location == new Point(120, 100), "Held left button must move the overlay.");
                    Raise(control, "OnMouseUp", MouseButtons.Right);
                    Check((bool)Field(form, "_dragging"), "Right release must not replace left release.");
                    Raise(control, "OnMouseUp", MouseButtons.Left);
                    Check(!(bool)Field(form, "_dragging") && !control.Capture, "Left release must finish drag.");
                    Point placed = form.Location;
                    Raise(control, "OnMouseMove", MouseButtons.None);
                    Check(form.Location == placed && state.Positions.Count > 0, "Released position must persist.");
                    Check(!string.IsNullOrEmpty(state.OverlayMonitorDeviceName), "Chosen monitor must persist.");
                    Raise(control, "OnMouseDown", MouseButtons.Left);
                    control.Capture = false;
                    Check(!(bool)Field(form, "_dragging"), "Losing mouse capture must finish drag.");
                    state.PositionLocked = true;
                    Raise(control, "OnMouseDown", MouseButtons.Left);
                    Check(!(bool)Field(form, "_dragging"), "Locked overlay must not start moving.");
                }
                foreach (string code in new[] { "WR", "MG", "WB", "WF", "EA", "EP", "MC", "ES", "SK", "MS" })
                {
                    object[] title = { "Teste[" + code + "]", null, null };
                    type.GetMethod("ParseTitle", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, title);
                    Check((string)title[1] == "Teste" && (string)title[2] == code, "Title recognition failed: " + code);
                    Check((bool)type.GetMethod("IsValidClass", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, new object[] { code }), "Manual association rejected " + code);
                }
                var choiceType = type.Assembly.GetType("PWQuestOverlay.CultivationChoiceForm");
                using (var choice = (Form)Activator.CreateInstance(choiceType, Hidden, null, new object[] { "Teste", null }, null))
                {
                    var god = (RadioButton)Field(choice, "_god");
                    var evil = (RadioButton)Field(choice, "_evil");
                    var confirm = (Button)Field(choice, "_confirm");
                    var selected = choiceType.GetProperty("SelectedPath", Hidden);
                    Check(!god.Checked && !evil.Checked && !confirm.Enabled && selected.GetValue(choice, null) == null,
                        "Cultivation choice must wait for an explicit selection.");
                    god.Checked = true;
                    Check(confirm.Enabled && (string)selected.GetValue(choice, null) == "GOD", "God selection failed.");
                    evil.Checked = true;
                    Check(!god.Checked && confirm.Enabled && (string)selected.GetValue(choice, null) == "EVIL", "Evil selection failed.");
                }
            }
            finally
            {
                ((NotifyIcon)Field(form, "_trayIcon")).Dispose();
                var memory = Field(form, "_memoryMonitor") as IDisposable;
                if (memory != null) memory.Dispose();
                form.Dispose();
            }
            Console.WriteLine("Overlay interaction tests passed: independent client profiles, saved-profile selection, process lifetime token, dragging, monitor persistence, ten classes and God/Evil.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
