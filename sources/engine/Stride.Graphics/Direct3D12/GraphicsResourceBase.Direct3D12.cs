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
    public abstract unsafe partial class GraphicsResourceBase
    {
        private ID3D12DeviceChild* nativeDeviceChild;
        private ID3D12Resource* nativeResource;

        /// <summary>
        ///   Gets the internal Direct3D 11 Resource.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        protected internal ComPtr<ID3D12Resource> NativeResource => ToComPtr(nativeResource);

        /// <summary>
        ///   Gets the internal Direct3D 12 Device Child.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
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

        /// <summary>
        ///   Sets the internal Direct3D 12 Device Child to <see langword="null"/> without releasing it.
        ///   This is used when the Graphics Device is being destroyed, but the resource is a View.
        /// </summary>
        protected internal void ForgetNativeChildWithoutReleasing()
        {
            nativeDeviceChild = null;
            nativeResource = null;
        }

        /// <summary>
        ///   Gets the internal Direct3D 11 device (<see cref="ID3D11Device"/>) if the resource is attached to
        ///   a <see cref="Graphics.GraphicsDevice"/>, or <see langword="null"/> if not.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        protected ComPtr<ID3D12Device> NativeDevice => GraphicsDevice?.NativeDevice ?? default;


        // No Direct3D-specific initialization
        private partial void Initialize() { }

        /// <summary>
        ///   Called when the <see cref="GraphicsDevice"/> has been detected to be internally destroyed,
        ///   or when the <see cref="Destroy"/> methad has been called. Raises the <see cref="Destroyed"/> event.
        /// </summary>
        /// <remarks>
        ///   This method releases the underlying native resources (<see cref="ID3D12Resource"/> and <see cref="ID3D12DeviceChild"/>).
        /// </remarks>
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
        ///   Called when the <see cref="GraphicsDevice"/> has been recreated.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if resource has transitioned to the <see cref="GraphicsResourceLifetimeState.Active"/> state.
        /// </returns>
        protected internal virtual bool OnRecreate()
        {
            return false;
        }
    }
}

#endif
