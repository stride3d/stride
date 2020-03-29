// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Controls.Primitives;
using Microsoft.Xaml.Behaviors;
using Xenko.Assets.Presentation.ViewModel;

namespace Xenko.Assets.Presentation.View.Behaviors
{
    public class ThumbBehavior : Behavior<Thumb>
    {
        public static readonly DependencyProperty DirectionProperty = DependencyProperty.Register(nameof(Direction), typeof(ResizingDirection), typeof(ThumbBehavior));

        public static readonly DependencyProperty TargetProperty = DependencyProperty.Register(nameof(Target), typeof(IResizingTarget), typeof(ThumbBehavior));

        /// <summary>
        /// The direction of the resizing.
        /// </summary>
        public ResizingDirection Direction { get { return (ResizingDirection)GetValue(DirectionProperty); } set { SetValue(DirectionProperty, value); } }

        /// <summary>
        /// The target of the resizing.
        /// </summary>
        public IResizingTarget Target { get { return (IResizingTarget)GetValue(TargetProperty); } set { SetValue(TargetProperty, value); } }

        public bool IsDragging { get; private set; }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.DragStarted += OnDragStarted;
            AssociatedObject.DragDelta += OnDragDelta;
            AssociatedObject.DragCompleted += OnDragCompleted;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.DragCompleted -= OnDragCompleted;
            AssociatedObject.DragDelta -= OnDragDelta;
            AssociatedObject.DragStarted -= OnDragStarted;
        }

        protected virtual void OnDragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (IsDragging) Target?.OnResizingCompleted(Direction, e.HorizontalChange, e.VerticalChange);
            this.IsDragging = false;
        }

        protected virtual void OnDragDelta(object sender, DragDeltaEventArgs e)
        {
            if (IsDragging) Target?.OnResizingDelta(Direction, e.HorizontalChange, e.VerticalChange);
        }

        protected virtual void OnDragStarted(object sender, DragStartedEventArgs e)
        {
            this.IsDragging = true;
            Target?.OnResizingStarted(Direction);
        }
    }
}
