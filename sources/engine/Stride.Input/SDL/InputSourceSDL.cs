// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using Silk.NET.SDL;
using Stride.Games;
using Stride.Graphics.SDL;
using Window = Stride.Graphics.SDL.Window;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for mouse/touch/keyboard/gamepads using SDL
    /// </summary>
    internal unsafe class InputSourceSDL : InputSourceBase
    {
        private static Sdl SDL = Window.SDL;

        private readonly HashSet<Guid> devicesToRemove = new HashSet<Guid>();
        private readonly Dictionary<int, Guid> joystickInstanceIdToDeviceId = new Dictionary<int, Guid>();
        private readonly Window uiControl;
        private MouseSDL mouse;
        private KeyboardSDL keyboard;
        private PointerSDL pointer; // Touch
        private InputManager inputManager;

        public InputSourceSDL(Window uiControl)
        {
            this.uiControl = uiControl ?? throw new ArgumentNullException(nameof(uiControl));
        }

        public override void Initialize(InputManager inputManager)
        {
            this.inputManager = inputManager;

            SDL.InitSubSystem(Sdl.InitJoystick);

            mouse = new MouseSDL(this, uiControl);
            keyboard = new KeyboardSDL(this, uiControl);
            pointer = new PointerSDL(this, uiControl);

            RegisterDevice(mouse);
            RegisterDevice(keyboard);
            RegisterDevice(pointer);

            // Scan for gamepads
            Scan();

            // Handle future device changes
            uiControl.JoystickDeviceAdded += UIControlOnJoystickDeviceAdded;
            uiControl.JoystickDeviceRemoved += UIControlOnJoystickDeviceRemoved;
        }
        
        public override void Dispose()
        {
            // Stop handling device changes
            uiControl.JoystickDeviceAdded -= UIControlOnJoystickDeviceAdded;
            uiControl.JoystickDeviceRemoved -= UIControlOnJoystickDeviceRemoved;

            // Dispose all the game controllers
            foreach (var pair in Devices)
            {
                var gameController = pair.Value as GameControllerSDL;
                gameController?.Dispose();
            }

            SDL.QuitSubSystem(Sdl.InitJoystick);

            base.Dispose();
        }

        public override void Update()
        {
            // Notify event listeners of device removals
            foreach (var deviceIdToRemove in devicesToRemove)
            {
                var gameController = Devices[deviceIdToRemove];
                (gameController as IDisposable)?.Dispose();
                UnregisterDevice(gameController);
            }
            devicesToRemove.Clear();
        }

        public override void Scan()
        {
            for (int i = 0; i < SDL.NumJoysticks(); i++)
            { 
                if (!joystickInstanceIdToDeviceId.ContainsKey(GetJoystickInstanceId(i)))
                {
                    OpenDevice(i);
                }
            }
        }

        private void OpenDevice(int deviceIndex)
        {
            var joystickId = SDL.JoystickGetDeviceGUID(deviceIndex);
            var joystickName = SDL.JoystickNameForIndexS(deviceIndex);
            if (joystickInstanceIdToDeviceId.ContainsKey(GetJoystickInstanceId(deviceIndex)))
                throw new InvalidOperationException($"SDL GameController already opened {deviceIndex}/{*(Guid*)&joystickId}/{joystickName}");

            var controller = new GameControllerSDL(this, deviceIndex);

            IInputDevice resultingDevice = controller;

            // Find gamepad layout
            var layout = GamePadLayouts.FindLayout(this, controller);
            if (layout != null)
            {
                // Create a gamepad wrapping around the controller
                var gamePad = new GamePadSDL(this, inputManager, controller, layout);
                resultingDevice = gamePad; // Register gamepad instead
            }

            controller.Disconnected += (sender, args) =>
            {
                // Queue device for removal
                devicesToRemove.Add(resultingDevice.Id);
                joystickInstanceIdToDeviceId.Remove(controller.InstanceId);
            };

            RegisterDevice(resultingDevice);
            joystickInstanceIdToDeviceId.Add(controller.InstanceId, resultingDevice.Id);
        }
        
        private void UIControlOnJoystickDeviceRemoved(int which)
        {
            Guid deviceId;
            if (joystickInstanceIdToDeviceId.TryGetValue(which, out deviceId))
            {
                devicesToRemove.Add(deviceId);
            }
        }

        private void UIControlOnJoystickDeviceAdded(int which)
        {
            if (!joystickInstanceIdToDeviceId.ContainsKey(GetJoystickInstanceId(which)))
            {
                OpenDevice(which);
            }
        }

        private int GetJoystickInstanceId(int deviceIndex)
        {
            var joystick = SDL.JoystickOpen(deviceIndex);
            var instance = SDL.JoystickInstanceID(joystick);
            SDL.JoystickClose(joystick);
            return instance;
        }
    }
}

#endif
