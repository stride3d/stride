// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    /// <summary>
    /// Applies no light attenuation.
    /// </summary>
    [DataContract("MaterialHairLightAttenuationFunctionNone")]
    [Display("None")]
    public class MaterialHairLightAttenuationFunctionNone : IMaterialHairLightAttenuationFunction
    {
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            return new ShaderClassSource("MaterialHairLightAttenuationFunctionNone");
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialHairLightAttenuationFunctionNone;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialHairLightAttenuationFunctionNone).GetHashCode();
        }
    }
}
