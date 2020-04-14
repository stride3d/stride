// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Media;

namespace Xenko.Core.Presentation.Themes
{
    /// <summary>
    /// Contains a predefined set of <see cref="IconTheme"/>
    /// </summary>
    public static class IconThemeSelector
    {
        public enum KnownThemes
        {
            Light,
            Dark
        }

        public static IconTheme GetIconTheme(this KnownThemes theme)
        {
            switch (theme)
            {
                case KnownThemes.Dark:
                    return new IconTheme("Dark", Color.FromRgb(16, 16, 17));

                case KnownThemes.Light: 
                    return new IconTheme("Light", Color.FromRgb(245, 245, 245));

                default:
                    return default(IconTheme);
            }
        }
    }
}
