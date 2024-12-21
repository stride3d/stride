// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using System.Collections.Generic;
using Silk.NET.SDL;
using Window = Stride.Graphics.SDL.Window;

namespace Stride.Input
{
    internal unsafe class GameControllerSDL : GameControllerDeviceBase, IDisposable
    {
        private static Sdl SDL = Window.SDL;

        private readonly List<GameControllerButtonInfo> buttonInfos = new List<GameControllerButtonInfo>();
        private readonly List<GameControllerAxisInfo> axisInfos = new List<GameControllerAxisInfo>();
        private readonly List<GameControllerDirectionInfo> povControllerInfos = new List<GameControllerDirectionInfo>();
        
        private readonly Joystick* joystick;

        private bool disposed;

        internal int InstanceId { get; private set; } 

        public GameControllerSDL(InputSourceSDL source, int deviceIndex)
        {
            Source = source;
            joystick = SDL.JoystickOpen(deviceIndex);

            Id = Guid.NewGuid(); // Should be unique
            var joystickGUID = SDL.JoystickGetGUID(joystick); // Will identify the type of controller
            ProductId = *(Guid*)&joystickGUID;
            Name = SDL.JoystickNameS(joystick);

            InstanceId = SDL.JoystickInstanceID(joystick);

            for (int i = 0; i < SDL.JoystickNumButtons(joystick); i++)
            {
                buttonInfos.Add(new GameControllerButtonInfo { Name = $"Button {i}" });
            }

            for (int i = 0; i < SDL.JoystickNumAxes(joystick); i++)
            {
                axisInfos.Add(new GameControllerAxisInfo { Name = $"Axis {i}" });
            }

            for (int i = 0; i < SDL.JoystickNumHats(joystick); i++)
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
                SDL.JoystickClose(joystick);
                if (Disconnected == null)
                    throw new InvalidOperationException("Something should handle controller disconnect");
                Disconnected.Invoke(this, null);
                disposed = true;
            }
        }

        public override void Update(List<InputEvent> inputEvents)
        {
            if (SDL.JoystickGetAttached(joystick) == SdlBool.False)
            {
                Dispose();
                return;
            }

            for (int i = 0; i < buttonInfos.Count; i++)
            {
                HandleButton(i, SDL.JoystickGetButton(joystick, i) != 0);
            }

            for (int i = 0; i < axisInfos.Count; i++)
            {
                short input = SDL.JoystickGetAxis(joystick, i);
                float axis = (float)input / 0x7FFF;
                HandleAxis(i, axis);
            }

            for (int i = 0; i < povControllerInfos.Count; i++)
            {
                var hat = SDL.JoystickGetHat(joystick, i);
                GamePadButton buttons;
                bool hatEnabled = ConvertJoystickHat(hat, out buttons);
                HandleDirection(i, hatEnabled ? GameControllerUtils.ButtonsToDirection(buttons) : Direction.None);
            }

            base.Update(inputEvents);
        }

        private bool ConvertJoystickHat(byte hat, out GamePadButton buttons)
        {
            buttons = 0;

            if (hat == Sdl.HatCentered)
                return false;

            for (int j = 0; j < 4; j++)
            {
                int mask = 1 << j;
                if ((hat & mask) != 0)
                {
                    switch (mask)
                    {
                        case Sdl.HatUp:
                            buttons |= GamePadButton.PadUp;
                            break;
                        case Sdl.HatRight:
                            buttons |= GamePadButton.PadRight;
                            break;
                        case Sdl.HatDown:
                            buttons |= GamePadButton.PadDown;
                            break;
                        case Sdl.HatLeft:
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
