// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="GaussianBlur"/> and GaussianBlurEffect sdfx
    /// </summary>
    internal static class GaussianBlurKeys
    {
        public static readonly PermutationParameterKey<int> Count = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(GaussianBlurKeys), "GaussianBlur.Count");

        public static readonly PermutationParameterKey<bool> VerticalBlur = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(GaussianBlurKeys), "GaussianBlur.VerticalBlur");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(GaussianBlurKeys).TypeHandle);
        }
    }
}
