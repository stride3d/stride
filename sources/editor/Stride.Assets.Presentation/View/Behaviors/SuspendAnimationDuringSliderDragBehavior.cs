// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;

using Stride.Assets.Presentation.ViewModel;

namespace Stride.Assets.Presentation.View.Behaviors
{
    class SuspendAnimationDuringSliderDragBehavior : Behavior<Slider>
    {
        public static readonly DependencyProperty AnimatedPreviewViewModelProperty = DependencyProperty.Register("AnimatedPreviewViewModel", typeof(IAnimatedPreviewViewModel), typeof(SuspendAnimationDuringSliderDragBehavior));

        public IAnimatedPreviewViewModel AnimatedPreviewViewModel { get { return (IAnimatedPreviewViewModel)GetValue(AnimatedPreviewViewModelProperty); } set { SetValue(AnimatedPreviewViewModelProperty, value); } }

        private bool wasPlaying;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)DragStarted);
            AssociatedObject.AddHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)DragCompleted);
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)TrackMouseEvent, true);
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)TrackMouseEvent, true);
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(Thumb.DragStartedEvent, (DragStartedEventHandler)DragStarted);
            AssociatedObject.RemoveHandler(Thumb.DragCompletedEvent, (DragCompletedEventHandler)DragCompleted);
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)TrackMouseEvent);
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)TrackMouseEvent);
            base.OnDetaching();
        }

        private void TrackMouseEvent(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && e.ButtonState == MouseButtonState.Pressed)
            {
                wasPlaying = AnimatedPreviewViewModel.IsPlaying;
                if (wasPlaying)
                    AnimatedPreviewViewModel.PauseCommand.Execute();
            }
        }

        void DragStarted(object sender, DragStartedEventArgs e)
        {
            if (AnimatedPreviewViewModel != null)
            {
                // wasPlaying can have been set by TrackMouseEvent
                wasPlaying = wasPlaying || AnimatedPreviewViewModel.IsPlaying;
                if (wasPlaying)
                    AnimatedPreviewViewModel.PauseCommand.Execute();
            }
        }

        void DragCompleted(object sender, DragCompletedEventArgs e)
        {
            if (AnimatedPreviewViewModel != null)
            {
                if (wasPlaying)
                    AnimatedPreviewViewModel.PlayCommand.Execute();
            }
            wasPlaying = false;
        }
    }
}
