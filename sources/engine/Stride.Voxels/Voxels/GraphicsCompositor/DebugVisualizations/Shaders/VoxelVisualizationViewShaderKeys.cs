// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Shaders;

namespace Xenko.Rendering.Voxels.Debug
{
    public partial class VoxelVisualizationViewShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> marcher = ParameterKeys.NewPermutation<ShaderSource>();
    }
}
