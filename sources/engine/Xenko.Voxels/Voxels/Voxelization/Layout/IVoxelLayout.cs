using System;
using System.Collections.Generic;
using Xenko.Shaders;
using static Xenko.Rendering.Voxels.VoxelAttributeEmissionOpacity;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelLayout
    {
        int PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage);
        void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage);
        void ClearOutputStorage();

        void PostProcess(RenderDrawContext drawContext, LightFalloffs LightFalloff);

        //Writing
        ShaderSource GetVoxelizationShader(List<IVoxelModifierEmissionOpacity> modifiers);
        void UpdateVoxelizationLayout(string compositionName, List<IVoxelModifierEmissionOpacity> modifiers);
        void ApplyVoxelizationParameters(ParameterCollection parameters, List<IVoxelModifierEmissionOpacity> modifiers);

        //Sampling
        ShaderSource GetSamplingShader();
        void UpdateSamplingLayout(string compositionName);
        void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters);
    }
}
