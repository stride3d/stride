// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Effects;
using Stride.Effects.Cubemap;
using Stride.Effects.Renderers;
using Stride.Engine;
using Stride.EntityModel;
using Stride.Extensions;

namespace Stride.Graphics.Tests
{
    public class TestCubemapDeferred : TestGameBase
    {
        private LightingIBLRenderer IBLRenderer;

        private Entity teapotEntity;

        private Entity dynamicCubemapEntity;

        public TestCubemapDeferred()
        {
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // create pipeline
            CreatePipeline();

            // setup the scene
            var material = Asset.Load<Material>("BasicMaterial");
            teapotEntity = new Entity()
            {
                new ModelComponent()
                {
                    Model = new Model()
                    {
                        material,
                        new Mesh()
                        {
                            Draw = GeometricPrimitive.Teapot.New(GraphicsDevice).ToMeshDraw(),
                            MaterialIndex = 0,
                        }
                    }
                }
            };
            Entities.Add(teapotEntity);

            var textureCube = Asset.Load<Texture>("uv_cube");
            var staticCubemapEntity = new Entity()
            {
                new CubemapSourceComponent(textureCube) { InfluenceRadius = 2f, IsDynamic = false },
                new TransformationComponent() { Translation = Vector3.UnitZ }
            };
            Entities.Add(staticCubemapEntity);

            dynamicCubemapEntity = new Entity()
            {
                new CubemapSourceComponent(textureCube) { InfluenceRadius = 0.5f, IsDynamic = false },
                new TransformationComponent() { Translation = Vector3.Zero }
            };
            Entities.Add(dynamicCubemapEntity);

            var mainCamera = new Entity()
            {
                new CameraComponent
                {
                    AspectRatio = 8/4.8f,
                    FarPlane = 20,
                    NearPlane = 1,
                    VerticalFieldOfView = 0.6f,
                    Target = teapotEntity,
                    TargetUp = Vector3.UnitY,
                },
                new TransformationComponent
                {
                    Translation = new Vector3(4, 3, 0)
                }
            };
            Entities.Add(mainCamera);

            RenderSystem.Pipeline.SetCamera(mainCamera.Get<CameraComponent>());

            Script.Add(GameScript1);
        }

        private void CreatePipeline()
        {
            // Processor
            Entities.Processors.Add(new CubemapSourceProcessor(GraphicsDevice));

            // Rendering pipeline
            RenderSystem.Pipeline.Renderers.Add(new CameraSetter(Services));

            RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services)
            {
                ClearColor = Color.CornflowerBlue,
                EnableClearDepth = true,
                ClearDepth = 1f
            });

            // Create G-buffer pass
            var gbufferPipeline = new RenderPipeline("GBuffer");
            // Renders the G-buffer for opaque geometry.
            gbufferPipeline.Renderers.Add(new ModelRenderer(Services, "CubemapIBLEffect.StrideGBufferShaderPass"));
            var gbufferProcessor = new GBufferRenderProcessor(Services, gbufferPipeline, GraphicsDevice.DepthStencilBuffer, false);

            // Add sthe G-buffer pass to the pipeline.
            RenderSystem.Pipeline.Renderers.Add(gbufferProcessor);

            var readOnlyDepthBuffer = GraphicsDevice.DepthStencilBuffer; // TODO ToDepthStencilBuffer(true);
            IBLRenderer = new LightingIBLRenderer(Services, "CubemapIBLSpecular", readOnlyDepthBuffer);
            RenderSystem.Pipeline.Renderers.Add(IBLRenderer);
            RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services)
            {
                ClearColor = Color.CornflowerBlue,
                EnableClearDepth = false,
            });
            RenderSystem.Pipeline.Renderers.Add(new DelegateRenderer(Services) { Render = ShowIBL });
        }

        private void ShowIBL(RenderContext context)
        {
            GraphicsDevice.DrawTexture(IBLRenderer.IBLTexture);
        }

        private async Task GameScript1()
        {
            while (IsRunning)
            {
                // Wait next rendering frame
                await Script.NextFrame();

                teapotEntity.Transform.Rotation = Quaternion.RotationY((float)(2 * Math.PI * UpdateTime.Total.TotalMilliseconds / 5000.0f));
                dynamicCubemapEntity.Transform.Translation = new Vector3(2f * (float)Math.Sin(2 * Math.PI * UpdateTime.Total.TotalMilliseconds / 15000.0f), 0, 0);
            }
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunCubemapRendering()
        {
            RunGameTest(new TestCubemapDeferred());
        }
    }
}
