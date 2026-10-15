// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Rendering.Images
{
    public static class AdditiveLightEffectKeys
    {
        public static readonly PermutationParameterKey<bool> Color = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation<bool>(), typeof(AdditiveLightEffectKeys), "AdditiveLightEffect.Color");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(AdditiveLightEffectKeys).TypeHandle);
        }
    }
}
