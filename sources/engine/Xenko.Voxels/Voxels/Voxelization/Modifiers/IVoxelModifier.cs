using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelModifier
    {
        bool Enabled { get; set; }
        bool RequiresColumns();
        void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output);
        void UpdateVoxelizationLayout(string compositionName);
        void ApplyVoxelizationParameters(ParameterCollection parameters);
        ShaderSource GetApplier(string layout);
    }
}