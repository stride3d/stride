// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;

using Stride.Core;

namespace Stride.Graphics.Regression;

internal static class Module
{
    [DllImport("kernel32.dll")]
    private static extern uint SetErrorMode(uint uMode);

    [DllImport("dbghelp.dll", SetLastError = true)]
    private static extern bool MiniDumpWriteDump(IntPtr hProcess, uint processId, IntPtr hFile,
        uint dumpType, IntPtr exceptionParam, IntPtr userStreamParam, IntPtr callbackParam);

    private static string crashDumpDir;

    [System.Runtime.CompilerServices.ModuleInitializer]
    internal static void Initialize()
    {
        if (OperatingSystem.IsWindows())
        {
            // Prevent error dialogs that would hang CI or interrupt local testing.
            // SEM_NOGPFAULTERRORBOX suppresses the Windows Error Reporting crash dialog.
            // Crash dumps are handled by DOTNET_DbgEnableMiniDump and our FirstChanceException handler.
            SetErrorMode(0x0001 /* SEM_FAILCRITICALERRORS */ | 0x0002 /* SEM_NOGPFAULTERRORBOX */ | 0x8000 /* SEM_NOOPENFILEERRORBOX */);
        }

        // GPU drivers (including software renderers like WARP and SwiftShader) can crash with
        // native access violations when used incorrectly (e.g., releasing resources still in use).
        // These crashes are hard to diagnose: .NET 8+ can't catch them via try/catch, and WER
        // doesn't trigger because .NET handles the exception dispatch internally.
        // When STRIDE_TESTS_CRASH_DUMPS is set, we register a FirstChanceException handler to log
        // the stack trace and write a minidump before the process terminates. This complements
        // DOTNET_DbgEnableMiniDump (for .NET unhandled exceptions) and WER LocalDumps (for pure
        // native crashes).
        var crashDiag = Environment.GetEnvironmentVariable("STRIDE_TESTS_CRASH_DUMPS");
        if (crashDiag == "1")
        {
            // Use STRIDE_TESTS_CRASH_DUMP_DIR if set (e.g., from CI), otherwise fall back to working dir
            crashDumpDir = Environment.GetEnvironmentVariable("STRIDE_TESTS_CRASH_DUMP_DIR")
                ?? Path.Combine(Environment.CurrentDirectory, "crash-dumps");
            Directory.CreateDirectory(crashDumpDir);

            AppDomain.CurrentDomain.FirstChanceException += OnFirstChanceException;
        }

        // Default to software rendering unless STRIDE_TESTS_GPU=1 is set.
        // This ensures Test Explorer and dotnet test match the gold images out of the box.
        if (Environment.GetEnvironmentVariable("STRIDE_TESTS_GPU") != "1")
        {
            Environment.SetEnvironmentVariable("STRIDE_GRAPHICS_SOFTWARE_RENDERING", "1");

            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("STRIDE_MAX_PARALLELISM")))
                Environment.SetEnvironmentVariable("STRIDE_MAX_PARALLELISM", "8");

            // Auto-configure SwiftShader ICD path for Vulkan software rendering.
            // SwiftShader libs are in runtimes/{rid}/native/ — find the right one for this OS.
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VK_DRIVER_FILES")))
            {
                var (rid, libName) = Platform.Type switch
                {
                    PlatformType.Windows => ("win-x64", "vk_swiftshader.dll"),
                    PlatformType.Linux => ("linux-x64", "libvk_swiftshader.so"),
                    PlatformType.macOS => ("osx-arm64", "libvk_swiftshader.dylib"),
                    _ => throw new PlatformNotSupportedException($"Software rendering not supported on {Platform.Type}")
                };
                var libPath = Path.Combine(AppContext.BaseDirectory, "runtimes", rid, "native", libName);
                if (File.Exists(libPath))
                {
                    var icdPath = Path.Combine(AppContext.BaseDirectory, "vk_swiftshader_icd.json");
                    File.WriteAllText(icdPath, $$"""
                        {
                            "file_format_version": "1.0.0",
                            "ICD": {
                                "library_path": "{{libPath.Replace(@"\", @"\\")}}",
                                "api_version": "1.1.0"
                            }
                        }
                        """);
                    Environment.SetEnvironmentVariable("VK_DRIVER_FILES", icdPath);
                }
            }
        }
    }

    /// <summary>
    ///   Logs SEHException details on first-chance and writes a minidump. These are native access
    ///   violations that .NET 8+ cannot catch via try/catch and will terminate the process.
    /// </summary>
    private static void OnFirstChanceException(object sender, FirstChanceExceptionEventArgs e)
    {
        if (e.Exception is SEHException seh)
        {
            Console.Error.WriteLine($"[CrashDiag] FirstChanceException: SEHException HResult=0x{seh.HResult:X8}");
            Console.Error.WriteLine($"[CrashDiag] Exception stack: {seh.StackTrace}");
            foreach (var line in Environment.StackTrace.Split('\n'))
                Console.Error.WriteLine($"[CrashDiag]   {line.TrimEnd()}");

            WriteMiniDump("firstchance_seh");
        }
    }

    private static void WriteMiniDump(string tag)
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            var path = Path.Combine(crashDumpDir, $"{tag}_{Environment.ProcessId}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.dmp");
            using var fs = File.Create(path);
            var process = Process.GetCurrentProcess();
            const uint MiniDumpWithThreadInfo = 0x00001000;
            const uint MiniDumpWithFullMemoryInfo = 0x00000800;
            bool ok = MiniDumpWriteDump(process.Handle, (uint)process.Id, fs.SafeFileHandle.DangerousGetHandle(),
                MiniDumpWithThreadInfo | MiniDumpWithFullMemoryInfo, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
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
