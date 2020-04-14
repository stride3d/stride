// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

using Stride.Core.Mathematics;
using Stride.Assets.Materials;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.Graphics.GeometricPrimitives;
using Stride.Rendering.ProceduralModels;
using Stride.Rendering;

namespace Stride.Assets.Presentation.Preview
{
    public enum MaterialPreviewPrimitive
    {
        Sphere,
        Cube,
        Cylinder,
        Torus,
        Plane,
        Teapot,
        Cone,
        Capsule
    }

    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview materials.
    /// </summary>
    [AssetPreview(typeof(MaterialAsset), typeof(MaterialPreviewView))]
    public class MaterialPreview : PreviewFromEntity<MaterialAsset>
    {
        public const string EditorMaterialPreviewEffect = "StrideEditorMaterialPreviewEffect";

        private MaterialPreviewPrimitive previewPrimitive;

        public MaterialPreview() : base(EditorMaterialPreviewEffect)
        {
        }

        protected override async Task Initialize()
        {
            CameraScript.DefaultYaw = MathUtil.Pi; // want to see the center of the texture by default (and not corners).

            await base.Initialize();
        }

        public async void SetPrimitive(MaterialPreviewPrimitive primitive)
        {
            if (previewPrimitive != primitive)
            {
                previewPrimitive = primitive;
                await Update();
            }
        }

        /// <inheritdoc/>
        protected override PreviewEntity CreatePreviewEntity()
        {
            // load the material from the data base
            var material = LoadAsset<Material>(AssetItem.Location);

            // create a sphere model to display the material
            var proceduralModel = CreatePrimitiveModel(previewPrimitive);
            var model = proceduralModel.GenerateModel(Game.Services); // TODO: should dispose those resources at some points!
            model.Add(material);

            // create the entity, create and set the model component
            var materialEntity = new Entity { Name = BuildName() };
            materialEntity.Add(new ModelComponent { Model = model });

            var previewEntity = new PreviewEntity(materialEntity);
            previewEntity.Disposed += () => UnloadAsset(material);
            return previewEntity;
        }

        internal static ProceduralModelDescriptor CreatePrimitiveModel(MaterialPreviewPrimitive primitive)
        {
            switch (primitive)
            {
                case MaterialPreviewPrimitive.Sphere:
                    return new ProceduralModelDescriptor { Type = new SphereProceduralModel() };
                case MaterialPreviewPrimitive.Cube:
                    return new ProceduralModelDescriptor { Type = new CubeProceduralModel() };
                case MaterialPreviewPrimitive.Cylinder:
                    return new ProceduralModelDescriptor { Type = new CylinderProceduralModel() };
                case MaterialPreviewPrimitive.Torus:
                    return new ProceduralModelDescriptor { Type = new TorusProceduralModel() };
                case MaterialPreviewPrimitive.Plane:
                    return new ProceduralModelDescriptor { Type = new PlaneProceduralModel { GenerateBackFace = true, Normal = NormalDirection.UpZ } };
                case MaterialPreviewPrimitive.Teapot:
                    return new ProceduralModelDescriptor { Type = new TeapotProceduralModel() };
                case MaterialPreviewPrimitive.Cone:
                    return new ProceduralModelDescriptor { Type = new ConeProceduralModel() };
                case MaterialPreviewPrimitive.Capsule:
                    return new ProceduralModelDescriptor { Type = new CapsuleProceduralModel() };
                default:
                    throw new ArgumentOutOfRangeException(nameof(primitive));
            }
        }
    }
}
