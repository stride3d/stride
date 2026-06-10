// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks;

using Stride.Core.Mathematics;
using Stride.Games;
using Xunit;

namespace Stride.Graphics.Tests
{
    public class TestStaticSpriteFont : TestSpriteFont
    {
        // SDF fonts are offline-compiled (like static fonts) but rendered through a dedicated
        // signed-distance-field shader that keeps edges crisp at any scale.
        private SpriteFont signedDistanceFieldFont;

        public TestStaticSpriteFont() : base(assetPrefix: "StaticFonts/", saveImageSuffix: "sta")
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawSignedDistanceFieldFont).TakeScreenshot(testName: "SDF");
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            signedDistanceFieldFont = Content.Load<SpriteFont>("StaticFonts/NotoSansSDF");
        }

        private void DrawSignedDistanceFieldFont()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Deferred, spriteBatch.TextureSpriteFontEffect);

            spriteBatch.DrawString(signedDistanceFieldFont, "Signed distance field font", new Vector2(20, 20), Color.White);

            // Scaled up: the distance field stays crisp where a bitmap font would blur.
            spriteBatch.DrawString(signedDistanceFieldFont, "Crisp at 3x", new Vector2(20, 80), Color.White,
                0f, Vector2.Zero, 3f * Vector2.One, SpriteEffects.None, 0f, TextAlignment.Left);

            spriteBatch.DrawString(signedDistanceFieldFont, "AaBbCc 0123456789 /\\Z {}:;=&%@", new Vector2(20, 300), Color.White);

            spriteBatch.End();
        }


        [Fact]
        public void RunTestStaticSpriteFont()
        {
            RunGameTest(new TestStaticSpriteFont());
        }
    }
}
