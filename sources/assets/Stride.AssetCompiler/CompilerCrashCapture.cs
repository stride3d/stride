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
        // One run per process, shared by all master captures (per-command hook + top-level handler) so a process's
        // crashes surface as a single reporter. Slaves use the master's run (CrashRunDirectory), not this.
        private static CrashRun processRun;
        private static readonly object processRunGate = new();

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

        /// <summary>
        /// On Windows, arms a record-only vectored handler that writes each native crash's faulting frame beside the
        /// dump <c>createdump</c> will write, so the post-build adopt step can name the fault location -- Windows
        /// <c>createdump</c> omits the exception stream (dotnet/runtime#133065). No-op when crash reporting is off,
        /// off Windows (the exception stream is present there), or when <c>createdump</c> isn't armed for this process.
        /// Call once per process (master and each slave) before commands run.
        /// </summary>
        public void InstallNativeFaultRecorder()
        {
            if (mode == CrashMode.Off || !OperatingSystem.IsWindows())
                return;
            var framePath = FaultingFramePathForThisProcess();
            if (framePath != null)
                Stride.NativeCrashHandler.InstallFaultingFrameRecorder(framePath);
        }

        /// <summary>The run captured crashes are written to, created on first use. Shared with the build's slaves.</summary>
        public CrashRun EnsureRun()
        {
            if (run != null)
                return run; // a slave's shared run, or a master run already resolved
            lock (processRunGate)
                return run = processRun ??= store.CreateRun();
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
                    // The native fault frame (module+rva): from the dump's exception stream on Linux/macOS, or
                    // recorded live by the vectored handler on Windows (createdump writes no exception stream there).
                    var faultingFrame = NativeCrashReporting.FaultingFrameFromDump(dumpPath)
                                        ?? ReadRecordedFaultingFrame(dumpPath);

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
                    // Walk the dump for the crashing thread's managed stack (and the others'), so a native asset
                    // crash reports like a managed one instead of a bare message. createdump dumps carry the CLR
                    // memory this needs; best-effort, so a dump that can't be walked still sends the message.
                    DumpStackWalk.EnrichFromDump(crash, dumpPath, faultingFrame);

                    // Dedup key: prefer the crashing thread's managed fault site (stable and readable, and the only
                    // key available on Windows where the dump has no faulting frame), else the native fault frame,
                    // else a per-dump fallback that never groups. Set before the suppression check so both agree.
                    var faultSite = crash.Exceptions.FirstOrDefault()?.Frames.FirstOrDefault(f => !string.IsNullOrEmpty(f.Module))?.Function;
                    crash.Signature = NativeCrashReporting.NativeSignature(faultSite ?? faultingFrame, dumpPath);
                    if (store.IsSuppressed(crash.Signature, version))
                    {
                        TryDelete(dumpPath);
                        TryDelete(dumpPath + ".frame");
                        continue;
                    }

                    EnsureRun().Add(crash, File.ReadAllBytes(dumpPath));
                    TryDelete(dumpPath);
                    TryDelete(dumpPath + ".frame");
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

        // The faulting frame the vectored handler recorded beside a Windows createdump dump ("<dump>.frame"),
        // or null when there is none (a clean build, off Windows, or the handler didn't run). See InstallNativeFaultRecorder.
        private static string ReadRecordedFaultingFrame(string dumpPath)
        {
            try
            {
                var framePath = dumpPath + ".frame";
                if (File.Exists(framePath))
                {
                    var frame = File.ReadAllText(framePath).Trim();
                    return string.IsNullOrEmpty(frame) ? null : frame;
                }
            }
            catch { /* best effort */ }
            return null;
        }

        // The ".frame" sidecar the vectored handler writes for this process: DOTNET_DbgMiniDumpName with createdump's
        // %p (pid) substituted (and the quotes it carries for spaced paths trimmed), so it sits next to the dump.
        // Null when createdump isn't armed for this process.
        private static string FaultingFramePathForThisProcess()
        {
            var pattern = Environment.GetEnvironmentVariable("DOTNET_DbgMiniDumpName");
            if (string.IsNullOrEmpty(pattern))
                return null;
            return pattern.Trim('"').Replace("%p", Environment.ProcessId.ToString()) + ".frame";
        }

        /// <summary>Records and handles a crash that escaped the whole build (not a single command), so it is reported
        /// like a per-command one. Best-effort.</summary>
        public static void CaptureTopLevel(PackageBuilderOptions options, Exception exception,
            IReadOnlyList<StoredThread> threads = null, int? crashedThreadId = null, string crashedThreadName = null)
        {
            try
            {
                var capture = new CompilerCrashCapture(options);
                if (!capture.Enabled)
                    return;
                capture.Capture(command: null, asset: null, exception, stepKindOverride: "TopLevel",
                    threads, crashedThreadId, crashedThreadName);
                capture.HandleRun(options.Logger);
            }
            catch
            {
                // A failure while reporting the crash must not mask it.
            }
        }

        /// <summary>Headless end-of-run handling: send on CI, else leave the run for GameStudio / <c>stride crash send</c>.</summary>
        public void HandleRun(ILogger logger)
        {
            if (run == null)
                return;
            if (run.IsEmpty)
            {
                run.Delete();
                return;
            }
            switch (CrashPolicy.ResolveAction())
            {
                case CrashAction.Send:
                    SendRun(run, logger);
                    break;
                case CrashAction.Report:
                    logger.Warning($"{run.Read().Count} asset-build crash(es) saved to {run.Directory}. Review and submit them with 'stride crash send'.");
                    break;
                // Save / Ignore: leave the run on disk for a later 'stride crash send'.
            }
            PruneOld();
        }

        private void Capture(Command command, AssetItem asset, Exception exception, string stepKindOverride = null,
            IReadOnlyList<StoredThread> threads = null, int? crashedThreadId = null, string crashedThreadName = null)
        {
            if (mode == CrashMode.Off)
                return;
            // Denylist is control-flow only: a cancelled build is not a crash. TaskCanceledException derives from it.
            if (exception is OperationCanceledException)
                return;

            var stepKind = stepKindOverride ?? command?.GetType().Name ?? "UnknownCommand";
            var signature = CrashSignature.Compute(exception, stepKind);

            lock (gate)
            {
                // Suppression is a master concept (the user chose it at the reporter); a slave writes unconditionally
                // and the master's own suppression already skips its local occurrences of the same signature.
                if (isMaster && store.IsSuppressed(signature, version))
                    return;
                EnsureRun();
                var crash = BuildCrash(command, asset, exception, stepKind, signature);
                if (threads != null)
                    crash.Threads = threads.ToList();
                crash.CrashedThreadId = crashedThreadId;
                crash.CrashedThreadName = crashedThreadName;
                run.Add(crash);
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
