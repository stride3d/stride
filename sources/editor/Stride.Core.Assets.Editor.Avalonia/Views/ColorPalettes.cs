// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public static class ColorPalettes
{
    public static readonly Dictionary<string, Color4> Metals = new(9)
    {
        { "Silver", new Color4(0.98695203f, 0.981576133f, 0.960581067f) },
        { "Aluminium", new Color4(0.959559115f, 0.963518888f, 0.964957682f) },
        { "Gold", new Color4(1.0f, 0.88565079f, 0.609162496f) },
        { "Copper", new Color4(0.979292159f, 0.814900815f, 0.754550145f) },
        { "Chromium", new Color4(0.761787833f, 0.765888197f, 0.764724015f) },
        { "Nickel", new Color4(0.827766422f, 0.797984931f, 0.74652364f) },
        { "Titanium", new Color4(0.756946965f, 0.727607463f, 0.695207239f) },
        { "Cobalt", new Color4(0.829103572f, 0.824958926f, 0.812750243f) },
        { "Platinum", new Color4(0.834934076f, 0.814845027f, 0.783999116f) },
    };
}
