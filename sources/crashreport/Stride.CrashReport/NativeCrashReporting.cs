// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
    /// Shared entry point for the GUI/CLI hosts; the asset compiler wires the handler itself but reuses
    /// <see cref="ResolveCrashReporter"/> and <see cref="TrySpawnReporter"/>.
    /// </remarks>
    public static class NativeCrashReporting
    {
        private static bool installed;

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

            var informational = version ?? Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var resolvedVersion = string.IsNullOrEmpty(informational) ? "unknown" : informational;
            var resolvedEnvironment = environment ?? CrashReportSender.BuildEnvironment ?? "local";

            string dumpDir;
            try
            {
                // The reporter creates this run only if a crash fires, so a normal launch leaves nothing on disk.
                dumpDir = new CrashStore(applicationName).ReserveRunPath();
            }
            catch
            {
                return; // Can't resolve the store (e.g. permissions); nothing to arm.
            }

            // Out-of-process capture: resolve the reporter and arm the native trigger, passing the host's identity on
            // its command line. A managed VEH is unsupported (dotnet/runtime#119142) and can turn a caught exception
            // fatal; revisit this out-of-process design if a supported managed VEH lands.
            var reporter = ResolveCrashReporter();
            if (reporter == null || !reporter.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
                return;

            try { NativeInvoke.stride_crash_install(reporter, dumpDir, CaptureTimeoutMilliseconds, applicationName, resolvedVersion, resolvedEnvironment); }
            catch { return; }
        }

        // The crashing thread stays frozen while the reporter captures the dump; this bounds that wait so a
        // missing or wedged reporter can't hang the (already dying) process indefinitely.
        private const uint CaptureTimeoutMilliseconds = 30000;

        /// <summary>
        /// The dedup signature for a native crash: the faulting frame (<c>module+0x&lt;rva&gt;</c>) when the handler
        /// resolved one, else a per-dump fallback that does not group. Prefixed so native crashes stay distinct from
        /// managed signatures. Shared with the asset compiler's own native handler.
        /// </summary>
        public static string NativeSignature(string faultingFrame, string dumpPath)
            => "NativeCrash|" + (string.IsNullOrEmpty(faultingFrame) ? Path.GetFileNameWithoutExtension(dumpPath) : faultingFrame);

        /// <summary>
        /// Recovers the faulting frame (<c>module+0x&lt;rva&gt;</c>) from a minidump, post-mortem. On Windows the
        /// vectored handler computes this live at fault time; on Linux/macOS the runtime's <c>createdump</c> writes
        /// only the dump, so the frame is parsed back out of it here (same value, so the signature matches). Returns
        /// null when the dump carries no exception record or the address is outside every module.
        /// </summary>
        public static string FaultingFrameFromDump(string dumpPath) => MinidumpReader.FaultingFrame(dumpPath);

        /// <summary>
        /// The crash-report exception line for a native access violation, naming the faulting frame when known. The
        /// frame travels in the message so Sentry — which has no stack trace for these — groups them by fault location
        /// instead of collapsing every native crash into one issue.
        /// </summary>
        public static string NativeCrashMessage(string faultingFrame)
            => string.IsNullOrEmpty(faultingFrame)
                ? "Native crash (access violation). See the attached minidump for the faulting thread and loaded modules."
                : $"Native crash (access violation) in {faultingFrame}. See the attached minidump for the faulting thread and loaded modules.";

        /// <summary>
        /// Launches the out-of-process reporter for a crash run, returning false when the reporter can't be
        /// found (the caller then leaves the crash on disk for <c>stride crash send</c>). Fire-and-forget: the
        /// reporter is a separate GUI process that outlives the crashing one.
        /// </summary>
        public static bool TrySpawnReporter(string runDirectory)
        {
            var reporter = ResolveCrashReporter();
            if (reporter == null)
                return false;

            if (reporter.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                // No apphost for this OS (the publish output ships one only for the OS it was built on): run the
                // dll through the shared dotnet host. This is the Unix path, where spawning doesn't leak handles.
                var viaHost = new ProcessStartInfo("dotnet") { UseShellExecute = false };
                viaHost.ArgumentList.Add(reporter);
                AddReporterArguments(viaHost, runDirectory);
                Process.Start(viaHost);
                return true;
            }

            // Detach fully on Windows: with UseShellExecute=false the reporter inherits our std handles and, under
            // MSBuild's <Exec>, the inherited pipe blocks the build until the reporter closes; ShellExecuteEx inherits
            // nothing. Unix has no such leak (pipes are O_CLOEXEC) and shell-execute there would route through xdg-open.
            var viaApphost = new ProcessStartInfo(reporter) { UseShellExecute = OperatingSystem.IsWindows() };
            AddReporterArguments(viaApphost, runDirectory);
            Process.Start(viaApphost);
            return true;
        }

        private static void AddReporterArguments(ProcessStartInfo startInfo, string runDirectory)
        {
            startInfo.ArgumentList.Add(runDirectory);
            if (!string.IsNullOrEmpty(CrashReportSender.BuildDsn))
            {
                startInfo.ArgumentList.Add("--dsn");
                startInfo.ArgumentList.Add(CrashReportSender.BuildDsn);
            }
        }

        /// <summary>
        /// Locates the reporter to launch. An explicit override wins (dev/testing, odd deploy layouts), then a
        /// sibling copy (the reporter's own publish output, or a host that carries it), then — in a source
        /// checkout — the reporter's dev build output, and finally the newest <c>Stride.CrashReporter</c> package
        /// installed in the NuGet global store. Returns the apphost exe on Windows and the managed dll elsewhere;
        /// null when nothing is found.
        /// </summary>
        public static string ResolveCrashReporter()
        {
            var overridePath = Environment.GetEnvironmentVariable("STRIDE_CRASH_REPORTER");
            if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
                return overridePath;

            return FindReporterIn(AppContext.BaseDirectory) ?? FindReporterInDevTree() ?? FindReporterInStore();
        }

        // Source-checkout layout: the reporter isn't in the NuGet store (a source-built host doesn't restore its
        // own package), it's built under the checkout. Detect the root the same way the NuGet resolver does — walk
        // up for build/Stride.slnx — then resolve the reporter from its build output, preferring the configuration
        // the caller is running under. Its TFM (the xplat-editor net10.0) differs from the host's, so probe each.
        private static string FindReporterInDevTree()
        {
            try
            {
                var root = FindSourceRoot(AppContext.BaseDirectory);
                if (root == null)
                    return null;

                var reporterBin = Path.Combine(root, "sources", "crashreport", "Stride.CrashReporter", "bin");
                if (!Directory.Exists(reporterBin))
                    return null;

                foreach (var configuration in new[] { ConfigurationFromBinPath(AppContext.BaseDirectory), "Debug", "Release" })
                {
                    if (string.IsNullOrEmpty(configuration))
                        continue;
                    var configurationDirectory = Path.Combine(reporterBin, configuration);
                    if (!Directory.Exists(configurationDirectory))
                        continue;
                    foreach (var tfmDirectory in Directory.GetDirectories(configurationDirectory))
                    {
                        var reporter = FindReporterIn(tfmDirectory);
                        if (reporter != null)
                            return reporter;
                    }
                }
            }
            catch
            {
                // Best effort: a dev-tree probe failure just falls through to the store lookup.
            }
            return null;
        }

        // Walks up looking for build/Stride.slnx, the source-checkout marker the NuGet resolver keys on.
        private static string FindSourceRoot(string startDirectory)
        {
            var directory = startDirectory;
            while (!string.IsNullOrEmpty(directory))
            {
                if (File.Exists(Path.Combine(directory, "build", "Stride.slnx")))
                    return directory;
                directory = Path.GetDirectoryName(directory);
            }
            return null;
        }

        // The <Config> segment of a .../bin/<Config>/<tfm>/ path, or null when the path isn't such a layout.
        private static string ConfigurationFromBinPath(string path)
        {
            var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            for (var i = 0; i < parts.Length - 1; i++)
                if (string.Equals(parts[i], "bin", StringComparison.OrdinalIgnoreCase))
                    return parts[i + 1];
            return null;
        }

        // The reporter ships as a self-contained publish tree: prefer the native apphost on Windows (a GUI-subsystem
        // exe, so no console flashes on launch), else the managed dll run through dotnet. Require the managed dll: an
        // apphost can't run without it, and a Private=false project reference leaks a bare apphost stub (exe + config,
        // no dll or natives) into a consumer's bin — accepting that stub would "spawn" a reporter that instantly dies.
        private static string FindReporterIn(string directory)
        {
            var dll = Path.Combine(directory, "Stride.CrashReporter.dll");
            if (!File.Exists(dll))
                return null;

            if (OperatingSystem.IsWindows())
            {
                var exe = Path.Combine(directory, "Stride.CrashReporter.exe");
                if (File.Exists(exe))
                    return exe;
            }
            return dll;
        }

        // In a real install the reporter is delivered as the Stride.CrashReporter package, its publish tree under
        // tools/. Probe the NuGet global store directly (no NuGet assemblies pulled into this minimal library) and
        // take the newest installed version. Best-effort: any failure just means "no reporter", handled by callers.
        private static string FindReporterInStore()
        {
            try
            {
                var globalPackages = Environment.GetEnvironmentVariable("NUGET_PACKAGES");
                if (string.IsNullOrEmpty(globalPackages))
                {
                    var home = Environment.GetEnvironmentVariable("USERPROFILE") ?? Environment.GetEnvironmentVariable("HOME");
                    if (string.IsNullOrEmpty(home))
                        return null;
                    globalPackages = Path.Combine(home, ".nuget", "packages");
                }

                var packageRoot = Path.Combine(globalPackages, "stride.crashreporter");
                if (!Directory.Exists(packageRoot))
                    return null;

                foreach (var versionDirectory in Directory.GetDirectories(packageRoot).OrderByDescending(ParseVersion))
                {
                    var tools = Path.Combine(versionDirectory, "tools");
                    var reporter = Directory.Exists(tools) ? FindReporterIn(tools) : null;
                    if (reporter != null)
                        return reporter;
                }
            }
            catch
            {
                // Best effort: an unreadable store just means the reporter isn't auto-launched.
            }
            return null;
        }

        private static Version ParseVersion(string versionDirectory)
        {
            var name = Path.GetFileName(versionDirectory);
            var release = name.Split('-', '+')[0]; // drop the prerelease/build suffix; a coarse ordering is enough
            return Version.TryParse(release, out var version) ? version : new Version(0, 0);
        }
    }
}
