// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Rendering.ProceduralModels;
using Xunit;

namespace Stride.Graphics.Tests
{
    /// <summary>
    /// Renders a single shared render stage into two render targets with different pixel formats
    /// (RGBA and BGRA), as happens when a stage is shared between a render-texture and the backbuffer
    /// (e.g. the AnimatedModel sample on Vulkan, where the surface only offers BGRA). The pipeline
    /// state is cached per output format on <see cref="RenderEffect"/>; without that, the second
    /// target reuses the first's pipeline state and trips a Vulkan framebuffer/renderpass format
    /// mismatch. <see cref="GameTestBase"/> fails the test on any unexpected GPU validation error.
    /// </summary>
    public class TestSharedStageMultipleOutputs : Graphics.Regression.GameTestBase
    {
        private Texture renderTargetRgba;
        private Texture renderTargetBgra;

        public TestSharedStageMultipleOutputs()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = 256;
            GraphicsDeviceManager.PreferredBackBufferHeight = 256;
            GraphicsDeviceManager.PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = [GraphicsProfile.Level_11_0];
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            renderTargetRgba = Texture.New2D(GraphicsDevice, 256, 256, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
            renderTargetBgra = Texture.New2D(GraphicsDevice, 256, 256, PixelFormat.B8G8R8A8_UNorm_SRgb, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);

            var camera = new CameraComponent();
            var compositor = GraphicsCompositorHelper.CreateDefault(enablePostEffects: false, camera: camera, graphicsProfile: GraphicsProfile.Level_9_1);
            var forwardRgba = (ForwardRenderer)compositor.SingleView;

            // A second renderer sharing the same render stages, so the same object is drawn into both
            // targets through one stage (and thus one cached pipeline state per (object, stage)).
            var forwardBgra = new ForwardRenderer
            {
                Clear = { Color = Color.Black },
                OpaqueRenderStage = forwardRgba.OpaqueRenderStage,
                TransparentRenderStage = forwardRgba.TransparentRenderStage,
            };

            var cameraSlot = compositor.Cameras[0];
            compositor.Game = new SceneRendererCollection
            {
                new SceneCameraRenderer { Camera = cameraSlot, Child = new RenderTextureSceneRenderer { RenderTexture = renderTargetRgba, Child = forwardRgba } },
                new SceneCameraRenderer { Camera = cameraSlot, Child = new RenderTextureSceneRenderer { RenderTexture = renderTargetBgra, Child = forwardBgra } },
            };

            var scene = new Scene();

            var cameraEntity = new Entity { camera };
            cameraEntity.Transform.Position = new Vector3(0, 0, 3);
            scene.Entities.Add(cameraEntity);

            // Unlit emissive material: the cube only needs to draw (so the pipeline states and
            // framebuffers are created for both outputs); lighting is irrelevant to the repro.
            var model = new CubeProceduralModel().Generate(Services);
            model.Materials.Add(Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Emissive = new MaterialEmissiveMapFeature(new ComputeColor(Color.White)),
                },
            }));
            scene.Entities.Add(new Entity { new ModelComponent { Model = model } });

            SceneSystem.SceneInstance = new SceneInstance(Services, scene);
            SceneSystem.GraphicsCompositor = compositor;
        }

        [SkippableFact]
        public void RunSharedStageMultipleOutputs()
        {
            var game = new TestSharedStageMultipleOutputs();
            game.Script.AddTask(async () =>
            {
                game.ScreenShotAutomationEnabled = false;

                // Render several frames so the shared effect compiles and is drawn into both targets.
                for (int i = 0; i < 10; i++)
                    await game.Script.NextFrame();

                game.Exit();
            });
            RunGameTest(game);
        }
    }
}
