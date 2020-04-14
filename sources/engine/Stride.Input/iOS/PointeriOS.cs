// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_IOS
using System;
using System.Collections.Generic;
using CoreGraphics;
using Foundation;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Games;
using UIKit;

namespace Stride.Input
{
    /// <summary>
    /// iOS pointer device
    /// </summary>
    internal class PointeriOS : PointerDeviceBase, IDisposable
    {
        private readonly StrideGameController gameController;
        private readonly iOSWindow uiControl;
        private readonly Dictionary<int, int> touchFingerIndexMap = new Dictionary<int, int>();
        private int touchCounter;

        public PointeriOS(InputSourceiOS source, iOSWindow uiControl, StrideGameController gameController)
        {
            Source = source;
            this.uiControl = uiControl;
            this.gameController = gameController;
            var window = uiControl.MainWindow;
            window.UserInteractionEnabled = true;
            window.MultipleTouchEnabled = true;
            uiControl.GameView.MultipleTouchEnabled = true;
            gameController.TouchesBeganDelegate += Touched;
            gameController.TouchesMovedDelegate += Touched;
            gameController.TouchesEndedDelegate += Touched;
            gameController.TouchesCancelledDelegate += Touched;
            uiControl.GameView.Resize += OnResize;

            OnResize(null, EventArgs.Empty);
        }
       
        public override string Name => "iOS Pointer";

        public override Guid Id => new Guid("6fa378ee-1ffe-41c1-947a-b425adcd5258");

        public override IInputSource Source { get; }

        public void Dispose()
        {
            gameController.TouchesBeganDelegate -= Touched;
            gameController.TouchesMovedDelegate -= Touched;
            gameController.TouchesEndedDelegate -= Touched;
            gameController.TouchesCancelledDelegate -= Touched;
            uiControl.GameView.Resize -= OnResize;
        }

        private void Touched(NSSet touchesSet, UIEvent evt)
        {
            if (touchesSet != null)
            {
                // Convert touches to pointer events
                foreach (var item in touchesSet)
                {
                    var uitouch = (UITouch)item;
                    var pointerEvent = new PointerDeviceState.InputEvent();
                    var touchId = uitouch.Handle.ToInt32();

                    pointerEvent.Position = Normalize(CGPointToVector2(uitouch.LocationInView(uiControl.GameView)));
                    switch (uitouch.Phase)
                    {
                        case UITouchPhase.Began:
                            pointerEvent.Type = PointerEventType.Pressed;
                            break;

                        case UITouchPhase.Moved:
                        case UITouchPhase.Stationary:
                            pointerEvent.Type = PointerEventType.Moved;
                            break;

                        case UITouchPhase.Ended:
                            pointerEvent.Type = PointerEventType.Released;
                            break;

                        case UITouchPhase.Cancelled:
                            pointerEvent.Type = PointerEventType.Canceled;
                            break;

                        default:
                            throw new ArgumentException("Got an invalid Touch event in GetState");
                    }

                    // Assign finger index (starting at 0) to touch ID
                    int touchFingerIndex = 0;
                    if (pointerEvent.Type == PointerEventType.Pressed)
                    {
                        touchFingerIndex = touchCounter++;
                        touchFingerIndexMap.Add(touchId, touchFingerIndex);
                    }
                    else
                    {
                        touchFingerIndex = touchFingerIndexMap[touchId];
                    }

                    // Remove index
                    if (pointerEvent.Type == PointerEventType.Released)
                    {
                        touchFingerIndexMap.Remove(touchId);
                        touchCounter = 0; // Reset touch counter

                        // Recalculate next finger index
                        if (touchFingerIndexMap.Count > 0)
                        {
                            touchFingerIndexMap.ForEach(pair => touchCounter = Math.Max(touchCounter, pair.Value));
                            touchCounter++; // next
                        }
                    }

                    pointerEvent.Id = touchFingerIndex;
                    PointerState.PointerInputEvents.Add(pointerEvent);
                }
            }
        }
        
        private void OnResize(object sender, EventArgs eventArgs)
        {
            SetSurfaceSize(new Vector2(
                (float)uiControl.GameView.Frame.Width,
                (float)uiControl.GameView.Frame.Height));
        }

        private Vector2 CGPointToVector2(CGPoint point)
        {
            return new Vector2((float)point.X, (float)point.Y);
        }
    }
}
#endif