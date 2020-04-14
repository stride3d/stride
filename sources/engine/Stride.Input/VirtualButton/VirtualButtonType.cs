// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Input
{
    /// <summary>
    /// Type of a <see cref="VirtualButton" />.
    /// </summary>
    public enum VirtualButtonType
    {
        /// <summary>
        /// A keyboard virtual button.
        /// </summary>
        Keyboard = 1 << 28,

        /// <summary>
        /// A mouse virtual button.
        /// </summary>
        Mouse = 2 << 28,

        /// <summary>
        /// A pointer virtual button.
        /// </summary>
        Pointer = 3 << 28,

        /// <summary>
        /// A gamepad virtual button.
        /// </summary>
        GamePad = 4 << 28,
    }
}
