// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xunit;
using Xenko.Core.IO;
using Xenko.Core.Mathematics;
using Xenko.Core.Serialization.Contents;
using Xenko.Engine;
using Xenko.Graphics.Regression;
using Xenko.Rendering;
using Xenko.Rendering.Compositing;

namespace Xenko.Graphics.Tests
{
    public class FixedAspectRatioTests : GameTestBase
    {
        protected Scene Scene;

        public FixedAspectRatioTests()
        {
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // Force aspect ratio
            SceneSystem.GraphicsCompositor = GraphicsCompositorHelper.CreateDefault(false, clearColor: Color.Green, graphicsProfile: GraphicsProfile.Level_9_1);
            SceneSystem.GraphicsCompositor.Game = new ForceAspectRatioSceneRenderer { Child = SceneSystem.GraphicsCompositor.Game, FixedAspectRatio = 3.0f, ForceAspectRatio = true };

            Scene = new Scene();

            Texture png;
            using (var pngStream = Content.FileProvider.OpenStream("PngImage", VirtualFileMode.Open, VirtualFileAccess.Read))
            using (var pngImage = Image.Load(pngStream, GraphicsDevice.ColorSpace == ColorSpace.Linear))
                png = Texture.New(GraphicsDevice, pngImage);

            var camera = new Entity { new CameraComponent { Slot = SceneSystem.GraphicsCompositor.Cameras[0].ToSlotId() } };
            var plane = new Entity { new BackgroundComponent { Texture = png } };
            Scene.Entities.Add(plane);
            Scene.Entities.Add(camera);

            SceneSystem.SceneInstance = new SceneInstance(Services, Scene);
        }

        [Fact]
        public void TestFixedRatio()
        {
            RunGameTest(new FixedAspectRatioTests());
        }

        public static void Main(string[] args)
        {
            using (Game game = new FixedAspectRatioTests())
            {
                game.Run();
            }
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            // Take screenshot first frame
            FrameGameSystem.TakeScreenshot();
        }
    }
}
