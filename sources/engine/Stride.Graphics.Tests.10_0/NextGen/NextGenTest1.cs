// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Games;
using Stride.Graphics;
using Stride.Graphics.Regression;
using Stride.Graphics.Tests;
using Stride.Rendering;
using Stride.Rendering.Colors;
using Stride.Rendering.Compositing;
using Stride.Rendering.Lights;
using Stride.Rendering.Skyboxes;
using Stride.Rendering.Sprites;

namespace Stride.Engine.NextGen
{
    public class NextGenTest1 : GraphicTestGameBase
    {
        private TestCamera camera;
        protected Scene Scene;
        protected Entity Camera = new Entity();

        private Model model;
        private Material material1, material2;

        protected CameraComponent CameraComponent
        {
            get { return Camera.Get<CameraComponent>(); }
            set
            {
                Camera.Add(value);
                value.Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId();
            }
        }

        public NextGenTest1()
        {
            //GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
            //Profiler.EnableAll();
        }

        protected override Task LoadContent()
        {
            //Profiler.Enable(GameProfilingKeys.GameDrawFPS);
            ProfilingSystem.EnableProfiling(false, GameProfilingKeys.GameDrawFPS, CompositingProfilingKeys.Opaque);

            model = Content.Load<Model>("Model");
            material1 = Content.Load<Material>("Material1");
            material2 = Content.Load<Material>("Material2");

            SetupScene();

            int cubeWidth = 8;

            //var skybox = Content.Load<Skybox>("Skybox");
            //var skyboxEntity = new Entity { new SkyboxComponent { Skybox = skybox } };
            //Scene.Entities.Add(skyboxEntity);

            var backgroundTexture = Content.Load<Texture>("StrideBackground");
            var backgroundEntity = new Entity { new BackgroundComponent { Texture = backgroundTexture } };
            Scene.Entities.Add(backgroundEntity);

            for (int i = 0; i < cubeWidth; ++i)
            {
                for (int j = 0; j < cubeWidth; ++j)
                {
                    for (int k = 0; k < cubeWidth; ++k)
                    {
                        var position = new Vector3((i - cubeWidth / 2) * 2.4f, (j - cubeWidth / 2) * 2.4f, (k - cubeWidth / 2) * 2.4f);
                        //var material = (k/4)%2 == 0 ? material1 : material2;
                        //var isShadowReceiver = (k / 2) % 2 == 0;

                        var entity = new Entity
                        {
                            new ModelComponent { Model = model, Materials = { { 0, material1 } } },
                        };
                        entity.Transform.Position = position;
                        Scene.Entities.Add(entity);
                    }
                }
            }

            //var spriteSheet = Content.Load<SpriteSheet>("SpriteSheet");
            //var spriteEntity = new Entity
            //{
            //    new SpriteComponent
            //    {
            //        SpriteProvider = new SpriteFromSheet { Sheet = spriteSheet },
            //    }
            //};
            //Scene.Entities.Add(spriteEntity);

            camera.Position = new Vector3(35.0f, 5.5f, 22.0f);
            camera.SetTarget(Scene.Entities.Last(), true);

            return base.LoadContent();
        }

        private void SetupScene()
        {
            // Load default graphics compositor
            SceneSystem.GraphicsCompositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");

            Scene = new Scene();
            Scene.Entities.Add(Camera);

            //var ambientLight = new Entity { new LightComponent { Type = new LightAmbient { Color = new ColorRgbProvider(Color.White) }, Intensity = 1 } };
            ////var ambientLight = new Entity { new LightComponent { Type = new LightDirectional { Color = new ColorRgbProvider(Color.White) }, Intensity = 1 } };
            ////ambientLight.Transform.RotationEulerXYZ = new Vector3(0.0f, (float) Math.PI, 0.0f);
            //Scene.Entities.Add(ambientLight);

            var directionalLight = new Entity { new LightComponent { Type = new LightDirectional { Color = new ColorRgbProvider(Color.White), Shadow = { Enabled = true } }, Intensity = 1 }, };
            directionalLight.Transform.Rotation = Quaternion.RotationY(MathUtil.PiOverTwo * 0.2f);
            Scene.Entities.Add(directionalLight);

            //var pointLight1 = new Entity { new LightComponent() { Type = new LightPoint { Color = new ColorRgbProvider(Color.Red), Radius = 100.0f, Shadow = { Enabled = false } }, Intensity = 1.0f } };
            //pointLight1.Transform.Position.X = 5.0f;
            //Scene.Entities.Add(pointLight1);

            //var directionalLight2 = new Entity { new LightComponent { Type = new LightSpot { Range = 25, Color = new ColorRgbProvider(Color.White), Shadow = { Enabled = false } }, Intensity = 20 }, };
            //directionalLight2.Transform.Position = Vector3.UnitX * 40;
            //directionalLight2.Transform.Rotation = Quaternion.RotationY(MathUtil.PiOverTwo);
            //Scene.Entities.Add(directionalLight2);

            var rand = new Random();
            for (int i = 0; i < 512; ++i)
            {
                var pointLight1 = new Entity
                {
                    new LightComponent()
                    {
                        Type = i % 2 == 0 ? (ILight)new LightPoint
                        {
                            Color = new ColorRgbProvider(new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble(), 1.0f)),
                            Radius = 2.0f,
                            Shadow = { Enabled = false },
                        }
                        : new LightSpot
                        {
                            Color = new ColorRgbProvider(new Color((float)rand.NextDouble(), (float)rand.NextDouble(), (float)rand.NextDouble(), 1.0f)),
                            Range = 4.0f,
                            Shadow = { Enabled = false },
                        },
                        Intensity = 10.0f
                    }
                };
                pointLight1.Transform.Position = new Vector3(
                    (float)rand.NextDouble() * 20.0f - 10.0f,
                    (float)rand.NextDouble() * 20.0f - 10.0f,
                    (float)rand.NextDouble() * 20.0f - 10.0f
                    );
                Scene.Entities.Add(pointLight1);
            }


            SceneSystem.SceneInstance = new SceneInstance(Services, Scene);

            // Load default graphics compositor
            SceneSystem.GraphicsCompositor = Content.Load<GraphicsCompositor>("GraphicsCompositor");

            camera = new TestCamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor);
            CameraComponent = camera.Camera;
            Script.Add(camera);
        }

        static void Main(string[] args)
        {
            // Profiler.EnableAll();
            using (var game = new NextGenTest1())
            {
                game.Run();
            }
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunNextGenTest()
        {
            RunGameTest(new NextGenTest1());
        }
    }
}
