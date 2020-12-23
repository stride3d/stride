// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Graphics;

namespace JumpyJet
{
    /// <summary>
    /// A section of the parallax background.
    /// </summary>
    public class BackgroundSection
    {
        private readonly float depth;
        private readonly Int2 screenResolution;
        private readonly Vector2 screenCenter;

        // Texture
        private Texture texture;
        private RectangleF textureRegion;

        // First quad parameters
        private readonly Vector2 firstQuadPos;
        private Vector2 firstQuadOrigin;
        private RectangleF firstQuadRegion;

        // Second quad parameters
        private Vector2 secondQuadPos;
        private Vector2 secondQuadOrigin;
        private RectangleF secondQuadRegion;

        public bool IsUpdating { get; set; }
        public bool IsRunning { get; protected set; }
        public bool IsVisible { get; protected set; }
        public float ScrollPos { get; protected set; }
        public float ScrollWidth { get; protected set; }
        public float ScrollSpeed { get; protected set; }

        public BackgroundSection(Sprite backgroundSprite, Vector3 screenVirtualResolution, float scrollSpeed, float depth, Vector2 startPos = default(Vector2))
        {
            screenResolution = new Int2((int)screenVirtualResolution.X, (int)screenVirtualResolution.Y);
            screenCenter = new Vector2(screenVirtualResolution.X / 2, screenVirtualResolution.Y / 2);

            this.depth = depth;
            firstQuadPos = startPos;
            secondQuadPos = startPos;

            ScrollSpeed = scrollSpeed;
            ScrollPos = 0;

            CreateBackground(backgroundSprite.Texture, backgroundSprite.Region);

            IsUpdating = true;
        }

        public void DrawSprite(float elapsedTime, SpriteBatch spriteBatch)
        {
            if (IsUpdating)
            {
                // Update Scroll position
                if (ScrollPos >= textureRegion.Width)
                    ScrollPos = 0;

                ScrollPos += elapsedTime * ScrollSpeed;

                UpdateSpriteQuads();
            }

            // DrawParallax the first quad
            spriteBatch.Draw(texture, firstQuadPos + screenCenter, firstQuadRegion, Color.White, 0f, firstQuadOrigin, 1f, SpriteEffects.None, ImageOrientation.AsIs, depth);

            if (secondQuadRegion.Width > 0)
            {
                // DrawParallax the second quad
                spriteBatch.Draw(texture, secondQuadPos + screenCenter, secondQuadRegion, Color.White, 0f, secondQuadOrigin, 1f, SpriteEffects.None, ImageOrientation.AsIs, depth);
            }
        }

        private void CreateBackground(Texture bgTexture, RectangleF texReg)
        {
            texture = bgTexture;
            textureRegion = texReg;

            // Set offset to rectangle
            firstQuadRegion.X = textureRegion.X;
            firstQuadRegion.Y = textureRegion.Y;

            firstQuadRegion.Width = (textureRegion.Width > screenResolution.X) ? screenResolution.X : textureRegion.Width;
            firstQuadRegion.Height = (textureRegion.Height > screenResolution.Y) ? screenResolution.Y : textureRegion.Height;

            // Centering the content
            firstQuadOrigin.X = 0.5f * firstQuadRegion.Width;
            firstQuadOrigin.Y = 0.5f * firstQuadRegion.Height;

            // Copy data from first quad to second one
            secondQuadRegion = firstQuadRegion;
            secondQuadOrigin = firstQuadOrigin;
        }

        private void UpdateSpriteQuads()
        {
            // Update first Quad
            var firstQuadNewWidth = textureRegion.Width - ScrollPos;
            firstQuadRegion.Width = firstQuadNewWidth;
            // Update X position of the first Quad
            firstQuadRegion.X = ScrollPos;

            // Update second Quad
            // Calculate new X position and width of the second quad
            var secondQuadNewWidth = (ScrollPos + screenResolution.X) - textureRegion.Width;
            var secondQuadNewXPosition = (screenResolution.X - secondQuadNewWidth) / 2;

            secondQuadRegion.Width = secondQuadNewWidth;
            secondQuadPos.X = secondQuadNewXPosition;
            secondQuadOrigin.X = secondQuadNewWidth / 2f;
        }
    }
}
