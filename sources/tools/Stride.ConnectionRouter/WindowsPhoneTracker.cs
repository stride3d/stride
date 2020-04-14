// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.ServiceProcess;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32;
using Stride.Core.Diagnostics;
using Stride.Engine.Network;

namespace Stride.ConnectionRouter
{
    /// <summary>
    /// Track Windows Phone devices (with IpOverUsbEnum.exe) and establish port mapping.
    /// </summary>
    class WindowsPhoneTracker
    {
        private static string IpOverUsbStrideName = "StrideRouterServer";
        private static readonly Logger Log = GlobalLogger.GetLogger("WindowsPhoneTracker");

        public static void TrackDevices(Router router)
        {
            // Find AppDeployCmd.exe
            var programFilesX86 = Environment.GetEnvironmentVariable(Environment.Is64BitOperatingSystem ? "COMMONPROGRAMFILES(X86)" : "COMMONPROGRAMFILES");
            var ipOverUsbEnum = Path.Combine(programFilesX86, @"Microsoft Shared\Phone Tools\CoreCon\11.0\Bin\IpOverUsbEnum.exe");
            if (!File.Exists(ipOverUsbEnum))
            {
                return;
            }

            var portRegex = new Regex(string.Format(@"{0} (\d+) ->", IpOverUsbStrideName));
            var currentWinPhoneDevices = new Dictionary<int, ConnectedDevice>();

            bool checkIfPortMappingIsSetup = false;

            while (true)
            {
                ProcessOutputs devicesOutputs;
                try
                {
                    devicesOutputs = ShellHelper.RunProcessAndGetOutput(ipOverUsbEnum, "");
                }
                catch (Exception)
                {
                    continue;
                }

                if (devicesOutputs.ExitCode != 0)
                    continue;

                var newWinPhoneDevices = new Dictionary<int, string>();

                // First time a device is detected, we check port mapping is properly setup in registry
                var isThereAnyDevices = devicesOutputs.OutputLines.Any(x => x == "Partner:");
                if (isThereAnyDevices && !checkIfPortMappingIsSetup)
                {

                    using (var ipOverUsb = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IpOverUsb") ?? Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IpOverUsbSdk"))
                    {
                        if (ipOverUsb != null)
                        {
                            using (var ipOverUsbStride = ipOverUsb.OpenSubKey(IpOverUsbStrideName))
                            {
                                if (ipOverUsbStride == null)
                                {
                                    RegisterWindowsPhonePortMapping();
                                }
                            }
                        }
                    }

                    checkIfPortMappingIsSetup = true;
                }

                // Match forwarded ports
                foreach (var outputLine in devicesOutputs.OutputLines)
                {
                    int port;
                    var match = portRegex.Match(outputLine);
                    if (match.Success && Int32.TryParse(match.Groups[1].Value, out port))
                    {
                        newWinPhoneDevices.Add(port, "Device");
                    }
                }

                DeviceHelper.UpdateDevices(Log, newWinPhoneDevices, currentWinPhoneDevices, (connectedDevice) =>
                {
                    // Launch a client thread that will automatically tries to connect to this port
                    var localPort = (int)connectedDevice.Key;

                    Log.Info($"Device connected: {connectedDevice.Name}; mapped port {localPort}");

                    Task.Run(() => DeviceHelper.LaunchPersistentClient(connectedDevice, router, "localhost", localPort));
                });

                Thread.Sleep(1000); // Detect new devices every 1000 msec
            }
        }

        public static void RegisterWindowsPhonePortMapping()
        {
            if (!IsElevated)
            {
                Log.Info("Not enough permissions to install Windows Phone IpOverUsb port mapping, relaunching as administrator...");

                // No entry for effet compiler, let's create it
                var info = new ProcessStartInfo
                {
                    FileName = Assembly.GetExecutingAssembly().Location,
                    UseShellExecute = true,
                    Verb = "runas",
                    Arguments = "--register-windowsphone-portmapping"
                };
                var process = Process.Start(info);
                process.WaitForExit();
                return;
            }

            Log.Info("Installing Windows Phone IpOverUsb port mapping");

            // Add Windows Phone port mapping to registry
            using (var ipOverUsb = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IpOverUsb", true) ?? Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\IpOverUsbSdk", true))
            {
                if (ipOverUsb == null)
                {
                    Log.Error("There is no IpOverUsb in registry. Is Windows Phone SDK properly installed?");
                    return;
                }
                using (var ipOverUsbStride = ipOverUsb.CreateSubKey(IpOverUsbStrideName))
                {
                    ipOverUsbStride.SetValue("LocalAddress", "127.0.0.1");
                    ipOverUsbStride.SetValue("LocalPort", 40153);
                    ipOverUsbStride.SetValue("DestinationAddress", "127.0.0.1");
                    ipOverUsbStride.SetValue("DestinationPort", RouterClient.DefaultListenPort);
                }
            }

            // Restart Windows Phone IP over USB service (IpOverUsbSvc)
            RestartService(Log, "IpOverUsbSvc", 4000);
        }

        private static bool IsElevated
        {
            get
            {
                return new WindowsPrincipal
                    (WindowsIdentity.GetCurrent()).IsInRole
                    (WindowsBuiltInRole.Administrator);
            }
        }

        private static void RestartService(Logger log, string serviceName, int timeout)
        {
            var serviceController = new ServiceController(serviceName);
            try
            {
                serviceController.Stop();
                serviceController.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromMilliseconds(timeout));

                serviceController.Start();
                serviceController.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromMilliseconds(timeout));
            }
            catch
            {
                log.Error($"Error restarting {serviceName} service");
            }
        }
    }
}
