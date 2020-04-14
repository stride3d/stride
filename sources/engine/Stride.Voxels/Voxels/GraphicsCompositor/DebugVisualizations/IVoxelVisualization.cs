using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Rendering.Images;

namespace Xenko.Rendering.Voxels.Debug
{
    public interface IVoxelVisualization
    {
        ImageEffectShader GetShader(RenderDrawContext context, VoxelAttribute attr);
    }
}
