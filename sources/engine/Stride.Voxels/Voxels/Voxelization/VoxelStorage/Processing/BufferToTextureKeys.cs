// Copyright (c) Stride contributors (https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public partial class BufferToTextureKeys
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributesIndirect = ParameterKeys.NewPermutation<ShaderSourceCollection>();
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributesTemp = ParameterKeys.NewPermutation<ShaderSourceCollection>();
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributeLocalSamples = ParameterKeys.NewPermutation<ShaderSourceCollection>();
        public static readonly PermutationParameterKey<string> IndirectReadAndStoreMacro = ParameterKeys.NewPermutation<string>();
        public static readonly PermutationParameterKey<string> IndirectStoreMacro = ParameterKeys.NewPermutation<string>();
    }
}
