// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_SDL
using System;
using Silk.NET.SDL;
using Stride.Core.Mathematics;
using Point = Stride.Core.Mathematics.Point;
using Window = Stride.Graphics.SDL.Window;

namespace Stride.Input
{
    internal class MouseSDL : MouseDeviceBase, IDisposable
    {
        private readonly Window uiControl;

        private bool isMousePositionLocked;
        private Point relativeCapturedPosition;

        public MouseSDL(InputSourceSDL source, Window uiControl)
        {
            Source = source;
            this.uiControl = uiControl;

            uiControl.MouseMoveActions += OnMouseMoveEvent;
            uiControl.PointerButtonPressActions += OnMouseInputEvent;
            uiControl.PointerButtonReleaseActions += OnMouseInputEvent;
            uiControl.MouseWheelActions += OnMouseWheelEvent;
            uiControl.ResizeEndActions += OnSizeChanged;
            OnSizeChanged(new WindowEvent());

            Id = InputDeviceUtils.DeviceNameToGuid(uiControl.SdlHandle.ToString() + Name);
        }
        
        public override string Name => "SDL Mouse";

        public override Guid Id { get; }

        public override bool IsPositionLocked => isMousePositionLocked;

        public override IInputSource Source { get; }

        public void Dispose()
        {
            uiControl.MouseMoveActions -= OnMouseMoveEvent;
            uiControl.PointerButtonPressActions -= OnMouseInputEvent;
            uiControl.PointerButtonReleaseActions -= OnMouseInputEvent;
            uiControl.MouseWheelActions -= OnMouseWheelEvent;
            uiControl.ResizeEndActions -= OnSizeChanged;
        }

        public override void LockPosition(bool forceCenter = false)
        {
            if (!IsPositionLocked)
            {
                if (forceCenter)
                {
                    relativeCapturedPosition = new Point(uiControl.ClientSize.Width / 2, uiControl.ClientSize.Height / 2);
                }
                else
                {
                    relativeCapturedPosition = uiControl.RelativeCursorPosition;
                }

                uiControl.SetRelativeMouseMode(true);

                isMousePositionLocked = true;
            }
        }

        public override void UnlockPosition()
        {
            if (IsPositionLocked)
            {
                uiControl.SetRelativeMouseMode(false);
                uiControl.RelativeCursorPosition = relativeCapturedPosition;
                isMousePositionLocked = false;
                relativeCapturedPosition = Point.Zero;
            }
        }

        public override void SetPosition(Vector2 normalizedPosition)
        {
            Vector2 position = normalizedPosition * SurfaceSize;
            uiControl.RelativeCursorPosition = new Point((int)position.X, (int)position.Y);
        }
        
        private void OnSizeChanged(WindowEvent eventArgs)
        {
            SetSurfaceSize(new Vector2(uiControl.ClientSize.Width, uiControl.ClientSize.Height));
        }

        private void OnMouseWheelEvent(Silk.NET.SDL.MouseWheelEvent sdlMouseWheelEvent)
        {
            var flip = sdlMouseWheelEvent.Direction == (uint)MouseWheelDirection.MousewheelFlipped ? -1 : 1;
            MouseState.HandleMouseWheel(sdlMouseWheelEvent.Y * flip);
        }

        private void OnMouseInputEvent(Silk.NET.SDL.MouseButtonEvent e)
        {
            MouseButton button = ConvertMouseButton(e.Button);

            if ((EventType)e.Type == EventType.Mousebuttondown)
            {
                MouseState.HandleButtonDown(button);
            }
            else
            {
                MouseState.HandleButtonUp(button);
            }
        }

        private void OnMouseMoveEvent(MouseMotionEvent e)
        {
            if (IsPositionLocked)
            {
                MouseState.HandleMouseDelta(new Vector2(e.Xrel, e.Yrel));
            }
            else
            {
                MouseState.HandleMove(new Vector2(e.X, e.Y));
            }
        }

        private static MouseButton ConvertMouseButton(uint mouseButton)
        {
            switch ((SdlMouseButton)mouseButton)
            {
                case SdlMouseButton.Left:
                    return MouseButton.Left;
                case SdlMouseButton.Right:
                    return MouseButton.Right;
                case SdlMouseButton.Middle:
                    return MouseButton.Middle;
                case SdlMouseButton.X1:
                    return MouseButton.Extended1;
                case SdlMouseButton.X2:
                    return MouseButton.Extended2;
            }

            return (MouseButton)(-1);
        }

        enum SdlMouseButton
        {
            Left = 1,
            Middle = 2,
            Right = 3,
            X1 = 4,
            X2 = 5,
        }
    }
}
#endif
