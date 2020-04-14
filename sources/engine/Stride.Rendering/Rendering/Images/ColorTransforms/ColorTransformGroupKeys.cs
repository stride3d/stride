// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="ToneMap"/> and ToneMapEffect sdfx
    /// </summary>
    internal static class ColorTransformGroupKeys
    {
        public static readonly PermutationParameterKey<List<ColorTransform>> Transforms = ParameterKeys.NewPermutation<List<ColorTransform>>();
    }
}
