// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using SDL2;
using Stride.Games;
using Stride.Graphics.SDL;

namespace Stride.Input
{
    /// <summary>
    /// Provides support for mouse/touch/keyboard/gamepads using SDL
    /// </summary>
    internal class InputSourceSDL : InputSourceBase
    {
        private readonly HashSet<Guid> devicesToRemove = new HashSet<Guid>();
        private readonly Dictionary<int, Guid> joystickInstanceIdToDeviceId = new Dictionary<int, Guid>();
        private GameContext<Window> context;
        private Window uiControl;
        private MouseSDL mouse;
        private KeyboardSDL keyboard;
        private PointerSDL pointer; // Touch
        private InputManager inputManager;

        public override void Initialize(InputManager inputManager)
        {
            this.inputManager = inputManager;
            context = inputManager.Game.Context as GameContext<Window>;
            uiControl = context.Control;

            SDL.SDL_InitSubSystem(SDL.SDL_INIT_JOYSTICK);

            mouse = new MouseSDL(this, inputManager.Game, uiControl);
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

            SDL.SDL_QuitSubSystem(SDL.SDL_INIT_JOYSTICK);

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
            for (int i = 0; i < SDL.SDL_NumJoysticks(); i++)
            { 
                if (!joystickInstanceIdToDeviceId.ContainsKey(GetJoystickInstanceId(i)))
                {
                    OpenDevice(i);
                }
            }
        }

        private void OpenDevice(int deviceIndex)
        {
            var joystickId = SDL.SDL_JoystickGetDeviceGUID(deviceIndex);
            var joystickName = SDL.SDL_JoystickNameForIndex(deviceIndex);
            if (joystickInstanceIdToDeviceId.ContainsKey(GetJoystickInstanceId(deviceIndex)))
                throw new InvalidOperationException($"SDL GameController already opened {deviceIndex}/{joystickId}/{joystickName}");

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
            var joystick = SDL.SDL_JoystickOpen(deviceIndex);
            var instance = SDL.SDL_JoystickInstanceID(joystick);
            SDL.SDL_JoystickClose(joystick);
            return instance;
        }
    }
}

#endif
