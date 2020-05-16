// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
                foreach (var process in Process.GetProcessesByName("Stride.ConnectionRouter"))
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

                // Make sure to use .exe rather than .dll (.NET Core)
                var defaultRouterAssemblyLocation = Path.ChangeExtension(typeof(Router).Assembly.Location, ".exe");
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
                if (package != null)
                {
                    routerAssemblyLocation = package.GetFiles().FirstOrDefault(x => string.Compare(Path.GetFileName(x.Path), routerAssemblyExe, true) == 0)?.FullPath ?? routerAssemblyLocation;
                }

                // If already started, check if found version is same that we wanted to start
                if (runningRouterVersion != null)
                {
                    var routerAssemblyFileVersionInfo = FileVersionInfo.GetVersionInfo(routerAssemblyLocation);

                    // Check that current router is at least as good as the one of latest found Stride
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
