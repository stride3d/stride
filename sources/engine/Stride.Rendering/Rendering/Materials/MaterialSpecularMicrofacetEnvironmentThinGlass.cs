// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
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
    [DataContract("MaterialSpecularMicrofacetEnvironmentThinGlass")]
    [Display("Thin Glass")]
    public class MaterialSpecularMicrofacetEnvironmentThinGlass : IMaterialSpecularMicrofacetEnvironmentFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialSpecularMicrofacetEnvironmentThinGlass");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSpecularMicrofacetEnvironmentThinGlass;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSpecularMicrofacetEnvironmentThinGlass).GetHashCode();
        }
    }
}
