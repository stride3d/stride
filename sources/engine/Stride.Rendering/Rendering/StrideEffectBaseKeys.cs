// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering
{
    public static class StrideEffectBaseKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> ExtensionPostVertexStageShader = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(StrideEffectBaseKeys), "StrideEffectBase.ExtensionPostVertexStageShader");

        public static readonly PermutationParameterKey<ShaderSource> ComputeVelocityShader = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(StrideEffectBaseKeys), "StrideEffectBase.ComputeVelocityShader");

        public static readonly PermutationParameterKey<ShaderSource> RenderTargetExtensions = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(StrideEffectBaseKeys), "StrideEffectBase.RenderTargetExtensions");

        public static readonly PermutationParameterKey<bool> HasInstancing = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(StrideEffectBaseKeys), "StrideEffectBase.HasInstancing");

        public static readonly PermutationParameterKey<int> ModelTransformUsage = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(StrideEffectBaseKeys), "StrideEffectBase.ModelTransformUsage");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(StrideEffectBaseKeys).TypeHandle);
        }
    }
}
