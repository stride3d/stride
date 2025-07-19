// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics;

public abstract unsafe partial class GraphicsResourceBase
{
    private ID3D11DeviceChild* nativeDeviceChild;
    private ID3D11Resource* nativeResource;

    /// <summary>
    ///   Gets the internal Direct3D 11 Resource.
    /// </summary>
    /// <remarks>
    ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    protected internal ComPtr<ID3D11Resource> NativeResource => ToComPtr(nativeResource);

    /// <summary>
    ///   Gets or sets the internal <see cref="ID3D11DeviceChild"/>.
    /// </summary>
    /// <remarks>
    ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    protected internal ComPtr<ID3D11DeviceChild> NativeDeviceChild
    {
        get => ToComPtr(nativeDeviceChild);
        set
        {
            if (nativeDeviceChild == value.Handle)
                return;

            var oldDeviceChild = nativeDeviceChild;
            if (oldDeviceChild is not null)
                oldDeviceChild->Release();

            nativeDeviceChild = value.Handle;

            if (nativeDeviceChild is null)
                return;

            nativeDeviceChild->AddRef();

            HResult result = nativeDeviceChild->QueryInterface(out ComPtr<ID3D11Resource> d3dResource);

            // The device child can be something that is not a Direct3D resource actually,
            // like a Sampler State, for example
            if (result.IsSuccess)
            {
                nativeResource = d3dResource.Handle;
            }

            NativeDeviceChild.SetDebugName(Name);
        }
    }

    /// <summary>
    ///   Internal method to detach the internal <see cref="ID3D11DeviceChild"/> without incrementing or decrementing
    ///   the reference count.
    /// </summary>
    protected internal void UnsetNativeDeviceChild()
    {
        nativeDeviceChild = default;
    }

    /// <summary>
    ///   Internal method to set the internal <see cref="ID3D11DeviceChild"/> without incrementing or decrementing
    ///   the reference count.
    /// </summary>
    protected internal void SetNativeDeviceChild(ComPtr<ID3D11DeviceChild> deviceChild)
    {
        nativeDeviceChild = deviceChild.Handle;

        // The device child can be something that is not a Direct3D resource actually,
        // like a Sampler State, for example
        nativeResource = TryGetResource();

        NativeDeviceChild.SetDebugName(Name);
    }

    /// <summary>
    ///   Attempts to retrieve the Direct3D 11 resource associated with the current device child.
    /// </summary>
    /// <returns>
    ///   A <see cref="ComPtr{ID3D11Resource}"/> representing the Direct3D 11 resource if the operation is successful;
    ///   otherwise, the a <see langword="null"/> COM pointer.
    /// </returns>
    private ComPtr<ID3D11Resource> TryGetResource()
    {
        HResult result = nativeDeviceChild->QueryInterface(out ComPtr<ID3D11Resource> d3dResource);
        if (result.IsSuccess)
        {
            return d3dResource;
        }
        return default;
    }

    /// <summary>
    ///   Gets the internal Direct3D 11 device (<see cref="ID3D11Device"/>) if the resource is attached to
    ///   a <see cref="Graphics.GraphicsDevice"/>, or <see langword="null"/> if not.
    /// </summary>
    /// <remarks>
    ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    protected ComPtr<ID3D11Device> NativeDevice => GraphicsDevice?.NativeDevice ?? default;


    // No Direct3D-specific initialization
    private partial void Initialize() { }

    /// <summary>
    ///   Called when the <see cref="GraphicsDevice"/> has been detected to be internally destroyed,
    ///   or when the <see cref="Destroy"/> methad has been called. Raises the <see cref="Destroyed"/> event.
    /// </summary>
    /// <remarks>
    ///   This method releases the underlying native resources (<see cref="ID3D11Resource"/> and <see cref="ID3D11DeviceChild"/>).
    /// </remarks>
    protected internal virtual partial void OnDestroyed()
    {
        Destroyed?.Invoke(this, EventArgs.Empty);

        SafeRelease(ref nativeDeviceChild);
        SafeRelease(ref nativeResource);
    }

    /// <summary>
    ///   Called when the <see cref="GraphicsDevice"/> has been recreated.
    /// </summary>
    /// <returns>
    ///   <see langword="true"/> if resource has transitioned to the <see cref="GraphicsResourceLifetimeState.Active"/> state.
    /// </returns>
    protected internal virtual bool OnRecreate()
    {
        return false;
    }

    /// <summary>
    ///   Gets the CPU access flags from the intended resource usage.
    /// </summary>
    /// <param name="usage">The intended usage for the resource.</param>
    /// <returns>A combination of one or more <see cref="CpuAccessFlag"/> flags.</returns>
    internal static CpuAccessFlag GetCpuAccessFlagsFromUsage(GraphicsResourceUsage usage)
    {
        return usage switch
        {
            GraphicsResourceUsage.Dynamic => CpuAccessFlag.Write,
            GraphicsResourceUsage.Staging => CpuAccessFlag.Read | CpuAccessFlag.Write,
            GraphicsResourceUsage.Immutable => CpuAccessFlag.None,

            _ => CpuAccessFlag.Read
        };
    }

}

#endif
