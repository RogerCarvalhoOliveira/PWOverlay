using System;
using System.IO;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Text;
using System.Threading;

namespace PWOverlayShared
{
    internal static class AppShutdown
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool QueryFullProcessImageName(IntPtr handle, int flags, StringBuilder path, ref int size);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);

        internal static string ProcessImagePath(int pid)
        {
            IntPtr handle = OpenProcess(0x1000, false, pid);
            if (handle == IntPtr.Zero) return null;
            try
            {
                var path = new StringBuilder(32768);
                int size = path.Capacity;
                return QueryFullProcessImageName(handle, 0, path, ref size) ? Path.GetFullPath(path.ToString()) : null;
            }
            finally { CloseHandle(handle); }
        }

        internal static string DirectoryKey(string directory)
        {
            string full = Path.GetFullPath(directory);
            if (full.Length > Path.GetPathRoot(full).Length) full = full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return full.ToUpperInvariant();
        }

        internal static string EventName(string tool, string directory)
        {
            using (var hash = SHA256.Create())
                return @"Local\PWOverlay.Stop." + tool + "." + BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(DirectoryKey(directory)))).Replace("-", "").Substring(0, 24);
        }

        internal static bool Signal(string tool, string directory)
        {
            try { using (var handle = EventWaitHandle.OpenExisting(EventName(tool, directory), EventWaitHandleRights.Modify)) { handle.Set(); return true; } }
            catch (WaitHandleCannotBeOpenedException) { return false; }
        }
    }
}
