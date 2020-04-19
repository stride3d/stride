// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Assets.Physics;
using Stride.Assets.Presentation.Preview.Views;
using Stride.Core.Mathematics;
using Stride.Editor.Preview;
using Stride.Graphics;
using Stride.Physics;

namespace Stride.Assets.Presentation.Preview
{
    [AssetPreview(typeof(HeightmapAsset), typeof(HeightmapPreviewView))]
    public class HeightmapPreview : PreviewFromSpriteBatch<HeightmapAsset>
    {
        private Heightmap heightmap;
        private Texture heightmapTexture;
        private BlendStateDescription adequateBlendState;

        public int Width => heightmap?.Size.X ?? 0;
        public int Length => heightmap?.Size.Y ?? 0;

        /// <summary>
        /// Gets or sets a callback that will be invoked when the texture is loaded.
        /// </summary>
        public Action NotifyHeightmapLoaded { get; set; }

        protected override Vector2 SpriteSize
        {
            get
            {
                if (heightmapTexture == null)
                    return base.SpriteSize;

                return new Vector2(heightmapTexture.Width, heightmapTexture.Height);
            }
        }

        protected virtual Vector2 ImageCenter
        {
            get
            {
                if (heightmapTexture == null)
                    return Vector2.Zero;

                var imageSize = new Vector2(heightmapTexture.Width, heightmapTexture.Height);

                return imageSize / 2f;
            }
        }

        protected override void LoadContent()
        {
            heightmap = LoadAsset<Heightmap>(AssetItem.Location);

            heightmapTexture = heightmap?.CreateTexture(Game.GraphicsDevice);

            adequateBlendState = BlendStates.Opaque;

            NotifyHeightmapLoaded?.Invoke();

            // Always use LDR
            RenderingMode = RenderingMode.LDR;
        }

        protected override void UnloadContent()
        {
            if (heightmapTexture != null)
            {
                heightmapTexture.Dispose();
                heightmapTexture = null;
            }

            if (heightmap != null)
            {
                UnloadAsset(heightmap);
                heightmap = null;
            }
        }

        protected override void RenderSprite()
        {
            if (heightmapTexture == null)
                return;

            var origin = ImageCenter - SpriteOffsets;
            var region = new RectangleF(0, 0, heightmapTexture.Width, heightmapTexture.Height);
            var orientation = ImageOrientation.AsIs;

            SpriteBatch.Begin(Game.GraphicsContext, SpriteSortMode.Texture, adequateBlendState);
            SpriteBatch.Draw(heightmapTexture, WindowSize / 2, region, Color.White, 0, origin, SpriteScale, SpriteEffects.None, orientation, swizzle: SwizzleMode.RRR1);
            SpriteBatch.End();
        }
    }
}
