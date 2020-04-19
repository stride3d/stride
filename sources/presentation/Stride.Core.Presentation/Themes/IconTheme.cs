// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Media;
using Stride.Core.Presentation.Drawing;

namespace Stride.Core.Presentation.Themes
{
    public struct IconTheme
    {
        public IconTheme(string name, Color backgroundColor)
        {
            Name = name;
            BackgroundColor = backgroundColor;
        }

        public string Name { get; }

        public Color BackgroundColor { get; }

        public double BackgroundLuminosity => BackgroundColor.ToHslColor().Luminosity;

    }
}
