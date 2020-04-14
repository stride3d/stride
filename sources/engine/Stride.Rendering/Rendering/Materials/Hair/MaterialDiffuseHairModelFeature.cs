// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// The diffuse subsurface scattering for the diffuse material model attribute.
    /// </summary>
    [DataContract("MaterialDiffuseHairModelFeature")]
    [Display("Hair")]
    public class MaterialDiffuseHairModelFeature : MaterialFeature, IMaterialDiffuseModelFeature
    {
        public bool IsLightDependent
        {
            get
            {
                return true;
            }
        }

        [DataMemberIgnore]
        internal bool IsEnergyConservative { get; set; }

        /// <summary>
        /// Enables/disables color coding of each render pass.
        /// </summary>
        /// <userdoc>
        /// Enables/disables color coding of each render pass.
        /// The opaque pixels get colored in red.
        /// The transparent back-face pixels get colored in green.
        /// The transparent front-face pixels get colored in blue.
        /// </userdoc>
        [DataMember(5)]
        [DefaultValue(false)]
        [Display("Debug render passes")]
        public bool DebugRenderPasses { get; set; } = false;

        /// <summary>
        /// The shading model to use for the hair shading.
        /// </summary>
        /// <userdoc>
        /// The shading model to use for the hair shading.
        /// "Scheuermann approximation" offsers the best performance, but worst quality.
        /// "Scheuermann improved" offers better quality at slightly worse performance.
        /// "Kajiya-Kay shifted" offers the best quality at the lowest performance.
        /// </userdoc>
        [DataMember(10)]
        [DefaultValue(HairShared.HairShadingModel.KajiyaKayShifted)]
        [Display("Shading model")]
        public HairShared.HairShadingModel ShadingModel { get; set; } = HairShared.HairShadingModel.KajiyaKayShifted;

        /// <summary>
        /// Defines whether the tangent or bitangent vectors represent the hair strands direction.
        /// </summary>
        /// <userdoc>
        /// This option depends on how the 3D model was authored.
        /// If your 3D model's tangent vectors are aligned with the direction of hair flow, select "Tangent".
        /// If the binormal/bitangent vectors are aligned with the hair flow instead, select "Bitangent".
        /// </userdoc>
        [NotNull]
        [DataMember(20)]
        [Display("Hair direction")]
        public IMaterialHairDirectionFunction HairDirectionFunction { get; set; } = new MaterialHairDirectionFunctionBitangent();

        /// <summary>
        /// Defines whether to use traditional shadow mapping or subsurface scattering for shadowing the hair.
        /// </summary>
        /// <userdoc>
        /// Defines whether to use traditional shadow mapping or subsurface scattering for shadowing the hair.
        /// Subsurface scattering makes the hair partially translucent, allowing light to pass through the hair mesh.
        /// </userdoc>
        [NotNull]
        [DataMember(181)]
        [Display("Shadowing type")]
        public IMaterialHairShadowingFunction HairShadowingFunction { get; set; } = new MaterialHairShadowingFunctionShadowing();

        /// <summary>
        /// Additional light attenuation.
        /// </summary>
        /// <userdoc>
        /// Additional light attenuation.
        /// Select "Directional" for attenuation based on the surface normals.
        /// Select "None" for no attenuation.
        /// </userdoc>
        [NotNull]
        [DataMember(70)]
        [Display("Light attenuation")]
        public IMaterialHairLightAttenuationFunction LightAttenuationFunction { get; set; } = new MaterialHairLightAttenuationFunctionDirectional();

        /// <summary>
        /// Defines the value used in the alpha test.
        /// </summary>
        /// <userdoc>
        /// Any alpha value above this value is considered opaque.
        /// ATTENTION: Make sure that the value defined in the specular hair shading model is the same as this one!
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(0.99f)]
        [DataMemberRange(0.0, 1.0, 1, 2, 2)]
        [Display("Alpha threshold")]
        public float AlphaThreshold { get; set; } = 0.99f;

        /*
        public override void MultipassGeneration(MaterialGeneratorContext context)  // Sadly we can't have this both in the diffuse and specular models.
        {
            context.SetMultiplePasses("Hair", 3);
        }
        */

        private static readonly ValueParameterKey<float> HairAlphaThresholdKey = ParameterKeys.NewValue<float>();

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var shaderSource = new ShaderMixinSource();
            shaderSource.Mixins.Add(new ShaderClassSource("MaterialSurfaceShadingDiffuseHair", IsEnergyConservative, (int)ShadingModel, DebugRenderPasses));

            shaderSource.AddComposition("hairLightAttenuationFunction", LightAttenuationFunction.Generate(context));
            shaderSource.AddComposition("hairDirectionFunction", HairDirectionFunction.Generate(context));
            shaderSource.AddComposition("hairShadowingFunction", HairShadowingFunction.Generate(context));

            HairShared.SetMaterialPassParameters(context, shaderSource, AlphaThreshold);    // Set the rendering parameters and generate the pass-dependent compositions.
            
            context.Parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true); // Indicates that material requries using the pixel shader stage during the depth-only pass (Z prepass or shadow map rendering).

            if (DebugRenderPasses)
            {
                context.Parameters.Set(MaterialHairSharedKeys.PassID, context.PassIndex);   // For debugging the different hair passes.
            }

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.LightDependentSurface = shaderSource;
        }

        protected bool Equals(MaterialDiffuseHairModelFeature other)
        {
            return IsEnergyConservative == other.IsEnergyConservative && ShadingModel == other.ShadingModel && HairDirectionFunction.Equals(other.HairDirectionFunction) && HairShadowingFunction.Equals(other.HairShadowingFunction) && LightAttenuationFunction.Equals(other.LightAttenuationFunction) && AlphaThreshold.Equals(other.AlphaThreshold);
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return Equals(other as MaterialDiffuseHairModelFeature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MaterialDiffuseHairModelFeature)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsEnergyConservative.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)ShadingModel;
                hashCode = (hashCode * 397) ^ HairDirectionFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ HairShadowingFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ LightAttenuationFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ AlphaThreshold.GetHashCode();
                return hashCode;
            }
        }
    }
}
