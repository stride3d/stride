// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by the CombineLevelsFromCoCffect
    /// </summary>
    public static class CombineLevelsFromCoCKeys
    {
        public static readonly PermutationParameterKey<int> LevelCount = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation<int>(), typeof(CombineLevelsFromCoCKeys), "CombineLevelsFromCoC.LevelCount");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(CombineLevelsFromCoCKeys).TypeHandle);
        }
    }
}
