// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Xenko.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="ToneMap"/> and ToneMapEffect xkfx
    /// </summary>
    internal static class ColorTransformGroupKeys
    {
        public static readonly PermutationParameterKey<List<ColorTransform>> Transforms = ParameterKeys.NewPermutation<List<ColorTransform>>();
    }
}
