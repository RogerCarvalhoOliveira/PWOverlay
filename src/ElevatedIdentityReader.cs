using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using PWOverlayShared;
using PWQuestOverlay.Memory;

namespace PWQuestOverlay
{
    internal sealed class ElevatedIdentityReader : IDisposable
    {
        internal const string ProfileId = "the_classic_126_identity";
        internal const string WorkerArgument = "--identity-worker";
        internal const string WorkerFile = "PW Identity Reader.exe";
        private readonly object _gate = new object();
        private readonly object _writeGate = new object();
        private NamedPipeServerStream _pipe;
        private StreamWriter _requests;
        private bool _listening, _disposed, _readActive;
        private Task _starting;
        private TaskCompletionSource<bool> _attached = NewSignal<bool>();
        private TaskCompletionSource<HistoryReply> _pendingHistory;
        private string _requestId;
        private Dictionary<string, Sample> _samples = new Dictionary<string, Sample>();
        private DateTime _lastRead;

        public sealed class Sample
        {
            public string SessionKey { get; set; }
            public string SessionToken { get; set; }
            public string Name { get; set; }
            public string ClassCode { get; set; }
            public uint[] Active { get; set; }
        }

        public sealed class Request
        {
            public string Id { get; set; }
            public string SessionKey { get; set; }
            public string Name { get; set; }
            public string ClassCode { get; set; }
            public bool? ReadActive { get; set; }
        }

        public sealed class HistoryReply
        {
            public string Id { get; set; }
            public uint[] Active { get; set; }
            public uint[] Finished { get; set; }
            public int Level { get; set; }
            public int? Reputation { get; set; }
            public string Error { get; set; }
        }

        public sealed class Update
        {
            public List<Sample> Samples { get; set; }
            public HistoryReply History { get; set; }
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetNamedPipeClientProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out uint processId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetNamedPipeServerProcessId(Microsoft.Win32.SafeHandles.SafePipeHandle pipe, out uint processId);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, int pid);
        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr process, uint access, out IntPtr token);

        private static TaskCompletionSource<T> NewSignal<T>()
        {
            return new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
        }

        private static string PipeName(string directory, SecurityIdentifier user)
        {
            using (Process current = Process.GetCurrentProcess())
                return AppShutdown.EventName("identity", directory).Substring(@"Local\".Length) + "." + current.SessionId + "." + user.Value;
        }

        internal bool IsRunning
        {
            get { lock (_gate) return _requests != null; }
        }

        internal Sample Find(string sessionKey)
        {
            lock (_gate)
            {
                Sample sample;
                return DateTime.UtcNow - _lastRead <= TimeSpan.FromSeconds(2) &&
                    _samples.TryGetValue(sessionKey, out sample) ? sample : null;
            }
        }

        internal void Listen(string directory, Action<string> log)
        {
            lock (_gate)
            {
                if (_listening || _disposed) return;
                _listening = true;
            }
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                SecurityIdentifier user = identity.User;
                Task.Run(() => Receive(directory, user, log));
            }
        }

        internal Task Start(string executable, Action<string> log)
        {
            string directory = Path.GetDirectoryName(executable);
            Listen(directory, log);
            lock (_gate)
            {
                if (_requests != null) return Task.FromResult(true);
                if (_starting == null || _starting.IsCompleted)
                    _starting = LaunchWorker(directory, _attached.Task);
                return _starting;
            }
        }

        private async Task LaunchWorker(string directory, Task<bool> connected)
        {
            if (await Task.WhenAny(connected, Task.Delay(700)) == connected && await connected) return;
            await Task.Run(delegate
            {
                using (WindowsIdentity user = WindowsIdentity.GetCurrent())
                using (Process process = Process.Start(new ProcessStartInfo(
                    Path.Combine(directory, WorkerFile), WorkerArgument + " " + user.User.Value)
                {
                    WorkingDirectory = directory, UseShellExecute = true, Verb = "runas", WindowStyle = ProcessWindowStyle.Hidden
                }))
                {
                    if (process == null) throw new IOException("Não foi possível iniciar o leitor do PW.");
                }
            });
            if (await Task.WhenAny(connected, Task.Delay(12000)) != connected || !await connected)
                throw new IOException("O leitor elevado não respondeu.");
        }

