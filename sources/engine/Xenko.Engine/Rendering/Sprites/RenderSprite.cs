// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;
using Xenko.Graphics;

namespace Xenko.Rendering.Sprites
{
    public enum SpriteSampler
    {
        [Display("Point (Nearest)")]
        PointClamp,

        [Display("Linear")]
        LinearClamp,

        [Display("Anisotropic")]
        AnisotropicClamp,

        // Note These values are left out on purpose, but can be included if needed
        //PointWrap,
        //LinearWrap,
        //AnisotropicWrap,
    }
    public enum SpriteBlend
    {
        ///<userdoc>No blending, the sprite is drawn as-is.</userdoc>
        None,

        ///<userdoc>Use alpha blending if the sprite has transparent pixels, disable the blending otherwise.</userdoc>
        Auto,

        /// <userdoc>Use the alpha component for blending the source and sprite.</userdoc>
        [Display("Alpha blend")]
        AlphaBlend,

        /// <userdoc>The sprite color is added to the source without using the alpha.</userdoc>
        [Display("Additive blend")]
        AdditiveBlend,

        ///<userdoc>Do not render the colors of the sprite. Renders only the depth to the stencil buffer.</userdoc>
        [Display("No color")]
        NoColor,
    }

    public class RenderSprite : RenderObject
    {
        // Cached states
        private Matrix lastWorldMatrix;
        private Vector2 lastHalfSpriteSize;
        private SpriteType lastSpriteType;

        public Matrix WorldMatrix;
        public float RotationEulerZ;

        public Sprite Sprite;
        public SpriteType SpriteType;
        public bool IgnoreDepth;
        public SpriteSampler Sampler;
        public SpriteBlend BlendMode;
        public SwizzleMode Swizzle;
        public bool IsAlphaCutoff;
        public bool PremultipliedAlpha;
        public Color4 Color;

        internal void CalculateBoundingBox()
        {
            // update the sprite bounding box
            var halfSpriteSize = Sprite?.Size / 2 ?? Vector2.Zero;

            // Only calculate if we've changed...
            if (lastWorldMatrix != WorldMatrix || lastHalfSpriteSize != halfSpriteSize || lastSpriteType != SpriteType)
            {
                Vector3 halfBoxSize;
                var boxWorldPosition = WorldMatrix.TranslationVector;

                if (SpriteType == SpriteType.Billboard)
                {
                    // Make a gross estimation here as we don't have access to the camera view matrix
                    // TODO: move this code or grant camera view matrix access to this processor
                    var maxScale = Math.Max(WorldMatrix.Row1.Length(), Math.Max(WorldMatrix.Row2.Length(), WorldMatrix.Row3.Length()));
                    halfBoxSize = maxScale * halfSpriteSize.Length() * Vector3.One;
                }
                else
                {
                    halfBoxSize = new Vector3(
                        Math.Abs(WorldMatrix.M11 * halfSpriteSize.X) + Math.Abs(WorldMatrix.M21 * halfSpriteSize.Y),
                        Math.Abs(WorldMatrix.M12 * halfSpriteSize.X) + Math.Abs(WorldMatrix.M22 * halfSpriteSize.Y),
                        Math.Abs(WorldMatrix.M13 * halfSpriteSize.X) + Math.Abs(WorldMatrix.M23 * halfSpriteSize.Y));
                }

                // Update bounding box
                BoundingBox = new BoundingBoxExt(boxWorldPosition - halfBoxSize, boxWorldPosition + halfBoxSize);

                // Save current state for next check
                lastWorldMatrix = WorldMatrix;
                lastHalfSpriteSize = halfSpriteSize;
                lastSpriteType = SpriteType;
            }
        }
    }
}
