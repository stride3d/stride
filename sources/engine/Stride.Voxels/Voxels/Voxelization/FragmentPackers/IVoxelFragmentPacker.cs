using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Shaders;
using Stride.Rendering.Materials;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelFragmentPacker
    {
        ShaderSource GetShader();
        int GetBits(int channels);
    }
}
