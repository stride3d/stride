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

        public void UpdateVoxelizationLayout(string compositionName, List<VoxelModifierEmissionOpacity> modifier)
        {
            DirectOutput = VoxelAnisotropicWriter_Float4Keys.DirectOutput.ComposeWith(compositionName);
            BrightnessInvKey = VoxelAnisotropicWriter_Float4Keys.maxBrightnessInv.ComposeWith(compositionName);
        }
        public void UpdateSamplingLayout(string compositionName)
        {
            BrightnessKey = VoxelAnisotropicSamplerKeys.maxBrightness.ComposeWith(compositionName);
            storageTex.UpdateSamplingLayout("storage." + compositionName);
        }

        ShaderClassSource mipmapXP = new ShaderClassSource("Voxel2x2x2Mipmapper_AnisoXP");
        ShaderClassSource mipmapXN = new ShaderClassSource("Voxel2x2x2Mipmapper_AnisoXN");
        ShaderClassSource mipmapYP = new ShaderClassSource("Voxel2x2x2Mipmapper_AnisoYP");
        ShaderClassSource mipmapYN = new ShaderClassSource("Voxel2x2x2Mipmapper_AnisoYN");
        ShaderClassSource mipmapZP = new ShaderClassSource("Voxel2x2x2Mipmapper_AnisoZP");
        ShaderClassSource mipmapZN = new ShaderClassSource("Voxel2x2x2Mipmapper_AnisoZN");
        override public void PostProcess(RenderDrawContext drawContext, LightFalloffs LightFalloff)
        {
            if (mipmapperSharp == null)
            {
                PrepareMipmapShaders();
                mipmapperHeuristic[0] = mipmapXP;
                mipmapperHeuristic[1] = mipmapXN;
                mipmapperHeuristic[2] = mipmapYP;
                mipmapperHeuristic[3] = mipmapYN;
                mipmapperHeuristic[4] = mipmapZP;
                mipmapperHeuristic[5] = mipmapZN;
            }
            base.PostProcess(drawContext, LightFalloff);
        }
        override public void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters)
        {
            if (StorageFormat != StorageFormats.RGBA16F)
                parameters.Set(BrightnessKey, maxBrightness * (float)Math.PI);
            else
                parameters.Set(BrightnessKey, (float)Math.PI);

            storageTex.ApplySamplingParameters(viewContext, parameters);
        }
    }
}
