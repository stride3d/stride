using System;
using System.Collections.Generic;
using System.Text;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public interface IVoxelMarchMethod
    {
        ShaderSource GetMarchingShader(int attrID);
        void UpdateMarchingLayout(string compositionName);
        void ApplyMarchingParameters(ParameterCollection parameters);
    }
}
