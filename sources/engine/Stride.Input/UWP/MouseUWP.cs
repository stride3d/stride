// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_UWP
using System;
using System.Collections.Generic;
using Windows.Foundation;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Games;
using PointUWP = Windows.Foundation.Point;
using Windows.Devices.Input;

namespace Stride.Input
{
    internal class MouseUWP : PointerUWP, IMouseDevice
    {
        private MouseDeviceStateUWP mouseState;
        private bool isPositionLocked;
        private CoreCursor previousCursor;
        
        public MouseUWP(InputSourceUWP source, CoreWindow uiControl)
            : base(source, uiControl)
        {
            mouseState = new MouseDeviceStateUWP(PointerState, this);
            uiControl.PointerWheelChanged += UIControlOnPointerWheelChanged;
        }

        public override void Dispose()
        {
            base.Dispose();
            UIControl.PointerWheelChanged -= UIControlOnPointerWheelChanged;
        }
        
        public override string Name { get; } = "UWP Mouse";

        public override Guid Id { get; } = new Guid("5156D1C2-7B9B-46E7-A54E-CFCC67DA6958");

        public bool IsPositionLocked => isPositionLocked;

        public IReadOnlySet<MouseButton> PressedButtons => mouseState.PressedButtons;
        public IReadOnlySet<MouseButton> ReleasedButtons => mouseState.ReleasedButtons;
        public IReadOnlySet<MouseButton> DownButtons => mouseState.DownButtons;

        public Vector2 Position => mouseState.Position;
        public Vector2 Delta => mouseState.Delta;

        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);
            mouseState.Update(inputEvents);
        }

        protected override void UIControlOnPointerReleased(CoreWindow o, PointerEventArgs args)
        {
            mouseState.HandlePointerReleased(args.CurrentPoint);
        }

        protected override void UIControlOnPointerPressed(CoreWindow o, PointerEventArgs args)
        {
            mouseState.HandlePointerPressed(args.CurrentPoint);
        }

        protected override void UIControlOnPointerMoved(CoreWindow o, PointerEventArgs args)
        {
            if (!isPositionLocked)
            {
                mouseState.HandlePointerMoved(args.CurrentPoint);
            }
        }

        private void UIControlOnPointerWheelChanged(CoreWindow sender, PointerEventArgs args)
        {
            mouseState.HandlePointerWheelChanged(args.CurrentPoint);
        }

        public void SetPosition(Vector2 normalizedPosition)
        {
            var position = normalizedPosition * SurfaceSize;
            UIControl.PointerPosition = new PointUWP(position.X, position.Y);
        }

        private void OnRelativeMouseMoved(MouseDevice sender, MouseEventArgs args)
        {
            mouseState.HandleMouseDelta( new Vector2((float)args.MouseDelta.X, (float)args.MouseDelta.Y));
        }
        
        public void LockPosition(bool forceCenter = false)
        {
            if (!isPositionLocked)
            {
                MouseDevice.GetForCurrentView().MouseMoved += OnRelativeMouseMoved;
                previousCursor = UIControl.PointerCursor;
                UIControl.PointerCursor = null;
                if (forceCenter)
                {
                    var capturedPosition = new PointUWP(UIControl.Bounds.Left, UIControl.Bounds.Top);
                    capturedPosition.X += UIControl.Bounds.Width / 2;
                    capturedPosition.Y += UIControl.Bounds.Height / 2;
                    UIControl.PointerPosition = capturedPosition;
                }
                isPositionLocked = true;
            }
        }

        public void UnlockPosition()
        {
            if (isPositionLocked)
            {
                MouseDevice.GetForCurrentView().MouseMoved -= OnRelativeMouseMoved;
                UIControl.PointerCursor = previousCursor;
                previousCursor = null;
                isPositionLocked = false;
            }
        }
    }
}
#endif
