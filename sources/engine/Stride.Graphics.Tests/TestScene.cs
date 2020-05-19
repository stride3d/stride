// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Rendering.Materials;
using Stride.Rendering.ProceduralModels;
using Stride.Games;

namespace Stride.Graphics.Tests
{
    public class TestScene : GraphicTestGameBase
    {
        private Entity cubeEntity;

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            Window.AllowUserResizing = true;

            // Instantiate a scene with a single entity and model component
            var scene = new Scene();

            // Create a cube entity
            cubeEntity = new Entity();

            // Create a procedural model with a diffuse material
            var model = new Model();
            var material = Material.New(GraphicsDevice, new MaterialDescriptor
            {
                Attributes =
                {
                    Diffuse = new MaterialDiffuseMapFeature(new ComputeColor(Color.White)),
                    DiffuseModel = new MaterialDiffuseLambertModelFeature()
                }
            });
            model.Materials.Add(material);
            cubeEntity.Add(new ModelComponent(model));

            var modelDescriptor = new ProceduralModelDescriptor(new CubeProceduralModel());
            modelDescriptor.GenerateModel(Services, model);

            // Add the cube to the scene
            scene.Entities.Add(cubeEntity);

            // Use this graphics compositor
            SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(false, graphicsProfile: GraphicsProfile.Level_9_1);

            // Create a camera entity and add it to the scene
            var cameraEntity = new Entity { new CameraComponent { Slot = Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor.Cameras[0].ToSlotId() } };
            cameraEntity.Transform.Position = new Vector3(0, 0, 5);
            scene.Entities.Add(cameraEntity);

            // Create a light
            var lightEntity = new Entity()
            {
                new LightComponent()
            };

            lightEntity.Transform.Position = new Vector3(0, 0, 1);
            lightEntity.Transform.Rotation = Quaternion.RotationY(MathUtil.DegreesToRadians(45));
            scene.Entities.Add(lightEntity);

            // Create a scene instance
            SceneSystem.SceneInstance = new SceneInstance(Services, scene);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var time = (float)gameTime.Total.TotalSeconds;
            cubeEntity.Transform.Rotation = Quaternion.RotationY(time) * Quaternion.RotationX(time * 0.5f);

            //if (!ScreenShotAutomationEnabled)
            //    DrawCustomEffect();
        }

        //private void DrawCustomEffect()
        //{
        //    GraphicsDevice.Clear(GraphicsDevice.BackBuffer, Color.Black);
        //    GraphicsDevice.Clear(GraphicsDevice.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
        //    GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);

        //    effectParameters.Set(MyCustomShaderKeys.ColorFactor2, (Vector4)Color.Red);
        //    effectParameters.Set(CustomShaderKeys.SwitchEffectLevel, switchEffectLevel);
        //    effectParameters.Set(TexturingKeys.Texture0, UVTexture);
        //    // TODO: Add switch Effect to test and capture frames
        //    //switchEffectLevel++;
        //    dynamicEffectCompiler.Update(effectInstance, null);

        //    GraphicsDevice.DrawQuad(effectInstance.Effect, effectParameters);
        //}

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunSceneTests()
        {
            RunGameTest(new TestScene());
        }
    }
}
