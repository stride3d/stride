// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Extensions;
using Stride.Core.Presentation.Internal;

// Remark: The drag'n'drop is pretty broken in WPF, especially the DragLeave event (see https://social.msdn.microsoft.com/Forums/vstudio/en-US/d326384b-e182-4f48-ab8b-841a2c2ca4ab/whats-up-with-dragleave-and-egetposition?forum=wpf&prof=required)

namespace Stride.Core.Presentation.Behaviors
{
    public class DragOverAutoScrollBehavior : Behavior<Control>
    {
        private readonly object lockObject = new object();
        private bool scrollStarted;
        private CancellationTokenSource cancellationTokenSource;
        private Dock? edgeUnderMouse;

        public static readonly DependencyProperty ScrollBorderThicknessProperty = DependencyProperty.Register("ScrollBorderThickness", typeof(Thickness), typeof(DragOverAutoScrollBehavior), new PropertyMetadata(new Thickness(32)));

        public static readonly DependencyProperty DelaySecondsProperty = DependencyProperty.Register("DelaySeconds", typeof(double), typeof(DragOverAutoScrollBehavior), new PropertyMetadata(0.5));

        public static readonly DependencyProperty ScrollingSpeedWidthProperty = DependencyProperty.Register("ScrollingSpeed", typeof(double), typeof(DragOverAutoScrollBehavior), new PropertyMetadata(300.0));

        public static readonly DependencyProperty VerticalScrollProperty = DependencyProperty.Register("VerticalScroll", typeof(bool), typeof(DragOverAutoScrollBehavior), new PropertyMetadata(BooleanBoxes.TrueBox));

        public static readonly DependencyProperty HorizontalScrollProperty = DependencyProperty.Register("HorizontalScroll", typeof(bool), typeof(DragOverAutoScrollBehavior), new PropertyMetadata(BooleanBoxes.TrueBox));

        public Thickness ScrollBorderThickness { get { return (Thickness)GetValue(ScrollBorderThicknessProperty); } set { SetValue(ScrollBorderThicknessProperty, value); } }
        
        public double DelaySeconds { get { return (double)GetValue(DelaySecondsProperty); } set { SetValue(DelaySecondsProperty, value); } }

        public double ScrollingSpeed { get { return (double)GetValue(ScrollingSpeedWidthProperty); } set { SetValue(ScrollingSpeedWidthProperty, value); } }

        public bool VerticalScroll { get { return (bool)GetValue(VerticalScrollProperty); } set { SetValue(VerticalScrollProperty, value.Box()); } }
        
        public bool HorizontalScroll { get { return (bool)GetValue(HorizontalScrollProperty); } set { SetValue(HorizontalScrollProperty, value.Box()); } }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.AddHandler(UIElement.PreviewDragOverEvent, (DragEventHandler)DragOver);
            AssociatedObject.AddHandler(UIElement.DragLeaveEvent, (DragEventHandler)DragLeave);
            AssociatedObject.AddHandler(UIElement.DropEvent, (DragEventHandler)Drop);
        }

        private void Drop(object sender, DragEventArgs e)
        {
            StopScroll();
        }

        protected override void OnDetaching()
        {
            AssociatedObject.RemoveHandler(UIElement.PreviewDragOverEvent, (DragEventHandler)DragOver);
            AssociatedObject.RemoveHandler(UIElement.DragLeaveEvent, (DragEventHandler)DragLeave);
            AssociatedObject.RemoveHandler(UIElement.DropEvent, (DragEventHandler)Drop);
            base.OnDetaching();
        }

        private void DragOver(object sender, DragEventArgs e)
        {
            var position = AssociatedObject.GetCursorRelativePosition();
            lock (lockObject)
            {
                edgeUnderMouse = GetEdgeUnderMouse(position);
            }
            if (edgeUnderMouse != null)
            {
                StartScroll();
            }
        }

        private void DragLeave(object sender, DragEventArgs e)
        {
            var position = AssociatedObject.GetCursorRelativePosition();
            if (position.X <= 0 || position.Y <= 0 || position.X >= AssociatedObject.ActualWidth || position.Y >= AssociatedObject.ActualHeight)
            {
                edgeUnderMouse = null;
            }
        }

        private void StopScroll()
        {
            if (!scrollStarted)
                return;

            cancellationTokenSource.Cancel();
            scrollStarted = false;
        }

        private void StartScroll()
        {
            if (scrollStarted)
                return;

            cancellationTokenSource = new CancellationTokenSource();
            var scrollViewer = AssociatedObject.FindVisualChildOfType<ScrollViewer>();
            var delaySeconds = DelaySeconds;
            if (scrollViewer != null)
            {
                scrollStarted = true;
                Task.Run(() => ScrollTask(scrollViewer, delaySeconds), cancellationTokenSource.Token);
            }
        }

        private async Task ScrollTask([NotNull] ScrollViewer scrollViewer, double delaySeconds)
        {
            const int refreshDelay = 25;

            await Task.Delay((int)TimeSpan.FromSeconds(delaySeconds).TotalMilliseconds);

            while (scrollStarted)
            {
                    Dispatcher.Invoke(() =>
                    {
                        if (edgeUnderMouse.HasValue)
                        {
                            var offset = ScrollingSpeed * refreshDelay / 1000.0;
                            switch (edgeUnderMouse.Value)
                            {
                                case Dock.Left:
                                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - offset);
                                    break;
                                case Dock.Top:
                                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - offset);
                                    break;
                                case Dock.Right:
                                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset + offset);
                                    break;
                                case Dock.Bottom:
                                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + offset);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                        }
                        else
                            StopScroll();
                    });
                await Task.Delay(refreshDelay);
            }
        }

        private Dock? GetEdgeUnderMouse(Point point)
        {
            var scrollViewer = AssociatedObject.FindVisualChildOfType<ScrollViewer>();
            if (point.X >= 0 && point.X <= ScrollBorderThickness.Left)
                return Dock.Left;
            if (point.X <= scrollViewer.RenderSize.Width && scrollViewer.RenderSize.Width - point.X <= ScrollBorderThickness.Right)
                return Dock.Right;
            if (point.Y >= 0 && point.Y <= ScrollBorderThickness.Top)
                return Dock.Top;
            if (point.Y <= scrollViewer.RenderSize.Height && scrollViewer.RenderSize.Height - point.Y <= ScrollBorderThickness.Bottom)
                return Dock.Bottom;

            return null;
        }
    }
}
