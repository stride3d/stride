// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Drawing
{
    using Color = Stride.Core.Mathematics.Color;

    public interface IDrawingContext
    {
        /// <summary>
        /// Clears the drawing.
        /// </summary>
        void Clear();

        /// <summary>
        /// Draws an ellipse.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="size"></param>
        /// <param name="fillColor">The color of the shape's interior.</param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawEllipse(Point point, Size size, Color fillColor, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool isHitTestVisible = false);

        /// <summary>
        /// Draws a collection of ellipses, where all have the same visual appearance (stroke, fill, etc.).
        /// </summary>
        /// <remarks>
        /// This performs better than calling <see cref="CanvasRenderer.DrawEllipse"/> multiple times.
        /// </remarks>
        /// <param name="points"></param>
        /// <param name="radiusX">The horizontal radius of the ellipse.</param>
        /// <param name="radiusY">The vertical radius of the ellipse.</param>
        /// <param name="fillColor">The color of the shape's interior.</param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawEllipses([NotNull] IList<Point> points, double radiusX, double radiusY, Color fillColor, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool isHitTestVisible = false);

        /// <summary>
        /// Draws a straight line between <paramref name="p1"/> and <paramref name="p2"/>.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="aliased"></param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawLine(Point p1, Point p2, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false);

        /// <summary>
        /// Draws line segments defined by points (0,1) (2,3) (4,5) etc.
        /// </summary>
        /// <param name="points">The points.</param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="aliased"></param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawLineSegments([NotNull] IList<Point> points, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false);

        /// <summary>
        /// Draws a polygon.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="fillColor"></param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="aliased"></param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawPolygon([NotNull] IList<Point> points, Color fillColor, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false);

        /// <summary>
        /// Draws a polyline.
        /// </summary>
        /// <param name="points"></param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="aliased"></param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawPolyline([NotNull] IList<Point> points, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool aliased = false, bool isHitTestVisible = false);

        /// <summary>
        /// Draws a rectangle.
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="fillColor">The color of the shape's interior.</param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawRectangle(Rect rect, Color fillColor, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool isHitTestVisible = false);

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="color">The color of the text.</param>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="hAlign">The horizontal alignment. The default is <see cref="HorizontalAlignment.Left"/>.</param>
        /// <param name="vAlign">The vertical alignment. The default is <see cref="VerticalAlignment.Top"/>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawText(Point point, Color color, string text, [NotNull] FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            HorizontalAlignment hAlign = HorizontalAlignment.Left, VerticalAlignment vAlign = VerticalAlignment.Top, bool isHitTestVisible = false);

        /// <summary>
        /// Draws a collection of texts where all have the same visual appearance (color, font, alignment).
        /// </summary>
        /// <remarks>
        /// This performs better than calling <see cref="CanvasRenderer.DrawText"/> multiple times.
        /// </remarks>
        /// <param name="points"></param>
        /// <param name="color">The color of the text.</param>
        /// <param name="texts"></param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="hAlign">The horizontal alignment. The default is <see cref="HorizontalAlignment.Left"/>.</param>
        /// <param name="vAlign">The vertical alignment. The default is <see cref="VerticalAlignment.Top"/>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        void DrawTexts([NotNull] IList<Point> points, Color color, [NotNull] IList<string> texts, [NotNull] FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            HorizontalAlignment hAlign = HorizontalAlignment.Left, VerticalAlignment vAlign = VerticalAlignment.Top, bool isHitTestVisible = false);

        /// <summary>
        /// Measures the size of the specified text.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="measurementMethod"></param>
        /// <returns>
        /// The size of the text (in device independent units, 1/96 inch).
        /// </returns>
        Size MeasureText(string text, [NotNull] FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface);

        /// <summary>
        /// Measures the size of the specified texts where all have the same visual appearance (color, font, alignment) and returns the maximum.
        /// </summary>
        /// <remarks>
        /// This performs better than calling <see cref="CanvasRenderer.MeasureText"/> multiple times.
        /// </remarks>
        /// <param name="texts">The texts.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">Size of the font.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="measurementMethod"></param>
        /// <returns>
        /// The maximum size of the texts (in device independent units, 1/96 inch).
        /// </returns>
        Size MeasureTexts([NotNull] IList<string> texts, [NotNull] FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            TextMeasurementMethod measurementMethod = TextMeasurementMethod.GlyphTypeface);

        /// <summary>
        /// Resets the clip rectangle.
        /// </summary>
        void ResetClip();

        /// <summary>
        /// Sets the clipping rectangle.
        /// </summary>
        /// <param name="clippingRect">The clipping rectangle.</param>
        void SetClip(Rect clippingRect);
    }
}
