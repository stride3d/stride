// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Text;
using Stride.Core;
using Stride.Shaders;
using Stride.Rendering.Materials;

namespace Stride.Rendering.Voxels
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
