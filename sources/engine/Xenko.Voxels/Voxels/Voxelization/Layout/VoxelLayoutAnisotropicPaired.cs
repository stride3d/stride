using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Anisotropic (3 sided)")]
    public class VoxelLayoutAnisotropicPaired : VoxelLayoutBase, IVoxelLayout
    {
        protected override int LayoutCount { get; set; } = 3;
        protected override ShaderClassSource Writer { get; set; } = new ShaderClassSource("VoxelAnisotropicPairedWriter_Float4");
        protected override ShaderClassSource Sampler { get; set; } = new ShaderClassSource("VoxelAnisotropicPairedSampler");
        protected override string ApplierKey { get; set; } = "AnisotropicPaired";

        public void UpdateVoxelizationLayout(string compositionName, List<VoxelModifierEmissionOpacity> modifier)
        {
            DirectOutput = VoxelAnisotropicPairedWriter_Float4Keys.DirectOutput.ComposeWith(compositionName);
            BrightnessInvKey = VoxelAnisotropicPairedWriter_Float4Keys.maxBrightnessInv.ComposeWith(compositionName);
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            BrightnessKey = VoxelAnisotropicPairedSamplerKeys.maxBrightness.ComposeWith(compositionName);
            storageTex.UpdateSamplingLayout("storage." + compositionName);
        }
    }
}
