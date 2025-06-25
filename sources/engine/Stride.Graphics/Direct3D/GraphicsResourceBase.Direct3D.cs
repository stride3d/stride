// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

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
    protected internal ComPtr<ID3D11Resource> NativeResource => ComPtrHelpers.ToComPtr(nativeResource);

    /// <summary>
    ///   Gets or sets the internal <see cref="ID3D11DeviceChild"/>.
    /// </summary>
    /// <remarks>
    ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
    ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
    /// </remarks>
    protected internal ComPtr<ID3D11DeviceChild> NativeDeviceChild
    {
        get => ComPtrHelpers.ToComPtr(nativeDeviceChild);
        set
        {
            if (nativeDeviceChild == value.Handle)
                return;

            var oldDeviceChild = nativeDeviceChild;
            if (oldDeviceChild != null)
            {
                oldDeviceChild->Release();
            }

            nativeDeviceChild = value.Handle;
            if (nativeDeviceChild != null)
            {
                nativeDeviceChild->AddRef();
            }
            else return;

            Debug.Assert(nativeDeviceChild != null);

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

        if (nativeDeviceChild != null)
        {
            nativeDeviceChild->Release();
            nativeDeviceChild = null;
        }
        if (nativeResource != null)
        {
            nativeResource->Release();
            nativeResource = null;
        }
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
