// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Keys used by the CombineLevelsFromCoCffect
    /// </summary>
    public static class CombineLevelsFromCoCKeys
    {
        public static readonly PermutationParameterKey<int> LevelCount = ParameterKeys.NewPermutation<int>();
    }
}
