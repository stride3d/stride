// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading.Tasks;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.Graphics.Tests
{
    public class TestSpriteBatch : GraphicTestGameBase
    {
        private SpriteBatch spriteBatch;
        private Texture sphere;

        private const int SphereSpace = 4;
        private const int SphereWidth = 150;
        private const int SphereHeight = 150;
        private const int SphereCountPerRow = 6;
        private const int SphereTotalCount = 32;

        private float timeInSeconds;

        private SpriteSheet rotatedImages;

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(() => SetTimeAndDrawScene(0)).TakeScreenshot();
            FrameGameSystem.Draw(() => SetTimeAndDrawScene(0.27f)).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);
            sphere = Content.Load<Texture>("Sphere");
            rotatedImages = Content.Load<SpriteSheet>("RotatedImages");
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            timeInSeconds += 1 / 60f; // frame dependent for graphic unit testing.

            if (!ScreenShotAutomationEnabled)
                DrawScene();
        }

        private void SetTimeAndDrawScene(float time)
        {
            timeInSeconds = time;

            DrawScene();
        }

        private void DrawScene()
        {
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.BackBuffer, Color.Black);
            GraphicsContext.CommandList.Clear(GraphicsDevice.Presenter.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsContext.CommandList.SetRenderTargetAndViewport(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsDevice.Presenter.BackBuffer);

            spriteBatch.Begin(GraphicsContext);

            var pos = new Vector2(0f);
            var noRotation = rotatedImages["NoRotation"];
            var rotation90 = rotatedImages["Rotation90"];
            var width = noRotation.Region.Width;
            var height = noRotation.Region.Height;

            // Test image orientations API1
            spriteBatch.Draw(noRotation.Texture, pos, noRotation.Region, Color.White, 0, Vector2.Zero);
            pos.Y += height;
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, 0, Vector2.Zero, 1, orientation: ImageOrientation.Rotated90);
            pos.Y -= height;
            pos.X += width;

            // Test image orientations API2
            spriteBatch.Draw(noRotation.Texture, new RectangleF(pos.X, pos.Y, width, height), noRotation.Region, Color.White, 0, Vector2.Zero);
            pos.Y += height;
            spriteBatch.Draw(rotation90.Texture, new RectangleF(pos.X, pos.Y, width, height), rotation90.Region, Color.White, 0, Vector2.Zero, SpriteEffects.None, ImageOrientation.Rotated90);
            pos.Y -= height;
            pos.X += width;

            // Test image inversions (no rotation)
            spriteBatch.Draw(noRotation.Texture, pos, noRotation.Region, Color.White, 0, Vector2.Zero);
            pos.Y += height;
            spriteBatch.Draw(noRotation.Texture, pos, noRotation.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally);
            pos.Y += height;
            spriteBatch.Draw(noRotation.Texture, pos, noRotation.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipVertically);
            pos.Y += height;
            spriteBatch.Draw(noRotation.Texture, pos, noRotation.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipBoth);
            pos.Y -= 3 * height;
            pos.X += width;

            // Test image inversions (rotation 90)
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.None, ImageOrientation.Rotated90);
            pos.Y += height;
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipHorizontally, ImageOrientation.Rotated90);
            pos.Y += height;
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipVertically, ImageOrientation.Rotated90);
            pos.Y += height;
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, 0, Vector2.Zero, 1, SpriteEffects.FlipBoth, ImageOrientation.Rotated90);
            pos.Y -= 3 * height;
            pos.X += width;

            // Test with scales
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, 0, Vector2.Zero, new Vector2(0.66f, 1.33f), SpriteEffects.None, ImageOrientation.Rotated90);
            pos.X += 0.66f * width;

            // Test color
            spriteBatch.Draw(noRotation.Texture, pos, noRotation.Region, Color.Gray);
            pos.X += width;

            // Rotations and centers
            pos.Y += 2.5f * height;
            pos.X -= 0.66f * width;
            spriteBatch.Draw(rotation90.Texture, pos, rotation90.Region, Color.White, timeInSeconds, new Vector2(height / 2f, width / 2f), 1, SpriteEffects.None, ImageOrientation.Rotated90);

            pos.X = 0;
            pos.Y += 2f * height;

            const int NbRows = 1;
            const int NbColumns = 5;
            var textureOffset = new Vector2((float)GraphicsDevice.Presenter.BackBuffer.ViewWidth / NbColumns, (float)GraphicsDevice.Presenter.BackBuffer.ViewHeight / NbRows);
            var textureOrigin = new Vector2(SphereWidth / 2.0f, SphereHeight / 2.0f);
            var random = new Random(0);

            pos.Y += textureOrigin.Y / 2;
            pos.X += textureOrigin.X;

            // Display a grid of sphere
            for (int y = 0; y < NbRows; y++)
            {
                for (int x = 0; x < NbColumns; x++)
                {
                    var time = timeInSeconds + random.NextDouble();
                    var rotation = (float)(time * Math.PI * 2.0);
                    var sourceRectangle = GetSphereAnimation((float)time);
                    spriteBatch.Draw(sphere, pos + new Vector2(x * textureOffset.X, y * textureOffset.Y), sourceRectangle, Color.White, rotation, textureOrigin, layerDepth: -1);
                }
            }

            spriteBatch.End();
        }

        /// <summary>
        /// Calculates the rectangle region from the original Sphere bitmap.
        /// </summary>
        /// <param name="time">The current time</param>
        /// <returns>The region from the sphere texture to display</returns>
        private Rectangle GetSphereAnimation(float time)
        {
            var sphereIndex = MathUtil.Clamp((int)((time % 1.0f) * SphereTotalCount), 0, SphereTotalCount);

            int sphereX = sphereIndex % SphereCountPerRow;
            int sphereY = sphereIndex / SphereCountPerRow;
            return new Rectangle(sphereX * (SphereWidth + SphereSpace), sphereY * (SphereHeight + SphereSpace), SphereWidth, SphereHeight);
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Fact]
        public void RunTestSpriteBatch()
        {
            RunGameTest(new TestSpriteBatch());
        }
    }
}
