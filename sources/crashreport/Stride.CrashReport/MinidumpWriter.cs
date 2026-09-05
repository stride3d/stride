// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace Stride.CrashReport;

/// <summary>
/// Writes a minidump of the current process: thread stacks and module list, not full memory. Windows-only (dbghelp).
/// </summary>
[SupportedOSPlatform("windows")]
public static class MinidumpWriter
{
    private const int MiniDumpNormal = 0x0;
    private const int MiniDumpWithFullMemory = 0x2;
    private const int MiniDumpWithHandleData = 0x4;
    private const int MiniDumpWithUnloadedModules = 0x20;
    private const int MiniDumpWithFullMemoryInfo = 0x800;
    private const int MiniDumpWithThreadInfo = 0x1000;

    // Thread stacks + module list, no process memory.
    private const int TriageFlags = MiniDumpNormal | MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo;

    public static byte[] TryWrite()
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), $"stride-crash-{Environment.ProcessId}.dmp");
            try
            {
                using (var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None))
                using (var process = Process.GetCurrentProcess())
                {
                    if (!MiniDumpWriteDump(process.Handle, (uint)Environment.ProcessId, file.SafeFileHandle,
                            TriageFlags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
                        return null;
                }
                var bytes = File.ReadAllBytes(path);
                ScrubDump(bytes);
                return bytes;
            }
            finally
            {
                File.Delete(path);
            }
        }
        catch (Exception)
        {
            return null;
        }
    }

    private const int ModuleListStream = 4;
    private const int MemoryListStream = 5;
    private const int Memory64ListStream = 9;
    private const int UnloadedModuleListStream = 14;

    /// <summary>
    /// Masks the user name and profile path where they can appear: module path strings and the captured
    /// memory ranges (thread stacks). Walks the stream directory so headers are never touched.
    /// </summary>
    private static void ScrubDump(byte[] dump)
    {
        try
        {
            if (dump.Length < 16 || BitConverter.ToUInt32(dump, 0) != 0x504D444D) // 'MDMP'
                return;

            var streamCount = BitConverter.ToInt32(dump, 8);
            var directory = BitConverter.ToInt32(dump, 12);
            for (var i = 0; i < streamCount; i++)
            {
                var streamType = BitConverter.ToInt32(dump, directory + i * 12);
                var rva = BitConverter.ToInt32(dump, directory + i * 12 + 8);
                switch (streamType)
                {
                    case ModuleListStream:
                    {
                        // MINIDUMP_MODULE is 108 bytes: name RVA at offset 20, CodeView record (PDB path) at 76
                        var count = BitConverter.ToInt32(dump, rva);
                        for (var m = 0; m < count; m++)
                        {
                            var module = rva + 4 + m * 108;
                            ScrubString(dump, BitConverter.ToInt32(dump, module + 20));
                            var cvSize = BitConverter.ToInt32(dump, module + 76);
                            var cvRva = BitConverter.ToInt32(dump, module + 80);
                            CrashReportAnonymizer.Scrub(dump, cvRva, cvSize);
                        }
                        break;
                    }
                    case UnloadedModuleListStream:
                    {
                        // Sized header, then entries with the name RVA at offset 20
                        var headerSize = BitConverter.ToInt32(dump, rva);
                        var entrySize = BitConverter.ToInt32(dump, rva + 4);
                        var count = BitConverter.ToInt32(dump, rva + 8);
                        for (var m = 0; m < count; m++)
                            ScrubString(dump, BitConverter.ToInt32(dump, rva + headerSize + m * entrySize + 20));
                        break;
                    }
                    case MemoryListStream:
                    {
                        // MINIDUMP_MEMORY_DESCRIPTOR: address (8), data size (4), data RVA (4)
                        var count = BitConverter.ToInt32(dump, rva);
                        for (var m = 0; m < count; m++)
                        {
                            var size = BitConverter.ToInt32(dump, rva + 4 + m * 16 + 8);
                            var dataRva = BitConverter.ToInt32(dump, rva + 4 + m * 16 + 12);
                            CrashReportAnonymizer.Scrub(dump, dataRva, size);
                        }
                        break;
                    }
                    case Memory64ListStream:
                    {
                        // All ranges are stored contiguously starting at the base RVA
                        var count = BitConverter.ToInt64(dump, rva);
                        var baseRva = BitConverter.ToInt64(dump, rva + 8);
                        long total = 0;
                        for (var m = 0; m < count; m++)
                            total += BitConverter.ToInt64(dump, rva + 16 + m * 16 + 8);
                        CrashReportAnonymizer.Scrub(dump, (int)baseRva, (int)Math.Min(total, dump.Length - baseRva));
                        break;
                    }
                }
            }
        }
        catch (Exception)
        {
            // Scrubbing is best-effort; never lose the dump over it
        }
    }

    /// <summary>MINIDUMP_STRING: byte length prefix, then UTF-16 characters.</summary>
    private static void ScrubString(byte[] dump, int rva)
    {
        if (rva <= 0 || rva + 4 > dump.Length)
            return;
        var byteLength = BitConverter.ToInt32(dump, rva);
        CrashReportAnonymizer.Scrub(dump, rva + 4, byteLength);
    }

    /// <summary>
    /// Writes a dump directly to a file, for local saving. A full memory dump can be several GB and is
    /// not scrubbed; it never leaves the machine unless the user shares it themselves.
    /// </summary>
    public static bool TryWriteFile(string path, bool fullMemory)
    {
        try
        {
            using var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            using var process = Process.GetCurrentProcess();
            var flags = fullMemory
                ? MiniDumpWithFullMemory | MiniDumpWithFullMemoryInfo | MiniDumpWithHandleData | MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo
                : TriageFlags;
            return MiniDumpWriteDump(process.Handle, (uint)Environment.ProcessId, file.SafeFileHandle, flags,
                IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Writes a dump of <em>another</em> process from this (healthy) one, carrying that process's crash
    /// exception record so the dump has an exception stream (the faulting thread and fault are recorded). The
    /// out-of-process reporter uses this to capture a dying host: the host's native trigger freezes the crashing
    /// thread and hands us its pid, thread id, and the address of its <c>EXCEPTION_POINTERS</c>, which dbghelp
    /// reads across the process boundary (<c>ClientPointers</c>). With <paramref name="exceptionPointers"/> zero
    /// (a managed crash dumped on demand: the host is alive and waiting, not faulting) no exception stream is
    /// written, just the process snapshot. A full-memory dump can be several GB and is not scrubbed; it never
    /// leaves the machine unless the user shares it themselves. Returns false if the target can't be opened or
    /// the dump can't be written.
    /// </summary>
    public static bool TryWriteTargetProcess(int processId, uint threadId, IntPtr exceptionPointers, string path, bool fullMemory)
    {
        const uint ProcessQueryInformation = 0x0400;
        const uint ProcessVmRead = 0x0010;

        var target = OpenProcess(ProcessQueryInformation | ProcessVmRead, false, (uint)processId);
        if (target == IntPtr.Zero)
            return false;
        try
        {
            using var file = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.None);
            var flags = fullMemory
                ? MiniDumpWithFullMemory | MiniDumpWithFullMemoryInfo | MiniDumpWithHandleData | MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo
                : TriageFlags;
            if (exceptionPointers == IntPtr.Zero)
                return MiniDumpWriteDump(target, (uint)processId, file.SafeFileHandle, flags, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            var information = new MinidumpExceptionInformation
            {
                ThreadId = threadId,
                ExceptionPointers = exceptionPointers, // an address in the target; ClientPointers reads it there
                ClientPointers = 1,
            };
            var pinned = GCHandle.Alloc(information, GCHandleType.Pinned);
            try
            {
                return MiniDumpWriteDump(target, (uint)processId, file.SafeFileHandle, flags,
                    pinned.AddrOfPinnedObject(), IntPtr.Zero, IntPtr.Zero);
            }
            finally
            {
                pinned.Free();
            }
        }
        catch (Exception)
        {
            return false;
        }
        finally
        {
            CloseHandle(target);
        }
    }

    // Pack=4 required: dbghelp.h structs are 4-byte packed; natural x64 layout makes dbghelp read a garbage
    // pointer and fail with ERROR_NOACCESS.
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    private struct MinidumpExceptionInformation
    {
        public uint ThreadId;
        public IntPtr ExceptionPointers;
        public int ClientPointers; // BOOL
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(uint desiredAccess, bool inheritHandle, uint processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr handle);

    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeFileHandle hFile, int dumpType,
        IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);
}
