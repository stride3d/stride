// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#region Copyright and license
// Some parts of this file were inspired by OxyPlot (https://github.com/oxyplot/oxyplot)
/*
The MIT license (MTI)
https://opensource.org/licenses/MIT

Copyright (c) 2014 OxyPlot contributors

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, 
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Drawing;
using Stride.Core.Presentation.Extensions;

namespace Stride.Assets.Presentation.CurveEditor.ViewModels
{
    using Color = Core.Mathematics.Color;

    // FIXME: should we move this into a dedicated class?
    partial class CurveEditorViewModel : IDrawingModel
    {
        private static readonly Color[] VectorComponentColor =
        {
            new Color(220, 78, 78),
            new Color(101, 195, 106),
            new Color(63, 116, 209),
            new Color(188, 188, 188)
        };
        
        private IDrawingView canvasView;
        private Rect curveArea;

        /// <summary>
        /// Gets the actual curve area margins.
        /// </summary>
        /// <value>The actual curve area margins.</value>
        public Thickness ActualCurveAreaMargins { get; private set; }

        /// <summary>
        /// Gets or sets the distance between two neighborhood tiers of the same AxisPosition.
        /// </summary>
        public double AxisTierDistance { get; set; }

        /// <summary>
        /// Gets the area including both the curves and the axes.
        /// </summary>
        /// <value>The curve and axis area.</value>
        public Rect CurveAndAxesArea { get; private set; }

        /// <summary>
        /// Gets the curve area. This area is used to draw the series (not including axes).
        /// </summary>
        /// <value>The curve area.</value>
        public Rect CurveArea { get { return curveArea; } private set { SetValue(ref curveArea, value); } }

        /// <summary>
        /// Gets or sets the margins around the curve area (this should be large enough to fit the axes).
        /// If any of the values is set to <c>double.NaN</c>, the margin is adjusted to the value required by the axes.
        /// </summary>
        public Thickness CurveAreaMargins { get; set; }

        /// <summary>
        /// Gets the total height of the curve area (in device units).
        /// </summary>
        public double Height { get; private set; }

        /// <summary>
        /// Gets or sets the padding around the curve area.
        /// </summary>
        /// <value>The padding.</value>
        public Thickness Padding { get; set; }

        /// <summary>
        /// Gets the total width of the curve area (in device units).
        /// </summary>
        public double Width { get; private set; }

        public IEnumerable<AxisBase> Axes
        {
            get
            {
                if (SelectedCurve == null)
                    yield break;

                // TODO: allow curves to have other axes than the two default ones
                // HACK: for some unknown reason, returning the x-axis first causes some issues on the view part
                yield return SelectedCurve.YAxis;
                yield return SelectedCurve.XAxis;
            }
        }

        public Color GetColor(VectorComponent component)
        {
            return VectorComponentColor[(int)component];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void InvalidateView()
        {
            canvasView?.InvalidateDrawing();
        }

        /// <inheritdoc/>
        void IDrawingModel.Attach([NotNull] IDrawingView view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));

            if (ReferenceEquals(canvasView, view))
            {
                // TODO: do something. E.g. exception?
                return;
            }
            canvasView = view;
        }

        /// <inheritdoc/>
        void IDrawingModel.Detach([NotNull] IDrawingView view)
        {
            if (view == null) throw new ArgumentNullException(nameof(view));

            if (!ReferenceEquals(canvasView, view))
            {
                // TODO: do something. E.g. exception?
                return;
            }
            canvasView = null;
        }

        /// <inheritdoc/>
        void IDrawingModel.Render([NotNull] IDrawingContext drawingContext, double width, double height)
        {
            if (drawingContext == null) throw new ArgumentNullException(nameof(drawingContext));

            RenderOverride(drawingContext, width, height);
        }

        /// <inheritdoc/>
        void IDrawingModel.Update(bool updateData)
        {
            UpdateOverride(updateData);
        }
        
        /// <summary>
        /// Renders the canvas with the specified renderer.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="width">The available width.</param>
        /// <param name="height">The available height.</param>
        protected virtual void RenderOverride(IDrawingContext drawingContext, double width, double height)
        {
            // ajust width and height
            var minimumWidth = Padding.Left + Padding.Right;
            var minimumHeight = Padding.Top + Padding.Bottom;
            if (width <= minimumWidth || height <= minimumHeight)
            {
                return;
            }

            Width = width;
            Height = height;

            // calculate curve area margins (i.e. the curve area without the axes)
            ActualCurveAreaMargins = new Thickness(
                double.IsNaN(CurveAreaMargins.Left) ? 0 : CurveAreaMargins.Left,
                double.IsNaN(CurveAreaMargins.Top) ? 0 : CurveAreaMargins.Top,
                double.IsNaN(CurveAreaMargins.Right) ? 0 : CurveAreaMargins.Right,
                double.IsNaN(CurveAreaMargins.Bottom) ? 0 : CurveAreaMargins.Bottom);

            // incrementally adjust curve area, axis transforms and intervals
            while (true)
            {
                UpdateCurveArea();
                UpdateAxisTransforms();
                UpdateIntervals();
                if (!AdjustCurveAreaMargins(drawingContext))
                    break;
            }
            // actual rendering:
            RenderBackgrounds(drawingContext);
            RenderAxes(drawingContext, AxisLayer.BelowCurves);
            RenderCurves(drawingContext);
            RenderBox(drawingContext);
            RenderAxes(drawingContext, AxisLayer.AboveCurves);
        }

        protected virtual void UpdateOverride(bool updateData)
        {
            // Update data of the curve
            if (updateData)
            {
                SelectedCurve?.UpdateData();
            }

            // Update the max and min of the axes
            UpdateMaxMin(updateData);
        }

        partial void InitializeRendering()
        {
            // auto-margins
            CurveAreaMargins = new Thickness(double.NaN);
        }

        /// <summary>
        /// Increases margin size if needed, do it on the specified border.
        /// </summary>
        /// <param name="currentMargin">The current margin.</param>
        /// <param name="minBorderSize">Minimum size of the border.</param>
        /// <param name="borderPosition">The border position.</param>
        private static void EnsureMarginIsBigEnough(ref Thickness currentMargin, double minBorderSize, AxisPosition borderPosition)
        {
            switch (borderPosition)
            {
                case AxisPosition.Bottom:
                    currentMargin = new Thickness(currentMargin.Left, currentMargin.Top, currentMargin.Right, Math.Max(currentMargin.Bottom, minBorderSize));
                    break;

                case AxisPosition.Left:
                    currentMargin = new Thickness(Math.Max(currentMargin.Left, minBorderSize), currentMargin.Top, currentMargin.Right, currentMargin.Bottom);
                    break;

                case AxisPosition.Right:
                    currentMargin = new Thickness(currentMargin.Left, currentMargin.Top, Math.Max(currentMargin.Right, minBorderSize), currentMargin.Bottom);
                    break;

                case AxisPosition.Top:
                    currentMargin = new Thickness(currentMargin.Left, Math.Max(currentMargin.Top, minBorderSize), currentMargin.Right, currentMargin.Bottom);
                    break;

                case AxisPosition.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(borderPosition), borderPosition, null);
            }
        }

        /// <summary>
        /// Calculates the maximum size of the specified axes.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="axesOfPositionTier">The axes of position tier.</param>
        /// <returns>The maximum size.</returns>
        private static double MaxSizeOfPositionTier(IDrawingContext drawingContext, [NotNull] IEnumerable<AxisBase> axesOfPositionTier)
        {
            double maxSizeOfPositionTier = 0;
            foreach (var axis in axesOfPositionTier)
            {
                var size = axis.Measure(drawingContext);
                if (axis.IsVertical())
                {
                    if (size.Width > maxSizeOfPositionTier)
                    {
                        maxSizeOfPositionTier = size.Width;
                    }
                }
                else
                {
                    if (size.Height > maxSizeOfPositionTier)
                    {
                        maxSizeOfPositionTier = size.Height;
                    }
                }
            }

            return maxSizeOfPositionTier;
        }

        /// <summary>
        /// Adjust the positions of parallel axes, returns total size
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="parallelAxes">The parallel axes.</param>
        /// <returns>The maximum value of the position tier??</returns>
        private double AdjustAxesPositions(IDrawingContext drawingContext, [NotNull] IList<AxisBase> parallelAxes)
        {
            double maxValueOfPositionTier = 0;

            foreach (var positionTier in parallelAxes.Select(a => a.PositionTier).Distinct().OrderBy(l => l))
            {
                var axesOfPositionTier = parallelAxes.Where(a => a.PositionTier == positionTier).ToList();
                var maxSizeOfPositionTier = MaxSizeOfPositionTier(drawingContext, axesOfPositionTier);
                var minValueOfPositionTier = maxValueOfPositionTier;

                if (Math.Abs(maxValueOfPositionTier) > 1e-5)
                {
                    maxValueOfPositionTier += AxisTierDistance;
                }

                maxValueOfPositionTier += maxSizeOfPositionTier;

                foreach (var axis in axesOfPositionTier)
                {
                    axis.PositionTierSize = maxSizeOfPositionTier;
                    axis.PositionTierMinShift = minValueOfPositionTier;
                    axis.PositionTierMaxShift = maxValueOfPositionTier;
                }
            }

            return maxValueOfPositionTier;
        }

        /// <summary>
        /// Adjusts the curve area margins.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <returns><c>true</c> if the margins were adjusted; otherwise, <c>false</c>.</returns>
        private bool AdjustCurveAreaMargins(IDrawingContext drawingContext)
        {
            var currentMargin = ActualCurveAreaMargins;

            for (var position = AxisPosition.Left; position <= AxisPosition.Bottom; position++)
            {
                var axesOfPosition = Axes.Where(a => a.Position == position).ToList();
                var requiredSize = AdjustAxesPositions(drawingContext, axesOfPosition);

                if (!IsCurveAreaMarginAutoSized(position))
                {
                    continue;
                }

                EnsureMarginIsBigEnough(ref currentMargin, requiredSize, position);
            }

            if (currentMargin.Equals(ActualCurveAreaMargins))
            {
                return false;
            }

            ActualCurveAreaMargins = currentMargin;
            return true;
        }

        /// <summary>
        /// Determines whether the curve area margin for the specified axis position is auto-sized.
        /// </summary>
        /// <param name="position">The axis position.</param>
        /// <returns><c>true</c> if it is auto-sized.</returns>
        private bool IsCurveAreaMarginAutoSized(AxisPosition position)
        {
            switch (position)
            {
                case AxisPosition.Left:
                    return double.IsNaN(CurveAreaMargins.Left);
                case AxisPosition.Right:
                    return double.IsNaN(CurveAreaMargins.Right);
                case AxisPosition.Top:
                    return double.IsNaN(CurveAreaMargins.Top);
                case AxisPosition.Bottom:
                    return double.IsNaN(CurveAreaMargins.Bottom);
                case AxisPosition.None:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException(nameof(position), position, null);
            }
        }
        
        /// <summary>
        /// Renders the axes.
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <param name="layer">The layer.</param>
        private void RenderAxes(IDrawingContext drawingContext, AxisLayer layer)
        {
            var axisRenderer = new HorizontalAndVerticalAxisRenderer(this, drawingContext);
            // render pass 0
            foreach (var axis in Axes.Where(a => a.Layer == layer))
            {
                axisRenderer.Render(axis, 0);
            }
            // render pass 1
            foreach (var axis in Axes.Where(a => a.Layer == layer))
            {
                axisRenderer.Render(axis, 1);
            }
        }

        /// <summary>
        /// Render the backgrounds.
        /// </summary>
        /// <param name="drawingContext"></param>
        private void RenderBackgrounds(IDrawingContext drawingContext)
        {
            // Nothing for now, but to keep in mind
        }

        /// <summary>
        /// Renders the border around the curve area.
        /// </summary>
        /// <param name="drawingContext">The render.</param>
        /// <remarks>The border will only by rendered if there are axes.</remarks>
        private void RenderBox(IDrawingContext drawingContext)
        {
            if (SelectedCurve != null)
            {
                drawingContext.DrawRectangle(CurveArea, Color.Transparent, Color.Thistle);
            }
        }

        /// <summary>
        /// Render the curves.
        /// </summary>
        /// <param name="drawingContext"></param>
        private void RenderCurves(IDrawingContext drawingContext)
        {
            // Only renders the selected curve
            SelectedCurve?.Render(drawingContext, true);
        }

        /// <summary>
        /// Updates the axis transforms.
        /// </summary>
        private void UpdateAxisTransforms()
        {
            // Update the transforms for all axes
            foreach (var a in Axes)
            {
                a.UpdateTransform(CurveArea);
            }
        }

        /// <summary>
        /// Calculates the curve area (subtract padding).
        /// </summary>
        private void UpdateCurveArea()
        {
            var curveArea = new Rect(Padding.Left, Padding.Top, Width - Padding.Left - Padding.Right, Height - Padding.Top - Padding.Bottom);

            curveArea = curveArea.Deflate(ActualCurveAreaMargins);
            // Ensure the curve area is valid
            if (curveArea.Height < 0)
            {
                curveArea = new Rect(curveArea.Left, curveArea.Top, curveArea.Width, 1);
            }

            if (curveArea.Width < 0)
            {
                curveArea = new Rect(curveArea.Left, curveArea.Top, 1, curveArea.Height);
            }
            CurveArea = curveArea;
            CurveAndAxesArea = curveArea.Inflate(ActualCurveAreaMargins);
        }

        /// <summary>
        /// Updates the intervals (major and minor step values).
        /// </summary>
        private void UpdateIntervals()
        {
            // Update the intervals for all axes
            foreach (var a in Axes)
            {
                a.UpdateIntervals(CurveArea);
            }
        }

        /// <summary>
        /// Updates maximum and minimum values of the axes from values of all data series.
        /// </summary>
        /// <param name="isDataUpdated">if set to <c>true</c>, the data has been updated.</param>
        private void UpdateMaxMin(bool isDataUpdated)
        {
            if (isDataUpdated)
            {
                foreach (var axis in Axes)
                {
                    axis.ResetDataMaxMin();
                }

                // data has been updated, so we need to calculate the max/min of the curves again
                SelectedCurve?.UpdateMaxMin();
            }
            
            SelectedCurve?.UpdateAxisMaxMin();

            foreach (var axis in Axes)
            {
                axis.UpdateActualMaxMin();
            }
        }
    }
}
