// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// The GGX Normal Distribution.
    /// </summary>
    [DataContract("MaterialSpecularMicrofacetNormalDistributionGGX")]
    [Display("GGX")]
    public class MaterialSpecularMicrofacetNormalDistributionGGX : IMaterialSpecularMicrofacetNormalDistributionFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialSpecularMicrofacetNormalDistributionGGX");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSpecularMicrofacetNormalDistributionGGX;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSpecularMicrofacetNormalDistributionGGX).GetHashCode();
        }
    }
}
