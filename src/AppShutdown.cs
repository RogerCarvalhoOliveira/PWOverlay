using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace PWOverlayShared
{
    internal static class AppShutdown
    {
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
            try { using (var handle = EventWaitHandle.OpenExisting(EventName(tool, directory))) { handle.Set(); return true; } }
            catch (WaitHandleCannotBeOpenedException) { return false; }
        }
    }
}
