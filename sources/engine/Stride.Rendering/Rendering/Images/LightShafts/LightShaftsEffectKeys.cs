// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Shaders;

namespace Stride.Rendering.Images
{
    public static class LightShaftsEffectKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> LightGroup = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(LightShaftsEffectKeys), "LightShaftsEffect.LightGroup");
        public static readonly PermutationParameterKey<int> SampleCount = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(LightShaftsEffectKeys), "LightShaftsEffect.SampleCount");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LightShaftsEffectKeys).TypeHandle);
        }
    }
}
