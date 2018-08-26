// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Graphics
{
    /// <summary>
    /// <p>Describes parameters that are used to create a device.</p>
    /// </summary>
    [Flags]
    public enum DeviceCreationFlags : int
    {
        /// <summary>
        /// Creates a device that supports the debug layer.
        /// </summary>
        Debug = unchecked((int)2),

        /// <summary>
        /// Required for Direct2D interoperability with Direct3D resource.
        /// </summary>
        BgraSupport = unchecked((int)32),

        /// <summary>
        /// Forces the creation of the Direct3D device to fail if the display driver is not implemented to the WDDM for Windows Developer Preview (WDDM 1.2). When the display driver is not implemented to WDDM 1.2, only a Direct3D device that is created with feature level 9.1, 9.2, or 9.3 supports video; therefore, if this flag is set, the runtime creates the Direct3D device only for feature level 9.1, 9.2, or 9.3. We recommend not to specify this flag for applications that want to favor Direct3D capability over video. If feature level 10 and higher is available, the runtime will use that feature level regardless of video support.</p> <p>If this flag is set, device creation on the Basic Render Device (BRD) will succeed regardless of the BRD's missing support for video decode. This is because the Media Foundation video stack operates in software mode on BRD. In this situation, if you force the video stack to create the Direct3D device twice (create the device once with this flag, next discover BRD, then again create the device without the flag), you actually degrade performance.</p> <p>If you attempt to create a Direct3D device with driver type <strong><see cref="SharpDX.Direct3D.DriverType.Null"/></strong>, <strong><see cref="SharpDX.Direct3D.DriverType.Reference"/></strong>, or <strong><see cref="SharpDX.Direct3D.DriverType.Software"/></strong>, device creation fails at any feature level because none of the associated drivers provide video capability. If you attempt to create a Direct3D device with driver type <strong><see cref="SharpDX.Direct3D.DriverType.Warp"/></strong>, device creation succeeds to allow software fallback for video.</p> <strong>Direct 3D 11:??</strong>This value is not supported until Direct3D 11.1. 
        /// </summary>
        VideoSupport = unchecked((int)2048),

        /// <summary>
        /// None.
        /// </summary>
        None = unchecked((int)0),
    }
}
