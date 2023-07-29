// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

namespace Stride.Graphics
{
    public unsafe partial class QueryPool
    {
        private ID3D12Resource* readbackBuffer;
        private ID3D12Fence* readbackFence;

        internal ulong CompletedValue;
        internal ulong PendingValue;
        internal ID3D12QueryHeap* NativeQueryHeap;

        public unsafe bool TryGetData(long[] dataArray)
        {
            HResult result;

            // If readback has completed, return the data from the staging buffer
            if (readbackFence->GetCompletedValue() == PendingValue)
            {
                CompletedValue = PendingValue;

                Silk.NET.Direct3D12.Range range = default;
                void* mappedData;

                result = readbackBuffer->Map(Subresource: 0, in range, &mappedData);

                if (result.IsFailure)
                    result.Throw();

                ref var destData = ref MemoryMarshal.Cast<long, byte>(dataArray.AsSpan())[0];
                ref var srcData = ref Unsafe.AsRef<byte>(mappedData);

                //Unsafe.CopyBlockUnaligned(ref destData, ref srcData, byteCount: (uint) QueryCount * sizeof(long));
                Core.Utilities.CopyWithAlignmentFallback(dataPointer, mappedData, (uint) QueryCount * sizeof(long));

                readbackBuffer->Unmap(Subresource: 0, pWrittenRange: in range);
                return true;
            }

            // Otherwise, queue readback
            var commandList = GraphicsDevice.NativeCopyCommandList;

            result = commandList->Reset(GraphicsDevice.NativeCopyCommandAllocator, pInitialState: null);

            if (result.IsFailure)
                result.Throw();

            commandList->ResolveQueryData(NativeQueryHeap, Silk.NET.Direct3D12.QueryType.Timestamp,
                                          StartIndex: 0, (uint) QueryCount, readbackBuffer, AlignedDestinationBufferOffset: 0);

            result = commandList->Close();

            if (result.IsFailure)
                result.Throw();

            var commandQueue = GraphicsDevice.NativeCommandQueue;
            commandQueue->ExecuteCommandLists(NumCommandLists: 1, (ID3D12CommandList**) GraphicsDevice.NativeCopyCommandList);

            result = commandQueue->Signal(readbackFence, PendingValue);

            if (result.IsFailure)
                result.Throw();

            return false;
        }

        private void Recreate()
        {
            var description = new QueryHeapDesc
            {
                Count = (uint) QueryCount,
                Type = QueryType switch
                {
                    QueryType.Timestamp => QueryHeapType.Timestamp,
                    _ => throw new NotImplementedException()
                }
            };

            ID3D12QueryHeap* queryHeap;
            HResult result = NativeDevice->CreateQueryHeap(description, SilkMarshal.GuidPtrOf<ID3D12QueryHeap>(), (void**) &queryHeap);

            if (result.IsFailure)
                result.Throw();

            var readbackBufferDesc = new ResourceDesc
            {
                Dimension = ResourceDimension.Buffer,
                Width = (uint) QueryCount * sizeof(long),
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Alignment = 0,

                SampleDesc = { Count = 1, Quality = 0 },

                Format = Format.FormatUnknown,
                Layout = TextureLayout.LayoutRowMajor,
                Flags = ResourceFlags.None
            };

            ID3D12Resource* resource;
            var heap = new HeapProperties { Type = HeapType.Readback };
            result = NativeDevice->CreateCommittedResource(heap, HeapFlags.None, readbackBufferDesc, ResourceStates.CopyDest,
                                                           pOptimizedClearValue: null, SilkMarshal.GuidPtrOf<ID3D12Resource>(),
                                                           (void**) &resource);
            if (result.IsFailure)
                result.Throw();

            readbackBuffer = resource;

            ID3D12Fence* fence;
            result = NativeDevice->CreateFence(InitialValue: 0, FenceFlags.None, SilkMarshal.GuidPtrOf<ID3D12Fence>(),
                                               (void**) &fence);
            if (result.IsFailure)
                result.Throw();

            readbackFence = fence;

            CompletedValue = 0;
            PendingValue = 0;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            if (NativeQueryHeap is not null)
                NativeQueryHeap->Release();

            if (readbackBuffer is not null)
                readbackBuffer->Release();

            if (readbackFence is not null)
                readbackFence->Release();

            base.OnDestroyed();
        }

        internal void ResetInternal()
        {
            if (CompletedValue <= readbackFence->GetCompletedValue())
                CompletedValue = readbackFence->GetCompletedValue() + 1;
        }
    }
}

#endif
