// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Rendering.Images
{
    internal static class ToneMapKeys
    {
        public static readonly PermutationParameterKey<bool> AutoExposure = ParameterKeys.NewPermutation(false);

        public static readonly PermutationParameterKey<bool> AutoKey = ParameterKeys.NewPermutation(false);

        public static readonly PermutationParameterKey<bool> UseLocalLuminance = ParameterKeys.NewPermutation(false);

        public static readonly PermutationParameterKey<ToneMapOperator> Operator = ParameterKeys.NewPermutation<ToneMapOperator>();
    }
}
