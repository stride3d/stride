// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// The microfacet specular shading model.
    /// </summary>
    [DataContract("MaterialSpecularHairModelFeature")]
    [Display("Hair")]
    public class MaterialSpecularHairModelFeature : MaterialFeature, IMaterialSpecularModelFeature, IEquatable<MaterialSpecularHairModelFeature>
    {
        private static readonly ObjectParameterKey<Texture> PrimarySpecularReflectionNoiseTexture = ParameterKeys.NewObject<Texture>();
        private static readonly ValueParameterKey<float> PrimarySpecularReflectionNoiseValue = ParameterKeys.NewValue<float>();

        private static readonly ObjectParameterKey<Texture> SecondarySpecularReflectionNoiseTexture = ParameterKeys.NewObject<Texture>();
        private static readonly ValueParameterKey<float> SecondarySpecularReflectionNoiseValue = ParameterKeys.NewValue<float>();

        public bool IsLightDependent
        {
            get { return true; }
        }

        /// <summary>
        /// Enables/disables color coding of each render pass.
        /// </summary>
        /// <userdoc>
        /// Enables/disables color coding of each render pass.
        /// The opaque pixels get colored in red.
        /// The transparent back-face pixels get colored in green.
        /// The transparent front-face pixels get colored in blue.
        /// </userdoc>
        [DataMember(30)]
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
        [DataMember(40)]
        [DefaultValue(HairShared.HairShadingModel.KajiyaKayShifted)]
        [Display("Shading model")]
        public HairShared.HairShadingModel ShadingModel { get; set; } = HairShared.HairShadingModel.KajiyaKayShifted;

        /// <summary>
        /// Exponent of the primary specular highlight.
        /// </summary>
        /// <userdoc>
        /// You can use this parameter to control the size and hardness of the primary specular highlight.
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(100.0f)]
        [DataMemberRange(1.0, 1000.0, 1, 2, 2)]
        [Display("Primary specular reflection exponent")]
        public float SpecularExponent1 { get; set; } = 100.0f;

        /// <summary>
        /// Color of the primary specular reflecton.
        /// </summary>
        /// <userdoc>
        /// Even though the primary specular highlight is theoretically white, you can modify its color for artistic purposes.
        /// </userdoc>
        [DataMember(80)]
        [Display("Primary specular reflection color")]
        public Color3 SpecularColor1 { get; set; } = new Color3(1.0f, 1.0f, 1.0f); // TODO: Use IComputeColor instead?

        /// <summary>
        /// Strength of the primary specular reflection.
        /// </summary>
        /// <userdoc>
        /// You can use this parameter to control the strength of the primary specular highlight.
        /// </userdoc>
        [DataMember(90)]
        [DefaultValue(0.05f)]
        [DataMemberRange(0.0, 1.0, 1, 2, 3)]
        [Display("Primary specular reflection strength")]
        public float SpecularScale1 { get; set; } = 0.05f;

        /// <summary>
        /// Exponent of the secondary specular highlight.
        /// </summary>
        /// <userdoc>
        /// You can use this parameter to control the size and hardness of the secondary specular highlight.
        /// </userdoc>
        [DataMember(100)]
        [DefaultValue(10.0f)]
        [DataMemberRange(1.0, 1000.0, 1, 2, 2)]
        [Display("Secondary specular reflection exponent")]
        public float SpecularExponent2 { get; set; } = 10.0f;

        /// <summary>
        /// Color of the secondary specular reflecton.
        /// </summary>
        /// <userdoc>
        /// The secondary specular highlight has a color that should depend on the hair color.
        /// </userdoc>
        [DataMember(110)]
        [Display("Secondary specular reflection color")]
        public Color3 SpecularColor2 { get; set; } = new Color3(1.0f, 1.0f, 1.0f); // TODO: Use IComputeColor instead?

        /// <summary>
        /// Strength of the secondary specular reflection.
        /// </summary>
        /// <userdoc>
        /// You can use this parameter to control the strength of the secondary specular highlight.
        /// </userdoc>
        [DataMember(120)]
        [DefaultValue(1.0f)]
        [DataMemberRange(0.0, 1.0, 1, 2, 3)]
        [Display("Secondary specular reflection strength")]
        public float SpecularScale2 { get; set; } = 1.0f;

        /// <summary>
        /// Controls the offset between the primary and secondary reflections.
        /// </summary>
        /// <userdoc>
        /// This is the ratio between the angle of the orientation of the primary and secondary specular highlights.
        // The theoretical value is 1.5, but you can change it for more artistic control. With this parameter you can change the position of the secondary highlight.
        /// </userdoc>
        [DataMember(125)]
        [DefaultValue(1.5f)]
        [DataMemberRange(0.0, 3.0, 1, 2, 2)]
        [Display("Secondary reflection shift ratio")]
        public float SpecularShiftRatio { get; set; } = 1.5f;

        /// <summary>
        /// Controls the shift of the primary and secondary specular reflections.
        /// </summary>
        /// <userdoc>
        /// The highlights are shifted because of the angle of the hair scales. You can control this angle (real-life values are between 5~10 degrees) to vary the position of the highlights.
        /// </userdoc>
        [DataMember(127)]
        [DefaultValue(7.0f)]
        [Display("Hair scales angle")]
        [DataMemberRange(0.0, 25.0, 1, 2, 2)]
        public float ScalesAngle { get; set; } = 7.0f;

        /// <summary>
        /// Scale that gets multiplied with the noise value produced by HairSpecularHighlightsShiftNoise.
        /// </summary>
        /// <userdoc>
        /// Use this to control how much the specular shift texture should shift the specular reflections. 
        /// </userdoc>
        [DataMember(131)]
        [DefaultValue(1.0f)]
        [DataMemberRange(0.0, 10.0, 1, 2, 2)]
        [Display("Shift noise strength")]
        public float ShiftNoiseScale { get; set; } = 1.0f;

        /// <summary>
        /// </summary>
        /// <userdoc>
        /// </userdoc>
        [DataMember(132)]
        [DefaultValue(1.0f)]
        [DataMemberRange(0.0, 1.0, 1, 2, 2)]
        [Display("Glints noise strength")]
        public float GlintsNoiseStrength { get; set; } = 1.0f;

        /// <summary>
        /// Defines the value used in the alpha test.
        /// </summary>
        /// <userdoc>
        /// Any alpha value above this value is considered opaque.
        /// ATTENTION: Make sure that the value defined in the diffuse hair shading model is the same as this one!
        /// </userdoc>
        [DataMember(135)]
        [DefaultValue(0.99f)]
        [DataMemberRange(0.0, 1.0, 1, 2, 2)]
        [Display("Alpha threshold")]
        public float AlphaThreshold { get; set; } = 0.99f;

        /// <summary>
        /// This texture is used to shift the two specular highlights to break the uniform look of the hair.
        /// </summary>
        /// <userdoc>
        /// This texture is used to shift the two specular highlights to break the uniform look of the hair.
        /// </userdoc>
        [NotNull]
        [DataMember(140)]
        [DataMemberCustomSerializer]
        [Display("Specular shift texture")]
        public IComputeScalar HairSpecularHighlightsShiftNoise { get; set; } = new ComputeTextureScalar(); // TODO: Document which texture must be used for this, so there is no confusion like in the Mizuchi documentation.

        /// <summary>
        /// The texure that is multiplied with the secondary specular reflections to give them a sparkling look.
        /// </summary>
        /// <userdoc>
        /// The texure that is multiplied with the secondary specular reflections to give them a sparkling look.
        /// </userdoc>
        [NotNull]
        [DataMember(150)]
        [DataMemberCustomSerializer]
        [Display("Secondary specular noise")]
        public IComputeScalar HairSecondarySpecularGlintsNoise { get; set; } = new ComputeTextureScalar(); // TODO: Document which texture must be used for this, so there is no confusion like in the Mizuchi documentation.

        /// <summary>
        /// Defines whether the tangent or bitangent vectors represent the hair strands direction.
        /// </summary>
        /// <userdoc>
        /// This option depends on how the 3D model was authored.
        /// If your 3D model's tangent vectors are aligned with the direction of hair flow, select "Tangent".
        /// If the binormal/bitangent vectors are aligned with the hair flow instead, select "Bitangent".
        /// </userdoc>
        [NotNull]
        [DataMember(180)]
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
        [DataMember(210)]
        [Display("Light attenuation")]
        public IMaterialHairLightAttenuationFunction LightAttenuationFunction { get; set; } = new MaterialHairLightAttenuationFunctionDirectional();

        /// <userdoc>
        /// Specifies the function to use to calculate the environment DFG term in the micro-facet lighting equation. 
        /// This defines how the material reflects specular cubemaps.
        /// </userdoc>
        [NotNull]
        [DataMember(220)]
        [Display("Environment (DFG)")]
        public IMaterialSpecularMicrofacetEnvironmentFunction Environment { get; set; } = new MaterialSpecularMicrofacetEnvironmentGGXLUT();

        private static readonly ValueParameterKey<float> HairAlphaThresholdKey = ParameterKeys.NewValue<float>();

        public override void MultipassGeneration(MaterialGeneratorContext context)
        {
            context.SetMultiplePasses("Hair", 3);
        }
        
        private void AddSpecularHighlightsShiftNoiseTexture(MaterialGeneratorContext context, ShaderMixinSource shaderSource)
        {
            MaterialComputeColorKeys materialComputeColorKeys = new MaterialComputeColorKeys(PrimarySpecularReflectionNoiseTexture,
                PrimarySpecularReflectionNoiseValue);
            var computeColorSource = HairSpecularHighlightsShiftNoise.GenerateShaderSource(context, materialComputeColorKeys);
            shaderSource.AddComposition("SpecularHighlightsShiftNoiseTexture", computeColorSource);
        }
        private void AddSecondarySpecularGlintsNoiseTexture(MaterialGeneratorContext context, ShaderMixinSource shaderSource)
        {
            MaterialComputeColorKeys materialComputeColorKeys = new MaterialComputeColorKeys(SecondarySpecularReflectionNoiseTexture,
                SecondarySpecularReflectionNoiseValue);
            var computeColorSource = HairSecondarySpecularGlintsNoise.GenerateShaderSource(context, materialComputeColorKeys);
            shaderSource.AddComposition("SecondarySpecularGlintsNoiseTexture", computeColorSource);
        }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            // TODO: Look through the compositions and don't add the discard mixin if it has already been added?
            // TODO: That doesn't seem to work well because the diffuse shader doesn't get recreated when the specular one is being recreated... unless I'm wrong about that.

            var shaderSource = new ShaderMixinSource();
            shaderSource.Mixins.Add(new ShaderClassSource("MaterialSurfaceShadingSpecularHair", (int)ShadingModel, DebugRenderPasses));

            shaderSource.AddComposition("hairLightAttenuationFunction", LightAttenuationFunction.Generate(context));
            shaderSource.AddComposition("hairDirectionFunction", HairDirectionFunction.Generate(context));
            shaderSource.AddComposition("hairShadowingFunction", HairShadowingFunction.Generate(context));
            shaderSource.AddComposition("environmentFunction", Environment.Generate(context));

            AddSpecularHighlightsShiftNoiseTexture(context, shaderSource);
            AddSecondarySpecularGlintsNoiseTexture(context, shaderSource);

            HairShared.SetMaterialPassParameters(context, shaderSource, AlphaThreshold); // Set the rendering parameters and generate the pass-dependent compositions.

            // Set the additional parameters used only in the specular shading model:
            var parameters = context.MaterialPass.Parameters;
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairScalesAngle, MathUtil.DegreesToRadians(ScalesAngle));
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularShiftRatio, SpecularShiftRatio);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularColor1, SpecularColor1);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularColor2, SpecularColor2);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularExponent1, SpecularExponent1);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularExponent2, SpecularExponent2);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularScale1, SpecularScale1);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairSpecularScale2, SpecularScale2);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairShiftNoiseScale, ShiftNoiseScale);
            parameters.Set(MaterialSurfaceShadingSpecularHairKeys.HairGlintsNoiseStrength, GlintsNoiseStrength);
            parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true); // Indicates that the material requries the full pixel shader durin the depth-only passes (Z prepass or shadow map rendering).

            if (DebugRenderPasses)
            {
                parameters.Set(MaterialHairSharedKeys.PassID, context.PassIndex);   // For debugging the different hair passes.
            }

            var shaderBuilder = context.AddShading(this);
            shaderBuilder.LightDependentSurface = shaderSource;
        }

        public bool Equals(IMaterialShadingModelFeature other)
        {
            return Equals(other as MaterialSpecularHairModelFeature);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MaterialSpecularHairModelFeature)obj);
        }

        public bool Equals(MaterialSpecularHairModelFeature other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ShadingModel == other.ShadingModel && SpecularExponent1.Equals(other.SpecularExponent1) && SpecularColor1.Equals(other.SpecularColor1) && SpecularScale1.Equals(other.SpecularScale1) && SpecularExponent2.Equals(other.SpecularExponent2) && SpecularColor2.Equals(other.SpecularColor2) && SpecularScale2.Equals(other.SpecularScale2) && SpecularShiftRatio.Equals(other.SpecularShiftRatio) && ScalesAngle.Equals(other.ScalesAngle) && ShiftNoiseScale.Equals(other.ShiftNoiseScale) && GlintsNoiseStrength.Equals(other.GlintsNoiseStrength) && AlphaThreshold.Equals(other.AlphaThreshold) && HairSpecularHighlightsShiftNoise.Equals(other.HairSpecularHighlightsShiftNoise) && HairSecondarySpecularGlintsNoise.Equals(other.HairSecondarySpecularGlintsNoise) && HairDirectionFunction.Equals(other.HairDirectionFunction) && HairShadowingFunction.Equals(other.HairShadowingFunction) && LightAttenuationFunction.Equals(other.LightAttenuationFunction) && Environment.Equals(other.Environment);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int)ShadingModel;
                hashCode = (hashCode * 397) ^ SpecularExponent1.GetHashCode();
                hashCode = (hashCode * 397) ^ SpecularColor1.GetHashCode();
                hashCode = (hashCode * 397) ^ SpecularScale1.GetHashCode();
                hashCode = (hashCode * 397) ^ SpecularExponent2.GetHashCode();
                hashCode = (hashCode * 397) ^ SpecularColor2.GetHashCode();
                hashCode = (hashCode * 397) ^ SpecularScale2.GetHashCode();
                hashCode = (hashCode * 397) ^ SpecularShiftRatio.GetHashCode();
                hashCode = (hashCode * 397) ^ ScalesAngle.GetHashCode();
                hashCode = (hashCode * 397) ^ ShiftNoiseScale.GetHashCode();
                hashCode = (hashCode * 397) ^ GlintsNoiseStrength.GetHashCode();
                hashCode = (hashCode * 397) ^ AlphaThreshold.GetHashCode();
                hashCode = (hashCode * 397) ^ HairSpecularHighlightsShiftNoise.GetHashCode();
                hashCode = (hashCode * 397) ^ HairSecondarySpecularGlintsNoise.GetHashCode();
                hashCode = (hashCode * 397) ^ HairDirectionFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ HairShadowingFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ LightAttenuationFunction.GetHashCode();
                hashCode = (hashCode * 397) ^ Environment.GetHashCode();
                return hashCode;
            }
        }
    }
}
