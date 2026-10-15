// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Sean Boettger <sean@whypenguins.com>
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Shaders;

namespace Stride.Rendering.Voxels
{
    public partial class BufferToTextureKeys
    {
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributesIndirect = (PermutationParameterKey<ShaderSourceCollection>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSourceCollection>(), typeof(BufferToTextureKeys), "BufferToTexture.AttributesIndirect");
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributesTemp = (PermutationParameterKey<ShaderSourceCollection>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSourceCollection>(), typeof(BufferToTextureKeys), "BufferToTexture.AttributesTemp");
        public static readonly PermutationParameterKey<ShaderSourceCollection> AttributeLocalSamples = (PermutationParameterKey<ShaderSourceCollection>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSourceCollection>(), typeof(BufferToTextureKeys), "BufferToTexture.AttributeLocalSamples");
        public static readonly PermutationParameterKey<string> IndirectReadAndStoreMacro = (PermutationParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewPermutation<string>(), typeof(BufferToTextureKeys), "BufferToTexture.IndirectReadAndStoreMacro");
        public static readonly PermutationParameterKey<string> IndirectStoreMacro = (PermutationParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewPermutation<string>(), typeof(BufferToTextureKeys), "BufferToTexture.IndirectStoreMacro");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
