// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;
using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="AmbientOcclusionBlur"/> and AmbientOcclusionBlurEffect sdfx
    /// </summary>
    internal static class AmbientOcclusionBlurKeys
    {
        public static readonly PermutationParameterKey<int> Count = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(9), typeof(AmbientOcclusionBlurKeys), "AmbientOcclusionBlur.Count");

        public static readonly PermutationParameterKey<bool> VerticalBlur = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(AmbientOcclusionBlurKeys), "AmbientOcclusionBlur.VerticalBlur");

        public static readonly PermutationParameterKey<float> BlurScale = (PermutationParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewPermutation<float>(2f), typeof(AmbientOcclusionBlurKeys), "AmbientOcclusionBlur.BlurScale");

        public static readonly PermutationParameterKey<float> EdgeSharpness = (PermutationParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewPermutation<float>(4f), typeof(AmbientOcclusionBlurKeys), "AmbientOcclusionBlur.EdgeSharpness");

        public static readonly PermutationParameterKey<bool> IsOrthographic = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(AmbientOcclusionBlurKeys), "AmbientOcclusionBlur.IsOrthographic");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(AmbientOcclusionBlurKeys).TypeHandle);
        }
    }
}
