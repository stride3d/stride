// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// List all the available type of Gestures.
    /// </summary>
    public enum GestureType
    {
        /// <summary>
        /// The user touched the screen and then performed a free translation.
        /// </summary>
        /// <remarks>Drag is a continuous gesture.</remarks>
        Drag,
        /// <summary>
        /// The user touched the screen and then performed a quick swipe.
        /// </summary>
        /// <remarks>Flick is a discrete gesture.</remarks>
        Flick,
        /// <summary>
        /// The user touched and maintained a point on the screen.
        /// </summary>
        /// <remarks>LongPress is a discrete gesture.</remarks>
        LongPress,
        /// <summary>
        /// The user touched two points of the screen and performed a composite transformation (translation, rotation, scale).
        /// </summary>
        /// <remarks>Rotation is a continuous gesture.</remarks>
        Composite,
        /// <summary>
        /// The user touched the screen.
        /// </summary>
        /// <remarks>Tap is a discrete gesture.</remarks>
        Tap,
    }
}
