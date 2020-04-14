using System;
using System.Collections.Generic;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelStorage
    {
        void UpdateFromContext(VoxelStorageContext context);
        void CollectVoxelizationPasses(ProcessedVoxelVolume data, VoxelStorageContext storageContext);
        
        int RequestTempStorage(int count);
        void UpdateTexture(VoxelStorageContext context, ref IVoxelStorageTexture texture, Xenko.Graphics.PixelFormat pixelFormat, int layoutSize);
        void UpdateTempStorage(VoxelStorageContext context);
        void PostProcess(VoxelStorageContext context, RenderDrawContext drawContext, ProcessedVoxelVolume data);
    }
}
