using System.Collections.Generic;
using Stride.Rendering.Voxels;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public static partial class VoxelizeToFragmentsKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> Storage = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly PermutationParameterKey<bool> RequireGeometryShader = ParameterKeys.NewPermutation<bool>();
        public static readonly PermutationParameterKey<int> GeometryShaderMaxVertexCount = ParameterKeys.NewPermutation<int>();
    }
}