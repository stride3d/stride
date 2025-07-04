// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract unsafe partial class GraphicsResourceBase
    {
        private ID3D12DeviceChild* nativeDeviceChild;
        private ID3D12Resource* nativeResource;

        protected internal ComPtr<ID3D12Resource> NativeResource => ToComPtr(nativeResource);

        /// <summary>
        /// Gets or sets the device child.
        /// </summary>
        /// <value>The device child.</value>
        protected internal ComPtr<ID3D12DeviceChild> NativeDeviceChild
        {
            get => ToComPtr(nativeDeviceChild);

            set
            {
                if (nativeDeviceChild == value.Handle)
                    return;

                var oldDeviceChild = nativeDeviceChild;
                if (oldDeviceChild is not null)
                    oldDeviceChild->Release();

                nativeDeviceChild = value;
                if (nativeDeviceChild is not null)
                    nativeDeviceChild->AddRef();
                else
                    return;

                Debug.Assert(nativeDeviceChild is not null);

                HResult result = nativeDeviceChild->QueryInterface(out ComPtr<ID3D12Resource> d3d12Resource);

                // The device child can be something that is not a Direct3D resource actually,
                // like a Sampler State, for example
                if (result.IsSuccess)
                {
                    nativeResource = d3d12Resource.Handle;
                }

                NativeDeviceChild.SetDebugName(Name);
            }
        }

        protected internal void ForgetNativeChildWithoutReleasing()
        {
            nativeDeviceChild = null;
            nativeResource = null;
        }

        /// <summary>
        /// Associates the private data to the device child, useful to get the name in PIX debugger.
        /// </summary>
        protected ComPtr<ID3D12Device> NativeDevice => GraphicsDevice?.NativeDevice ?? default;


        // No Direct3D-specific initialization
        private partial void Initialize() { }

        /// <summary>
        /// Called when graphics device has been detected to be internally destroyed.
        /// </summary>
        protected internal virtual partial void OnDestroyed()
        {
            Destroyed?.Invoke(this, EventArgs.Empty);

            if (nativeDeviceChild is not null)
            {
                // Schedule the resource for destruction (as soon as we are done with it)
                GraphicsDevice.TemporaryResources.Enqueue((GraphicsDevice.NextFenceValue, NativeResource));
                nativeDeviceChild = null;
            }

            SafeRelease(ref nativeResource);
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
