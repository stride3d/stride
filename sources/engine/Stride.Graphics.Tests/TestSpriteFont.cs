// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestSpriteFont : GraphicTestGameBase
    {
        private SpriteBatch spriteBatch;
        private SpriteFont arial18;
        private SpriteFont msSansSerif13;
        private SpriteFont arial20;
        private SpriteFont arial20ClearType;
        private SpriteFont arial20Bold;
        private SpriteFont courrierNew13;
        private SpriteFont calibri85;
        private Texture colorTexture;

        private float rotationAngle;

        private readonly string assetPrefix = "";
        private readonly string saveImageSuffix = "";

        public TestSpriteFont(string assetPrefix, string saveImageSuffix)
        {
            this.assetPrefix = assetPrefix;
            this.saveImageSuffix = saveImageSuffix;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(() => SetRotationAndDraw(0)).TakeScreenshot();
            FrameGameSystem.Draw(() => SetRotationAndDraw(3.1415f)).TakeScreenshot();
            FrameGameSystem.Draw(() => SetRotationAndDraw(4)).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            arial18 = Content.Load<SpriteFont>(assetPrefix+"Arial18");
            msSansSerif13 = Content.Load<SpriteFont>(assetPrefix+"MicrosoftSansSerif13");
            arial20 = Content.Load<SpriteFont>(assetPrefix+"Arial20");
            arial20ClearType = Content.Load<SpriteFont>(assetPrefix+"Arial20ClearType");
            arial20Bold = Content.Load<SpriteFont>(assetPrefix+"Arial20Bold");
            calibri85 = Content.Load<SpriteFont>(assetPrefix+"Calibri85");
            courrierNew13 = Content.Load<SpriteFont>(assetPrefix+"CourierNew13");
            
            // Instantiate a SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);
            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            
            if (!ScreenShotAutomationEnabled)
            {
                rotationAngle += 1 / 60f; // frame-dependent and not time-dependent

                DrawSpriteFont();
            }
        }

        private void SetRotationAndDraw(float rotation)
        {
            rotationAngle = rotation;

            DrawSpriteFont();
        }

        private void DrawSpriteFont()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Render the text
            spriteBatch.Begin(GraphicsContext);

            var text = "This text is in Arial 20 with anti-alias\nand multiline...";
            var dim = arial20.MeasureString(text);

            int x = 20, y = 20;
            spriteBatch.Draw(colorTexture, new Rectangle(x, y, (int)dim.X, (int)dim.Y), Color.Green);


            arial20.PreGenerateGlyphs(text, arial20.Size * Vector2.One);
            spriteBatch.DrawString(arial20, text, new Vector2(x, y), Color.White);

            text = "Measured: " + dim;
            courrierNew13.PreGenerateGlyphs(text, courrierNew13.Size * Vector2.One);
            spriteBatch.DrawString(courrierNew13, text, new Vector2(x, y + dim.Y + 5), Color.GreenYellow);

            text = @"
-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
Text using Courier New 13 fixed font
0123456789 - 0123456789 - 0123456789
ABCDEFGHIJ - ABCDEFGHIJ - A1C3E5G7I9
-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_";

            courrierNew13.PreGenerateGlyphs(text, courrierNew13.Size * Vector2.One);
            spriteBatch.DrawString(courrierNew13, text, new Vector2(x, y + dim.Y + 8), Color.White);

            text = "Arial 13, font with with antialias.";
            arial18.PreGenerateGlyphs(text, arial18.Size * Vector2.One);
            spriteBatch.DrawString(arial18, text, new Vector2(x, y + 150), Color.White);

            text = "Microsoft Sans Serif 13, font with cleartype antialias.";
            msSansSerif13.PreGenerateGlyphs(text, msSansSerif13.Size * Vector2.One);
            spriteBatch.DrawString(msSansSerif13, text, new Vector2(x, y + 175), Color.White);

            text = "Font is in bold - Arial 20";
            arial20Bold.PreGenerateGlyphs(text, arial20Bold.Size * Vector2.One);
            spriteBatch.DrawString(arial20Bold, text, new Vector2(x, y + 190), Color.White);

            text = "Bigger font\nCalibri 85";
            y = 240;
            dim = calibri85.MeasureString(text);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y, (int)dim.X, (int)dim.Y), Color.Red);
            calibri85.PreGenerateGlyphs(text, calibri85.Size * Vector2.One);
            spriteBatch.DrawString(calibri85, text, new Vector2(x, y), Color.White);

            text = "Rendering test\nRotated On Center";
            dim = arial20.MeasureString(text);
            arial20.PreGenerateGlyphs(text, arial20.Size * Vector2.One);
            spriteBatch.DrawString(arial20, text, new Vector2(600, 120), Color.White, -rotationAngle, new Vector2(dim.X / 2.0f, dim.Y / 2.0f), Vector2.One, SpriteEffects.None, 0.0f, TextAlignment.Left);
            
            text = "Arial20 - ClearType\nAbc /\\Z Ghi SWy {}:;=&%@";
            arial20ClearType.PreGenerateGlyphs(text, arial20ClearType.Size * Vector2.One);
            spriteBatch.DrawString(arial20ClearType, text, new Vector2(470, 250), Color.White);

            text = "Abc /\\Z Ghi SWy {}:;=&%@\nArial20 - Standard";
            arial20.PreGenerateGlyphs(text, arial20.Size * Vector2.One);
            spriteBatch.DrawString(arial20, text, new Vector2(470, 300), Color.White);

            text = "Arial20 simulate shadow";
            arial20.PreGenerateGlyphs(text, arial20.Size * Vector2.One);
            spriteBatch.DrawString(arial20, text, new Vector2(471, 391), Color.Red);
            spriteBatch.DrawString(arial20, text, new Vector2(470, 390), Color.White);

            text = "Arial20 scaled x1.5";
            arial20.PreGenerateGlyphs(text, arial20.Size * Vector2.One);
            spriteBatch.DrawString(arial20, text, new Vector2(470, 420), Color.White, 0.0f, Vector2.Zero, 1.5f * Vector2.One, SpriteEffects.None, 0.0f, TextAlignment.Left);

            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.S))
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "sprite-font-" + saveImageSuffix + ".png");
        }
    }
}
