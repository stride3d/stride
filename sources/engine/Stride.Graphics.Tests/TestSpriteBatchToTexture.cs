// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.Graphics.Tests
{
    public class TestSpriteBatchToTexture : GraphicTestGameBase
    {
        private const int OfflineWidth = 512;
        private const int OfflineHeight = 512;

        private Texture offlineTarget;
        private Texture depthBuffer;

        private SpriteBatch spriteBatch;

        private Texture uv;
        private SpriteSheet spheres;

        private SpriteFont arial;

        private int width;
        private int height;

        public TestSpriteBatchToTexture()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(RenderToTexture).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);

            offlineTarget = Texture.New2D(GraphicsDevice, OfflineWidth, OfflineHeight, PixelFormat.R8G8B8A8_UNorm, TextureFlags.ShaderResource | TextureFlags.RenderTarget).DisposeBy(this);
            depthBuffer = Texture.New2D(GraphicsDevice, OfflineWidth, OfflineHeight, PixelFormat.D16_UNorm, TextureFlags.DepthStencil).DisposeBy(this);

            uv = Content.Load<Texture>("uv");
            spheres = Content.Load<SpriteSheet>("SpriteSphere");

            arial = Content.Load<SpriteFont>("StaticFonts/Arial18");

            width = GraphicsDevice.Presenter.BackBuffer.ViewWidth;
            height = GraphicsDevice.Presenter.BackBuffer.ViewHeight;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                RenderToTexture();
        }

        private void RenderToTexture()
        {
            // render into texture
            GraphicsContext.CommandList.Clear(offlineTarget, new Color4(0,0,0,0));
            GraphicsContext.CommandList.Clear(depthBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(depthBuffer, offlineTarget);

            spriteBatch.Begin(GraphicsContext);
            spriteBatch.Draw(uv, new RectangleF(0, 0, OfflineWidth, OfflineHeight), null, Color.White, 0, Vector2.Zero);
            spriteBatch.Draw(spheres[0].Texture, Vector2.Zero, spheres[0].Region, Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, ImageOrientation.AsIs, 1);
            spriteBatch.DrawString(arial, "Text on Top", new Vector2(75, 75), Color.White, 0, Vector2.Zero, Vector2.One, SpriteEffects.None, 2f, TextAlignment.Left);
            spriteBatch.End();

            // copy texture on screen
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(GraphicsContext);
            spriteBatch.Draw(offlineTarget, new RectangleF(0, 0, width, height), Color.White);
            spriteBatch.End();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunSpriteBatchToTexture()
        {
            RunGameTest(new TestSpriteBatchToTexture());
        }
    }
}
