// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// A size in a drawing, in device independent units, independent from any UI toolkit.
/// </summary>
/// <param name="Width">The width.</param>
/// <param name="Height">The height.</param>
public readonly record struct DrawingSize(double Width, double Height)
{
    /// <summary>
    /// A size of zero width and height.
    /// </summary>
    public static DrawingSize Empty => default;
}
