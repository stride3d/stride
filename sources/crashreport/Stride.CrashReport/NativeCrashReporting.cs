// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Stride.CrashReport
{
    /// <summary>
    /// Arms in-process capture of native access violations for an application. A native crash (native interop,
    /// GPU drivers, audio) kills the process before the managed unhandled-exception handlers can run, so this
    /// writes a triage minidump plus a <see cref="StoredCrash"/> to the crash store at fault time and, when
    /// attended, spawns the out-of-process reporter for consent. Windows/CoreCLR only; a no-op elsewhere.
    /// </summary>
    /// <remarks>
    /// This is the shared entry point for the GUI/CLI hosts (GameStudio, the launcher, the CLI). The asset
    /// compiler wires the same underlying handler itself because it folds native crashes into its per-build
    /// aggregation, but it reuses <see cref="ResolveCrashReporter"/> and <see cref="TrySpawnReporter"/> here.
    /// </remarks>
    public static class NativeCrashReporting
    {
        private static bool installed;
        private static CrashRun run;
        private static string application;
        private static string version;
        private static string environment;

        /// <summary>
        /// Arms native-crash capture for <paramref name="applicationName"/> (e.g. "GameStudio", "Launcher", "Cli").
        /// Safe to call once at startup; later calls are ignored. Off-Windows, under NativeAOT, or when
        /// STRIDE_CRASH_MODE=off, it does nothing.
        /// </summary>
        public static void Install(string applicationName, string version = null, string environment = null)
        {
            if (installed || !OperatingSystem.IsWindows())
                return;
            if (CrashPolicy.ResolveMode() == CrashMode.Off)
                return;

            installed = true;
            application = applicationName;

            var informational = version ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            NativeCrashReporting.version = string.IsNullOrEmpty(informational) ? "unknown" : informational;
            NativeCrashReporting.environment = environment ?? CrashReportSender.BuildEnvironment ?? "local";

            try
            {
                run = new CrashStore(applicationName).CreateRun();
            }
            catch
            {
                return; // Can't create the store (e.g. permissions); nothing to arm.
            }

            NativeCrashHandler.InstallForReporting(run.Directory, NativeCrashHandler.TriageDump, OnNativeCrash);

            // A run directory is created eagerly so the dump has a home; delete it on a clean exit so normal
            // launches don't litter the store.
            AppDomain.CurrentDomain.ProcessExit += (_, _) =>
            {
                try { if (run.IsEmpty) run.Delete(); } catch { /* best effort */ }
            };
        }

        // Runs inside the faulting, possibly-corrupt process: do the minimum — record a crash referencing the dump,
        // then (attended) spawn the reporter. Never throws. The dump carries the faulting thread and modules.
        private static void OnNativeCrash(string dumpPath)
        {
            try
            {
                var data = new CrashReportData
                {
                    ["Application"] = application,
                    ["Exception"] = "Native crash (access violation). See the attached minidump for the faulting thread and loaded modules.",
                };
                CrashReportAnonymizer.Scrub(data);

                var crash = StoredCrash.FromReportData(data);
                crash.Application = application;
                crash.Version = version;
                crash.Environment = environment;
                crash.TimestampUtc = DateTime.UtcNow.ToString("o");
                // No faulting frame is available in-handler, so the signature can't be precise yet (a follow-up can
                // derive it from the dump). Native crashes kill the process, so there is at most one per run.
                crash.Signature = "NativeCrash|" + Path.GetFileNameWithoutExtension(dumpPath);
                crash.DumpFileName = Path.GetFileName(dumpPath);
                File.WriteAllText(Path.Combine(run.Directory, $"crash-native-{Environment.ProcessId}.json"), crash.ToJson());
            }
            catch
            {
                // Dying process: never throw from the fault handler.
            }

            // Only the interactive-attended case pops the reporter; save/send modes leave the files for
            // 'stride crash send' / CI artifact collection (sending from a dying process is too heavy).
            if (CrashPolicy.ResolveAction() == CrashAction.Report)
            {
                try { TrySpawnReporter(run.Directory); } catch { /* dying process */ }
            }
        }

        /// <summary>
        /// Launches the out-of-process reporter for a crash run, returning false when the reporter exe can't be
        /// found (the caller then leaves the crash on disk for <c>stride crash send</c>). Fire-and-forget: the
        /// reporter is a separate GUI process that outlives the crashing one.
        /// </summary>
        public static bool TrySpawnReporter(string runDirectory)
        {
            var reporter = ResolveCrashReporter();
            if (reporter == null)
                return false;

            var arguments = $"\"{runDirectory}\"";
            if (!string.IsNullOrEmpty(CrashReportSender.BuildDsn))
                arguments += $" --dsn \"{CrashReportSender.BuildDsn}\"";
            // Detach fully on Windows: CreateProcess-based spawning (UseShellExecute=false) makes the reporter
            // inherit our std handles, and when the host runs under MSBuild's <Exec> the inherited output pipe
            // keeps the build waiting until the reporter window closes (verified; redirecting doesn't help, the
            // old handles leak regardless). ShellExecuteEx inherits nothing. On Unix there is no such leak
            // (.NET pipes are O_CLOEXEC) and shell-execute would route through xdg-open, so keep plain spawning.
            Process.Start(new ProcessStartInfo(reporter, arguments) { UseShellExecute = OperatingSystem.IsWindows() });
            return true;
        }

        /// <summary>The reporter exe: an explicit override wins (dev/testing, odd deploy layouts), else the sibling exe.</summary>
        public static string ResolveCrashReporter()
        {
            var overridePath = Environment.GetEnvironmentVariable("STRIDE_CRASH_REPORTER");
            if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
                return overridePath;
            var name = OperatingSystem.IsWindows() ? "Stride.CrashReporter.exe" : "Stride.CrashReporter";
            var beside = Path.Combine(AppContext.BaseDirectory, name);
            return File.Exists(beside) ? beside : null;
        }
    }
}
