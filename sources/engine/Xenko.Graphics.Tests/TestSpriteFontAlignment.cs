// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

using System.Threading.Tasks;

using Xenko.Core.Mathematics;
using Xenko.Games;

namespace Xenko.Graphics.Tests
{
    public class TestSpriteFontAlignment : GraphicTestGameBase
    {
        private SpriteFont arial;

        private SpriteBatch spriteBatch;
        private Texture colorTexture;

        private const string AssetPrefix = "StaticFonts/";

        private const string Text1 = @"This is a sample text.
It covers several lines
Short ones,
Medium ones,
And very long long ones.";

        private const string Text2 = @"
One blank line above


Two blank lines above
One blank line below
";

        public TestSpriteFontAlignment()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawText).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            arial = Content.Load<SpriteFont>(AssetPrefix + "Arial18");

            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

            // Instantiate a SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                DrawText();
        }

        private void DrawText()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Render the text
            spriteBatch.Begin(GraphicsContext);

            var dim1 = arial.MeasureString(Text1);
            var dim2 = arial.MeasureString(Text2);

            var x = 20;
            var y = 10;
            var title = "Arial Left aligned";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim1.X, (int)dim1.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text1, new Vector2(x, y + 20), Color.Black);

            x = 270;
            title = "Arial center aligned";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim1.X, (int)dim1.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text1, new Vector2(x, y + 20), Color.Black, TextAlignment.Center);

            x = 520;
            title = "Arial right aligned";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim1.X, (int)dim1.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text1, new Vector2(x, y + 20), Color.Black, TextAlignment.Right);

            x = 20;
            y = 250;
            title = "Test on blank lines";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim2.X, (int)dim2.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text2, new Vector2(x, y + 20), Color.Black, TextAlignment.Center);

            spriteBatch.End();
        }

        internal static void Main()
        {
            using (var game = new TestSpriteFontAlignment())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestSpriteFontAlignment()
        {
            RunGameTest(new TestSpriteFontAlignment());
        }
    }
}
