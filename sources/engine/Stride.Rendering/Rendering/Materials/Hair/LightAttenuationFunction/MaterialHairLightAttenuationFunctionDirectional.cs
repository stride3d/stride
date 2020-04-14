// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Applies light attenuation with configurable hardness.
    /// </summary>
    [DataContract("MaterialHairLightAttenuationFunctionDirectional")]
    [Display("Directional")]
    public class MaterialHairLightAttenuationFunctionDirectional : IMaterialHairLightAttenuationFunction
    {
        /// <summary>
        /// Defines the exponent used in the shader.
        /// </summary>
        /// <userdoc>
        /// Defines the hardness of the transition from shadowed to unshadowed areas.
        /// </userdoc>
        [DataMember(10)]
        [DataMemberRange(0.01, 64.0, 0.01, 1.0, 2)] // A minimum value of 0.01 avoids NAN from "pow(x, y)" in the shader when "x == 0.0" and "y == 0.0".
        [Display("Hardness")]
        public float Hardness { get; set; } = 1.0f;

        /// <summary>
        /// The interpolation factor used for shifting the boundary between lit and unlit areas.
        /// </summary>
        /// <userdoc>
        /// Use this parameter to shift the boundary between lit and unlit areas in order to mitigate shadow mapping artifacts.
        /// </userdoc>
        [DataMember(20)]
        [DataMemberRange(0.0, 0.5, 0.01, 0.1, 2)]
        [Display("Shadow boundary shift")]
        public float BoundaryShift { get; set; } = 0.0f;

        // These must match with the values defined in "MaterialHairLightAttenuationFunctionDirectional.sdsl"!
        public enum NormalMode
        {
            [Display("Mesh normals")]
            Mesh = 0,
            [Display("Normal map normals")]
            NormalMap = 1,
            [Display("Mesh & normal map normals")]
            MeshAndNormalMap = 2,
        }

        /// <summary>
        /// The normals to use for the light attenuation computation.
        /// </summary>
        /// <userdoc>
        /// Defines the types of normals to use for computing the directional attenuation.
        /// Mesh normals: Use the mesh normals.
        /// Normal map normals: Use the normal map values (falls back to mesh normals if no normal map is present).
        /// Mesh & normal map normals: Use the normal map and mesh normals. This can fix artifacts on edges between lit and unlit areas (falls back to mesh normals if no normal map is present).
        /// </userdoc>
        [DataMember(20)]
        [Display("Normals to use")]
        public NormalMode SelectedNormalMode { get; set; } = NormalMode.MeshAndNormalMap;

        private static readonly ValueParameterKey<float> HardnessReciprocalParameterKey = ParameterKeys.NewValue<float>();
        private static readonly ValueParameterKey<float> BoundaryShiftParameterKey = ParameterKeys.NewValue<float>();

        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            // Generate unique keys because this mixin can be used more than once per shader and we want their constants to be independent.
            var uniqueHardnessReciprocalKey = (ValueParameterKey<float>)context.GetParameterKey(HardnessReciprocalParameterKey);
            var uniqueBoundaryShiftKey = (ValueParameterKey<float>)context.GetParameterKey(BoundaryShiftParameterKey);

            context.Parameters.Set(uniqueHardnessReciprocalKey, 1.0f / Hardness);
            context.Parameters.Set(uniqueBoundaryShiftKey, BoundaryShift);

            return new ShaderClassSource("MaterialHairLightAttenuationFunctionDirectional", (int)SelectedNormalMode, uniqueHardnessReciprocalKey, uniqueBoundaryShiftKey);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialHairLightAttenuationFunctionDirectional;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialHairLightAttenuationFunctionDirectional).GetHashCode();
        }
    }
}
