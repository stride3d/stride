// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Assets.Models;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(ModelAsset), typeof(ThumbnailCompilationContext))]
    public class ModelThumbnailCompiler : ThumbnailCompilerBase<ModelAsset>
    {
        public ModelThumbnailCompiler()
        {
            IsStatic = false;
            Priority = 10000;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new ModelThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage,
                new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        private class ModelThumbnailBuildCommand : ThumbnailFromEntityCommand<Model>
        {
            public ModelThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem modelItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
                : base(context, modelItem, assetFinder, url, description)
            {
            }

            protected override Entity CreateEntity()
            {
                // create the entity, create and set the model component
                var entity = new Entity { Name = "Thumbnail Entity of model: " + AssetUrl };
                entity.Add(new ModelComponent { Model = LoadedAsset });

                return entity;
            }
        }
    }
}
