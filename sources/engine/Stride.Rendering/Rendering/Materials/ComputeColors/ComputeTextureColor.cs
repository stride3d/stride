// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A color texture node.
    /// </summary>
    [DataContract("ComputeTextureColor")]
    [Display("Texture")]
    public class ComputeTextureColor : ComputeTextureBase, IComputeColor
    {
        private bool hasChanged = true;

        // Possible optimization will be to keep this on the ComputeTextureBase side
        private Texture cachedTexture;

        /// <summary>
        /// Constructor
        /// </summary>
        public ComputeTextureColor()
            : this(null, TextureCoordinate.Texcoord0, Vector2.One, Vector2.Zero)
        {
            hasChanged = true;
            cachedTexture = null;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeTextureColor"/> class.
        /// </summary>
        /// <param name="texture">The texture.</param>
        public ComputeTextureColor(Texture texture)
            : this(texture, TextureCoordinate.Texcoord0, Vector2.One, Vector2.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeTextureColor" /> class.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="texcoordIndex">Index of the texcoord.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="offset">The offset.</param>
        public ComputeTextureColor(Texture texture, TextureCoordinate texcoordIndex, Vector2 scale, Vector2 offset)
            : base(texture, texcoordIndex, scale, offset)
        {
            FallbackValue = new ComputeColor(Color4.White);
        }

        /// <inheritdoc/>
        public bool HasChanged
        {
            get
            {
                if (!hasChanged && cachedTexture == Texture)
                    return false;

                hasChanged = false;
                cachedTexture = Texture;
                return true;
            }
        }

        /// <summary>
        /// Sets the channel swizzling for texture sampling.
        /// </summary>
        /// <userdoc>The default value is `rgba`.</userdoc>
        public string Swizzle { get; set; }

        /// <summary>
        /// Gets or sets the default value used when no texture is set.
        /// </summary>
        /// <userdoc>The fallback value used when no texture is set.</userdoc>
        [NotNull]
        [DataMember(15)]
        public ComputeColor FallbackValue { get; set; }

        protected override string GetTextureChannelAsString()
        {
            // Use all channels
            return string.IsNullOrEmpty(Swizzle) ? "rgba" : Swizzle;
        }

        public override ShaderSource GenerateShaderFromFallbackValue(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            return FallbackValue?.GenerateShaderSource(context, baseKeys);
        }
    }
}
