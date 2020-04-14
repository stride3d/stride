// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;
using Stride.Core.Presentation.Core;
using Stride.Editor.Resources;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails
{
    /// <summary>
    /// The compiler used by default to create thumbnails when the user has not explicitly defined the compiler to use for its asset.
    /// </summary>
    [AssetCompiler(typeof(Asset), typeof(ThumbnailCompilationContext))]
    public class CustomAssetThumbnailCompiler : ThumbnailCompilerBase<Asset>
    {
        public CustomAssetThumbnailCompiler()
        {
            IsStatic = true;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new CustomAssetThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage, new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution)));
        }
        
        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        private class CustomAssetThumbnailBuildCommand : ThumbnailFromTextureCommand<object>
        {
            public CustomAssetThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem assetItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
                : base(context, assetItem, assetFinder, url, description)
            {
            }

            protected override void PreloadAsset()
            {
            }

            protected override void SetThumbnailParameters()
            {
                var assetType = Parameters.Asset.GetType();
                TitleText = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(assetType)?.Name ?? assetType.Name;
                Font = DefaultFont;
                FontColor = Color.White;
                BackgroundColor = (Color)assetType.GetUniqueColor();
                BackgroundTexture = TextureExtensions.FromFileData(GraphicsDevice, DefaultThumbnails.UserAssetThumbnail); 
            }
        }
    }
}
