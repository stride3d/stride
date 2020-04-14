// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.BuildEngine;
using Stride.Core.Mathematics;
using Stride.Assets.Materials;
using Stride.Editor.Thumbnails;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Presentation.Thumbnails
{
    [AssetCompiler(typeof(MaterialAsset), typeof(ThumbnailCompilationContext))]
    public class MaterialThumbnailCompiler : ThumbnailCompilerBase<MaterialAsset>
    {
        public MaterialThumbnailCompiler()
        {
            IsStatic = false;
            Priority = -5000;
        }

        protected override void CompileThumbnail(ThumbnailCompilerContext context, string thumbnailStorageUrl, AssetItem assetItem, Package originalPackage, AssetCompilerResult result)
        {
            result.BuildSteps.Add(new ThumbnailBuildStep(new MaterialThumbnailBuildCommand(context, thumbnailStorageUrl, assetItem, originalPackage, new ThumbnailCommandParameters(Asset, thumbnailStorageUrl, context.ThumbnailResolution))));
        }

        /// <summary>
        /// Command used to build the thumbnail of the texture in the storage
        /// </summary>
        private class MaterialThumbnailBuildCommand : ThumbnailFromEntityCommand<Material>
        {
            private const string EditorMaterialPreviewEffect = "StrideEditorMaterialPreviewEffect";

            private Model model;

            public MaterialThumbnailBuildCommand(ThumbnailCompilerContext context, string url, AssetItem assetItem, IAssetFinder assetFinder, ThumbnailCommandParameters description)
                : base(context, assetItem, assetFinder, url, description)
            {
            }

            protected override string ModelEffectName => EditorMaterialPreviewEffect;

            protected override Entity CreateEntity()
            {
                // create a sphere model to display the material
                var proceduralModel = new ProceduralModelDescriptor { Type = new SphereProceduralModel { MaterialInstance = { Material = LoadedAsset } } };
                model = proceduralModel.GenerateModel(Services);

                // create the entity, create and set the model component
                var materialEntity = new Entity { Name = "thumbnail Entity of material: " + AssetUrl };
                materialEntity.Add(new ModelComponent { Model = model });

                return materialEntity;
            }

            protected override void AdjustEntity()
            {
                base.AdjustEntity();

                // override the rotation so that the part of the model facing the screen display the center of the material by default and not the extremities
                Entity.Transform.Rotation = Quaternion.RotationY(MathUtil.Pi);
            }

            protected override void DestroyScene(Scene scene)
            {
                // TODO dispose resources allocated by the procedural model "model.Dispose"
                base.DestroyScene(scene);
            }
        }
    }
}
