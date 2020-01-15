using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Rendering;
using Xenko.Rendering.Voxels;
using Xenko.Shaders;

[DataContract(DefaultMemberMode = DataMemberMode.Default)]
[Display("Anti Aliasing")]
public class VoxelModifierEmissionOpacityAntiAliasing : VoxelModifierBase, IVoxelModifierEmissionOpacity
{
    VoxelAttributeDirectionalCoverage directionalCoverage = new VoxelAttributeDirectionalCoverage();

    public void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output)
    {
        if (!Enabled) return;
        directionalCoverage.CollectAttributes(attributes, stage, output);
    }
    public bool RequiresColumns()
    {
        if (!Enabled) return false;
        return true;
    }
    public ShaderSource GetApplier(string layout)
    {
        if (!Enabled) return null;
        return new ShaderClassSource("VoxelModifierApplierAntiAliasing" + layout, directionalCoverage.GetLocalSamplerID());
    }

    public void UpdateVoxelizationLayout(string compositionName) { }
    public void ApplyVoxelizationParameters(ParameterCollection parameters) { }
}