// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Rendering;
using Stride.Shaders;

namespace Stride.Rendering
{
    public static class StrideEffectBaseKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> ExtensionPostVertexStageShader = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> ComputeVelocityShader = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> RenderTargetExtensions = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<bool> HasInstancing = ParameterKeys.NewPermutation<bool>();

        public static readonly PermutationParameterKey<int> ModelTransformUsage = ParameterKeys.NewPermutation<int>();
    }
}
