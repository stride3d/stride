// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Xunit;

using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Rendering.Lights;
using Xenko.Rendering;
using Xenko.Rendering.ProceduralModels;
using Xenko.Rendering.Tessellation;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Graphics.Regression;
using Xenko.Input;
using Xenko.Rendering.Compositing;

namespace Xenko.Engine.Tests
{
    public class TesselationTest : EngineTestBase
    {
        private List<Entity> entities = new List<Entity>();
        private List<Material> materials = new List<Material>();

        private Entity currentEntity;
        private Material currentMaterial;

        private int currentModelIndex;

        private TestCamera camera;

        private int currentMaterialIndex;

        private RasterizerStateDescription wireframeState;

        private SpriteBatch spriteBatch;

        private SpriteFont font;

        private bool debug;

        public TesselationTest() : this(false)
        {
        }

        protected TesselationTest(bool isDebug)
        {
            debug = isDebug;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
            GraphicsDeviceManager.ShaderProfile = GraphicsProfile.Level_11_0;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("Font");

            wireframeState = new RasterizerStateDescription(CullMode.Back) { FillMode = FillMode.Wireframe };

            materials.Add(Content.Load<Material>("NoTessellation"));
            materials.Add(Content.Load<Material>("FlatTessellation"));
            materials.Add(Content.Load<Material>("PNTessellation"));
            materials.Add(Content.Load<Material>("PNTessellationAE"));
            materials.Add(Content.Load<Material>("FlatTessellationDispl"));
            materials.Add(Content.Load<Material>("FlatTessellationDisplAE"));
            materials.Add(Content.Load<Material>("PNTessellationDisplAE"));

            RenderContext.GetShared(Services).RendererInitialized += RendererInitialized;

            var cube = new Entity("Cube") { new ModelComponent(new ProceduralModelDescriptor(new CubeProceduralModel { Size = new Vector3(80), MaterialInstance = { Material = materials[0] } }).GenerateModel(Services)) };
            var sphere = new Entity("Sphere") { new ModelComponent(new ProceduralModelDescriptor(new SphereProceduralModel { Radius = 50, Tessellation = 5, MaterialInstance = { Material = materials[0] }} ).GenerateModel(Services)) };

            var megalodon = new Entity { new ModelComponent { Model = Content.Load<Model>("megalodon Model") } };
            megalodon.Transform.Position= new Vector3(0, -30f, -10f);

            var knight = new Entity { new ModelComponent { Model = Content.Load<Model>("knight Model") } };
            knight.Transform.RotationEulerXYZ = new Vector3(-MathUtil.Pi / 2, MathUtil.Pi / 4, 0);
            knight.Transform.Position = new Vector3(0, -50f, 20f);
            knight.Transform.Scale= new Vector3(0.6f);

            entities.Add(sphere);
            entities.Add(cube);
            entities.Add(megalodon);
            entities.Add(knight);

            camera = new TestCamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor);
            CameraComponent = camera.Camera;
            Script.Add(camera);

            // TODO GRAPHICS REFACTOR
            ChangeModel(0);

            camera.Position = new Vector3(25, 45, 80);
            camera.SetTarget(currentEntity, true);
        }

        void RendererInitialized(IGraphicsRendererCore obj)
        {
            // TODO: callback will be called also for editor renderers. We might want to filter down this
            (obj as MeshRenderFeature)?.PipelineProcessors.Add(new WireframeCullbackPipelineProcessor());
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).Draw(() => ChangeModel(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).Draw(() => ChangeModel(-1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!debug)
                return;

            spriteBatch.Begin(GraphicsContext);
            spriteBatch.DrawString(font, "Desired triangle size: {0}".ToFormat(currentMaterial.Passes[0].Parameters.Get(TessellationKeys.DesiredTriangleSize)), new Vector2(0), Color.Black);
            spriteBatch.DrawString(font, "FPS: {0}".ToFormat(DrawTime.FramePerSecond), new Vector2(0, 20), Color.Black);
            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.Up))
                ChangeModel(1);

            if (Input.IsKeyPressed(Keys.Down))
                ChangeModel(-1);

            if (Input.IsKeyPressed(Keys.Left))
                ChangeMaterial(-1);

            if (Input.IsKeyPressed(Keys.Right))
                ChangeMaterial(1);

            if (Input.IsKeyDown(Keys.NumPad1))
                ChangeDesiredTriangleSize(-0.2f);

            if (Input.IsKeyDown(Keys.NumPad2))
                ChangeDesiredTriangleSize(0.2f);
        }

        private void ChangeDesiredTriangleSize(float f)
        {
            if (currentMaterial == null)
                return;

            var oldValue = currentMaterial.Passes[0].Parameters.Get(TessellationKeys.DesiredTriangleSize);
            currentMaterial.Passes[0].Parameters.Set(TessellationKeys.DesiredTriangleSize, oldValue + f);
        }

        private void ChangeModel(int offset)
        {
            if (currentEntity != null)
            {
                Scene.Entities.Remove(currentEntity);
                currentEntity = null;
            }

            currentModelIndex = (currentModelIndex + offset + entities.Count) % entities.Count;
            currentEntity = entities[currentModelIndex];

            Scene.Entities.Add(currentEntity);

            ChangeMaterial(0);
        }

        private void ChangeMaterial(int i)
        {
            currentMaterialIndex = ((currentMaterialIndex + i + materials.Count) % materials.Count);
            currentMaterial = materials[currentMaterialIndex];

            if (currentEntity != null)
            {
                var modelComponent = currentEntity.Get<ModelComponent>();
                modelComponent.Materials.Clear();

                if (modelComponent.Model != null)
                {
                    // ensure the same number of materials than original model.
                    for (int j = 0; j < modelComponent.Model.Materials.Count; j++)
                        modelComponent.Materials[j] = currentMaterial;
                }
            }
        }

        [SkippableFact]
        public void RunTestGame()
        {
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGL);
            IgnoreGraphicPlatform(GraphicsPlatform.OpenGLES);
            IgnoreGraphicPlatform(GraphicsPlatform.Vulkan);

            RunGameTest(new TesselationTest());
        }

        internal static void Main()
        {
            using (var game = new TesselationTest(true))
            {
                game.Run();
            }
        }

        private class WireframeCullbackPipelineProcessor : PipelineProcessor
        {
            public override void Process(RenderNodeReference renderNodeReference, ref RenderNode renderNode, RenderObject renderObject, PipelineStateDescription pipelineState)
            {
                pipelineState.RasterizerState = RasterizerStates.WireframeCullBack;
            }
        }
    }
}
