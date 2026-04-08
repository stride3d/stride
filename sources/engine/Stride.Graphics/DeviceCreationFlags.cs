// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Flags describing the parameters that are used to create a Graphics Device.
/// </summary>
[Flags]
public enum DeviceCreationFlags : int
{
    None = 0,

    /// <summary>
    ///   Creates a Graphics Device that supports the debug layer.
    /// </summary>
    Debug = 2,

    /// <summary>
    ///   Creates a Graphics Device requiring BGRA pixel format support, which is required for
    ///   Direct2D interoperability with Direct3D resources.
    /// </summary>
    BgraSupport = 32,

    // TODO: Should public API mention Silk? It's internal detail and device type is not configurable by Stride

    /// <summary>
    ///   Forces the creation of the Graphics Device to fail if the display driver is not implemented
    ///   as a WDDM 1.2 driver, in which case only a Direct3D device that is created with feature level
    ///   9.1, 9.2, or 9.3 supports video;
    ///   Therefore, if this flag is set, the runtime creates the Direct3D device only for feature level
    ///   9.1, 9.2, or 9.3.
    /// </summary>
    /// <remarks>
    ///   We recommend not to specify this flag for applications that want to favor Direct3D capability over video.
    ///   If feature level 10 and higher is available, the runtime will use that feature level regardless of
    ///   video support.
    ///   <para/>
    ///   If this flag is set, device creation on the Basic Render Device (BRD) will succeed regardless of the
    ///   BRD's missing support for video decode.
    ///   This is because the Media Foundation video stack operates in software mode on BRD. In this situation,
    ///   if you force the video stack to create the Direct3D device twice (create the device once with this flag,
    ///   next discover BRD, then again create the device without the flag), you actually degrade performance.
    ///   <para/>
    ///   If you attempt to create a Direct3D device with driver type <see cref="Silk.NET.Core.Native.D3DDriverType.Null"/>,
    ///   <see cref="Silk.NET.Core.Native.D3DDriverType.Reference"/>, or <see cref="Silk.NET.Core.Native.D3DDriverType.Software"/>,
    ///   device creation fails at any feature level because none of the associated drivers provide video capability.
    ///   If you attempt to create a Direct3D device with driver type <see cref="Silk.NET.Core.Native.D3DDriverType.Warp"/>,
    ///   device creation succeeds to allow software fallback for video.
    ///   <para/>
    ///   This value is not supported until Direct3D 11.1.
    /// </remarks>
    VideoSupport = 2048
}
