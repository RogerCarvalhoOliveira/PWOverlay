using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class PWBorderless
{
    private const int GWL_STYLE = -16;
    private const int GWL_EXSTYLE = -20;
    private const int GA_ROOT = 2;
    private const uint GW_OWNER = 4;

    private const long WS_BORDER = 0x00800000L;
    private const long WS_DLGFRAME = 0x00400000L;
    private const long WS_CAPTION = WS_BORDER | WS_DLGFRAME;
    private const long WS_THICKFRAME = 0x00040000L;
    private const long WS_MINIMIZEBOX = 0x00020000L;
    private const long WS_MAXIMIZEBOX = 0x00010000L;
    private const long WS_SYSMENU = 0x00080000L;
    private const long WS_MINIMIZE = 0x20000000L;
    private const long WS_MAXIMIZE = 0x01000000L;
    private const long WS_POPUP = unchecked((long)0x80000000L);
    private const long WS_VISIBLE = 0x10000000L;

    private const long WS_EX_DLGMODALFRAME = 0x00000001L;
    private const long WS_EX_CLIENTEDGE = 0x00000200L;
    private const long WS_EX_STATICEDGE = 0x00020000L;
    private const long WS_EX_WINDOWEDGE = 0x00000100L;

    private const uint MONITOR_DEFAULTTONEAREST = 2;
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const uint SWP_NOACTIVATE = 0x0010;
    private const uint SWP_NOOWNERZORDER = 0x0200;
    private const uint SWP_FRAMECHANGED = 0x0020;
    private const uint SWP_SHOWWINDOW = 0x0040;
    private const int SW_RESTORE = 9;
    private const int SW_MAXIMIZE = 3;

    private static readonly IntPtr HWND_TOP = IntPtr.Zero;
    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "PWQuestOverlay");
    private static readonly string LogPath = Path.Combine(LogDirectory, "PWBorderless.log");

    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public override string ToString()
        {
            return string.Format("{0},{1}-{2},{3}", Left, Top, Right, Bottom);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private sealed class OriginalWindowState
    {
        public long Style;
        public long ExStyle;
        public RECT Bounds;
        public bool WasMaximized;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    private struct MONITORINFO
    {
        public int Size;
        public RECT Monitor;
        public RECT Work;
        public uint Flags;
    }

    [DllImport("user32.dll")]
    private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int GetWindowTextLength(IntPtr window);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr window, uint command);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr window, int flags);

    [DllImport("user32.dll")]
    private static extern bool IsWindow(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool IsZoomed(IntPtr window);

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr window, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr window, ref POINT point);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr window, out RECT rect);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowLongPtr(IntPtr window, int index, IntPtr value);

    [DllImport("user32.dll")]
    private static extern IntPtr MonitorFromWindow(IntPtr window, uint flags);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern bool GetMonitorInfo(IntPtr monitor, ref MONITORINFO info);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr window, int command);

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

    private static void Log(string message)
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogPath,
                DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + " " + message + Environment.NewLine);
        }
        catch { }
    }

    private static HashSet<int> GetGameProcessIds()
    {
        var ids = new HashSet<int>();
        foreach (Process game in Process.GetProcessesByName("elementclient"))
        {
            try { ids.Add(game.Id); }
            catch { }
            finally { game.Dispose(); }
        }
        return ids;
    }

    private static HashSet<int> GetPwHelperProcessIds()
    {
        var ids = new HashSet<int>();
        foreach (Process helper in Process.GetProcessesByName("PW Helper"))
        {
            try { ids.Add(helper.Id); }
            catch { }
            finally { helper.Dispose(); }
        }
        return ids;
    }

    private static bool WindowBelongsTo(IntPtr window, HashSet<int> processIds)
    {
        if (window == IntPtr.Zero || processIds.Count == 0)
            return false;
        uint processId;
        GetWindowThreadProcessId(window, out processId);
        return processIds.Contains((int)processId);
    }

    private static string GetWindowTitle(IntPtr window)
    {
        int length = GetWindowTextLength(window);
        if (length <= 0)
            return string.Empty;
        var text = new StringBuilder(length + 1);
        GetWindowText(window, text, text.Capacity);
        return text.ToString();
    }

    private static List<IntPtr> FindPwHelperOverlayWindows()
    {
        HashSet<int> helperProcessIds = GetPwHelperProcessIds();
        var windows = new List<IntPtr>();
        if (helperProcessIds.Count == 0)
            return windows;

        EnumWindows(delegate(IntPtr window, IntPtr parameter)
        {
            uint processId;
            GetWindowThreadProcessId(window, out processId);
            if (!helperProcessIds.Contains((int)processId) || !IsWindowVisible(window))
                return true;

            string title = GetWindowTitle(window).Trim();
            if (title.Length > 0 && !string.Equals(title, "PW Helper", StringComparison.OrdinalIgnoreCase))
                windows.Add(window); // todos os painéis flutuantes, independentemente do nome
            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static void RaisePwHelperOverlay()
    {
        foreach (IntPtr overlay in FindPwHelperOverlayWindows())
        {
            bool result = SetWindowPos(
                overlay,
                HWND_TOPMOST,
                0,
                0,
                0,
                0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE | SWP_NOOWNERZORDER);
            Log(string.Format(
                "PW Helper overlay raised hwnd=0x{0:X} result={1}",
                overlay.ToInt64(), result));
        }
    }

    private static List<IntPtr> FindGameWindows()
    {
        var processIds = new HashSet<int>();
        var windows = new List<IntPtr>();
        foreach (Process game in Process.GetProcessesByName("elementclient"))
        {
            try
            {
                processIds.Add(game.Id);
                game.Refresh();
                IntPtr mainWindow = game.MainWindowHandle;
                if (mainWindow != IntPtr.Zero && IsWindow(mainWindow))
                    windows.Add(mainWindow);
            }
            catch { }
            finally { game.Dispose(); }
        }

        if (processIds.Count == 0)
            return windows;

        EnumWindows(delegate(IntPtr window, IntPtr parameter)
        {
            uint processId;
            GetWindowThreadProcessId(window, out processId);
            if (!processIds.Contains((int)processId) || !IsWindowVisible(window))
                return true;

            RECT client;
            if (!GetClientRect(window, out client))
                return true;

            int width = client.Right - client.Left;
            int height = client.Bottom - client.Top;
            if (width >= 640 && height >= 480 && !windows.Contains(window))
                windows.Add(window);

            return true;
        }, IntPtr.Zero);

        return windows;
    }

    private static bool GetMonitorBounds(IntPtr window, out RECT bounds)
    {
        bounds = new RECT();
        IntPtr monitor = MonitorFromWindow(window, MONITOR_DEFAULTTONEAREST);
        var info = new MONITORINFO { Size = Marshal.SizeOf(typeof(MONITORINFO)) };
        if (!GetMonitorInfo(monitor, ref info))
            return false;
        bounds = info.Monitor;
        return true;
    }

    private static bool IsClientAreaFullscreen(IntPtr window)
    {
        RECT monitorBounds;
        RECT client;
        var origin = new POINT { X = 0, Y = 0 };
        if (!GetMonitorBounds(window, out monitorBounds) ||
            !GetClientRect(window, out client) ||
            !ClientToScreen(window, ref origin))
            return false;

        return origin.X == monitorBounds.Left &&
            origin.Y == monitorBounds.Top &&
            client.Right - client.Left == monitorBounds.Right - monitorBounds.Left &&
            client.Bottom - client.Top == monitorBounds.Bottom - monitorBounds.Top;
    }

    private static bool IsAlreadyBorderless(IntPtr window)
    {
        RECT bounds;
        RECT current;
        if (!GetMonitorBounds(window, out bounds) || !GetWindowRect(window, out current))
            return false;

        long style = GetWindowLongPtr(window, GWL_STYLE).ToInt64();
        bool noFrame = (style & (WS_CAPTION | WS_THICKFRAME | WS_BORDER | WS_DLGFRAME)) == 0;
        bool fillsMonitor = current.Left == bounds.Left && current.Top == bounds.Top &&
            current.Right == bounds.Right && current.Bottom == bounds.Bottom;
        return noFrame && fillsMonitor;
    }

    private static bool ApplyBorderless(IntPtr window)
    {
        if (window == IntPtr.Zero || !IsWindow(window) || IsIconic(window))
            return false;

        RECT monitorBounds;
        RECT before;
        if (!GetMonitorBounds(window, out monitorBounds))
            return false;
        GetWindowRect(window, out before);

        if (IsZoomed(window))
        {
            ShowWindow(window, SW_RESTORE);
            Thread.Sleep(75);
        }

        long originalStyle = GetWindowLongPtr(window, GWL_STYLE).ToInt64();
        long style = originalStyle;
        style &= ~(WS_CAPTION | WS_THICKFRAME | WS_MINIMIZEBOX | WS_MAXIMIZEBOX |
            WS_SYSMENU | WS_MINIMIZE | WS_MAXIMIZE);
        style |= WS_POPUP | WS_VISIBLE;
        SetWindowLongPtr(window, GWL_STYLE, new IntPtr(style));
        int styleError = Marshal.GetLastWin32Error();

        long originalExStyle = GetWindowLongPtr(window, GWL_EXSTYLE).ToInt64();
        long exStyle = originalExStyle;
        exStyle &= ~(WS_EX_DLGMODALFRAME | WS_EX_CLIENTEDGE | WS_EX_STATICEDGE | WS_EX_WINDOWEDGE);
        SetWindowLongPtr(window, GWL_EXSTYLE, new IntPtr(exStyle));
        int exStyleError = Marshal.GetLastWin32Error();

        int width = monitorBounds.Right - monitorBounds.Left;
        int height = monitorBounds.Bottom - monitorBounds.Top;
        bool result = SetWindowPos(
            window,
            HWND_TOP,
            monitorBounds.Left,
            monitorBounds.Top,
            width,
            height,
            SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);

        RECT after;
        RECT client;
        GetWindowRect(window, out after);
        GetClientRect(window, out client);
        bool verified = result &&
            after.Left == monitorBounds.Left && after.Top == monitorBounds.Top &&
            after.Right == monitorBounds.Right && after.Bottom == monitorBounds.Bottom &&
            client.Right - client.Left == width && client.Bottom - client.Top == height;
        Log(string.Format(
            "apply hwnd=0x{0:X} before={1} target={2} after={3} client={4} " +
            "style=0x{5:X}->0x{6:X} styleErr={7} ex=0x{8:X}->0x{9:X} exErr={10} " +
            "result={11} verified={12}",
            window.ToInt64(), before, monitorBounds, after, client,
            originalStyle, style, styleError, originalExStyle, exStyle, exStyleError,
            result, verified));

        if (!verified)
        {
            // Some secondary PW clients restore their framed style immediately.
            // Keep that frame, but move the non-client area outside the monitor so
            // the actual game client still fills the complete screen.
            var clientOrigin = new POINT { X = 0, Y = 0 };
            GetWindowRect(window, out after);
            GetClientRect(window, out client);
            if (ClientToScreen(window, ref clientOrigin))
            {
                int clientWidth = client.Right - client.Left;
                int clientHeight = client.Bottom - client.Top;
                int outerWidth = after.Right - after.Left;
                int outerHeight = after.Bottom - after.Top;
                int leftInset = clientOrigin.X - after.Left;
                int topInset = clientOrigin.Y - after.Top;
                int rightInset = outerWidth - clientWidth - leftInset;
                int bottomInset = outerHeight - clientHeight - topInset;

                bool sensibleInsets = leftInset >= 0 && rightInset >= 0 &&
                    topInset >= 0 && bottomInset >= 0 &&
                    leftInset <= 100 && rightInset <= 100 &&
                    topInset <= 100 && bottomInset <= 100;

                if (sensibleInsets)
                {
                    int framedWidth = width + leftInset + rightInset;
                    int framedHeight = height + topInset + bottomInset;
                    bool fallbackResult = SetWindowPos(
                        window,
                        HWND_TOP,
                        monitorBounds.Left - leftInset,
                        monitorBounds.Top - topInset,
                        framedWidth,
                        framedHeight,
                        SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_FRAMECHANGED |
                            SWP_SHOWWINDOW);

                    var finalOrigin = new POINT { X = 0, Y = 0 };
                    RECT finalWindow;
                    RECT finalClient;
                    GetWindowRect(window, out finalWindow);
                    GetClientRect(window, out finalClient);
                    ClientToScreen(window, ref finalOrigin);
                    bool fallbackVerified = fallbackResult &&
                        finalOrigin.X == monitorBounds.Left &&
                        finalOrigin.Y == monitorBounds.Top &&
                        finalClient.Right - finalClient.Left == width &&
                        finalClient.Bottom - finalClient.Top == height;

                    Log(string.Format(
                        "frame fallback hwnd=0x{0:X} insets={1},{2},{3},{4} " +
                        "window={5} client={6} origin={7},{8} result={9} verified={10}",
                        window.ToInt64(), leftInset, topInset, rightInset, bottomInset,
                        finalWindow, finalClient, finalOrigin.X, finalOrigin.Y,
                        fallbackResult, fallbackVerified));
                    verified = fallbackVerified;
                }
            }
        }
        return verified;
    }

    private static void RememberOriginalState(
        IntPtr window,
        Dictionary<IntPtr, OriginalWindowState> originalStates)
    {
        if (window == IntPtr.Zero || originalStates.ContainsKey(window))
            return;

        RECT bounds;
        if (!GetWindowRect(window, out bounds))
            return;

        originalStates[window] = new OriginalWindowState
        {
            Style = GetWindowLongPtr(window, GWL_STYLE).ToInt64(),
            ExStyle = GetWindowLongPtr(window, GWL_EXSTYLE).ToInt64(),
            Bounds = bounds,
            WasMaximized = IsZoomed(window)
        };
        Log(string.Format(
            "remember hwnd=0x{0:X} bounds={1} style=0x{2:X} ex=0x{3:X} maximized={4}",
            window.ToInt64(), bounds, originalStates[window].Style,
            originalStates[window].ExStyle, originalStates[window].WasMaximized));
    }

    private static void RestoreOriginalWindows(
        Dictionary<IntPtr, OriginalWindowState> originalStates)
    {
        foreach (KeyValuePair<IntPtr, OriginalWindowState> item in originalStates)
        {
            IntPtr window = item.Key;
            OriginalWindowState state = item.Value;
            if (!IsWindow(window))
                continue;

            ShowWindow(window, SW_RESTORE);
            SetWindowLongPtr(window, GWL_STYLE, new IntPtr(state.Style));
            SetWindowLongPtr(window, GWL_EXSTYLE, new IntPtr(state.ExStyle));
            int width = state.Bounds.Right - state.Bounds.Left;
            int height = state.Bounds.Bottom - state.Bounds.Top;
            bool restored = SetWindowPos(
                window,
                HWND_NOTOPMOST,
                state.Bounds.Left,
                state.Bounds.Top,
                width,
                height,
                SWP_NOACTIVATE | SWP_NOOWNERZORDER | SWP_FRAMECHANGED | SWP_SHOWWINDOW);
            if (state.WasMaximized)
                ShowWindow(window, SW_MAXIMIZE);
            Log(string.Format(
                "restore hwnd=0x{0:X} bounds={1} result={2} maximized={3}",
                window.ToInt64(), state.Bounds, restored, state.WasMaximized));
        }
    }

    private static void UpdateTaskbarOverlap(
        IntPtr window,
        IntPtr activeGame,
        HashSet<IntPtr> topmostWindows)
    {
        bool shouldBeTopmost = activeGame == window;
        bool isTopmost = topmostWindows.Contains(window);

        if (shouldBeTopmost && !isTopmost)
        {
            if (SetWindowPos(window, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE))
            {
                topmostWindows.Add(window);
                Log(string.Format("topmost on hwnd=0x{0:X}", window.ToInt64()));
                RaisePwHelperOverlay();
            }
        }
        else if (!shouldBeTopmost && isTopmost)
        {
            SetWindowPos(window, HWND_NOTOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);
            topmostWindows.Remove(window);
            Log(string.Format("topmost off hwnd=0x{0:X}", window.ToInt64()));
        }
    }

    public static void Main()
    {
        bool createdNew;
        using (var instance = new Mutex(true, @"Local\PWBorderlessMonitor", out createdNew))
        {
            if (!createdNew)
                return;

            try { SetProcessDpiAwarenessContext(new IntPtr(-4)); } catch { }
            Log("monitor started");

            var firstSeenWindows = new Dictionary<IntPtr, DateTime>();
            var configuredWindows = new HashSet<IntPtr>();
            var topmostWindows = new HashSet<IntPtr>();
            var originalStates = new Dictionary<IntPtr, OriginalWindowState>();
            using (var disableEvent = new EventWaitHandle(
                false,
                EventResetMode.AutoReset,
                @"Local\PWBorderlessDisableEvent"))
            {
            IntPtr lastForegroundGame = IntPtr.Zero;
            IntPtr pendingFocusRepair = IntPtr.Zero;
            DateTime pendingFocusRepairAt = DateTime.MinValue;
            bool helperWasForeground = false;
            DateTime helperNeutralUntil = DateTime.MinValue;
            IntPtr deferredGameAfterHelper = IntPtr.Zero;
            int diagnosticLoops = 0;
            while (true)
            {
                try
                {
                    if (disableEvent.WaitOne(0))
                    {
                        RestoreOriginalWindows(originalStates);
                        Log("monitor disabled; original windows restored");
                        return;
                    }

                    List<IntPtr> windows = FindGameWindows();
                    IntPtr foregroundRoot = GetAncestor(GetForegroundWindow(), GA_ROOT);
                    bool gameIsForeground = windows.Contains(foregroundRoot);
                    bool helperIsForeground = WindowBelongsTo(
                        foregroundRoot, GetPwHelperProcessIds());
                    DateTime now = DateTime.UtcNow;

                    if (helperIsForeground)
                    {
                        helperNeutralUntil = now.AddMilliseconds(500);
                        if (!helperWasForeground)
                        {
                            Log(string.Format(
                                "PW Helper foreground; preserving active game hwnd=0x{0:X}",
                                lastForegroundGame.ToInt64()));
                            helperWasForeground = true;
                        }
                    }
                    else if (gameIsForeground && now < helperNeutralUntil)
                    {
                        if (deferredGameAfterHelper != foregroundRoot)
                        {
                            Log(string.Format(
                                "deferring transient game hwnd=0x{0:X} after PW Helper",
                                foregroundRoot.ToInt64()));
                            deferredGameAfterHelper = foregroundRoot;
                        }
                    }
                    else if (gameIsForeground)
                    {
                        helperWasForeground = false;
                        deferredGameAfterHelper = IntPtr.Zero;
                        if (foregroundRoot != lastForegroundGame)
                        {
                            lastForegroundGame = foregroundRoot;
                            pendingFocusRepair = foregroundRoot;
                            pendingFocusRepairAt = now.AddMilliseconds(50);
                            Log(string.Format(
                                "stable focus changed to hwnd=0x{0:X}; scheduling one repair",
                                foregroundRoot.ToInt64()));
                        }
                    }
                    else
                    {
                        helperWasForeground = false;
                        helperNeutralUntil = DateTime.MinValue;
                        deferredGameAfterHelper = IntPtr.Zero;
                        lastForegroundGame = IntPtr.Zero;
                        pendingFocusRepair = IntPtr.Zero;
                    }

                    IntPtr effectiveActiveGame =
                        gameIsForeground || helperIsForeground || now < helperNeutralUntil
                            ? lastForegroundGame
                            : IntPtr.Zero;

                    if (diagnosticLoops < 5 || diagnosticLoops % 40 == 0)
                        Log(string.Format("scan processes={0} windows={1}",
                            GetGameProcessIds().Count, windows.Count));
                    diagnosticLoops++;
                    foreach (IntPtr window in windows)
                    {
                        if (!firstSeenWindows.ContainsKey(window))
                        {
                            RememberOriginalState(window, originalStates);
                            firstSeenWindows[window] = DateTime.UtcNow;
                            Log(string.Format("detected hwnd=0x{0:X}; waiting for stability",
                                window.ToInt64()));
                        }

                        if (window == pendingFocusRepair &&
                            DateTime.UtcNow >= pendingFocusRepairAt)
                        {
                            bool focusVerified;
                            if (IsClientAreaFullscreen(window))
                            {
                                focusVerified = true;
                                Log(string.Format(
                                    "focus repair skipped hwnd=0x{0:X}; client already fullscreen",
                                    window.ToInt64()));
                            }
                            else
                            {
                                focusVerified = ApplyBorderless(window);
                            }
                            configuredWindows.Add(window);
                            topmostWindows.Remove(window);
                            pendingFocusRepair = IntPtr.Zero;
                            Log(string.Format(
                                "focus repair hwnd=0x{0:X} verified={1}; applied once",
                                window.ToInt64(), focusVerified));
                        }

                        if (!configuredWindows.Contains(window) &&
                            DateTime.UtcNow - firstSeenWindows[window] >= TimeSpan.FromSeconds(2))
                        {
                            bool verified = ApplyBorderless(window);
                            configuredWindows.Add(window);
                            Log(string.Format(
                                "finalized hwnd=0x{0:X} verified={1}; no more geometry retries",
                                window.ToInt64(), verified));
                        }

                        if (configuredWindows.Contains(window))
                            UpdateTaskbarOverlap(
                                window, effectiveActiveGame, topmostWindows);
                    }

                    var staleWindows = new List<IntPtr>();
                    foreach (IntPtr window in firstSeenWindows.Keys)
                        if (!IsWindow(window))
                            staleWindows.Add(window);
                    foreach (IntPtr window in staleWindows)
                    {
                        firstSeenWindows.Remove(window);
                        originalStates.Remove(window);
                    }
                    configuredWindows.RemoveWhere(window => !IsWindow(window));
                    topmostWindows.RemoveWhere(window => !IsWindow(window));
                    Thread.Sleep(windows.Count == 0 ? 1000 : 50);
                }
                catch (Exception error)
                {
                    Log("error " + error.GetType().Name + ": " + error.Message);
                    Thread.Sleep(1000);
                }
            }
            }
        }
    }
}
