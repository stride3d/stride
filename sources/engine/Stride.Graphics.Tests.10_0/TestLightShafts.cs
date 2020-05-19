// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.NextGen;
using Stride.Games;
using Stride.Graphics.Regression;
using Stride.Rendering.Compositing;
using Stride.Rendering.Images;

namespace Stride.Graphics.Tests
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
        
        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunLightShafts()
        {
            RunGameTest(new TestLightShafts());
        }
    }
}
