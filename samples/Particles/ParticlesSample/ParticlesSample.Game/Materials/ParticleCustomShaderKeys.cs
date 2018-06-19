// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

//namespace Xenko.Rendering
namespace Xenko.Rendering
{
    public partial class ParticleCustomShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> BaseColor = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly ObjectParameterKey<Texture> EmissiveMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Color4> EmissiveValue = ParameterKeys.NewValue<Color4>();

        public static readonly PermutationParameterKey<ShaderSource> BaseIntensity = ParameterKeys.NewPermutation<ShaderSource>();
        public static readonly ObjectParameterKey<Texture> IntensityMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<float> IntensityValue = ParameterKeys.NewValue<float>();
    }
}
