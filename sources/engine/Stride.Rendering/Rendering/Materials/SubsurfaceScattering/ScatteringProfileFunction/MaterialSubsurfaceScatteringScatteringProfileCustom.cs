// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Materials.ComputeColors;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Calculates a custom scattering profile, which is applied during the forward pass using the subsurface scattering shading model.
    /// 
    /// ATTENTION: This class is subject to change because it does not yet give you full control over generating scattering kernels and profiles.
    /// </summary>
    [Display("Custom (Skin based)")]
    [DataContract("MaterialSubsurfaceScatteringScatteringProfileCustom")]
    public class MaterialSubsurfaceScatteringScatteringProfileCustom : IMaterialSubsurfaceScatteringScatteringProfile
    {
        private static readonly ObjectParameterKey<Texture> FalloffTexture = ParameterKeys.NewObject<Texture>();
        private static readonly ValueParameterKey<Color4> FalloffValue = ParameterKeys.NewValue<Color4>();
        private static readonly Color DefaultProfileColor = new Color(1.0f, 0.5f, 0.2f, 1.0f);

        public MaterialSubsurfaceScatteringScatteringProfileCustom()
        {
            FalloffMap = new ComputeColor(Color.Red);
        }

        /// <summary>
        /// This parameter defines the per-channel falloff of the gradients produced by the subsurface scattering events.
        /// It can be used to fine tune the color of the gradients.
        /// </summary>
        /// <userdoc>
        /// Attention: This parameter only affects the SSS post-process. If you want to change the shading, use a different scattering profile. 
        /// This parameter defines the per-channel falloff of the gradients produced by the subsurface scattering events.
        /// It can be used to fine tune the color of the gradients.
        /// 
        /// Note: The alpha channel is ignored.
        /// </userdoc>
        [DataMember(10)]
        [Display("Falloff map")]
        [NotNull]
        // TODO: Make the editor only display RGB, not RGBA.
        public IComputeColor FalloffMap { get; set; } = new ComputeColor(new Color4(1.0f, 0.37f, 0.3f, 1.0f));    // Default falloff for skin.
        
        public ShaderSource Generate(MaterialGeneratorContext context)
        {
            var shaderSource = new ShaderMixinSource();

            MaterialComputeColorKeys materialComputeColorKeys = new MaterialComputeColorKeys(FalloffTexture, FalloffValue, DefaultProfileColor);
            
            // Add the shader for computing the transmittance using the custom scattering profile:
            if (FalloffMap is ComputeTextureColor ||
                FalloffMap is ComputeBinaryColor ||
                FalloffMap is ComputeShaderClassColor ||
                FalloffMap is ComputeVertexStreamColor)
            {
                var computeColorSource = FalloffMap.GenerateShaderSource(context, materialComputeColorKeys);
                shaderSource.AddComposition("FalloffMap", computeColorSource);

                // Use the expensive pixel shader because the scattering falloff can vary per pixel because we're using a texture:
                shaderSource.Mixins.Add(new ShaderClassSource("MaterialSubsurfaceScatteringScatteringProfileCustomVarying"));
            }
            else
            {
                Vector3 falloff = new Vector3(1.0f);

                ComputeColor falloffComputeColor = FalloffMap as ComputeColor;
                if (falloffComputeColor != null)
                {
                    falloff.X = falloffComputeColor.Value.R;
                    falloff.Y = falloffComputeColor.Value.G;
                    falloff.Z = falloffComputeColor.Value.B;
                }

                ComputeFloat4 falloffComputeFloat4 = FalloffMap as ComputeFloat4;
                if (falloffComputeFloat4 != null)
                {
                    falloff.X = falloffComputeFloat4.Value.X;
                    falloff.Y = falloffComputeFloat4.Value.Y;
                    falloff.Z = falloffComputeFloat4.Value.Z;
                }

                // Use the precomputed pixel shader because the scattering falloff is constant across pixels because we're using a texture:
                Vector4[] scatteringProfile = SubsurfaceScatteringKernelGenerator.CalculateTransmittanceProfile(falloff);   // Applied during forward pass.

                context.MaterialPass.Parameters.Set(MaterialSubsurfaceScatteringScatteringProfileCustomUniformKeys.ScatteringProfile, scatteringProfile);

                shaderSource.Mixins.Add(new ShaderClassSource("MaterialSubsurfaceScatteringScatteringProfileCustomUniform"));
            }

            return shaderSource;
        }

        protected bool Equals(MaterialSubsurfaceScatteringScatteringProfileCustom other)
        {
            return FalloffMap.Equals(other.FalloffMap);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((MaterialSubsurfaceScatteringScatteringProfileCustom)obj);
        }

        public override int GetHashCode()
        {
            return FalloffMap.GetHashCode();
        }
    }
}
