// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Input;

namespace Stride.Assets.Presentation.CurveEditor.Views.Behaviors
{
    class AxisPaneBehavior : AxisBehavior
    {
        /// <summary>
        /// Identifies the <see cref="MouseButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MouseButtonProperty =
               DependencyProperty.Register(nameof(MouseButton), typeof(MouseButton), typeof(AxisPaneBehavior), new PropertyMetadata(MouseButton.Middle));

        private Point previousPosition;

        public MouseButton MouseButton { get { return (MouseButton)GetValue(MouseButtonProperty); } set { SetValue(MouseButtonProperty, value); } }

        public bool IsPanning { get; private set; }

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            AssociatedObject.MouseDown += OnMouseDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
            AssociatedObject.LostMouseCapture += OnLostMouseCapture;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            AssociatedObject.MouseDown -= OnMouseDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            AssociatedObject.LostMouseCapture -= OnLostMouseCapture;
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsPanning)
                return;

            if (!IsInputValid(e.MouseDevice))
                return;

            e.Handled = true;
            AssociatedObject.Focus();
            AssociatedObject.CaptureMouse();
            IsPanning = true;
            var originPoint = e.GetPosition(AssociatedObject);
            previousPosition = originPoint;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsPanning)
                return;

            if (IsInputValid(e.MouseDevice))
            {
                var position = e.GetPosition(AssociatedObject);
                if (HasXModifiers())
                {
                    XAxis?.Pan(previousPosition, position);
                }
                if (HasYModifiers())
                {
                    YAxis?.Pan(previousPosition, position);
                }
                previousPosition = position;
                DrawingView?.InvalidateDrawing();
                e.Handled = true;
            }
            else
            {
                if (ReferenceEquals(e.MouseDevice.Captured, AssociatedObject))
                    AssociatedObject.ReleaseMouseCapture();
                IsPanning = false;
            }
        }

        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (AssociatedObject.IsMouseCaptured && IsPanning)
            {
                e.Handled = true;
                IsPanning = false;
                AssociatedObject.ReleaseMouseCapture();
            }
        }

        private void OnLostMouseCapture(object sender, MouseEventArgs e)
        {
            var obj = (UIElement)sender;

            if (!ReferenceEquals(Mouse.Captured, obj))
            {
                CancelPanning();
            }
        }

        private void CancelPanning()
        {
            if (!IsPanning)
                return;

            IsPanning = false;
            if (AssociatedObject.IsMouseCaptured)
            {
                AssociatedObject.ReleaseMouseCapture();
            }
        }
        
        private bool IsInputValid(MouseDevice mouseDevice)
        {
            MouseButtonState state;
            switch (MouseButton)
            {
                case MouseButton.Left:
                    state = mouseDevice.LeftButton;
                    break;

                case MouseButton.Middle:
                    state = mouseDevice.MiddleButton;
                    break;

                case MouseButton.Right:
                    state = mouseDevice.RightButton;
                    break;

                case MouseButton.XButton1:
                    state = mouseDevice.XButton1;
                    break;

                case MouseButton.XButton2:
                    state = mouseDevice.XButton2;
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            return state == MouseButtonState.Pressed && (HasXModifiers() || HasYModifiers());
        }
    }
}
