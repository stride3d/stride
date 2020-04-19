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
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Stride.Core.Presentation.Drawing;

namespace Stride.Assets.Presentation.CurveEditor
{
    /// <summary>
    /// Specifies the layer of an <see cref="AxisBase" />.
    /// </summary>
    public enum AxisLayer
    {
        /// <summary>
        /// Below all curves.
        /// </summary>
        BelowCurves,

        /// <summary>
        /// Above all curves.
        /// </summary>
        AboveCurves
    }

    /// <summary>
    /// Specifies the position of an <see cref="AxisBase" />.
    /// </summary>
    public enum AxisPosition
    {
        /// <summary>
        /// No position.
        /// </summary>
        None,

        /// <summary>
        /// Left of the curve area.
        /// </summary>
        Left,

        /// <summary>
        /// Right of the curve area.
        /// </summary>
        Right,

        /// <summary>
        /// Top of the curve area.
        /// </summary>
        Top,

        /// <summary>
        /// Bottom of the curve area.
        /// </summary>
        Bottom
    }

    /// <summary>
    /// Defines the style of axis ticks.
    /// </summary>
    public enum TickStyle
    {
        /// <summary>
        /// The ticks are rendered crossing the axis line.
        /// </summary>
        Crossing,

        /// <summary>
        /// The ticks are rendered inside of the curve area.
        /// </summary>
        Inside,

        /// <summary>
        /// The ticks are rendered Outside the curve area.
        /// </summary>
        Outside,

        /// <summary>
        /// The ticks are not rendered.
        /// </summary>
        None
    }

    public abstract class AxisBase
    {
        /// <summary>
        /// Gets or sets the absolute maximum. This is only used for the UI control. It will not be possible to zoom/pan beyond this limit.
        /// The default value is <c>double.MaxValue</c>.
        /// </summary>
        public double AbsoluteMaximum { get; set; } = double.MaxValue;

        /// <summary>
        /// Gets or sets the absolute minimum. This is only used for the UI control. It will not be possible to zoom/pan beyond this limit.
        /// The default value is <c>double.MinValue</c>.
        /// </summary>
        public double AbsoluteMinimum { get; set; } = double.MinValue;

        /// <summary>
        /// Gets or sets the actual major step.
        /// </summary>
        public double ActualMajorStep { get; protected set; }

        /// <summary>
        /// Gets or sets the actual maximum value of the axis.
        /// </summary>
        /// <remarks>If <see cref="ViewMaximum" /> is not <c>NaN</c>, this value will be defined by <see cref="ViewMaximum" />.
        /// Otherwise, if <see cref="Maximum" /> is not <c>NaN</c>, this value will be defined by <see cref="Maximum" />.
        /// Otherwise, this value will be defined by the maximum (+padding) of the data.</remarks>
        public double ActualMaximum { get; protected set; }

        /// <summary>
        /// Gets or sets the actual minimum value of the axis.
        /// </summary>
        /// <remarks>If <see cref="ViewMinimum" /> is not <c>NaN</c>, this value will be defined by <see cref="ViewMinimum" />.
        /// Otherwise, if <see cref="Minimum" /> is not <c>NaN</c>, this value will be defined by <see cref="Minimum" />.
        /// Otherwise this value will be defined by the minimum (+padding) of the data.</remarks>
        public double ActualMinimum { get; protected set; }

        /// <summary>
        /// Gets or sets the actual minor step.
        /// </summary>
        public double ActualMinorStep { get; protected set; }

        /// <summary>
        /// Gets or sets the distance between the plot area and the axis.
        /// The default value is <c>0</c>.
        /// </summary>
        public double AxisDistance { get; set; }

        /// <summary>
        /// Gets or sets the distance from the end of the tick lines to the labels.
        /// The default value is <c>4</c>.
        /// </summary>
        public double AxisTickToLabelDistance { get; set; } = 4;

        /// <summary>
        /// Gets or sets the maximum value of the data displayed on this axis.
        /// </summary>
        public double DataMaximum { get; protected set; }

        /// <summary>
        /// Gets or sets the minimum value of the data displayed on this axis.
        /// </summary>
        public double DataMinimum { get; protected set; }

        /// <summary>
        /// Gets or sets the end position of the axis on the plot area. The default value is <c>1</c>.
        /// </summary>
        /// <remarks>The position is defined by a fraction in the range from <c>0</c> to <c>1</c>, where <c>0</c> is at the bottom/left
        /// and <c>1</c> is at the top/right. </remarks>
        public double EndPosition { get; set; } = 1;

        /// <summary>
        /// Gets or sets the font. The default is Arial.
        /// </summary>
        public FontFamily FontFamily { get; } = new FontFamily("Arial");

