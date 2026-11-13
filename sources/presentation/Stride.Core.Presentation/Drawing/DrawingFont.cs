// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Drawing;

/// <summary>
/// The font of a text in a drawing, independent from any UI toolkit.
/// </summary>
/// <param name="Family">The name of the font family, for example <c>Arial</c>.</param>
/// <param name="Size">The size of the font, in device independent units.</param>
/// <param name="Weight">The weight of the font.</param>
public readonly record struct DrawingFont(string Family, double Size, DrawingFontWeight Weight = DrawingFontWeight.Normal);
