// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;

using static Stride.Graphics.DebugHelpers;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract unsafe partial class GraphicsResourceBase
    {
        private ID3D11DeviceChild* nativeDeviceChild;
        private ID3D11Resource* nativeResource;

        protected internal ID3D11Resource* NativeResource => nativeResource;

        /// <summary>
        /// Gets or sets the device child.
        /// </summary>
        /// <value>The device child.</value>
        protected internal ID3D11DeviceChild* NativeDeviceChild
        {
            get => nativeDeviceChild;
            set
            {
                if (nativeDeviceChild == value)
                    return;

                var oldDeviceChild = nativeDeviceChild;
                if (oldDeviceChild != null)
                    oldDeviceChild->Release();

                nativeDeviceChild = value;
                if (nativeDeviceChild != null)
                    nativeDeviceChild->AddRef();
                else
                    return;

                Debug.Assert(nativeDeviceChild != null);

                HResult result = nativeDeviceChild->QueryInterface(out ComPtr<ID3D11Resource> d3dResource);

                // The device child can be something that is not a Direct3D resource actually,
                // like a Sampler State, for example
                if (result.IsSuccess)
                {
                    nativeResource = (ID3D11Resource*) d3dResource;
                }

                SetDebugName(nativeDeviceChild, Name);
            }
        }

        protected ID3D11Device* NativeDevice => GraphicsDevice != null ? GraphicsDevice.NativeDevice : null;

        private void Initialize()
        {
        }

        /// <summary>
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
        protected internal virtual void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);

            if (nativeDeviceChild != null)
                nativeDeviceChild->Release();

            if (NativeResource != null)
                NativeResource->Release();
        }

        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        protected internal virtual bool OnRecreate()
        {
            return false;
        }

        /// <summary>
        /// Gets the cpu access flags from resource usage.
        /// </summary>
        /// <param name="usage">The usage.</param>
        /// <returns></returns>
        internal static CpuAccessFlag GetCpuAccessFlagsFromUsage(GraphicsResourceUsage usage)
        {
            return usage switch
            {
                GraphicsResourceUsage.Dynamic => CpuAccessFlag.Write,
                GraphicsResourceUsage.Staging => CpuAccessFlag.Read | CpuAccessFlag.Write,

                _ => CpuAccessFlag.Read
            };
        }

    }
}

#endif
