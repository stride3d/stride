using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelBufferWriter
    {
        ShaderSource GetShader();
    }
}
