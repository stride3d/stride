// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
    /// A transparent blend material.
    /// </summary>
    [DataContract("MaterialTransparencyBlendFeature")]
    [Display("Blend")]
    public class MaterialTransparencyBlendFeature : MaterialFeature, IMaterialTransparencyFeature
    {
        public const int ShadingColorAlphaFinalCallbackOrder = MaterialGeneratorContext.DefaultFinalCallbackOrder;

        // Alpha a volume is clamped to, so a fully opaque one still has a finite absorption
        private const float MaxVolumeAlpha = 0.999f;

        // The pass that draws a volume's back faces, after its front faces
        internal const int VolumeBackFacePass = 1;

        // Body passes, front then back faces; an absorbing body's surface comes after
        private const int VolumeBodyPassCount = 2;

        private static readonly MaterialStreamDescriptor AlphaBlendStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend", "matDiffuseSpecularAlphaBlend", MaterialKeys.DiffuseSpecularAlphaBlendValue.PropertyType);

        private static readonly MaterialStreamDescriptor AlphaBlendColorStream = new MaterialStreamDescriptor("DiffuseSpecularAlphaBlend - Color", "matAlphaBlendColor", MaterialKeys.AlphaBlendColorValue.PropertyType);

        // The pass the final callback was added to: tags outlive passes, and each pass needs its own
        private static readonly PropertyKey<MaterialPass> FinalCallbackPass = new PropertyKey<MaterialPass>("MaterialTransparencyBlendFeature.FinalCallbackPass", typeof(MaterialTransparencyBlendFeature));
    
        /// <summary>
        /// Initializes a new instance of the <see cref="MaterialTransparencyBlendFeature"/> class.
        /// </summary>
        public MaterialTransparencyBlendFeature()
        {
            Alpha = new ComputeFloat(1f);
            Tint = new ComputeColor(Color.White);
        }
    
        /// <summary>
        /// Gets or sets the alpha.
        /// </summary>
        /// <value>The alpha.</value>
        /// <userdoc>An additional factor that can be used to modulate original alpha of the material.</userdoc>
        [NotNull]
        [DataMember(10)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public IComputeScalar Alpha { get; set; }

        /// <summary>
        /// Gets or sets the tint color.
        /// </summary>
        /// <value>The tint.</value>
        /// <userdoc>The tint color to apply on the material during the blend.</userdoc>
        [NotNull]
        [DataMember(20)]
        public IComputeColor Tint { get; set; }

        /// <userdoc>
        /// Dither shadows cast by this object to simulate semi-transparent shadows, works best at higher PCF filtering levels.
        /// </userdoc>
        [DataMember(30)]
        public bool DitheredShadows { get; set; } = true;

        /// <summary>
        /// Gets or sets the thickness, in world units, that is as opaque as <see cref="Alpha"/>; 0 keeps the same opacity whatever the thickness.
        /// </summary>
        /// <remarks>
        /// The mesh must be closed; the volume ends where it goes into the opaque scene. Needs a <c>VolumeThicknessRenderStage</c>
        /// on the forward renderer; without one the material blends with its alpha as usual.
        /// </remarks>
        /// <userdoc>How thick the material is when it is as opaque as its alpha. Thicker parts get more opaque, thinner parts clearer. 0 turns it off. The mesh must be closed.</userdoc>
        [DataMember(40)]
        [DataMemberRange(0.0, 3)]
        public float OpacityThickness { get; set; }

        /// <summary>
        /// Gets or sets how the material's body affects what is seen through it when <see cref="OpacityThickness"/> is set.
        /// </summary>
        /// <userdoc>Absorbing filters what is behind through the tint, like tinted glass. Scattering fades it to the material's lit colour, like water or fog.</userdoc>
        [DataMember(50)]
        public MaterialVolumeMedium Medium { get; set; }

        /// <summary>
        /// Gets or sets the density of the medium at each point of the volume, as a factor of its opacity; uniform when null.
        /// </summary>
        /// <remarks>Evaluated with <c>streams.PositionWS</c> at points along each pixel's ray through the volume, for noise, gradients or animated gas.</remarks>
        /// <userdoc>How dense the medium is at each point of the volume, as a factor of its opacity: a texture, a value or a shader of the world position. Empty means the same density everywhere.</userdoc>
        [DataMember(55)]
        [DefaultValue(null)]
        public IComputeScalar Density { get; set; }

        public override void MultipassGeneration(MaterialGeneratorContext context)
        {
            // The body's front faces, then its back faces (only drawn with the camera inside), then an absorbing body's surface
            if (OpacityThickness > 0)
                context.SetMultiplePasses("Volume", Medium == MaterialVolumeMedium.Absorbing ? 3 : 2);
        }

        public override void GenerateShader(MaterialGeneratorContext context)
        {
            var alpha = Alpha ?? new ComputeFloat(1f);
            var tint = Tint ?? new ComputeColor(Color.White);

            alpha.ClampFloat(0, 1);

            // Use pre-multiplied alpha to support both additive and alpha blending
            if (context.MaterialPass.BlendState == null)
                context.MaterialPass.BlendState = BlendStates.AlphaBlend;
            context.MaterialPass.HasTransparency = true;
            // Disable alpha-to-coverage. We wanna do alpha blending, not alpha testing.
            context.MaterialPass.AlphaToCoverage = false;
            // TODO GRAPHICS REFACTOR
            //context.Parameters.SetResourceSlow(Effect.BlendStateKey, BlendState.NewFake(blendDesc));

            context.SetStream(AlphaBlendStream.Stream, alpha, MaterialKeys.DiffuseSpecularAlphaBlendMap, MaterialKeys.DiffuseSpecularAlphaBlendValue, Color.White);
            context.SetStream(AlphaBlendColorStream.Stream, tint, MaterialKeys.AlphaBlendColorMap, MaterialKeys.AlphaBlendColorValue, Color.White);

            if (OpacityThickness > 0)
            {
                // The alpha is the opacity of OpacityThickness units: absorption = -ln(1 - alpha) / thickness
                var alphaValue = alpha is ComputeFloat constantAlpha ? MathUtil.Clamp(constantAlpha.Value, 0f, MaxVolumeAlpha) : 0.5f;
                var tintValue = tint is ComputeColor constantTint ? (Color3)constantTint.Value.ToColorSpace(context.ColorSpace) : new Color3(1f);
                context.MaterialPass.Parameters.Set(MaterialVolumeKeys.Absorption, -MathF.Log(1f - alphaValue) / OpacityThickness);
                // Each channel lets 1 - alpha (1 - tint) through per OpacityThickness, so thicknesses multiply exactly
                var greyAbsorption = MathF.Log(1f - alphaValue);
                var channelAbsorption = alphaValue > 0
                    ? new Color3(MathF.Log(1f - alphaValue * (1f - tintValue.R)), MathF.Log(1f - alphaValue * (1f - tintValue.G)), MathF.Log(1f - alphaValue * (1f - tintValue.B))) * (1f / greyAbsorption)
                    : new Color3(1f) - tintValue;
                context.MaterialPass.Parameters.Set(MaterialVolumeKeys.ChannelAbsorption, channelAbsorption);
                context.MaterialPass.Parameters.Set(MaterialVolumeKeys.Scattering, Medium == MaterialVolumeMedium.Scattering ? 1f : 0f);

                if (context.PassIndex < VolumeBodyPassCount)
                    context.MaterialPass.CullMode = context.PassIndex == VolumeBackFacePass ? CullMode.Front : CullMode.Back;

                // The body passes average the density along their segment; an absorbing body's surface pass needs none
                if (Density != null && context.PassIndex < VolumeBodyPassCount)
                {
                    var density = new ShaderMixinSource();
                    density.Mixins.Add(new ShaderClassSource("MaterialSurfaceVolumeDensity"));
                    density.AddComposition("densityMap", Density.GenerateShaderSource(context, new MaterialComputeColorKeys(MaterialVolumeKeys.DensityMap, MaterialVolumeKeys.DensityValue, Color.White)));
                    context.AddShaderSource(MaterialShaderStage.Pixel, density);
                }
                else
                {
                    context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceVolumeUniformDensity"));
                }

                if (Medium == MaterialVolumeMedium.Absorbing && context.PassIndex < VolumeBodyPassCount)
                {
                    // The body multiplies what is behind it, unlit
                    var blendState = new BlendStateDescription(Blend.Zero, Blend.SourceColor);
                    blendState.RenderTargets[0].AlphaSourceBlend = Blend.Zero;
                    blendState.RenderTargets[0].AlphaDestinationBlend = Blend.One;
                    context.MaterialPass.BlendState = blendState;
                    context.AddFinalCallback(MaterialShaderStage.Pixel, AddVolumeTransmittance, ShadingColorAlphaFinalCallbackOrder + 1);
                }
                else
                {
                    context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceVolumeAlpha"));
                }
            }

            context.MaterialPass.Parameters.Set(MaterialKeys.UsePixelShaderWithDepthPass, true);
            if (DitheredShadows)
            {
                context.MaterialPass.Parameters.Set(MaterialKeys.UseDitheredShadows, true);
            }
            
            if (context.Tags.Get(FinalCallbackPass) != context.MaterialPass)
            {
                context.Tags.Set(FinalCallbackPass, context.MaterialPass);
                context.AddFinalCallback(MaterialShaderStage.Pixel, AddDiffuseSpecularAlphaBlendColor, ShadingColorAlphaFinalCallbackOrder);
            }
        }
    
        private void AddDiffuseSpecularAlphaBlendColor(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddShaderSource(MaterialShaderStage.Pixel, new ShaderClassSource("MaterialSurfaceDiffuseSpecularAlphaBlendColor"));
        }

        private static void AddVolumeTransmittance(MaterialShaderStage stage, MaterialGeneratorContext context)
        {
            context.AddShaderSource(stage, new ShaderClassSource("MaterialSurfaceVolumeTransmittance"));
            context.MaterialPass.IsLightDependent = false;
        }
    }
}
