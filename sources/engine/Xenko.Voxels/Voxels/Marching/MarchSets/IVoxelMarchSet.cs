using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public interface IVoxelMarchSet
    {
        ShaderSource GetMarchingShader(int attrID);
        void UpdateMarchingLayout(string compositionName);
        void ApplyMarchingParameters(ParameterCollection parameters);
    }
}
