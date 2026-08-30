// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Stride.Core.Assets;
using Stride.Core.BuildEngine;
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
        private CrashRun run; // created on the first capture, so a clean build leaves no empty run directory

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

        /// <summary>False when crash handling is turned off entirely; the build wires the hook only when true.</summary>
        public bool Enabled => mode != CrashMode.Off;

        /// <summary>The run captured crashes were written to, or null when nothing crashed.</summary>
        public CrashRun Run => run;

        /// <summary>Removes crash runs older than the retention window, so the store never grows unbounded.</summary>
        public void PruneOld() => store.Prune(TimeSpan.FromDays(30));

        /// <summary>
        /// Records a command failure. Called from build worker threads, possibly concurrently, so the
        /// read-modify-write of the store is serialized.
        /// </summary>
        public void Capture(CommandBuildStep step, Exception exception)
        {
            if (mode == CrashMode.Off)
                return;
            // Denylist is control-flow only: a cancelled build is not a crash. TaskCanceledException derives from it.
            if (exception is OperationCanceledException)
                return;

            var stepKind = step.Command?.GetType().Name ?? "UnknownCommand";
            var signature = CrashSignature.Compute(exception, stepKind);

            lock (gate)
            {
                if (store.IsSuppressed(signature, version))
                    return;
                run ??= store.CreateRun();
                run.Add(BuildCrash(step, exception, stepKind, signature));
            }
        }

        private StoredCrash BuildCrash(CommandBuildStep step, Exception exception, string stepKind, string signature)
        {
            var asset = step.Tag as AssetItem;
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
            // Mask user name and profile path in the report text before it is stored.
            CrashReportAnonymizer.Scrub(data);

            var crash = StoredCrash.FromReportData(data);
            crash.Application = ApplicationName;
            crash.Version = version;
            crash.Environment = environment;
            crash.TimestampUtc = DateTime.UtcNow.ToString("o");
            crash.Signature = signature;
            if (assetLabel != null)
                crash.AffectedAssets.Add(assetLabel);
            // Local-only real paths, so the reporter can offer to attach the asset later. Never sent as text.
            crash.AssetSourcePaths.AddRange(CollectSourcePaths(step, asset));
            return crash;
        }

        private static IEnumerable<string> CollectSourcePaths(CommandBuildStep step, AssetItem asset)
        {
            var paths = new List<string>();
            var assetPath = asset?.FullPath;
            if (assetPath != null)
                paths.Add(assetPath.ToOSPath());

            try
            {
                foreach (var input in step.Command?.GetInputFiles() ?? Enumerable.Empty<ObjectUrl>())
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
