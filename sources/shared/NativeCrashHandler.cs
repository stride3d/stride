// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

namespace Stride
{
    /// <summary>
    /// Native-crash diagnostics for test/sample runs: suppresses the Windows crash dialog so a crash
    /// can't hang CI, and — when STRIDE_TESTS_CRASH_DUMPS=1 — logs the SEH stack and writes a minidump
    /// from a <see cref="AppDomain.FirstChanceException"/> handler.
    /// </summary>
    /// <remarks>
    /// GPU drivers (incl. software renderers like WARP/Lavapipe) and native audio (XAudio2) can crash
    /// with access violations on teardown. .NET 8+ can't catch these via try/catch and the runtime
    /// dispatches them internally, so without this they die silently. Shared by
    /// Stride.Graphics.Regression and Stride.Games.AutoTesting; each calls <see cref="Install"/> from
    /// its own ModuleInitializer.
    /// </remarks>
    internal static class NativeCrashHandler
    {
        [DllImport("kernel32.dll")]
        private static extern uint SetErrorMode(uint uMode);

        [DllImport("dbghelp.dll", SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile,
            uint dumpType, IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

        private static string crashDumpDir;

        /// <summary>
        /// Installs the crash-dialog suppression and (when STRIDE_TESTS_CRASH_DUMPS=1) the SEH minidump
        /// handler. Idempotent enough for one call per assembly ModuleInitializer.
        /// </summary>
        public static void Install()
        {
            bool capture = Environment.GetEnvironmentVariable("STRIDE_TESTS_CRASH_DUMPS") == "1";

            if (OperatingSystem.IsWindows())
            {
                // SEM_NOGPFAULTERRORBOX hides the native-AV crash dialog but also blocks WER LocalDumps
                // and the runtime minidump for pure-native crashes. So gate it: in capture mode omit it
                // so the crash routes to WER (kept quiet by the WER DontShowUI registry); otherwise keep
                // it so a crash can't hang the runner on a dialog. Capture mode is opt-in because dropping
                // the flag relies on the caller having a hang backstop (e.g. the orchestrator's timeout).
                uint mode = 0x0001 /* SEM_FAILCRITICALERRORS */ | 0x8000 /* SEM_NOOPENFILEERRORBOX */;
                if (!capture)
                    mode |= 0x0002 /* SEM_NOGPFAULTERRORBOX */;
                SetErrorMode(mode);
            }

            if (!capture)
                return;

            // STRIDE_TESTS_CRASH_DUMP_DIR (set by CI) wins; otherwise dump next to the exe.
            crashDumpDir = Environment.GetEnvironmentVariable("STRIDE_TESTS_CRASH_DUMP_DIR")
                ?? Path.Combine(AppContext.BaseDirectory, "crash-dumps");
            try { Directory.CreateDirectory(crashDumpDir); } catch { /* best effort */ }

            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is not SEHException seh)
                return;

            Console.Error.WriteLine($"[CrashDiag] FirstChanceException: SEHException HResult=0x{seh.HResult:X8}");
            Console.Error.WriteLine($"[CrashDiag] Exception stack: {seh.StackTrace}");
            foreach (var line in Environment.StackTrace.Split('\n'))
                Console.Error.WriteLine($"[CrashDiag]   {line.TrimEnd()}");

            WriteMiniDump("firstchance_seh");
        }

        private static void WriteMiniDump(string tag)
        {
            if (!OperatingSystem.IsWindows() || crashDumpDir is null)
                return;

            try
            {
                var path = Path.Combine(crashDumpDir, $"{tag}_{Environment.ProcessId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dmp");
                using var fs = File.Create(path);
                using var process = Process.GetCurrentProcess();
                const uint MiniDumpWithFullMemory = 0x00000002;
                const uint MiniDumpWithFullMemoryInfo = 0x00000800;
                const uint MiniDumpWithHandleData = 0x00000004;
                const uint MiniDumpWithThreadInfo = 0x00001000;
                bool ok = MiniDumpWriteDump(process.Handle, (uint)process.Id, fs.SafeFileHandle.DangerousGetHandle(),
                    MiniDumpWithFullMemory | MiniDumpWithFullMemoryInfo | MiniDumpWithHandleData | MiniDumpWithThreadInfo,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                Console.Error.WriteLine(ok
                    ? $"[CrashDiag] Dump written: {path}"
                    : $"[CrashDiag] Dump failed: error {Marshal.GetLastWin32Error()}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[CrashDiag] Dump exception: {ex.Message}");
            }
        }
    }
}
