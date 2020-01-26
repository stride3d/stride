using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Anti Aliasing")]
    public class VoxelModifierEmissionOpacityAntiAliasing : VoxelModifierEmissionOpacity
    {
        VoxelAttributeDirectionalCoverage directionalCoverage = new VoxelAttributeDirectionalCoverage();

        public override void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            directionalCoverage.CollectAttributes(attributes, stage, output);
        }

        public override ShaderSource GetApplier(string layout)
        {
            return new ShaderClassSource("VoxelModifierApplierAntiAliasing" + layout, directionalCoverage.LocalSamplerID);
        }

        public override void UpdateVoxelizationLayout(string compositionName) { }
        public override void ApplyVoxelizationParameters(ParameterCollection parameters) { }
    }
}