// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xunit;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Games;

namespace Xenko.Graphics.Tests
{
    public class TestDrawQuad : GraphicTestGameBase
    {
        private Texture offlineTarget;
        private bool firstSave;

        public TestDrawQuad()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawQuad).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // TODO DisposeBy is not working with device reset
            offlineTarget = Texture.New2D(GraphicsDevice, 512, 512, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                DrawQuad();

            if (firstSave)
            {
                SaveTexture(offlineTarget, "offlineTarget.png");
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "backBuffer.png");
                firstSave = false;
            }
        }

        private void DrawQuad()
        {
            // Clears the screen 
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.LimeGreen);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);

            // Render to the backbuffer
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);
            GraphicsContext.DrawTexture(UVTexture);

            // -> Render to back by using intermediate texture
            //GraphicsDevice.SetDepthAndRenderTarget(offlineTarget);
            //GraphicsDevice.DrawTexture(UVTexture);
            //
            //// Render to the backbuffer using offline texture
            //GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            //GraphicsDevice.DrawTexture(offlineTarget.Texture);
        }

        internal static void Main()
        {
            using (var game = new TestDrawQuad())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunDrawQuad()
        {
            RunGameTest(new TestDrawQuad());
        }
    }
}
