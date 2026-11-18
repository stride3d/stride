// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics.Regression;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Draws the same white cube in two views in one frame, through two camera renderers with different light masks.
    /// Each view is lit only by the lights that are in a layer of its light mask.
    /// </summary>
    public class TestLightMask : GameTestBase
    {
        private const int Size = 64;

        private enum Lights
        {
            /// <summary>Red lights (ambient and shadowed directional) in the first layer, blue ones in the second.</summary>
            RedAndBlueMasked,

            /// <summary>The same red and blue lights, left in every light layer.</summary>
            RedAndBlueDefault,

            /// <summary>A skybox light in the first layer.</summary>
            SkyboxMasked,
        }

        private readonly Lights lights;

        private Texture layer0View;
        private Texture layer1View;

        public TestLightMask() : this(Lights.RedAndBlueMasked)
        {
        }

        private TestLightMask(Lights lights)
        {
            this.lights = lights;
            GraphicsDeviceManager.PreferredBackBufferWidth = Size;
            GraphicsDeviceManager.PreferredBackBufferHeight = Size;
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_10_0];
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            layer0View = Texture.New2D(GraphicsDevice, Size, Size, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
            layer1View = Texture.New2D(GraphicsDevice, Size, Size, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            var camera = new CameraComponent();
            var compositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false, camera: camera, clearColor: Color.Black);
            var layer0Forward = (ForwardRenderer)compositor.SingleView;
            var layer1Forward = new ForwardRenderer
            {
                Clear = { Color = Color.Black },
                OpaqueRenderStage = layer0Forward.OpaqueRenderStage,
                TransparentRenderStage = layer0Forward.TransparentRenderStage,
            };
            foreach (var stage in layer0Forward.ShadowMapRenderStages)
                layer1Forward.ShadowMapRenderStages.Add(stage);

            var cameraSlot = compositor.Cameras[0];
            compositor.Game = new SceneRendererCollection
            {
                new SceneCameraRenderer { Camera = cameraSlot, LightMask = LightLayerMask.Layer0, Child = new RenderTextureSceneRenderer { RenderTexture = layer0View, Child = layer0Forward } },
                new SceneCameraRenderer { Camera = cameraSlot, LightMask = LightLayerMask.Layer1, Child = new RenderTextureSceneRenderer { RenderTexture = layer1View, Child = layer1Forward } },
            };

            var scene = new Scene();

            var cameraEntity = new Entity { camera };
            cameraEntity.Transform.Position = new Vector3(0, 0, 3);
            scene.Entities.Add(cameraEntity);

            var material = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature(),
                },
            });
            var model = new CubeProceduralModel().Generate(Services);
            model.Materials.Add(material);
            scene.Entities.Add(new Entity { new ModelComponent { Model = model } });

            switch (lights)
            {
                case Lights.RedAndBlueMasked:
                    AddLights(scene, Color.Red, LightLayerMask.Layer0);
                    AddLights(scene, Color.Blue, LightLayerMask.Layer1);
                    break;
                case Lights.RedAndBlueDefault:
                    AddLights(scene, Color.Red, LightLayerMask.All);
                    AddLights(scene, Color.Blue, LightLayerMask.All);
                    break;
                case Lights.SkyboxMasked:
                    var lightingScene = Content.Load<Scene>("LightingTests/LightingScene");
                    var skybox = lightingScene.Entities.First(x => x.Name == "Skybox");
                    lightingScene.Entities.Remove(skybox);
                    var skyboxLight = skybox.Get<LightComponent>();
                    skyboxLight.Enabled = true;
                    skyboxLight.LightLayers = LightLayerMask.Layer0;
                    scene.Entities.Add(skybox);
                    break;
            }

            SceneSystem.SceneInstance = new SceneInstance(Services, scene);
            SceneSystem.GraphicsCompositor = compositor;
        }

        private static void AddLights(Scene scene, Color color, LightLayerMask layers)
        {
            var ambient = new LightComponent { Type = new LightAmbient { Color = new ColorRgbProvider(color) }, Intensity = 0.25f, LightLayers = layers };

            // A directional light shines down -Z, from the camera onto the cube's front face
            var directionalType = new LightDirectional { Color = new ColorRgbProvider(color) };
            directionalType.Shadow.Enabled = true;
            var directional = new LightComponent { Type = directionalType, Intensity = 0.5f, LightLayers = layers };

            scene.Entities.Add(new Entity { ambient });
            scene.Entities.Add(new Entity { directional });
        }

        private Color CenterOf(Texture view)
        {
            var pixels = view.GetData<Color>(GraphicsContext.CommandList);
            return pixels[(Size / 2) * Size + Size / 2];
        }

        private static (Color Layer0, Color Layer1) RenderViews(Lights lights, [CallerMemberName] string testName = null)
        {
            var game = new TestLightMask(lights);
            Color layer0 = default, layer1 = default;
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                // Render several frames so the effects compile and the lights are drawn
                for (int i = 0; i < 10; i++)
                    await game.Script.NextFrame();

                layer0 = game.CenterOf(game.layer0View);
                layer1 = game.CenterOf(game.layer1View);
                game.Exit();
            });
            RunGameTest(game, testName);
            return (layer0, layer1);
        }

        [SkippableFact]
        public void EachViewIsLitByTheLightsOfItsLightMask()
        {
            var (layer0, layer1) = RenderViews(Lights.RedAndBlueMasked);

            Assert.True(layer0.R > 64 && layer0.G < 8 && layer0.B < 8, $"The Layer0 view should be lit red alone, got {layer0}");
            Assert.True(layer1.B > 64 && layer1.R < 8 && layer1.G < 8, $"The Layer1 view should be lit blue alone, got {layer1}");
        }

        [SkippableFact]
        public void LightsWithTheDefaultMaskLightEveryView()
        {
            var (layer0, layer1) = RenderViews(Lights.RedAndBlueDefault);

            Assert.True(layer0.R > 64 && layer0.B > 64 && layer0.G < 8, $"The Layer0 view should be lit red and blue, got {layer0}");
            Assert.True(layer1.R > 64 && layer1.B > 64 && layer1.G < 8, $"The Layer1 view should be lit red and blue, got {layer1}");
        }

        [SkippableFact]
        public void SkyboxLightsOnlyTheViewsOfItsLightMask()
        {
            var (layer0, layer1) = RenderViews(Lights.SkyboxMasked);

            Assert.True(layer0.R > 16 || layer0.G > 16 || layer0.B > 16, $"The Layer0 view should be lit by the skybox, got {layer0}");
            Assert.True(layer1.R < 8 && layer1.G < 8 && layer1.B < 8, $"The Layer1 view should not be lit, got {layer1}");
        }
    }
}
