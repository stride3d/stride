// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract unsafe partial class GraphicsResourceBase
    {
        private ID3D12DeviceChild* nativeDeviceChild;

        protected internal ID3D12Resource* NativeResource { get; private set; }

        /// <summary>
        /// Gets or sets the device child.
        /// </summary>
        /// <value>The device child.</value>
        protected internal ID3D12DeviceChild* NativeDeviceChild
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

                ID3D12Resource* d3d12Resource;
                HResult result = nativeDeviceChild->QueryInterface(SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**) &d3d12Resource);

                // The device child can be something that is not a Direct3D resource actually,
                // like a Sampler State, for example
                if (result.IsSuccess)
                {
                    NativeResource = d3d12Resource;
                }

                // Associate PrivateData to this DeviceResource
                SetDebugName(GraphicsDevice, nativeDeviceChild, Name);
            }
        }

        protected ID3D12Device* NativeDevice => GraphicsDevice != null ? GraphicsDevice.NativeDevice : null;

        private void Initialize()
        {
        }

        /// <summary>
        /// Associates the private data to the device child, useful to get the name in PIX debugger.
        /// </summary>
        internal static void SetDebugName(GraphicsDevice graphicsDevice, ID3D12DeviceChild* deviceChild, string name)
        {
            DebugHelpers.SetDebugName(deviceChild, name);
        }

        /// <summary>
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
        protected internal virtual void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);

            if (nativeDeviceChild != null)
            {
                // Schedule the resource for destruction (as soon as we are done with it)
                GraphicsDevice.TemporaryResources.Enqueue((GraphicsDevice.NextFenceValue, new Pointer<ID3D12Resource>(NativeResource)));
                nativeDeviceChild = null;
            }
            NativeResource = null;
        }

        /// <summary>
        /// Called when graphics device has been recreated.
        /// </summary>
        /// <returns>True if item transitioned to a <see cref="GraphicsResourceLifetimeState.Active"/> state.</returns>
        protected internal virtual bool OnRecreate()
        {
            return false;
        }
    }
}

#endif
