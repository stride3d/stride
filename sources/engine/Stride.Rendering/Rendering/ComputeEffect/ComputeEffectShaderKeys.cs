// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.Rendering.ComputeEffect
{
    public class ComputeEffectShaderKeys
    {
        public static readonly PermutationParameterKey<string> ComputeShaderName = ParameterKeys.NewPermutation<string>();
        public static readonly PermutationParameterKey<Int3> ThreadNumbers = ParameterKeys.NewPermutation<Int3>();
    }
}
