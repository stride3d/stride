// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.IO;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.IO;
using Stride.Assets.SpriteFont;
using Stride.Editor.Resources;
using Stride.Editor.Thumbnails;
using Stride.Graphics;

namespace Stride.Assets.Presentation.Thumbnails
{
    /// <summary>
    /// The compiler used by default to create thumbnails when the user has not explicitly defined the compiler to use for its asset.
    /// </summary>
    [AssetCompiler(typeof(SpriteFontAsset), typeof(ThumbnailCompilationContext))]
    public class FontThumbnailCompiler : ThumbnailCompilerBase<SpriteFontAsset>
    {
        public FontThumbnailCompiler()
        {
            IsStatic = false;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            UFile fontPathOnDisk = Asset.FontSource.GetFontPath();
            bool sourceIsValid = (fontPathOnDisk != null && File.Exists(fontPathOnDisk));

            if (sourceIsValid)
            {
                result.BuildSteps.Add(new ThumbnailBuildStep(new FontThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage,
                    new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
            }
            else
            {
                var thumbnailSize = context.ThumbnailResolution;
                var isLinear = context.GetGameSettingsAsset().GetOrCreate<RenderingSettings>().ColorSpace == ColorSpace.Linear;
                result.Error($"Source is null or unreachable for Texture Asset [{Asset}]");
                result.BuildSteps.Add(new StaticThumbnailCommand<SpriteFontAsset>(thumbnailStorageUrl, DefaultThumbnails.AssetBrokenThumbnail, thumbnailSize, isLinear, assetItem.Package));
            }
        }
    }

    /// <summary>
    /// Command used to build the thumbnail of the texture in the storage
    /// </summary>
    internal class FontThumbnailBuildCommand : ThumbnailFromTextureCommand<Graphics.SpriteFont>
    {
        public FontThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem assetItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
            : base(context, assetItem, assetFinder, url, description)
        {
        }

        protected override void SetThumbnailParameters()
        {
            Font = LoadedAsset;
            FontSize = 0.75f;
            TitleText = BuildTitleText();

            if (Font != null && Font.FontType == SpriteFontType.SDF)
                EffectInstance = UIBatch.SDFSpriteFontEffect;
        }

        protected virtual string BuildTitleText()
        {
            var fontAsset = (SpriteFontAsset)Parameters.Asset;
            var title = fontAsset.FontSource.GetFontName();
            var splitedFontName = SpitStringIntoSeveralLines(title); // if the name is too long we insert line returns

            return splitedFontName + "\n" + fontAsset.FontType.Size + " " + fontAsset.FontSource.Style;
        }

        /// <summary>
        /// Split a string at space characters so that it is not longer than maxCharaterNumber
        /// </summary>
        /// <param name="fontName">The string to split at its white spaces</param>
        /// <param name="maxCharacterNumber">The maximum character than a line can have</param>
        /// <returns></returns>
        protected static string SpitStringIntoSeveralLines(string fontName, int maxCharacterNumber = 10)

        {
            var fontNameParts = fontName.Split(' ');
            if (fontNameParts.Length > 1)
            {
                var currentLine = fontNameParts[0];
                fontName = currentLine;
                for (int i = 1; i < fontNameParts.Length; i++)
                {
                    var concatWithNext = currentLine + " " + fontNameParts[i];

                    if (concatWithNext.Length > maxCharacterNumber)
                    {
                        fontName = fontName + "\n" + fontNameParts[i];
                        currentLine = fontNameParts[i];
                    }
                    else
                    {
                        fontName = fontName + " " + fontNameParts[i];
                        currentLine = concatWithNext;
                    }
                }
            }

            return fontName;
        }
    }
}
