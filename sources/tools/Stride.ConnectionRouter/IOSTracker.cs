// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Xenko.Core.Diagnostics;
using Xenko.Core.Extensions;
using Xenko.Engine.Network;

namespace Xenko.ConnectionRouter
{
    public class IosTracker
    {
        private static readonly Logger Log = GlobalLogger.GetLogger("IosTracker");

        private readonly Dictionary<string, ConnectedDevice> devices = new Dictionary<string, ConnectedDevice>();
        private readonly Dictionary<string, Process> proxies = new Dictionary<string, Process>();

        int startLocalPort = 51153; // Use ports in the dynamic port range

        private readonly Router router;

        public IosTracker(Router router)
        {
            this.router = router;
        }

        private static bool CheckAvailableServerPort(int port)
        {
            var ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            var tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();

            return tcpConnInfoArray.All(endpoint => endpoint.Port != port);
        }

        public static bool CanProxy()
        {
            var iosId = "iproxy.exe";
            return File.Exists(iosId);
        }

        internal Process SetupProxy(ConnectedDevice device)
        {
            var iosId = "iproxy.exe";

            int testedLocalPort;
            do
            {
                testedLocalPort = startLocalPort++;
                if (startLocalPort >= 65536) // Make sure we stay in the range of dynamic ports: 49152-65535
                    startLocalPort = 49152;
            } while (!CheckAvailableServerPort(testedLocalPort));

            Task.Run(async () =>
            {
                while (!device.DeviceDisconnected)
                {
                    try
                    {
                        await router.TryConnect("localhost", testedLocalPort);
                    }
                    catch (Exception)
                    {
                        // Mute exceptions and try to connect again
                        // TODO: Mute connection only, not message loop?
                    }

                    await Task.Delay(200);
                }
            });

            var process = new Process
            {
                StartInfo =
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        FileName = iosId,
                        Arguments = $"{testedLocalPort} {RouterClient.DefaultListenPort} {device.Name}"
                    }
            };
            process.Start();
            new AttachedChildProcessJob(process);

            Log.Info($"iOS Device connected: {device.Name}; successfully mapped port {testedLocalPort}:{RouterClient.DefaultListenPort}");

            return process;
        }

        public async Task TrackDevices()
        {
            var iosId = "idevice_id.exe";

            while (true)
            {
                var thisRunDevices = new List<string>();
                var process = new Process
                {
                    StartInfo =
                    {
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        FileName = iosId,
                        Arguments = "-l"
                    }
                };
                process.OutputDataReceived += (sender, args) =>
                {
                    if (string.IsNullOrEmpty(args.Data)) return;

                    thisRunDevices.Add(args.Data);
                    if (devices.ContainsKey(args.Data)) return;

                    Log.Info($"New iOS devices: {args.Data}");

                    var newDev = new ConnectedDevice
                    {
                        DeviceDisconnected = false,
                        Name = args.Data
                    };
                    proxies[args.Data] = SetupProxy(newDev);
                    devices[args.Data] = newDev;
                };
                process.Start();
                process.BeginOutputReadLine();
                process.WaitForExit();

                var toRemove = devices.Where(device => !thisRunDevices.Contains(device.Key)).ToList();
                foreach (var device in toRemove)
                {
                    device.Value.DeviceDisconnected = true;
                    proxies[device.Key].Kill();
                    proxies[device.Key].WaitForExit();
                    proxies.Remove(device.Key);

                    devices.Remove(device.Key);

                    Log.Info($"Disconnected iOS devices: {device}");
                }

                await Task.Delay(1000);
            }
        }
    }
}
