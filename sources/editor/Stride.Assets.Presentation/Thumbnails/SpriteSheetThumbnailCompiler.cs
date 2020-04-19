// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Assets.Sprite;
using Stride.Editor.Thumbnails;
using Stride.Graphics;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Thumbnails
{
    /// <summary>
    /// The thumbnail builder for <see cref="SpriteSheetAsset"/>.
    /// </summary>
    [AssetCompiler(typeof(SpriteSheetAsset), typeof(ThumbnailCompilationContext))]
    public class SpriteSheetThumbnailCompiler : ThumbnailCompilerBase<SpriteSheetAsset>
    {
        public SpriteSheetThumbnailCompiler()
        {
            IsStatic = false;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new SpriteSheetThumbnailCommand(context, assetItem, originalPackage, thumbnailStorageUrl,
                new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        public class SpriteSheetThumbnailCommand : ThumbnailFromSpriteBatchCommand<SpriteSheet>
        {
            // ReSharper disable once StaticFieldInGenericType
            private static readonly RectangleF[] DestinationRegions = 
            {
                new RectangleF(0,0,0.5f,0.5f), 
                new RectangleF(0.5f,0,0.5f,0.5f), 
                new RectangleF(0,0.5f,0.5f,0.5f), 
                new RectangleF(0.5f,0.5f,0.5f,0.5f) 
            };

            public SpriteSheetThumbnailCommand(ThumbnailCompilerContext context, AssetItem assetItem, IAssetFinder assetFinder, string url, ThumbnailCommandParameters parameters)
                : base(context, assetItem, assetFinder, url, parameters)
            {
            }

            protected override void RenderSprites(RenderDrawContext context)
            {
                if (LoadedAsset == null || LoadedAsset.Sprites == null || LoadedAsset.Sprites.Count == 0)
                    return;

                var imageIndex = 0f;
                foreach (RectangleF region in DestinationRegions)
                {
                    var image = LoadedAsset.Sprites[(int)imageIndex];
                    var middleFrameRegion = image.Region;
                    var origin = new Vector2(image.Region.Width / 2f, image.Region.Height / 2f);

                    var thumbnailSize = new Vector2(Parameters.ThumbnailSize.X, Parameters.ThumbnailSize.Y);
                    var thumbnailRegionRatio = Math.Min(thumbnailSize.X / middleFrameRegion.Width, thumbnailSize.Y / middleFrameRegion.Height);
                    var destinationSize = new Vector2(middleFrameRegion.Width * thumbnailRegionRatio, middleFrameRegion.Height * thumbnailRegionRatio);
                    var destinationRectangle = new RectangleF((thumbnailSize.X - destinationSize.X) / 2, (thumbnailSize.Y - destinationSize.Y) / 2, destinationSize.X, destinationSize.Y);

                    destinationRectangle.Width *= region.Width;
                    destinationRectangle.Height *= region.Height;

                    if (image.Orientation == ImageOrientation.Rotated90)
                    {
                        var swap = destinationRectangle.X;
                        destinationRectangle.X = destinationRectangle.Y;
                        destinationRectangle.Y = swap;
                        swap = destinationRectangle.Width;
                        destinationRectangle.Width = destinationRectangle.Height;
                        destinationRectangle.Height = swap;
                    }
                    destinationRectangle.X = region.X * thumbnailSize.X + region.Width * destinationRectangle.X + 0.5f * destinationRectangle.Width;
                    destinationRectangle.Y = region.Y * thumbnailSize.Y + region.Height * destinationRectangle.Y + 0.5f * destinationRectangle.Height;

                    if (image.Texture != null)
                        SpriteBatch.Draw(image.Texture, destinationRectangle, middleFrameRegion, Color.White, 0f, origin, SpriteEffects.None, image.Orientation);

                    // increase the sprite index 
                    imageIndex = Math.Min(LoadedAsset.Sprites.Count - 1, imageIndex + LoadedAsset.Sprites.Count / (float)DestinationRegions.Length);
                }
            }
        }
    }
}