        internal void SetAutoAdvance(bool enabled)
        {
            lock (_gate) _readActive = enabled;
            Task.Run(delegate
            {
                try { SendActiveSetting(); }
                catch (IOException) { }
                catch (ObjectDisposedException) { }
            });
        }

        private void SendActiveSetting()
        {
            lock (_writeGate)
            {
                StreamWriter writer;
                bool enabled;
                lock (_gate) { writer = _requests; enabled = _readActive; }
                if (writer != null) writer.WriteLine(new JavaScriptSerializer().Serialize(new Request { ReadActive = enabled }));
            }
        }

        internal async Task<QuestMemorySnapshot> ReadHistory(ClientWindowInfo window, string name, string classCode)
        {
            var request = new Request
            {
                Id = Guid.NewGuid().ToString("N"), SessionKey = window.SessionKey, Name = name, ClassCode = classCode
            };
            var result = NewSignal<HistoryReply>();
            StreamWriter writer;
            lock (_gate)
            {
                if (_requests == null || _pendingHistory != null)
                    throw new IOException("O leitor elevado ainda não está pronto para importar.");
                writer = _requests;
                _requestId = request.Id;
                _pendingHistory = result;
            }
            try
            {
                await Task.Run(delegate
                {
                    lock (_writeGate) writer.WriteLine(new JavaScriptSerializer().Serialize(request));
                });
                if (await Task.WhenAny(result.Task, Task.Delay(8000)) != result.Task)
                    throw new IOException("O leitor elevado não respondeu à consulta do histórico.");
                HistoryReply reply = await result.Task;
                if (reply == null) throw new IOException("O leitor elevado foi encerrado.");
                if (!string.IsNullOrEmpty(reply.Error)) throw new IOException(reply.Error);
                var snapshot = new QuestMemorySnapshot { PlayerLevel = reply.Level, PlayerReputation = reply.Reputation };
                snapshot.FinishedTaskIds.AddRange(reply.Finished);
                snapshot.ActiveTasks.AddRange(reply.Active.Select(id => new ActiveQuestMemoryEntry { TaskId = id }));
                return snapshot;
            }
            finally
            {
                lock (_gate)
                    if (_pendingHistory == result) { _pendingHistory = null; _requestId = null; }
            }
        }

