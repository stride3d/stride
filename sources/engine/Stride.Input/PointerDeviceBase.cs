// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Base class for pointer devices
    /// </summary>
    public abstract class PointerDeviceBase : IPointerDevice
    {
        protected PointerDeviceState PointerState;

        protected PointerDeviceBase()
        {
            PointerState = new PointerDeviceState(this);
        }

        public Vector2 SurfaceSize => PointerState.SurfaceSize;
        public float SurfaceAspectRatio => PointerState.SurfaceAspectRatio;
        public IReadOnlySet<PointerPoint> PressedPointers => PointerState.PressedPointers;
        public IReadOnlySet<PointerPoint> ReleasedPointers => PointerState.ReleasedPointers;
        public IReadOnlySet<PointerPoint> DownPointers => PointerState.DownPointers;
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
    }
}
