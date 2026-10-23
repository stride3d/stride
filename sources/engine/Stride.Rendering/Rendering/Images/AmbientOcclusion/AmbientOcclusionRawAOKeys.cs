// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
{
    public static partial class AmbientOcclusionRawAOKeys
    {
        public static readonly PermutationParameterKey<int> Count = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(AmbientOcclusionRawAOKeys), "AmbientOcclusionRawAO.Count");
        public static readonly PermutationParameterKey<bool> IsOrthographic = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(AmbientOcclusionRawAOKeys), "AmbientOcclusionRawAO.IsOrthographic");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(AmbientOcclusionRawAOKeys).TypeHandle);
        }
    }
}
