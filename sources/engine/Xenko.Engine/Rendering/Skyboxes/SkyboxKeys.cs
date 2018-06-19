// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Xenko.Core.Mathematics;
using Xenko.Graphics;
using Xenko.Shaders;

namespace Xenko.Rendering.Skyboxes
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
