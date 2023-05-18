// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_UI_WINFORMS
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Games;
using static Stride.Games.TouchUtils;

namespace Stride.Input
{
    /// <summary>
    /// Class handling finger touch inputs using the Winforms backend
    /// </summary>
    internal class PointerWinforms : PointerDeviceBase, IDisposable
    {
        private readonly GameForm uiControl;

        public PointerWinforms(InputSourceWinforms source, Control uiControl)
        {
            Source = source;
            this.uiControl = uiControl as GameForm;

            this.uiControl.FingerMoveActions += OnFingerMoveEvent;
            this.uiControl.FingerPressActions += OnFingerPressEvent;
            this.uiControl.FingerReleaseActions += OnFingerReleaseEvent;

            uiControl.Resize += OnSizeChanged;
            OnSizeChanged(uiControl, EventArgs.Empty);

            Id = InputDeviceUtils.DeviceNameToGuid(uiControl.Handle.ToString() + Name);
        }

        public override string Name => "Winforms Pointer";

        public override Guid Id { get; }

        public override IInputSource Source { get; }

        public void Dispose()
        {
            uiControl.FingerMoveActions -= OnFingerMoveEvent;
            uiControl.FingerPressActions -= OnFingerPressEvent;
            uiControl.FingerReleaseActions -= OnFingerReleaseEvent;

            uiControl.Resize -= OnSizeChanged;
        }

        private void OnSizeChanged(object sender, EventArgs eventArgs)
        {
            SetSurfaceSize(new Vector2(uiControl.ClientSize.Width, uiControl.ClientSize.Height));
        }

        private void HandleFingerEvent(POINTER_TOUCH_INFO e, PointerEventType type)
        {
            var pointerInfo = e.pointerInfo;
            var point = uiControl.PointToClient(new System.Drawing.Point(pointerInfo.ptPixelLocationX, pointerInfo.ptPixelLocationY));
            var newPosition = new Vector2(point.X, point.Y);
            var id = GetFingerId(pointerInfo.sourceDevice.ToInt64(), pointerInfo.pointerId, type);

            PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent { Type = type, Position = newPosition, Id = id });
        }

        private void OnFingerMoveEvent(POINTER_TOUCH_INFO e)
        {
            HandleFingerEvent(e, PointerEventType.Moved);
        }

        private void OnFingerPressEvent(POINTER_TOUCH_INFO e)
        {
            HandleFingerEvent(e, PointerEventType.Pressed);
        }

        private void OnFingerReleaseEvent(POINTER_TOUCH_INFO e)
        {
            HandleFingerEvent(e, PointerEventType.Released);
        }
    }
}
#endif
