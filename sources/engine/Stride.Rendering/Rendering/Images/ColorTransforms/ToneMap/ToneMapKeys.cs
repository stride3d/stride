// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Rendering.Images
{
    internal static class ToneMapKeys
    {
        public static readonly PermutationParameterKey<bool> AutoExposure = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation(false), typeof(ToneMapKeys), "ToneMap.AutoExposure");

        public static readonly PermutationParameterKey<bool> AutoKey = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation(false), typeof(ToneMapKeys), "ToneMap.AutoKey");

        public static readonly PermutationParameterKey<bool> UseLocalLuminance = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation(false), typeof(ToneMapKeys), "ToneMap.UseLocalLuminance");

        public static readonly PermutationParameterKey<ToneMapOperator> Operator = (PermutationParameterKey<ToneMapOperator>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ToneMapOperator>(), typeof(ToneMapKeys), "ToneMap.Operator");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ToneMapKeys).TypeHandle);
        }
    }
}
