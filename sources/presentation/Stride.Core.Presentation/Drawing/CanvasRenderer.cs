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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Extensions;

namespace Stride.Core.Presentation.Drawing
{
    using Color = Stride.Core.Mathematics.Color;

    public class CanvasRenderer : IDrawingContext
    {
        private readonly Dictionary<Color, Brush> cachedBrushes = new Dictionary<Color, Brush>();
        private const int MaxPolylinesPerLine = 64;
        private const int MinPointsPerPolyline = 16;

        /// <summary>
        /// The clip rectangle.
        /// </summary>
        private Rect? clip;

        public CanvasRenderer([NotNull] Canvas canvas)
        {
            if (canvas == null) throw new ArgumentNullException(nameof(canvas));
            Canvas = canvas;
            UseStreamGeometry = true;
        }

        /// <summary>
        /// Gets or sets the thickness limit for "balanced" line drawing.
        /// </summary>
        public double BalancedLineDrawingThicknessLimit { get; set; } = 3.5;

        [NotNull]
        public Canvas Canvas { get; }

        /// <summary>
        /// Gets or sets a value indicating whether to use stream geometry for lines and polygons rendering.
        /// </summary>
        /// <value><c>true</c> if stream geometry should be used; otherwise, <c>false</c> .</value>
        /// <remarks>Using stream geometry seems to be slightly faster than using path geometry.</remarks>
        public bool UseStreamGeometry { get; set; }

        /// <inheritdoc/>
        public void Clear()
        {
            Canvas.Children.Clear();
        }

        /// <inheritdoc/>
        public void DrawEllipse(Point point, Size size, Color fillColor, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool isHitTestVisible)
        {
            point.Offset(-size.Width / 2, -size.Height / 2);
            var rect = new Rect(point, size);

            var ellipse = Create<Ellipse>(isHitTestVisible, rect.Left, rect.Top);

            ellipse.Fill = GetBrush(fillColor);
            SetStroke(ellipse, strokeColor, thickness, lineJoin, dashArray, dashOffset, false);

            ellipse.Height = rect.Height;
            ellipse.Width = rect.Width;
            Canvas.SetLeft(ellipse, rect.Left);
            Canvas.SetTop(ellipse, rect.Top);
        }

        /// <inheritdoc/>
        public void DrawEllipses(IList<Point> points, double radiusX, double radiusY, Color fillColor, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool isHitTestVisible)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count == 0)
                return;

            var fillBrush = GetBrush(fillColor);
            var strokeBrush = GetBrush(strokeColor);
            var pen = new Pen(strokeBrush, thickness)
            {
                LineJoin = lineJoin,
                DashStyle = new DashStyle(dashArray, dashOffset),
            };

            var visual = new DrawingVisual();
            var context = visual.RenderOpen();
            foreach (var point in points)
            {
                context.DrawEllipse(fillBrush, pen, point, radiusX, radiusY);
            }
            context.Close();

