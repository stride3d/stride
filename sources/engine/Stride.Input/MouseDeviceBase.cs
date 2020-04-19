// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

        protected MouseDeviceBase()
        {
            MouseState = new MouseDeviceState(PointerState, this);
        }

        public abstract bool IsPositionLocked { get; }

        public IReadOnlySet<MouseButton> PressedButtons => MouseState.PressedButtons;
        public IReadOnlySet<MouseButton> ReleasedButtons => MouseState.ReleasedButtons;
        public IReadOnlySet<MouseButton> DownButtons => MouseState.DownButtons;

        public Vector2 Position => MouseState.Position;
        public Vector2 Delta => MouseState.Delta;

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
