// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Assets.Presentation.Preview.Views;
using Xenko.Assets.Skyboxes;
using Xenko.Editor.Preview;
using Xenko.Engine;
using Xenko.Rendering.Skyboxes;
using Xenko.Graphics;
using Xenko.Graphics.GeometricPrimitives;
using Xenko.Rendering;
using Xenko.Rendering.Colors;
using Xenko.Rendering.Lights;
using Xenko.Rendering.Materials;
using Xenko.Rendering.Materials.ComputeColors;
using Xenko.Rendering.ProceduralModels;

namespace Xenko.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview models.
    /// </summary>
    [AssetPreview(typeof(SkyboxAsset), typeof(SkyboxPreviewView))]
    public class SkyboxPreview : PreviewFromEntity<SkyboxAsset>
    {
        private Entity targetEntity;
        private ModelComponent previewModel;

        public float Metalness { get; private set; } = 1.0f;
        public float Glossiness { get; private set; } = 1.0f;

        public new async void ResetCamera()
        {
            await IsInitialized();

            // Hint view distance
            CameraScript.ResetViewTarget(targetEntity);
            CameraScript.ResetViewAngle();
        }

        public void SetMetalness(float metalness)
        {
            Metalness = metalness;
            previewModel.Materials[0] = CreateMaterial(Metalness, Glossiness);
        }

        public void SetGlossiness(float glossiness)
        {
            Glossiness = glossiness;
            previewModel.Materials[0] = CreateMaterial(Metalness, Glossiness);
        }

        protected override void SetupLighting(Entity camera)
        {
            // No default lighting
        }

        protected override PreviewEntity CreatePreviewEntity()
        {
            var skybox = LoadAsset<Skybox>(AssetItem.Location);

            var rootEntity = new Entity();

            // Create skybox lighting
            var skyLight = new Entity();
            skyLight.Add(new LightComponent { Type = new LightSkybox { Skybox = skybox } });
            rootEntity.AddChild(skyLight);

            targetEntity = CreateMaterialPreview(rootEntity, Vector3.Zero, out previewModel);

            var previewEntity = new PreviewEntity(rootEntity);
            previewEntity.Disposed += () => UnloadAsset(skybox);
            return previewEntity;
        }

        protected override async Task Initialize()
        {
            await base.Initialize();
            CameraScript.DefaultYaw = 0.0f;
            CameraScript.DefaultPitch = MathUtil.DegreesToRadians(-40.0f);
            ResetCamera();
        }

        protected override void PrepareLoadedEntity()
        {
        }

        private Entity CreateMaterialPreview(Entity root, Vector3 position, out ModelComponent previewModel)
        {
            // create a sphere model to display the material
            var proceduralModel = new ProceduralModelDescriptor { Type = new SphereProceduralModel() };
            var model = proceduralModel.GenerateModel(Game.Services); // TODO: should dispose those resources at some points!

            // create the entity, create and set the model component
            var materialEntity = new Entity { Name = BuildName() };
            materialEntity.Add(previewModel = new ModelComponent
            {
                Model = model,
                Materials = { [0] = CreateMaterial(Metalness, Glossiness) }
            });
            materialEntity.Transform.Position = position;

            root.AddChild(materialEntity);
            return materialEntity;
        }

        private Material CreateMaterial(float metalness, float glossiness)
        {
            return Material.New(Game.GraphicsDevice, new MaterialDescriptor
            {
                Attributes = new MaterialAttributes
                {
                    Diffuse = new MaterialDiffuseMapFeature
                    {
                        DiffuseMap = new ComputeColor(Color4.White)
                    },
                    MicroSurface = new MaterialGlossinessMapFeature
                    {
                        GlossinessMap = new ComputeFloat(glossiness)
                    },
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                    Specular = new MaterialMetalnessMapFeature
                    {
                        MetalnessMap = new ComputeFloat(metalness)
                    },
                    SpecularModel = new MaterialSpecularMicrofacetModelFeature()
                }
            });
        }
    }
}
