// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Stride.Editor.CrashReport;

/// <summary>
/// Writes a minidump of the current process: thread stacks and module list, not full memory.
/// </summary>
public static class MinidumpWriter
{
    private const int MiniDumpNormal = 0x0;
    private const int MiniDumpWithUnloadedModules = 0x20;
    private const int MiniDumpWithThreadInfo = 0x1000;

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
                            MiniDumpNormal | MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo,
                            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero))
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

    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeFileHandle hFile, int dumpType,
        IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);
}
