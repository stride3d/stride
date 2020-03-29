using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Solidify")]
    public class VoxelModifierEmissionOpacitySolidify : VoxelModifierEmissionOpacity
    {
        VoxelAttributeSolidity solidityAttribute = new VoxelAttributeSolidity();

        public override void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            solidityAttribute.CollectAttributes(attributes, stage, output);
        }
        public override bool RequiresColumns()
        {
            return true;
        }
        public override ShaderSource GetApplier(string layout)
        {
            return new ShaderClassSource("VoxelModifierApplierSolidify" + layout, solidityAttribute.LocalSamplerID);
        }
        public override void UpdateVoxelizationLayout(string compositionName) { }
        public override void ApplyVoxelizationParameters(ParameterCollection parameters) { }
    }
}