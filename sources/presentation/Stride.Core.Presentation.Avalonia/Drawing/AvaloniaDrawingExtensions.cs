// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.Avalonia.Drawing;

/// <summary>
/// Conversions between the drawing types of <see cref="Stride.Core.Presentation.Drawing"/> and Avalonia's.
/// </summary>
public static class AvaloniaDrawingExtensions
{
    public static Point ToAvalonia(this DrawingPoint point) => new(point.X, point.Y);

    public static Vector ToAvalonia(this DrawingVector vector) => new(vector.X, vector.Y);

    public static Size ToAvalonia(this DrawingSize size) => new(Math.Max(0, size.Width), Math.Max(0, size.Height));

    public static Rect ToAvalonia(this DrawingRect rect) => new(rect.X, rect.Y, Math.Max(0, rect.Width), Math.Max(0, rect.Height));

    public static DrawingPoint ToDrawing(this Point point) => new(point.X, point.Y);

    public static DrawingVector ToDrawing(this Vector vector) => new(vector.X, vector.Y);

    public static DrawingSize ToDrawing(this Size size) => new(size.Width, size.Height);

    public static DrawingRect ToDrawing(this Rect rect) => new(rect.X, rect.Y, rect.Width, rect.Height);
}
