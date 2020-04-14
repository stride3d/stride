// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Xenko.Graphics.Font
{
    public static class FontHelper
    {
        /// <summary>
        /// Build the path of a font in the database given the name of the font family and the font style.
        /// </summary>
        /// <param name="fontName">Family name of the font</param>
        /// <param name="style">The style of the font</param>
        /// <remarks>This function does not indicate it the font exists or not in the database.</remarks>
        /// <returns>The absolute path of the font in the database</returns>
        public static string GetFontPath(string fontName, FontStyle style)
        {
            var styleName = string.Empty;
            if ((style & FontStyle.Bold) == FontStyle.Bold)
                styleName += " Bold";
            if ((style & FontStyle.Italic) == FontStyle.Italic)
                styleName += " Italic";

            return "fonts/" + fontName + styleName + ".ttf";
        }
    }
}
