// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestBitmapSpriteFont : GraphicTestGameBase
    {
        private SpriteBatch spriteBatch;
        private SpriteFont testFont;
        private Texture colorTexture;

        public TestBitmapSpriteFont()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawSpriteFont).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            testFont = Content.Load<SpriteFont>("StaticFonts/TestBitmapFont");

            // Instantiate a SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);
            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                DrawSpriteFont();
        }

        private void DrawSpriteFont()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Render the text
            spriteBatch.Begin(GraphicsContext);

            const string text = "test 0123456789";
            var dim = testFont.MeasureString(text);

            const int x = 20;
            const int y = 20;
            spriteBatch.Draw(colorTexture, new Rectangle(x, y, (int)dim.X, (int)dim.Y), Color.Green);
            spriteBatch.DrawString(testFont, text, new Vector2(x, y), Color.White);
            spriteBatch.DrawString(testFont, text, new Vector2(x, y + dim.Y + 10), Color.Red);

            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.S))
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "sprite-font-bitmap-test.png");
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunBitmapSpriteFont()
        {
            RunGameTest(new TestBitmapSpriteFont());
        }
    }
}
