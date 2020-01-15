using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xenko.Core;
using Xenko.Rendering;
using Xenko.Rendering.Voxels;
using Xenko.Shaders;

public interface IVoxelModifier
{
    bool RequiresColumns();
    void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output);
    void UpdateVoxelizationLayout(string compositionName);
    void ApplyVoxelizationParameters(ParameterCollection parameters);
    ShaderSource GetApplier(string layout);
}