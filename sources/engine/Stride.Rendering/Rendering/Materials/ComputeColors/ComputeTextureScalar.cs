// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Materials.ComputeColors
{
    /// <summary>
    /// A scalar texture node.
    /// </summary>
    [DataContract("ComputeTextureScalar")]
    [Display("Texture")]
    public class ComputeTextureScalar : ComputeTextureBase, IComputeScalar
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public ComputeTextureScalar()
            : this(null, TextureCoordinate.Texcoord0, Vector2.One, Vector2.Zero)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComputeTextureColor" /> class.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="texcoordIndex">Index of the texcoord.</param>
        /// <param name="scale">The scale.</param>
        /// <param name="offset">The offset.</param>
        public ComputeTextureScalar(Texture texture, TextureCoordinate texcoordIndex, Vector2 scale, Vector2 offset)
            : base(texture, texcoordIndex, scale, offset)
        {
            Channel = ColorChannel.R;
            FallbackValue = new ComputeFloat(1);
        }

        /// <summary>
        /// Gets or sets the default value used when no texture is set.
        /// </summary>
        /// <userdoc>The fallback value used when no texture is set.</userdoc>
        [NotNull]
        [DataMember(15)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 3)]
        public ComputeFloat FallbackValue { get; set; }

        /// <summary>
        /// Gets or sets the channel.
        /// </summary>
        /// <value>The channel.</value>
        /// <userdoc>Selects the RGBA channel to sample from the texture.</userdoc>
        [DataMember(20)]
        [DefaultValue(ColorChannel.R)]
        public ColorChannel Channel { get; set; }

        protected override string GetTextureChannelAsString()
        {
            return MaterialUtility.GetAsShaderString(Channel);
        }

        public override ShaderSource GenerateShaderFromFallbackValue(ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            return FallbackValue?.GenerateShaderSource(context, baseKeys);
        }
    }
}