            var host = Create<VisualHost>(isHitTestVisible);
            host.AddChild(visual);
        }

        /// <inheritdoc/>
        public void DrawLine(Point p1, Point p2, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool aliased, bool isHitTestVisible)
        {
            var line = Create<Line>(isHitTestVisible);
            SetStroke(line, strokeColor, thickness, lineJoin, dashArray, dashOffset, aliased);
            line.X1 = p1.X;
            line.Y1 = p1.Y;
            line.X2 = p2.X;
            line.Y2 = p2.Y;
        }

        /// <inheritdoc/>
        public void DrawLineSegments(IList<Point> points, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool aliased, bool isHitTestVisible)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (points.Count < 2)
                return;

            if (UseStreamGeometry)
            {
                DrawLineSegmentsByStreamGeometry(points, strokeColor, thickness, lineJoin, dashArray, dashOffset, aliased, isHitTestVisible);
                return;
            }

            var pathGeometry = new PathGeometry();
            for (var i = 0; i < points.Count - 1; i += 2)
            {
                var figure = new PathFigure
                {
                    IsClosed = false,
                    StartPoint = aliased ? ToPixelAlignedPoint(points[i]) : points[i],
                };
                var segment = new LineSegment
                {
                    IsSmoothJoin = false,
                    IsStroked = true,
                    Point = aliased ? ToPixelAlignedPoint(points[i + 1]) : points[i + 1],
                };
                figure.Segments.Add(segment);
                pathGeometry.Figures.Add(figure);
            }

            var path = Create<Path>(isHitTestVisible);
            SetStroke(path, strokeColor, thickness, lineJoin, dashArray, dashOffset, aliased);
            path.Data = pathGeometry;
        }

        /// <inheritdoc/>
        public void DrawPolygon(IList<Point> points, Color fillColor, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool aliased, bool isHitTestVisible)
        {
            var polygon = Create<Polygon>(isHitTestVisible);

            polygon.Fill = GetBrush(fillColor);
            SetStroke(polygon, strokeColor, thickness, lineJoin, dashArray, dashOffset, false);

            polygon.Points = ToPointCollection(points, aliased);
        }

        /// <inheritdoc/>
        public void DrawPolyline(IList<Point> points, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool aliased, bool isHitTestVisible)
        {
            if (thickness < BalancedLineDrawingThicknessLimit)
            {
                DrawPolylineBalanced(points, strokeColor, thickness, lineJoin, dashArray, aliased, isHitTestVisible);
            }

            var polyline = Create<Polyline>(isHitTestVisible);
            SetStroke(polyline, strokeColor, thickness, lineJoin, dashArray, dashOffset, aliased);
            polyline.Points = ToPointCollection(points, aliased);
        }

        /// <inheritdoc/>
        public void DrawRectangle(Rect rect, Color fillColor, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool isHitTestVisible)
        {
            var rectangle = Create<Rectangle>(isHitTestVisible, rect.Left, rect.Top);

            rectangle.Fill = GetBrush(fillColor);
            SetStroke(rectangle, strokeColor, thickness, lineJoin, dashArray, dashOffset, false);

            rectangle.Height = rect.Height;
            rectangle.Width = rect.Width;
            Canvas.SetLeft(rectangle, rect.Left);
            Canvas.SetTop(rectangle, rect.Top);
        }

        /// <inheritdoc/>
        public void DrawText(Point point, Color color, string text, FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            HorizontalAlignment hAlign, VerticalAlignment vAlign, bool isHitTestVisible)
        {
            var textBlock = Create<TextBlock>(isHitTestVisible);
            textBlock.Foreground = GetBrush(color);
            textBlock.FontFamily = fontFamily;
            textBlock.FontSize = fontSize;
            textBlock.FontWeight = fontWeight;
            textBlock.Text = text;

            var dx = 0.0;
            var dy = 0.0;

            if (hAlign != HorizontalAlignment.Left || vAlign != VerticalAlignment.Top)
            {
                textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                var size = textBlock.DesiredSize;
                if (hAlign == HorizontalAlignment.Center)
                    dx = -size.Width / 2;

                if (hAlign == HorizontalAlignment.Right)
                    dx = -size.Width;

                if (vAlign == VerticalAlignment.Center)
                    dy = -size.Height / 2;

                if (vAlign == VerticalAlignment.Bottom)
                    dy = -size.Height;
            }

            textBlock.RenderTransform = new TranslateTransform(point.X + dx, point.Y + dy);
            textBlock.SetValue(RenderOptions.ClearTypeHintProperty, ClearTypeHint.Enabled);
        }

        /// <inheritdoc/>
        public void DrawTexts(IList<Point> points, Color color, IList<string> texts, FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            HorizontalAlignment hAlign, VerticalAlignment vAlign, bool isHitTestVisible)
        {
            if (points == null) throw new ArgumentNullException(nameof(points));
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            if (points.Count != texts.Count) throw new ArgumentException($"{nameof(points)} and {nameof(texts)} must have the same number of elements.");

            var brush = GetBrush(color);
            var typeFace = new Typeface(fontFamily, FontStyles.Normal, fontWeight, FontStretches.Normal);

            var visual = new DrawingVisual();
            var context = visual.RenderOpen();
            for (var i = 0; i < points.Count; ++i)
            {
                var text = texts[i];
                var point = points[i];
                var formatted = new FormattedText(text, CultureInfo.CurrentUICulture, FlowDirection.LeftToRight, typeFace, fontSize, brush);
                var dx = 0.0;
                var dy = 0.0;
                if (hAlign != HorizontalAlignment.Left || vAlign != VerticalAlignment.Top)
                {
                    var size = new Size(formatted.Width, formatted.Height);
                    if (hAlign == HorizontalAlignment.Center)
                        dx = -size.Width / 2;

                    if (hAlign == HorizontalAlignment.Right)
                        dx = -size.Width;

                    if (vAlign == VerticalAlignment.Center)
                        dy = -size.Height / 2;

                    if (vAlign == VerticalAlignment.Bottom)
                        dy = -size.Height;
                }
                point.Offset(dx, dy);
                context.DrawText(formatted, point);
            }
            context.Close();

            var host = Create<VisualHost>(isHitTestVisible);
            host.AddChild(visual);
        }

        /// <inheritdoc/>
        public Size MeasureText(string text, FontFamily fontFamily, double fontSize, FontWeight fontWeight, TextMeasurementMethod measurementMethod)
        {
            if (string.IsNullOrEmpty(text))
                return Size.Empty;

            switch (measurementMethod)
            {
                case TextMeasurementMethod.GlyphTypeface:
                    GlyphTypeface glyphTypeface;
                    if (TryGetGlyphTypeface(fontFamily, FontStyles.Normal, fontWeight, FontStretches.Normal, out glyphTypeface))
                        return MeasureTextSize(glyphTypeface, fontSize, text);
                    // Fallback to TextBlock measurement method
                    goto case TextMeasurementMethod.TextBlock;

                case TextMeasurementMethod.TextBlock:
                    var textBlock = new TextBlock
                    {
                        FontFamily = fontFamily,
                        FontSize = fontSize,
                        FontWeight = fontWeight,
                        Text = text,
                    };
                    textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                    return new Size(textBlock.DesiredSize.Width, textBlock.DesiredSize.Height);

                default:
                    throw new ArgumentOutOfRangeException(nameof(measurementMethod));
            }
        }

        /// <inheritdoc/>
        public Size MeasureTexts(IList<string> texts, FontFamily fontFamily, double fontSize, FontWeight fontWeight, TextMeasurementMethod measurementMethod)
        {
            if (texts == null) throw new ArgumentNullException(nameof(texts));

            var maxWidth = 0.0;
            var maxHeight = 0.0;
            TextBlock textBlock = null;
            for (var i = 0; i < texts.Count; ++i)
            {
                var text = texts[i];
                Size size;
                switch (measurementMethod)
                {
                    case TextMeasurementMethod.GlyphTypeface:
                        GlyphTypeface glyphTypeface;
                        if (TryGetGlyphTypeface(fontFamily, FontStyles.Normal, fontWeight, FontStretches.Normal, out glyphTypeface))
                        {
                            size = MeasureTextSize(glyphTypeface, fontSize, text);
                            break;
                        }
                        // Fallback to TextBlock measurement method
                        goto case TextMeasurementMethod.TextBlock;

                    case TextMeasurementMethod.TextBlock:
                        if (textBlock == null)
                        {
                            textBlock = new TextBlock
                            {
                                FontFamily = fontFamily,
                                FontSize = fontSize,
                                FontWeight = fontWeight,
                            };
                        }
                        textBlock.Text = text;
                        textBlock.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                        size = textBlock.DesiredSize;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(measurementMethod));
                }
                if (size.Width > maxWidth)
                    maxWidth = size.Width;
                if (size.Height > maxHeight)
                    maxHeight = size.Height;
            }
            return new Size(maxWidth, maxHeight);
        }

        /// <inheritdoc/>
        public void ResetClip()
        {
            clip = null;
        }

        /// <inheritdoc/>
        public void SetClip(Rect clippingRect)
        {
            clip = clippingRect;
        }

        /// <summary>
        /// Creates an element and adds it to the canvas.
        /// </summary>
        /// <typeparam name="TElement"></typeparam>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise.</param>
        /// <param name="clipOffsetX"></param>
        /// <param name="clipOffsetY"></param>
        /// <returns></returns>
        [NotNull]
        private TElement Create<TElement>(bool isHitTestVisible, double clipOffsetX = 0, double clipOffsetY = 0)
            where TElement : UIElement, new()
        {
            var element = new TElement();
            if (clip.HasValue && !clip.Value.IsEmpty)
            {
                element.Clip = new RectangleGeometry(
                    new Rect(
                        clip.Value.X - clipOffsetX,
                        clip.Value.Y - clipOffsetY,
                        clip.Value.Width,
                        clip.Value.Height));
            }
            Canvas.Children.Add(element);
            element.IsHitTestVisible = isHitTestVisible;
            return element;
        }

        /// <summary>
        /// Draws the line segments by stream geometry.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="strokeColor">The stroke color.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="lineJoin">The line join.</param>
        /// <param name="dashArray">The dash array. Use <c>null</c> to get a solid line.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins.</param>
        /// <param name="aliased"></param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise.</param>
        /// <remarks>Using stream geometry seems to be slightly faster than using path geometry.</remarks>
        private void DrawLineSegmentsByStreamGeometry([NotNull] IList<Point> points, Color strokeColor,
            double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool aliased, bool isHitTestVisible)
        {
            var streamGeometry = new StreamGeometry();

            var streamGeometryContext = streamGeometry.Open();
            for (var i = 0; i < points.Count - 1; i += 2)
            {
                streamGeometryContext.BeginFigure(aliased ? ToPixelAlignedPoint(points[i]) : points[i], false, false);
                streamGeometryContext.LineTo(aliased ? ToPixelAlignedPoint(points[i + 1]) : points[i + 1], true, false);
            }
            streamGeometryContext.Close();

            var path = Create<Path>(isHitTestVisible);
            SetStroke(path, strokeColor, thickness, lineJoin, dashArray, dashOffset, aliased);
            path.Data = streamGeometry;
        }

        /// <summary>
        /// Draws the line using the MaxPolylinesPerLine and MinPointsPerPolyline properties.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="strokeColor">The stroke color.</param>
        /// <param name="thickness">The thickness.</param>
        /// <param name="lineJoin">The line join.</param>
        /// <param name="dashArray">The dash array. Use <c>null</c> to get a solid line.</param>
        /// <param name="aliased"></param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise.</param>
        /// <remarks>See <a href="https://oxyplot.codeplex.com/discussions/456679">discussion</a>.</remarks>
        private void DrawPolylineBalanced([NotNull] IList<Point> points, Color strokeColor, double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, bool aliased, bool isHitTestVisible)
        {
            // balance the number of points per polyline and the number of polylines
            var numPointsPerPolyline = Math.Max(points.Count / MaxPolylinesPerLine, MinPointsPerPolyline);

            var polyline = Create<Polyline>(isHitTestVisible);
            SetStroke(polyline, strokeColor, thickness, lineJoin, dashArray, 0, aliased);
            var pointCollection = new PointCollection(numPointsPerPolyline);

            var pointCount = points.Count;
            double lineLength = 0;
            var dashPatternLength = dashArray?.Sum() ?? 0;
            var last = new Point();
            for (var i = 0; i < pointCount; i++)
            {
                var current = aliased ? ToPixelAlignedPoint(points[i]) : points[i];
                pointCollection.Add(current);

                // get line length
                if (dashArray != null)
                {
                    if (i > 0)
                    {
                        var delta = current - last;
                        var dist = Math.Sqrt((delta.X * delta.X) + (delta.Y * delta.Y));
                        lineLength += dist;
                    }

                    last = current;
                }

                // use multiple polylines with limited number of points to improve WPF performance
                if (pointCollection.Count >= numPointsPerPolyline)
                {
                    polyline.Points = pointCollection;

                    if (i < pointCount - 1)
                    {
                        // start a new polyline at last point so there is no gap (it is not necessary to use the % operator)
                        var dashOffset = dashPatternLength > 0 ? lineLength / thickness : 0;
                        polyline = Create<Polyline>(isHitTestVisible);
                        SetStroke(polyline, strokeColor, thickness, lineJoin, dashArray, dashOffset, aliased);
                        pointCollection = new PointCollection(numPointsPerPolyline) { pointCollection.Last() };
                    }
                }
            }

            if (pointCollection.Count > 1 || pointCount == 1)
            {
                polyline.Points = pointCollection;
            }
        }

        /// <summary>
        /// Gets a brush for the given <paramref name="color"/>.
        /// </summary>
        /// <remarks>Brushes are cached and frozen to improve performance.</remarks>
        /// <seealso cref="Freezable.Freeze"/>
        /// <param name="color"></param>
        /// <returns></returns>
        private Brush GetBrush(Color color)
        {
            if (color.A == 0)
            {
                // If color is fully transparent, no need for a brush
                return null;
            }

            Brush brush;
            if (!cachedBrushes.TryGetValue(color, out brush))
            {
                brush = new SolidColorBrush(color.ToSystemColor());
                if (brush.CanFreeze)
                    brush.Freeze(); // Freezing should improve rendering performance
                cachedBrushes.Add(color, brush);
            }

            return brush;
        }

        private void SetStroke([NotNull] Shape shape, Color color, double thickness, PenLineJoin lineJoin, ICollection<double> dashArray, double dashOffset, bool aliased)
        {
            shape.Stroke = GetBrush(color);
            shape.StrokeThickness = thickness;
            shape.StrokeLineJoin = lineJoin;
            if (dashArray != null)
            {
                shape.StrokeDashArray = new DoubleCollection(dashArray);
                shape.StrokeDashOffset = dashOffset;
            }

            if (aliased)
            {
                shape.SetValue(RenderOptions.EdgeModeProperty, EdgeMode.Aliased);
                shape.SnapsToDevicePixels = true;
            }
        }

        /// <summary>
        /// Fast text size calculation.
        /// </summary>
        /// <param name="glyphTypeface">The glyph typeface.</param>
        /// <param name="sizeInEm">The size.</param>
        /// <param name="text">The text.</param>
        /// <returns>The text size.</returns>
        private static Size MeasureTextSize([NotNull] GlyphTypeface glyphTypeface, double sizeInEm, [NotNull] string text)
        {
            double width = 0;
            double lineWidth = 0;
            var lines = 0;
            foreach (var ch in text)
            {
                switch (ch)
                {
                    case '\n':
                        lines++;
                        if (lineWidth > width)
                        {
                            width = lineWidth;
                        }

                        lineWidth = 0;
                        continue;

                    case '\t':
                        continue;
                }

                var glyph = glyphTypeface.CharacterToGlyphMap[ch];
                var advanceWidth = glyphTypeface.AdvanceWidths[glyph];
                lineWidth += advanceWidth;
            }

            lines++;
            if (lineWidth > width)
            {
                width = lineWidth;
            }

            return new Size(Math.Round(width*sizeInEm, 2), Math.Round(lines*glyphTypeface.Height*sizeInEm, 2));
        }

        /// <summary>
        /// Converts a <see cref="Point" /> to a pixel aligned<see cref="Point" />.
        /// </summary>
        /// <param name="point">The point.</param>
        /// <returns>A pixel aligned <see cref="Point" />.</returns>
        private static Point ToPixelAlignedPoint(Point point)
        {
            // adding 0.5 to get pixel boundary alignment, seems to work
            // http://weblogs.asp.net/mschwarz/archive/2008/01/04/silverlight-rectangles-paths-and-line-comparison.aspx
            return new Point(0.5 + (int)point.X, 0.5 + (int)point.Y);
        }

        /// <summary>
        /// Creates a point collection from the specified points.
        /// </summary>
        /// <param name="points">The points to convert.</param>
        /// <param name="aliased">Convert to pixel aligned points if set to <c>true</c>.</param>
        /// <returns>The point collection.</returns>
        [NotNull]
        private static PointCollection ToPointCollection(IEnumerable<Point> points, bool aliased)
        {
            return new PointCollection(aliased ? points.Select(ToPixelAlignedPoint) : points);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool TryGetGlyphTypeface([NotNull] FontFamily fontFamily, FontStyle fontStyle, FontWeight fontWeight, FontStretch fontStretch, out GlyphTypeface glyphTypeface)
        {
            var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
            return typeface.TryGetGlyphTypeface(out glyphTypeface);
        }
    }
}
