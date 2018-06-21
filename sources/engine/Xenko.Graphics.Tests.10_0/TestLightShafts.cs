// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Engine.NextGen;
using Xenko.Games;
using Xenko.Graphics.Regression;
using Xenko.Rendering.Compositing;
using Xenko.Rendering.Images;

namespace Xenko.Graphics.Tests
{
    public class TestLightShafts : GraphicTestGameBase
    {
        public TestLightShafts()
        {
            // 2 = Fix projection issues
            // 3 = Simplifiy density parameters
            // 4 = Change random jitter position hash

            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_10_0 };
            GraphicsDeviceManager.SynchronizeWithVerticalRetrace = false;
        }

        protected override void PrepareContext()
        {
            base.PrepareContext();

            SceneSystem.InitialGraphicsCompositorUrl = "LightShaftsGraphicsCompositor";
            SceneSystem.InitialSceneUrl = "LightShafts";
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();
            
            Window.AllowUserResizing = true;

            var cameraEntity = SceneSystem.SceneInstance.First(x => x.Get<CameraComponent>() != null);
            cameraEntity.Add(new FpsTestCamera() {MoveSpeed = 10.0f });
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.TakeScreenshot(2);
        }

        public static void Main()
        {
            using (var game = new TestLightShafts())
                game.Run();
        }
        
        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunLightShafts()
        {
            RunGameTest(new TestLightShafts());
        }
    }
}
