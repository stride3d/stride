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
    /// GraphicsResource class
    /// </summary>
    protected internal ComPtr<ID3D11Resource> NativeResource => ComPtrHelpers.ToComPtr(nativeResource);

        /// <summary>
        /// Gets or sets the device child.
        /// </summary>
        /// <value>The device child.</value>
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
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
    protected ComPtr<ID3D11Device> NativeDevice => GraphicsDevice?.NativeDevice ?? default;


    // No Direct3D-specific initialization
    private partial void Initialize() { }

    protected internal virtual partial void OnDestroyed()
    {
        Destroyed?.Invoke(this, EventArgs.Empty);

        if (nativeDeviceChild != null)
        {
            nativeDeviceChild->Release();
            nativeDeviceChild = null;
        }
        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        if (nativeResource != null)
        {
            nativeResource->Release();
            nativeResource = null;
        }
    }

        /// <summary>
        ///   Gets the CPU access flags from the intended resource usage.
        /// </summary>
        /// <param name="usage">The usage.</param>
        /// <returns>A combination of one or more <see cref="CpuAccessFlag"/> flags.</returns>
    protected internal virtual bool OnRecreate()
    {
        return false;
    }

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
