// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_ANDROID
using System;
using System.Collections.Generic;
using Android.Views;
using Stride.Core.Mathematics;
using Stride.Games.Android;

namespace Stride.Input
{
    internal class PointerAndroid : PointerDeviceBase, IDisposable
    {
        private readonly AndroidStrideGameView uiControl;

        public PointerAndroid(InputSourceAndroid source, AndroidStrideGameView uiControl)
        {
            Source = source;
            this.uiControl = uiControl;
            var listener = new Listener(this);
            uiControl.Resize += OnResize;
            uiControl.SetOnTouchListener(listener);

            OnResize(this, null);
        }

        public void Dispose()
        {
            uiControl.Resize -= OnResize;
            uiControl.SetOnTouchListener(null);
        }

        public override string Name => "Android Pointer";

        public override Guid Id => new Guid("21370b00-aaf9-4ecf-afb2-575dde6c6c56");

        public override IInputSource Source { get; }

        private void OnResize(object sender, EventArgs eventArgs)
        {
            SetSurfaceSize(new Vector2(uiControl.Size.Width, uiControl.Size.Height));
        }

        private void AddPointerEvent(PointerDeviceState.InputEvent evt)
        { 
            PointerState.PointerInputEvents.Add(evt);
        }

        protected class Listener : Java.Lang.Object, View.IOnTouchListener
        {
            private readonly PointerAndroid pointer;

            public Listener(PointerAndroid pointer)
            {
                this.pointer = pointer;
            }

            public bool OnTouch(View v, MotionEvent e)
            {
                // Choose action type
                PointerEventType actionType;
                switch (e.ActionMasked)
                {
                    case MotionEventActions.Down:
                    case MotionEventActions.Pointer1Down:
                        actionType = PointerEventType.Pressed;
                        break;

                    case MotionEventActions.Outside:
                    case MotionEventActions.Cancel:
                        actionType = PointerEventType.Canceled;
                        break;

                    case MotionEventActions.Up:
                    case MotionEventActions.Pointer1Up:
                        actionType = PointerEventType.Released;
                        break;

                    default:
                        actionType = PointerEventType.Moved;
                        break;
                }

                if (actionType != PointerEventType.Moved)
                {
                    pointer.AddPointerEvent(new PointerDeviceState.InputEvent
                    {
                        Id = e.GetPointerId(e.ActionIndex),
                        Position = pointer.Normalize(new Vector2(e.GetX(e.ActionIndex), e.GetY(e.ActionIndex))),
                        Type = actionType
                    });
                }
                else
                {
                    for (int i = 0; i < e.PointerCount; i++)
                    {
                        pointer.AddPointerEvent(new PointerDeviceState.InputEvent
                        {
                            Id = e.GetPointerId(i),
                            Position = pointer.Normalize(new Vector2(e.GetX(i), e.GetY(i))),
                            Type = actionType
                        });
                    }
                }

                return true;
            }
        }
    }
}
#endif