        private void Receive(string directory, SecurityIdentifier user, Action<string> log)
        {
            var security = new PipeSecurity();
            security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
                PipeAccessRights.ReadWrite | PipeAccessRights.ReadPermissions | PipeAccessRights.Synchronize, AccessControlType.Allow));
            string name = PipeName(directory, user);
            while (true)
            {
                NamedPipeServerStream pipe = null;
                try
                {
                    lock (_gate)
                    {
                        if (_disposed) return;
                        _pipe = pipe = new NamedPipeServerStream(name, PipeDirection.InOut, 1, PipeTransmissionMode.Byte,
                            PipeOptions.Asynchronous, 0, 0, security);
                    }
                    pipe.WaitForConnection();
                    uint peer;
                    if (!GetNamedPipeClientProcessId(pipe.SafePipeHandle, out peer) ||
                        !string.Equals(AppShutdown.ProcessImagePath((int)peer), Path.Combine(directory, WorkerFile), StringComparison.OrdinalIgnoreCase))
                        continue;
                    ReceiveSamples(pipe, log, peer);
                }
                catch (ObjectDisposedException) { }
                catch (Exception error)
                {
                    lock (_gate) if (_disposed) return;
                    log("Leitor elevado 1.2.6: " + error.Message);
                    Thread.Sleep(250);
                }
                finally
                {
                    if (pipe != null) pipe.Dispose();
                    lock (_gate)
                    {
                        _pipe = null;
                        _requests = null;
                        _samples.Clear();
                        if (_pendingHistory != null) _pendingHistory.TrySetResult(null);
                        if (_attached.Task.IsCompleted && !_disposed) _attached = NewSignal<bool>();
                    }
                }
            }
        }

        private void ReceiveSamples(NamedPipeServerStream pipe, Action<string> log, uint peer)
        {
            var serializer = new JavaScriptSerializer();
            using (var reader = new StreamReader(pipe, Encoding.UTF8))
            using (var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true })
            {
                lock (_gate)
                {
                    if (_disposed) return;
                    _requests = writer;
                }
                SendActiveSetting();
                lock (_gate) _attached.TrySetResult(true);
                log("Leitor elevado 1.2.6 conectado. PID " + peer + ".");
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    Update update = serializer.Deserialize<Update>(line);
                    TaskCompletionSource<HistoryReply> history = null;
                    lock (_gate)
                    {
                        if (_disposed) return;
                        _samples = update.Samples.ToDictionary(sample => sample.SessionKey);
                        _lastRead = DateTime.UtcNow;
                        if (update.History != null && update.History.Id == _requestId) history = _pendingHistory;
                    }
                    if (history != null) history.TrySetResult(update.History);
                }
            }
        }

        internal static void Serve(string sid)
        {
            SecurityIdentifier user;
            try { user = new SecurityIdentifier(sid); }
            catch (ArgumentException) { return; }

            string directory = AppDomain.CurrentDomain.BaseDirectory;
            if (!string.Equals(Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                WorkerFile, StringComparison.OrdinalIgnoreCase)) return;
            using (Stream resource = typeof(ElevatedIdentityReader).Assembly.GetManifestResourceStream("PWOverlay.MemoryLayouts.json"))
            {
                if (resource == null) return;
                using (var reader = new StreamReader(resource))
                {
                    MemoryLayoutProfile profile = MemoryLayoutFile.Parse(reader.ReadToEnd()).Find(ProfileId);
                    if (profile == null || !profile.identityOnly || profile.quests == null) return;
                    ListenAsWorker(directory, user, profile);
                }
            }
        }

        private static void ListenAsWorker(string directory, SecurityIdentifier user, MemoryLayoutProfile profile)
        {
            var security = new EventWaitHandleSecurity();
            using (WindowsIdentity self = WindowsIdentity.GetCurrent())
                security.AddAccessRule(new EventWaitHandleAccessRule(self.User, EventWaitHandleRights.FullControl, AccessControlType.Allow));
            security.AddAccessRule(new EventWaitHandleAccessRule(user,
                EventWaitHandleRights.Modify | EventWaitHandleRights.Synchronize, AccessControlType.Allow));
            bool created;
            using (var stop = new EventWaitHandle(false, EventResetMode.ManualReset,
                AppShutdown.EventName("identity", directory), out created, security))
            {
                if (!created) return;
                string name = PipeName(directory, user);
                while (!stop.WaitOne(0))
                {
                    try
                    {
                        using (var pipe = new NamedPipeClientStream(".", name, PipeDirection.InOut,
                            PipeOptions.Asynchronous, TokenImpersonationLevel.Identification))
                        {
                            pipe.Connect(500);
                            uint peer;
                            if (!GetNamedPipeServerProcessId(pipe.SafePipeHandle, out peer) ||
                                !string.Equals(AppShutdown.ProcessImagePath((int)peer), Path.Combine(directory, "PW Quest Overlay.exe"), StringComparison.OrdinalIgnoreCase) ||
                                !user.Equals(ProcessOwner((int)peer)))
                                continue;
                            Publish(pipe, stop, profile);
                        }
                    }
                    catch (IOException) { }
                    catch (TimeoutException) { }
                    catch (UnauthorizedAccessException) { }
                    if (stop.WaitOne(200)) return;
                }
            }
        }

        private static SecurityIdentifier ProcessOwner(int pid)
        {
            IntPtr process = OpenProcess(0x1000, false, pid);
            if (process == IntPtr.Zero) return null;
            try
            {
                IntPtr token;
                if (!OpenProcessToken(process, 8, out token)) return null;
                try { using (var identity = new WindowsIdentity(token)) return identity.User; }
                finally { CloseHandle(token); }
            }
            finally { CloseHandle(process); }
        }

        private static void Publish(NamedPipeClientStream pipe, EventWaitHandle stop, MemoryLayoutProfile profile)
        {
            var serializer = new JavaScriptSerializer();
            using (var monitor = new QuestMemoryMonitor(profile, null))
            using (var reader = new StreamReader(pipe, Encoding.UTF8))
            using (var writer = new StreamWriter(pipe, new UTF8Encoding(false), 1024, true) { AutoFlush = true })
            {
                bool readActive = false;
                Task<string> pending = reader.ReadLineAsync();
                while (pipe.IsConnected && !stop.WaitOne(0))
                {
                    var windows = NativeMethods.FindGameWindows().Select(NativeMethods.DescribeGameWindow)
                        .Where(window => window != null && window.Server != null && window.Server.IdentityMemoryOnly).ToList();
                    HistoryReply history = null;
                    if (pending.IsCompleted)
                    {
                        string line = pending.GetAwaiter().GetResult();
                        if (line == null) break;
                        Request request = serializer.Deserialize<Request>(line);
                        if (request.ReadActive.HasValue) readActive = request.ReadActive.Value;
                        else history = ReadHistoryRequest(request, windows, profile);
                        pending = reader.ReadLineAsync();
                    }
                    monitor.Poll(windows.Select(window => window.ProcessId), readQuests: readActive);
                    var samples = new List<Sample>();
                    foreach (ClientWindowInfo window in windows)
                    {
                        QuestMemoryClientState state = monitor.Find(window.ProcessId);
                        PlayerIdentityMemory player = state == null ? null : state.Identity;
                        samples.Add(new Sample
                        {
                            SessionKey = window.SessionKey, SessionToken = state == null ? null : state.SessionToken,
                            Name = player == null ? null : player.Name, ClassCode = player == null ? null : player.ClassCode,
                            Active = readActive && state != null && state.Ready && state.Snapshot != null
                                ? state.Snapshot.ActiveTasks.Select(task => task.TaskId).ToArray() : null
                        });
                    }
                    writer.WriteLine(serializer.Serialize(new Update { Samples = samples, History = history }));
                    if (stop.WaitOne(250)) return;
                }
            }
        }

        private static HistoryReply ReadHistoryRequest(Request request, List<ClientWindowInfo> windows, MemoryLayoutProfile profile)
        {
            var reply = new HistoryReply { Id = request.Id };
            try
            {
                ClientWindowInfo window = windows.Find(item => item.SessionKey == request.SessionKey);
                if (window == null) throw new IOException("O cliente selecionado mudou.");
                QuestMemorySnapshot snapshot = QuestMemoryMonitor.ReadHistory(window.ProcessId, profile, request.Name, request.ClassCode);
                reply.Finished = snapshot.FinishedTaskIds.ToArray();
                reply.Active = snapshot.ActiveTasks.Select(task => task.TaskId).ToArray();
                reply.Level = snapshot.PlayerLevel;
                reply.Reputation = snapshot.PlayerReputation;
            }
            catch (Exception error) { reply.Error = error.Message; }
            return reply;
        }

        public void Dispose()
        {
            NamedPipeServerStream pipe;
            lock (_gate)
            {
                _disposed = true;
                pipe = _pipe;
                _pipe = null;
                _requests = null;
                _attached.TrySetResult(false);
                if (_pendingHistory != null) _pendingHistory.TrySetResult(null);
                _samples.Clear();
            }
            if (pipe != null) pipe.Dispose();
        }
    }
}
