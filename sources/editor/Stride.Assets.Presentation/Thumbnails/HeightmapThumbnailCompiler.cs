// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Assets.Physics;
using Xenko.Core.Assets;
using Xenko.Core.Assets.Compiler;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Editor.Thumbnails;
using Xenko.Graphics;
using Xenko.Physics;
using Xenko.Rendering;

namespace Xenko.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(HeightmapAsset), typeof(ThumbnailCompilationContext))]
    public class HeightmapThumbnailCompiler : ThumbnailCompilerBase<HeightmapAsset>
    {
        public HeightmapThumbnailCompiler()
        {
            IsStatic = false;
            Priority = 10050;
        }

        public override IEnumerable<ObjectUrl> GetInputFiles(AssetItem assetItem)
        {
            var asset = (HeightmapAsset)assetItem.Asset;
            var url = asset.Source.FullPath;
            if (!string.IsNullOrEmpty(url))
            {
                yield return new ObjectUrl(UrlType.File, url);
            }
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new HeightmapThumbnailCommand(context, assetItem, originalPackage, thumbnailStorageUrl,
                new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))
            { InputFilesGetter = () => GetInputFiles(assetItem) }));
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        public class HeightmapThumbnailCommand : ThumbnailFromSpriteBatchCommand<Heightmap>
        {
            private Texture texture;

            public HeightmapThumbnailCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
                : base(context, assetItem, assetFinder, url, parameters)
            {
                parameters.ColorSpace = ColorSpace.Linear;
            }

            protected override void PreloadAsset()
            {
                base.PreloadAsset();

                texture = LoadedAsset?.CreateTexture(GraphicsDevice);
            }

            protected override void UnloadAsset()
            {
                if (texture != null)
                {
                    texture.Dispose();
                    texture = null;
                }

                base.UnloadAsset();
            }

            protected override void RenderSprites(RenderDrawContext context)
            {
                if (LoadedAsset == null)
                    return;

                if (texture != null)
                {
                    var destinationRectangle = new RectangleF(0, 0, Parameters.ThumbnailSize.X, Parameters.ThumbnailSize.Y);

                    SpriteBatch.Draw(texture, destinationRectangle, new RectangleF(0, 0, texture.Width, texture.Height), Color.White, 0f, new Vector2(0, 0), SpriteEffects.None, swizzle: SwizzleMode.RRR1);
                }
            }
        }
    }
}
