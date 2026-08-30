// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace Stride
{
    /// <summary>
    /// Native-crash diagnostics. Two entry points: <see cref="Install"/> for test/sample runs (suppresses the
    /// Windows crash dialog so a crash can't hang CI, and — when STRIDE_TESTS_CRASH_DUMPS=1 — logs the SEH stack
    /// and writes a minidump), and <see cref="InstallForReporting"/> for crash reporting (writes a triage dump on
    /// a native access violation and calls back so a reporter can be spawned).
    /// </summary>
    /// <remarks>
    /// GPU drivers (incl. software renderers like WARP/Lavapipe), native audio (XAudio2), and native asset
    /// importers/shader compilers can crash with access violations. .NET 8+ can't catch these via try/catch: a
    /// pure-native AV is a corrupted-state exception the runtime fast-fails without raising
    /// <see cref="AppDomain.FirstChanceException"/>, so it needs a Vectored Exception Handler to observe at all.
    /// Shared by Stride.Graphics.Regression and Stride.Games.AutoTesting (via <see cref="Install"/> from a
    /// ModuleInitializer) and by the asset compiler (via <see cref="InstallForReporting"/>).
    /// </remarks>
    internal static class NativeCrashHandler
    {
        [DllImport("kernel32.dll")]
        private static extern uint SetErrorMode(uint uMode);

        [DllImport("dbghelp.dll", SetLastError = true)]
        private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile,
            uint dumpType, IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

        [DllImport("kernel32.dll")]
        private static extern IntPtr AddVectoredExceptionHandler(uint first, IntPtr handler);
        [DllImport("kernel32.dll")]
        private static extern uint RemoveVectoredExceptionHandler(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetModuleHandleExW(uint flags, IntPtr address, out IntPtr module);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint GetModuleFileNameW(IntPtr module, [Out] char[] filename, uint size);

        [DllImport("kernel32.dll")]
        private static extern uint GetCurrentThreadId();

        // PVECTORED_EXCEPTION_HANDLER: LONG (*)(PEXCEPTION_POINTERS). Kept in a static field so the reverse
        // P/Invoke thunk isn't collected while registered.
        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int VectoredHandler(IntPtr exceptionPointers);

        // Pack=4 required: dbghelp.h structs are 4-byte packed; natural x64 layout makes dbghelp read a
        // garbage pointer and fail with ERROR_NOACCESS.
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        private struct MinidumpExceptionInformation
        {
            public uint ThreadId;
            public IntPtr ExceptionPointers;
            public int ClientPointers; // BOOL
        }

        private const uint StatusAccessViolation = 0xC0000005;
        private const int ExceptionContinueSearch = 0; // let the runtime terminate the process as it would
        // GetModuleHandleEx: resolve by an address inside the module, without touching its refcount.
        private const uint GetModuleByAddressUnchanged = 0x0004 /* FROM_ADDRESS */ | 0x0002 /* UNCHANGED_REFCOUNT */;

        // MINIDUMP_TYPE flags.
        private const uint MiniDumpNormal = 0x00000000;
        private const uint MiniDumpWithFullMemory = 0x00000002;
        private const uint MiniDumpWithHandleData = 0x00000004;
        private const uint MiniDumpWithUnloadedModules = 0x00000020;
        private const uint MiniDumpWithFullMemoryInfo = 0x00000800;
        private const uint MiniDumpWithThreadInfo = 0x00001000;

        /// <summary>A full-memory dump: everything in the process, several GB, unanonymized. For local test triage.</summary>
        public const uint FullMemoryDump = MiniDumpWithFullMemory | MiniDumpWithFullMemoryInfo | MiniDumpWithHandleData | MiniDumpWithThreadInfo;

        /// <summary>A triage dump: every thread's call stack plus the module list, no process memory. What crash reports ship.</summary>
        public const uint TriageDump = MiniDumpNormal | MiniDumpWithUnloadedModules | MiniDumpWithThreadInfo;

        private static string crashDumpDir;
        private static uint crashDumpType;
        private static string crashDumpTag;
        private static Action<string> onCrashDump;
        private static bool logToConsole;
        private static VectoredHandler vectoredHandler; // rooted so the thunk survives while registered
        private static IntPtr vectoredHandlerHandle;    // for the unregistration at process exit
        private static int crashHandled;               // 0/1 guard: dump at most once across VEH + FirstChance

        /// <summary>
        /// Installs the crash-dialog suppression and (when STRIDE_TESTS_CRASH_DUMPS=1) the SEH minidump
        /// handler for test/sample runs. Idempotent enough for one call per assembly ModuleInitializer.
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
            var dir = Environment.GetEnvironmentVariable("STRIDE_TESTS_CRASH_DUMP_DIR")
                ?? Path.Combine(AppContext.BaseDirectory, "crash-dumps");
            RegisterHandler(dir, FullMemoryDump, tag: "firstchance_seh", onDump: null, log: true);

            // FirstChanceException only catches native AVs that surface as managed SEH; pure-native ones (incl. null
            // derefs) it misses. CI covers those with createdump (DOTNET_DbgEnableMiniDump) + WER LocalDumps, but a
            // local run has neither unless the developer sets up the WER registry. So when createdump is NOT enabled,
            // add the vectored handler to capture pure-native AVs in-process — zero-setup local repro. When it IS
            // enabled (CI), skip it: createdump already covers these and a second in-process dump would duplicate it.
            if (OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("DOTNET_DbgEnableMiniDump") != "1")
                RegisterVectoredHandler();
        }

        /// <summary>
        /// Installs the native-fault handler for crash reporting: writes a dump on a native access violation and
        /// hands its path to <paramref name="onDump"/> (which runs in the faulting, possibly-corrupt process, so it
        /// must do the minimum — e.g. spawn a reporter). The crash dialog is suppressed so a fault can't hang a build.
        /// </summary>
        /// <remarks>
        /// A pure-native access violation is a corrupted-state exception: the runtime fast-fails it without ever
        /// raising <see cref="AppDomain.FirstChanceException"/>. So this registers a <c>Vectored Exception Handler</c>
        /// — which sees the raw fault before the runtime tears down — as the primary capture, and keeps the
        /// FirstChanceException handler only for the SEH exceptions that do surface as managed. Because the VEH
        /// writes the dump at fault time, suppressing the dialog (which also blocks WER/createdump) costs no coverage.
        /// </remarks>
        public static void InstallForReporting(string dumpDir, uint dumpType, Action<string> onDump)
        {
            RegisterHandler(dumpDir, dumpType, tag: "native", onDump: onDump, log: false);

            if (OperatingSystem.IsWindows())
            {
                SetErrorMode(0x0001 /* SEM_FAILCRITICALERRORS */ | 0x0002 /* SEM_NOGPFAULTERRORBOX */ | 0x8000 /* SEM_NOOPENFILEERRORBOX */);
                RegisterVectoredHandler();
            }
        }

        // Registers the native-AV handler LAST (first=0) so the runtime's own vectored handler runs before us: it
        // redirects managed hardware null-checks to the NullReferenceException throw path before our (managed) thunk
        // is invoked, which both keeps managed NREs catchable and avoids a managed-call-in-fault-context fault.
        // Genuine native AVs, which the runtime passes through, still reach us. The thunk is created here, not at
        // fault time, so nothing is JIT-compiled in the corrupt context.
        private static void RegisterVectoredHandler()
        {
            // The filter tells managed faults from native ones by "JIT'd managed code has no backing module." That
            // holds under CoreCLR but not NativeAOT, where managed code lives in the app module and would be misread
            // as native. Skip under AOT; WER/createdump stay the native-crash path there.
            if (!RuntimeFeature.IsDynamicCodeSupported)
                return;

            vectoredHandler = OnVectoredException;
            vectoredHandlerHandle = AddVectoredExceptionHandler(0, Marshal.GetFunctionPointerForDelegate(vectoredHandler));
            // A vectored handler sees every exception in the process, including the debug prints (DBG_PRINTEXCEPTION_C)
            // the D3D/DXGI debug layers raise from DllMain while the process unloads. By then the runtime is shutting
            // down and this managed thunk can no longer run, so the runtime turns that benign print into a fatal dump.
            // Unregister while managed code is still allowed; the native fault path (createdump / WER) stays armed.
            AppDomain.CurrentDomain.ProcessExit += (_, _) => UnregisterVectoredHandler();
        }

        private static void UnregisterVectoredHandler()
        {
            var handle = Interlocked.Exchange(ref vectoredHandlerHandle, IntPtr.Zero);
            if (handle != IntPtr.Zero)
                RemoveVectoredExceptionHandler(handle);
        }

        // Runs in the raw native-fault context (after the runtime's own vectored handler — see the first=0 note in
        // RegisterVectoredHandler), so it does the minimum and never assumes a healthy CLR. Only genuine native access
        // violations are acted on (see ShouldCaptureNativeFault); the CLR's own hardware null-checks and everything
        // else are passed straight through.
        private static int OnVectoredException(IntPtr exceptionPointers)
        {
            if (exceptionPointers == IntPtr.Zero)
                return ExceptionContinueSearch;

            // EXCEPTION_POINTERS { EXCEPTION_RECORD* ExceptionRecord; CONTEXT* ContextRecord; }
            var record = Marshal.ReadIntPtr(exceptionPointers, 0);
            if (record == IntPtr.Zero)
                return ExceptionContinueSearch;

            // EXCEPTION_RECORD.ExceptionCode is the first field.
            if ((uint)Marshal.ReadInt32(record, 0) != StatusAccessViolation)
                return ExceptionContinueSearch;

            if (!ShouldCaptureNativeFault(record))
                return ExceptionContinueSearch;

            if (Interlocked.Exchange(ref crashHandled, 1) != 0)
                return ExceptionContinueSearch;

            try
            {
                var path = WriteMiniDump(crashDumpTag, exceptionPointers);
                if (path != null)
                {
                    try { onCrashDump?.Invoke(path); } catch { /* dying process; do not throw */ }
                }
            }
            catch { /* dying process; do not throw */ }

            // Let the fault propagate so the process terminates as it would have — we only observed it.
            return ExceptionContinueSearch;
        }

        // Decides whether an access violation is a genuine native crash worth a dump, vs the CLR's own hardware
        // null-check (which becomes a NullReferenceException the runtime handles). The reliable signal is where the
        // faulting *instruction* lives, not the data address: JIT-compiled managed code is backed by no module.
        private static bool ShouldCaptureNativeFault(IntPtr record)
        {
            // EXCEPTION_RECORD: ExceptionAddress (faulting instruction) at x64 offset 0x10; ExceptionInformation[1]
            // (faulting data address) at 0x28.
            var instructionAddress = Marshal.ReadIntPtr(record, 0x10);
            var faultAddress = (ulong)Marshal.ReadIntPtr(record, 0x28);

            if (!GetModuleHandleExW(GetModuleByAddressUnchanged, instructionAddress, out var module) || module == IntPtr.Zero)
            {
                // No module backs the faulting instruction → JIT-compiled managed code. A sub-64 KB fault there is a
                // hardware null-check (→ NullReferenceException); ignore it. Higher is a genuine unsafe/marshaling AV.
                return faultAddress >= 0x10000;
            }

            // The instruction is in a loaded module. The runtime's own modules also fault-and-fix-up managed null
            // checks, so apply the same 64 KB rule there; every other module is native code, where even a null-pointer
            // dereference (a low fault address) is a real crash we must capture.
            return !IsRuntimeModule(module) || faultAddress >= 0x10000;
        }

        private static bool IsRuntimeModule(IntPtr module)
        {
            var buffer = new char[512];
            uint length = GetModuleFileNameW(module, buffer, (uint)buffer.Length);
            if (length == 0)
                return false;

            var name = Path.GetFileName(new string(buffer, 0, (int)length));
            return name.Equals("coreclr.dll", StringComparison.OrdinalIgnoreCase)
                || name.Equals("clrjit.dll", StringComparison.OrdinalIgnoreCase)
                || name.Equals("clrgc.dll", StringComparison.OrdinalIgnoreCase)
                || name.Equals("clrgcexp.dll", StringComparison.OrdinalIgnoreCase);
        }

        private static void RegisterHandler(string dir, uint dumpType, string tag, Action<string> onDump, bool log)
        {
            crashDumpDir = dir;
            crashDumpType = dumpType;
            crashDumpTag = tag;
            onCrashDump = onDump;
            logToConsole = log;
            try { Directory.CreateDirectory(dir); } catch { /* best effort */ }

            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
        {
            if (e.Exception is not SEHException seh)
                return;

            if (logToConsole)
            {
                Console.Error.WriteLine($"[CrashDiag] FirstChanceException: SEHException HResult=0x{seh.HResult:X8}");
                Console.Error.WriteLine($"[CrashDiag] Exception stack: {seh.StackTrace}");
                foreach (var line in Environment.StackTrace.Split('\n'))
                    Console.Error.WriteLine($"[CrashDiag]   {line.TrimEnd()}");
            }

            // Shared guard: a native AV can trip the VEH and then surface here too; dump only once.
            if (Interlocked.Exchange(ref crashHandled, 1) != 0)
                return;

            var path = WriteMiniDump(crashDumpTag, IntPtr.Zero);
            if (path != null)
            {
                // Minimal, best-effort: the caller decides what to do (typically spawn a reporter). Any failure
                // here must not mask the crash.
                try { onCrashDump?.Invoke(path); } catch { /* dying process; do not throw */ }
            }
        }

        // Writes a dump of every thread's stack plus the module list. When exceptionPointers is non-null (the
        // VEH path), the dump also carries the exception record, so a debugger auto-selects the faulting thread;
        // zero (the FirstChance path, where only the managed exception exists) writes a plain dump.
        private static string WriteMiniDump(string tag, IntPtr exceptionPointers)
        {
            if (!OperatingSystem.IsWindows() || crashDumpDir is null)
                return null;

            try
            {
                var path = Path.Combine(crashDumpDir, $"{tag}_{Environment.ProcessId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dmp");
                using var fs = File.Create(path);
                using var process = Process.GetCurrentProcess();

                var exceptionParam = IntPtr.Zero;
                if (exceptionPointers != IntPtr.Zero)
                {
                    exceptionParam = Marshal.AllocHGlobal(Marshal.SizeOf<MinidumpExceptionInformation>());
                    Marshal.StructureToPtr(new MinidumpExceptionInformation
                    {
                        ThreadId = GetCurrentThreadId(),
                        ExceptionPointers = exceptionPointers,
                        ClientPointers = 0,
                    }, exceptionParam, false);
                }

                bool ok;
                try
                {
                    ok = MiniDumpWriteDump(process.Handle, (uint)process.Id, fs.SafeFileHandle.DangerousGetHandle(),
                        crashDumpType, exceptionParam, IntPtr.Zero, IntPtr.Zero);
                }
                finally
                {
                    if (exceptionParam != IntPtr.Zero)
                        Marshal.FreeHGlobal(exceptionParam);
                }
                if (logToConsole)
                    Console.Error.WriteLine(ok
                        ? $"[CrashDiag] Dump written: {path}"
                        : $"[CrashDiag] Dump failed: error {Marshal.GetLastWin32Error()}");
                return ok ? path : null;
            }
            catch (Exception ex)
            {
                if (logToConsole)
                    Console.Error.WriteLine($"[CrashDiag] Dump exception: {ex.Message}");
                return null;
            }
        }
    }
}
