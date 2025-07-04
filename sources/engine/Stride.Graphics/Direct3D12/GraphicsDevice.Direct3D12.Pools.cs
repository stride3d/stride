// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Stride.Graphics;

public unsafe partial class GraphicsDevice
{
    internal abstract class ResourcePool<T> : IDisposable
        where T : unmanaged, IComVtbl<ID3D12Pageable>, IComVtbl<T>
    {
        // A queue to hold live objects that will be reused when it is safe to do so
        private readonly Queue<LiveObject> liveObjects = new();

        #region LiveObject structure

        private readonly record struct LiveObject(ulong FenceValue, ComPtr<T> Object) : IDisposable
        {
            public void Dispose()
            {
                if (Object.IsNotNull())
                    Object.Dispose();
            }
        }

        #endregion

        protected GraphicsDevice GraphicsDevice { get; }


        protected ResourcePool(GraphicsDevice graphicsDevice)
        {
            GraphicsDevice = graphicsDevice;
        }

        public void Dispose()
        {
            lock (liveObjects)
            {
                foreach (var (_, liveObject) in liveObjects)
                {
                    liveObject.Dispose();
                }
                liveObjects.Clear();
            }
        }


        public ComPtr<T> GetObject()
        {
            // TODO: D3D12: SpinLock
            lock (liveObjects)
            {
                // Check if first pooled object is ready for reuse
                if (liveObjects.TryPeek(out LiveObject liveObject))
                {
                    if (liveObject.FenceValue <= GraphicsDevice.nativeFence->GetCompletedValue())
                    {
                        liveObjects.Dequeue();
                        var reusableObject = liveObject.Object;
                        ResetObject(reusableObject);

                        if (reusableObject.IsNull())
                        {
                            // TODO: Incomplete? CreateObject?
                        }

                        return reusableObject;
                    }
                }

                // No pooled object ready to be used, let's create a new one
                return CreateObject();
            }
        }

        protected abstract ComPtr<T> CreateObject();

        protected abstract void ResetObject(ComPtr<T> obj);

        public void RecycleObject(ulong fenceValue, ComPtr<T> obj)
        {
            // TODO D3D12: SpinLock
            lock (liveObjects)
            {
                // Enqueue for reuse when the fence value is reached
                liveObjects.Enqueue(new LiveObject(fenceValue, obj));
            }
        }
    }


    internal class CommandAllocatorPool(GraphicsDevice graphicsDevice) : ResourcePool<ID3D12CommandAllocator>(graphicsDevice)
    {
        protected override ComPtr<ID3D12CommandAllocator> CreateObject()
        {
            // No Command Allocator ready to be used, let's create a new one
            HResult result = GraphicsDevice.NativeDevice.CreateCommandAllocator(CommandListType.Direct, out ComPtr<ID3D12CommandAllocator> commandAllocator);

            if (result.IsFailure)
                result.Throw();

            return commandAllocator;
        }

        protected override void ResetObject(ComPtr<ID3D12CommandAllocator> obj)
        {
            // Reset the Command Allocator to prepare it for reuse
            HResult result = obj.Reset();

            if (result.IsFailure)
                result.Throw();
        }
    }


    internal class HeapPool(GraphicsDevice graphicsDevice, int heapSize, DescriptorHeapType heapType) : ResourcePool<ID3D12DescriptorHeap>(graphicsDevice)
    {
        private readonly int heapSize = heapSize;
        private readonly DescriptorHeapType heapType = heapType;


        protected override ComPtr<ID3D12DescriptorHeap> CreateObject()
        {
            // No heap ready to be used, let's create a new one
            var descriptorHeapDesc = new DescriptorHeapDesc
            {
                Flags = DescriptorHeapFlags.ShaderVisible,
                Type = heapType,
                NumDescriptors = (uint) heapSize
            };

            HResult result = GraphicsDevice.NativeDevice.CreateDescriptorHeap(in descriptorHeapDesc,
                                                                              out ComPtr<ID3D12DescriptorHeap> descriptorHeap);
            if (result.IsFailure)
                result.Throw();

            return descriptorHeap;
        }

        protected override void ResetObject(ComPtr<ID3D12DescriptorHeap> obj) { }
    }
}

#endif
