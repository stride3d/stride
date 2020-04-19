// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input
{
    /// <summary>
    /// Event for a mouse wheel being used
    /// </summary>
    public class MouseWheelEvent : InputEvent
    {
        /// <summary>
        /// The amount the mouse wheel scrolled
        /// </summary>
        public float WheelDelta;

        /// <summary>
        /// The mouse that sent this event
        /// </summary>
        public IMouseDevice Mouse => (IMouseDevice)Device;

        public override string ToString()
        {
            return $"{nameof(WheelDelta)}: {WheelDelta} {nameof(Mouse)}: {Mouse.Name}";
        }
    }
}