        /// <summary>
        /// Gets or sets the size of the font. The default is <c>12</c>.
        /// </summary>
        public int FontSize { get; } = 12;

        /// <summary>
        /// Gets or sets the font weight. The default is <see cref="FontWeights.Normal"/>.
        /// </summary>
        public FontWeight FontWeight { get; set; } = FontWeights.Normal;

        /// <summary>
        /// Gets or sets the maximum length (screen space) of the intervals.
        /// The available length of the axis will be divided by this length to get the approximate number of major intervals on the axis.
        /// The default value is <c>60</c>.
        /// </summary>
        public double IntervalLength { get; protected set; } = 60;

        /// <summary>
        /// Determines whether the axis is used for X/Y values.
        /// </summary>
        /// <returns><c>true</c> if it is an XY axis; otherwise, <c>false</c> .</returns>
        public abstract bool IsXyAxis { get; }

        /// <summary>
        /// Gets or sets the layer of the axis. The default value is <see cref="AxisLayer.BelowCurves"/>.
        /// </summary>
        public AxisLayer Layer { get; set; } = AxisLayer.BelowCurves;

        /// <summary>
        /// Gets or sets the maximum value of this axis. The default value is <c>double.NaN</c>.
        /// </summary>
        public double Maximum { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the 'padding' fraction of the maximum value. The default value is <c>0.05</c>.
        /// </summary>
        /// <remarks>
        /// A value of 0.05 gives 5% more space on the maximum end of the axis.
        /// This property is not used if the <see cref="Maximum" /> property is set.
        /// </remarks>
        public double MaximumPadding { get; set; } = 0.05;

        /// <summary>
        /// Gets or sets the maximum range of the axis. Setting this property ensures that <c>ActualMaximum-ActualMinimum &lt; MaximumRange</c>.
        /// The default value is <c>double.PositiveInfinity</c>.
        /// </summary>
        public double MaximumRange { get; set; } = double.PositiveInfinity;

        /// <summary>
        /// Gets or sets the interval between major ticks. The default value is <c>double.NaN</c>.
        /// </summary>
        public double MajorStep { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the size of the major ticks. The default value is <c>7</c>.
        /// </summary>
        public double MajorTickSize { get; set; } = 7;

        /// <summary>
        /// Gets or sets the minimum value of this axis. The default value is <c>double.NaN</c>.
        /// </summary>
        public double Minimum { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the 'padding' fraction of the minimum value. The default value is <c>0.05</c>.
        /// </summary>
        /// <remarks>
        /// A value of 0.05 gives 5% more space on the minimum end of the axis.
        /// This property is not used if the <see cref="Minimum" /> property is set.
        /// </remarks>
        public double MinimumPadding { get; set; } = 0.05;

        /// <summary>
        /// Gets or sets the minimum range of the axis. Setting this property ensures that <c>ActualMaximum-ActualMinimum > MinimumRange</c>.
        /// The default value is <c>0</c>.
        /// </summary>
        public double MinimumRange { get; set; }

        /// <summary>
        /// Gets or sets the interval between minor ticks. The default value is <c>double.NaN</c>.
        /// </summary>
        public double MinorStep { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the size of the minor ticks. The default value is <c>4</c>.
        /// </summary>
        public double MinorTickSize { get; set; } = 4;

        /// <summary>
        /// Gets the offset. This is used to transform between data and relative screen coordinates.
        /// </summary>
        public double Offset { get; private set; }

        /// <summary>
        /// Gets or sets the position of the axis. The default value is <see cref="AxisPosition.Left"/>.
        /// </summary>
        public AxisPosition Position { get; set; } = AxisPosition.Left;

        /// <summary>
        /// Gets or sets the position tier which defines in which tier the axis is displayed. The default value is <c>0</c>.
        /// </summary>
        /// <remarks>The bigger the value the further afar is the axis from the graph.</remarks>
        public int PositionTier { get; set; }

        /// <summary>
        /// Gets the scaling factor of the axis. This is used to transform between data and relative screen coordinates.
        /// </summary>
        public double Scale { get; private set; }

        /// <summary>
        /// Gets or sets the screen coordinate of the maximum end of the axis.
        /// </summary>
        public Point ScreenMax { get; protected set; }

        /// <summary>
        /// Gets or sets the screen coordinate of the minimum end of the axis.
        /// </summary>
        public Point ScreenMin { get; protected set; }

        /// <summary>
        /// Gets or sets the start position of the axis on the plot area. The default value is <c>0</c>.
        /// </summary>
        /// <remarks>The position is defined by a fraction in the range from <c>0</c> to <c>1</c>, where <c>0</c> is at the bottom/left
        /// and <c>1</c> is at the top/right. </remarks>
        public double StartPosition { get; set; }
        
        /// <summary>
        /// Gets or sets the string format used for formatting the axis values. The default value is <c>null</c>.
        /// </summary>
        public string StringFormat { get; set; }

        /// <summary>
        /// Gets or sets the tick style for major and minor ticks.
        /// The default value is <see cref="CurveEditor.TickStyle.Outside"/>.
        /// </summary>
        public TickStyle TickStyle { get; set; } = TickStyle.Outside;

        /// <summary>
        /// Gets or sets the position tier max shift.
        /// </summary>
        internal double PositionTierMaxShift { get; set; }

        /// <summary>
        /// Gets or sets the position tier min shift.
        /// </summary>
        internal double PositionTierMinShift { get; set; }

        /// <summary>
        /// Gets or sets the size of the position tier.
        /// </summary>
        internal double PositionTierSize { get; set; }

        /// <summary>
        /// Gets or sets the current view's maximum. The default value is <c>double.NaN</c>.
        /// </summary>
        /// <remarks>
        /// This value is used when the user zooms or pans.
        /// </remarks>
        protected internal double ViewMaximum { get; protected set; } = double.NaN;

        /// <summary>
        /// Gets or sets the current view's minimum. The default value is <c>double.NaN</c>.
        /// </summary>
        /// <remarks>
        /// This value is used when the user zooms or pans.
        /// </remarks>
        protected internal double ViewMinimum { get; protected set; } = double.NaN;

        /// <summary>
        /// Occurs when the axis has been changed (by zooming, panning or resetting).
        /// </summary>
        public event EventHandler<AxisChangedEventArgs> AxisChanged;

        /// <summary>
        /// Occurs when the transform changed (size or axis range was changed).
        /// </summary>
        public event EventHandler TransformChanged;

        /// <summary>
        /// Creates tick values at the specified interval.
        /// </summary>
        /// <param name="from">The start value.</param>
        /// <param name="to">The end value.</param>
        /// <param name="step">The interval.</param>
        /// <param name="maxTicks">The maximum number of ticks (optional). The default value is 1000.</param>
        /// <returns>A sequence of values.</returns>
        /// <exception cref="System.ArgumentException">Step cannot be zero or negative.;step</exception>
        public static IList<double> CreateTickValues(double from, double to, double step, int maxTicks = 1000)
        {
            if (step <= 0)
            {
                throw new ArgumentException("Step cannot be zero or negative.", nameof(step));
            }

            if (to <= from && step > 0)
            {
                step *= -1;
            }

            var startValue = Math.Round(from / step) * step;
            var numberOfValues = Math.Max((int)((to - from) / step), 1);
            var epsilon = step * 1e-3 * Math.Sign(step);
            var values = new List<double>(numberOfValues);

            for (var k = 0; k < maxTicks; k++)
            {
                var lastValue = startValue + (step * k);

                // If we hit the maximum value before reaching the max number of ticks, exit
                if (lastValue > to + epsilon)
                {
                    break;
                }

                // try to get rid of numerical noise
                var v = Math.Round(lastValue / step, 14) * step;
                values.Add(v);
            }

            return values;
        }

        /// <summary>
        /// Center the axis on the given screen coordinates.
        /// </summary>
        /// <param name="point">The screen corrdinates.</param>
        public void Center(Point point)
        {
            if (IsHorizontal())
            {
                var delta = (ScreenMax.X - ScreenMin.X) * 0.5 - point.X;
                Pan(delta);
            }
            else if (IsVertical())
            {
                var delta = (ScreenMax.Y - ScreenMin.Y) * 0.5 - point.Y;
                Pan(delta);
            }
        }

        /// <summary>
        /// Coerces the actual maximum and minimum values.
        /// </summary>
        public virtual void CoerceActualMaxMin()
        {
            // Coerce actual minimum
            if (double.IsNaN(ActualMinimum) || double.IsInfinity(ActualMinimum))
            {
                ActualMinimum = 0;
            }

            // Coerce actual maximum
            if (double.IsNaN(ActualMaximum) || double.IsInfinity(ActualMaximum))
            {
                ActualMaximum = 1.0; // normalized
            }

            if (ActualMaximum <= ActualMinimum)
            {
                ActualMaximum = ActualMinimum + 1.0; // normalized
            }
        }

        /// <summary>
        /// Formats the value to be used on the axis.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <returns>The formatted value.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public string FormatValue(double x)
        {
            return x.ToString(StringFormat, CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Gets the coordinates used to draw ticks and tick labels (numbers or category names).
        /// </summary>
        /// <param name="majorLabelValues">The major label values.</param>
        /// <param name="majorTickValues">The major tick values.</param>
        /// <param name="minorTickValues">The minor tick values.</param>
        public virtual void GetTickValues(out IList<double> majorLabelValues, out IList<double> majorTickValues, out IList<double> minorTickValues)
        {
            minorTickValues = CreateTickValues(ActualMinimum, ActualMaximum, ActualMinorStep);
            majorTickValues = CreateTickValues(ActualMinimum, ActualMaximum, ActualMajorStep);
            majorLabelValues = majorTickValues;
        }

        /// <summary>
        /// Inverse transforms the specified relative screen coordinate.
        /// </summary>
        /// <param name="sx">The relative screen coordinate.</param>
        /// <param name="isVector">True if the transform is a vector transform (i.e. only applies scale, not translation). The default value is False.</param>
        /// <returns>The value.</returns>
        public virtual double InverseTransform(double sx, bool isVector = false)
        {
            return PostInverseTransform((sx / Scale) + (isVector ? 0 : Offset));
        }

        /// <summary>
        /// Inverse transform the specified screen point.
        /// </summary>
        /// <param name="x">The x coordinate.</param>
        /// <param name="y">The y coordinate.</param>
        /// <param name="yaxis">The y-axis.</param>
        /// <param name="isVector">True if the transform is a vector transform (i.e. only applies scale, not translation). The default value is False.</param>
        /// <returns>The data point.</returns>
        public virtual Point InverseTransform(double x, double y, AxisBase yaxis, bool isVector = false)
        {
            if (yaxis == null) throw new ArgumentNullException(nameof(yaxis), "Y axis should not be null when transforming.");
            return new Point(InverseTransform(x, isVector), yaxis.InverseTransform(y, isVector));
        }

        /// <summary>
        /// Determines whether the axis is horizontal.
        /// </summary>
        /// <returns><c>true</c> if the axis is horizontal; otherwise, <c>false</c> .</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsHorizontal()
        {
            return Position == AxisPosition.Top || Position == AxisPosition.Bottom;
        }

        /// <summary>
        /// Determines whether the specified value is valid.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns><c>true</c> if the specified value is valid; otherwise, <c>false</c>.</returns>
        public bool IsValidValue(double value)
        {
            return !double.IsNaN(value) &&
                !double.IsPositiveInfinity(value) &&
                !double.IsNegativeInfinity(value);
        }

        /// <summary>
        /// Determines whether the axis is vertical.
        /// </summary>
        /// <returns><c>true</c> if the axis is vertical; otherwise, <c>false</c> .</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsVertical()
        {
            return Position == AxisPosition.Left || Position == AxisPosition.Right;
        }

        /// <summary>
        /// Modifies the data range of the axis [DataMinimum,DataMaximum] to includes the specified value.
        /// </summary>
        /// <param name="value">The value.</param>
        public virtual void Include(double value)
        {
            if (!IsValidValue(value))
                return;

            DataMinimum = double.IsNaN(DataMinimum) ? value : Math.Min(DataMinimum, value);
            DataMaximum = double.IsNaN(DataMaximum) ? value : Math.Max(DataMaximum, value);
        }

        /// <summary>
        /// Measures the size of the axis (maximum axis label width/height).
        /// </summary>
        /// <param name="drawingContext">The drawing context.</param>
        /// <returns>The size of the axis.</returns>
        public virtual Size Measure(IDrawingContext drawingContext)
        {
            IList<double> majorTickValues;
            IList<double> minorTickValues;
            IList<double> majorLabelValues;

            GetTickValues(out majorLabelValues, out majorTickValues, out minorTickValues);
            
            var texts = majorLabelValues.Select(FormatValue).ToList();
            var maximumTextSize = drawingContext.MeasureTexts(texts, FontFamily, FontSize, FontWeight);
            var width = 0.0;
            var height = 0.0;
            if (IsVertical())
            {
                switch (TickStyle)
                {
                    case TickStyle.Outside:
                        width += MajorTickSize;
                        break;

                    case TickStyle.Crossing:
                        width += MajorTickSize * 0.75;
                        break;
                }
                width += MajorTickSize;
                width += AxisDistance;
                width += AxisTickToLabelDistance;
                width += maximumTextSize.Width;
            }
            else
            {
                switch (TickStyle)
                {
                    case TickStyle.Outside:
                        height += MajorTickSize;
                        break;
                    case TickStyle.Crossing:
                        height += MajorTickSize * 0.75;
                        break;
                }
                height += AxisDistance;
                height += AxisTickToLabelDistance;
                height += maximumTextSize.Height;
            }

            return new Size(width, height);
        }
        
        /// <summary>
        /// Pans the specified axis.
        /// </summary>
        /// <param name="ppt">The previous point (screen coordinates).</param>
        /// <param name="cpt">The current point (screen coordinates).</param>
        public virtual void Pan(Point ppt, Point cpt)
        {
            var delta = IsHorizontal() ? cpt.X - ppt.X : cpt.Y - ppt.Y;
            Pan(delta);
        }

        /// <summary>
        /// Pans the specified axis.
        /// </summary>
        /// <param name="delta">The delta.</param>
        public virtual void Pan(double delta)
        {
            var oldMinimum = ActualMinimum;
            var oldMaximum = ActualMaximum;

            var dx = delta / Scale;

            var newMinimum = ActualMinimum - dx;
            var newMaximum = ActualMaximum - dx;
            if (newMinimum < AbsoluteMinimum)
            {
                newMinimum = AbsoluteMinimum;
                newMaximum = Math.Min(newMinimum + ActualMaximum - ActualMinimum, AbsoluteMaximum);
            }

            if (newMaximum > AbsoluteMaximum)
            {
                newMaximum = AbsoluteMaximum;
                newMinimum = Math.Max(newMaximum - (ActualMaximum - ActualMinimum), AbsoluteMinimum);
            }

            ViewMinimum = newMinimum;
            ViewMaximum = newMaximum;
            UpdateActualMaxMin();

            var deltaMinimum = ActualMinimum - oldMinimum;
            var deltaMaximum = ActualMaximum - oldMaximum;

            AxisChanged?.Invoke(this, new AxisChangedEventArgs(AxisChangeTypes.Pan, deltaMinimum, deltaMaximum));
        }

        /// <summary>
        /// Resets the user's modification (zooming/panning) to minimum and maximum of this axis.
        /// </summary>
        public virtual void Reset()
        {
            var oldMinimum = ActualMinimum;
            var oldMaximum = ActualMaximum;

            ViewMinimum = double.NaN;
            ViewMaximum = double.NaN;
            UpdateActualMaxMin();

            var deltaMinimum = ActualMinimum - oldMinimum;
            var deltaMaximum = ActualMaximum - oldMaximum;

            AxisChanged?.Invoke(this, new AxisChangedEventArgs(AxisChangeTypes.Reset, deltaMinimum, deltaMaximum));
        }

        /// <summary>
        /// Sets the <see cref="ViewMinimum"/> and <see cref="ViewMaximum"/> properties
        /// respectively to the value of <see cref="ActualMinimum"/> and <see cref="ActualMaximum"/> properties.
        /// </summary>
        /// <remarks>
        /// This effectively disables automatic pan/zoom when changing the extents of the data (e.g. by adding or removing data).
        /// </remarks>
        public void SetViewMaxMinToActualMaxMin()
        {
            ViewMinimum = ActualMinimum;
            ViewMaximum = ActualMaximum;
        }

        /// <summary>
        /// Transforms the specified coordinate to relative screen coordinate.
        /// </summary>
        /// <param name="x">The value.</param>
        /// <param name="isVector">True if the transform is a vector transform (i.e. only applies scale, not translation). The default value is False.</param>
        /// <returns>The transformed value (relative screen coordinate).</returns>
        public virtual double Transform(double x, bool isVector = false)
        {
            return (PreTransform(x) - (isVector ? 0 : Offset)) * Scale;
        }

        /// <summary>
        /// Transforms the specified point to relative screen coordinates.
        /// </summary>
        /// <param name="x">The x value (for the current axis).</param>
        /// <param name="y">The y value.</param>
        /// <param name="yaxis">The y axis.</param>
        /// <param name="isVector">True if the transform is a vector transform (i.e. only applies scale, not translation). The default value is False.</param>
        /// <returns>The transformed point.</returns>
        public virtual Point Transform(double x, double y, AxisBase yaxis, bool isVector = false)
        {
            if (yaxis == null) throw new ArgumentNullException(nameof(yaxis), "Y axis should not be null when transforming.");
            return new Point(Transform(x, isVector), yaxis.Transform(y, isVector));
        }

        /// <summary>
        /// Zoom to the specified scale.
        /// </summary>
        /// <param name="newScale">The new scale.</param>
        public virtual void Zoom(double newScale)
        {
            var oldMinimum = ActualMinimum;
            var oldMaximum = ActualMaximum;

            var sx1 = Transform(ActualMaximum);
            var sx0 = Transform(ActualMinimum);

            var sgn = Math.Sign(Scale);
            var mid = (ActualMaximum + ActualMinimum) / 2;

            var dx = (Offset - mid) * Scale;
            var newOffset = (dx / (sgn * newScale)) + mid;
            SetTransform(sgn * newScale, newOffset);

            var newMaximum = InverseTransform(sx1);
            var newMinimum = InverseTransform(sx0);

            if (newMinimum < AbsoluteMinimum && newMaximum > AbsoluteMaximum)
            {
                newMinimum = AbsoluteMinimum;
                newMaximum = AbsoluteMaximum;
            }
            else
            {
                if (newMinimum < AbsoluteMinimum)
                {
                    var d = newMaximum - newMinimum;
                    newMinimum = AbsoluteMinimum;
                    newMaximum = AbsoluteMinimum + d;
                    if (newMaximum > AbsoluteMaximum)
                    {
                        newMaximum = AbsoluteMaximum;
                    }
                }
                else if (newMaximum > AbsoluteMaximum)
                {
                    var d = newMaximum - newMinimum;
                    newMaximum = AbsoluteMaximum;
                    newMinimum = AbsoluteMaximum - d;
                    if (newMinimum < AbsoluteMinimum)
                    {
                        newMinimum = AbsoluteMinimum;
                    }
                }
            }

            ViewMaximum = newMaximum;
            ViewMinimum = newMinimum;
            UpdateActualMaxMin();

            var deltaMinimum = ActualMinimum - oldMinimum;
            var deltaMaximum = ActualMaximum - oldMaximum;

            AxisChanged?.Invoke(this, new AxisChangedEventArgs(AxisChangeTypes.Zoom, deltaMinimum, deltaMaximum));
        }

        /// <summary>
        /// Zooms the axis to the range [x0,x1].
        /// </summary>
        /// <param name="x0">The new minimum.</param>
        /// <param name="x1">The new maximum.</param>
        public virtual void Zoom(double x0, double x1)
        {
            var oldMinimum = ActualMinimum;
            var oldMaximum = ActualMaximum;

            var newMinimum = Math.Max(Math.Min(x0, x1), AbsoluteMinimum);
            var newMaximum = Math.Min(Math.Max(x0, x1), AbsoluteMaximum);

            ViewMinimum = newMinimum;
            ViewMaximum = newMaximum;
            UpdateActualMaxMin();

            var deltaMinimum = ActualMinimum - oldMinimum;
            var deltaMaximum = ActualMaximum - oldMaximum;

            AxisChanged?.Invoke(this, new AxisChangedEventArgs(AxisChangeTypes.Zoom, deltaMinimum, deltaMaximum));
        }

        /// <summary>
        /// Zooms the axis at the specified coordinate.
        /// </summary>
        /// <param name="factor">The zoom factor.</param>
        /// <param name="x">The coordinate to zoom at.</param>
        public virtual void ZoomAt(double factor, double x)
        {
            var oldMinimum = ActualMinimum;
            var oldMaximum = ActualMaximum;

            var dx0 = (ActualMinimum - x) * Scale;
            var dx1 = (ActualMaximum - x) * Scale;
            SetTransform(Scale * factor, Offset);

            var newMinimum = (dx0 / Scale) + x;
            var newMaximum = (dx1 / Scale) + x;

            if (newMaximum - newMinimum > MaximumRange)
            {
                var mid = (newMinimum + newMaximum) * 0.5;
                newMaximum = mid + MaximumRange * 0.5;
                newMinimum = mid - MaximumRange * 0.5;
            }

            if (newMaximum - newMinimum < MinimumRange)
            {
                var mid = (newMinimum + newMaximum) * 0.5;
                newMaximum = mid + MinimumRange * 0.5;
                newMinimum = mid - MinimumRange * 0.5;
            }

            newMinimum = Math.Max(newMinimum, AbsoluteMinimum);
            newMaximum = Math.Min(newMaximum, AbsoluteMaximum);

            ViewMinimum = newMinimum;
            ViewMaximum = newMaximum;
            UpdateActualMaxMin();

            var deltaMinimum = ActualMinimum - oldMinimum;
            var deltaMaximum = ActualMaximum - oldMaximum;

            AxisChanged?.Invoke(this, new AxisChangedEventArgs(AxisChangeTypes.Zoom, deltaMinimum, deltaMaximum));
        }

        /// <summary>
        /// Zooms the axis with the specified zoom factor at the center of the axis.
        /// </summary>
        /// <param name="factor">The zoom factor.</param>
        public virtual void ZoomAtCenter(double factor)
        {
            var sx = (Transform(ActualMaximum) + Transform(ActualMinimum)) * 0.5;
            var x = InverseTransform(sx);
            ZoomAt(factor, x);
        }

        /// <summary>
        /// Resets the <see cref="DataMaximum" /> and <see cref="DataMinimum" /> values.
        /// </summary>
        internal virtual void ResetDataMaxMin()
        {
            DataMaximum = DataMinimum = ActualMaximum = ActualMinimum = double.NaN;
        }

        /// <summary>
        /// Updates the <see cref="ActualMaximum" /> and <see cref="ActualMinimum" /> values.
        /// </summary>
        /// <remarks>If the user has zoomed/panned the axis, the internal ViewMaximum/ViewMinimum
        /// values will be used. If Maximum or Minimum have been set, these values will be used. Otherwise the maximum and minimum values
        /// of the series will be used, including the 'padding'.</remarks>
        internal virtual void UpdateActualMaxMin()
        {
            if (!double.IsNaN(ViewMaximum))
            {
                // The user has zoomed/panned the axis, use the ViewMaximum value.
                ActualMaximum = ViewMaximum;
            }
            else if (!double.IsNaN(Maximum))
            {
                // The Maximum value has been set
                ActualMaximum = Maximum;
            }
            else
            {
                // Calculate the actual maximum, including padding
                ActualMaximum = CalculateActualMaximum();
            }

            if (!double.IsNaN(ViewMinimum))
            {
                // The user has zoomed/panned the axis, use the ViewMinimum value.
                ActualMinimum = ViewMinimum;
            }
            else if (!double.IsNaN(Minimum))
            {
                // The Minimum value has been set
                ActualMinimum = Minimum;
            }
            else
            {
                // Calculate the actual minimum, including padding
                ActualMinimum = CalculateActualMinimum();
            }

            CoerceActualMaxMin();
        }

        /// <summary>
        /// Updates the actual minor and major step intervals.
        /// </summary>
        /// <param name="curveArea">The curve area rectangle.</param>
        internal virtual void UpdateIntervals(Rect curveArea)
        {
            var labelSize = IntervalLength;
            var length = IsHorizontal() ? curveArea.Width : curveArea.Height;
            length *= Math.Abs(EndPosition - StartPosition);

            ActualMajorStep = !double.IsNaN(MajorStep)
                                       ? MajorStep
                                       : CalculateActualInterval(length, labelSize, ActualMaximum - ActualMinimum);

            ActualMinorStep = !double.IsNaN(MinorStep)
                                       ? MinorStep
                                       : CalculateMinorInterval(ActualMajorStep);

            if (double.IsNaN(ActualMinorStep))
            {
                ActualMinorStep = 2;
            }
            if (double.IsNaN(ActualMajorStep))
            {
                ActualMajorStep = 10;
            }
        }

        /// <summary>
        /// Updates the scale and offset properties of the transform from the specified boundary rectangle.
        /// </summary>
        /// <param name="bounds">The bounds.</param>
        internal virtual void UpdateTransform(Rect bounds)
        {
            var x0 = bounds.Left;
            var x1 = bounds.Right;
            var y0 = bounds.Bottom;
            var y1 = bounds.Top;

            ScreenMin = new Point(x0, y1);
            ScreenMax = new Point(x1, y0);

            var a0 = IsHorizontal() ? x0 : y0;
            var a1 = IsHorizontal() ? x1 : y1;

            var dx = a1 - a0;
            a1 = a0 + (EndPosition * dx);
            a0 = a0 + (StartPosition * dx);
            ScreenMin = new Point(a0, a1);
            ScreenMax = new Point(a1, a0);

            if (ActualMaximum - ActualMinimum < double.Epsilon)
            {
                ActualMaximum = ActualMinimum + 1;
            }

            var max = PreTransform(ActualMaximum);
            var min = PreTransform(ActualMinimum);

            var da = a0 - a1;
            double newOffset, newScale;
            if (Math.Abs(da) > double.Epsilon)
            {
                newOffset = (a0 / da * max) - (a1 / da * min);
            }
            else
            {
                newOffset = 0;
            }

            var range = max - min;
            if (Math.Abs(range) > double.Epsilon)
            {
                newScale = (a1 - a0) / range;
            }
            else
            {
                newScale = 1;
            }

            SetTransform(newScale, newOffset);
        }

        /// <summary>
        /// Returns the actual interval to use to determine which values are displayed in the axis.
        /// </summary>
        /// <param name="availableSize">The available size.</param>
        /// <param name="maxIntervalSize">The maximum interval size.</param>
        /// <param name="range">The range.</param>
        /// <returns>Actual interval to use to determine which values are displayed in the axis.</returns>
        protected double CalculateActualInterval(double availableSize, double maxIntervalSize, double range)
        {
            if (availableSize <= 0)
            {
                return maxIntervalSize;
            }

            Func<double, double> exponent = x => Math.Ceiling(Math.Log(x, 10));
            Func<double, double> mantissa = x => x / Math.Pow(10, exponent(x) - 1);

            // reduce intervals for horizontal axis.
            // double maxIntervals = Orientation == AxisOrientation.x ? MaximumAxisIntervalsPer200Pixels * 0.8 : MaximumAxisIntervalsPer200Pixels;
            // real maximum interval count
            var maxIntervalCount = availableSize / maxIntervalSize;

            range = Math.Abs(range);
            var interval = Math.Pow(10, exponent(range));
            var intervalCandidate = interval;

            // Function to remove 'double precision noise'
            // TODO: can this be improved?
            Func<double, double> removeNoise = x => double.Parse(x.ToString("e14"));

            // decrease interval until interval count becomes less than maxIntervalCount
            while (true)
            {
                var m = (int)mantissa(intervalCandidate);
                if (m == 5)
                {
                    // reduce 5 to 2
                    intervalCandidate = removeNoise(intervalCandidate / 2.5);
                }
                else if (m == 2 || m == 1 || m == 10)
                {
                    // reduce 2 to 1, 10 to 5, 1 to 0.5
                    intervalCandidate = removeNoise(intervalCandidate / 2.0);
                }
                else
                {
                    intervalCandidate = removeNoise(intervalCandidate / 2.0);
                }

                if (range / intervalCandidate > maxIntervalCount)
                {
                    break;
                }

                if (double.IsNaN(intervalCandidate) || double.IsInfinity(intervalCandidate))
                {
                    break;
                }

                interval = intervalCandidate;
            }

            return interval;
        }

        /// <summary>
        /// Calculates the actual maximum value of the axis, including the <see cref="MaximumPadding" />.
        /// </summary>
        /// <returns>The new actual maximum value of the axis.</returns>
        protected virtual double CalculateActualMaximum()
        {
            var result = DataMaximum;
            var range = DataMaximum - DataMinimum;

            if (range < double.Epsilon)
            {
                var zeroRange = DataMaximum > 0 ? DataMaximum : 1;
                result += zeroRange * 0.5;
            }

            if (!double.IsNaN(DataMinimum) && !double.IsNaN(result))
            {
                var x1 = PreTransform(result);
                var x0 = PreTransform(DataMinimum);
                var dx = MaximumPadding * (x1 - x0);
                return PostInverseTransform(x1 + dx);
            }

            return result;
        }

        /// <summary>
        /// Calculates the actual minimum value of the axis, including the <see cref="MinimumPadding" />.
        /// </summary>
        /// <returns>The new actual minimum value of the axis.</returns>
        protected virtual double CalculateActualMinimum()
        {
            var result = DataMinimum;
            var range = DataMaximum - DataMinimum;

            if (range < double.Epsilon)
            {
                var zeroRange = DataMaximum > 0 ? DataMaximum : 1;
                result -= zeroRange * 0.5;
            }

            if (!double.IsNaN(ActualMaximum))
            {
                var x1 = PreTransform(ActualMaximum);
                var x0 = PreTransform(result);
                var dx = MinimumPadding * (x1 - x0);
                return PostInverseTransform(x0 - dx);
            }

            return result;
        }
        
        /// <summary>
        /// Calculates the minor interval.
        /// </summary>
        /// <param name="majorInterval">The major interval.</param>
        /// <returns>The minor interval.</returns>
        protected double CalculateMinorInterval(double majorInterval)
        {
            // if major interval is 100, the minor interval will be 20.
            return majorInterval / 5;
        }

        /// <summary>
        /// Applies a transformation after the inverse transform of the value.
        /// </summary>
        /// <param name="x">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        /// <remarks>If this method is overridden, the <see cref="InverseTransform(double, bool)" /> method must also be overridden.
        /// See <see cref="LogarithmicAxis" /> for examples on how to implement this.</remarks>
        protected abstract double PostInverseTransform(double x);

        /// <summary>
        /// Applies a transformation before the transform the value.
        /// </summary>
        /// <param name="x">The value to transform.</param>
        /// <returns>The transformed value.</returns>
        /// <remarks>If this method is overridden, the <see cref="Transform(double, bool)" /> method must also be overridden.
        /// See <see cref="LogarithmicAxis" /> for examples on how to implement this.</remarks>
        protected abstract double PreTransform(double x);

        /// <summary>
        /// Sets the transform.
        /// </summary>
        /// <param name="newScale">The new scale.</param>
        /// <param name="newOffset">The new offset.</param>
        protected void SetTransform(double newScale, double newOffset)
        {
            // ReSharper disable CompareOfFloatsByEqualityOperator
            if (newScale == Scale && newOffset == Offset)
                return;
            // ReSharper restore CompareOfFloatsByEqualityOperator

            Scale = newScale;
            Offset = newOffset;
            TransformChanged?.Invoke(this, new EventArgs());
        }
    }
}
