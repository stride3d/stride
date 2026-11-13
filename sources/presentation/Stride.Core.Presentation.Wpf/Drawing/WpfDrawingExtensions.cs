// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;

namespace Stride.Core.Presentation.Drawing
{
    /// <summary>
    /// Conversions between the toolkit-free drawing types and the WPF ones.
    /// </summary>
    public static class WpfDrawingExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Point ToWpf(this DrawingPoint point) => new(point.X, point.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Vector ToWpf(this DrawingVector vector) => new(vector.X, vector.Y);

        /// <summary>
        /// Converts the size; a negative width or height (which WPF refuses) becomes zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Size ToWpf(this DrawingSize size) => new(Math.Max(0, size.Width), Math.Max(0, size.Height));

        /// <summary>
        /// Converts the rectangle; a negative width or height (which WPF refuses) becomes zero.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Rect ToWpf(this DrawingRect rect) => new(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Thickness ToWpf(this DrawingThickness thickness) => new(thickness.Left, thickness.Top, thickness.Right, thickness.Bottom);

        public static PenLineJoin ToWpf(this DrawingLineJoin lineJoin) => lineJoin switch
        {
            DrawingLineJoin.Bevel => PenLineJoin.Bevel,
            DrawingLineJoin.Round => PenLineJoin.Round,
            _ => PenLineJoin.Miter,
        };

        public static HorizontalAlignment ToWpf(this DrawingHorizontalAlignment alignment) => alignment switch
        {
            DrawingHorizontalAlignment.Center => HorizontalAlignment.Center,
            DrawingHorizontalAlignment.Right => HorizontalAlignment.Right,
            _ => HorizontalAlignment.Left,
        };

        public static VerticalAlignment ToWpf(this DrawingVerticalAlignment alignment) => alignment switch
        {
            DrawingVerticalAlignment.Center => VerticalAlignment.Center,
            DrawingVerticalAlignment.Bottom => VerticalAlignment.Bottom,
            _ => VerticalAlignment.Top,
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static FontWeight ToWpf(this DrawingFontWeight weight) => FontWeight.FromOpenTypeWeight((int)weight);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DrawingPoint ToDrawing(this Point point) => new(point.X, point.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DrawingVector ToDrawing(this Vector vector) => new(vector.X, vector.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DrawingSize ToDrawing(this Size size) => size.IsEmpty ? DrawingSize.Empty : new(size.Width, size.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DrawingRect ToDrawing(this Rect rect) => rect.IsEmpty ? default : new(rect.X, rect.Y, rect.Width, rect.Height);
    }
}
