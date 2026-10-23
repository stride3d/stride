// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Rendering
{
    public partial class SpriteBaseKeys
    {
        public static readonly PermutationParameterKey<bool> ColorIsSRgb = (PermutationParameterKey<bool>)ParameterKeys.Merge(ParameterKeys.NewPermutation(false), typeof(SpriteBaseKeys), "SpriteBase.ColorIsSRgb");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
