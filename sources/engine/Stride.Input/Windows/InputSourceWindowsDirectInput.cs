// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;
using System.Collections.Generic;
using Microsoft.Management.Infrastructure;
using SharpDX;
using SharpDX.DirectInput;
using System.Linq;
using Microsoft.Management.Infrastructure.Options;
using System.Text.RegularExpressions;

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
        /// Select all device IDs that contain "IG_".  If so, it's an XInput device
        /// This information can not be found from DirectInput 
        /// </summary>
        private IEnumerable<string> GetAllXInputDevices()
        {
            // Set security level to IMPERSONATE

            DComSessionOptions DComOptions = new DComSessionOptions();
            DComOptions.Impersonation = ImpersonationType.Impersonate;

            var session = CimSession.Create(null, DComOptions);
            var query = session.QueryInstances(@"root\cimv2", "WQL", "SELECT DeviceID FROM Win32_PNPEntity WHERE DeviceID LIKE '%&IG_%'");

            var deviceIdPrefixes = query.Select(device => xInputDeviceIdRegex.Match(device.CimInstanceProperties["DeviceID"].Value.ToString()))
                .Where(match => match.Success)
                .Select(match => (match.Groups[2].ToString() + match.Groups[1].ToString()).ToLower())
                .Distinct()
                .ToList();

            return deviceIdPrefixes;
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
