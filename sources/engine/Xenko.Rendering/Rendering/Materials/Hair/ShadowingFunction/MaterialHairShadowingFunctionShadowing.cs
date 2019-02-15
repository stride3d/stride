// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Shadows the hair using traditional shadow mapping.
    /// The hair will be treated as an opaque surface.
    /// </summary>
    /// <userdoc>
    /// Shadows the hair using traditional shadow mapping.
    /// The hair will be treated as an opaque surface.
    /// </userdoc>
    [DataContract("MaterialHairShadowingFunctionShadowing")]
    [Display("Shadowing")]
    public class MaterialHairShadowingFunctionShadowing : IMaterialHairShadowingFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialHairShadowingFunctionShadowing");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialHairShadowingFunctionShadowing;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialHairShadowingFunctionShadowing).GetHashCode();
        }
    }
}
