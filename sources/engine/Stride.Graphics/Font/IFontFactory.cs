// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// The interface to create and manage fonts.
    /// </summary>
    public interface IFontFactory
    {
        /// <summary>
        /// Create a new instance of a static font.
        /// </summary>
        /// <param name="size">The size of the font in pixels.</param>
        /// <param name="glyphs">The list of the font glyphs</param>
        /// <param name="textures">The list of textures containing the font character data</param>
        /// <param name="baseOffset">The number of pixels from the absolute top of the line to the base of the characters.</param>
        /// <param name="defaultLineSpacing">The default line spacing of the font.</param>
        /// <param name="kernings">The list of the kerning information of the font</param>
        /// <param name="extraSpacing">The character extra spacing in pixels. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="extraLineSpacing">This is the extra distance in pixels to add between each line of text. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="defaultCharacter">The default character fall-back.</param>
        /// <remarks>The font does not copy the provided textures, glyphs information. 
        /// Provided data should not be modified after the creation of the font.
        /// The textures should be disposed manually (if useless) after that the sprite font is not used anymore.</remarks>
        /// <returns>The newly created static font</returns>
        SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing,
                             IList<Kerning> kernings = null, float extraSpacing = 0f, float extraLineSpacing = 0f, char defaultCharacter = ' ');

        /// <summary>
        /// Create a new instance of a static font.
        /// </summary>
        /// <param name="size">The size of the font in pixels.</param>
        /// <param name="glyphs">The list of the font glyphs</param>
        /// <param name="images">The list of images containing the font character data</param>
        /// <param name="baseOffset">The number of pixels from the absolute top of the line to the base of the characters.</param>
        /// <param name="defaultLineSpacing">The default line spacing of the font.</param>
        /// <param name="kernings">The list of the kerning information of the font</param>
        /// <param name="extraSpacing">The character extra spacing in pixels. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="extraLineSpacing">This is the extra distance in pixels to add between each line of text. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="defaultCharacter">The default character fall-back.</param>
        /// <remarks>The font does not copy the provided glyphs information. Provided glyphs should not be modified after the creation of the font.</remarks>
        /// <returns>The newly created static font</returns>
        SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing,
                             IList<Kerning> kernings = null, float extraSpacing = 0f, float extraLineSpacing = 0f, char defaultCharacter = ' ');

        /// <summary>
        /// Create a new instance of a dynamic font.
        /// </summary>
        /// <param name="defaultSize">The default size of the font in pixels.</param>
        /// <param name="fontName">The family name of the (TrueType) font.</param>
        /// <param name="antiAliasMode">The anti-aliasing mode to use when rendering the font. By default, font textures are rendered in levels of grey.</param>
        /// <param name="style">The style for the font. A combinaison of 'regular', 'bold' or 'italic'.</param>
        /// <param name="useKerning">Specifies whether to use kerning information when rendering the font. Default value is false (NOT SUPPORTED YET)</param>
        /// <param name="extraSpacing">The character extra spacing in pixels. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="extraLineSpacing">This is the extra distance in pixels to add between each line of text. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="defaultCharacter">The default character fall-back.</param>
        /// <returns>The newly created dynamic font</returns>
        SpriteFont NewDynamic(float defaultSize, string fontName, FontStyle style, FontAntiAliasMode antiAliasMode = FontAntiAliasMode.Default,
                              bool useKerning = false, float extraSpacing = 0f, float extraLineSpacing = 0f, char defaultCharacter = ' ');

        /// <summary>
        /// Create a new instance of a scalable font.
        /// </summary>
        /// <param name="size">The size of the font in pixels.</param>
        /// <param name="glyphs">The list of the font glyphs</param>
        /// <param name="textures">The list of textures containing the font character data</param>
        /// <param name="baseOffset">The number of pixels from the absolute top of the line to the base of the characters.</param>
        /// <param name="defaultLineSpacing">The default line spacing of the font.</param>
        /// <param name="kernings">The list of the kerning information of the font</param>
        /// <param name="extraSpacing">The character extra spacing in pixels. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="extraLineSpacing">This is the extra distance in pixels to add between each line of text. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="defaultCharacter">The default character fall-back.</param>
        /// <remarks>The font does not copy the provided textures, glyphs information. 
        /// Provided data should not be modified after the creation of the font.
        /// The textures should be disposed manually (if useless) after that the sprite font is not used anymore.</remarks>
        /// <returns>The newly created static font</returns>
        SpriteFont NewScalable(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing,
                             IList<Kerning> kernings = null, float extraSpacing = 0f, float extraLineSpacing = 0f, char defaultCharacter = ' ');

        /// <summary>
        /// Create a new instance of a scalable font.
        /// </summary>
        /// <param name="size">The size of the font in pixels.</param>
        /// <param name="glyphs">The list of the font glyphs</param>
        /// <param name="images">The list of images containing the font character data</param>
        /// <param name="baseOffset">The number of pixels from the absolute top of the line to the base of the characters.</param>
        /// <param name="defaultLineSpacing">The default line spacing of the font.</param>
        /// <param name="kernings">The list of the kerning information of the font</param>
        /// <param name="extraSpacing">The character extra spacing in pixels. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="extraLineSpacing">This is the extra distance in pixels to add between each line of text. Zero is default spacing, negative closer together, positive further apart.</param>
        /// <param name="defaultCharacter">The default character fall-back.</param>
        /// <remarks>The font does not copy the provided glyphs information. Provided glyphs should not be modified after the creation of the font.</remarks>
        /// <returns>The newly created static font</returns>
        SpriteFont NewScalable(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing,
                             IList<Kerning> kernings = null, float extraSpacing = 0f, float extraLineSpacing = 0f, char defaultCharacter = ' ');
    }
}
