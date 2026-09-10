// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Stride.Core.Assets.Editor.Components.Transactions;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Extensions;
using Stride.Core.Transactions;
using Stride.Core.Windows;
using Stride.Assets;
using Stride.Core.Presentation.Services;
using Stride.CrashReport;
using Stride.Graphics;
using Stride.GameStudio.AssetsEditors;
using Stride.Core.Assets.Editor.Services;

namespace Stride.GameStudio.Helpers
{
    public static class CrashReportHelper
    {
        private const int DebugVersion = 4;

        public static void SendReport(Exception exception, int crashLocation, string[] logs, string threadName,
            int threadId, System.Collections.Generic.IReadOnlyList<StoredThread> threads)
        {
            var crashReport = new CrashReportData
            {
                ["Application"] = "GameStudio",
                ["StrideVersion"] = StrideVersion.NuGetVersion,
                ["GameStudioVersion"] = DebugVersion.ToString(),
                ["ThreadName"] = string.IsNullOrEmpty(threadName) ? "" : threadName,
#if DEBUG
                ["CrashLocation"] = crashLocation.ToString(),
                ["ProcessID"] = Environment.ProcessId.ToString()
#endif
            };

            try
            {
                // Add session-specific information in this try/catch block
                var gameSettingsAsset = SessionViewModel.Instance?.CurrentProject?.Package.GetGameSettingsAsset();
                if (gameSettingsAsset != null)
                {
                    crashReport["DefaultGraphicProfile"] = gameSettingsAsset.GetOrCreate<RenderingSettings>().DefaultGraphicsProfile.ToString();
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            // opened assets
            try
            {
                if (SessionViewModel.Instance?.ServiceProvider.TryGet<IAssetEditorsManager>() is AssetEditorsManager manager)
                {
                    var sb = new StringBuilder();
                    foreach (var asset in manager.GetCurrentlyOpenedAssets())
                    {
                        sb.AppendLine($"{asset.Id}:{asset.Name} ({asset.TypeDisplayName})");
                    }
                    crashReport["OpenedAssets"] = sb.ToString();
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            // action history
            try
            {
                // Add session-specific information in this try/catch block
                var actionsViewModel = SessionViewModel.Instance?.ActionHistory;
                if (actionsViewModel != null)
                {
                    var actions = actionsViewModel.Transactions.ToList();
                    var sb = new StringBuilder();
                    for (var i = Math.Max(0, actions.Count - 5); i < actions.Count; ++i)
                    {
                        ExpandAction(actions[i], sb, 4);
                    }
                    crashReport["LastActions"] = sb.ToString();
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            // transaction in progress
            try
            {
                // Add session-specific information in this try/catch block
                var actionService = SessionViewModel.Instance?.UndoRedoService;
                if (actionService != null && actionService.TransactionInProgress)
                {
                    // FIXME: expose some readonly properties/methods from ITransactionStack or ITransaction to reduce reflection
                    var stackField = typeof(UndoRedoService).GetField("stack", BindingFlags.Instance | BindingFlags.NonPublic);
                    if (stackField != null)
                    {
                        var transactionsInProgressField = typeof(ITransactionStack).Assembly.GetType("Stride.Core.Transactions.TransactionStack")?.GetField("transactionsInProgress", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (transactionsInProgressField != null)
                        {
                            var stack = stackField.GetValue(actionService);
                            if (transactionsInProgressField.GetValue(stack) is IEnumerable<IReadOnlyTransaction> transactionsInProgress)
                            {
                                var sb = new StringBuilder();
                                sb.AppendLine("Transactions in progress:");
                                foreach (var transaction in transactionsInProgress)
                                {
                                    PrintTransaction(transaction, sb, 4);
                                }
                                crashReport["TransactionInProgress"] = sb.ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            crashReport["CurrentDirectory"] = Environment.CurrentDirectory;
            crashReport["CommandArgs"] = string.Join(" ", AppHelper.GetCommandLineArgs());
            crashReport["OsVersion"] = $"{System.Runtime.InteropServices.RuntimeInformation.OSDescription} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}";
            crashReport["Cpu"] = AppHelper.GetCpuName();
            crashReport["ProcessorCount"] = Environment.ProcessorCount.ToString();
            crashReport["Exception"] = exception.FormatFull();

            try
            {
                crashReport["GraphicsPlatform"] = GraphicsDevice.Platform.ToString();
                crashReport["GraphicsAdapter"] = GraphicsAdapterFactory.DefaultAdapter?.Description;
            }
            catch (Exception e)
            {
                e.Ignore();
            }

            var videoConfig = AppHelper.GetVideoConfig();
            foreach (var conf in videoConfig)
            {
                crashReport.Data.Add((conf.Key, conf.Value));
            }

            foreach (var info in AppHelper.GetMemoryInfo())
            {
                crashReport.Data.Add((info.Key, info.Value));
            }

            var nonFatalReport = new StringBuilder();
            for (var index = 0; index < logs.Length; index++)
            {
                var log = logs[index];
                nonFatalReport.AppendFormat($"{index + 1}: {log}\r\n");
            }

            crashReport["Log"] = nonFatalReport.ToString();

            CrashReportAnonymizer.Scrub(crashReport);

            // Unattended sessions (CI editor tests, remote/service sessions) must not block on a dialog;
            // STRIDE_CRASH_MODE=save/send/off are explicit overrides. Only the interactive case shows a window.
            switch (CrashPolicy.ResolveAction())
            {
                case CrashAction.Ignore:
                    return;
                case CrashAction.Save:
                    SaveToStore(crashReport, exception, threads, threadId, threadName);
                    return;
                case CrashAction.Send:
                    try
                    {
                        if (CrashReportSender.IsDisabled)
                            throw new InvalidOperationException("Crash sending is disabled in this build.");
                        CrashReportSender.SendAsync(crashReport, "GameStudio", exception, CrashReportSender.ResolveDsn(),
                            threads: threads, crashedThreadId: threadId, crashedThreadName: threadName).GetAwaiter().GetResult();
                    }
                    catch (Exception e)
                    {
                        e.Ignore();
                        SaveToStore(crashReport, exception, threads, threadId, threadName); // a failed send must not lose the report
                    }
                    return;
            }

            // Attended: the same out-of-process reporter the headless tools use. We are still alive here (the runtime
            // waits for this handler), so the report and a dump go to the store, the reporter is spawned with our pid,
            // and we block until it closes -- the freeze an in-process modal dialog gave, and what lets the reporter
            // write a full memory dump of this live process on demand. No reporter: the run stays for 'stride crash send'.
            var run = SaveToStore(crashReport, exception, threads, threadId, threadName);
            if (run == null)
                return;
            try
            {
                using var reporter = NativeCrashReporting.TrySpawnHostCrashReporter(run.Directory);
                reporter?.WaitForExit();
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        /// <summary>
        /// Persists the report to the crash store with the structured exception, the thread snapshot and a dump of
        /// this process: a scrubbed triage dump (stacks and modules, sendable) or, under STRIDE_CRASH_DUMP=full, a
        /// full-memory one (unscrubbed, kept local). Returns the run, or null when the store could not be written.
        /// </summary>
        private static CrashRun SaveToStore(CrashReportData report, Exception exception,
            IReadOnlyList<StoredThread> threads, int threadId, string threadName)
        {
            try
            {
                var crash = StoredCrash.FromReportData(report);
                crash.Application = "GameStudio";
                crash.Version = StrideVersion.NuGetVersion;
                crash.Environment = CrashReportSender.BuildEnvironment ?? "local";
                crash.TimestampUtc = DateTime.UtcNow.ToString("o");
                crash.Signature = CrashSignature.Compute(exception, "GameStudio");
                crash.Exceptions = StoredException.Capture(exception);
                crash.Threads = threads?.ToList() ?? new List<StoredThread>();
                crash.CrashedThreadId = threadId;
                crash.CrashedThreadName = threadName;

                var run = new CrashStore("GameStudio").CreateRun();
                if (CrashPolicy.FullMemoryDump())
                {
                    crash.DumpIsFullMemory = true;
                    run.Add(crash, path => MinidumpWriter.TryWriteFile(path, fullMemory: true));
                }
                else
                {
                    run.Add(crash, MinidumpWriter.TryWrite());
                }
                return run;
            }
            catch (Exception e)
            {
                e.Ignore(); // saving the report must never mask the crash handling itself
                return null;
            }
        }

        private static void ExpandAction(TransactionViewModel actionItem, StringBuilder sb, int increment)
        {
            sb.AppendLine($"* {(actionItem.IsDone ? "+" : "-")}[{actionItem.Name}]");

            var memberInfo = typeof(TransactionViewModel).GetField("transaction", BindingFlags.Instance | BindingFlags.NonPublic);
            if (memberInfo != null)
            {
                var transaction = (IReadOnlyTransaction)memberInfo.GetValue(actionItem);
                PrintTransaction(transaction, sb, increment);
            }
        }

        private static void PrintOperation(Operation operation, StringBuilder stringBuilder, int increment, int offset)
        {
            if (operation is IReadOnlyTransaction transaction)
            {
                PrintTransaction(transaction, stringBuilder, increment, offset);
                return;
            }

            stringBuilder.Append("".PadLeft(offset) + "*");
            stringBuilder.AppendLine($" {operation}");
        }

        private static void PrintTransaction(IReadOnlyTransaction transaction, StringBuilder stringBuilder, int increment, int offset = 0)
        {
            foreach (var operation in transaction.Operations)
            {
                PrintOperation(operation, stringBuilder, increment, offset + increment);
            }
        }
    }
}
