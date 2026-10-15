// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Rendering.ComputeEffect
{
    public class ComputeEffectShaderKeys
    {
        public static readonly PermutationParameterKey<string> ComputeShaderName = (PermutationParameterKey<string>)ParameterKeys.Merge(ParameterKeys.NewPermutation<string>(), typeof(ComputeEffectShaderKeys), "ComputeEffectShader.ComputeShaderName");
        public static readonly PermutationParameterKey<Int3> ThreadNumbers = (PermutationParameterKey<Int3>)ParameterKeys.Merge(ParameterKeys.NewPermutation<Int3>(), typeof(ComputeEffectShaderKeys), "ComputeEffectShader.ThreadNumbers");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ComputeEffectShaderKeys).TypeHandle);
        }
    }
}
