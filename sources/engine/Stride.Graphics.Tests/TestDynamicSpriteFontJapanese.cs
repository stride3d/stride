// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.Graphics.Tests
{
    public class TestDynamicSpriteFontJapanese : GraphicTestGameBase
    {
        private SpriteFont hanSans13;
        private SpriteFont hanSans18;

        private SpriteBatch spriteBatch;

        private const string AssetPrefix = "DynamicFonts/";

        private const string Text = @"
漢字（かんじ）は、古代中国に発祥を持つ文字。
古代において中国から日本、朝鮮、ベトナムな
ど周辺諸国にも伝わり、その形態・機能を利用
して日本語など各地の言語の表記にも使われて
いる（ただし、現在は漢字表記を廃している言
語もある。日本の漢字については日本における
漢字を参照）。
漢字は、現代も使われ続けている文字の中で最
も古く成立した[1][2]。人類史上、最も文字数
が多い文字体系であり、その数は10万文字をは
るかに超え他の文字体系を圧倒している。ただ
し万単位の種類のほとんどは歴史的な文書の中
でしか見られない頻度の低いものである。研究
によると、中国で機能的非識字状態にならない
ようにするには、3000から4000の漢字を知って
いれば充分という[3]";

        public TestDynamicSpriteFontJapanese()
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

            hanSans13 = Content.Load<SpriteFont>(AssetPrefix + "HanSans13");
            hanSans18 = Content.Load<SpriteFont>(AssetPrefix + "HanSans18");

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

            var x = 20;
            var y = 10;
            var title = "Han-Sans 13 aliased:";
            hanSans13.PreGenerateGlyphs(title, hanSans13.Size * Vector2.One);
            hanSans13.PreGenerateGlyphs(Text, hanSans13.Size * Vector2.One);
            spriteBatch.DrawString(hanSans13, title, new Vector2(x, y), Color.LawnGreen);
            spriteBatch.DrawString(hanSans13, Text, new Vector2(x, y + 10), Color.White);

            x = 320;
            y = 0;
            title = "Han-Sans 18 anti-aliased:";
            hanSans18.PreGenerateGlyphs(title, hanSans18.Size * Vector2.One);
            hanSans18.PreGenerateGlyphs(Text, hanSans18.Size * Vector2.One);
            spriteBatch.DrawString(hanSans18, title, new Vector2(x, y), Color.Red);
            spriteBatch.DrawString(hanSans18, Text, new Vector2(x, y + 5), Color.White);

            spriteBatch.End();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunDynamicSpriteFontJapanese()
        {
            RunGameTest(new TestDynamicSpriteFontJapanese());
        }
    }
}
