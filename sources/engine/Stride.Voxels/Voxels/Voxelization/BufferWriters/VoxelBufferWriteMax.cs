using System;
using System.Collections.Generic;
using System.Text;
using Xenko.Core;
using Xenko.Shaders;
using Xenko.Rendering.Materials;

namespace Xenko.Rendering.Voxels
{
    //[DataContract("VoxelFlickerReductionAverage")]
    [DataContract(DefaultMemberMode = DataMemberMode.Default)]
    [Display("Max")]
    public class VoxelBufferWriteMax : IVoxelBufferWriter
    {
        ShaderSource source = new ShaderClassSource("VoxelBufferWriteMax");
        public ShaderSource GetShader()
        {
            return source;
        }
    }
}
