using System;
using System.Collections.Generic;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelStorageMethod
    {
        void Apply(ShaderMixinSource mixin);
        int PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage, int channels, int layoutCount);
    }
}
