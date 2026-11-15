// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// A rectangle in a drawing, in device independent units, independent from any UI toolkit.
/// </summary>
/// <param name="X">The left coordinate.</param>
/// <param name="Y">The top coordinate.</param>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct DrawingRect(double X, double Y, double Width, double Height)
{
    /// <summary>
    /// Initializes a rectangle from its top-left corner and its size.
    /// </summary>
    public DrawingRect(DrawingPoint location, DrawingSize size)
        : this(location.X, location.Y, size.Width, size.Height)
    {
    }

    /// <summary>
    /// Initializes the smallest rectangle that contains the two given points.
    /// </summary>
    public DrawingRect(DrawingPoint point1, DrawingPoint point2)
        : this(Math.Min(point1.X, point2.X), Math.Min(point1.Y, point2.Y), Math.Abs(point2.X - point1.X), Math.Abs(point2.Y - point1.Y))
    {
    }

    public double Left => X;

    public double Top => Y;

    public double Right => X + Width;

    public double Bottom => Y + Height;

    /// <summary>
    /// The top-left corner.
    /// </summary>
    public DrawingPoint Location => new(X, Y);

    public DrawingSize Size => new(Width, Height);

    /// <summary>
    /// The center of the rectangle.
    /// </summary>
    public DrawingPoint Center => new(X + Width / 2, Y + Height / 2);

    /// <summary>
    /// Whether the rectangle has no area.
    /// </summary>
    public bool IsEmpty => Width <= 0 || Height <= 0;

    /// <summary>
    /// Whether the given point is inside the rectangle or on its edges.
    /// </summary>
    public bool Contains(DrawingPoint point) => point.X >= Left && point.X <= Right && point.Y >= Top && point.Y <= Bottom;

    /// <summary>
    /// Returns this rectangle shrunk by the given thickness on each side.
    /// </summary>
    /// <remarks>The result has a negative width or height when the thickness is larger than the rectangle.</remarks>
    public DrawingRect Deflate(DrawingThickness thickness)
        => new(X + thickness.Left, Y + thickness.Top, Width - thickness.Left - thickness.Right, Height - thickness.Top - thickness.Bottom);

    /// <summary>
    /// Returns this rectangle grown by the given thickness on each side.
    /// </summary>
    public DrawingRect Inflate(DrawingThickness thickness)
        => new(X - thickness.Left, Y - thickness.Top, Width + thickness.Left + thickness.Right, Height + thickness.Top + thickness.Bottom);
}
