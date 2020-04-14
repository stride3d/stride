// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Extensions;

namespace Xenko.LauncherApp.Services
{
    internal static class MetricsHelper
    {
        public static void NotifyDownloadStarting(string packageName, string packageVersion) => NotifyDownloadEvent(packageName, packageVersion, "DownloadStarting");

        public static void NotifyDownloadFailed(string packageName, string packageVersion) => NotifyDownloadEvent(packageName, packageVersion, "DownloadFailed");

        public static void NotifyDownloadCompleted(string packageName, string packageVersion) => NotifyDownloadEvent(packageName, packageVersion, "DownloadCompleted");

        private static void NotifyDownloadEvent(string packageName, string packageVersion, string downloadEvent)
        {
            try
            {
                var downloadInfo = BuildMessage(DateTime.UtcNow, downloadEvent, packageName, packageVersion);
                Launcher.Metrics?.DownloadPackage(downloadInfo);
            }
            catch (Exception e)
            {
                e.Ignore();
            }
        }

        private static string BuildMessage(DateTime dateTime, string downloadEvent, string packageName, string packageVersion)
        {
            var timestamp = (long)(dateTime - new DateTime(1970, 1, 1)).TotalSeconds;
            return $"{Escape(packageName)}||{Escape(packageVersion)}||{downloadEvent}||{timestamp}";
        }

        private static string Escape(string s) => s.Replace("|", @"\|");
    }
}
