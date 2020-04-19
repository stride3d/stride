// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Trades traditional shadow mapping for thickness attenuated shadowing to fake scattering.
    /// </summary>
    /// <userdoc>
    /// Light will be scattered in the hair. 
    /// You can control the scattering falloff with the "Extinction strength" parameter.
    /// Keep in mind that any shadow will create partial occlusion, even shadows cast from opaque objects onto the hair.
    /// </userdoc>
    [DataContract("MaterialHairShadowingFunctionScattering")]
    [Display("Scattering")]
    public class MaterialHairShadowingFunctionScattering : IMaterialHairShadowingFunction
    {
        /// <summary>
        /// Controls how strong the scattering falloff is.
        /// </summary>
        /// <userdoc>
        /// Controls how strong the material absorbs the light.
        /// A higher value results in a "faster" falloff.
        /// </userdoc>
        [DataMember(200)]
        [DataMemberRange(0.01, 1000.0, 1, 2, 2)] // A minimum value of 0.01 avoids NAN from "pow(x, y)" in the shader when "x == 0.0" and "y == 0.0".
        [Display("Extinction strength")]
        public float ExtinctionStrength { get; set; } = 15.0f;

        private static readonly ValueParameterKey<float> ExtinctionStrengthKey = ParameterKeys.NewValue<float>();

        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            // Generate a unique key because this mixin can be used more than once per shader and we want their constants to be independent.
            var uniqueExtinctionStrengthKey = (ValueParameterKey<float>)context.GetParameterKey(ExtinctionStrengthKey);

            context.MaterialPass.Parameters.Set(uniqueExtinctionStrengthKey, ExtinctionStrength);

            return new ShaderClassSource("MaterialHairShadowingFunctionScattering", uniqueExtinctionStrengthKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialHairShadowingFunctionScattering;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialHairShadowingFunctionScattering).GetHashCode();
        }
    }
}
