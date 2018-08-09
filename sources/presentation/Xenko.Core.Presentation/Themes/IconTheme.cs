// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Media;
using Xenko.Core.Presentation.Drawing;

namespace Xenko.Core.Presentation.Themes
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
