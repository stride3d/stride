// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Shaders;

namespace Stride.Rendering.Skyboxes
{
    public static class SkyboxKeys
    {
        public static readonly ValueParameterKey<float> Intensity = (ValueParameterKey<float>)ParameterKeys.Merge(ParameterKeys.NewValue(1.0f), typeof(SkyboxKeys), "Skybox.Intensity");

        public static readonly ValueParameterKey<Matrix> SkyMatrix = (ValueParameterKey<Matrix>)ParameterKeys.Merge(ParameterKeys.NewValue(Matrix.Identity), typeof(SkyboxKeys), "Skybox.SkyMatrix");

        public static readonly PermutationParameterKey<ShaderSource> Shader = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(SkyboxKeys), "Skybox.Shader");

        public static readonly PermutationParameterKey<ShaderSource> DiffuseLighting = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(SkyboxKeys), "Skybox.DiffuseLighting");

        public static readonly PermutationParameterKey<ShaderSource> SpecularLighting = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(SkyboxKeys), "Skybox.SpecularLighting");

        public static readonly ObjectParameterKey<Texture> CubeMap = (ObjectParameterKey<Texture>)ParameterKeys.Merge(ParameterKeys.NewObject<Texture>(), typeof(SkyboxKeys), "Skybox.CubeMap");

        [ModuleInitializer]
        internal static void __Initialize__()
        {
            System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(typeof(SkyboxKeys).TypeHandle);
        }
    }
}
