// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Xaml.Behaviors;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Behaviors
{
    public sealed class ResizeBehavior : Behavior<FrameworkElement>
    {
        /// <summary>
        /// Identifies the <see cref="SizeRatio"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SizeRatioProperty =
            DependencyProperty.Register(nameof(SizeRatio), typeof(Size), typeof(ResizeBehavior));

        public Size SizeRatio { get { return (Size)GetValue(SizeRatioProperty); } set { SetValue(SizeRatioProperty, value); } }

        protected override void OnAttached()
        {
            AssociatedObject.SizeChanged += SizeChanged;
            AssociatedObject.AddHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChanged));
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SizeChanged -= SizeChanged;
            AssociatedObject.RemoveHandler(ScrollViewer.ScrollChangedEvent, new ScrollChangedEventHandler(ScrollChanged));
        }

        // Text wrapped at the width of the ratio can need more height than the ratio gives: grow by what a scroll viewer inside
        // can't show, as far as the window can grow on its screen; past that, the scroll viewer scrolls
        private void ScrollChanged(object sender, [NotNull] ScrollChangedEventArgs e)
        {
            var overflow = e.ExtentHeight - e.ViewportHeight;
            if (overflow <= 0 || double.IsNaN(AssociatedObject.Height))
                return;

            var maxHeight = AssociatedObject.MaxHeight;
            if (Window.GetWindow(AssociatedObject) is { } window)
            {
                var windowMaxHeight = window.MaxHeight;
                var area = window.GetWorkArea();
                if (area != Rect.Empty)
                    windowMaxHeight = Math.Min(windowMaxHeight, area.Height);
                // The rest of the window (title, buttons, margins) keeps its height
                maxHeight = Math.Min(maxHeight, windowMaxHeight - (window.ActualHeight - AssociatedObject.ActualHeight));
            }
            var height = Math.Min(Math.Ceiling(AssociatedObject.Height + overflow), Math.Floor(maxHeight));
            if (height > AssociatedObject.Height)
                AssociatedObject.Height = height;
        }

        private void SizeChanged(object sender, [NotNull] SizeChangedEventArgs e)
        {
            if (!IsSizeRatioValid() || !e.HeightChanged || !e.WidthChanged)
                return;

            // Measure the required size
            AssociatedObject.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            var desiredSize = AssociatedObject.DesiredSize;
            var surface = desiredSize.Height*desiredSize.Width;

            var width = Math.Round(Math.Sqrt(SizeRatio.Width * surface / SizeRatio.Height));
            width = MathUtil.Clamp(width, AssociatedObject.MinWidth, AssociatedObject.MaxWidth);
            AssociatedObject.Width = width;

            if (width <= AssociatedObject.MinWidth)
            {
                // Keep default value for height
                return;
            }
            var height = Math.Round(SizeRatio.Height * width / SizeRatio.Width);
            height = MathUtil.Clamp(height, AssociatedObject.MinHeight, AssociatedObject.MaxHeight);
            AssociatedObject.Height = height;
        }

        private bool IsSizeRatioValid()
        {
            return !SizeRatio.IsEmpty && !double.IsNaN(SizeRatio.Width) && !double.IsInfinity(SizeRatio.Width) && SizeRatio.Width >= 1
                                      && !double.IsNaN(SizeRatio.Height) && !double.IsInfinity(SizeRatio.Height) && SizeRatio.Height >= 1;
        }
    }
}
