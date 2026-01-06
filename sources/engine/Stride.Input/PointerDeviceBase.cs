// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Base class for pointer devices
    /// </summary>
    public abstract class PointerDeviceBase : IPointerDevice
    {
        private readonly Dictionary<(long touchId, long fingerId), int> touchFingerIndexMap = new Dictionary<(long touchId, long fingerId), int>();
        private int touchCounter;

        protected PointerDeviceState PointerState;

        protected PointerDeviceBase()
        {
            PointerState = new PointerDeviceState(this);
        }

        public Vector2 SurfaceSize => PointerState.SurfaceSize;
        public float SurfaceAspectRatio => PointerState.SurfaceAspectRatio;
        public Core.Collections.IReadOnlySet<PointerPoint> PressedPointers => PointerState.PressedPointers;
        public Core.Collections.IReadOnlySet<PointerPoint> ReleasedPointers => PointerState.ReleasedPointers;
        public Core.Collections.IReadOnlySet<PointerPoint> DownPointers => PointerState.DownPointers;
        public event EventHandler<SurfaceSizeChangedEventArgs> SurfaceSizeChanged;

        public int Priority { get; set; }

        public abstract string Name { get; }
        public abstract Guid Id { get; }
        public abstract IInputSource Source { get; }

        public virtual void Update(List<InputEvent> inputEvents)
        {
            PointerState.Update(inputEvents);
        }

        /// <summary>
        /// Calls <see cref="PointerDeviceState.SetSurfaceSize"/> and invokes the <see cref="SurfaceSizeChanged"/> event
        /// </summary>
        /// <param name="newSize">New size of the surface</param>
        protected void SetSurfaceSize(Vector2 newSize)
        {
            PointerState.SetSurfaceSize(newSize);
            SurfaceSizeChanged?.Invoke(this, new SurfaceSizeChangedEventArgs { NewSurfaceSize = newSize });
        }

        protected Vector2 Normalize(Vector2 position)
        {
            return position * PointerState.InverseSurfaceSize;
        }

        protected int GetFingerId(long touchId, long fingerId, PointerEventType type)
        {
            // Assign finger index (starting at 0) to touch ID
            int touchFingerIndex = 0;
            var key = (touchId, fingerId);
            if (type == PointerEventType.Pressed)
            {
                touchFingerIndex = touchCounter++;
                touchFingerIndexMap[key] = touchFingerIndex;
            }
            else
            {
                touchFingerIndexMap.TryGetValue(key, out touchFingerIndex);
            }

            // Remove index
            if (type == PointerEventType.Released && touchFingerIndexMap.Remove(key))
            {
                touchCounter = 0; // Reset touch counter

                // Recalculate next finger index
                if (touchFingerIndexMap.Count > 0)
                {
                    touchFingerIndexMap.ForEach(pair => touchCounter = Math.Max(touchCounter, pair.Value));
                    touchCounter++; // next
                }
            }

            return touchFingerIndex;
        }
    }
}
