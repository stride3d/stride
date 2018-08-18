// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Xenko.Core;
using Xenko.Core.IO;

namespace Xenko.Graphics.Font
{
    /// <summary>
    /// The system managing the fonts.
    /// </summary>
    public class FontSystem : IFontFactory
    {
        internal int FrameCount { get; private set; }
        internal FontManager FontManager { get; private set; }
        internal GraphicsDevice GraphicsDevice { get; private set; }
        internal FontCacheManager FontCacheManager { get; private set; }
        internal readonly HashSet<SpriteFont> AllocatedSpriteFonts = new HashSet<SpriteFont>();

        /// <summary>
        /// Create a new instance of <see cref="FontSystem" /> base on the provided <see cref="GraphicsDevice" />.
        /// </summary>
        public FontSystem()
        {
        }

        /// <summary>
        /// Load this system.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <exception cref="System.ArgumentNullException">graphicsDevice</exception>
        public void Load(GraphicsDevice graphicsDevice, IDatabaseFileProviderService fileProviderService)
        {
            // TODO possibly load cached character bitmaps from the disk
            if (graphicsDevice == null) throw new ArgumentNullException("graphicsDevice");
            GraphicsDevice = graphicsDevice;
            FontManager = new FontManager(fileProviderService);
            FontCacheManager = new FontCacheManager(this);
        }

        public void Draw()
        {
            ++FrameCount;
        }

        public void Unload()
        {
            // TODO possibly save generated characters bitmaps on the disk
            FontManager.Dispose();
            FontCacheManager.Dispose();

            // Dispose create sprite fonts
            foreach (var allocatedSpriteFont in AllocatedSpriteFonts.ToArray())
                allocatedSpriteFont.Dispose();
        }

        public SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            var font = new OfflineRasterizedSpriteFont(size, glyphs, null, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter) { FontSystem = this };

            // affects the textures from the images.
            foreach (var image in images)
                font.StaticTextures.Add(Texture.New(GraphicsDevice, image).DisposeBy(font));

            return font;
        }

        public SpriteFont NewScalable(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            var font = new SignedDistanceFieldSpriteFont(size, glyphs, null, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter) { FontSystem = this };

            // affects the textures from the images.
            foreach (var image in images)
                font.StaticTextures.Add(Texture.New(GraphicsDevice, image).DisposeBy(font));

            return font;
        }

        public SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            return new OfflineRasterizedSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter) { FontSystem = this };
        }

        public SpriteFont NewScalable(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            return new SignedDistanceFieldSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter) { FontSystem = this };
        }

        public SpriteFont NewDynamic(float defaultSize, string fontName, FontStyle style, FontAntiAliasMode antiAliasMode = FontAntiAliasMode.Default, bool useKerning = false, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            var font = new RuntimeRasterizedSpriteFont
            {
                Size = defaultSize,
                FontName = fontName,
                Style = style,
                AntiAlias = antiAliasMode,
                UseKerning = useKerning,
                ExtraSpacing = extraSpacing,
                ExtraLineSpacing = extraLineSpacing,
                DefaultCharacter = defaultCharacter,
                FontSystem = this,
            };

            return font;
        }
    }
}
