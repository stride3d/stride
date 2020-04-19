// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Graphics
{
    /// <summary>
    /// Resource options for textures.
    /// </summary>
    /// <remarks>
    /// This enumeration is used in TextureDescription.The TextureOptions
    ///     must be 'None' when creating textures with CPU access flags.   
    /// </remarks>
    [Flags]
    public enum TextureOptions
    {
        /// <summary>
        /// None. The default.
        /// </summary>
        None = 0,

        /// <summary>
        /// Enables resource data sharing between two or more Direct3D devices.    
        /// </summary>
        /// <remarks>
        /// The only resources that can be shared are 2D non-mipmapped textures. SharpDX.Direct3D11.ResourceOptionFlags.Shared
        ///     and SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex are mutually exclusive.
        ///     WARP and REF devices do not support shared resources. If you try to create a
        ///     resource with this flag on either a WARP or REF device, the create method will
        ///     return an E_OUTOFMEMORY error code. Note?? Starting with Windows?8, WARP devices
        ///     fully support shared resources. ? Note?? Starting with Windows?8, we recommend
        ///     that you enable resource data sharing between two or more Direct3D devices by
        ///     using a combination of the SharpDX.Direct3D11.ResourceOptionFlags.SharedNthandle
        ///     and SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex flags instead.
        /// </remarks>
        Shared = 2,
#if STRIDE_GRAPHICS_API_DIRECT3D11
        /// <summary>
        ///     Enables the resource to be synchronized by using the SharpDX.DXGI.KeyedMutex.Acquire(System.Int64,System.Int32)
        ///     and SharpDX.DXGI.KeyedMutex.Release(System.Int64) APIs.         
        /// </summary>
        /// <remarks>
        /// The following Direct3D 11 resource creation APIs, that take SharpDX.Direct3D11.ResourceOptionFlags parameters,
        ///     have been extended to support the new flag. SharpDX.Direct3D11.Device.CreateTexture1D(SharpDX.Direct3D11.Texture1DDescription@,SharpDX.DataBox[],SharpDX.Direct3D11.Texture1D)
        ///     SharpDX.Direct3D11.Device.CreateTexture2D(SharpDX.Direct3D11.Texture2DDescription@,SharpDX.DataBox[],SharpDX.Direct3D11.Texture2D)
        ///     SharpDX.Direct3D11.Device.CreateTexture3D(SharpDX.Direct3D11.Texture3DDescription@,SharpDX.DataBox[],SharpDX.Direct3D11.Texture3D)
        ///     SharpDX.Direct3D11.Device.CreateBuffer(SharpDX.Direct3D11.BufferDescription@,System.Nullable{SharpDX.DataBox},SharpDX.Direct3D11.Buffer)
        ///     If you call any of these methods with the SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex
        ///     flag set, the interface returned will support the SharpDX.DXGI.KeyedMutex interface.
        ///     You can retrieve a reference to the SharpDX.DXGI.KeyedMutex interface from the
        ///     resource by using IUnknown::QueryInterface. The SharpDX.DXGI.KeyedMutex interface
        ///     implements the SharpDX.DXGI.KeyedMutex.Acquire(System.Int64,System.Int32) and
        ///     SharpDX.DXGI.KeyedMutex.Release(System.Int64) APIs to synchronize access to the
        ///     surface. The device that creates the surface, and any other device that opens
        ///     the surface by using OpenSharedResource, must call SharpDX.DXGI.KeyedMutex.Acquire(System.Int64,System.Int32)
        ///     before they issue any rendering commands to the surface. When those devices finish
        ///     rendering, they must call SharpDX.DXGI.KeyedMutex.Release(System.Int64). SharpDX.Direct3D11.ResourceOptionFlags.Shared
        ///     and SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex are mutually exclusive.
        ///     WARP and REF devices do not support shared resources. If you try to create a
        ///     resource with this flag on either a WARP or REF device, the create method will
        ///     return an E_OUTOFMEMORY error code. Note?? Starting with Windows?8, WARP devices
        ///     fully support shared resources.
        /// </remarks>
        SharedKeyedmutex = 256,

        /// <summary>
        ///  Set this flag to enable the use of NT HANDLE values when you create a shared
        ///     resource. 
        /// </summary>
        /// <remarks>
        /// By enabling this flag, you deprecate the use of existing HANDLE values.
        ///     When you use this flag, you must combine it with the SharpDX.Direct3D11.ResourceOptionFlags.SharedKeyedmutex
        ///     flag by using a bitwise OR operation. The resulting value specifies a new shared
        ///     resource type that directs the runtime to use NT HANDLE values for the shared
        ///     resource. The runtime then must confirm that the shared resource works on all
        ///     hardware at the specified feature level. Without this flag set, the runtime does
        ///     not strictly validate shared resource parameters (that is, formats, flags, usage,
        ///     and so on). When the runtime does not validate shared resource parameters, behavior
        ///     of much of the Direct3D API might be undefined and might vary from driver to
        ///     driver. Direct3D 11 and earlier: This value is not supported until Direct3D 11.1.
        /// </remarks>
        SharedNthandle = 2048,
#endif
    }
}
