// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_UWP
using Windows.Devices.Input;
using Windows.UI.Input;
using Stride.Core.Mathematics;

namespace Stride.Input
{
    internal class MouseDeviceStateUWP : MouseDeviceState
    {
        public MouseDeviceStateUWP(PointerDeviceState pointerState, IMouseDevice mouseDevice) : base(pointerState, mouseDevice)
        {
        }

        public void HandlePointerWheelChanged(Windows.UI.Input.PointerPoint point)
        {
            if (point.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse)
            {
                HandleMouseWheel(point.Properties.MouseWheelDelta / 120.0f);
            }
        }

        public void HandlePointerMoved(Windows.UI.Input.PointerPoint pointerPoint)
        {
            if (pointerPoint.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse)
            {
                var newPosition = new Vector2((float)pointerPoint.Position.X, (float)pointerPoint.Position.Y);
                HandleMove(newPosition);
            }
        }

        public void HandlePointerPressed(Windows.UI.Input.PointerPoint point)
        {
            if (point.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse)
            {
                switch (point.Properties.PointerUpdateKind)
                {
                    case PointerUpdateKind.LeftButtonPressed:
                        HandleButtonDown(MouseButton.Left);
                        break;
                    case PointerUpdateKind.RightButtonPressed:
                        HandleButtonDown(MouseButton.Right);
                        break;
                    case PointerUpdateKind.MiddleButtonPressed:
                        HandleButtonDown(MouseButton.Middle);
                        break;
                    case PointerUpdateKind.XButton1Pressed:
                        HandleButtonDown(MouseButton.Extended1);
                        break;
                    case PointerUpdateKind.XButton2Pressed:
                        HandleButtonDown(MouseButton.Extended2);
                        break;
                }
            }
        }

        public void HandlePointerReleased(Windows.UI.Input.PointerPoint point)
        {
            if (point.PointerDevice.PointerDeviceType == PointerDeviceType.Mouse)
            {
                switch (point.Properties.PointerUpdateKind)
                {
                    case PointerUpdateKind.LeftButtonReleased:
                        HandleButtonUp(MouseButton.Left);
                        break;
                    case PointerUpdateKind.RightButtonReleased:
                        HandleButtonUp(MouseButton.Right);
                        break;
                    case PointerUpdateKind.MiddleButtonReleased:
                        HandleButtonUp(MouseButton.Middle);
                        break;
                    case PointerUpdateKind.XButton1Released:
                        HandleButtonUp(MouseButton.Extended1);
                        break;
                    case PointerUpdateKind.XButton2Released:
                        HandleButtonUp(MouseButton.Extended2);
                        break;
                }
            }
        }
    }
}
#endif
