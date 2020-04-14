// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Provides a <see cref="IGameControllerDevice"/> to <see cref="IGamePadDevice"/> mapping
    /// </summary>
    public abstract class GamePadLayout
    {
        /// <summary>
        /// Should direction controller 0 be mapped to the directional pad?
        /// </summary>
        protected bool mapFirstPovToPad = true;

        private List<GamePadButton> buttonMap = new List<GamePadButton>();
        private List<GamePadButton> axisToButtonMap = new List<GamePadButton>();
        private List<MappedAxis> axisMap = new List<MappedAxis>();
        private List<MappedAxis> buttonsToTriggerMap = new List<MappedAxis>();

        /// <summary>
        /// Compares a product id
        /// </summary>
        /// <param name="a">id a</param>
        /// <param name="b">id b</param>
        /// <param name="numBytes">number of bytes to compare, starting from <paramref name="byteOffset"/></param>
        /// <param name="byteOffset">starting byte index from where to compare</param>
        /// <returns></returns>
        public static bool CompareProductId(Guid a, Guid b, int numBytes = 16, int byteOffset = 0)
        {
            byte[] aBytes = a.ToByteArray();
            byte[] bBytes = b.ToByteArray();
            byteOffset = MathUtil.Clamp(byteOffset, 0, aBytes.Length);
            numBytes = MathUtil.Clamp(byteOffset + numBytes, 0, aBytes.Length) - byteOffset;
            for (int i = byteOffset; i < numBytes; i++)
                if (aBytes[i] != bBytes[i]) return false;
            return true;
        }

        /// <summary>
        /// Checks if a device matches this gamepad layout, and thus should use this when mapping it to a <see cref="GamePadState"/>
        /// </summary>
        /// <param name="source">Source that this device comes from</param>
        /// <param name="device">The device to match</param>
        public abstract bool MatchDevice(IInputSource source, IGameControllerDevice device);

        /// <summary>
        /// Allows the user to perform some additional setup operations when using this layout on a device
        /// </summary>
        /// <param name="targetDevice">The gamepad that events are mapped to</param>
        /// <param name="sourceDevice">The game controller that is mapped to a gamepad</param>
        public virtual void InitializeDevice(IGamePadDevice targetDevice, IGameControllerDevice sourceDevice)
        {
        }

        /// <summary>
        /// Maps game controller events to gamepad events
        /// </summary>
        /// <returns>The equivalent gamepad event</returns>
        /// <param name="targetDevice">The gamepad that events are mapped to</param>
        /// <param name="sourceDevice">The game controller that is mapped to a gamepad</param>
        /// <param name="controllerEvent">The controller input event as a source</param>
        /// <param name="target">Target list</param>
        public virtual void MapInputEvent(IGamePadDevice targetDevice, IGameControllerDevice sourceDevice, InputEvent controllerEvent, List<InputEvent> target)
        {
            var buttonEvent = controllerEvent as GameControllerButtonEvent;
            if (buttonEvent != null)
            {
                if (buttonEvent.Index < buttonMap.Count && 
                    buttonMap[buttonEvent.Index] != GamePadButton.None)
                {
                    GamePadButtonEvent buttonEvent1 = InputEventPool<GamePadButtonEvent>.GetOrCreate(targetDevice);
                    buttonEvent1.Button = buttonMap[buttonEvent.Index];
                    buttonEvent1.IsDown = buttonEvent.IsDown;
                    target.Add(buttonEvent1);
                }

                if (buttonEvent.Index < buttonsToTriggerMap.Count && 
                    buttonsToTriggerMap[buttonEvent.Index].Axis != GamePadAxis.None)
                {
                    var mappedAxis = buttonsToTriggerMap[buttonEvent.Index];

                    GamePadAxisEvent axisEvent1 = InputEventPool<GamePadAxisEvent>.GetOrCreate(targetDevice);
                    axisEvent1.Axis = mappedAxis.Axis;
                    axisEvent1.Value = buttonEvent.IsDown ? 1.0f : 0.0f;
                    if (mappedAxis.Invert)
                        axisEvent1.Value = -axisEvent1.Value;
                    target.Add(axisEvent1);
                }
            }
            else
            {
                var axisEvent = controllerEvent as GameControllerAxisEvent;
                if (axisEvent != null)
                {
                    if (axisEvent.Index < axisMap.Count && 
                        axisMap[axisEvent.Index].Axis != GamePadAxis.None)
                    {
                        var mappedAxis = axisMap[axisEvent.Index];

                        GamePadAxisEvent axisEvent1 = InputEventPool<GamePadAxisEvent>.GetOrCreate(targetDevice);
                        axisEvent1.Axis = mappedAxis.Axis;
                        if (mappedAxis.Invert)
                            axisEvent1.Value = -axisEvent.Value;
                        else
                            axisEvent1.Value = axisEvent.Value;
                        if (mappedAxis.Remap)
                        {
                            axisEvent1.Value = (axisEvent1.Value + 1.0f) * 0.5f;
                            if (axisEvent1.Value < 0.0001f)
                                axisEvent1.Value = 0.0f;
                        }

                        target.Add(axisEvent1);
                    }
                    if (axisEvent.Index < axisToButtonMap.Count &&
                        axisToButtonMap[axisEvent.Index] != GamePadButton.None)
                    {
                        GamePadButtonEvent buttonEvent1 = InputEventPool<GamePadButtonEvent>.GetOrCreate(targetDevice);
                        buttonEvent1.Button = axisToButtonMap[axisEvent.Index];
                        buttonEvent1.IsDown = axisEvent.Value > 0.5f;
                        target.Add(buttonEvent1);
                    }
                }
                else if (mapFirstPovToPad)
                {
                    var directionEvent = controllerEvent as GameControllerDirectionEvent;
                    if (directionEvent?.Index == 0)
                    {
                        GamePadButton targetButtons = GameControllerUtils.DirectionToButtons(directionEvent.Direction);

                        // Pad buttons down
                        for (int i = 0; i < 4; i++)
                        {
                            int mask = (1 << i);
                            if (((int)targetDevice.State.Buttons & mask) != ((int)targetButtons & mask))
                            {
                                GamePadButtonEvent buttonEvent1 = InputEventPool<GamePadButtonEvent>.GetOrCreate(targetDevice);
                                buttonEvent1.Button = (GamePadButton)mask;
                                buttonEvent1.IsDown = ((int)targetButtons & mask) != 0;
                                target.Add(buttonEvent1);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a mapping from a button index to <see cref="GamePadButton"/>
        /// </summary>
        /// <param name="index">The button index of the button on this device</param>
        /// <param name="button">The button(s) to map to</param>
        protected void AddButtonToButton(int index, GamePadButton button)
        {
            while (buttonMap.Count <= index) buttonMap.Add(GamePadButton.None);
            buttonMap[index] = button;
        }

        /// <summary>
        /// Adds a mapping from an axis index to <see cref="GamePadButton"/>
        /// </summary>
        /// <param name="index">The axis index of the axis on this device</param>
        /// <param name="button">The button(s) to map to</param>
        protected void AddAxisToButton(int index, GamePadButton button)
        {
            while (axisToButtonMap.Count <= index) axisToButtonMap.Add(GamePadButton.None);
            axisToButtonMap[index] = button;
        }
        
        /// <summary>
        /// Adds a mapping from a button index to <see cref="GamePadAxis"/>
        /// </summary>
        /// <param name="index">The button index of the button on this device</param>
        /// <param name="axis">The axi to map to</param>
        /// <param name="invert">Should axis be inverted, output -1 instead of 1 on press</param>
        protected void AddButtonToAxis(int index, GamePadAxis axis, bool invert = false)
        {
            while (buttonsToTriggerMap.Count <= index) buttonsToTriggerMap.Add(new MappedAxis { Axis = GamePadAxis.None });
            buttonsToTriggerMap[index] = new MappedAxis { Axis = axis, Invert = invert, Remap = false };
        }

        /// <summary>
        /// Adds a mapping from an axis index to <see cref="GamePadAxis"/>
        /// </summary>
        /// <param name="index">The axis index of the axis on this device</param>
        /// <param name="axis">The axis to map to</param>
        /// <param name="invert">Should axis be inverted</param>
        /// <param name="remap">Remap this axis from (-1,1) to (0,1)</param>
        protected void AddAxisToAxis(int index, GamePadAxis axis, bool invert = false, bool remap = false)
        {
            while (axisMap.Count <= index) axisMap.Add(new MappedAxis { Axis = GamePadAxis.None });
            axisMap[index] = new MappedAxis { Axis = axis, Invert = invert, Remap = remap };
        }

        private struct MappedAxis
        {
            public GamePadAxis Axis;
            public bool Invert;
            public bool Remap;
        }
    }
}
