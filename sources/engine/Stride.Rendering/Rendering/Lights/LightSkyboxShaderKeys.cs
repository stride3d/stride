// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Shaders;

namespace Stride.Rendering.Lights
{
    public partial class LightSkyboxShaderKeys
    {
        public static readonly PermutationParameterKey<ShaderSource> LightDiffuseColor = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(LightSkyboxShaderKeys), "LightSkyboxShader.LightDiffuseColor");
        public static readonly PermutationParameterKey<ShaderSource> LightSpecularColor = (PermutationParameterKey<ShaderSource>)ParameterKeys.Merge(ParameterKeys.NewPermutation<ShaderSource>(), typeof(LightSkyboxShaderKeys), "LightSkyboxShader.LightSpecularColor");

        // __Initialize__ is defined by the source generator in another partial class.
    }
}
