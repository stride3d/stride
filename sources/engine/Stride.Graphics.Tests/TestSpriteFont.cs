// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Threading.Tasks;

using Stride.Core.Mathematics;
using Stride.Games;
using Stride.Input;

namespace Stride.Graphics.Tests
{
    public class TestSpriteFont(string assetPrefix, string saveImageSuffix) : GraphicTestGameBase
    {
        protected SpriteBatch spriteBatch;

        private SpriteFont notoSans18;
        private SpriteFont notoSans13;
        private SpriteFont notoSans20;
        private SpriteFont notoSans20ClearType;
        private SpriteFont notoSansBold20;
        private SpriteFont liberationMono13;
        private SpriteFont notoSans65;

        private Texture whiteTexture;

        private float rotationAngle;


        /// <inheritdoc/>
        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(() => SetRotationAndDraw(0)).TakeScreenshot();
            FrameGameSystem.Draw(() => SetRotationAndDraw(3.1415f)).TakeScreenshot();
            FrameGameSystem.Draw(() => SetRotationAndDraw(4)).TakeScreenshot();
        }

        /// <inheritdoc/>
        protected override async Task LoadContent()
        {
            await base.LoadContent();

            notoSans18 = Content.Load<SpriteFont>(assetPrefix + "NotoSans18");
            notoSans13 = Content.Load<SpriteFont>(assetPrefix + "NotoSans13");
            notoSans20 = Content.Load<SpriteFont>(assetPrefix + "NotoSans20");
            notoSans20ClearType = Content.Load<SpriteFont>(assetPrefix + "NotoSans20ClearType");
            notoSansBold20 = Content.Load<SpriteFont>(assetPrefix + "NotoSansBold20");
            notoSans65 = Content.Load<SpriteFont>(assetPrefix + "NotoSans65");
            liberationMono13 = Content.Load<SpriteFont>(assetPrefix + "LiberationMono13");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            whiteTexture = GraphicsDevice.GetSharedWhiteTexture();
        }

        /// <inheritdoc/>
        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if (!ScreenShotAutomationEnabled)
            {
                rotationAngle += 1 / 60f; // Frame-dependent and not time-dependent

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

            var text = "This text is in Noto Sans 20 with anti-alias\nand multiline...";
            var dim = notoSans20.MeasureString(text);

            int x = 20, y = 20;
            spriteBatch.Draw(whiteTexture, new Rectangle(x, y, (int) dim.X, (int) dim.Y), Color.Green);

            notoSans20.PreGenerateGlyphs(text, notoSans20.Size * Vector2.One);
            spriteBatch.DrawString(notoSans20, text, new Vector2(x, y), Color.White);

            text = FormattableString.Invariant($"Measured: {dim:F3}");
            liberationMono13.PreGenerateGlyphs(text, liberationMono13.Size * Vector2.One);
            spriteBatch.DrawString(liberationMono13, text, new Vector2(x, y + dim.Y + 5), Color.GreenYellow);

            text = @"
-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_
Text using Liberation Mono 13 fixed font
0123456789 - 0123456789 - 0123456789
ABCDEFGHIJ - ABCDEFGHIJ - A1C3E5G7I9
-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_-_";

            liberationMono13.PreGenerateGlyphs(text, liberationMono13.Size * Vector2.One);
            spriteBatch.DrawString(liberationMono13, text, new Vector2(x, y + dim.Y + 8), Color.White);

            text = "Noto Sans 18, font with antialias.";
            notoSans18.PreGenerateGlyphs(text, notoSans18.Size * Vector2.One);
            spriteBatch.DrawString(notoSans18, text, new Vector2(x, y + 150), Color.White);

            text = "Noto Sans 13, font with cleartype antialias.";
            notoSans13.PreGenerateGlyphs(text, notoSans13.Size * Vector2.One);
            spriteBatch.DrawString(notoSans13, text, new Vector2(x, y + 175), Color.White);

            text = "Font is in bold - Noto Sans Bold 20";
            notoSansBold20.PreGenerateGlyphs(text, notoSansBold20.Size * Vector2.One);
            spriteBatch.DrawString(notoSansBold20, text, new Vector2(x, y + 190), Color.White);

            text = "Bigger font\nNoto Sans 65";
            y = 240;
            dim = notoSans65.MeasureString(text);
            spriteBatch.Draw(whiteTexture, new Rectangle(x, y, (int) dim.X, (int) dim.Y), Color.Red);
            notoSans65.PreGenerateGlyphs(text, notoSans65.Size * Vector2.One);
            spriteBatch.DrawString(notoSans65, text, new Vector2(x, y), Color.White);

            text = "Rendering test\nRotated On Center";
            dim = notoSans20.MeasureString(text);
            notoSans20.PreGenerateGlyphs(text, notoSans20.Size * Vector2.One);
            spriteBatch.DrawString(notoSans20, text, new Vector2(600, 120), Color.White, -rotationAngle, new Vector2(dim.X / 2.0f, dim.Y / 2.0f), Vector2.One, SpriteEffects.None, 0.0f, TextAlignment.Left);

            text = "NotoSans 20 - ClearType\nAbc /\\Z Ghi SWy {}:;=&%@";
            notoSans20ClearType.PreGenerateGlyphs(text, notoSans20ClearType.Size * Vector2.One);
            spriteBatch.DrawString(notoSans20ClearType, text, new Vector2(470, 250), Color.White);

            text = "Abc /\\Z Ghi SWy {}:;=&%@\nNotoSans 20 - Standard";
            notoSans20.PreGenerateGlyphs(text, notoSans20.Size * Vector2.One);
            spriteBatch.DrawString(notoSans20, text, new Vector2(470, 300), Color.White);

            text = "NotoSans 20 simulate shadow";
            notoSans20.PreGenerateGlyphs(text, notoSans20.Size * Vector2.One);
            spriteBatch.DrawString(notoSans20, text, new Vector2(471, 391), Color.Red);
            spriteBatch.DrawString(notoSans20, text, new Vector2(470, 390), Color.White);

            text = "NotoSans 20 scaled x1.5";
            notoSans20.PreGenerateGlyphs(text, notoSans20.Size * Vector2.One);
            spriteBatch.DrawString(notoSans20, text, new Vector2(470, 420), Color.White, 0.0f, Vector2.Zero, 1.5f * Vector2.One, SpriteEffects.None, 0.0f, TextAlignment.Left);

            spriteBatch.End();
        }

        /// <inheritdoc/>
        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.S))
                SaveTexture(GraphicsDevice.Presenter.BackBuffer, "sprite-font-" + saveImageSuffix + ".png");
        }
    }
}
