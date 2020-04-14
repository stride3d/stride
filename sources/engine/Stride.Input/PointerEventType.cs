// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// State of a pointer event.
    /// </summary>
    public enum PointerEventType
    {
        /// <summary>
        /// The pointer just started to hit the digitizer.
        /// </summary>
        Pressed,

        /// <summary>
        /// The pointer is moving on the digitizer.
        /// </summary>
        Moved,

        /// <summary>
        /// The pointer just released pressure to the digitizer.
        /// </summary>
        Released,

        /// <summary>
        /// The pointer has been canceled.
        /// </summary>
        Canceled,
    }
}