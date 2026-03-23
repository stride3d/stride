// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;

using Stride.Core.Mathematics;

namespace Stride.Core.Assets.Editor.ViewModel
{
    public static class ColorPaletteViewModel
    {
        public const string PaletteFileName = "ColorPalette.gpl";

        public static readonly Dictionary<string, Color3> DefaultColors = new Dictionary<string, Color3>
        {
            { "Silver",    new Color3(0.98695203f, 0.981576133f, 0.960581067f) },
            { "Aluminium", new Color3(0.959559115f, 0.963518888f, 0.964957682f) },
            { "Gold",      new Color3(1.0f,         0.88565079f,  0.609162496f) },
            { "Copper",    new Color3(0.979292159f, 0.814900815f, 0.754550145f) },
            { "Chromium",  new Color3(0.761787833f, 0.765888197f, 0.764724015f) },
            { "Nickel",    new Color3(0.827766422f, 0.797984931f, 0.74652364f)  },
            { "Titanium",  new Color3(0.756946965f, 0.727607463f, 0.695207239f) },
            { "Cobalt",    new Color3(0.829103572f, 0.824958926f, 0.812750243f) },
            { "Platinum",  new Color3(0.834934076f, 0.814845027f, 0.783999116f) }, // Fixed typo: was "Platimium"
        };

        private static Dictionary<string, Color3>? _colors;
        public static Dictionary<string, Color3> Colors
        {
            get
            {
                if (_colors is null)
                    _colors = LoadColors();

                return _colors;
            }
        }

        public static void Reload() => _colors = null;

        private static Dictionary<string, Color3> LoadColors()
        {
            var palettePath = Path.Combine(
                Path.GetDirectoryName(typeof(ColorPaletteViewModel).Assembly.Location) ?? string.Empty,
                PaletteFileName);

            var parsed = GplPaletteParser.TryParse(palettePath);

            return parsed ?? DefaultColors;
        }
    }
}
