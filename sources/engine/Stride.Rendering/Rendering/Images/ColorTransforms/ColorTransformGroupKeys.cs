// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="ToneMap"/> and ToneMapEffect sdfx
    /// </summary>
    internal static class ColorTransformGroupKeys
    {
        public static readonly PermutationParameterKey<List<ColorTransform>> Transforms = (PermutationParameterKey<List<ColorTransform>>)ParameterKeys.Merge(ParameterKeys.NewPermutation<List<ColorTransform>>(), typeof(ColorTransformGroupKeys), "ColorTransformGroup.Transforms");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(ColorTransformGroupKeys).TypeHandle);
        }
    }
}
