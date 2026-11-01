// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Stride.Shaders;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// An effect is compiled for the lights of the views that draw its object. Two views look at two spheres far
    /// apart, a shadowed point light beside the first: the first sphere's effect takes the point light group and the
    /// second's does not, where one lighting permutation over every view of the frame would give it to both.
    /// </summary>
    public class LightingSetsTest : EngineTestBase
    {
        private Entity litSphere;
        private Entity unlitSphere;
        private MeshRenderFeature meshRenderFeature;

        public LightingSetsTest()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Two cameras, each drawing the scene through a forward renderer of its own
            var compositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false, graphicsProfile: GraphicsProfile.Level_11_0);
            var first = (SceneCameraRenderer)compositor.Game;
            var secondSlot = new SceneCameraSlot { Name = "Second" };
            compositor.Cameras.Add(secondSlot);
            compositor.Game = new SceneRendererCollection
            {
                Children =
                {
                    first,
                    new SceneCameraRenderer { Camera = secondSlot, Child = ForwardRendererLike((ForwardRenderer)first.Child) },
                },
            };
            SceneSystem.GraphicsCompositor = compositor;
            meshRenderFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().Single();

            CameraComponent.Slot = compositor.Cameras[0].ToSlotId();
            Scene.Entities.Add(new Entity("Second camera", new Vector3(100, 0, 0)) { new CameraComponent { Slot = secondSlot.ToSlotId() } });

            var material = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
            litSphere = Sphere("Lit sphere", new Vector3(0, 0, -5), material);
            unlitSphere = Sphere("Unlit sphere", new Vector3(100, 0, -5), material);

            // A point light has a bounding box, so only the first camera's frustum holds it
            Scene.Entities.Add(new Entity("Point light", new Vector3(0, 2, -5))
            {
                new LightComponent { Type = new LightPoint { Radius = 4, Shadow = new LightPointShadowMap { Enabled = true } }, Intensity = 10 },
            });
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(2, () =>
            {
                var lit = DirectLightShaders(litSphere);
                var unlit = DirectLightShaders(unlitSphere);
                Assert.True(HasPointLightGroup(lit), "The sphere beside the point light is not compiled for it");
                Assert.False(HasPointLightGroup(unlit), "The sphere the other view draws is compiled for the point light that view does not see");
                Assert.NotEqual(lit, unlit);
            });
        }

        [SkippableFact]
        public void RunTestGame()
        {
            RunGameTest(new LightingSetsTest());
        }

        private Entity Sphere(string name, Vector3 position, Material material)
        {
            var sphere = new SphereProceduralModel { Radius = 1, MaterialInstance = { Material = material } };
            var entity = new Entity(name, position) { new ModelComponent(new ProceduralModelDescriptor(sphere).GenerateModel(Services)) };
            Scene.Entities.Add(entity);
            return entity;
        }

        private static ForwardRenderer ForwardRendererLike(ForwardRenderer other)
        {
            var renderer = new ForwardRenderer
            {
                Clear = { Color = other.Clear.Color },
                OpaqueRenderStage = other.OpaqueRenderStage,
                TransparentRenderStage = other.TransparentRenderStage,
            };
            foreach (var shadowMapRenderStage in other.ShadowMapRenderStages)
                renderer.ShadowMapRenderStages.Add(shadowMapRenderStage);
            return renderer;
        }

        /// <summary>
        /// The direct light groups the effect of <paramref name="entity"/> was validated with.
        /// </summary>
        private ShaderSourceCollection DirectLightShaders(Entity entity)
        {
            var renderModel = SceneSystem.SceneInstance.GetProcessor<ModelRenderProcessor>().RenderModels[entity.Get<ModelComponent>()];
            var renderEffects = meshRenderFeature.RenderData.GetData(meshRenderFeature.RenderEffectKey);
            var slotCount = meshRenderFeature.EffectPermutationSlotCount;
            foreach (var renderMesh in renderModel.Meshes)
            {
                for (int slot = 0; slot < slotCount; slot++)
                {
                    var renderEffect = renderEffects[renderMesh.StaticObjectNode * slotCount + slot];
                    if (renderEffect == null)
                        continue;

                    var validated = renderEffect.EffectValidator.EffectValues;
                    for (int i = 0; i < validated.Count; i++)
                    {
                        if (validated.Items[i].Key == LightingKeys.DirectLightGroups)
                            return (ShaderSourceCollection)validated.Items[i].Value;
                    }
                }
            }

            Assert.True(false, $"{entity.Name} has no effect validated with lights");
            return null;
        }

        private static bool HasPointLightGroup(ShaderSourceCollection directLightShaders)
        {
            return directLightShaders.Any(source => source is ShaderMixinSource mixin && mixin.Mixins.Any(part => part is ShaderClassSource { ClassName: "LightPointGroup" }));
        }
    }
}
