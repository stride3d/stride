// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Stride.Shaders;

namespace Stride.Engine.Tests
{
    /// <summary>
    /// An effect is compiled for the lights of the views that draw its object. Two views far apart each look at a
    /// white sphere of their own, and both see a third one far away. The first sphere stands by a red shadowed point
    /// light and a green shadowed spot light, the second by a blue and a green shadowed spot light, so the spot light
    /// group is in the three lighting sets, the first view's, the second's and that of both, at two indices and with
    /// two light counts. Each sphere's effect takes the groups of its views alone, and each view draws its sphere in
    /// the colors of its lights. Then the lights go and the ambient one comes: the views are lit alike, and the three
    /// spheres are compiled for the same shaders.
    /// The spheres enter at the second frame: the first draws the lights alone, and a group laid out for no set in a
    /// frame keeps its shader for the next.
    /// </summary>
    public class LightingSetsTest : EngineTestBase
    {
        private const int ViewSize = 64;

        private Texture firstView;
        private Texture secondView;
        private Entity firstSphere;
        private Entity secondSphere;
        private Entity farSphere;
        private Entity[] lights;
        private MeshRenderFeature meshRenderFeature;

        public LightingSetsTest()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            firstView = Texture.New2D(GraphicsDevice, ViewSize, ViewSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
            secondView = Texture.New2D(GraphicsDevice, ViewSize, ViewSize, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            // Two cameras, each drawing the scene to a texture through a forward renderer of its own
            var compositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false, clearColor: Color.Black, graphicsProfile: GraphicsProfile.Level_11_0);
            var firstForward = (ForwardRenderer)((SceneCameraRenderer)compositor.Game).Child;
            var secondSlot = new SceneCameraSlot { Name = "Second" };
            compositor.Cameras.Add(secondSlot);
            compositor.Game = new SceneRendererCollection
            {
                Children =
                {
                    new SceneCameraRenderer { Camera = compositor.Cameras[0], Child = new RenderTextureSceneRenderer { RenderTexture = firstView, Child = firstForward } },
                    new SceneCameraRenderer { Camera = secondSlot, Child = new RenderTextureSceneRenderer { RenderTexture = secondView, Child = ForwardRendererLike(firstForward) } },
                },
            };
            SceneSystem.GraphicsCompositor = compositor;
            meshRenderFeature = compositor.RenderFeatures.OfType<MeshRenderFeature>().Single();

            CameraComponent.Slot = compositor.Cameras[0].ToSlotId();
            Scene.Entities.Add(new Entity("Second camera", new Vector3(100, 0, 0)) { new CameraComponent { Slot = secondSlot.ToSlotId() } });
            AmbientLight.Intensity = 0;

            var material = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
            firstSphere = Sphere("First sphere", new Vector3(0, 0, -5), 1, material);
            secondSphere = Sphere("Second sphere", new Vector3(100, 0, -5), 1, material);
            farSphere = Sphere("Far sphere", new Vector3(50, 0, -200), 20, material);

            // The lights have a bounding box, so only the frustum of the camera they stand before holds them, and
            // they shine from the camera onto the middle of the sphere it looks at
            lights = new[]
            {
                new Entity("Red point light", new Vector3(0, 0.5f, -2.5f))
                {
                    new LightComponent { Type = new LightPoint { Color = new ColorRgbProvider(Color.Red), Radius = 4, Shadow = { Enabled = true } }, Intensity = 10 },
                },
                SpotLight("Green spot light", new Vector3(0, 0, -2), Color.Green),
                SpotLight("Blue spot light", new Vector3(100, 0, -2), Color.Blue),
                SpotLight("Second green spot light", new Vector3(100, 0.5f, -2.5f), Color.Green),
            };
            foreach (var light in lights)
                Scene.Entities.Add(light);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Update(1, () =>
            {
                Scene.Entities.Add(firstSphere);
                Scene.Entities.Add(secondSphere);
                Scene.Entities.Add(farSphere);
            });
            FrameGameSystem.Draw(4, () =>
            {
                // Each effect takes the groups of the views that draw its object, sized by the lights of those views
                var first = DirectLightShaders(firstSphere);
                var second = DirectLightShaders(secondSphere);
                var far = DirectLightShaders(farSphere);
                Assert.Equal((1, 1), (LightCount(first, "LightPointGroup"), LightCount(first, "LightSpotGroup")));
                Assert.Equal((0, 2), (LightCount(second, "LightPointGroup"), LightCount(second, "LightSpotGroup")));
                Assert.Equal((1, 2), (LightCount(far, "LightPointGroup"), LightCount(far, "LightSpotGroup")));

                // And each view lights its sphere with its own lights, the group they share laid out for each set
                var firstColor = CenterOf(firstView);
                var secondColor = CenterOf(secondView);
                Assert.True(firstColor.R > 64 && firstColor.G > 64 && firstColor.B < 8, $"The first view should be lit red and green alone, got {firstColor}");
                Assert.True(secondColor.G > 64 && secondColor.B > 64 && secondColor.R < 8, $"The second view should be lit green and blue alone, got {secondColor}");
            });
            FrameGameSystem.Update(5, () =>
            {
                foreach (var light in lights)
                    Scene.Entities.Remove(light);
                AmbientLight.Intensity = 1;
            });
            FrameGameSystem.Draw(8, () =>
            {
                // Views lit alike share a lighting set: the spheres are compiled for the same shaders
                var first = DirectLightShaders(firstSphere);
                Assert.Equal((0, 0), (LightCount(first, "LightPointGroup"), LightCount(first, "LightSpotGroup")));
                Assert.Same(first, DirectLightShaders(secondSphere));
                Assert.Same(first, DirectLightShaders(farSphere));

                var firstColor = CenterOf(firstView);
                var secondColor = CenterOf(secondView);
                Assert.True(firstColor.R > 64 && firstColor.G > 64 && firstColor.B > 64, $"The first view should be lit white, got {firstColor}");
                Assert.Equal(firstColor, secondColor);
            });
        }

        [SkippableFact]
        public void RunTestGame()
        {
            RunGameTest(new LightingSetsTest());
        }

        private Entity Sphere(string name, Vector3 position, float radius, Material material)
        {
            var sphere = new SphereProceduralModel { Radius = radius, MaterialInstance = { Material = material } };
            return new Entity(name, position) { new ModelComponent(new ProceduralModelDescriptor(sphere).GenerateModel(Services)) };
        }

        private static Entity SpotLight(string name, Vector3 position, Color color)
        {
            return new Entity(name, position)
            {
                new LightComponent
                {
                    Type = new LightSpot { Color = new ColorRgbProvider(color), Range = 6, AngleInner = 60, AngleOuter = 70, Shadow = { Enabled = true } },
                    Intensity = 10,
                },
            };
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

        private Color CenterOf(Texture view)
        {
            var pixels = view.GetData<Color>(GraphicsContext.CommandList);
            return pixels[(ViewSize / 2) * ViewSize + ViewSize / 2];
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

            Assert.Fail($"{entity.Name} has no effect validated with lights");
            return null;
        }

        /// <summary>
        /// The number of lights the group of shader <paramref name="className"/> holds in
        /// <paramref name="directLightShaders"/>, zero when the group is not among them.
        /// </summary>
        private static int LightCount(ShaderSourceCollection directLightShaders, string className)
        {
            var group = directLightShaders.OfType<ShaderMixinSource>().SelectMany(mixin => mixin.Mixins).FirstOrDefault(part => part.ClassName == className);
            return group != null ? int.Parse(group.GenericArguments[0]) : 0;
        }
    }
}
