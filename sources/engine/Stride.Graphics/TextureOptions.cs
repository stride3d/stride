// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Flags describing resource options for a <see cref="Texture"/>.
/// </summary>
/// <remarks>
///   This enumeration is used in <see cref="TextureDescription"/>.
/// </remarks>
[Flags]
public enum TextureOptions
{
    None = 0,

    /// <summary>
    ///   Enables data sharing between two or more Direct3D devices.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The only resources that can be shared are 2D non-mipmapped Textures.
    ///   </para>
    ///   <para>
    ///     <see cref="Shared"/> and <see cref="SharedKeyedMutex"/> are mutually exclusive.
    ///   </para>
    ///   <para>
    ///     Note that, starting with Windows 8, it is recommended to enable resource data sharing between two or more Direct3D devices
    ///     by using a combination of the <see cref="SharedNtHandle"/> and <see cref="SharedKeyedMutex"/> flags instead.
    ///   </para>
    /// </remarks>
    Shared = 2,

#if STRIDE_GRAPHICS_API_DIRECT3D11
    /// <summary>
    ///   Enables the Texture to be synchronized by using <see cref="Silk.NET.DXGI.IDXGIKeyedMutex"/> APIs.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     When using this flag when creating a Texture with a graphics device, any other device can open
    ///     the same Texture by using <c>OpenSharedResource</c>. Then they must obtain the <c>KeyedMutex</c>
    ///     from the Texture and <strong>acquire</strong> the mutex before they issue any rendering commands
    ///     to the Texture. When those devices finish rendering, they must <strong>release</strong> the mutex.
    ///   </para>
    ///   <para>
    ///     <see cref="Shared"/> and <see cref="SharedKeyedMutex"/> are mutually exclusive.
    ///   </para>
    /// </remarks>
    SharedKeyedMutex = 256,  // TODO: Support KeyedMutex from Texture

    /// <summary>
    ///   Enable the use of NT HANDLE values when you create a shared Texture.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     When you use this flag, you must combine it with the <see cref="SharedKeyedMutex"/>
    ///     flag by using a bitwise OR operation. The resulting value specifies a new shared
    ///     resource type that directs the runtime to use NT HANDLE values for the shared
    ///     resource. The runtime then must confirm that the shared resource works on all
    ///     hardware at the specified graphics profile. Without this flag set, the runtime does
    ///     not strictly validate shared resource parameters (that is, formats, flags, usage,
    ///     and so on).
    ///   </para>
    ///   <para>
    ///     This flag is not supported until <see cref="GraphicsProfile.Level_11_1"/>.
    ///   </para>
    /// </remarks>
    SharedNtHandle = 2048
#endif
}
