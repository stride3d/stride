// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
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
    public class InSceneUITest : UITestGameBase
    {
        private readonly List<Entity> elements = new List<Entity>();

        public InSceneUITest()
        {
            CurrentVersion = 8;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // setup the camera
            var camera = new TestUICamera(Services.GetSafeServiceAs<SceneSystem>().GraphicsCompositor) { Yaw = 0, Pitch = 0, Position = new Vector3(0, 0, 1000), MoveSpeed = 100 };
            CameraComponent = camera.Camera;
            Script.Add(camera);

            UIRoot.Transform.Rotation = Quaternion.RotationX(MathUtil.Pi / 3f);
            UIComponent.Page = new UIPage { RootElement = new ImageElement { Source = (SpriteFromTexture)new Sprite(Content.Load<Texture>("uv")) } };
            UIComponent.IsFullScreen = false;
            UIComponent.IsBillboard = false;
            UIComponent.Resolution = new Vector3(200, 200, 100);
            UIRoot.Transform.Scale = new Vector3(200, 200, 100);

            var cube = new Entity { new ModelComponent { Model = Content.Load<Model>("cube Model") } };
            cube.Transform.Scale = new Vector3(10000);
            cube.Transform.Position = new Vector3(0, 0, 10);
            Scene.Entities.Add(cube);
            
            var font = Content.Load<SpriteFont>("CourierNew12");
            var textBlockZ0 = new TextBlock { Font = font, TextColor = Color.Black, TextSize = 20, Text = "At depth 0", VerticalAlignment = VerticalAlignment.Center, SynchronousCharacterGeneration = true, BackgroundColor = Color.Red };
            var entity1 = new Entity { new UIComponent { Page = new UIPage { RootElement = textBlockZ0 }, IsFullScreen = false, IsBillboard = false, Resolution = new Vector3(150) } };
            entity1.Transform.Scale = new Vector3(150);
            entity1.Transform.Position = new Vector3(-500, 0, 0);
            Scene.Entities.Add(entity1);

            var textBlockZ500 = new TextBlock { Font = font, TextColor = Color.Black, TextSize = 20, Text = "At depth 300", VerticalAlignment = VerticalAlignment.Center, SynchronousCharacterGeneration = true, BackgroundColor = Color.Red };
            var entity2 = new Entity { new UIComponent { Page = new UIPage { RootElement = textBlockZ500 }, IsFullScreen = false, IsBillboard = false, Resolution = new Vector3(150) } };
            entity2.Transform.Scale = new Vector3(150);
            entity2.Transform.Position = new Vector3(300, 0, 300);
            Scene.Entities.Add(entity2);

            var textBlockZM500 = new TextBlock { Font = font, TextColor = Color.Black, TextSize = 20, Text = "At depth -300", VerticalAlignment = VerticalAlignment.Center, SynchronousCharacterGeneration = true, BackgroundColor = Color.Red };
            var entity3 = new Entity { new UIComponent { Page = new UIPage { RootElement = textBlockZM500 }, IsFullScreen = false, IsBillboard = false, Resolution = new Vector3(150) } };
            entity3.Transform.Scale = new Vector3(150);
            entity3.Transform.Position = new Vector3(0, 300, -300);
            Scene.Entities.Add(entity3);

            elements.Add(entity1);
            elements.Add(entity2);
            elements.Add(entity3);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(ToggleSnapping).TakeScreenshot();
        }

        private void ToggleSnapping()
        {
            foreach (var element in elements)
            {
                var comp = element.Get<UIComponent>();
                if (comp != null)
                    comp.SnapText = !comp.SnapText;
            }
        }

        [Test]
        public void RunInSceneUITest()
        {
            RunGameTest(new InSceneUITest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new InSceneUITest())
                game.Run();
        }
    }
}
