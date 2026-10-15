// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by <see cref="LightStreak"/> and LightStreakEffect sdfx.
    /// </summary>
    internal static class LightStreakKeys
    {
        public static readonly PermutationParameterKey<int> Count = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(LightStreakKeys), "LightStreak.Count");
        
        public static readonly PermutationParameterKey<int> AnamorphicCount = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(LightStreakKeys), "LightStreak.AnamorphicCount");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(LightStreakKeys).TypeHandle);
        }
    }
}
