// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Fresnel using Schlick approximation.
    /// </summary>
    [DataContract("MaterialSpecularMicrofacetFresnelSchlick")]
    [Display("Schlick")]
    public class MaterialSpecularMicrofacetFresnelSchlick : IMaterialSpecularMicrofacetFresnelFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialSpecularMicrofacetFresnelSchlick");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSpecularMicrofacetFresnelSchlick;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSpecularMicrofacetFresnelSchlick).GetHashCode();
        }
    }
}
