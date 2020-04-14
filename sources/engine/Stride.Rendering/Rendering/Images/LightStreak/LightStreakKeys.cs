// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="LightStreak"/> and LightStreakEffect sdfx.
    /// </summary>
    internal static class LightStreakKeys
    {
        public static readonly PermutationParameterKey<int> Count = ParameterKeys.NewPermutation<int>();
        
        public static readonly PermutationParameterKey<int> AnamorphicCount = ParameterKeys.NewPermutation<int>();
    }
}
