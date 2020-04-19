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
using System.Runtime.CompilerServices;
using System.Windows;
using Stride.Core.Presentation.Drawing;
using Stride.Assets.Presentation.CurveEditor.ViewModels;

namespace Stride.Assets.Presentation.CurveEditor
{
    using Color = Core.Mathematics.Color;

    /// <summary>
    /// Provides functionality to render horizontal and vertical axes.
    /// </summary>
    internal class HorizontalAndVerticalAxisRenderer
    {
        private IList<double> majorLabelValues;
        private IList<double> majorTickValues;
        private IList<double> minorTickValues;

        public HorizontalAndVerticalAxisRenderer(CurveEditorViewModel editor, IDrawingContext drawingContext)
        {
            Editor = editor;
            DrawingContext = drawingContext;
        }

        /// <summary>
        /// Gets or sets the color of the axis line. The default value is <see cref=" Color.LightGray" />.
        /// </summary>
        public Color AxislineColor { get; set; } = Color.LightGray;

        /// <summary>
        /// Gets or sets the color of the major gridlines. The default value is <c>#40D3D3D3</c>.
        /// </summary>
        public Color MajorColor { get; set; } = new Color(211, 211, 211, 64);

        /// <summary>
        /// Gets or sets the color of the major ticks. The default value is <see cref="Color.LightGray"/>.
        /// </summary>
        public Color MajorTickColor { get; set; } = Color.LightGray;

        /// <summary>
        /// Gets or sets the color of the minor gridlines. The default value is <c>#20D3D3D3</c>.
        /// </summary>
        public Color MinorColor { get; set; } = new Color(211, 211, 211, 32);

        /// <summary>
        /// Gets or sets the color of the minor ticks. The default value is <see cref="Color.LightGray"/>.
        /// </summary>
        public Color MinorTickColor { get; set; } = Color.LightGray;

        /// <summary>
        /// Gets or sets the color of the text. The default is <see cref="Color.LightGray"/>.
        /// </summary>
        public Color TextColor { get; set; } = Color.LightGray;

        protected CurveEditorViewModel Editor { get; }

        /// <summary>
        /// Gets or sets the major label values.
        /// </summary>
        protected IList<double> MajorLabelValues => majorLabelValues;

        /// <summary>
        /// Gets or sets the major tick values.
        /// </summary>
        protected IList<double> MajorTickValues => majorTickValues;

        /// <summary>
        /// Gets or sets the minor tick values.
        /// </summary>
        protected IList<double> MinorTickValues => minorTickValues;

        protected IDrawingContext DrawingContext { get; }

