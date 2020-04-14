// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Text;
using System.Threading.Tasks;

using Xunit;

using Xenko.Core.Mathematics;
using Xenko.Games;
using Xenko.Graphics.Font;

namespace Xenko.Graphics.Tests
{
    public class TestDynamicSpriteFontVarious : GraphicTestGameBase
    {
        private SpriteFont hanSans13;

        private SpriteBatch spriteBatch;
        
        private const string AssetPrefix = "DynamicFonts/";
        
        private readonly StringBuilder varyingString = new StringBuilder(VaryingStringLength);
        private const int VaryingStringLength = 200;
        private int varyingStringCurrentIndex = VaryingStringStartIndex;
        private const int VaryingStringStartIndex = 0x4e00;
        private const int VaryingStringEndIndex = 0x9faf;
        private const double VaryingStringTimeInterval = 1;
        private double accumulatedSeconds;

        public TestDynamicSpriteFontVarious()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(()=>SetTimeAndDraw(0)).TakeScreenshot();
            FrameGameSystem.Draw(()=>SetTimeAndDraw(1.1f)).TakeScreenshot();
            FrameGameSystem.Draw(()=>SetTimeAndDraw(3.5f)).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            hanSans13 = Content.Load<SpriteFont>(AssetPrefix + "HanSans13");

            // Instantiate a SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);

            for (int i = 0; i < VaryingStringLength; i++)
                varyingString.Append(' ');
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            accumulatedSeconds += 1 / 60f;

            if (!ScreenShotAutomationEnabled)
                DrawText();
        }

        private void SetTimeAndDraw(float time)
        {
            accumulatedSeconds = time;

            DrawText();
        }

        private void DrawText()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            // Render the text
            spriteBatch.Begin(GraphicsContext);

            var x = 20;
            var y = 10;
            
            var size = 11;
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            var dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);
            size = 13;
            y += (int)Math.Ceiling(dim.Y);
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);
            size = 16;
            y += (int)Math.Ceiling(dim.Y);
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);
            size = 19;
            y += (int)Math.Ceiling(dim.Y);
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);
            size = 21;
            y += (int)Math.Ceiling(dim.Y);
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);
            size = 27;
            y += (int)Math.Ceiling(dim.Y);
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);
            size = 33;
            y += (int)Math.Ceiling(dim.Y);
            hanSans13.PreGenerateGlyphs(BuildTextSize(size), size * Vector2.One);
            dim = hanSans13.MeasureString(BuildTextSize(size), size);
            spriteBatch.DrawString(hanSans13, BuildTextSize(size), size, new Vector2(x, y), Color.White);

            // change the varying string if necessary
            if (accumulatedSeconds > VaryingStringTimeInterval)
            {
                accumulatedSeconds = 0;
                for (int i = 0; i < VaryingStringLength;)
                {
                    for (int j = 0; j < 50 && i < VaryingStringLength; j++, ++i)
                    {
                        varyingString[i] = (char)varyingStringCurrentIndex;

                        ++varyingStringCurrentIndex;
                        if (varyingStringCurrentIndex > VaryingStringEndIndex)
                            varyingStringCurrentIndex = VaryingStringStartIndex;
                    }

                    // add return lines
                    if (i < VaryingStringLength)
                    {
                        varyingString[i] = '\n';
                        ++i;
                    }
                }
            }

            // print varying text
            y += (int)Math.Ceiling(dim.Y) + 10;
            hanSans13.PreGenerateGlyphs(varyingString.ToString(), 21 * Vector2.One);
            spriteBatch.DrawString(hanSans13, varyingString, 21, new Vector2(x, y), Color.White);

            spriteBatch.End();
        }

        private string BuildTextSize(int size)
        {
            return "HanSans size " + size +" pixels. 漢字のサイズは" + size + "ピクセル。";
        }

        internal static void Main()
        {
            using (var game = new TestDynamicSpriteFontVarious())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunDynamicSpriteFontVarious()
        {
            RunGameTest(new TestDynamicSpriteFontVarious());
        }
    }
}
