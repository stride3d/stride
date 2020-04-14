using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelFragmentPacker
    {
        ShaderSource GetShader();
        int GetBits(int channels);
    }
}
