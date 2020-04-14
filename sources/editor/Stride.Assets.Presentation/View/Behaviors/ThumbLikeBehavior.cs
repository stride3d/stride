// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using Xenko.Core.Presentation.Behaviors;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.View.Behaviors
{
    public sealed class ThumbLikeBehavior : MouseMoveCaptureBehaviorBase<UIElement>
    {
        /// <summary>
        /// Identifies the <see cref="Direction"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(nameof(Direction), typeof(ResizingDirection), typeof(ThumbLikeBehavior));
        /// <summary>
        /// Identifies the <see cref="Target"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(nameof(Target), typeof(IResizingTarget), typeof(ThumbLikeBehavior));
        /// <summary>
        /// Identifies the <see cref="Reference"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ReferenceProperty = DependencyProperty.Register(nameof(Reference), typeof(UIElement), typeof(ThumbLikeBehavior));

        private Point previousPosition;
        private Point originPoint;

        /// <summary>
        /// The direction of the resizing.
        /// </summary>
        public ResizingDirection Direction { get { return (ResizingDirection)GetValue(DirectionProperty); } set { SetValue(DirectionProperty, value); } }

        /// <summary>
        /// The target of the resizing.
        /// </summary>
        public IResizingTarget Target { get { return (IResizingTarget)GetValue(TargetProperty); } set { SetValue(TargetProperty, value); } }

        /// <summary>
        /// The reference from which deltas are calculated.
        /// When null, <see cref="Microsoft.Xaml.Behaviors.Behavior.AssociatedObject"/> will be used instead.
        /// </summary>
        public UIElement Reference { get { return (UIElement)GetValue(ReferenceProperty); } set { SetValue(ReferenceProperty, value); } }

        public bool IsDragging { get; private set; }

        private UIElement ActualReference => Reference ?? AssociatedObject;

        ///  <inheritdoc/>
        protected override void CancelOverride()
        {
            if (!IsDragging)
                return;

            IsDragging = false;
            var totalMovement = previousPosition - originPoint;
            Target?.OnResizingCompleted(Direction, totalMovement.X, totalMovement.Y);
        }

        ///  <inheritdoc/>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            if (!AreModifiersValid() || e.ChangedButton != MouseButton.Left)
                return;

#if DEBUG
            if (IsInProgress) throw new InvalidOperationException("Got MouseLeftButtonDown event while dragging!");
#endif // DEBUG

            e.Handled = true;
            CaptureMouse();

            originPoint = e.GetPosition(ActualReference);
            previousPosition = originPoint;

            Target?.OnResizingStarted(Direction);
        }

        ///  <inheritdoc/>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (!AreModifiersValid() || e.MouseDevice.LeftButton != MouseButtonState.Pressed)
            {
                Cancel();
                return;
            }

            var position = e.GetPosition(ActualReference);
            if (!IsDragging)
            {
                var dragDelta = position - originPoint;
                if (Math.Abs(dragDelta.X) > SystemParameters.MinimumHorizontalDragDistance ||
                    Math.Abs(dragDelta.Y) > SystemParameters.MinimumVerticalDragDistance)
                {
                    IsDragging = true;
                }
            }
            if (IsDragging)
            {
                var movement = position - previousPosition;
                if (movement.LengthSquared > double.Epsilon)
                {
                    previousPosition = position;
                    Target?.OnResizingDelta(Direction, movement.X, movement.Y);
                }
            }
            e.Handled = true;
        }

        ///  <inheritdoc/>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            if (!AreModifiersValid() || e.ChangedButton != MouseButton.Left)
                return;

            e.Handled = true;
            ReleaseMouseCapture();

            if (!IsDragging)
                return;

            IsDragging = false;
            var finalPosition = e.GetPosition(ActualReference);
            var totalMovement = finalPosition - originPoint;
            Target?.OnResizingCompleted(Direction, totalMovement.X, totalMovement.Y);
        }
    }
}
