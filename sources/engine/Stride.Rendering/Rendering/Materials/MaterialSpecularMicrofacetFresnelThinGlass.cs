// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Fresnel for glass materials.
    /// </summary>
    [DataContract("MaterialSpecularMicrofacetFresnelThinGlass")]
    [Display("Thin Glass")]
    public class MaterialSpecularMicrofacetFresnelThinGlass : IMaterialSpecularMicrofacetFresnelFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialSpecularMicrofacetFresnelThinGlass");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSpecularMicrofacetFresnelThinGlass;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSpecularMicrofacetFresnelThinGlass).GetHashCode();
        }
    }
}
