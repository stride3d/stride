using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Solidify")]
    public class VoxelModifierEmissionOpacitySolidify : VoxelModifierBase, IVoxelModifierEmissionOpacity
    {
        VoxelAttributeSolidity solidityAttribute = new VoxelAttributeSolidity();

        public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
        {
            solidityAttribute.CollectAttributes(attributes, stage, output);
        }
        override public bool RequiresColumns()
        {
            return true;
        }
        public ShaderSource GetApplier(string layout)
        {
            return new ShaderClassSource("VoxelModifierApplierSolidify" + layout, solidityAttribute.LocalSamplerID);
        }
        public void UpdateVoxelizationLayout(string compositionName) { }
        public void ApplyVoxelizationParameters(ParameterCollection parameters) { }
    }
}