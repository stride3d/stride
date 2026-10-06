// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Core.Assets.Compiler;
using Stride.Core.Mathematics;
using Stride.Assets.Skyboxes;
using Stride.Editor.Annotations;
using Stride.Editor.Preview;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Skyboxes;
using Stride.Rendering;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;

namespace Stride.Assets.Presentation.Preview
{
    /// <summary>
    /// An implementation of the <see cref="AssetPreview"/> that can preview models.
    /// </summary>
    [AssetPreview<SkyboxAsset>]
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

        protected override AssetCompilerResult Compile()
        {
            var result = base.Compile();

            // The preview material is generated at run time, so the specular lookup table it references
            // is not a dependency of the skybox asset; compile it too so the preview database can serve it
            foreach (var profile in new[] { GraphicsProfile.Level_9_1, GraphicsProfile.Level_10_0 })
            {
                var lutReference = MaterialSpecularMicrofacetEnvironmentGGXLUT.CreateLookupTableReference(profile);
                var lutItem = AssetItem.Package.Session.FindAssetFromProxyObject(lutReference);
                if (lutItem != null)
                    result.BuildSteps.Add(Builder.Compile(lutItem).BuildSteps);
            }

            return result;
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
            }, Game.Content);
        }
    }
}
