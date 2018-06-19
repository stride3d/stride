// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Shaders.Convertor
{
    public enum GlslShaderPlatform
    {
        /// <summary>
        /// GLSL OpenGL Shader.
        /// </summary>
        OpenGL,

        /// <summary>
        /// GLSL OpenGL ES Shader.
        /// </summary>
        OpenGLES,

        /// <summary>
        /// GLSL Vulkan Shader.
        /// </summary>
        Vulkan,
    }
}
