using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace PWQuestOverlay.Memory
{
    internal sealed class ProcessMemoryReader : IDisposable
    {
        private const uint ProcessVmRead = 0x0010;
        private const uint ProcessQueryLimitedInformation = 0x1000;

        private IntPtr _handle;

        private ProcessMemoryReader(
            int processId,
            IntPtr handle,
            IntPtr moduleBase,
            int moduleSize)
        {
            ProcessId = processId;
            _handle = handle;
            ModuleBase = moduleBase;
            ModuleSize = moduleSize;
        }

        internal int ProcessId { get; private set; }
        internal IntPtr ModuleBase { get; private set; }
        internal int ModuleSize { get; private set; }

        internal static ProcessMemoryReader Open(Process process, string moduleName)
        {
            if (process == null) throw new ArgumentNullException("process");
            IntPtr handle = OpenProcess(
                ProcessVmRead | ProcessQueryLimitedInformation,
                false,
                process.Id);
            if (handle == IntPtr.Zero)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Não foi possível abrir o processo somente para leitura.");

            try
            {
                IntPtr moduleBase = IntPtr.Zero;
                int moduleSize = 0;
                foreach (ProcessModule module in process.Modules)
                {
                    if (string.Equals(module.ModuleName, moduleName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        moduleBase = module.BaseAddress;
                        moduleSize = module.ModuleMemorySize;
                        break;
                    }
                }
                if (moduleBase == IntPtr.Zero)
                    throw new InvalidOperationException(
                        "Módulo não encontrado no processo: " + moduleName);
                return new ProcessMemoryReader(
                    process.Id, handle, moduleBase, moduleSize);
            }
            catch
            {
                CloseHandle(handle);
                throw;
            }
        }

        internal byte[] ReadBytes(long address, int count)
        {
            if (_handle == IntPtr.Zero)
                throw new ObjectDisposedException("ProcessMemoryReader");
            if (count <= 0) throw new ArgumentOutOfRangeException("count");

            var buffer = new byte[count];
            IntPtr bytesRead;
            bool success = ReadProcessMemory(
                _handle,
                new IntPtr(address),
                buffer,
                count,
                out bytesRead);
            if (!success || bytesRead.ToInt64() != count)
                throw new Win32Exception(Marshal.GetLastWin32Error(),
                    "Falha ao ler 0x" + address.ToString("X") + ".");
            return buffer;
        }

        internal bool TryReadBytes(long address, byte[] buffer, int count)
        {
            if (_handle == IntPtr.Zero || buffer == null || count <= 0 ||
                count > buffer.Length) return false;
            IntPtr bytesRead;
            bool success = ReadProcessMemory(
                _handle,
                new IntPtr(address),
                buffer,
                count,
                out bytesRead);
            return success && bytesRead.ToInt64() == count;
        }

        internal int ReadInt32(long address)
        {
            return BitConverter.ToInt32(ReadBytes(address, 4), 0);
        }

        internal ushort ReadUInt16(long address)
        {
            return BitConverter.ToUInt16(ReadBytes(address, 2), 0);
        }

        internal byte ReadByte(long address)
        {
            return ReadBytes(address, 1)[0];
        }

        internal uint ReadUInt32(long address)
        {
            return BitConverter.ToUInt32(ReadBytes(address, 4), 0);
        }

        internal bool TryReadUInt32(long address, out uint value)
        {
            var buffer = new byte[4];
            if (!TryReadBytes(address, buffer, buffer.Length))
            {
                value = 0;
                return false;
            }
            value = BitConverter.ToUInt32(buffer, 0);
            return true;
        }

        internal long ResolvePointerChain(uint baseRva, uint[] chain)
        {
            long address = ModuleBase.ToInt64() + baseRva;
            long pointer = ReadUInt32(address);
            if (pointer == 0) throw new InvalidOperationException(
                "Ponteiro raiz do personagem ainda não está disponível.");

            if (chain != null)
            {
                foreach (uint offset in chain)
                {
                    pointer = ReadUInt32(pointer + offset);
                    if (pointer == 0) throw new InvalidOperationException(
                        "A cadeia de ponteiros terminou em NULL.");
                }
            }
            return pointer;
        }

        internal long FindObjectByVTable(
            uint vtable,
            Func<long, bool> validator)
        {
            const uint memCommit = 0x1000;
            const uint memPrivate = 0x20000;
            const uint pageNoAccess = 0x01;
            const uint pageGuard = 0x100;
            const long maximumAddress = 0x7FFF0000L;
            const int chunkSize = 1024 * 1024;

            byte[] needle = BitConverter.GetBytes(vtable);
            long address = 0x10000;
            while (address < maximumAddress)
            {
                MemoryBasicInformation info;
                IntPtr queried = VirtualQueryEx(
                    _handle,
                    new IntPtr(address),
                    out info,
                    new UIntPtr((uint)Marshal.SizeOf(typeof(MemoryBasicInformation))));
                if (queried == IntPtr.Zero) break;

                long regionBase = info.BaseAddress.ToInt64();
                long regionSize = checked((long)info.RegionSize.ToUInt64());
                long next = regionBase + Math.Max(regionSize, 0x1000);
                bool readable = info.State == memCommit && info.Type == memPrivate &&
                    (info.Protect & pageNoAccess) == 0 &&
                    (info.Protect & pageGuard) == 0;
                if (readable && regionSize > 0)
                {
                    for (long offset = 0; offset < regionSize; offset += chunkSize)
                    {
                        int count = (int)Math.Min(chunkSize, regionSize - offset);
                        var buffer = new byte[count];
                        long chunkAddress = regionBase + offset;
                        if (!TryReadBytes(chunkAddress, buffer, count)) continue;
                        for (int index = 0; index <= count - 4; index += 4)
                        {
                            if (buffer[index] != needle[0] ||
                                buffer[index + 1] != needle[1] ||
                                buffer[index + 2] != needle[2] ||
                                buffer[index + 3] != needle[3]) continue;
                            long candidate = chunkAddress + index;
                            try
                            {
                                if (validator == null || validator(candidate))
                                    return candidate;
                            }
                            catch { }
                        }
                    }
                }
                if (next <= address) break;
                address = next;
            }
            return 0;
        }

        public void Dispose()
        {
            if (_handle == IntPtr.Zero) return;
            CloseHandle(_handle);
            _handle = IntPtr.Zero;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(
            uint desiredAccess,
            bool inheritHandle,
            int processId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool ReadProcessMemory(
            IntPtr process,
            IntPtr baseAddress,
            [Out] byte[] buffer,
            int size,
            out IntPtr numberOfBytesRead);

        [StructLayout(LayoutKind.Sequential)]
        private struct MemoryBasicInformation
        {
            internal IntPtr BaseAddress;
            internal IntPtr AllocationBase;
            internal uint AllocationProtect;
            internal UIntPtr RegionSize;
            internal uint State;
            internal uint Protect;
            internal uint Type;
        }

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr VirtualQueryEx(
            IntPtr process,
            IntPtr address,
            out MemoryBasicInformation buffer,
            UIntPtr length);

        [DllImport("kernel32.dll")]
        private static extern bool CloseHandle(IntPtr handle);
    }
}
