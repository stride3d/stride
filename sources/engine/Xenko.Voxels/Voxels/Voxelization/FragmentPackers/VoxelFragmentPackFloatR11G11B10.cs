using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("R11G11B10F")]
    public class VoxelFragmentPackFloatR11G11B10 : IVoxelFragmentPacker
    {
        ShaderSource source = new ShaderClassSource("VoxelFragmentPackFloatR11G11B10");
        public ShaderSource GetShader()
        {
            return source;
        }
        public int GetBits(int channels)
        {
            return ((channels+2) / 3)*32;//(channels)/3 * 32 + (channels % 3) * 11;
        }
    }
}
