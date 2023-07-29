// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using Silk.NET.Direct3D12;

namespace Stride.Graphics
{
    public unsafe partial struct CompiledCommandList
    {
        internal CommandList Builder;
        internal ID3D12GraphicsCommandList* NativeCommandList;
        internal ID3D12CommandAllocator* NativeCommandAllocator;
        internal List<HeapPtr> SrvHeaps;
        internal List<HeapPtr> SamplerHeaps;
        internal List<GraphicsResource> StagingResources;
    }

    #region HeapPtr structure

    // Ancillary struct to store a Descriptor Heap without messing with its reference count (as ComPtr<T> does).
    internal readonly unsafe record struct HeapPtr(ID3D12DescriptorHeap* Heap) : IDisposable
    {
        public static implicit operator ID3D12DescriptorHeap*(HeapPtr heapPtr) => heapPtr.Heap;
        public static implicit operator HeapPtr(ID3D12DescriptorHeap* heap) => new(heap);

        public void Dispose()
        {
            if (Heap != null)
                Heap->Release();
        }
    }

    #endregion
}

#endif
