// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;
using System.Collections.Generic;
using Microsoft.Management.Infrastructure;
using SharpDX;
using SharpDX.DirectInput;
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

        public override void Initialize(InputManager inputManager)
        {
            this.inputManager = inputManager;
            directInput = new DirectInput();
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
        /// Scans for new devices
        /// </summary>
        public override void Scan()
        {
            var connectedDevices = directInput.GetDevices(DeviceClass.GameControl, DeviceEnumerationFlags.AttachedOnly);
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
            if (IsXInputDevice(ref deviceInstance.ProductGuid))
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
            // Set security level to IMPERSONATE

            DComSessionOptions DComOptions = new DComSessionOptions();
            DComOptions.Impersonation = ImpersonationType.Impersonate;

            var mySession = CimSession.Create(null, DComOptions);

            IEnumerable<CimInstance> allDevices = mySession.QueryInstances(@"root\cimv2", "WQL", "SELECT * FROM Win32_PNPEntity");

            var regex = new Regex(@"VID_(\w+)?&PID_(\w+)?&IG_");

            // Loop over all devices
            foreach (var device in allDevices)
            {
                var deviceId = device.CimInstanceProperties["DeviceID"].Value.ToString();
                
                var match = regex.Match(deviceId);

                // Check if the device ID contains "IG_".  If it does, then it's an XInput device
                // This information can not be found from DirectInput 
                
                if (match.Success)
                {
                    string guidPart = (match.Groups[1].ToString()+match.Groups[2].ToString()).ToLower();

                    if (productGuid.ToString().StartsWith(guidPart))
                    {
                        return true;
                    }
                }
                 
            }

            return false;
        }
    }
}

#endif
