// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using SDL2;

namespace Stride.Input
{
    internal class GameControllerSDL : GameControllerDeviceBase, IDisposable
    {
        private readonly List<GameControllerButtonInfo> buttonInfos = new List<GameControllerButtonInfo>();
        private readonly List<GameControllerAxisInfo> axisInfos = new List<GameControllerAxisInfo>();
        private readonly List<GameControllerDirectionInfo> povControllerInfos = new List<GameControllerDirectionInfo>();
        
        private readonly IntPtr joystick;

        private bool disposed;

        internal int InstanceId { get; private set; } 

        public GameControllerSDL(InputSourceSDL source, int deviceIndex)
        {
            Source = source;
            joystick = SDL.SDL_JoystickOpen(deviceIndex);

            Id = Guid.NewGuid(); // Should be unique
            ProductId = SDL.SDL_JoystickGetGUID(joystick); // Will identify the type of controller
            Name = SDL.SDL_JoystickName(joystick);

            InstanceId = SDL.SDL_JoystickInstanceID(joystick);

            for (int i = 0; i < SDL.SDL_JoystickNumButtons(joystick); i++)
            {
                buttonInfos.Add(new GameControllerButtonInfo { Name = $"Button {i}" });
            }

            for (int i = 0; i < SDL.SDL_JoystickNumAxes(joystick); i++)
            {
                axisInfos.Add(new GameControllerAxisInfo { Name = $"Axis {i}" });
            }

            for (int i = 0; i < SDL.SDL_JoystickNumHats(joystick); i++)
            {
                povControllerInfos.Add(new GameControllerDirectionInfo { Name = $"Hat {i}" });
            }

            InitializeButtonStates();
        }
        
        public override string Name { get; }

        public override Guid Id { get; }

        public override Guid ProductId { get; }

        public override IInputSource Source { get; }

        public override IReadOnlyList<GameControllerButtonInfo> ButtonInfos => buttonInfos;

        public override IReadOnlyList<GameControllerAxisInfo> AxisInfos => axisInfos;

        public override IReadOnlyList<GameControllerDirectionInfo> DirectionInfos => povControllerInfos;

        public event EventHandler Disconnected;

        public void Dispose()
        {
            if (!disposed)
            {
                SDL.SDL_JoystickClose(joystick);
                if (Disconnected == null)
                    throw new InvalidOperationException("Something should handle controller disconnect");
                Disconnected.Invoke(this, null);
                disposed = true;
            }
        }

        public override void Update(List<InputEvent> inputEvents)
        {
            if (SDL.SDL_JoystickGetAttached(joystick) == SDL.SDL_bool.SDL_FALSE)
            {
                Dispose();
                return;
            }

            for (int i = 0; i < buttonInfos.Count; i++)
            {
                HandleButton(i, SDL.SDL_JoystickGetButton(joystick, i) != 0);
            }

            for (int i = 0; i < axisInfos.Count; i++)
            {
                short input = SDL.SDL_JoystickGetAxis(joystick, i);
                float axis = (float)input / 0x7FFF;
                HandleAxis(i, axis);
            }

            for (int i = 0; i < povControllerInfos.Count; i++)
            {
                var hat = SDL.SDL_JoystickGetHat(joystick, i);
                GamePadButton buttons;
                bool hatEnabled = ConvertJoystickHat(hat, out buttons);
                HandleDirection(i, hatEnabled ? GameControllerUtils.ButtonsToDirection(buttons) : Direction.None);
            }

            base.Update(inputEvents);
        }

        private bool ConvertJoystickHat(byte hat, out GamePadButton buttons)
        {
            buttons = 0;

            if (hat == SDL.SDL_HAT_CENTERED)
                return false;

            for (int j = 0; j < 4; j++)
            {
                int mask = 1 << j;
                if ((hat & mask) != 0)
                {
                    switch (mask)
                    {
                        case SDL.SDL_HAT_UP:
                            buttons |= GamePadButton.PadUp;
                            break;
                        case SDL.SDL_HAT_RIGHT:
                            buttons |= GamePadButton.PadRight;
                            break;
                        case SDL.SDL_HAT_DOWN:
                            buttons |= GamePadButton.PadDown;
                            break;
                        case SDL.SDL_HAT_LEFT:
                            buttons |= GamePadButton.PadLeft;
                            break;
                    }
                }
            }

            return true;
        }
    }
}
#endif