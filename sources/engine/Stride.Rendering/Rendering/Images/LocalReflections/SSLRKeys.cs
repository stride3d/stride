// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by the SSLRResolvePass
    /// </summary>
    public static class SSLRKeys
    {
        public static readonly PermutationParameterKey<int> ResolveSamples = ParameterKeys.NewPermutation(1);
        public static readonly PermutationParameterKey<bool> ReduceHighlights = ParameterKeys.NewPermutation(true);
    }
}
