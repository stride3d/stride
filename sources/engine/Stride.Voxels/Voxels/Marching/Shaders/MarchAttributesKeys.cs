// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Shaders;

namespace Xenko.Rendering.Voxels
{
    public partial class MarchAttributesKeys
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributeSamplers = ParameterKeys.NewPermutation<ShaderSourceCollection>();
    }
}
