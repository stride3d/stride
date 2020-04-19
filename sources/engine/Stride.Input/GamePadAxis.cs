// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input
{
    /// <summary>
    /// Axis for gamepad returned by <see cref="GamePadState"/>.
    /// </summary>
    [Flags]
    public enum GamePadAxis : ushort
    {
        /// <summary>
        /// The X-Axis of the left thumb stick
        /// </summary>
        LeftThumbX = 1 << 1,

        /// <summary>
        /// The Y-Axis of the left thumb stick
        /// </summary>
        LeftThumbY = 1 << 2,

        /// <summary>
        /// The X-Axis of the right thumb stick
        /// </summary>
        RightThumbX = 1 << 3,

        /// <summary>
        /// The Y-Axis of the right thumb stick
        /// </summary>
        RightThumbY = 1 << 4,

        /// <summary>
        /// The left trigger
        /// </summary>
        LeftTrigger = 1 << 5,

        /// <summary>
        /// The right trigger
        /// </summary>
        RightTrigger = 1 << 6,

        /// <summary>
        /// No Axis
        /// </summary>
        None = 0,
    }
}