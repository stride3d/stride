using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Rendering;
using Xenko.Rendering.Voxels;
using Xenko.Shaders;

[DataContract(DefaultMemberMode = DataMemberMode.Default)]
[Display("Solidify")]
public class VoxelModifierEmissionOpacitySolidify : VoxelModifierBase, IVoxelModifierEmissionOpacity
{
    VoxelAttributeSolidity solidityAttribute = new VoxelAttributeSolidity();

    public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
    {
        if (!Enabled) return;
        solidityAttribute.CollectAttributes(attributes, stage, output);
    }
    public bool RequiresColumns()
    {
        if (!Enabled) return false;
        return true;
    }
    public ShaderSource GetApplier(string layout)
    {
        if (!Enabled) return null;
        return new ShaderClassSource("VoxelModifierApplierSolidify" + layout, solidityAttribute.GetLocalSamplerID());
    }
    public void UpdateVoxelizationLayout(string compositionName) { }
    public void ApplyVoxelizationParameters(ParameterCollection parameters) { }
}
