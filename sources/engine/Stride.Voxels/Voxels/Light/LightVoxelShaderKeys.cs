// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;
using Stride.Rendering.Lights;

namespace Stride.Rendering.Voxels.VoxelGI
{
    public partial class LightVoxelShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> diffuseMarcher = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly PermutationParameterKey<ShaderSource> specularMarcher = ParameterKeys.NewPermutation<ShaderSource>();
    }
}
