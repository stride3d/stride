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
                return File.ReadAllBytes(path);
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

    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, SafeFileHandle hFile, int dumpType,
        IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);
}
