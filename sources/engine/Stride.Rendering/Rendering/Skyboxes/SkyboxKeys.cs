// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Skyboxes
{
    public static class SkyboxKeys
    {
        public static readonly ValueParameterKey<float> Intensity = ParameterKeys.NewValue(1.0f);

        public static readonly ValueParameterKey<Matrix> SkyMatrix = ParameterKeys.NewValue(Matrix.Identity);

        public static readonly PermutationParameterKey<ShaderSource> Shader = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> DiffuseLighting = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly PermutationParameterKey<ShaderSource> SpecularLighting = ParameterKeys.NewPermutation<ShaderSource>();

        public static readonly ObjectParameterKey<Texture> CubeMap = ParameterKeys.NewObject<Texture>();
    }
}
