// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using Microsoft.Xaml.Behaviors;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;

namespace Xenko.Core.Presentation.Behaviors
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
        }

        protected override void OnDetaching()
        {
            AssociatedObject.SizeChanged -= SizeChanged;
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
