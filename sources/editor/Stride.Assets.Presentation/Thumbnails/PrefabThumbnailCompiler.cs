// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Assets.Entities;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Extensions;
using Stride.Particles.Components;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(PrefabAsset), typeof(ThumbnailCompilationContext))]
    public class PrefabThumbnailCompiler : ThumbnailCompilerBase<PrefabAsset>
    {
        public PrefabThumbnailCompiler()
        {
            IsStatic = false;
            Priority = 10000;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new PrefabThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage,
                new ThumbnailCommandParameters(assetItem.Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        private class PrefabThumbnailBuildCommand : ThumbnailFromEntityCommand<Prefab>
        {
            public PrefabThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem prefabItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
                : base(context, prefabItem, assetFinder, url, description)
            {
            }

            protected override Entity CreateEntity()
            {
                // create the entity, create and set the model component
                var entity = new Entity { Name = "Thumbnail Entity of prefab: " + AssetUrl };

                foreach (var prefabEntity in LoadedAsset.Entities)
                {
                    entity.AddChild(prefabEntity);
                }

                return entity;
            }

            private void AdjustChildEntity(Entity entity, ref bool canRotateEntity)
            {
                if (entity.Components.Get<ModelComponent>() != null || entity.Components.Get<ParticleSystemComponent>() != null)
                {
                    canRotateEntity = true;
                }
                var particles = entity.Components.Get<ParticleSystemComponent>();
                if (particles?.ParticleSystem?.Settings != null && particles?.Control != null)
                {
                    particles.ParticleSystem.Settings.WarmupTime = particles.Control.ThumbnailWarmupTime;                    
                }

                foreach (var child in entity.GetChildren())
                {
                    AdjustChildEntity(child, ref canRotateEntity);
                }
            }

            protected override void AdjustEntity()
            {
                base.AdjustEntity();

                var canRotateEntity = false;
                foreach (var entity in LoadedAsset.Entities)
                {
                    AdjustChildEntity(entity, ref canRotateEntity);
                }

                if (!canRotateEntity)
                {
                    // Don't rotate if we don't have 3d
                    Entity.Transform.Rotation = Quaternion.Identity;
                }
            }
        }
    }
}
