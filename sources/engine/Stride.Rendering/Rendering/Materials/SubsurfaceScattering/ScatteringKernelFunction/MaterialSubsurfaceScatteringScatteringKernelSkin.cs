// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Shaders;

namespace Stride.Rendering.Materials
{
    /// <summary>
    /// Calculates the scattering profile for skin, which is applied during
    /// the forward pass using the subsurface scattering shading model.
    /// It also calculates a scattering kernel based on the "Falloff" and "Strength" parameters.
    /// 
    /// </summary>
    [Display("Skin")]
    [DataContract("MaterialSubsurfaceScatteringScatteringKernelSkin")]
    public class MaterialSubsurfaceScatteringScatteringKernelSkin : IMaterialSubsurfaceScatteringScatteringKernel
    {
        /// <summary>
        /// This parameter defines the per-channel falloff of the gradients produced by the subsurface scattering events.
        /// It can be used to fine tune the color of the gradients.
        /// </summary>
        /// <userdoc>
        /// Attention: This parameter only affects the SSS post-process. If you want to change the shading, use a different scattering profile. 
        /// This parameter defines the per-channel falloff of the gradients produced by the subsurface scattering events.
        /// It can be used to fine tune the color of the gradients.
        /// </userdoc>
        [DataMember(10)]
        [Display("Falloff")]
        public Color3 Falloff { get; set; } = new Color3(1.0f, 0.37f, 0.3f);    // Default falloff for skin.

        /// <summary>
        /// This parameter specifies the how much of the diffuse light gets into the material, and thus gets modified by the SSS mechanism.
        /// It can be seen as a per-channel mix factor between the original image, and the SSS-filtered image.
        /// </summary>
        /// <userdoc>
        /// Attention: This parameter only affects the SSS post-process. If you want to change the shading, use a different scattering profile. 
        /// This parameter specifies the how much of the diffuse light gets into the material, and thus gets modified by the SSS mechanism.
        /// It can be seen as a per-channel mix factor between the original image, and the SSS-filtered image.
        /// </userdoc>
        [DataMember(20)]
        [Display("Strength")]
        public Color3 Strength { get; set; } = new Color3(0.48f, 0.41f, 0.28f); // Default strength for skin.

        public Vector4[] Generate()
        {
            return SubsurfaceScatteringKernelGenerator.CalculateScatteringKernel(SubsurfaceScatteringSettings.SamplesPerScatteringKernel2,
                                                                                 Strength, Falloff);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is MaterialSubsurfaceScatteringScatteringKernelSkin;
        }

        public override int GetHashCode()
        {
            return typeof(MaterialSubsurfaceScatteringScatteringKernelSkin).GetHashCode();
        }
    }
}
