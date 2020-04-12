// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Themes
{
    public enum ThemeType
    {
        [Display("Expression Dark (Default)")]
        ExpressionDark,
        [Display("Dark Steel")]
        DarkSteel,
    }

    public static class ThemeTypeExtensions
    {
        public static IconThemeSelector.ThemeBase GetThemeBase(this ThemeType themeType)
        {
            switch (themeType)
            {
                case ThemeType.ExpressionDark:
                case ThemeType.DarkSteel:
                default:
                    return IconThemeSelector.ThemeBase.Dark;
            }
        }
    }
}
