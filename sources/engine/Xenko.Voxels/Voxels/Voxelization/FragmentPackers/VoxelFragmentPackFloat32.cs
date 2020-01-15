using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Float32")]
    public class VoxelFragmentPackFloat32 : IVoxelFragmentPacker
    {
        ShaderSource source = new ShaderClassSource("VoxelFragmentPackFloat32");
        public ShaderSource GetShader()
        {
            return source;
        }
        public int GetBits(int channels)
        {
            return channels * 32;
        }
    }
}
