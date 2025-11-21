// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;

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
        ///   Gets a value indicating whether the Graphics Resource is in "Debug mode".
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Graphics Resource is initialized in "Debug mode"; otherwise, <see langword="false"/>.
        /// </value>
        protected bool IsDebugMode => GraphicsDevice?.IsDebugMode == true;

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

                var previousDeviceChild = nativeDeviceChild;
                if (previousDeviceChild is not null)
                    previousDeviceChild->Release();

                nativeDeviceChild = value.Handle;

                if (nativeDeviceChild is null)
                    return;

                nativeDeviceChild->AddRef();

                SetDebugName();

                // The device child can be something that is not a Direct3D resource actually,
                // like a Sampler State, for example.
                nativeResource = TryGetResource();
            }
        }

        /// <summary>
        ///   Internal method to detach the internal <see cref="ID3D12DeviceChild"/> without incrementing or decrementing
        ///   the reference count.
        /// </summary>
        protected internal void UnsetNativeDeviceChild()
        {
            nativeDeviceChild = default;
            nativeResource = default;
        }

        /// <summary>
        ///   Internal method to set the internal <see cref="ID3D12DeviceChild"/> without incrementing or decrementing
        ///   the reference count.
        /// </summary>
        protected internal void SetNativeDeviceChild(ComPtr<ID3D12DeviceChild> deviceChild)
        {
            nativeDeviceChild = deviceChild.Handle;

            // The device child can be something that is not a Direct3D resource actually,
            // like a Sampler State, for example.
            nativeResource = TryGetResource();

            SetDebugName();
        }

        /// <summary>
        ///   Attempts to retrieve the Direct3D 12 resource associated with the current device child.
        /// </summary>
        /// <returns>
        ///   A <see cref="ComPtr{ID3D12Resource}"/> representing the Direct3D 12 resource if the operation is successful;
        ///   otherwise, the a <see langword="null"/> COM pointer.
        /// </returns>
        private ComPtr<ID3D12Resource> TryGetResource()
        {
            // NOTE: This increments the reference count of the resource, if it is a valid one
            HResult result = nativeDeviceChild->QueryInterface(out ComPtr<ID3D12Resource> d3dResource);
            if (result.IsSuccess)
            {
                d3dResource.Release();  // Decrement the reference count as it's the same "device child" object
                return d3dResource;
            }
            return default;
        }

        /// <summary>
        /// Associates the private data to the device child, useful to get the name in PIX debugger.
        /// </summary>
        internal void SetDebugName()
        {
            if (GraphicsDevice.IsDebugMode && NativeDeviceChild.IsNotNull())
                NativeDeviceChild.SetDebugName($"{Name} ({(nint) NativeDeviceChild.Handle:X16})");
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
        /// <param name="immediately">
        ///   A value indicating whether the resource should be released immediately (<see langword="true"/>),
        ///   or queued for release once the GPU is done with it (<see langword="false"/>).
        /// </param>
        /// <remarks>
        ///   This method releases the underlying native resources (<see cref="ID3D12Resource"/> and <see cref="ID3D12DeviceChild"/>).
        /// </remarks>
        protected internal virtual partial void OnDestroyed(bool immediately = false)
        {
            Destroyed?.Invoke(this, EventArgs.Empty);

            if (nativeDeviceChild is not null)
            {
                if (immediately)
                {
                    // We make sure all previous Command Lists are completed (GPU -> CPU sync point)
                    // NOTE: This is a huge perf-hit in realtime, so it should be only used in rare cases
                    //       (i.e., Back-Buffer resize or application exit).
                    //       Also, we currently do that one by one but we might want to batch them if it proves too slow.
                    var commandListFenceValue = GraphicsDevice.CommandListFence.NextFenceValue;
                    GraphicsDevice.CommandListFence.WaitForFenceCPUInternal(commandListFenceValue);

                    nativeDeviceChild->Release();
                }
                else
                {
                    // Schedule the resource for destruction (as soon as we are done with it)
                    lock (GraphicsDevice.TemporaryResources)
                        GraphicsDevice.TemporaryResources.Enqueue((GraphicsDevice.FrameFence.NextFenceValue, NativeResource));
                }
            }

            nativeDeviceChild = null;

            // We do not Release the resource because it is the same as the "device child",
            // and we count it as just a single reference (see TryGetResource method).
            nativeResource = null;
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
        ///   Swaps the Graphics Resource's internal data with another Graphics Resource.
        /// </summary>
        /// <param name="other">The other Graphics Resource.</param>
        internal virtual void SwapInternal(GraphicsResourceBase other)
        {
            var deviceChild = nativeDeviceChild;
            nativeDeviceChild = other.nativeDeviceChild;
            other.nativeDeviceChild = deviceChild;

            var resource = nativeResource;
            nativeResource = other.nativeResource;
            other.nativeResource = resource;
        }
    }
}

#endif
