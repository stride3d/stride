// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Input
{
    /// <summary>
    /// Buttons for gamepad returned by <see cref="GamePadState"/>.
    /// </summary>
    [Flags]
    public enum GamePadButton : ushort
    {
        /// <summary>
        /// PadUp button. (DPad / Directional Pad)
        /// </summary>
        PadUp = 1 << 0,

        /// <summary>
        /// PadDown button. (DPad / Directional Pad)
        /// </summary>
        PadDown = 1 << 1,

        /// <summary>
        /// PadLeft button. (DPad / Directional Pad)
        /// </summary>
        PadLeft = 1 << 2,

        /// <summary>
        /// PadRight button. (DPad / Directional Pad)
        /// </summary>
        PadRight = 1 << 3,

        /// <summary>
        /// Any pad button (DPad / Directional Pad)
        /// </summary>
        Pad = 0xF,

        /// <summary>
        /// Start button.
        /// </summary>
        Start = 1 << 4,

        /// <summary>
        /// Back button.
        /// </summary>
        Back = 1 << 5,

        /// <summary>
        /// Left thumb button.
        /// </summary>
        LeftThumb = 1 << 6,

        /// <summary>
        /// Right thumb button.
        /// </summary>
        RightThumb = 1 << 7,

        /// <summary>
        /// Left shoulder button.
        /// </summary>
        LeftShoulder = 1 << 8,

        /// <summary>
        /// Right shoulder button.
        /// </summary>
        RightShoulder = 1 << 9,

        /// <summary>
        /// A button.
        /// </summary>
        A = 1 << 12,

        /// <summary>
        /// B button.
        /// </summary>
        B = 1 << 13,

        /// <summary>
        /// X button.
        /// </summary>
        X = 1 << 14,

        /// <summary>
        /// Y button.
        /// </summary>
        Y = 1 << 15,

        /// <summary>
        /// No buttons.
        /// </summary>
        None = 0,
    }
}
