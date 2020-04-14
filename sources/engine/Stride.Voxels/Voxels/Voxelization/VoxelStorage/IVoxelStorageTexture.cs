using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelStorageTexture
    {
        void UpdateVoxelizationLayout(string compositionName);
        void UpdateSamplingLayout(string compositionName);
        void ApplyVoxelizationParameters(ObjectParameterKey<Texture> MainKey, ParameterCollection parameters);
        void PostProcess(RenderDrawContext drawContext, ShaderSource[] mipmapShaders);
        ShaderClassSource GetSamplingShader();
        void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters);
    }
}
