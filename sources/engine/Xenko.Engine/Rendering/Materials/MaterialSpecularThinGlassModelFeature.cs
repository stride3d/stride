using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Materials
{
    [DataContract("MaterialSpecularThinGlassModelFeature")]
    [Display("Glass")]
    public class MaterialSpecularThinGlassModelFeature : MaterialSpecularMicrofacetModelFeature, IEquatable<MaterialSpecularThinGlassModelFeature>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialSpecularThinGlassModelFeature"/> class.
        /// </summary>
        public MaterialSpecularThinGlassModelFeature()
        {
            // Different default value for fresnel and environment
            Fresnel = new MaterialSpecularMicrofacetFresnelThinGlass();
            Environment = new MaterialSpecularMicrofacetEnvironmentThinGlass();
        }

        /// <summary>
        /// Gets or sets the refractive index of the material.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>An additional factor that can be used to modulate original alpha of the material.</userdoc>
        [DataMember(2)]
        [DataMemberRange(1.0, 5.0, 0.01, 0.1, 3)]
        public float RefractiveIndex { get; set; } = 1.52f;

        public override void MultipassGeneration(MaterialGeneratorContext context)
        {
            // Glass rendering is done in 2 passes:
            // - Transmittance evaluation to darken background (with alpha blend)
            // - Reflectance evaluation (with additive blending)
            int passCount = 2;
            if (context.CurrentMaterialDescriptor.Attributes.CullMode == CullMode.None)
                passCount *= 2; // This needs both front and back (in the following order: back[pass0,pass1], front[pass0,pass1])
            context.SetMultiplePasses("Glass", passCount);
        }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            base.GenerateShader(context);

            var attributes = context.CurrentMaterialDescriptor.Attributes;

            int glassPassIndex = context.PassIndex % 2;
            if (attributes.CullMode == CullMode.None)
                context.MaterialPass.CullMode = context.PassIndex < 2 ? CullMode.Back : CullMode.Front;
            else
                context.MaterialPass.CullMode = attributes.CullMode;
            
            // Compute transmittance
            context.GetShading(this).LightDependentExtraModels.Add(new ShaderClassSource("MaterialTransmittanceReflectanceStream"));

            context.Parameters.Set(MaterialTransmittanceReflectanceStreamKeys.RefractiveIndex, RefractiveIndex);
            context.MaterialPass.HasTransparency = true;
            if (glassPassIndex == 0)
            {
                // Transmittance pass
                context.MaterialPass.BlendState = new BlendStateDescription(Blend.Zero, Blend.SourceColor) { RenderTarget0 = { AlphaSourceBlend = Blend.One, AlphaDestinationBlend = Blend.Zero } };

                // Shader output is matTransmittance
                // Note: we make sure to run after MaterialTransparencyBlendFeature so that shadingColorAlpha is fully updated
                context.AddFinalCallback(MaterialShaderStage.Pixel, AddMaterialSurfaceTransmittanceShading, MaterialTransparencyBlendFeature.ShadingColorAlphaFinalCallbackOrder + 1);
            }
            else if (glassPassIndex == 1)
            {
                // Reflectance pass
                context.MaterialPass.BlendState = BlendStates.Additive;
            }
        }

        private void AddMaterialSurfaceTransmittanceShading(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddShaderSource(stage, new ShaderClassSource("MaterialSurfaceTransmittanceShading"));
        }

        public bool Equals(MaterialSpecularThinGlassModelFeature other) => base.Equals(other);
    }
}