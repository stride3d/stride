// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public static partial class VoxelizeToFragmentsKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> Storage = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(VoxelizeToFragmentsKeys), "VoxelizeToFragments.Storage");
        public static readonly PermutationParameterKey<bool> RequireGeometryShader = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(VoxelizeToFragmentsKeys), "VoxelizeToFragments.RequireGeometryShader");
        public static readonly PermutationParameterKey<int> GeometryShaderMaxVertexCount = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(VoxelizeToFragmentsKeys), "VoxelizeToFragments.GeometryShaderMaxVertexCount");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(VoxelizeToFragmentsKeys).TypeHandle);
        }
    }
}
