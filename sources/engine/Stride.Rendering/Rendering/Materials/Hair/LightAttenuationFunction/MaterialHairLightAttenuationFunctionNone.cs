// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Materials
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
