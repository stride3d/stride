// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// The thickness of a frame around a rectangle (margins, padding), in device independent units, independent from any UI toolkit.
/// </summary>
/// <param name="Left">The left side.</param>
/// <param name="Top">The top side.</param>
/// <param name="Right">The right side.</param>
/// <param name="Bottom">The bottom side.</param>
public readonly record struct DrawingThickness(double Left, double Top, double Right, double Bottom)
{
    /// <summary>
    /// Initializes a thickness with the same length on its four sides.
    /// </summary>
    /// <param name="uniformLength">The length of each side.</param>
    public DrawingThickness(double uniformLength)
        : this(uniformLength, uniformLength, uniformLength, uniformLength)
    {
    }
}
