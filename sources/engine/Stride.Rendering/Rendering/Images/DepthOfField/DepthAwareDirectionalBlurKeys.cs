// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by the DepthAwareDirectionalBlurEffect
    /// </summary>
    public static class DepthAwareDirectionalBlurKeys
    {
        public static readonly PermutationParameterKey<int> Count = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(DepthAwareDirectionalBlurKeys), "DepthAwareDirectionalBlur.Count");

        public static readonly PermutationParameterKey<int> TotalTap = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(DepthAwareDirectionalBlurKeys), "DepthAwareDirectionalBlur.TotalTap");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(DepthAwareDirectionalBlurKeys).TypeHandle);
        }
    }
}
