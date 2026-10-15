// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Rendering.Voxels.Debug
{
    public partial class VoxelVisualizationRawShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> Attribute = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(VoxelVisualizationRawShaderKeys), "VoxelVisualizationRawShader.Attribute");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
