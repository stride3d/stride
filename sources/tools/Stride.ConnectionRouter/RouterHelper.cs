// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Stride.Core.Assets;
using Stride.Core;
using Stride.Core.Packages;
using Stride.Engine.Network;
using Stride.Core.Diagnostics;

namespace Stride.ConnectionRouter
{
    public static class RouterHelper
    {
        private const string LaunchMutexName = @"Local\Stride.ConnectionRouter.Launch";

        private static string VersionWithoutSpecialPart(string version)
        {
            var indexOfDash = version.IndexOf('-');
            if (indexOfDash == -1)
                return version;

            return version.Substring(0, indexOfDash);
        }

        private static string VersionWithoutMetadata(string version)
        {
            var indexOfPlus = version?.IndexOf('+') ?? -1;
            return indexOfPlus != -1 ? version.Substring(0, indexOfPlus) : version;
        }

        public static bool EnsureRouterLaunched(bool attachChildJob = false, bool checkIfPortOpen = true)
        {
            try
            {
                // Make sure to use .exe rather than .dll (.NET Core)
                var defaultRouterAssemblyLocation = LoaderToolLocator.GetExecutable(typeof(Router).Assembly.Location);
                if (defaultRouterAssemblyLocation == null)
                {
                    throw new InvalidOperationException("Could not find Connection Router assembly location");
                }

                // Setup with default locations
                var routerAssemblyLocation = defaultRouterAssemblyLocation;
                var routerAssemblyExe = Path.GetFileName(routerAssemblyLocation);

                // Try to locate using Stride.ConnectionRouter package
                var logger = new LoggerResult();
                var package = PackageStore.Instance.FindLocalPackage("Stride.ConnectionRouter", new PackageVersionRange(new PackageVersion(StrideVersion.NuGetVersion)));
                var packageRouterLocation = package?.GetFiles().FirstOrDefault(x => string.Compare(Path.GetFileName(x.Path), routerAssemblyExe, true) == 0)?.FullPath;
                if (packageRouterLocation != null && File.Exists(packageRouterLocation))
                {
                    routerAssemblyLocation = packageRouterLocation;
                }
                var routerAssemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(routerAssemblyLocation);

                // A router found in its package runs from a copy of the package folder, so that builds and package refreshes can overwrite it.
                // The fallback location is the folder of the calling application, which is too big to copy: that one runs in place.
                var routerSource = routerAssemblyLocation == packageRouterLocation ? ShadowCopy.GetSource(routerAssemblyLocation) : null;

                // One caller at a time checks, copies and starts, so that the copy is never replaced while a router starts from it
                using var launchMutex = new Mutex(false, LaunchMutexName);
                bool launchMutexTaken;
                try
                {
                    launchMutexTaken = launchMutex.WaitOne(TimeSpan.FromSeconds(30));
                }
                catch (AbandonedMutexException)
                {
                    launchMutexTaken = true;
                }

                try
                {
                    return LaunchRouter(routerAssemblyLocation, routerAssemblyFileVersionInfo, routerSource, attachChildJob, checkIfPortOpen);
                }
                finally
                {
                    if (launchMutexTaken)
                        launchMutex.ReleaseMutex();
                }
            }
            catch
            {
                return false;
            }
        }

        private static bool LaunchRouter(string routerAssemblyLocation, FileVersionInfo routerAssemblyFileVersionInfo, string routerSource, bool attachChildJob, bool checkIfPortOpen)
        {
            // Try to connect to router
            FileVersionInfo runningRouterVersion = null;
            Process runningRouterProcess = null;
            string runningRouterPath = null;
            foreach (var process in Process.GetProcessesByName("Stride.ConnectionRouter"))
            {
                try
                {
                    runningRouterPath = process.MainModule.FileName;
                    runningRouterVersion = process.MainModule.FileVersionInfo;
                    runningRouterProcess = process;
                    break;
                }
                catch (Exception)
                {
                }
            }

            // If already started, check if found version is same that we wanted to start
            if (runningRouterVersion != null)
            {
                var wantedVersion = new PackageVersion(routerAssemblyFileVersionInfo.FileVersion);
                var runningVersion = new PackageVersion(runningRouterVersion.FileVersion);

                // Keep a newer router
                if (wantedVersion < runningVersion)
                    return true;

                // Keep a router of the same version, unless it is the same build (same -devN suffix) and doesn't run from a copy of the same files:
                // its files changed since (a rebuild), or it runs from the package folder and locks it
                if (wantedVersion == runningVersion)
                {
                    var sameBuild = VersionWithoutMetadata(routerAssemblyFileVersionInfo.ProductVersion) == VersionWithoutMetadata(runningRouterVersion.ProductVersion);
                    if (!sameBuild || routerSource == null || (ShadowCopy.IsInCopy(runningRouterPath) && ShadowCopy.IsCopyOf(routerSource)))
                        return true;
                }
            }

            // Kill previous router process (if any)
            if (runningRouterProcess != null)
            {
                runningRouterProcess.Kill();
                runningRouterProcess.WaitForExit();
            }

            // Copy once the previous router stopped, as it may run from the copy
            var routerExecutable = routerAssemblyLocation;
            if (routerSource != null)
            {
                try
                {
                    routerExecutable = ShadowCopy.GetOrCreateCopy(routerAssemblyLocation, routerSource);
                }
                catch (Exception e) when (e is IOException || e is UnauthorizedAccessException)
                {
                    // Run from the package folder
                }
            }

            // Start new router process
            var spawnedRouterProcess = Process.Start(routerExecutable);

            // If we are in "developer" mode, attach job so that it gets killed with editor
            if (attachChildJob && spawnedRouterProcess != null)
            {
                new AttachedChildProcessJob(spawnedRouterProcess);
            }

            if (checkIfPortOpen)
            {
                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    // Try during 5 seconds (10 * 500 msec)
                    for (int i = 0; i < 10; ++i)
                    {
                        try
                        {
                            socket.Connect("localhost", RouterClient.DefaultPort);
                        }
                        catch (SocketException)
                        {
                            // Try again in 500 msec
                            Thread.Sleep(500);
                            continue;
                        }
                        break;
                    }
                }
            }

            return spawnedRouterProcess != null;
        }

        public static void ParseUrl(string url, out string[] segments, out string parameters)
        {
            // Ideally we would like to reuse Uri (or some other similar code), but it doesn't work without a Host
            var parameterIndex = url.IndexOf('?');
            parameters = parameterIndex != -1 ? url.Substring(parameterIndex + 1) : string.Empty;

            var urlWithoutParameters = parameterIndex != -1 ? url.Substring(0, parameterIndex) : url;

            segments = urlWithoutParameters.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
        }

        public static NameValueCollection ParseQueryString(string query)
        {
            return System.Web.HttpUtility.ParseQueryString(query);
        }
    }
}
