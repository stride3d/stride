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
    [Display("Isotropic (single)")]
    public class VoxelLayoutIsotropic : VoxelLayoutBase, IVoxelLayout
    {
        protected override int LayoutCount { get; set; } = 1;
        protected override ShaderClassSource Writer { get; set; } = new ShaderClassSource("VoxelIsotropicWriter_Float4");
        protected override ShaderClassSource Sampler { get; set; } = new ShaderClassSource("VoxelIsotropicSampler");
        protected override string ApplierKey { get; set; } = "Isotropic";

        public void UpdateVoxelizationLayout(string compositionName, List<VoxelModifierEmissionOpacity> modifiers)
        {
            DirectOutput = VoxelIsotropicWriter_Float4Keys.DirectOutput.ComposeWith(compositionName);
            BrightnessInvKey = VoxelIsotropicWriter_Float4Keys.maxBrightnessInv.ComposeWith(compositionName);
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            BrightnessKey = VoxelIsotropicSamplerKeys.maxBrightness.ComposeWith(compositionName);
            storageTex.UpdateSamplingLayout("storage." + compositionName);
        }
    }
}
