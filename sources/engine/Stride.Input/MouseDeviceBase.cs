// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    /// <summary>
    /// Base class for mouse devices, implements some common functionality of <see cref="IMouseDevice"/>, inherits from <see cref="PointerDeviceBase"/>
    /// </summary>
    public abstract class MouseDeviceBase : PointerDeviceBase, IMouseDevice
    {
        protected MouseDeviceState MouseState;

        private readonly CaptureEdgeTracker<MouseButton> buttonTracker;

        protected MouseDeviceBase()
        {
            MouseState = new MouseDeviceState(PointerState, this);

            buttonTracker = new CaptureEdgeTracker<MouseButton>(CaptureState, MouseState.DownButtons, MouseState.PressedButtons, MouseState.ReleasedButtons);
            CaptureState.MaskChanged += buttonTracker.OnMaskChanged;
            CaptureState.DeviceUpdated += buttonTracker.AfterDeviceUpdate;
            CaptureState.DeviceMasksOwnState = true;
        }

        public abstract bool IsPositionLocked { get; }

        public Core.Collections.IReadOnlySet<MouseButton> PressedButtons => buttonTracker.Pressed;
        public Core.Collections.IReadOnlySet<MouseButton> ReleasedButtons => buttonTracker.Released;
        public Core.Collections.IReadOnlySet<MouseButton> DownButtons => buttonTracker.Down;

        public Vector2 Position => MouseState.Position;
        public Vector2 Delta => CaptureState.IsMasked ? Vector2.Zero : MouseState.Delta;

        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);
            MouseState.Update(inputEvents);
        }
        
        public abstract void SetPosition(Vector2 normalizedPosition);
        
        public abstract void LockPosition(bool forceCenter = false);
        
        public abstract void UnlockPosition();
    }
}
