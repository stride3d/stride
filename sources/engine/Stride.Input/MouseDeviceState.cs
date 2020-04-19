// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// An extension to <see cref="PointerDeviceState"/> that handle mouse input and translates it to pointer input
    /// </summary>
    public class MouseDeviceState
    {
        private Vector2 nextDelta = Vector2.Zero;

        protected readonly List<InputEvent> Events = new List<InputEvent>();

        private readonly HashSet<MouseButton> pressedButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> releasedButtons = new HashSet<MouseButton>();
        private readonly HashSet<MouseButton> downButtons = new HashSet<MouseButton>();

        protected IMouseDevice MouseDevice;
        protected PointerDeviceState PointerState;

        public MouseDeviceState(PointerDeviceState pointerState, IMouseDevice mouseDevice)
        {
            this.PointerState = pointerState;
            this.MouseDevice = mouseDevice;

            DownButtons = new ReadOnlySet<MouseButton>(downButtons);
            PressedButtons = new ReadOnlySet<MouseButton>(pressedButtons);
            ReleasedButtons = new ReadOnlySet<MouseButton>(releasedButtons);
        }

        public IReadOnlySet<MouseButton> PressedButtons { get; }
        public IReadOnlySet<MouseButton> ReleasedButtons { get; }
        public IReadOnlySet<MouseButton> DownButtons { get; }
        
        public Vector2 Position { get; set; }
        public Vector2 Delta { get; set; }

        /// <summary>
        /// Generate input events
        /// </summary>
        public void Update(List<InputEvent> inputEvents)
        {
            Reset();
            
            // Collect events from queue
            foreach (var evt in Events)
            {
                inputEvents.Add(evt);

                var mouseButtonEvent = evt as MouseButtonEvent;
                if (mouseButtonEvent != null)
                {
                    if (mouseButtonEvent.IsDown)
                    {
                        pressedButtons.Add(mouseButtonEvent.Button);
                    }
                    else
                    {
                        releasedButtons.Add(mouseButtonEvent.Button);
                    }
                }

                // Pass mouse-side generate pointer events through the pointer state
                // These should only be delta movement events so don't update it from this functions
                var pointerEvent = evt as PointerEvent;
                if (pointerEvent != null)
                {
                    PointerState.UpdatePointerState(pointerEvent, false);
                }
            }
            Events.Clear();

            // Reset mouse delta
            Delta = nextDelta;
            nextDelta = Vector2.Zero;
        }
        
        /// <summary>
        /// Special move that generates pointer events with just delta
        /// </summary>
        /// <param name="delta">The movement delta</param>
        public void HandleMouseDelta(Vector2 delta)
        {
            if (delta == Vector2.Zero)
                return;

            // Normalize delta
            delta *= PointerState.InverseSurfaceSize;
            
            nextDelta += delta;

            var pointerEvent = InputEventPool<PointerEvent>.GetOrCreate(MouseDevice);
            pointerEvent.Position = Position;
            pointerEvent.DeltaPosition = delta;
            pointerEvent.PointerId = 0;
            pointerEvent.EventType = PointerEventType.Moved;

            Events.Add(pointerEvent);
        }

        public void HandleButtonDown(MouseButton button)
        {
            // Prevent duplicate events
            if (downButtons.Contains(button))
                return;

            downButtons.Add(button);

            var buttonEvent = InputEventPool<MouseButtonEvent>.GetOrCreate(MouseDevice);
            buttonEvent.Button = button;
            buttonEvent.IsDown = true;
            Events.Add(buttonEvent);

            // Simulate tap on primary mouse button
            if (button == MouseButton.Left)
                HandlePointerDown();
        }

        public void HandleButtonUp(MouseButton button)
        {
            // Prevent duplicate events
            if (!downButtons.Contains(button))
                return;

            downButtons.Remove(button);

            var buttonEvent = InputEventPool<MouseButtonEvent>.GetOrCreate(MouseDevice);
            buttonEvent.Button = button;
            buttonEvent.IsDown = false;
            Events.Add(buttonEvent);

            // Simulate tap on primary mouse button
            if (button == MouseButton.Left)
                HandlePointerUp();
        }

        public void HandleMouseWheel(float wheelDelta)
        {
            var wheelEvent = InputEventPool<MouseWheelEvent>.GetOrCreate(MouseDevice);
            wheelEvent.WheelDelta = wheelDelta;
            Events.Add(wheelEvent);
        }

        /// <summary>
        /// Handles a single pointer down
        /// </summary>
        public void HandlePointerDown()
        {
            PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent { Type = PointerEventType.Pressed, Position = Position, Id = 0 });
        }

        /// <summary>
        /// Handles a single pointer up
        /// </summary>
        public void HandlePointerUp()
        {
            PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent { Type = PointerEventType.Released, Position = Position, Id = 0 });
        }

        /// <summary>
        /// Handles a single pointer move
        /// </summary>
        /// <param name="newPosition">New position of the pointer</param>
        public void HandleMove(Vector2 newPosition)
        {
            // Normalize position
            newPosition *= PointerState.InverseSurfaceSize;

            if (newPosition != Position)
            {
                nextDelta += newPosition - Position;
                Position = newPosition;

                // Generate Event
                PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent { Type = PointerEventType.Moved, Position = newPosition, Id = 0 });
            }
        }

        private void Reset()
        {
            pressedButtons.Clear();
            releasedButtons.Clear();
        }
    }
}
