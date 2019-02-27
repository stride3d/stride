// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Xenko.Core.Assets.Editor.Components.Transactions;
#if DEBUG
using System.Diagnostics;
#endif
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Extensions;
using Xenko.Core.Transactions;
using Xenko.Core.Windows;
using Xenko.Assets;
using Xenko.CrashReport;
using Xenko.Core.Presentation.Services;
using Xenko.Editor.CrashReport;
using Xenko.Graphics;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Xenko.GameStudio
{
    public static class CrashReportHelper
    {
        public class ReportSettings : ICrashEmailSetting
        {
            public ReportSettings()
            {
                Email = Xenko.Core.Assets.Editor.Settings.EditorSettings.StoreCrashEmail.GetValue();
                StoreCrashEmail = !string.IsNullOrEmpty(Email);
            }

            public bool StoreCrashEmail { get; set; }

            public string Email { get; set; }

            public void Save()
            {
                Xenko.Core.Assets.Editor.Settings.EditorSettings.StoreCrashEmail.SetValue(Email);
                Xenko.Core.Assets.Editor.Settings.EditorSettings.Save();
            }
        }

        public const int DebugVersion = 4;

        public static void SendReport(string exceptionMessage, int crashLocation, string[] logs, string threadName)
        {
            var crashReport = new CrashReportData
            {
                ["Application"] = "GameStudio",
                ["UserEmail"] = "",
                ["UserMessage"] = "",
                ["XenkoVersion"] = XenkoVersion.NuGetVersion,
                ["GameStudioVersion"] = DebugVersion.ToString(),
                ["ThreadName"] = string.IsNullOrEmpty(threadName) ? "" : threadName,
#if DEBUG
                ["CrashLocation"] = crashLocation.ToString(),
                ["ProcessID"] = Process.GetCurrentProcess().Id.ToString()
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
                var manager = SessionViewModel.Instance?.Dialogs?.AssetEditorsManager as AssetEditorsManager;
                if (manager != null)
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
                        var transactionsInProgressField = typeof(ITransactionStack).Assembly.GetType("Xenko.Core.Transactions.TransactionStack")?.GetField("transactionsInProgress", BindingFlags.Instance | BindingFlags.NonPublic);
                        if (transactionsInProgressField != null)
                        {
                            var stack = stackField.GetValue(actionService);
                            var transactionsInProgress = transactionsInProgressField.GetValue(stack) as IEnumerable<IReadOnlyTransaction>;
                            if (transactionsInProgress != null)
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
            var osVersion = CrashReportUtils.GetOsVersionAndCaption();
            crashReport["OsVersion"] = $"{osVersion.Key} {osVersion.Value} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}";
            crashReport["ProcessorCount"] = Environment.ProcessorCount.ToString();
            crashReport["Exception"] = exceptionMessage;
            var videoConfig = AppHelper.GetVideoConfig();
            foreach (var conf in videoConfig)
            {
                crashReport.Data.Add(Tuple.Create(conf.Key, conf.Value));
            }

            var nonFatalReport = new StringBuilder();
            for (var index = 0; index < logs.Length; index++)
            {
                var log = logs[index];
                nonFatalReport.AppendFormat($"{index + 1}: {log}\r\n");
            }

            crashReport["Log"] = nonFatalReport.ToString();

            // Try to anonymize reports
            // It also makes it easier to copy and paste paths
            for (var i = 0; i < crashReport.Data.Count; i++)
            {
                var data = crashReport.Data[i].Item2;

                data = Regex.Replace(data, Regex.Escape(Environment.GetEnvironmentVariable("USERPROFILE")), Regex.Escape("%USERPROFILE%"), RegexOptions.IgnoreCase);
                data = Regex.Replace(data, $@"\b{Regex.Escape(Environment.GetEnvironmentVariable("USERNAME"))}\b", Regex.Escape("%USERNAME%"), RegexOptions.IgnoreCase);

                crashReport.Data[i] = Tuple.Create(crashReport.Data[i].Item1, data);
            }

            var reporter = new CrashReportForm(crashReport, new ReportSettings());
            var result = reporter.ShowDialog();
            XenkoGameStudio.MetricsClient?.CrashedSession(result == DialogResult.Yes);
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
            var transaction = operation as IReadOnlyTransaction;
            if (transaction != null)
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
