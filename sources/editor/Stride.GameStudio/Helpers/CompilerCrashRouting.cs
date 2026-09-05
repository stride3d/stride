// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Stride.Core.Diagnostics;
using Stride.CrashReport;

namespace Stride.GameStudio.Helpers
{
    /// <summary>
    /// Routes asset-compiler crashes from a build this GameStudio triggered back to this GameStudio, so it can
    /// surface them. The headless compiler no longer pops its own reporter; instead the build carries a
    /// per-instance crash directory (passed to the compiler via the <c>StrideCrashDir</c> MSBuild property, which
    /// the compiler targets turn into <c>STRIDE_CRASH_DIR</c> on the compiler process only), and after the build
    /// this GameStudio hands any crash that landed there to the out-of-process reporter.
    /// </summary>
    /// <remarks>
    /// Routing is by process inheritance (the compiler inherits the dir this instance chose for its own builds),
    /// so two GameStudios — even on the same project — each surface only their own builds' crashes, with no
    /// single-instance assumption. Only the interactive case routes; otherwise the compiler falls back to the
    /// default store and <c>stride crash send</c>.
    /// </remarks>
    public static class CompilerCrashRouting
    {
        private static readonly string RootFolder = Path.Combine("stride", "crash-reports");
        private static readonly object gate = new();
        private static readonly HashSet<string> surfaced = new(StringComparer.OrdinalIgnoreCase);
        private static string directory;
        private static bool pruned;
        private static IntPtr ownerWindow;

        /// <summary>
        /// Records the main window so a surfaced compiler crash opens as a window owned by it: above GameStudio,
        /// never above other apps, minimized with it, and not modal. Call once the window is shown (UI thread).
        /// </summary>
        public static void SetOwnerWindow(System.Windows.Window window)
        {
            try { ownerWindow = new System.Windows.Interop.WindowInteropHelper(window).Handle; }
            catch { /* no owner: the reporter opens as a plain top-level window */ }
        }

        /// <summary>
        /// The per-instance directory the compiler routes this GameStudio's build crashes into, or null when
        /// crashes should not be surfaced in-editor (save/send/off/unattended) — then the compiler uses the
        /// default store. Passed to the build as the <c>StrideCrashDir</c> property.
        /// </summary>
        public static string CrashDirectory
        {
            get
            {
                if (CrashPolicy.ResolveAction() != CrashAction.Report)
                    return null;
                lock (gate)
                {
                    PruneOldSessions();
                    return directory ??= CreateDirectory();
                }
            }
        }

        private static string CreateDirectory()
        {
            try
            {
                var dir = Path.Combine(Path.GetTempPath(), RootFolder, Environment.ProcessId.ToString(CultureInfo.InvariantCulture));
                Directory.CreateDirectory(dir);
                return dir;
            }
            catch
            {
                return null; // Can't create it (permissions, etc.); routing just stays off.
            }
        }

        /// <summary>Drop crash-routing dirs left by previous sessions. Called at startup so cleanup doesn't wait for a build.</summary>
        public static void PruneStaleSessions()
        {
            lock (gate)
                PruneOldSessions();
        }

        // Drop a finished session's dir (no run-* left) once its GameStudio is gone; otherwise prune by age — a dir
        // still holding a run-* may have a reporter open on it, and reporters can outlive their GameStudio.
        private static void PruneOldSessions()
        {
            if (pruned)
                return;
            pruned = true;
            try
            {
                var root = Path.Combine(Path.GetTempPath(), RootFolder);
                if (!Directory.Exists(root))
                    return;
                var cutoff = DateTime.UtcNow - TimeSpan.FromDays(3);
                var alivePids = GetAlivePids();
                foreach (var dir in Directory.GetDirectories(root))
                {
                    try
                    {
                        if (Directory.GetLastWriteTimeUtc(dir) < cutoff || (IsFinished(dir) && !IsOwnerAlive(dir, alivePids)))
                            Directory.Delete(dir, recursive: true);
                    }
                    catch
                    {
                        // A live sibling session's dir or a locked one — skip it.
                    }
                }
            }
            catch
            {
                // Best effort.
            }
        }

        // No run-* dirs left: every crash was handled, so no reporter is open on it — safe to drop.
        private static bool IsFinished(string sessionDir)
        {
            try { return !Directory.EnumerateDirectories(sessionDir, "run-*", SearchOption.AllDirectories).Any(); }
            catch { return false; }
        }

        // Running process ids, taken once so the owner check is a set lookup rather than a per-dir
        // Process.GetProcessById that throws for every dead session (first-chance noise under a debugger). Null on failure.
        private static HashSet<int> GetAlivePids()
        {
            System.Diagnostics.Process[] processes = null;
            try
            {
                processes = System.Diagnostics.Process.GetProcesses();
                return processes.Select(p => p.Id).ToHashSet();
            }
            catch
            {
                return null;
            }
            finally
            {
                if (processes != null)
                    foreach (var p in processes)
                        p.Dispose();
            }
        }

        // Is the owning GameStudio (dir named by its pid) still running? Unknown name or no snapshot counts as alive.
        private static bool IsOwnerAlive(string sessionDir, HashSet<int> alivePids)
        {
            if (!int.TryParse(Path.GetFileName(sessionDir), out var pid))
                return true;
            return pid == Environment.ProcessId || alivePids == null || alivePids.Contains(pid);
        }

        /// <summary>
        /// After a build, hands any asset-compiler crash it produced to the out-of-process reporter (the healthy
        /// surface the headless compiler defers to). No-op when nothing crashed or routing is off. If the reporter
        /// can't be resolved, the crash stays on disk and a hint points at <c>stride crash send</c>.
        /// </summary>
        public static void SurfacePendingCrashes(ILogger logger)
        {
            string dir;
            lock (gate)
                dir = directory;
            if (dir == null)
                return;

            try
            {
                var appDirectory = Path.Combine(dir, "assetcompiler");
                if (!Directory.Exists(appDirectory))
                    return;

                foreach (var run in Directory.GetDirectories(appDirectory, "run-*"))
                {
                    if (!Directory.EnumerateFiles(run, "crash-*.json").Any())
                        continue;
                    lock (gate)
                    {
                        if (!surfaced.Add(run))
                            continue; // already handed to a reporter by an earlier build in this session
                    }
                    if (!NativeCrashReporting.TrySpawnReporter(run, ownerWindow))
                        logger.Warning($"The asset build crashed; the report was saved to {run} (submit it with 'stride crash send').");
                }
            }
            catch (Exception e)
            {
                logger.Warning($"Could not surface an asset-build crash: {e.Message}");
            }
        }
    }
}
