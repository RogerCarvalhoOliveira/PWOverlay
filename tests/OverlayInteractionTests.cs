using System;
using System.Drawing;
using System.IO;
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
            }
            finally
            {
                ((NotifyIcon)Field(form, "_trayIcon")).Dispose();
                var memory = Field(form, "_memoryMonitor") as IDisposable;
                if (memory != null) memory.Dispose();
                form.Dispose();
            }
            Console.WriteLine("Overlay interaction tests passed: held drag, release, lost capture, lock, monitor persistence and ten class codes.");
            return 0;
        }
        catch (Exception error) { Console.Error.WriteLine(error); return 1; }
    }
}
