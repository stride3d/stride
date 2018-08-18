// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    /// <summary>
    /// A sprite represents a series frames in an atlas forming an animation. 
    /// </summary>
    public static class SpriteExtensions
    {
        /// <summary>
        /// Draw a sprite using a sprite batch and with white color and scale of 1.
        /// </summary>
        /// <param name="sprite">The sprite</param>
        /// <param name="spriteBatch">The sprite batch used to draw the sprite.</param>
        /// <param name="position">The position to which draw the sprite</param>
        /// <param name="rotation">The rotation to apply on the sprite</param>
        /// <param name="depthLayer">The depth layer to which draw the sprite</param>
        /// <param name="spriteEffects">The sprite effect to apply on the sprite</param>
        /// <remarks>This function must be called between the <see cref="SpriteBatch.Begin(Xenko.Graphics.SpriteSortMode,Xenko.Graphics.Effect)"/> 
        /// and <see cref="SpriteBatch.End()"/> calls of the provided <paramref name="spriteBatch"/></remarks>
        /// <exception cref="ArgumentException">The provided frame index is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The provided spriteBatch is null</exception>
        public static void Draw(this Sprite sprite, SpriteBatch spriteBatch, Vector2 position, float rotation = 0, float depthLayer = 0, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            sprite.Draw(spriteBatch, position, Color.White, Vector2.One, rotation, depthLayer, spriteEffects);
        }

        /// <summary>
        /// Draw a sprite using a sprite batch.
        /// </summary>
        /// <param name="sprite">The sprite</param>
        /// <param name="spriteBatch">The sprite batch used to draw the sprite.</param>
        /// <param name="position">The position to which draw the sprite</param>
        /// <param name="color">The color to use to draw the sprite</param>
        /// <param name="rotation">The rotation to apply on the sprite</param>
        /// <param name="scales">The scale factors to apply on the sprite</param>
        /// <param name="depthLayer">The depth layer to which draw the sprite</param>
        /// <param name="spriteEffects">The sprite effect to apply on the sprite</param>
        /// <remarks>This function must be called between the <see cref="SpriteBatch.Begin(Xenko.Graphics.SpriteSortMode,Xenko.Graphics.Effect)"/> 
        /// and <see cref="SpriteBatch.End()"/> calls of the provided <paramref name="spriteBatch"/></remarks>
        /// <exception cref="ArgumentException">The provided frame index is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The provided spriteBatch is null</exception>
        public static void Draw(this Sprite sprite, SpriteBatch spriteBatch, Vector2 position, Color color, Vector2 scales, float rotation = 0f, float depthLayer = 0, SpriteEffects spriteEffects = SpriteEffects.None)
        {
            if (spriteBatch == null) throw new ArgumentNullException("spriteBatch");

            if (sprite.Texture == null)
                return;

            spriteBatch.Draw(sprite.Texture, position, sprite.Region, color, rotation, sprite.Center, scales, spriteEffects, sprite.Orientation, depthLayer);
        }

        /// <summary>
        /// Draw a sprite in the 3D world using the provided 3D sprite batch, world matrix and color.
        /// </summary>
        /// <param name="sprite">The sprite</param>
        /// <param name="spriteBatch">The sprite batch used to draw the sprite.</param>
        /// <param name="worldMatrix">The world matrix of the sprite</param>
        /// <param name="color">The color to apply on the sprite</param>
        /// <remarks>This function must be called between the <see cref="SpriteBatch.Begin(Xenko.Graphics.SpriteSortMode,Xenko.Graphics.Effect)"/> 
        /// and <see cref="SpriteBatch.End()"/> calls of the provided <paramref name="spriteBatch"/></remarks>
        /// <exception cref="ArgumentException">The provided frame index is not valid.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The provided spriteBatch is null</exception>
        public static void Draw3D(this Sprite sprite, Sprite3DBatch spriteBatch, ref Matrix worldMatrix, ref Color4 color)
        {
            spriteBatch.Draw(sprite.Texture, ref worldMatrix, ref sprite.RegionInternal, ref sprite.SizeInternal, ref color, sprite.Orientation);
        }
    }
}
