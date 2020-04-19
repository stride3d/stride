// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;

using Stride.Graphics.Data;

namespace Stride.Graphics.Font
{
    /// <summary>
    /// A font factory initializing only the data members of font. 
    /// Used when creating a font in the only purpose use serializing on disk.
    /// </summary>
    public class FontDataFactory : IFontFactory
    {
        public SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            if (textures == null) throw new ArgumentNullException("textures");

            return new OfflineRasterizedSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter);
        }

        public SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            // creates the textures from the images if any.
            Texture[] textures = null;
            if (images != null)
            {
                textures = new Texture[images.Count];
                for (int i = 0; i < textures.Length; i++)
                    textures[i] = images[i].ToSerializableVersion();
            }
            
            return new OfflineRasterizedSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter);
        }

        public SpriteFont NewDynamic(float defaultSize, string fontName, FontStyle style, FontAntiAliasMode antiAliasMode, bool useKerning, float extraSpacing, float extraLineSpacing, char defaultCharacter)
        {
            return new RuntimeRasterizedSpriteFont
            {
                Size = defaultSize,
                DefaultCharacter = defaultCharacter,
                FontName = fontName,
                ExtraLineSpacing = extraLineSpacing,
                ExtraSpacing = extraSpacing,
                Style = style,
                UseKerning = useKerning,
                AntiAlias = antiAliasMode,
            };
        }

        public SpriteFont NewScalable(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            if (textures == null) throw new ArgumentNullException("textures");

            return new SignedDistanceFieldSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter);
        }

        public SpriteFont NewScalable(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            // creates the textures from the images if any.
            Texture[] textures = null;
            if (images != null)
            {
                textures = new Texture[images.Count];
                for (int i = 0; i < textures.Length; i++)
                    textures[i] = images[i].ToSerializableVersion();
            }

            return new SignedDistanceFieldSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter);
        }
    }
}
