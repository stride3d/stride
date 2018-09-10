// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using Xenko.Core.Extensions;
using Xenko.Core.Windows;
using Xenko.LauncherApp.Views;
using Xenko.CrashReport;
using Xenko.Editor.CrashReport;

namespace Xenko.LauncherApp.CrashReport
{
    public static class CrashReportHelper
    {
        private static bool terminating;

        public static void HandleException(Dispatcher dispatcher, Exception exception)
        {
            if (exception == null)
                return;

            //prevent multiple crash reports
            if (terminating)
                return;

            terminating = true;

            var englishCulture = new CultureInfo("en-US");
            var crashLogThread = new Thread(CrashReport) { CurrentUICulture = englishCulture, CurrentCulture = englishCulture };
            crashLogThread.Start(new CrashReportArgs { Dispatcher = dispatcher, Exception = exception });
            crashLogThread.Join();
        }

        [STAThread]
        private static void CrashReport(object data)
        {
            var args = (CrashReportArgs)data;

            args.Dispatcher?.InvokeAsync(() => Thread.CurrentThread.Join());

            SendReport(args.Exception.FormatFull());

            Environment.Exit(0);
        }

        private static void SendReport(string exceptionMessage)
        {
            var crashReport = new CrashReportData
            {
                ["Application"] = "Launcher",
                ["UserEmail"] = "",

                ["UserMessage"] = "",
                ["CurrentDirectory"] = Environment.CurrentDirectory,
                ["CommandArgs"] = string.Join(" ", AppHelper.GetCommandLineArgs()),
                ["OsVersion"] = $"{Environment.OSVersion} {(Environment.Is64BitOperatingSystem ? "x64" : "x86")}",
                ["ProcessorCount"] = Environment.ProcessorCount.ToString(),
                ["Exception"] = exceptionMessage
            };

            var videoConfig = AppHelper.GetVideoConfig();
            foreach (var conf in videoConfig)
            {
                crashReport.Data.Add(Tuple.Create(conf.Key, conf.Value));
            }

            var reporter = new CrashReportForm(crashReport, new CrashReportSettings());
            reporter.ShowDialog();
        }

        private class CrashReportArgs
        {
            public Exception Exception;
            public Dispatcher Dispatcher;
        }
    }
}
