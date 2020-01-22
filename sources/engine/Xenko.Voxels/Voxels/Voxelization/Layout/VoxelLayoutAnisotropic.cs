using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;
using Xenko.Rendering.Materials;
using static Xenko.Rendering.Voxels.VoxelAttributeEmissionOpacity;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Anisotropic (6 sided)")]
    public class VoxelLayoutAnisotropic : VoxelLayoutBase, IVoxelLayout
    {
        protected override int LayoutCount { get; set; } = 6;
        protected override ShaderClassSource Writer { get; set; } = new ShaderClassSource("VoxelAnisotropicWriter_Float4");
        protected override ShaderClassSource Sampler { get; set; } = new ShaderClassSource("VoxelAnisotropicSampler");
        protected override string ApplierKey { get; set; } = "Anisotropic";

        public void UpdateVoxelizationLayout(string compositionName, List<IVoxelModifierEmissionOpacity> modifier)
        {
            DirectOutput = VoxelAnisotropicWriter_Float4Keys.DirectOutput.ComposeWith(compositionName);
            BrightnessInvKey = VoxelAnisotropicWriter_Float4Keys.maxBrightnessInv.ComposeWith(compositionName);
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            BrightnessKey = VoxelAnisotropicSamplerKeys.maxBrightness.ComposeWith(compositionName);
            storageTex.UpdateSamplingLayout("storage." + compositionName);
        }
    }
}
