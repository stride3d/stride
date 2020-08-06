// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
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