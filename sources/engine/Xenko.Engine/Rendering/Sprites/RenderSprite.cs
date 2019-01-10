// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Rendering.Sprites
{
    public class RenderSprite : RenderObject
    {
        public SpriteComponent SpriteComponent;
        public TransformComponent TransformComponent;

        internal void CalculateBoundingBox()
        {
            var transform = TransformComponent;
            var currentSprite = SpriteComponent.CurrentSprite;

            // TODO: Optimize this to cache calculations...

            // update the sprite bounding box
            Vector3 halfBoxSize;
            var halfSpriteSize = currentSprite?.Size / 2 ?? Vector2.Zero;
            var worldMatrix = TransformComponent.WorldMatrix;
            var boxWorldPosition = worldMatrix.TranslationVector;

            if (SpriteComponent.SpriteType == SpriteType.Billboard)
            {
                // Make a gross estimation here as we don't have access to the camera view matrix
                // TODO: move this code or grant camera view matrix access to this processor
                var maxScale = Math.Max(worldMatrix.Row1.Length(), Math.Max(worldMatrix.Row2.Length(), worldMatrix.Row3.Length()));
                halfBoxSize = maxScale * halfSpriteSize.Length() * Vector3.One;
            }
            else
            {
                halfBoxSize = new Vector3(
                    Math.Abs(worldMatrix.M11 * halfSpriteSize.X + worldMatrix.M21 * halfSpriteSize.Y),
                    Math.Abs(worldMatrix.M12 * halfSpriteSize.X + worldMatrix.M22 * halfSpriteSize.Y),
                    Math.Abs(worldMatrix.M13 * halfSpriteSize.X + worldMatrix.M23 * halfSpriteSize.Y));
            }

            // Get mins & maxes...
            var minX = boxWorldPosition.X - halfBoxSize.X;
            var minY = boxWorldPosition.Y - halfBoxSize.Y;
            var maxX = boxWorldPosition.X + halfBoxSize.X;
            var maxY = boxWorldPosition.Y + halfBoxSize.Y;

            // Calculate new size...
            var width = maxX - minX;
            var height = maxY - minY;

            // Assignment...
            BoundingBox.Center = transform.WorldMatrix.TranslationVector;
            BoundingBox.Extent = new Vector3(width / 2, height / 2, 0);
            RenderGroup = SpriteComponent.RenderGroup;
        }
    }
}
