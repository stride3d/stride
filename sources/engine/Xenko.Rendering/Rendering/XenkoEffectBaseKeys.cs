// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Rendering;
using Xenko.Shaders;

namespace Xenko.Rendering
{
    public static class XenkoEffectBaseKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> ExtensionPostVertexStageShader = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> ComputeVelocityShader = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> RenderTargetExtensions = ParameterKeys.NewPermutation<ShaderSource>();
    }
}
