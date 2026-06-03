// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using SharpDX;
using SharpDX.DirectInput;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for various game controllers on windows
    /// </summary>
    internal class InputSourceWindowsDirectInput : InputSourceBase
    {
        private readonly HashSet<Guid> devicesToRemove = new HashSet<Guid>();
        private InputManager inputManager;
        private DirectInput directInput;
        private IEnumerable<string> xInputDevices;
        private Regex xInputDeviceIdRegex;

        public override void Initialize(InputManager inputManager)
        {
            this.inputManager = inputManager;
            directInput = new DirectInput();
            xInputDeviceIdRegex = new Regex(@"VID_(\w+)?&PID_(\w+)?");

            Scan();
        }

        public override void Dispose()
        {
            // Dispose all the gamepads
            foreach (var pair in Devices)
            {
                var gameController = pair.Value as GameControllerDirectInput;
                gameController?.Dispose();
            }

            // Unregisters all devices
            base.Dispose();

            // Dispose DirectInput
            directInput.Dispose();
        }

        public override void Update()
        {
            // Process device removals
            foreach (var deviceIdToRemove in devicesToRemove)
            {
                var gameController = Devices[deviceIdToRemove];
                UnregisterDevice(gameController);
                (gameController as IDisposable)?.Dispose();
            }
            devicesToRemove.Clear();
        }

        /// <summary>
        /// Select all device IDs that contain "IG_". If so, it's an XInput device.
        /// This information cannot be found from DirectInput.
        /// </summary>
        private IEnumerable<string> GetAllXInputDevices()
        {
            // An XInput device's raw-input name carries the "&IG_" marker; pull the VID/PID out of any
            // such name to match against DirectInput's ProductGuid prefix. Uses the Raw Input API
            // (user32) rather than WMI/Microsoft.Management.Infrastructure, which isn't AOT/trim-safe.
            return GetRawInputDeviceNames()
                .Where(name => name.Contains("&IG_", StringComparison.OrdinalIgnoreCase))
                .Select(name => xInputDeviceIdRegex.Match(name))
                .Where(match => match.Success)
                .Select(match => (match.Groups[2].Value + match.Groups[1].Value).ToLowerInvariant())
                .Distinct()
                .ToList();
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RawInputDeviceList
        {
            public IntPtr Device;
            public uint Type;
        }

        private const uint RidiDeviceName = 0x20000007;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern uint GetRawInputDeviceList([Out] RawInputDeviceList[] rawInputDeviceList, ref uint numDevices, uint size);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode, EntryPoint = "GetRawInputDeviceInfoW")]
        private static extern uint GetRawInputDeviceInfo(IntPtr device, uint command, IntPtr data, ref uint dataCharCount);

        /// <summary>Enumerates the device name string of every raw-input device.</summary>
        private static List<string> GetRawInputDeviceNames()
        {
            var names = new List<string>();
            uint deviceCount = 0;
            uint listEntrySize = (uint)Marshal.SizeOf<RawInputDeviceList>();

            // First call with a null list just fills in the device count.
            GetRawInputDeviceList(null, ref deviceCount, listEntrySize);
            if (deviceCount == 0)
                return names;

            var devices = new RawInputDeviceList[deviceCount];
            if (GetRawInputDeviceList(devices, ref deviceCount, listEntrySize) == unchecked((uint)-1))
                return names;

            foreach (var device in devices)
            {
                uint charCount = 0;
                // First call queries the buffer size (in characters) for the device name.
                if (GetRawInputDeviceInfo(device.Device, RidiDeviceName, IntPtr.Zero, ref charCount) != 0 || charCount == 0)
                    continue;

                var buffer = Marshal.AllocHGlobal((int)charCount * sizeof(char));
                try
                {
                    if (GetRawInputDeviceInfo(device.Device, RidiDeviceName, buffer, ref charCount) != unchecked((uint)-1))
                        names.Add(Marshal.PtrToStringUni(buffer) ?? string.Empty);
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }

            return names;
        }
        
        /// <summary>
        /// Scans for new devices
        /// </summary>
        public override void Scan()
        {
            var connectedDevices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);

            xInputDevices = GetAllXInputDevices();

            foreach (var device in connectedDevices)
            {
                if (!Devices.ContainsKey(device.InstanceGuid))
                {
                    OpenDevice(device);
                }
            }
        }

        /// <summary>
        /// Opens a new game controller or gamepad
        /// </summary>
        /// <param name="deviceInstance">The device instance</param>
        public void OpenDevice(DeviceInstance deviceInstance)
        {
            // Ignore XInput devices since they are handled by XInput
            if (IsXInputDevice(deviceInstance.ProductGuid))
                return;

            if (Devices.ContainsKey(deviceInstance.InstanceGuid))
                throw new InvalidOperationException($"DirectInput GameController already opened {deviceInstance.InstanceGuid}/{deviceInstance.InstanceName}");

            GameControllerDirectInput controller;
            try
            {
                controller = new GameControllerDirectInput(this, directInput, deviceInstance);
            }
            catch (SharpDXException)
            {
                // Some failure occured during device creation
                return;
            }

            // Find gamepad layout
            var layout = GamePadLayouts.FindLayout(this, controller);
            if (layout != null)
            {
                // Create a gamepad wrapping around the controller
                var gamePad = new GamePadDirectInput(this, inputManager, controller, layout);
                controller.Disconnected += (sender, args) =>
                {
                    // Queue device for removal
                    devicesToRemove.Add(gamePad.Id);
                };
                RegisterDevice(gamePad); // Register gamepad instead
            }
            else
            {
                controller.Disconnected += (sender, args) =>
                {
                    // Queue device for removal
                    devicesToRemove.Add(controller.Id);
                };
                RegisterDevice(controller);
            }
        }

        private bool IsXInputDevice(Guid productGuid)
        {
            string productGuidStr = productGuid.ToString();
            
            // Loop over all devices
            foreach (var deviceId in xInputDevices)
            {
                if (productGuidStr.StartsWith(deviceId))
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#endif
