// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace Xenko.Rendering.Sprites
{
    public class RenderSprite : RenderObject
    {
        private Matrix lastWorldMatrix;
        private Vector2 lastHalfSpriteSize;
        private SpriteType lastSpriteType;

        public SpriteComponent SpriteComponent;
        public TransformComponent TransformComponent;

        internal void CalculateBoundingBox()
        {
            var transform = TransformComponent;
            var currentSprite = SpriteComponent.CurrentSprite;

            RenderGroup = SpriteComponent.RenderGroup;

            // update the sprite bounding box
            var halfSpriteSize = currentSprite?.Size / 2 ?? Vector2.Zero;
            var worldMatrix = transform.WorldMatrix;

            // Only calculate if we've changed...
            if (lastWorldMatrix != worldMatrix || lastHalfSpriteSize != halfSpriteSize || lastSpriteType != SpriteComponent.SpriteType)
            {
                Vector3 halfBoxSize;
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

                // Update bounding box
                BoundingBox = new BoundingBoxExt(boxWorldPosition - halfBoxSize, boxWorldPosition + halfBoxSize);

                // Save current state for next check
                lastWorldMatrix = worldMatrix;
                lastHalfSpriteSize = halfSpriteSize;
                lastSpriteType = SpriteComponent.SpriteType;
            }
        }
    }
}
