// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;

using Xunit;

using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.Graphics.Tests
{
    public class TestSprite : GraphicTestGameBase
    {
        private SpriteSheet spriteUv;
        private SpriteSheet spriteSphere;

        private SpriteBatch spriteBatch;

        public TestSprite()
        {
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawSprites).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteUv = Content.Load<SpriteSheet>("SpriteUV");
            spriteSphere = Content.Load<SpriteSheet>("SpriteSphere");
            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            DrawSprites();
        }

        private void DrawSprites()
        {
            const int spaceSpan = 5;

            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(GraphicsContext, SpriteSortMode.Texture, BlendStates.AlphaBlend);

            var spriteUvSize = new Vector2(spriteUv.Sprites[0].Region.Width, spriteUv.Sprites[0].Region.Height);
            var spriteSphereSize = new Vector2(spriteSphere.Sprites[0].Region.Width, spriteSphere.Sprites[0].Region.Height);

            // draw sprite using frame index
            var positionUv = new Vector2(spaceSpan + spriteUvSize.X/2, spaceSpan + spriteUvSize.Y/2);
            spriteUv.Sprites[0].Draw(spriteBatch, positionUv);

            positionUv.X += spriteUvSize.X + spaceSpan;
            spriteUv.Sprites[1].Draw(spriteBatch, positionUv);

            positionUv.X += spriteUvSize.X + spaceSpan;
            spriteUv.Sprites[2].Draw(spriteBatch, positionUv);

            positionUv.X += spriteUvSize.X + spaceSpan;
            spriteUv.Sprites[3].Draw(spriteBatch, positionUv);

            // draw spheres
            positionUv.X = spaceSpan + spriteUvSize.X/2;
            positionUv.Y += spriteUvSize.Y + spaceSpan;
            spriteUv.Sprites[0].Draw(spriteBatch, positionUv, depthLayer: -2);

            var positionSphere = positionUv + new Vector2(spriteUvSize.X / 2, 0);
            spriteSphere.Sprites[0].Draw(spriteBatch, positionSphere, depthLayer: -1);

            positionUv.X += spaceSpan + spriteUvSize.X;
            spriteUv.Sprites[0].Draw(spriteBatch, positionUv, spriteEffects: SpriteEffects.FlipVertically);

            positionSphere = positionUv + new Vector2(spriteSphereSize.X + spaceSpan, 0);
            spriteSphere.Sprites[0].Draw(spriteBatch, positionSphere, (float)Math.PI / 2);

            positionSphere.X += spriteSphereSize.X + spaceSpan;
            spriteSphere.Sprites[0].Draw(spriteBatch, positionSphere, Color.GreenYellow, Vector2.One);

            positionSphere.X += spriteSphereSize.X + spaceSpan;
            spriteSphere.Sprites[0].Draw(spriteBatch, positionSphere, Color.White, new Vector2(0.66f, 0.33f), depthLayer: 1);
            
            positionSphere.X = spaceSpan;
            positionSphere.Y += 1.5f * spriteSphereSize.Y;
            spriteSphere.Sprites[0].Center = new Vector2(0, spriteSphereSize.Y);
            spriteSphere.Sprites[0].Draw(spriteBatch, positionSphere, depthLayer: 1);
            spriteSphere.Sprites[0].Center = new Vector2(spriteSphereSize.X / 2, spriteSphereSize.Y / 2);

            spriteBatch.End();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestSprite()
        {
            RunGameTest(new TestSprite());
        }
    }
}
