// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// A pointer event.
    /// </summary>
    public class PointerEvent : InputEvent
    {
        /// <summary>
        /// Gets a unique identifier of the pointer. See remarks.
        /// </summary>
        /// <value>The pointer id.</value>
        /// <remarks>The default mouse pointer will always be affected to the PointerId 0. On a tablet, a pen or each fingers will get a unique identifier.</remarks>
        public int PointerId { get; internal set; }

        /// <summary>
        /// Gets the absolute screen position of the pointer.
        /// </summary>
        /// <value>The absolute delta position.</value>
        public Vector2 AbsolutePosition => Position * Pointer.SurfaceSize;

        /// <summary>
        /// Gets the normalized screen position of the pointer.
        /// </summary>
        /// <value>The position.</value>
        public Vector2 Position { get; internal set; }

        /// <summary>
        /// Gets the absolute delta position of the pointer since the previous frame.
        /// </summary>
        /// <value>The absolute delta position.</value>
        public Vector2 AbsoluteDeltaPosition => DeltaPosition * Pointer.SurfaceSize;

        /// <summary>
        /// Gets the delta position of the pointer since the previous frame.
        /// </summary>
        /// <value>The delta position.</value>
        public Vector2 DeltaPosition { get; internal set; }

        /// <summary>
        /// Gets the amount of time since the previous state.
        /// </summary>
        /// <value>The delta time.</value>
        public TimeSpan DeltaTime { get; internal set; }

        /// <summary>
        /// Gets the type of pointer event (pressed,released,etc.)
        /// </summary>
        /// <value>The state.</value>
        public PointerEventType EventType { get; internal set; }

        /// <summary>
        /// Gets if the pointer is down, useful for filtering out move events that are not placed between drags
        /// </summary>
        public bool IsDown { get; internal set; }

        /// <summary>
        /// The pointer that sent this event
        /// </summary>
        public IPointerDevice Pointer => (IPointerDevice)Device;

        public override string ToString()
        {
            return $"Pointer {PointerId} {EventType}, {AbsolutePosition}, Delta: {AbsoluteDeltaPosition}, DT: {DeltaTime}, {nameof(IsDown)}: {IsDown}, {nameof(Pointer)}: {Pointer.Name}";
        }

        /// <summary>
        /// Clones the pointer event, this is useful if you intend to use it after this frame, since otherwise it would be recycled by the input manager the next frame
        /// </summary>
        /// <returns>The cloned event</returns>
        public PointerEvent Clone()
        {
            return new PointerEvent
            {
                Device = Device,
                PointerId = PointerId,
                Position = Position,
                DeltaPosition = DeltaPosition,
                DeltaTime = DeltaTime,
                EventType = EventType,
                IsDown = IsDown,
            };
        }
    }
}
