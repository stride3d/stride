// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Stride.Core.Mathematics;
using Stride.Input.RawInput;
using Point = System.Drawing.Point;

namespace Stride.Input
{
    internal class MouseWinforms : MouseDeviceBase, IDisposable
    {
        private readonly Control uiControl;
        private bool isPositionLocked;
        private Point capturedPosition;

        // Stored position for SetPosition
        private Point targetPosition;
        private bool shouldSetPosition;

        private RawInputMouse rawInputMouse = null;

        public MouseWinforms(InputSourceWinforms source, Control uiControl)
        {
            Source = source;
            this.uiControl = uiControl;
            
            uiControl.MouseMove += OnMouseMove;
            uiControl.MouseDown += OnMouseDown;
            uiControl.MouseUp += OnMouseUp;
            uiControl.MouseWheel += OnMouseWheelEvent;
            uiControl.MouseCaptureChanged += OnLostMouseCapture;
            uiControl.SizeChanged += OnSizeChanged;
            uiControl.GotFocus += OnGotFocus;

            OnSizeChanged(this, null);

            BindRawInput();
        }

        public override IInputSource Source { get; }

        public void Dispose()
        {
            uiControl.MouseMove -= OnMouseMove;
            uiControl.MouseDown -= OnMouseDown;
            uiControl.MouseUp -= OnMouseUp;
            uiControl.MouseWheel -= OnMouseWheelEvent;
            uiControl.MouseCaptureChanged -= OnLostMouseCapture;
            uiControl.SizeChanged -= OnSizeChanged;

            if (rawInputMouse != null)
            {
                rawInputMouse.events -= OnMouseMove;
                rawInputMouse.Dispose();
            }
        }

        public override string Name => "Windows Mouse";
        public override Guid Id => new Guid("699e35c5-c363-4bb0-8e8b-0474ea1a5cf1");
        public override bool IsPositionLocked => isPositionLocked;

        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);

            // Set mouse position
            if (shouldSetPosition)
            {
                Cursor.Position = targetPosition;
                shouldSetPosition = false;
            }
        }

        public override void SetPosition(Vector2 normalizedPosition)
        {
            Vector2 position = normalizedPosition * SurfaceSize;

            // Store setting of mouse position since it will keep the message loop goining infinitely otherwise
            var targetPoint = new Point((int)position.X, (int)position.Y);
            targetPosition = uiControl.PointToScreen(targetPoint);
            shouldSetPosition = true;
        }

        public override void LockPosition(bool forceCenter = false)
        {
            if (!isPositionLocked)
            {
                rawInputMouse.Start();
                rawInputMouse.events += OnMouseMove;
                capturedPosition = Cursor.Position;
                if (forceCenter)
                {
                    capturedPosition = uiControl.PointToScreen(new Point(uiControl.ClientSize.Width / 2, uiControl.ClientSize.Height / 2));
                    Cursor.Position = capturedPosition;
                }

                var rect = new Rect(capturedPosition.X, capturedPosition.Y, capturedPosition.X, capturedPosition.Y);
                ClipCursor(rect);

                isPositionLocked = true;
            }
        }

        public override void UnlockPosition()
        {
            if (isPositionLocked)
            {
                if (rawInputMouse != null)
                {
                    rawInputMouse.events -= OnMouseMove;
                    rawInputMouse.End();
                }
                ClipCursor(null);
                isPositionLocked = false;
                capturedPosition = System.Drawing.Point.Empty;
            }
        }

        internal void ForceReleaseButtons()
        {
            foreach (var button in DownButtons.ToArray())
            {
                MouseState.HandleButtonUp(button);
            }
        }

        private void OnGotFocus(object sender, EventArgs e)
        {
            // reapply cursor clip when refocusing
            if (isPositionLocked)
            {
                var rect = new Rect(capturedPosition.X, capturedPosition.Y, capturedPosition.X, capturedPosition.Y);
                ClipCursor(rect);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!isPositionLocked)
            {
                MouseState.HandleMove(new Vector2(e.X, e.Y));
            }
        }

        private void OnMouseMove(object sender, RawInputMouseEventArgs e)
        {
            if (isPositionLocked && e.isRelative)
            {
                MouseState.HandleMouseDelta(new Vector2(e.X, e.Y));
            }
        }

        private void OnSizeChanged(object sender, EventArgs eventArgs)
        {
            SetSurfaceSize(new Vector2(uiControl.ClientSize.Width, uiControl.ClientSize.Height));
        }

        private void OnMouseWheelEvent(object sender, MouseEventArgs mouseEventArgs)
        {
            // The mouse wheel event are still received even when the mouse cursor is out of the control boundaries. Discard the event in this case.
            if (!uiControl.ClientRectangle.Contains(uiControl.PointToClient(Control.MousePosition)))
                return;

            MouseState.HandleMouseWheel((float)mouseEventArgs.Delta / (float)SystemInformation.MouseWheelScrollDelta);
        }

        private void OnMouseUp(object sender, MouseEventArgs mouseEventArgs)
        {
            MouseState.HandleButtonUp(ConvertMouseButton(mouseEventArgs.Button));
        }

        private void OnMouseDown(object sender, MouseEventArgs mouseEventArgs)
        {
            uiControl.Focus();
            MouseState.HandleButtonDown(ConvertMouseButton(mouseEventArgs.Button));
        }

        private void OnLostMouseCapture(object sender, EventArgs args)
        {
            var buttonsToRelease = DownButtons.ToArray();
            foreach (var button in buttonsToRelease)
            {
                MouseState.HandleButtonUp(button);
            }
        }

        private static MouseButton ConvertMouseButton(MouseButtons mouseButton)
        {
            switch (mouseButton)
            {
                case MouseButtons.Left:
                    return MouseButton.Left;
                case MouseButtons.Right:
                    return MouseButton.Right;
                case MouseButtons.Middle:
                    return MouseButton.Middle;
                case MouseButtons.XButton1:
                    return MouseButton.Extended1;
                case MouseButtons.XButton2:
                    return MouseButton.Extended2;
            }
            return (MouseButton)(-1);
        }

        private void BindRawInput()
        {
            if (uiControl.IsHandleCreated)
            {
                rawInputMouse = new RawInputMouse(uiControl.Handle);
            }
            else
            {
                uiControl.HandleCreated += (sender, args) =>
                {
                    if (uiControl.IsHandleCreated)
                    {
                        rawInputMouse = new RawInputMouse(uiControl.Handle);
                    }
                };
            }
        }

        public static unsafe void ClipCursor(Rect? rect)
        {
            if (rect is Rect r)
            {
                Win32.ClipCursor((IntPtr)(&r));
            }
            else
            {
                Win32.ClipCursor(IntPtr.Zero);
            }
        }
    }
}
#endif
