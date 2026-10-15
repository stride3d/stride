// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Rendering.Voxels.VoxelGI
{
    public partial class LightVoxelShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> diffuseMarcher = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(LightVoxelShaderKeys), "LightVoxelShader.diffuseMarcher");
        public static readonly PermutationParameterKey<ShaderSource> specularMarcher = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(LightVoxelShaderKeys), "LightVoxelShader.specularMarcher");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
