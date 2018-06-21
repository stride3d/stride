// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;
using Xenko.Rendering;
using Xenko.Rendering.Sprites;
using Xenko.UI.Controls;

namespace Xenko.UI.Tests.Regression
{
    /// <summary>
    /// Test for UI on scene entities
    /// </summary>
    public class BillboardModeTests : UITestGameBase
    {
        public BillboardModeTests()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var cube = new Entity { new ModelComponent { Model = Content.Load<Model>("cube Model") } };
            cube.Transform.Scale = new Vector3(10000);
            cube.Transform.Position = new Vector3(0, 0, 10);
            Scene.Entities.Add(cube);

            var imageElement = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) };
            var imageEntity = new Entity { new UIComponent { Page = new UIPage { RootElement = imageElement }, IsFullScreen = false, Resolution = new Vector3(150) } };
            imageEntity.Transform.Scale = new Vector3(150);
            imageEntity.Transform.Position = new Vector3(-500, 0, 0);
            Scene.Entities.Add(imageEntity);

            var imageEntity2 = new Entity { new UIComponent { Page = new UIPage { RootElement = imageElement }, IsFullScreen = false, Resolution = new Vector3(200) } };
            imageEntity2.Transform.Position = new Vector3(0, 250, 0);
            imageEntity2.Transform.Scale = new Vector3(200);
            Scene.Entities.Add(imageEntity2);

            var imageEntity3 = new Entity { new UIComponent { Page = new UIPage { RootElement = imageElement }, IsFullScreen = false, Resolution = new Vector3(250) } };
            imageEntity3.Transform.Position = new Vector3(0, 0, -500);
            imageEntity3.Transform.Scale = new Vector3(250);
            Scene.Entities.Add(imageEntity3);

            // setup the camera
            var camera = new TestUICamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor) { Yaw = MathUtil.Pi/4, Pitch = MathUtil.Pi/4, Position = new Vector3(500, 500, 500), MoveSpeed = 100 };
            camera.SetTarget(cube, true);
            CameraComponent = camera.Camera;
            Script.Add(camera);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        [Test]
        public void RunBillboardModeTests()
        {
            RunGameTest(new BillboardModeTests());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new BillboardModeTests())
                game.Run();
        }
    }
}