        /// <summary>
        /// Renders the specified axis.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="pass">The pass.</param>
        public void Render(AxisBase axis, int pass)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));
            
            axis.GetTickValues(out majorLabelValues, out majorTickValues, out minorTickValues);
            var totalShift = axis.AxisDistance + axis.PositionTierMinShift;
            var tierSize = axis.PositionTierSize - Editor.AxisTierDistance;

            // store properties locally for performance
            var plotAreaLeft = Editor.CurveArea.Left;
            var plotAreaRight = Editor.CurveArea.Right;
            var plotAreaTop = Editor.CurveArea.Top;
            var plotAreaBottom = Editor.CurveArea.Bottom;

            // Axis position (x or y screen coordinate)
            double axisPosition = 0;
            double titlePosition = 0;

            switch (axis.Position)
            {
                case AxisPosition.Left:
                    axisPosition = plotAreaLeft - totalShift;
                    titlePosition = axisPosition - tierSize;
                    break;

                case AxisPosition.Right:
                    axisPosition = plotAreaRight + totalShift;
                    titlePosition = axisPosition + tierSize;
                    break;

                case AxisPosition.Top:
                    axisPosition = plotAreaTop - totalShift;
                    titlePosition = axisPosition - tierSize;
                    break;

                case AxisPosition.Bottom:
                    axisPosition = plotAreaBottom + totalShift;
                    titlePosition = axisPosition + tierSize;
                    break;

                case AxisPosition.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (pass == 0)
            {
                RenderMinorItems(axis, axisPosition);
            }

            if (pass == 1)
            {
                RenderMajorItems(axis, axisPosition, true);
                RenderAxisTitle(axis, titlePosition);
            }
        }
        
        /// <summary>
        /// Determines whether the specified value is within the specified range.
        /// </summary>
        /// <param name="d">The value to check.</param>
        /// <param name="min">The minimum value of the range.</param>
        /// <param name="max">The maximum value of the range.</param>
        /// <returns><c>true</c> if the specified value is within the range; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsWithin(double d, double min, double max)
        {
            return min <= d && d <= max;
        }

        /// <summary>
        /// Gets the tick positions.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="tickStyle">The tick style.</param>
        /// <param name="tickSize">The tick size.</param>
        /// <param name="position">The position.</param>
        /// <param name="x0">The x 0.</param>
        /// <param name="x1">The x 1.</param>
        protected virtual void GetTickPositions(AxisBase axis, TickStyle tickStyle, double tickSize, AxisPosition position, out double x0, out double x1)
        {
            x0 = 0;
            x1 = 0;
            var isTopOrLeft = position == AxisPosition.Top || position == AxisPosition.Left;
            var sign = isTopOrLeft ? -1 : 1;
            switch (tickStyle)
            {
                case TickStyle.Crossing:
                    x0 = -tickSize * sign * 0.75;
                    x1 = tickSize * sign * 0.75;
                    break;

                case TickStyle.Inside:
                    x0 = -tickSize * sign;
                    break;

                case TickStyle.Outside:
                    x1 = tickSize * sign;
                    break;

                case TickStyle.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(tickStyle), tickStyle, null);
            }
        }

        /// <summary>
        /// Renders the axis title.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="titlePosition">The title position.</param>
        protected virtual void RenderAxisTitle(AxisBase axis, double titlePosition)
        {
            // TODO
        }

        /// <summary>
        /// Renders the major items.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="axisPosition">The axis position.</param>
        /// <param name="drawAxisLine">Draw the axis line if set to <c>true</c>.</param>
        protected virtual void RenderMajorItems(AxisBase axis, double axisPosition, bool drawAxisLine)
        {
            var eps = axis.ActualMinorStep*1e-3;

            var actualMinimum = axis.ActualMinimum;
            var actualMaximum = axis.ActualMaximum;

            var plotAreaLeft = Editor.CurveArea.Left;
            var plotAreaRight = Editor.CurveArea.Right;
            var plotAreaTop = Editor.CurveArea.Top;
            var plotAreaBottom = Editor.CurveArea.Bottom;
            var isHorizontal = axis.IsHorizontal();

            double a0;
            double a1;
            var majorSegments = new List<Point>();
            var majorTickSegments = new List<Point>();
            GetTickPositions(axis, axis.TickStyle, axis.MajorTickSize, axis.Position, out a0, out a1);

            foreach (var value in MajorTickValues)
            {
                if (value < actualMinimum - eps || value > actualMaximum + eps)
                    continue;

                var transformedValue = axis.Transform(value);
                if (isHorizontal)
                {
                    SnapTo(plotAreaLeft, ref transformedValue);
                    SnapTo(plotAreaRight, ref transformedValue);
                }
                else
                {
                    SnapTo(plotAreaTop, ref transformedValue);
                    SnapTo(plotAreaBottom, ref transformedValue);
                }

                if (isHorizontal)
                {
                    majorSegments.Add(new Point(transformedValue, plotAreaTop));
                    majorSegments.Add(new Point(transformedValue, plotAreaBottom));
                }
                else
                {
                    majorSegments.Add(new Point(plotAreaLeft, transformedValue));
                    majorSegments.Add(new Point(plotAreaRight, transformedValue));
                }

                if (axis.TickStyle != TickStyle.None && axis.MajorTickSize > 0)
                {
                    if (isHorizontal)
                    {
                        majorTickSegments.Add(new Point(transformedValue, axisPosition + a0));
                        majorTickSegments.Add(new Point(transformedValue, axisPosition + a1));
                    }
                    else
                    {
                        majorTickSegments.Add(new Point(axisPosition + a0, transformedValue));
                        majorTickSegments.Add(new Point(axisPosition + a1, transformedValue));
                    } 
                }
            }

            // Render the axis labels (numbers or category names)
            var hAlign = HorizontalAlignment.Left;
            var vAlign = VerticalAlignment.Top;
            switch (axis.Position)
            {
                case AxisPosition.Left:
                    hAlign = HorizontalAlignment.Right;
                    vAlign = VerticalAlignment.Center;
                    break;

                case AxisPosition.Right:
                    hAlign = HorizontalAlignment.Left;
                    vAlign = VerticalAlignment.Center;
                    break;

                case AxisPosition.Top:
                    hAlign = HorizontalAlignment.Center;
                    vAlign = VerticalAlignment.Bottom;
                    break;

                case AxisPosition.Bottom:
                    hAlign = HorizontalAlignment.Center;
                    vAlign = VerticalAlignment.Top;
                    break;

                case AxisPosition.None:
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
            var texts = new List<string>();
            var textPositions = new List<Point>();
            foreach (var value in MajorLabelValues)
            {
                if (value < actualMinimum - eps || value > actualMaximum + eps)
                    continue;

                var transformedValue = axis.Transform(value);
                if (isHorizontal)
                {
                    SnapTo(plotAreaLeft, ref transformedValue);
                    SnapTo(plotAreaRight, ref transformedValue);
                }
                else
                {
                    SnapTo(plotAreaTop, ref transformedValue);
                    SnapTo(plotAreaBottom, ref transformedValue);
                }

                var point = new Point();
                switch (axis.Position)
                {
                    case AxisPosition.Left:
                        point = new Point(axisPosition + a1 - axis.AxisTickToLabelDistance, transformedValue);
                        break;

                    case AxisPosition.Right:
                        point = new Point(axisPosition + a1 + axis.AxisTickToLabelDistance, transformedValue);
                        break;

                    case AxisPosition.Top:
                        point = new Point(transformedValue, axisPosition + a1 - axis.AxisTickToLabelDistance);
                        break;

                    case AxisPosition.Bottom:
                        point = new Point(transformedValue, axisPosition + a1 + axis.AxisTickToLabelDistance);
                        break;

                    case AxisPosition.None:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
                
                var text = axis.FormatValue(value);
                texts.Add(text);
                textPositions.Add(point);
            }
            // Batch all the texts together
            DrawingContext.DrawTexts(textPositions, TextColor, texts, axis.FontFamily, axis.FontSize, axis.FontWeight, hAlign, vAlign);

            if (drawAxisLine)
            {
                // Draw the axis line (across the tick marks)
                if (isHorizontal)
                {
                    DrawingContext.DrawLine(new Point(axis.Transform(actualMinimum), axisPosition), new Point(axis.Transform(actualMaximum), axisPosition), AxislineColor);
                }
                else
                {
                    DrawingContext.DrawLine(new Point(axisPosition, axis.Transform(actualMinimum)), new Point(axisPosition, axis.Transform(actualMaximum)), AxislineColor);
                }
            }
            DrawingContext.DrawLineSegments(majorSegments, MajorColor, 2);
            DrawingContext.DrawLineSegments(majorTickSegments, MajorTickColor, 2);
        }

        /// <summary>
        /// Renders the minor items.
        /// </summary>
        /// <param name="axis">The axis.</param>
        /// <param name="axisPosition">The axis position.</param>
        protected virtual void RenderMinorItems(AxisBase axis, double axisPosition)
        {
            var eps = axis.ActualMinorStep*1e-3;
            var actualMinimum = axis.ActualMinimum;
            var actualMaximum = axis.ActualMaximum;

            var plotAreaLeft = Editor.CurveArea.Left;
            var plotAreaRight = Editor.CurveArea.Right;
            var plotAreaTop = Editor.CurveArea.Top;
            var plotAreaBottom = Editor.CurveArea.Bottom;
            var isHorizontal = axis.IsHorizontal();

            double a0;
            double a1;
            var minorSegments = new List<Point>();
            var minorTickSegments = new List<Point>();

            GetTickPositions(axis, axis.TickStyle, axis.MinorTickSize, axis.Position, out a0, out a1);

            foreach (var value in MinorTickValues)
            {
                if (value < actualMinimum - eps || value > actualMaximum + eps)
                {
                    continue;
                }

                if (MajorTickValues.Contains(value))
                {
                    continue;
                }

                var transformedValue = axis.Transform(value);

                if (isHorizontal)
                {
                    SnapTo(plotAreaLeft, ref transformedValue);
                    SnapTo(plotAreaRight, ref transformedValue);
                }
                else
                {
                    SnapTo(plotAreaTop, ref transformedValue);
                    SnapTo(plotAreaBottom, ref transformedValue);
                }

                // Draw the minor grid line
                if (isHorizontal)
                {
                    minorSegments.Add(new Point(transformedValue, plotAreaTop));
                    minorSegments.Add(new Point(transformedValue, plotAreaBottom));
                }
                else
                {
                    minorSegments.Add(new Point(plotAreaLeft, transformedValue));
                    minorSegments.Add(new Point(plotAreaRight, transformedValue));
                }

                // Draw the minor tick
                if (axis.TickStyle != TickStyle.None && axis.MinorTickSize > 0)
                {
                    if (isHorizontal)
                    {
                        minorTickSegments.Add(new Point(transformedValue, axisPosition + a0));
                        minorTickSegments.Add(new Point(transformedValue, axisPosition + a1));
                    }
                    else
                    {
                        minorTickSegments.Add(new Point(axisPosition + a0, transformedValue));
                        minorTickSegments.Add(new Point(axisPosition + a1, transformedValue));
                    } 
                }
            }

            // Draw all the line segments
            DrawingContext.DrawLineSegments(minorSegments, MinorColor);
            DrawingContext.DrawLineSegments(minorTickSegments, MinorTickColor);
        }

        /// <summary>
        /// Snaps v to value if it is within the specified distance.
        /// </summary>
        /// <param name="target">The target value.</param>
        /// <param name="v">The value to snap.</param>
        /// <param name="eps">The distance tolerance.</param>
        protected static void SnapTo(double target, ref double v, double eps = 0.5)
        {
            if (v > target - eps && v < target + eps)
            {
                v = target;
            }
        }
    }
}
