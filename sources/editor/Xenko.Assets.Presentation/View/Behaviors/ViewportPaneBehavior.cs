// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Xenko.Assets.Presentation.AssetEditors.SpriteEditor.ViewModels;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.View.Behaviors
{
    class ViewportPaneBehavior : Behavior<UIElement>
    {
        public static readonly DependencyProperty ModifiersProperty =
               DependencyProperty.Register(nameof(Modifiers), typeof(ModifierKeys), typeof(ViewportPaneBehavior), new PropertyMetadata(ModifierKeys.Control));

        public static readonly DependencyProperty ViewportProperty =
               DependencyProperty.Register(nameof(Viewport), typeof(ViewportViewModel), typeof(ViewportPaneBehavior));

        private Point previousPosition;
        private Point originPoint;

        public ModifierKeys Modifiers { get { return (ModifierKeys)GetValue(ModifiersProperty); } set { SetValue(ModifiersProperty, value); } }

        public ViewportViewModel Viewport { get { return (ViewportViewModel)GetValue(ViewportProperty); } set { SetValue(ViewportProperty, value); } }

        public bool IsPanning { get; private set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.MouseDown += OnMouseDown;
            AssociatedObject.MouseMove += OnMouseMove;
            AssociatedObject.MouseLeftButtonUp += OnMouseLeftButtonUp;
            AssociatedObject.LostMouseCapture += OnLostMouseCapture;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.MouseDown -= OnMouseDown;
            AssociatedObject.MouseMove -= OnMouseMove;
            AssociatedObject.MouseLeftButtonUp -= OnMouseLeftButtonUp;
            AssociatedObject.LostMouseCapture -= OnLostMouseCapture;
            base.OnDetaching();
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (IsPanning)
                return;

            if (Viewport == null || !IsInputValid(e.MouseDevice))
                return;

            e.Handled = true;
            AssociatedObject.Focus();
            AssociatedObject.CaptureMouse();
            IsPanning = true;
            originPoint = e.GetPosition(AssociatedObject);
            previousPosition = originPoint;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (!IsPanning)
                return;

            if (Viewport != null && IsInputValid(e.MouseDevice))
            {
                var position = e.GetPosition(AssociatedObject);
                var movement = position - previousPosition;
                if (movement.LengthSquared > double.Epsilon)
                {
                    previousPosition = position;
                    e.Handled = true;
                    Viewport.HorizontalOffset += movement.X;
                    Viewport.VerticalOffset += movement.Y;
                }
            }
            else
            {
                if (ReferenceEquals(e.MouseDevice.Captured, AssociatedObject))
                    AssociatedObject.ReleaseMouseCapture();
                IsPanning = false;
                originPoint.X = 0;
                originPoint.Y = 0;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsInputValid(MouseDevice mouseDevice)
        {
            return mouseDevice.MiddleButton == MouseButtonState.Pressed && Keyboard.Modifiers.HasFlag(Modifiers);
        }
    }
}
