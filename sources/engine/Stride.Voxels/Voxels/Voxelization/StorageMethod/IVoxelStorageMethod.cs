using System;
using System.Collections.Generic;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelStorageMethod
    {
        void Apply(ShaderMixinSource mixin);
        int PrepareLocalStorage(VoxelStorageContext context, IVoxelStorage storage, int channels, int layoutCount);
    }
}
