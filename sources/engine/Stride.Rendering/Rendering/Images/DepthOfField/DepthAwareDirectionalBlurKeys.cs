// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by the DepthAwareDirectionalBlurEffect
    /// </summary>
    public static class DepthAwareDirectionalBlurKeys
    {
        public static readonly PermutationParameterKey<int> Count = ParameterKeys.NewPermutation<int>();

        public static readonly PermutationParameterKey<int> TotalTap = ParameterKeys.NewPermutation<int>();
    }
}
