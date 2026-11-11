using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace PWOverlayShared
{
    internal static class ToolMaintenance
    {
        internal static readonly string[] Executables = { "PW Quest Overlay.exe", "PW Borderless.exe", "PW Overlay Central.exe", "PW Memory Probe.exe", "PW Maintenance.exe", "PW Identity Reader.exe" };
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle, uint milliseconds);

        private static bool HasExited(int pid)
        {
            IntPtr handle = OpenProcess(0x100000, false, pid);
            if (handle == IntPtr.Zero) return ImagePath(pid) == null;
            try { return WaitForSingleObject(handle, 0) == 0; }
            finally { CloseHandle(handle); }
        }

        internal static string ImagePath(int pid)
        {
            return AppShutdown.ProcessImagePath(pid);
        }

        internal static bool BelongsToDirectory(string path, string directory)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Executables.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase) &&
                string.Equals(AppShutdown.DirectoryKey(Path.GetDirectoryName(path)), AppShutdown.DirectoryKey(directory), StringComparison.Ordinal);
        }

        internal static bool Stop(string directory, bool includeCentral, Action<string> log)
        {
            return Stop(directory, Executables.Where(file => includeCentral || file != "PW Overlay Central.exe"), log, verifyFiles: true);
        }

        internal static bool Stop(string directory, IEnumerable<string> executables, Action<string> log, bool verifyFiles = false)
        {
            string root = AppShutdown.DirectoryKey(directory);
            string[] files = executables.ToArray();
            int ownPid;
            using (Process current = Process.GetCurrentProcess()) ownPid = current.Id;
            var targets = new List<Process>();
            try
            {
                foreach (string file in files)
                {
                    foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(file)))
                    {
                        string path = ImagePath(process.Id);
                        if (process.Id == ownPid || !BelongsToDirectory(path, root)) { process.Dispose(); continue; }
                        if (HasExited(process.Id)) { process.Dispose(); continue; }
                        targets.Add(process); log("Solicitando saída: " + file + " PID " + process.Id);
                        try
                        {
                            if (file == "PW Borderless.exe")
                            {
                                if (!AppShutdown.Signal("borderless", root))
                                    try { using (var legacy = EventWaitHandle.OpenExisting(@"Local\PWBorderlessDisableEvent")) legacy.Set(); }
                                    catch (WaitHandleCannotBeOpenedException) { }
                            }
                            else if (file == "PW Quest Overlay.exe")
                                AppShutdown.Signal("overlay", root); // Legacy WM_CLOSE hides the overlay instead of exiting.
                            else if (file == "PW Identity Reader.exe")
                                AppShutdown.Signal("identity", root);
                            else if (file == "PW Overlay Central.exe")
                            { if (!AppShutdown.Signal("central", root)) process.CloseMainWindow(); }
                        }
                        catch (Exception error) { log("Saída cooperativa indisponível: " + error.Message); }
                    }
                }
                var watch = Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < 3500 && targets.Any(p => !HasExited(p.Id))) Thread.Sleep(50);
                foreach (Process process in targets.Where(p => !HasExited(p.Id)))
                {
                    // Fallback is limited to the exact installation directory, never the game or Helper.
                    if (!BelongsToDirectory(ImagePath(process.Id), root)) { log("Processo mudou; encerramento recusado."); return false; }
                    log("Encerrando versão antiga/sem resposta: PID " + process.Id);
                    try { process.Kill(); if (!process.WaitForExit(2500)) return false; }
                    catch (Exception error) { log(error.Message); return false; }
                }
                if (!verifyFiles) return true;
                foreach (string name in files)
                {
                    string path = Path.Combine(directory, name);
                    if (!File.Exists(path)) continue;
                    try { using (var file = File.Open(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)) { } }
                    catch (Exception error) { log("Arquivo ainda indisponível: " + name + ": " + error.Message); return false; }
                }
                return true;
            }
            finally { foreach (Process process in targets) process.Dispose(); }
        }
    }
}
