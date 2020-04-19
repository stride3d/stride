// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using System.IO;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Assets.Textures;
using Stride.Editor.Resources;
using Stride.Editor.Thumbnails;
using Stride.Graphics;

namespace Stride.Assets.Presentation.Thumbnails
{
    /// <summary>
    /// The thumbnail builder for <see cref="TextureAsset"/>.
    /// </summary>
    [AssetCompiler(typeof(TextureAsset), typeof(ThumbnailCompilationContext))]
    internal class TextureThumbnailCompiler : ThumbnailCompilerBase<TextureAsset>
    {
        public TextureThumbnailCompiler()
        {
            IsStatic = false;
            Priority = -10000;
        }

        /// <inheritdoc />
        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            var thumbnailSize = context.ThumbnailResolution;
            UFile assetSource = null;

            var sourceValid = !string.IsNullOrEmpty(Asset.Source);
            if (sourceValid)
            {
                // Get absolute path of asset source on disk
                var assetDirectory = assetItem.FullPath.GetParent();
                assetSource = UPath.Combine(assetDirectory, Asset.Source);
                sourceValid = File.Exists(assetSource);
            }

            if (sourceValid)
            {
                result.BuildSteps.Add(new ThumbnailBuildStep(new TextureThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage, new TextureThumbnailParameters(Asset, assetSource, thumbnailStorageUrl, thumbnailSize))));
            }
            else
            {
                var gameSettings = context.GetGameSettingsAsset();
                result.Error($"Source is null or unreachable for Texture Asset [{Asset}]");
                result.BuildSteps.Add(new StaticThumbnailCommand<TextureAsset>(thumbnailStorageUrl, DefaultThumbnails.TextureNoSource, thumbnailSize, gameSettings.GetOrCreate<RenderingSettings>().ColorSpace == ColorSpace.Linear, assetItem.Package));
            }
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        private class TextureThumbnailBuildCommand : ThumbnailFromTextureCommand<Texture>
        {
            public TextureThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem assetItem, IAssetFinder assetFinder, TextureThumbnailParameters parameters)
                : base(context, assetItem, assetFinder, url, parameters)
            {
                // Compute color space to use during rendering with hint and color space set on texture
                var textureAsset = (TextureAsset)Parameters.Asset;
                AdditiveColor = new Color(0.0f, 0.0f, 0.0f, 0.0f);
                parameters.ColorSpace = textureAsset.Type.IsSRgb(parameters.ColorSpace) ? ColorSpace.Linear : ColorSpace.Gamma;
                Swizzle = textureAsset.Type.Hint == TextureHint.Grayscale ? SwizzleMode.RRR1 : (textureAsset.Type.Hint == TextureHint.NormalMap ? SwizzleMode.NormalMap : SwizzleMode.None);
            }

            /// <inheritdoc />
            public override IEnumerable<ObjectUrl> GetInputFiles()
            {
                yield return new ObjectUrl(UrlType.File, ((TextureThumbnailParameters)Parameters).SourcePathFromDisk);
            }

            /// <inheritdoc />
            protected override void SetThumbnailParameters()
            {
                if (LoadedAsset.ViewDimension == TextureDimension.TextureCube)
                {
                    BackgroundTexture = LoadedAsset.ToTextureView(new TextureViewDescription { ArraySlice = 0, Type = ViewType.ArrayBand });
                }
                // TODO also handle Texture1D and Texture3D
                else
                {
                    BackgroundTexture = LoadedAsset;
                }
            }
        }
    }

    /// <summary>
    /// SharedParameters used for building the texture's thumbnail in the storage.
    /// </summary>
    [DataContract]
    public class TextureThumbnailParameters : ThumbnailCommandParameters
    {
        public TextureThumbnailParameters()
        {
        }

        public TextureThumbnailParameters(TextureAsset asset, string sourcePathFromDisk, string thumbnailUrl, Int2 thumbnailSize)
            : base(asset, thumbnailUrl, thumbnailSize)
        {
            SourcePathFromDisk = sourcePathFromDisk;
        }

        public string SourcePathFromDisk;
    }
}
