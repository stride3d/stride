using System;
using System.Collections.Generic;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelStorer
    {
        void PostProcess(VoxelStorageContext context, RenderDrawContext drawContext, ProcessedVoxelVolume data);

        ShaderSource GetVoxelizationShader(VoxelizationPass pass, ProcessedVoxelVolume data);
        void ApplyVoxelizationParameters(ParameterCollection param);
        void UpdateVoxelizationLayout(string compositionName);

        bool RequireGeometryShader();
        int GeometryShaderOutputCount();
        bool CanShareRenderStage(IVoxelStorer storer);
    }
}
