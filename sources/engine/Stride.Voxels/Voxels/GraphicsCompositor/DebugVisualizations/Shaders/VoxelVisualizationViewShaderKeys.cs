// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Rendering.Voxels.Debug
{
    public partial class VoxelVisualizationViewShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> marcher = ParameterKeys.NewPermutation<ShaderSource>();
    }
}
