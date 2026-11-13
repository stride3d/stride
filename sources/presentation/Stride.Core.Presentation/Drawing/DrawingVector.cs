// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// A displacement in a drawing, in device independent units, independent from any UI toolkit.
/// </summary>
/// <param name="X">The horizontal component.</param>
/// <param name="Y">The vertical component.</param>
public readonly record struct DrawingVector(double X, double Y)
{
    /// <summary>
    /// The length of this vector.
    /// </summary>
    public double Length => Math.Sqrt(LengthSquared);

    /// <summary>
    /// The square of the length of this vector.
    /// </summary>
    public double LengthSquared => X * X + Y * Y;

    public static DrawingVector operator +(DrawingVector vector1, DrawingVector vector2) => new(vector1.X + vector2.X, vector1.Y + vector2.Y);

    public static DrawingVector operator -(DrawingVector vector1, DrawingVector vector2) => new(vector1.X - vector2.X, vector1.Y - vector2.Y);

    public static DrawingVector operator -(DrawingVector vector) => new(-vector.X, -vector.Y);

    public static DrawingVector operator *(DrawingVector vector, double scalar) => new(vector.X * scalar, vector.Y * scalar);

    public static DrawingVector operator /(DrawingVector vector, double scalar) => new(vector.X / scalar, vector.Y / scalar);

    public static explicit operator DrawingPoint(DrawingVector vector) => new(vector.X, vector.Y);
}
