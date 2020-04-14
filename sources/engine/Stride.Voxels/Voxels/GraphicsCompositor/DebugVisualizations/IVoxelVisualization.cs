using System;
using System.Collections.Generic;
using System.Text;
using Stride.Rendering.Images;

namespace Stride.Rendering.Voxels.Debug
{
    public interface IVoxelVisualization
    {
        ImageEffectShader GetShader(RenderDrawContext context, VoxelAttribute attr);
    }
}
