// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Default Cel Shading ramp function applied
    /// </summary>
    [DataContract("MaterialCelShadingLightRamp")]
    [Display("Ramp")]
    public class MaterialCelShadingLightRamp : IMaterialCelShadingLightFunction
    {
        /// <summary>
        /// The texture Reference.
        /// </summary>
        /// <userdoc>
        /// The reference to the texture asset to use.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(null)]
        [Display("Ramp Texture")]
        public Texture RampTexture { get; set; }

        public ShaderSource Generate(MaterialGeneratorContext context) // (ShaderGeneratorContext context, MaterialComputeColorKeys baseKeys)
        {
            // If we haven't specified a texture use the default implementation
            if (RampTexture == null)
                return new ShaderClassSource("MaterialCelShadingLightDefault", false);

            context.MaterialPass.Parameters.Set(MaterialCelShadingLightRampKeys.CelShaderRamp, RampTexture);

            return new ShaderClassSource("MaterialCelShadingLightRamp");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialCelShadingLightRamp;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialCelShadingLightRamp).GetHashCode();
        }
    }
}
