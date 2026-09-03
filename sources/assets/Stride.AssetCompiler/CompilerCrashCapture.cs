// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization.Contents;
using Stride.CrashReport;

namespace Stride.AssetCompiler
{
    /// <summary>
    /// Captures asset-compiler crashes to the on-disk store. It is wired into the build engine's
    /// <see cref="Builder.CommandFailed"/> hook, so an exception that escapes a command's top-level catch — a
    /// bug, not a handled build error — becomes a deduped crash report. A separate reporter or
    /// <c>stride crash send</c> submits it later; this stays UI-free and headless.
    /// </summary>
    internal sealed class CompilerCrashCapture
    {
        private const string ApplicationName = "AssetCompiler";

        private readonly CrashStore store;
        private readonly CrashMode mode;
        private readonly string version;
        private readonly string environment;
        private readonly string platform;
        private readonly string graphicsApi;
        private readonly string configuration;
        private readonly object gate = new object();
        // The master handles the run at end of build (applies suppression, sends on CI); a slave only writes
        // crash files into the master's shared run for the master to collect.
        private bool isMaster = true;
        private CrashRun run; // created on the first capture (or eagerly when the native handler is armed / shared with slaves)

        public CompilerCrashCapture(PackageBuilderOptions options)
        {
            mode = CrashPolicy.ResolveMode();
            store = new CrashStore(ApplicationName);

            var informational = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            version = string.IsNullOrEmpty(informational) ? "unknown" : informational;
            // Stride's own CI sets mode=send and wants environment=ci; otherwise use the build-baked value or local.
            environment = mode == CrashMode.Send ? "ci" : (CrashReportSender.BuildEnvironment ?? "local");

            platform = options.Platform.ToString();
            configuration = options.ProjectConfiguration ?? string.Empty;
            graphicsApi = options.ExtraCompileProperties != null && options.ExtraCompileProperties.TryGetValue("StrideGraphicsApi", out var api) ? api : string.Empty;
        }

        // Slave: capture into the master's shared run directory. The master decided capture is on (it only passes
        // --crash-dir then) and owns reporting; the slave just writes managed and native crashes for it to collect.
        private CompilerCrashCapture(PackageBuilderOptions options, CrashRun sharedRun) : this(options)
        {
            run = sharedRun;
            isMaster = false;
        }

        /// <summary>Creates the capture an isolated slave process uses, bound to the master's shared run directory.</summary>
        public static CompilerCrashCapture ForSlave(PackageBuilderOptions options)
            => new CompilerCrashCapture(options, CrashStore.OpenRun(options.CrashRunDirectory));

        /// <summary>False when crash handling is turned off entirely; the build wires the hook only when true.</summary>
        public bool Enabled => mode != CrashMode.Off;

        /// <summary>The run captured crashes were written to, or null when nothing crashed.</summary>
        public CrashRun Run => run;

        /// <summary>Removes crash runs older than the retention window, so the store never grows unbounded.</summary>
        public void PruneOld() => store.Prune(TimeSpan.FromDays(30));

        /// <summary>The run captured crashes are written to, created on first use. Shared with the build's slaves.</summary>
        public CrashRun EnsureRun()
        {
            lock (gate)
                return run ??= store.CreateRun();
        }

        /// <summary>
        /// Records a command failure. Called from build worker threads, possibly concurrently, so the
        /// read-modify-write of the store is serialized.
        /// </summary>
        public void Capture(CommandBuildStep step, Exception exception)
            => Capture(step.Command, step.Tag as AssetItem, exception);

        /// <summary>
        /// Records a crash from an isolated slave process (its one command threw or died). The failing asset is
        /// known on the master's side of the command, not here, so slave crashes carry the command and exception only.
        /// </summary>
        public void CaptureCommand(Command command, Exception exception)
            => Capture(command, asset: null, exception);

        /// <summary>
        /// Adopts the minidumps the runtime's <c>createdump</c> left behind into the crash store, then does what
        /// the build would with them (send on CI, or point the user at <c>stride crash send</c>). This is the
        /// Linux/macOS native path: there is no in-process vectored handler there, so a native crash kills the
        /// process and the runtime writes a raw dump; a fresh <c>crash-adopt</c> invocation after the build turns
        /// each dump into a signed, deduped <see cref="StoredCrash"/>. Returns a process exit code (always success:
        /// the crash already failed the build).
        /// </summary>
        public static int AdoptNativeDumps(PackageBuilderOptions options)
            => new CompilerCrashCapture(options).AdoptDumps(options.NativeDumpDirectory, options.Logger);

