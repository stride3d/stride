// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using AvaloniaColor = Avalonia.Media.Color;

namespace Stride.Core.Presentation.Avalonia.Extensions;

public static class AvaloniaColorExtensions
{
    public static AvaloniaColor ToAvaloniaColor(this ColorHSV color)
    {
        return ToAvaloniaColor(color.ToColor());
    }

    public static AvaloniaColor ToAvaloniaColor(this Color color)
    {
        return AvaloniaColor.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static AvaloniaColor ToAvaloniaColor(this Color4 color4)
    {
        var color = (Color)color4;
        return AvaloniaColor.FromArgb(color.A, color.R, color.G, color.B);
    }

    public static AvaloniaColor ToAvaloniaColor(this Color3 color3)
    {
        var color = (Color)color3;
        return AvaloniaColor.FromArgb(255, color.R, color.G, color.B);
    }
}
