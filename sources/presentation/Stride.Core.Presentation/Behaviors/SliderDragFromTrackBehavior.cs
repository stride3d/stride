// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Behaviors
{
    public class SliderDragFromTrackBehavior : Behavior<Slider>
    {
        private bool trackMouseDown;
        private Track track;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)TrackMouseEvent, true);
            AssociatedObject.AddHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)TrackMouseEvent, true);
            AssociatedObject.Initialized += SliderInitialized;
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Initialized -= SliderInitialized;
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonDownEvent, (MouseButtonEventHandler)TrackMouseEvent);
            AssociatedObject.RemoveHandler(UIElement.PreviewMouseLeftButtonUpEvent, (MouseButtonEventHandler)TrackMouseEvent);
            if (track != null && track.Thumb != null)
            {
                track.Thumb.MouseEnter -= MouseEnter;
            }
            base.OnDetaching();
        }

        private void SliderInitialized(object sender, EventArgs e)
        {
            AssociatedObject.ApplyTemplate();

            track = AssociatedObject.FindVisualChildOfType<Track>();
            if (track == null || track.Name != "PART_Track")
                throw new InvalidOperationException("The associated slider must have a Track child named 'PART_Track'");
            track.Thumb.MouseEnter += MouseEnter;
            AssociatedObject.Initialized += SliderInitialized;
        }

        private void TrackMouseEvent(object sender, [NotNull] MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                trackMouseDown = e.ButtonState == MouseButtonState.Pressed;
        }

        private void MouseEnter(object sender, [NotNull] MouseEventArgs e)
        {
            if (trackMouseDown)
            {
                var args = new MouseButtonEventArgs(e.MouseDevice, e.Timestamp, MouseButton.Left) { RoutedEvent = UIElement.MouseLeftButtonDownEvent };
                track.Thumb.RaiseEvent(args);
                trackMouseDown = false;
            }
        }
    }
}
