// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Diagnostics;

namespace Stride.ConnectionRouter
{
    static class DeviceHelper
    {
        public static void UpdateDevices<T>(Logger log, Dictionary<T, string> enumeratedDevices, Dictionary<T, ConnectedDevice> currentDevices, Action<ConnectedDevice> connectDevice)
        {
            // Stop tasks used by disconnected devices
            foreach (var oldDevice in currentDevices.ToArray())
            {
                if (enumeratedDevices.ContainsKey(oldDevice.Key))
                    continue;

                oldDevice.Value.DeviceDisconnected = true;
                currentDevices.Remove(oldDevice.Key);

                log.Info($"Device removed: {oldDevice.Value.Name} ({oldDevice.Key})");
            }

            // Start new devices
            foreach (var androidDevice in enumeratedDevices)
            {
                if (currentDevices.ContainsKey(androidDevice.Key))
                    continue;

                var connectedDevice = new ConnectedDevice
                {
                    Key = androidDevice.Key,
                    Name = androidDevice.Value,
                };
                currentDevices.Add(androidDevice.Key, connectedDevice);

                connectDevice(connectedDevice);
            }
        }

        public static async Task LaunchPersistentClient(ConnectedDevice connectedDevice, Router router, string address, int localPort)
        {
            while (!connectedDevice.DeviceDisconnected)
            {
                try
                {
                    await router.TryConnect(address, localPort);
                }
                catch (Exception)
                {
                    // Mute exceptions and try to connect again
                    // TODO: Mute connection only, not message loop?
                }

                await Task.Delay(200);
            }
        }
    }
}
