// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Rendering.Images
{
    /// <summary>
    /// Keys used by the SSLRResolvePass
    /// </summary>
    public static class SSLRKeys
    {
        public static readonly PermutationParameterKey<int> ResolveSamples = (PermutationParameterKey<int>)ParameterKeys.Merge(ParameterKeys.NewPermutation(1), typeof(SSLRKeys), "SSLR.ResolveSamples");
        public static readonly PermutationParameterKey<bool> ReduceHighlights = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation(true), typeof(SSLRKeys), "SSLR.ReduceHighlights");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SSLRKeys).TypeHandle);
        }
    }
}
