// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP
using System;
using System.Collections.Generic;
using SharpDX.XInput;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for XInput gamepads
    /// </summary>
    internal class InputSourceWindowsXInput : InputSourceBase
    {
        private const int XInputGamePadCount = 4;

        private readonly List<int> devicesToRemove = new List<int>();

        // Always monitored gamepads
        private Controller[] controllers;
        private Guid[] controllerIds;
        private GamePadXInput[] devices;

        public static bool IsSupported()
        {
            try
            {
                var controller = new Controller();
                bool connected = controller.IsConnected;
            }
            catch (Exception ex)
            {
                InputManager.Logger.Warning($"XInput dll was not found on the computer. GameController detection will not fully work for the current game instance. To fix the problem, please install or repair DirectX installation. [Exception details: {ex.Message}]");
                return false;
            }

            return true;
        }

        public override void Initialize(InputManager inputManager)
        {
            Controller.SetReporting(true);

            controllers = new Controller[XInputGamePadCount];
            controllerIds = new Guid[XInputGamePadCount];
            devices = new GamePadXInput[XInputGamePadCount];

            // Prebuild fake GUID
            for (int i = 0; i < XInputGamePadCount; i++)
            {
                controllerIds[i] = new Guid(i, 11, 22, 33, 0, 0, 0, 0, 0, 0, 0);
                controllers[i] = new Controller((UserIndex)i);
            }
            Scan();
        }

        public override void Dispose()
        {
            base.Dispose();

            // Dispose all the gamepads
            foreach (var gamePad in devices)
            {
                gamePad?.Dispose();
            }
        }

        public override void Update()
        {
            // Notify event listeners of device removals
            foreach (var deviceIdToRemove in devicesToRemove)
            {
                var gamePad = devices[deviceIdToRemove];
                UnregisterDevice(gamePad);
                devices[deviceIdToRemove] = null;
                gamePad.Dispose();
            }
            devicesToRemove.Clear();
        }

        /// <summary>
        /// Scans for new devices
        /// </summary>
        public override void Scan()
        {
            for (int i = 0; i < XInputGamePadCount; i++)
            {
                if (devices[i] == null)
                {
                    // Should register controller
                    if (controllers[i].IsConnected)
                    {
                        OpenDevice(i);
                    }
                }
            }
        }

        /// <summary>
        /// Opens a new gamepad
        /// </summary>
        /// <param name="instance">The gamepad</param>
        public void OpenDevice(int index)
        {
            if (index < 0 || index >= XInputGamePadCount)
                throw new IndexOutOfRangeException($"Invalid XInput device index {index}");

            if (devices[index] != null)
                throw new InvalidOperationException($"XInput device already opened {index}");

            var newGamepad = new GamePadXInput(this, controllers[index], controllerIds[index], index);
            newGamepad.Disconnected += (sender, args) =>
            {
                // Queue device for removal
                devicesToRemove.Add(index);
            };

            devices[index] = newGamepad;
            RegisterDevice(newGamepad);
        }
    }
}

#endif