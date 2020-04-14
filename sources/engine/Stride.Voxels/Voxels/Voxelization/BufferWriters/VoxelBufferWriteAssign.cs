using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    //[DataContract("VoxelFlickerReductionNone")]
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("None")]
    public class VoxelBufferWriteAssign : IVoxelBufferWriter
    {
        ShaderSource source = new ShaderClassSource("VoxelBufferWriteAssign");
        public ShaderSource GetShader()
        {
            return source;
        }
    }
}
