// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary>
    /// Contains logic to generate pointer events and store the resulting state
    /// </summary>
    public class PointerDeviceState
    {
        public readonly List<InputEvent> PointerInputEvents = new List<InputEvent>();
        public readonly List<PointerData> PointerDatas = new List<PointerData>();

        private readonly HashSet<PointerPoint> pressedPointers = new HashSet<PointerPoint>();
        private readonly HashSet<PointerPoint> releasedPointers = new HashSet<PointerPoint>();
        private readonly HashSet<PointerPoint> downPointers = new HashSet<PointerPoint>();

        private Vector2 surfaceSize;
        private Vector2 invSurfaceSize;
        private float aspectRatio;
        
        public PointerDeviceState(IPointerDevice pointerDevice)
        {
            this.SourceDevice = pointerDevice;
            PressedPointers = new ReadOnlySet<PointerPoint>(pressedPointers);
            ReleasedPointers = new ReadOnlySet<PointerPoint>(releasedPointers);
            DownPointers = new ReadOnlySet<PointerPoint>(downPointers);
        }

        public Vector2 SurfaceSize => surfaceSize;
        public Vector2 InverseSurfaceSize => invSurfaceSize;
        public float SurfaceAspectRatio => aspectRatio;

        public IReadOnlySet<PointerPoint> PressedPointers { get; }
        public IReadOnlySet<PointerPoint> ReleasedPointers { get; }
        public IReadOnlySet<PointerPoint> DownPointers { get; }

        public IPointerDevice SourceDevice;

        /// <summary>
        /// Generate input events
        /// </summary>
        public void Update(List<Input.InputEvent> inputEvents)
        {
            Reset();

            // Turn internal input events into pointer events and mouse position + delta
            foreach (var evt in PointerInputEvents)
            {
                inputEvents.Add(ProcessPointerEvent(evt));
            }
            PointerInputEvents.Clear();
        }

        /// <summary>
        /// Updates the surface size of the pointing device, updates <see cref="SurfaceSize"/>, <see cref="SurfaceAspectRatio"/>, <see cref="invSurfaceSize"/> and calls <see cref="SurfaceSizeChanged"/>
        /// </summary>
        /// <param name="newSize">New size of the surface</param>
        public void SetSurfaceSize(Vector2 newSize)
        {
            surfaceSize = newSize;
            aspectRatio = SurfaceSize.Y / SurfaceSize.X;
            invSurfaceSize = 1.0f / SurfaceSize;
        }

        /// <summary>
        /// Processes a <see cref="InputEvent"/>, converting it to a <see cref="PointerEvent"/>. Also calls <see cref="OnPointer"/> and updates <see cref="CurrentPointerEvents"/>
        /// </summary>
        /// <param name="evt"></param>
        public PointerEvent ProcessPointerEvent(InputEvent evt)
        {
            var pointerEvent = InputEventPool<PointerEvent>.GetOrCreate(SourceDevice);
            pointerEvent.Position = evt.Position;
            pointerEvent.PointerId = evt.Id;
            pointerEvent.EventType = evt.Type;
            UpdatePointerState(pointerEvent);

            return pointerEvent;
        }

        /// <summary>
        /// Updates a pointer event with position / type / id set and updates the storted pointer data
        /// </summary>
        /// <param name="evt"></param>
        public void UpdatePointerState(PointerEvent evt, bool updateDelta = true)
        {
            var data = GetPointerData(evt.PointerId);

            if (updateDelta)
            {
                // Update delta based on change in position
                evt.DeltaPosition = data.Delta = evt.Position - data.Position;
            }
            else
            {
                data.Delta = evt.DeltaPosition;
            }

            // Update position
            data.Position = evt.Position;

            if (evt.EventType == PointerEventType.Pressed)
            {
                // Start pressed events with time 0
                data.Clock.Restart();
                data.IsDown = true;
                pressedPointers.Add(data);
                downPointers.Add(data);
            }
            else if (evt.EventType == PointerEventType.Released || evt.EventType == PointerEventType.Canceled)
            {
                releasedPointers.Add(data);
                downPointers.Remove(data);
                data.IsDown = false;
            }

            evt.IsDown = data.IsDown;
            evt.DeltaTime = data.Clock.Elapsed;

            // Reset pointer clock
            data.Clock.Restart();
        }

        /// <summary>
        /// Retrueves a pointer data structure unqiue to the given pointer ID
        /// </summary>
        /// <param name="pointerId"></param>
        /// <returns></returns>
        public PointerData GetPointerData(int pointerId)
        {
            while (PointerDatas.Count <= pointerId)
            {
                PointerDatas.Add(new PointerData { Pointer = SourceDevice });
            }
            return PointerDatas[pointerId];
        }

        /// <summary>
        /// Resets the state before processing input
        /// </summary>
        private void Reset()
        {
            // Reset delta for all pointers before processing newly received events
            foreach (var pointerData in PointerDatas)
            {
                pointerData.Delta = Vector2.Zero;
            }

            pressedPointers.Clear();
            releasedPointers.Clear();
        }

        /// <summary>
        /// Some additional data kept on top of <see cref="PointerPoint"/> for the purpose of generating <see cref="PointerEvent"/>
        /// </summary>
        public class PointerData : PointerPoint
        {
            public Stopwatch Clock = new Stopwatch();
        }

        /// <summary>
        /// Simplified event data used to generate the full events when <see cref="Update"/> gets called
        /// </summary>
        public struct InputEvent
        {
            public PointerEventType Type;
            public Vector2 Position;
            public Vector2 Delta;
            public int Id;
        }
    }
}
