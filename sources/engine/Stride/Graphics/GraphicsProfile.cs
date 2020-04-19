// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// Identifies the set of supported devices for the demo based on device capabilities.
    /// </summary>
    [DataContract("GraphicsProfile")]
    public enum GraphicsProfile
    {
        /// <summary>
        /// DirectX9 support (HLSL 3.0)
        /// </summary>
        [Display("Direct3D 9.1 / OpenGL ES 2.0")]
        Level_9_1 = 0x9100,

        /// <summary>
        /// DirectX9 support (HLSL 3.0)
        /// </summary>
        [Display("Direct3D 9.2")]
        Level_9_2 = 0x9200,

        /// <summary>
        /// DirectX9 support (HLSL 3.0)
        /// </summary>
        [Display("Direct3D 9.3")]
        Level_9_3 = 0x9300,
        
        /// <summary>
        /// DirectX10 support (HLSL 4.0, Geometry Shader)
        /// </summary>
        [Display("Direct3D 10.0 / OpenGL ES 3.0")]
        Level_10_0 = 0xA000,

        /// <summary>
        /// DirectX10.1 support (HLSL 4.1, Geometry Shader)
        /// </summary>
        [Display("Direct3D 10.1")]
        Level_10_1 = 0xA100,

        /// <summary>
        /// DirectX11 support (HLSL 5.0, Compute Shaders, Domain/Hull Shaders)
        /// </summary>
        [Display("Direct3D 11.0 / OpenGL ES 3.1")]
        Level_11_0 = 0xB000,

        /// <summary>
        /// DirectX11.1 support (HLSL 5.0, Compute Shaders, Domain/Hull Shaders)
        /// </summary>
        [Display("Direct3D 11.1")]
        Level_11_1 = 0xB100,

        /// <summary>
        /// DirectX11.2 support (HLSL 5.0, Compute Shaders, Domain/Hull Shaders)
        /// </summary>
        [Display("Direct3D 11.2")]
        Level_11_2 = 0xB200,
    }
}
