// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// A point of a drawing, in device independent units, independent from any UI toolkit.
/// </summary>
/// <param name="X">The horizontal coordinate.</param>
/// <param name="Y">The vertical coordinate.</param>
public readonly record struct DrawingPoint(double X, double Y)
{
    /// <summary>
    /// Returns this point moved by the given offsets.
    /// </summary>
    public DrawingPoint Offset(double offsetX, double offsetY) => new(X + offsetX, Y + offsetY);

    public static DrawingVector operator -(DrawingPoint point1, DrawingPoint point2) => new(point1.X - point2.X, point1.Y - point2.Y);

    public static DrawingPoint operator +(DrawingPoint point, DrawingVector vector) => new(point.X + vector.X, point.Y + vector.Y);

    public static DrawingPoint operator -(DrawingPoint point, DrawingVector vector) => new(point.X - vector.X, point.Y - vector.Y);

    public static explicit operator DrawingVector(DrawingPoint point) => new(point.X, point.Y);
}
