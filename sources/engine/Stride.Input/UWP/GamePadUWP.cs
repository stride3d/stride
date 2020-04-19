// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_UWP
using System;
using System.Collections.Generic;
using Windows.Gaming.Input;

namespace Stride.Input
{
    /// <summary>
    /// UWP Gamepad
    /// </summary>
    internal class GamePadUWP : GamePadDeviceBase
    {
        internal Gamepad Gamepad;
        private readonly double[] lastAxisState = new double[6];
        private GamePadState state = new GamePadState();

        private Dictionary<GamepadButtons, GamePadButton> buttonMap = new Dictionary<GamepadButtons, GamePadButton>
        {
            [GamepadButtons.DPadDown] = GamePadButton.PadDown,
            [GamepadButtons.DPadLeft] = GamePadButton.PadLeft,
            [GamepadButtons.DPadRight] = GamePadButton.PadRight,
            [GamepadButtons.DPadUp] = GamePadButton.PadUp,
            [GamepadButtons.A] = GamePadButton.A,
            [GamepadButtons.B] = GamePadButton.B,
            [GamepadButtons.X] = GamePadButton.X,
            [GamepadButtons.Y] = GamePadButton.Y,
            [GamepadButtons.Menu] = GamePadButton.Start,
            [GamepadButtons.View] = GamePadButton.Back,
            [GamepadButtons.LeftShoulder] = GamePadButton.LeftShoulder,
            [GamepadButtons.RightShoulder] = GamePadButton.RightShoulder,
            [GamepadButtons.LeftThumbstick] = GamePadButton.LeftThumb,
            [GamepadButtons.RightThumbstick] = GamePadButton.RightThumb,
        };

        public GamePadUWP(InputSourceUWP source, Gamepad gamepad, Guid id)
        {
            Source = source;
            Id = id;
            ProductId = new Guid("800BE63B-49DC-4214-A4D2-E39E24EA3542");
            Gamepad = gamepad;
        }

        public override string Name => "UWP GamePad";

        public override Guid Id { get; }

        public override Guid ProductId { get; }

        public override IInputSource Source { get; }

        public override GamePadState State => state;

        public override void Update(List<InputEvent> inputEvents)
        {
            var reading = Gamepad.GetCurrentReading();

            ClearButtonStates();

            // Check buttons
            for (int i = 0; i < 14; i++)
            {
                int mask = 1 << i;
                GamePadButton button = buttonMap[(GamepadButtons)mask];
                var oldState = (state.Buttons & button) != 0;
                var newState = (reading.Buttons & (GamepadButtons)mask) != 0;
                if (oldState != newState)
                {
                    GamePadButtonEvent buttonEvent = InputEventPool<GamePadButtonEvent>.GetOrCreate(this);
                    buttonEvent.IsDown = newState;
                    buttonEvent.Button = button;
                    inputEvents.Add(buttonEvent);
                    if (state.Update(buttonEvent))
                    {
                        UpdateButtonState(buttonEvent);
                    }
                }
            }
            if (ChangeAxis(0, reading.LeftThumbstickX))
                inputEvents.Add(CreateAxisEvent(GamePadAxis.LeftThumbX, reading.LeftThumbstickX));
            if (ChangeAxis(1, reading.LeftThumbstickY))
                inputEvents.Add(CreateAxisEvent(GamePadAxis.LeftThumbY, reading.LeftThumbstickY));

            if (ChangeAxis(2, reading.RightThumbstickX))
                inputEvents.Add(CreateAxisEvent(GamePadAxis.RightThumbX, reading.RightThumbstickX));
            if (ChangeAxis(3, reading.RightThumbstickY))
                inputEvents.Add(CreateAxisEvent(GamePadAxis.RightThumbY, reading.RightThumbstickY));

            if (ChangeAxis(4, reading.LeftTrigger))
                inputEvents.Add(CreateAxisEvent(GamePadAxis.LeftTrigger, reading.LeftTrigger));
            if (ChangeAxis(5, reading.RightTrigger))
                inputEvents.Add(CreateAxisEvent(GamePadAxis.RightTrigger, reading.RightTrigger));
        }

        public override void SetVibration(float smallLeft, float smallRight, float largeLeft, float largeRight)
        {
            Gamepad.Vibration = new GamepadVibration
            {
                LeftMotor = largeLeft,
                LeftTrigger = smallLeft,
                RightMotor = largeRight,
                RightTrigger = smallLeft,
            };
        }

        private bool ChangeAxis(int index, double newValue)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (lastAxisState[index] != newValue)
            {
                lastAxisState[index] = newValue;
                return true;
            }
            return false;
        }

        private GamePadAxisEvent CreateAxisEvent(GamePadAxis axis, double newValue)
        {
            GamePadAxisEvent axisEvent = InputEventPool<GamePadAxisEvent>.GetOrCreate(this);
            axisEvent.Value = (float)newValue;
            axisEvent.Axis = axis;
            state.Update(axisEvent);
            return axisEvent;
        }
    }
}

#endif