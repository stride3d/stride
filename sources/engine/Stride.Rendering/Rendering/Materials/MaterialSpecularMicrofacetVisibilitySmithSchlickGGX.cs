// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Schlick-GGX Geometric Shadowing.
    /// </summary>
    [DataContract("MaterialSpecularMicrofacetVisibilitySmithSchlickGGX")]
    [Display("Schlick-GGX")]
    public class MaterialSpecularMicrofacetVisibilitySmithSchlickGGX : IMaterialSpecularMicrofacetVisibilityFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialSpecularMicrofacetVisibilitySmithSchlickGGX");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSpecularMicrofacetVisibilitySmithSchlickGGX;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSpecularMicrofacetVisibilitySmithSchlickGGX).GetHashCode();
        }
    }
}
