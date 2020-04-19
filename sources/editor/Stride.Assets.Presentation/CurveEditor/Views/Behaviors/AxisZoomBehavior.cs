// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows;
using System.Windows.Input;

namespace Stride.Assets.Presentation.CurveEditor.Views.Behaviors
{
    class AxisZoomBehavior : AxisBehavior
    {
        private double zoomFactor = 1.0;

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
        }

        /// <inheritdoc/>
        protected override void OnDetaching()
        {
            AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        }

        private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var hasX = HasXModifiers();
            var hasY = HasYModifiers();
            if (!hasX && !hasY)
                return;

            if (e.Delta > 0)
            {
                ZoomFactorIn();
            }
            if (e.Delta < 0)
            {
                ZoomFactorOut();
            }

            var position = e.MouseDevice.GetPosition((IInputElement)sender);
            var current = InverseTransform(position);
            if (hasX)
            {
                XAxis?.ZoomAt(zoomFactor, current.X);
            }
            if (hasY)
            {
                YAxis?.ZoomAt(zoomFactor, current.Y);
            }
            DrawingView?.InvalidateDrawing();
            e.Handled = true;
        }

        private Point InverseTransform(Point point)
        {
            if (XAxis != null)
            {
                return XAxis.InverseTransform(point.X, point.Y, YAxis);
            }

            if (YAxis != null)
            {
                return new Point(0, YAxis.InverseTransform(point.Y));
            }

            return new Point();
        }

        private void ZoomFactorIn()
        {
            zoomFactor = 1.25;
        }

        private void ZoomFactorOut()
        {
            zoomFactor = 0.8;
        }
    }
}
