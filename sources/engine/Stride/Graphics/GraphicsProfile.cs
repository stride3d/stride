// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Identifies the base set of capabilities a Graphics Device supports.
/// </summary>
/// <remarks>
///   <para>
///     The graphics profile only indicates which capabilities are available on the device, not which
///     graphics API is used. For example, a Graphics Device with a <see cref="Level_10_0"/>
///     supports Direct3D 10.0, Direct3D 10.1, and Direct3D 11.0 APIs, as well as OpenGL ES 3.0.
///   </para>
///   <para>
///     Some platforms may not support all graphics profiles, or may have additional restrictions.
///     Also, some graphics APIs may not support all features of a given profile, or may need to
///     enable specific extensions to access certain features (OpenGL, Vulkan).
///   </para>
/// </remarks>
[DataContract("GraphicsProfile")]
public enum GraphicsProfile
{
    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 9.0a (HLSL 3.0),
    ///   OpenGL ES 2.0, or Vulkan 1.0.
    /// </summary>
    [Display("Level 9.1 ~ like Direct3D 9.0 / OpenGL ES 2.0 / Vulkan 1.0")]
    Level_9_1 = 0x9100,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 9.0b (HLSL 3.0),
    ///   OpenGL ES 2.0, or Vulkan 1.0.
    /// </summary>
    [Display("Level 9.2 ~ like Direct3D 9.0b / OpenGL ES 2.0 / Vulkan 1.0")]
    Level_9_2 = 0x9200,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 9.0c (HLSL 3.0),
    ///   OpenGL ES 2.0, or Vulkan 1.0.
    /// </summary>
    [Display("Level 9.3 ~ like Direct3D 9.0c / OpenGL ES 2.0 / Vulkan 1.0")]
    Level_9_3 = 0x9300,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 10
    ///   (HLSL 4.0, Geometry Shaders), OpenGL ES 3.0, or Vulkan 1.0.
    /// </summary>
    [Display("Level 10.0 ~ like Direct3D 10.0 / OpenGL ES 3.0 / Vulkan 1.0")]
    Level_10_0 = 0xA000,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 10.1
    ///   (HLSL 4.1, Geometry Shaders), OpenGL ES 3.0, or Vulkan 1.0.
    /// </summary>
    [Display("Level 10.1 ~ like Direct3D 10.1 / OpenGL ES 3.0 / Vulkan 1.0")]
    Level_10_1 = 0xA100,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 11
    ///   (HLSL 5.0, Compute Shaders, Domain Shaders, Hull Shaders), OpenGL ES 3.1, or Vulkan 1.1.
    /// </summary>
    [Display("Level 11.0 ~ like Direct3D 11.0 / OpenGL ES 3.1 / Vulkan 1.1")]
    Level_11_0 = 0xB000,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 11.1
    ///   (HLSL 5.0, Compute Shaders, Domain Shaders, Hull Shaders), OpenGL ES 3.1, or Vulkan 1.1.
    /// </summary>
    [Display("Level 11.1 ~ like Direct3D 11.1 / OpenGL ES 3.1 / Vulkan 1.1")]
    Level_11_1 = 0xB100,

    /// <summary>
    ///   Identifies Graphics Devices with capabilities roughly at the level of DirectX 11.2
    ///   (HLSL 5.0, Compute Shaders, Domain Shaders, Hull Shaders), OpenGL ES 3.1, or Vulkan 1.1.
    /// </summary>
    [Display("Level 11.2 ~ like Direct3D 11.2 / OpenGL ES 3.1 / Vulkan 1.1")]
    Level_11_2 = 0xB200

    // Future?
    //   DirectX 12 (HLSL 6.0, Ray-tracing, Mesh Shaders, Variable-rate Shading), or Vulkan 1.2+.
    //[Display("Level 12 ~ like Direct3D 12 / Vulkan 1.2")]
    //Level_12 = 0xC000
}
