// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Stride.Core.Assets;
using Stride.Core.IO;
using Stride.Assets.SpriteFont.Compiler;
using Stride.Graphics;
using Stride.Graphics.Data;
using Stride.Graphics.Font;
using Glyph = Stride.Graphics.Font.Glyph;

namespace Stride.Assets.SpriteFont
{
    public static class SpriteFontAssetExtensions
    {
        private static readonly FontDataFactory FontDataFactory = new FontDataFactory();

        /// <summary>
        /// Generate a precompiled sprite font from the current sprite font asset.
        /// </summary>
        /// <param name="asset">The sprite font asset</param>
        /// <param name="sourceAsset">The source sprite font asset item</param>
        /// <param name="texturePath">The path of the source texture</param>
        /// <param name="srgb">Indicate if the generated texture should be srgb</param>
        /// <returns>The precompiled sprite font asset</returns>
        public static PrecompiledSpriteFontAsset GeneratePrecompiledSpriteFont(this SpriteFontAsset asset, AssetItem sourceAsset, string texturePath, bool srgb)
        {
            var staticFont = (OfflineRasterizedSpriteFont)OfflineRasterizedFontCompiler.Compile(FontDataFactory, asset, srgb);

            var referenceToSourceFont = new AssetReference(sourceAsset.Id, sourceAsset.Location);
            var glyphs = new List<Glyph>(staticFont.CharacterToGlyph.Values);
            var textures = staticFont.Textures;
            
            var imageType = ImageFileType.Png;
            var textureFileName = new UFile(texturePath).GetFullPathWithoutExtension() + imageType.ToFileExtension();

            if (textures != null && textures.Count > 0)
            {
                // save the texture   TODO support for multi-texture
                using (var stream = File.OpenWrite(textureFileName))
                    staticFont.Textures[0].GetSerializationData().Image.Save(stream, imageType);
            }

            var precompiledAsset = new PrecompiledSpriteFontAsset
            {
                Glyphs = glyphs,
                Size = asset.FontType.Size,
                Style = asset.FontSource.Style,
                OriginalFont = referenceToSourceFont,
                FontDataFile = textureFileName,
                BaseOffset = staticFont.BaseOffsetY,
                DefaultLineSpacing = staticFont.DefaultLineSpacing,
                ExtraSpacing = staticFont.ExtraSpacing,
                ExtraLineSpacing = staticFont.ExtraLineSpacing,
                DefaultCharacter = asset.DefaultCharacter,
                FontName = asset.FontSource.GetFontName(),
                IsPremultiplied = asset.FontType.IsPremultiplied,
                IsSrgb = srgb
            };

            return precompiledAsset;
        }


        /// <summary>
        /// Generate a precompiled sprite font from the current sprite font asset.
        /// </summary>
        /// <param name="asset">The sprite font asset</param>
        /// <param name="sourceAsset">The source sprite font asset item</param>
        /// <param name="texturePath">The path of the source texture</param>
        /// <param name="srgb">Indicate if the generated texture should be srgb</param>
        /// <returns>The precompiled sprite font asset</returns>
        public static PrecompiledSpriteFontAsset GeneratePrecompiledSDFSpriteFont(this SpriteFontAsset asset, AssetItem sourceAsset, string texturePath)
        {
            // TODO create PrecompiledSDFSpriteFontAsset
            var scalableFont = (SignedDistanceFieldSpriteFont)SignedDistanceFieldFontCompiler.Compile(FontDataFactory, asset);

            var referenceToSourceFont = new AssetReference(sourceAsset.Id, sourceAsset.Location);
            var glyphs = new List<Glyph>(scalableFont.CharacterToGlyph.Values);
            var textures = scalableFont.Textures;

            var imageType = ImageFileType.Png;
            var textureFileName = new UFile(texturePath).GetFullPathWithoutExtension() + imageType.ToFileExtension();

            if (textures != null && textures.Count > 0)
            {
                // save the texture   TODO support for multi-texture
                using (var stream = File.OpenWrite(textureFileName))
                    scalableFont.Textures[0].GetSerializationData().Image.Save(stream, imageType);
            }

            var precompiledAsset = new PrecompiledSpriteFontAsset
            {
                Glyphs = glyphs,
                Size = asset.FontType.Size,
                Style = asset.FontSource.Style,
                OriginalFont = referenceToSourceFont,
                FontDataFile = textureFileName,
                BaseOffset = scalableFont.BaseOffsetY,
                DefaultLineSpacing = scalableFont.DefaultLineSpacing,
                ExtraSpacing = scalableFont.ExtraSpacing,
                ExtraLineSpacing = scalableFont.ExtraLineSpacing,
                DefaultCharacter = asset.DefaultCharacter,
                FontName = asset.FontSource.GetFontName(),
                IsPremultiplied = asset.FontType.IsPremultiplied,
                IsSrgb = false,
            };

            return precompiledAsset;
        }
    }
}
