// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestSpriteBatchResolution : GraphicTestGameBase
    {
        private SpriteFont staticFont;
        private SpriteFont dynamicFont;
        private SpriteBatch spriteBatch;

        private Texture round;
        private Texture colorTexture;
        private SpriteSheet spheres;

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(()=>SetVirtualResolutionAndDraw(new Vector2(1, 1))).TakeScreenshot();
            FrameGameSystem.Draw(()=>SetVirtualResolutionAndDraw(new Vector2(1.5f, 1.5f))).TakeScreenshot();
            FrameGameSystem.Draw(()=>SetVirtualResolutionAndDraw(new Vector2(0.5f, 0.5f))).TakeScreenshot();
            FrameGameSystem.Draw(()=>SetVirtualResolutionAndDraw(new Vector2(1.75f, 1.25f))).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var virtualResolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.ViewWidth, GraphicsDevice.Presenter.BackBuffer.ViewHeight, 200);
            spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = virtualResolution };
            spheres = Content.Load<SpriteSheet>("SpriteSphere");
            round = Content.Load<Texture>("round");
            staticFont = Content.Load<SpriteFont>("StaticFonts/CourierNew13");
            dynamicFont = Content.Load<SpriteFont>("DynamicFonts/CourierNew13");
            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
                DrawSprites();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.Left))
                spriteBatch.VirtualResolution = 3 / 4f * spriteBatch.VirtualResolution;
            if (Input.IsKeyReleased(Keys.Right))
                spriteBatch.VirtualResolution = 4 / 3f * spriteBatch.VirtualResolution;
        }

        private void SetVirtualResolutionAndDraw(Vector2 factor)
        {
            spriteBatch.VirtualResolution = new Vector3(factor.X * GraphicsDevice.Presenter.BackBuffer.ViewWidth, factor.Y*GraphicsDevice.Presenter.BackBuffer.ViewHeight, 100);

            DrawSprites();
        }

        private void DrawSprites()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(GraphicsContext);
            
            var x = 20f;
            var y = 20f;

            PerformStringDraws(ref x, ref y, false);
            PerformStringDraws(ref x, ref y, true);

            y += 10;

            spriteBatch.Draw(round, new Vector2(x, y), Color.Red);

            x += round.ViewWidth + 5;

            var sphere = spheres[0];
            spriteBatch.Draw(sphere.Texture, new Vector2(x, y), sphere.Region, Color.White);

            x += spheres[0].Region.Width + 5;

            spriteBatch.Draw(round, new RectangleF(x, y, round.ViewWidth / 2f, round.ViewHeight / 2f), Color.GreenYellow);
            
            spriteBatch.End();
        }

        private void PerformStringDraws(ref float x, ref float y, bool useDynamicFont)
        {
            var fontName = useDynamicFont ? "Dynamic" : "Static";
            var spriteFont = useDynamicFont ? dynamicFont : staticFont;
            var targetSize = new Vector2(GraphicsDevice.Presenter.BackBuffer.ViewWidth, GraphicsDevice.Presenter.BackBuffer.ViewHeight);
            var resolutionRatio = Vector2.One;
            if (useDynamicFont && spriteBatch.VirtualResolution.HasValue)
            {
                resolutionRatio.X = targetSize.X / spriteBatch.VirtualResolution.Value.X;
                resolutionRatio.Y = targetSize.Y / spriteBatch.VirtualResolution.Value.Y;
            }

            var text = fontName + " font drawn with SpriteBatch(text).";
            var dim = spriteBatch.MeasureString(spriteFont, text, targetSize);

            spriteBatch.Draw(colorTexture, new RectangleF(x, y, dim.X, dim.Y), Color.Green);

            spriteFont.PreGenerateGlyphs(text, spriteFont.Size * resolutionRatio);
            spriteBatch.DrawString(spriteFont, text, new Vector2(x, y), Color.White);

            y += 1.4f * dim.Y;

            var fontSize = 1.5f * spriteFont.Size;
            text = fontName + " font drawn with SpriteBatch(text, size).";
            dim = spriteBatch.MeasureString(spriteFont, text, fontSize, targetSize);

            spriteBatch.Draw(colorTexture, new RectangleF(x, y, dim.X, dim.Y), Color.Green);

            spriteFont.PreGenerateGlyphs(text, fontSize * resolutionRatio);
            spriteBatch.DrawString(spriteFont, text, fontSize, new Vector2(x, y), Color.White);

            y += 1.4f * dim.Y;
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestSpriteBatchResolution()
        {
            RunGameTest(new TestSpriteBatchResolution());
        }
    }
}
