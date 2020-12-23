// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;

using Xunit;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Sprites;
using Stride.UI.Controls;

namespace Stride.UI.Tests.Regression
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
            var imageEntity = new Entity { new UIComponent { Page = new UIPage { RootElement = imageElement }, IsFullScreen = false, Resolution = new Vector3(150), Size = new Vector3(1.0f) } };
            imageEntity.Transform.Scale = new Vector3(150);
            imageEntity.Transform.Position = new Vector3(-500, 0, 0);
            Scene.Entities.Add(imageEntity);

            var imageEntity2 = new Entity { new UIComponent { Page = new UIPage { RootElement = imageElement }, IsFullScreen = false, Resolution = new Vector3(200), Size = new Vector3(1.0f) } };
            imageEntity2.Transform.Position = new Vector3(0, 250, 0);
            imageEntity2.Transform.Scale = new Vector3(200);
            Scene.Entities.Add(imageEntity2);

            var imageEntity3 = new Entity { new UIComponent { Page = new UIPage { RootElement = imageElement }, IsFullScreen = false, Resolution = new Vector3(250), Size = new Vector3(1.0f) } };
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

        [Fact]
        public void RunBillboardModeTests()
        {
            RunGameTest(new BillboardModeTests());
        }
    }
}
