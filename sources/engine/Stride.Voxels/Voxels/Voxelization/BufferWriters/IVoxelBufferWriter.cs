using System;
using System.Collections.Generic;
using System.Text;
using Stride.Shaders;
using Stride.Rendering.Materials;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelBufferWriter
    {
        ShaderSource GetShader();
    }
}
