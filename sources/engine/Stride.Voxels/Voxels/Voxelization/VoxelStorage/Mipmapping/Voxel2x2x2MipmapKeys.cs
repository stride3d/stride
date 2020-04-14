using System;
using Stride.Core;
using Stride.Rendering;
using Stride.Graphics;
using Stride.Shaders;
using Stride.Core.Mathematics;
using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering.Voxels
{
    public static partial class Voxel2x2x2MipmapKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> mipmapper = ParameterKeys.NewPermutation<ShaderSource>();
    }
}
