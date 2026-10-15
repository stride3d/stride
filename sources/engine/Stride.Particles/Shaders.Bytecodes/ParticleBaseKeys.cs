// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

        public static readonly PermutationParameterKey<ShaderSource> BaseColor = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(ParticleBaseKeys), "ParticleBase.BaseColor");

        public static readonly PermutationParameterKey<uint> UsesSoftEdge = (PermutationParameterKey<uint>)ParameterKeys.Merge(ParameterKeys.NewPermutation<uint>(0), typeof(ParticleBaseKeys), "ParticleBase.UsesSoftEdge");

        //public static readonly ParameterKey<ShaderSource> BaseIntensity = ParameterKeys.New<ShaderSource>();

        public static readonly ObjectParameterKey<Texture> EmissiveMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(ParticleBaseKeys), "ParticleBase.EmissiveMap");
        public static readonly ValueParameterKey<Color4> EmissiveValue = (ValueParameterKey<Color4>)ParameterKeys.Merge(ParameterKeys.NewValue<Color4>(), typeof(ParticleBaseKeys), "ParticleBase.EmissiveValue");

        //public static readonly ParameterKey<Texture> IntensityMap = ParameterKeys.New<Texture>();
        //public static readonly ParameterKey<float>   IntensityValue = ParameterKeys.New<float>();

        // __Initialize__ is defined by the generator in another partial class file
    }
}
