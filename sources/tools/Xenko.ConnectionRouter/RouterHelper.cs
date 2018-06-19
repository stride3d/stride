// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using Xenko.Core.Assets;
using Xenko.Core;
using Xenko.Core.Packages;
using Xenko.Engine.Network;

namespace Xenko.ConnectionRouter
{
    public static class RouterHelper
    {
        /// <summary>
        /// Gets the xenko SDK dir.
        /// </summary>
        /// <param name="xenkoVersion">The xenko version. If null, it will get latest version.</param>
        /// <returns></returns>
        public static string FindXenkoSdkDir(string xenkoVersion = null)
        {
            // TODO: Almost duplicate of XenkoCommandsProxy.FindXenkoSdkDir!!
            // TODO: Maybe move it in some common class somewhere? (in this case it would be included with "Add as link" in VSPackage)
            var xenkoSdkDir = DirectoryHelper.GetInstallationDirectory("Xenko");

            if (xenkoSdkDir == null)
            {
                xenkoSdkDir = Environment.GetEnvironmentVariable("XenkoDir");
            }

            if (xenkoSdkDir == null)
            {
                return null;
            }

            // Check if it is a dev directory
            if (DirectoryHelper.IsRootDevDirectory(xenkoSdkDir))
                return xenkoSdkDir;

            // Check if we are in a root directory with store/packages facilities
            var store = new NugetStore(xenkoSdkDir);

            var xenkoPackages = store.GetPackagesInstalled(store.MainPackageIds);
            // Convert the provided xenko version into a valid package version
            PackageVersion.TryParse(xenkoVersion, out var packageVersion);
            // Retrieve the corresponding package, if it exists
            var xenkoPackage = packageVersion != null
                ? (xenkoPackages.FirstOrDefault(p => p.Version == packageVersion)
                    ?? xenkoPackages.FirstOrDefault(p => p.Version.Version == packageVersion.Version)) // If no exact match, try a second time without the special version tag (beta, alpha, etc...)
                : xenkoPackages.FirstOrDefault();
            if (xenkoPackage == null)
                return null;

            var packageDirectory = store.GetInstalledPath(xenkoPackage.Id, xenkoPackage.Version);
            return packageDirectory;
        }

        private static string VersionWithoutSpecialPart(string version)
        {
            var indexOfDash = version.IndexOf('-');
            if (indexOfDash == -1)
                return version;

            return version.Substring(0, indexOfDash);
        }

        public static bool EnsureRouterLaunched(bool attachChildJob = false, bool checkIfPortOpen = true)
        {
            try
            {
                // Try to connect to router
                FileVersionInfo runningRouterVersion = null;
                Process runningRouterProcess = null;
                foreach (var process in Process.GetProcessesByName("Xenko.ConnectionRouter"))
                {
                    try
                    {
                        runningRouterVersion = process.MainModule.FileVersionInfo;
                        runningRouterProcess = process;
                        break;
                    }
                    catch (Exception)
                    {
                    }
                }

                var defaultRouterAssemblyLocation = typeof(Router).Assembly.Location;
                if (defaultRouterAssemblyLocation == null)
                {
                    throw new InvalidOperationException("Could not find Connection Router assembly location");
                }

                var routerAssemblyLocation = defaultRouterAssemblyLocation;
                var routerAssemblyExe = Path.GetFileName(routerAssemblyLocation);

                // Find latest xenko
                var xenkoSdkDir = FindXenkoSdkDir();
                if (xenkoSdkDir != null)
                {
                    // Try to find Connection Router in Xenko Sdk
                    routerAssemblyLocation = Path.Combine(xenkoSdkDir, @"Bin\Windows", routerAssemblyExe);

                    // Could not find anything, use router from current version instead of latest version
                    if (!File.Exists(routerAssemblyLocation))
                        routerAssemblyLocation = defaultRouterAssemblyLocation;
                }

                // If already started, check if found version is better
                if (runningRouterVersion != null)
                {
                    var routerAssemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(routerAssemblyLocation);

                    // Check that current router is at least as good as the one of latest found Xenko
                    if (new PackageVersion(routerAssemblyFileVersionInfo.FileVersion) <= new PackageVersion(runningRouterVersion.FileVersion))
                        return true;
                }

                // Kill previous router process (if any)
                if (runningRouterProcess != null)
                {
                    runningRouterProcess.Kill();
                    runningRouterProcess.WaitForExit();
                }

                // Start new router process
                var spawnedRouterProcess = Process.Start(routerAssemblyLocation);

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
            catch
            {
                return false;
            }
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
