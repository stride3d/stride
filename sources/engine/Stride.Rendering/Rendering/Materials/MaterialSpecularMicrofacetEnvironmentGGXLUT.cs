// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Assets;
using Stride.Core.Serialization;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Environment function for Schlick fresnel, Smith-Schlick GGX visibility and GGX normal distribution.
    /// </summary>
    /// <remarks>
    /// Based on https://knarkowicz.wordpress.com/2014/12/27/analytical-dfg-term-for-ibl/.
    /// Note: their glossiness-roughness conversion formula is not same as ours, this will need to be recomputed.
    /// </remarks>
    [DataContract("MaterialSpecularMicrofacetEnvironmentGGXLUT")]
    [Display("GGX+Schlick+SchlickGGX (LUT)")]
    public class MaterialSpecularMicrofacetEnvironmentGGXLUT : IMaterialSpecularMicrofacetEnvironmentFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            var texture = context.GraphicsProfile >= GraphicsProfile.Level_10_0
                ? AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("a49995f8-2380-4baa-a03e-f8d1da35b79a"), "StrideEnvironmentLightingDFGLUT16")
                : AttachedReferenceManager.CreateProxyObject<Texture>(new AssetId("87540190-ab97-4b4e-b3c2-d57d2fbb1ff3"), "StrideEnvironmentLightingDFGLUT8");
            context.Parameters.Set(MaterialSpecularMicrofacetEnvironmentGGXLUTKeys.EnvironmentLightingDFG_LUT, texture);

            return new ShaderClassSource("MaterialSpecularMicrofacetEnvironmentGGXLUT");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSpecularMicrofacetEnvironmentGGXLUT;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSpecularMicrofacetEnvironmentGGXLUT).GetHashCode();
        }
    }
}
