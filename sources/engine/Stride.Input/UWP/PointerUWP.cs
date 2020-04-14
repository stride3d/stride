// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_UWP
using System;
using Windows.Devices.Input;
using Windows.UI.Core;
using Stride.Core.Mathematics;
using PointerPointUWP = Windows.UI.Input.PointerPoint;
using PointUWP = Windows.Foundation.Point;

namespace Stride.Input
{
    internal class PointerUWP : PointerDeviceBase, IDisposable
    {
        protected CoreWindow UIControl;

        public PointerUWP(InputSourceUWP source, CoreWindow uiControl)
        {
            this.UIControl = uiControl;
            Source = source;

            uiControl.SizeChanged += UIControlOnSizeChanged;
            uiControl.PointerMoved += UIControlOnPointerMoved;
            uiControl.PointerPressed += UIControlOnPointerPressed;
            uiControl.PointerReleased += UIControlOnPointerReleased;
            uiControl.PointerExited += UIControlOnPointerExited;
            uiControl.PointerCaptureLost += UIControlOnPointerCaptureLost;

            // Set initial surface size
            SetSurfaceSize(new Vector2((float)uiControl.Bounds.Width, (float)uiControl.Bounds.Height));
        }

        public virtual void Dispose()
        {
            UIControl.SizeChanged -= UIControlOnSizeChanged;
            UIControl.PointerMoved -= UIControlOnPointerMoved;
            UIControl.PointerPressed -= UIControlOnPointerPressed;
            UIControl.PointerReleased -= UIControlOnPointerReleased;
            UIControl.PointerExited -= UIControlOnPointerExited;
            UIControl.PointerCaptureLost -= UIControlOnPointerCaptureLost;
        }

        public override IInputSource Source { get; }

        public override string Name { get; } = "UWP Pointer";

        public override Guid Id { get; } = new Guid("9b1e36b6-de69-4313-89dd-7cbfbe1a436e");

        private void UIControlOnPointerCaptureLost(CoreWindow sender, PointerEventArgs args)
        {
            HandlePointer(PointerEventType.Canceled, args.CurrentPoint);
        }

        private void UIControlOnPointerExited(CoreWindow o, PointerEventArgs args)
        {
            HandlePointer(PointerEventType.Canceled, args.CurrentPoint);
        }

        protected virtual void UIControlOnPointerReleased(CoreWindow o, PointerEventArgs args)
        {
            HandlePointer(PointerEventType.Released, args.CurrentPoint);
        }

        protected virtual void UIControlOnPointerPressed(CoreWindow o, PointerEventArgs args)
        {
            HandlePointer(PointerEventType.Pressed, args.CurrentPoint);
        }

        protected virtual void UIControlOnPointerMoved(CoreWindow o, PointerEventArgs args)
        {
            HandlePointer(PointerEventType.Moved, args.CurrentPoint);
        }


        private void HandlePointer(PointerEventType type, PointerPointUWP point)
        {
            if (point.PointerDevice.PointerDeviceType == PointerDeviceType.Touch || point.PointerDevice.PointerDeviceType == PointerDeviceType.Pen)
            {
                PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent
                {
                    Id = (int)point.PointerId,
                    Position = Normalize(PointToVector2(point.Position)),
                    Type = type
                });
            }
        }

        private void UIControlOnSizeChanged(CoreWindow sender, WindowSizeChangedEventArgs sizeChangedEventArgs)
        {
            var newSize = sizeChangedEventArgs.Size;
            SetSurfaceSize(new Vector2((float)newSize.Width, (float)newSize.Height));
        }
        private Vector2 PointToVector2(PointUWP point)
        {
            return new Vector2((float)point.X, (float)point.Y);
        }
    }
}
#endif