        private int AdoptDumps(string dumpDirectory, ILogger logger)
        {
            if (mode == CrashMode.Off || string.IsNullOrEmpty(dumpDirectory) || !Directory.Exists(dumpDirectory))
                return 0;

            string[] dumps;
            try { dumps = Directory.GetFiles(dumpDirectory, "*.dmp"); }
            catch { return 0; }

            foreach (var dumpPath in dumps)
            {
                try
                {
                    // Recover the faulting frame the Windows handler would have computed live, so Linux/macOS
                    // signatures match Windows ones and identical native crashes dedup across platforms.
                    var faultingFrame = NativeCrashReporting.FaultingFrameFromDump(dumpPath);
                    var signature = NativeCrashReporting.NativeSignature(faultingFrame, dumpPath);
                    if (store.IsSuppressed(signature, version))
                    {
                        TryDelete(dumpPath);
                        continue;
                    }

                    var data = new CrashReportData
                    {
                        ["Application"] = ApplicationName,
                        ["Exception"] = NativeCrashReporting.NativeCrashMessage(faultingFrame),
                        ["Platform"] = platform,
                        ["GraphicsApi"] = graphicsApi,
                        ["Configuration"] = configuration,
                    };
                    if (!string.IsNullOrEmpty(faultingFrame))
                        data["FaultingFrame"] = faultingFrame;

                    var crash = NewStoredCrash(data);
                    crash.Signature = signature;
                    EnsureRun().Add(crash, File.ReadAllBytes(dumpPath));
                    TryDelete(dumpPath);
                }
                catch (Exception e)
                {
                    logger.Warning($"Could not adopt native crash dump {Path.GetFileName(dumpPath)}: {e.Message}");
                }
            }

            if (run != null && !run.IsEmpty)
            {
                switch (CrashPolicy.ResolveAction())
                {
                    case CrashAction.Send:
                        SendRun(run, logger);
                        break;
                    case CrashAction.Report:
                        logger.Warning($"{run.Read().Count} native crash(es) captured during asset build; saved to {run.Directory}. Submit them with 'stride crash send'.");
                        break;
                    // Save / Ignore: leave the run on disk for a later 'stride crash send'.
                }
                PruneOld();
            }
            return 0;
        }

        // CI: send every group headlessly, keeping the files (the CI runner is ephemeral and an artifact step may
        // still collect them, and a failed send must not lose the report). Mirrors the master's end-of-build send.
        private static void SendRun(CrashRun run, ILogger logger)
        {
            if (CrashReportSender.IsDisabled)
                return;
            var dsn = CrashReportSender.ResolveDsn();
            foreach (var crash in run.Read())
            {
                try
                {
                    CrashReportSender.SendAsync(crash, run.ReadDump(crash), dsn).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    logger.Warning($"Could not send crash report: {e.Message}");
                }
            }
        }

        private static void TryDelete(string path)
        {
            try { if (File.Exists(path)) File.Delete(path); }
            catch { /* best effort: a locked dump is left in the staging dir, retried next build */ }
        }

        private void Capture(Command command, AssetItem asset, Exception exception)
        {
            if (mode == CrashMode.Off)
                return;
            // Denylist is control-flow only: a cancelled build is not a crash. TaskCanceledException derives from it.
            if (exception is OperationCanceledException)
                return;

            var stepKind = command?.GetType().Name ?? "UnknownCommand";
            var signature = CrashSignature.Compute(exception, stepKind);

            lock (gate)
            {
                // Suppression is a master concept (the user chose it at the reporter); a slave writes unconditionally
                // and the master's own suppression already skips its local occurrences of the same signature.
                if (isMaster && store.IsSuppressed(signature, version))
                    return;
                run ??= store.CreateRun();
                run.Add(BuildCrash(command, asset, exception, stepKind, signature));
            }
        }

        private StoredCrash BuildCrash(Command command, AssetItem asset, Exception exception, string stepKind, string signature)
        {
            var assetType = asset?.Asset?.GetType().Name;
            var assetLabel = asset != null ? $"{asset.Location.GetFileName()} ({assetType})" : null;

            var data = new CrashReportData
            {
                ["Application"] = ApplicationName,
                ["Exception"] = exception.ToString(),
                ["StepKind"] = stepKind,
                ["AssetType"] = assetType,
                ["Asset"] = assetLabel,
                ["Platform"] = platform,
                ["GraphicsApi"] = graphicsApi,
                ["Configuration"] = configuration,
                ["NativeModules"] = DescribeNativeModules(),
            };
            var crash = NewStoredCrash(data);
            crash.Signature = signature;
            crash.Exceptions = StoredException.Capture(exception);
            if (assetLabel != null)
                crash.AffectedAssets.Add(assetLabel);
            // Local-only real paths, so the reporter can offer to attach them if the user opts in. Never sent as text.
            crash.AssetDefinitionPath = asset?.FullPath?.ToOSPath();
            crash.AssetSourcePaths.AddRange(CollectSourceInputs(command));
            return crash;
        }

        // Scrub the report and stamp the shared metadata; callers set Signature and any dump/asset fields.
        private StoredCrash NewStoredCrash(CrashReportData data)
        {
            CrashReportAnonymizer.Scrub(data);
            var crash = StoredCrash.FromReportData(data);
            crash.Application = ApplicationName;
            crash.Version = version;
            crash.Environment = environment;
            crash.TimestampUtc = DateTime.UtcNow.ToString("o");
            return crash;
        }

        private static IEnumerable<string> CollectSourceInputs(Command command)
        {
            var paths = new List<string>();
            try
            {
                foreach (var input in command?.GetInputFiles() ?? Enumerable.Empty<ObjectUrl>())
                    if (input.Type == UrlType.File && !string.IsNullOrEmpty(input.Path))
                        paths.Add(input.Path);
            }
            catch (Exception)
            {
                // GetInputFiles can itself throw on a broken asset; the rest of the report still stands.
            }

            return paths.Distinct();
        }

        // Loaded native modules and their versions — the gold for diagnosing native access violations.
        private static string DescribeNativeModules()
        {
            try
            {
                var lines = new List<string>();
                foreach (ProcessModule module in Process.GetCurrentProcess().Modules)
                {
                    var moduleVersion = module.FileVersionInfo?.FileVersion;
                    lines.Add(string.IsNullOrEmpty(moduleVersion) ? module.ModuleName : $"{module.ModuleName} {moduleVersion}");
                }
                lines.Sort(StringComparer.OrdinalIgnoreCase);
                return string.Join("\n", lines);
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }
    }
}
