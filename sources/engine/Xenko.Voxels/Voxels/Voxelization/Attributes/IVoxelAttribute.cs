using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelAttribute
    {
        void PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage);
        void PrepareOutputStorage(VoxelStorageContext context, IVoxelStorage storage);
        void ClearOutputStorage();

        void CollectVoxelizationPasses(VoxelizationPassList passList, IVoxelStorer storer, Matrix view, Vector3 resolution, VoxelizationStage stage, bool output);
        void CollectAttributes(List<AttributeStream> attributes, VoxelizationStage stage, bool output);

        void PostProcess(RenderDrawContext drawContext);

        //Writing
        ShaderSource GetVoxelizationShader();
        void UpdateVoxelizationLayout(string compositionName);
        void ApplyVoxelizationParameters(ParameterCollection parameters);

        void SetBufferOffset(int id);
        int GetBufferOffset();

        //Sampling
        ShaderSource GetSamplingShader();
        void UpdateSamplingLayout(string compositionName);
        void ApplySamplingParameters(VoxelViewContext viewContext, ParameterCollection parameters);

        void SetLocalSamplerID(int id);
        int GetLocalSamplerID();


    }
}
