// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Describes a virtual button (a key from a keyboard, a mouse button, an axis of a joystick...etc.).
    /// </summary>
    public partial class VirtualButton
    {
        /// <summary>
        /// GamePad virtual button.
        /// </summary>
        public class GamePad : VirtualButton
        {
            private GamePad(string name, int id, bool isPositiveAndNegative = false)
                : this(name, id, -1, isPositiveAndNegative)
            {
            }

            private GamePad(GamePad parentPad, int index) 
                : this(parentPad.ShortName, parentPad.Id, index, parentPad.IsPositiveAndNegative)
            {
            }

            protected GamePad(string name, int id, int padIndex, bool isPositiveAndNegative)
                : base(name, VirtualButtonType.GamePad, id, isPositiveAndNegative)
            {
                PadIndex = padIndex;
            }

            /// <summary>
            /// The pad index.
            /// </summary>
            public readonly int PadIndex;

            /// <summary>
            /// Return an instance of a particular GamePad.
            /// </summary>
            /// <param name="index">The gamepad index.</param>
            /// <returns>A new GamePad button linked to the gamepad index.</returns>
            public GamePad OfGamePad(int index)
            {
                return new GamePad(this, index);
            }

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.PadUp"/>.
            /// </summary>
            public static readonly GamePad PadUp = new GamePad("PadUp", 0);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.PadDown"/>.
            /// </summary>
            public static readonly GamePad PadDown = new GamePad("PadDown", 1);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.PadLeft"/>.
            /// </summary>
            public static readonly GamePad PadLeft = new GamePad("PadLeft", 2);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.PadRight"/>.
            /// </summary>
            public static readonly GamePad PadRight = new GamePad("PadRight", 3);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.Start"/>.
            /// </summary>
            public static readonly GamePad Start = new GamePad("Start", 4);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.Back"/>.
            /// </summary>
            public static readonly GamePad Back = new GamePad("Back", 5);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.LeftThumb"/>.
            /// </summary>
            public static readonly GamePad LeftThumb = new GamePad("LeftThumb", 6);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.RightThumb"/>.
            /// </summary>
            public static readonly GamePad RightThumb = new GamePad("RightThumb", 7);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.LeftShoulder"/>.
            /// </summary>
            public static readonly GamePad LeftShoulder = new GamePad("LeftShoulder", 8);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.RightShoulder"/>.
            /// </summary>
            public static readonly GamePad RightShoulder = new GamePad("RightShoulder", 9);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.A"/>.
            /// </summary>
            public static readonly GamePad A = new GamePad("A", 12);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.B"/>.
            /// </summary>
            public static readonly GamePad B = new GamePad("B", 13);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.X"/>.
            /// </summary>
            public static readonly GamePad X = new GamePad("X", 14);

            /// <summary>
            /// Equivalent to <see cref="GamePadButton.Y"/>.
            /// </summary>
            public static readonly GamePad Y = new GamePad("Y", 15);

            /// <summary>
            /// Equivalent to the X Axis of <see cref="GamePadState.LeftThumb"/>.
            /// </summary>
            public static readonly GamePad LeftThumbAxisX = new GamePad("LeftThumbAxisX", 16, true);

            /// <summary>
            /// Equivalent to the Y Axis of <see cref="GamePadState.LeftThumb"/>.
            /// </summary>
            public static readonly GamePad LeftThumbAxisY = new GamePad("LeftThumbAxisY", 17, true);

            /// <summary>
            /// Equivalent to the X Axis of <see cref="GamePadState.RightThumb"/>.
            /// </summary>
            public static readonly GamePad RightThumbAxisX = new GamePad("RightThumbAxisX", 18, true);

            /// <summary>
            /// Equivalent to the Y Axis of <see cref="GamePadState.RightThumb"/>.
            /// </summary>
            public static readonly GamePad RightThumbAxisY = new GamePad("RightThumbAxisY", 19, true);

            /// <summary>
            /// Equivalent to <see cref="GamePadState.LeftTrigger"/>.
            /// </summary>
            public static readonly GamePad LeftTrigger = new GamePad("LeftTrigger", 20);

            /// <summary>
            /// Equivalent to <see cref="GamePadState.RightTrigger"/>.
            /// </summary>
            public static readonly GamePad RightTrigger = new GamePad("RightTrigger", 21);

            protected override string BuildButtonName()
            {
                return PadIndex < 0 ? base.BuildButtonName() : Type.ToString() + PadIndex + "." + ShortName;
            }

            private IGamePadDevice GetGamePad(InputManager manager)
            {
                return PadIndex >= 0 ? manager.GetGamePadByIndex(PadIndex) : manager.DefaultGamePad;
            }

            public override float GetValue(InputManager manager)
            {
                var gamePad = GetGamePad(manager);
                if (gamePad == null)
                    return 0.0f;

                if (Index <= 15)
                {
                    if (IsDown(manager))
                        return 1.0f;
                }
                else
                {
                    var state = gamePad.State;
                    switch (Index)
                    {
                        case 16:
                            return state.LeftThumb.X;
                        case 17:
                            return state.LeftThumb.Y;
                        case 18:
                            return state.RightThumb.X;
                        case 19:
                            return state.RightThumb.Y;
                        case 20:
                            return state.LeftTrigger;
                        case 21:
                            return state.RightTrigger;
                    }
                }

                return 0.0f;
            }

            public override bool IsDown(InputManager manager)
            {
                var gamePad = GetGamePad(manager);
                if (gamePad == null)
                    return false;
                
                if (Index <= 15)
                {
                    return gamePad.IsButtonDown((GamePadButton)(1 << Index));
                }
                else if (Index == 20)
                {
                    return gamePad.State.LeftTrigger > 1f - MathUtil.ZeroTolerance;
                }
                else if (Index == 21)
                {
                    return gamePad.State.RightTrigger > 1f - MathUtil.ZeroTolerance;
                }

                return false;
            }

            public override bool IsPressed(InputManager manager)
            {
                var gamePad = GetGamePad(manager);
                if (gamePad == null)
                    return false;
                
                if (Index > 15)
                    return false;

                return gamePad.IsButtonPressed((GamePadButton)(1 << Index));
            }

            public override bool IsReleased(InputManager manager)
            {
                var gamePad = GetGamePad(manager);
                if (gamePad == null)
                    return false;

                if (Index > 15)
                    return false;

                return gamePad.IsButtonReleased((GamePadButton)(1 << Index));
            }
        }
    }
}
