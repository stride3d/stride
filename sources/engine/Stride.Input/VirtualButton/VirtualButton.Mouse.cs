// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Input
{
    /// <summary>
    /// Describes a virtual button (a key from a keyboard, a mouse button, an axis of a joystick...etc.).
    /// </summary>
    public partial class VirtualButton
    {
        /// <summary>
        /// Mouse virtual button.
        /// </summary>
        public class Mouse : VirtualButton
        {
            protected Mouse(string name, int id, bool isPositiveAndNegative)
                : base(name, VirtualButtonType.Mouse, id, isPositiveAndNegative)
            {
            }

            /// <summary>
            /// Equivalent to <see cref="MouseButton.Left"/>.
            /// </summary>
            public static readonly Mouse Left = new Mouse("Left", 0, false);

            /// <summary>
            /// Equivalent to <see cref="MouseButton.Middle"/>.
            /// </summary>
            public static readonly Mouse Middle = new Mouse("Middle", 1, false);

            /// <summary>
            /// Equivalent to <see cref="MouseButton.Right"/>.
            /// </summary>
            public static readonly Mouse Right = new Mouse("Right", 2, false);

            /// <summary>
            /// Equivalent to <see cref="MouseButton.Extended1"/>.
            /// </summary>
            public static readonly Mouse Extended1 = new Mouse("Extended1", 3, false);

            /// <summary>
            /// Equivalent to <see cref="MouseButton.Extended2"/>.
            /// </summary>
            public static readonly Mouse Extended2 = new Mouse("Extended2", 4, false);

            /// <summary>
            /// Equivalent to X Axis of <see cref="InputManager.MousePosition"/>.
            /// </summary>
            public static readonly Mouse PositionX = new Mouse("PositionX", 5, true);

            /// <summary>
            /// Equivalent to Y Axis of <see cref="InputManager.MousePosition"/>.
            /// </summary>
            public static readonly Mouse PositionY = new Mouse("PositionY", 6, true);

            /// <summary>
            /// Equivalent to X Axis delta of <see cref="InputManager.MousePosition"/>.
            /// </summary>
            public static readonly Mouse DeltaX = new Mouse("DeltaX", 7, true);

            /// <summary>
            /// Equivalent to Y Axis delta of <see cref="InputManager.MousePosition"/>.
            /// </summary>
            public static readonly Mouse DeltaY = new Mouse("DeltaY", 8, true);

            public override float GetValue(InputManager manager)
            {
                if (Index < 5)
                {
                    if (IsDown(manager))
                        return 1.0f;
                }
                else
                {
                    switch (Index)
                    {
                        case 5:
                            return manager.MousePosition.X;
                        case 6:
                            return manager.MousePosition.Y;
                        case 7:
                            return manager.MouseDelta.X;
                        case 8:
                            return manager.MouseDelta.Y;
                    }
                }

                return 0.0f;
            }

            public override bool IsDown(InputManager manager)
            {
                return Index < 5 ? manager.IsMouseButtonDown((MouseButton)Index) : false;
            }

            public override bool IsPressed(InputManager manager)
            {
                return Index < 5 ? manager.IsMouseButtonPressed((MouseButton)Index) : false;
            }

            public override bool IsReleased(InputManager manager)
            {
                return Index < 5 ? manager.IsMouseButtonReleased((MouseButton)Index) : false;
            }
        }
    }
}
