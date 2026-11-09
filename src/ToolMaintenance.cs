using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace PWOverlayShared
{
    internal static class ToolMaintenance
    {
        internal static readonly string[] Executables = { "PW Quest Overlay.exe", "PW Borderless.exe", "PW Overlay Central.exe", "PW Memory Probe.exe", "PW Maintenance.exe" };
        [DllImport("kernel32.dll", SetLastError = true)] static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool QueryFullProcessImageName(IntPtr handle, int flags, StringBuilder path, ref int size);
        [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);

        internal static string ImagePath(int pid)
        {
            IntPtr handle = OpenProcess(0x1000, false, pid);
            if (handle == IntPtr.Zero) return null;
            try { var path = new StringBuilder(32768); int size = path.Capacity; return QueryFullProcessImageName(handle, 0, path, ref size) ? Path.GetFullPath(path.ToString()) : null; }
            finally { CloseHandle(handle); }
        }

        internal static bool BelongsToDirectory(string path, string directory)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return Executables.Contains(Path.GetFileName(path), StringComparer.OrdinalIgnoreCase) &&
                string.Equals(AppShutdown.DirectoryKey(Path.GetDirectoryName(path)), AppShutdown.DirectoryKey(directory), StringComparison.Ordinal);
        }

        internal static bool Stop(string directory, bool includeCentral, Action<string> log)
        {
            string root = AppShutdown.DirectoryKey(directory);
            var targets = new List<Process>();
            try
            {
                foreach (string file in Executables)
                {
                    if (!includeCentral && file == "PW Overlay Central.exe") continue;
                    foreach (Process process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(file)))
                    {
                        string path = ImagePath(process.Id);
                        if (process.Id == Process.GetCurrentProcess().Id || !BelongsToDirectory(path, root)) { process.Dispose(); continue; }
                        if (process.HasExited) { process.Dispose(); continue; }
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
                            else if (file == "PW Overlay Central.exe")
                            { if (!AppShutdown.Signal("central", root)) process.CloseMainWindow(); }
                        }
                        catch (Exception error) { log("Saída cooperativa indisponível: " + error.Message); }
                    }
                }
                var watch = Stopwatch.StartNew();
                while (watch.ElapsedMilliseconds < 3500 && targets.Any(p => !p.HasExited)) Thread.Sleep(50);
                foreach (Process process in targets.Where(p => !p.HasExited))
                {
                    // Fallback is limited to the exact installation directory, never the game or Helper.
                    if (!BelongsToDirectory(ImagePath(process.Id), root)) { log("Processo mudou; encerramento recusado."); return false; }
                    log("Encerrando versão antiga/sem resposta: PID " + process.Id);
                    try { process.Kill(); if (!process.WaitForExit(2500)) return false; }
                    catch (Exception error) { log(error.Message); return false; }
                }
                foreach (string name in Executables)
                {
                    if (!includeCentral && name == "PW Overlay Central.exe") continue;
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
