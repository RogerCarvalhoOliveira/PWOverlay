using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace PWQuestOverlay
{
    internal static class NativeMethods
    {
        internal const int GA_ROOT = 2;
        internal const int GWL_EXSTYLE = -20;
        internal const long WS_EX_TOPMOST = 0x00000008L;
        internal const long WS_EX_TOOLWINDOW = 0x00000080L;
        internal const long WS_EX_NOACTIVATE = 0x08000000L;
        internal const int WM_HOTKEY = 0x0312;
        internal const int WM_MOUSEACTIVATE = 0x0021;
        internal const int MA_NOACTIVATE = 3;
        internal const uint SWP_NOSIZE = 0x0001;
        internal const uint SWP_NOMOVE = 0x0002;
        internal const uint SWP_NOACTIVATE = 0x0010;
        internal const uint SWP_NOOWNERZORDER = 0x0200;
        internal const uint SWP_SHOWWINDOW = 0x0040;
        internal const int SW_SHOWNOACTIVATE = 4;
        internal const uint MOD_ALT = 0x0001;
        internal const uint MOD_CONTROL = 0x0002;
        internal const int VK_LEFT = 0x25;
        internal const int VK_RIGHT = 0x27;
        internal const int VK_DOWN = 0x28;
        internal const byte VK_END = 0x23;
        internal const byte VK_BACK = 0x08;
        internal const uint KEYEVENTF_KEYUP = 0x0002;
        internal const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
        internal const uint MOUSEEVENTF_LEFTUP = 0x0004;

        internal static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

        internal delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

        [StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            internal int Left;
            internal int Top;
            internal int Right;
            internal int Bottom;

            internal int Width { get { return Right - Left; } }
            internal int Height { get { return Bottom - Top; } }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct POINT
        {
            internal int X;
            internal int Y;
        }

        [DllImport("user32.dll")]
        internal static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

        [DllImport("user32.dll")]
        internal static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        internal static extern int GetWindowText(IntPtr window, StringBuilder text, int maxCount);

        [DllImport("user32.dll")]
        internal static extern bool IsWindow(IntPtr window);

        [DllImport("user32.dll")]
        internal static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        internal static extern bool GetClientRect(IntPtr window, out RECT rect);

        [DllImport("user32.dll")]
        internal static extern bool GetWindowRect(IntPtr window, out RECT rect);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        internal static extern bool ClientToScreen(IntPtr window, ref POINT point);

        [DllImport("user32.dll")]
        internal static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        internal static extern bool GetCursorPos(out POINT point);

        [DllImport("user32.dll")]
        internal static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        internal static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, UIntPtr extraInfo);

        [DllImport("user32.dll")]
        internal static extern IntPtr GetAncestor(IntPtr window, int flags);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW", SetLastError = true)]
        internal static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool SetWindowPos(
            IntPtr window,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool RegisterHotKey(IntPtr window, int id, uint modifiers, int key);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern bool UnregisterHotKey(IntPtr window, int id);

        [DllImport("user32.dll")]
        internal static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        internal static string GetWindowTitle(IntPtr window)
        {
            if (window == IntPtr.Zero) return string.Empty;
            var text = new StringBuilder(512);
            GetWindowText(window, text, text.Capacity);
            return text.ToString();
        }

        internal static int GetProcessId(IntPtr window)
        {
            uint id;
            GetWindowThreadProcessId(window, out id);
            return (int)id;
        }

        internal static bool WindowBelongsToProcess(IntPtr window, string processName)
        {
            if (window == IntPtr.Zero) return false;
            try
            {
                int id = GetProcessId(window);
                using (Process process = Process.GetProcessById(id))
                    return string.Equals(process.ProcessName, processName, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        internal static bool WindowBelongsToCurrentProcess(IntPtr window)
        {
            return window != IntPtr.Zero && GetProcessId(window) == Process.GetCurrentProcess().Id;
        }

        internal static List<IntPtr> FindGameWindows()
        {
            var processIds = new HashSet<int>();
            var windows = new List<IntPtr>();
            foreach (Process game in Process.GetProcessesByName("elementclient"))
            {
                try
                {
                    processIds.Add(game.Id);
                    game.Refresh();
                    if (game.MainWindowHandle != IntPtr.Zero && IsWindow(game.MainWindowHandle))
                        windows.Add(game.MainWindowHandle);
                }
                catch { }
                finally { game.Dispose(); }
            }

            if (processIds.Count == 0) return windows;
            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                uint processId;
                GetWindowThreadProcessId(window, out processId);
                if (!processIds.Contains((int)processId) || !IsWindowVisible(window)) return true;
                RECT client;
                if (!GetClientRect(window, out client)) return true;
                if (client.Width >= 640 && client.Height >= 480 && !windows.Contains(window))
                    windows.Add(window);
                return true;
            }, IntPtr.Zero);
            return windows;
        }

        internal static bool TryFindPwHelperOverlay(out RECT rect)
        {
            IntPtr window;
            return TryFindPwHelperOverlay(out window, out rect);
        }

        internal static bool TryFindPwHelperOverlay(out IntPtr overlayWindow, out RECT rect)
        {
            overlayWindow = IntPtr.Zero;
            rect = new RECT();
            RECT foundRect = new RECT();
            IntPtr foundWindow = IntPtr.Zero;
            bool found = false;
            var helperIds = new HashSet<int>();
            foreach (Process helper in Process.GetProcessesByName("PW Helper"))
            {
                try { helperIds.Add(helper.Id); }
                catch { }
                finally { helper.Dispose(); }
            }
            if (helperIds.Count == 0) return false;

            EnumWindows(delegate(IntPtr window, IntPtr parameter)
            {
                uint processId;
                GetWindowThreadProcessId(window, out processId);
                if (!helperIds.Contains((int)processId) || !IsWindowVisible(window)) return true;
                if (!string.Equals(GetWindowTitle(window), "TODOS", StringComparison.OrdinalIgnoreCase))
                    return true;
                RECT candidate;
                if (GetWindowRect(window, out candidate))
                {
                    foundRect = candidate;
                    foundWindow = window;
                    found = true;
                }
                return !found;
            }, IntPtr.Zero);
            overlayWindow = foundWindow;
            rect = foundRect;
            return found;
        }

        internal static bool IsTopmost(IntPtr window)
        {
            return (GetWindowLongPtr(window, GWL_EXSTYLE).ToInt64() & WS_EX_TOPMOST) != 0;
        }

        internal static void ClickClientPoint(IntPtr window, int referenceX, int referenceY)
        {
            RECT client;
            if (!GetClientRect(window, out client) || client.Width <= 0 || client.Height <= 0) return;
            var point = new POINT
            {
                X = (int)Math.Round(referenceX * client.Width / 1920.0),
                Y = (int)Math.Round(referenceY * client.Height / 1080.0)
            };
            if (!ClientToScreen(window, ref point)) return;
            SetCursorPos(point.X, point.Y);
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
        }

        internal static void PressKey(byte virtualKey)
        {
            keybd_event(virtualKey, 0, 0, UIntPtr.Zero);
            keybd_event(virtualKey, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }
    }
}
