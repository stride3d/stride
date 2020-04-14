// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Core;

namespace Xenko.Graphics
{
    /// <summary>
    /// The graphics platform.
    /// </summary>
    [DataContract("GraphicsPlatform")]
    public enum GraphicsPlatform
    {
        /// <summary>
        /// The Null Shader.
        /// </summary>
        Null,

        /// <summary>
        /// HLSL Direct3D Shader.
        /// </summary>
        Direct3D11,

        /// <summary>
        /// HLSL Direct3D Shader.
        /// </summary>
        Direct3D12,

        /// <summary>
        /// GLSL OpenGL Shader.
        /// </summary>
        OpenGL,

        /// <summary>
        /// GLSL OpenGL ES Shader.
        /// </summary>
        OpenGLES,

        /// <summary>
        /// GLSL/SPIR-V Shader.
        /// </summary>
        Vulkan,
    }
}
