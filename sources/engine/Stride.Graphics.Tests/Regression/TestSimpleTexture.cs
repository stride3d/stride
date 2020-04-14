// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Graphics.Regression;

namespace Stride.Graphics.Tests.Regression
{
    public class TestSimpleTexture : GameTestBase
    {
        /// <summary>
        /// The texture.
        /// </summary>
        private Texture texture;
        
        public TestSimpleTexture()
        {
            GraphicsDeviceManager.PreferredBackBufferWidth = 256;
            GraphicsDeviceManager.PreferredBackBufferHeight = 256;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            Console.WriteLine(@"Begin load.");
            texture = Content.Load<Texture>("small_uv");
            Console.WriteLine(@"End load.");
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.Draw(DrawTexture).TakeScreenshot();
        }

        private void DrawTexture()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            GraphicsContext.DrawTexture(texture, GraphicsDevice.SamplerStates.PointClamp);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            DrawTexture();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestSimpleTexture()
        {
            RunGameTest(new TestSimpleTexture());
        }
    }
}
