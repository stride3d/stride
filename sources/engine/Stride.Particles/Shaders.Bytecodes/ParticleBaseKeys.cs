// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

// The namespace has to stay Stride.Rendering to match the generated shader code
namespace Stride.Rendering
{
    public partial class ParticleBaseKeys
    {
        static ParticleBaseKeys()
        {
            //MatrixTransform = ParameterKeys.New(Matrix.Identity);
        }

        //public static readonly ParameterKey<bool> ColorIsSRgb = ParameterKeys.New(false);

        //public static readonly ParameterKey<ShaderSource> ParticleColor = ParameterKeys.New<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> BaseColor = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<uint> UsesSoftEdge = ParameterKeys.NewPermutation<uint>(0);

        //public static readonly ParameterKey<ShaderSource> BaseIntensity = ParameterKeys.New<ShaderSource>();

        public static readonly ObjectParameterKey<Texture> EmissiveMap = ParameterKeys.NewObject<Texture>();
        public static readonly ValueParameterKey<Color4> EmissiveValue = ParameterKeys.NewValue<Color4>();

        //public static readonly ParameterKey<Texture> IntensityMap = ParameterKeys.New<Texture>();
        //public static readonly ParameterKey<float>   IntensityValue = ParameterKeys.New<float>();

    }
}
