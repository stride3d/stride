// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using Stride.Core.Annotations;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.Extensions
{
    using Color = Stride.Core.Mathematics.Color;

    /// <summary>
    /// Provides extension methods for the <see cref="IDrawingContext" />.
    /// </summary>
    public static class DrawingContextExtensions
    {
        /// <summary>
        /// Gets or sets the subscript alignment.
        /// </summary>
        private static double SubAlignment { get; } = 0.6;

        /// <summary>
        /// Gets or sets the subscript size.
        /// </summary>
        private static double SubSize { get; } = 0.62;

        /// <summary>
        /// Gets or sets the superscript alignment.
        /// </summary>
        private static double SuperAlignment { get; } = 0;

        /// <summary>
        /// Gets or sets the superscript size.
        /// </summary>
        private static double SuperSize { get; } = 0.62;

        /// <summary>
        /// Draws a circle in the canvas.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="point"></param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="fillColor">The color of the shape's interior.</param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawCircle([NotNull] this IDrawingContext renderer, Point point, double radius, Color fillColor, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
        {
            var diameter = radius*2;
            renderer.DrawEllipse(point, new Size(diameter, diameter), fillColor, strokeColor, thickness, lineJoin, dashArray, dashOffset, isHitTestVisible);
        }

        /// <summary>
        /// Draws a collection of circles in the canvas, where all have the same visual appearance (stroke, fill, etc.).
        /// This performs better than calling DrawCircle multiple times.
        /// </summary>
        /// <param name="renderer">The renderer.</param>
        /// <param name="points"></param>
        /// <param name="radius">The radius of the circle.</param>
        /// <param name="fillColor">The color of the shape's interior.</param>
        /// <param name="strokeColor">The color of the shape's outline.</param>
        /// <param name="thickness">The wifdth of the shape's outline. The default is <c>1</c>.</param>
        /// <param name="lineJoin">The type of join that is used at the vertices of the shape. The default is <see cref="PenLineJoin.Miter"/>.</param>
        /// <param name="dashArray">The pattern of dashes and gaps that is used to outline the shape. The default is <c>null</c>.</param>
        /// <param name="dashOffset">The distance within the dash pattern where a dash begins. The default is <c>0</c>.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DrawCircles([NotNull] this IDrawingContext renderer, [NotNull] IList<Point> points, double radius, Color fillColor, Color strokeColor,
            double thickness = 1.0, PenLineJoin lineJoin = PenLineJoin.Miter, ICollection<double> dashArray = null, double dashOffset = 0, bool isHitTestVisible = false)
        {
            renderer.DrawEllipses(points, radius, radius, fillColor, strokeColor, thickness, lineJoin, dashArray, dashOffset, isHitTestVisible);
        }

        /// <summary>
        /// Draws text containing sub- and superscript.
        /// </summary>
        /// <param name="renderer">The render context.</param>
        /// <param name="point">The point.</param>
        /// <param name="color">Color of the text.</param>
        /// <param name="text">The text.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="hAlign">The horizontal alignment.</param>
        /// <param name="vAlign">The vertical alignment.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        /// <returns>The size of the text.</returns>
        /// <example>Subscript: H_{2}O
        /// Superscript: E=mc^{2}
        /// Both: A^{2}_{i,j}</example>
        public static void DrawMathText([NotNull] this IDrawingContext renderer, Point point, Color color, string text, [NotNull] FontFamily fontFamily, double fontSize, FontWeight fontWeight,
            HorizontalAlignment hAlign = HorizontalAlignment.Left, VerticalAlignment vAlign = VerticalAlignment.Top, bool isHitTestVisible = false)
        {
            if (string.IsNullOrEmpty(text))
                return;

            if (text.Contains("^{") || text.Contains("_{"))
            {
                var x = point.X;
                var y = point.Y;
                InternalDrawMathText(renderer, x, y, color, text, fontFamily, fontSize, fontWeight, isHitTestVisible);
            }
            else
            {
                renderer.DrawText(point, color, text, fontFamily, fontSize, fontWeight, hAlign, vAlign, isHitTestVisible);
            }
        }

        /// <summary>
        /// Draws text with sub- and superscript items.
        /// </summary>
        /// <param name="renderer">The render context.</param>
        /// <param name="x">The x.</param>
        /// <param name="y">The y.</param>
        /// <param name="s">The s.</param>
        /// <param name="color">The text color.</param>
        /// <param name="fontFamily">The font family.</param>
        /// <param name="fontSize">The font size.</param>
        /// <param name="fontWeight">The font weight.</param>
        /// <param name="isHitTestVisible"><c>true</c> if hit testing should be enabled, <c>false</c> otherwise. The default is <c>false</c>.</param>
        /// <returns>The size of the text.</returns>
        private static void InternalDrawMathText([NotNull] IDrawingContext renderer, double x, double y, Color color, [NotNull] string s, [NotNull] FontFamily fontFamily, double fontSize, FontWeight fontWeight, bool isHitTestVisible)
        {
            var i = 0;

            var currentX = x;
            var maximumX = x;
            var currentY = y;

            // http://en.wikipedia.org/wiki/Subscript_and_superscript
            var superScriptYDisplacement = fontSize * SuperAlignment;
            var subscriptYDisplacement = fontSize * SubAlignment;

            var superscriptFontSize = fontSize * SuperSize;
            var subscriptFontSize = fontSize * SubSize;

            Func<double, double, string, double, Size> drawText = (xb, yb, text, fSize) =>
            {
                renderer.DrawText(new Point(xb, yb), color, text, fontFamily, fSize, fontWeight, isHitTestVisible: isHitTestVisible);

                var flatSize = renderer.MeasureText(text, fontFamily, fSize, fontWeight);
                return new Size(flatSize.Width, flatSize.Height);
            };

            while (i < s.Length)
            {
                // Superscript
                if (i + 1 < s.Length && s[i] == '^' && s[i + 1] == '{')
                {
                    var i1 = s.IndexOf('}', i);
                    if (i1 != -1)
                    {
                        var supString = s.Substring(i + 2, i1 - i - 2);
                        i = i1 + 1;
                        var sx = currentX;
                        var sy = currentY + superScriptYDisplacement;
                        var size = drawText(sx, sy, supString, superscriptFontSize);
                        maximumX = Math.Max(sx + size.Width, maximumX);

                        continue;
                    }
                }

                // Subscript
                if (i + 1 < s.Length && s[i] == '_' && s[i + 1] == '{')
                {
                    var i1 = s.IndexOf('}', i);
                    if (i1 != -1)
                    {
                        var subString = s.Substring(i + 2, i1 - i - 2);
                        i = i1 + 1;
                        var sx = currentX;
                        var sy = currentY + subscriptYDisplacement;
                        var size = drawText(sx, sy, subString, subscriptFontSize);
                        maximumX = Math.Max(sx + size.Width, maximumX);

                        continue;
                    }
                }

                // Regular text
                var i2 = s.IndexOfAny("^_".ToCharArray(), i + 1);
                string regularString;
                if (i2 == -1)
                {
                    regularString = s.Substring(i);
                    i = s.Length;
                }
                else
                {
                    regularString = s.Substring(i, i2 - i);
                    i = i2;
                }

                currentX = maximumX + 2;
                var size2 = drawText(currentX, currentY, regularString, fontSize);

                maximumX = Math.Max(currentX + size2.Width, maximumX);

                currentX = maximumX;
            }
        }
    }
}
