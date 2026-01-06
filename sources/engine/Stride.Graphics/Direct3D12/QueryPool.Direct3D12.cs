// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Runtime.CompilerServices;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

using Stride.Core;
using Stride.Core.UnsafeExtensions;

using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class QueryPool
    {
        private ID3D12Resource* readbackBuffer;
        private ID3D12Fence* readbackFence;

        private ID3D12QueryHeap* nativeQueryHeap;

        /// <summary>
        ///   Gets the internal Direct3D 12 Query Heap.
        /// </summary>
        /// <remarks>
        ///   If any of the references is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D12QueryHeap> NativeQueryHeap => ToComPtr(nativeQueryHeap);

        /// <summary>
        ///   The fence value of when the read-back of Queries data has completed.
        /// </summary>
        internal ulong CompletedValue;
        /// <summary>
        ///   The fence value signaled for a pending read-back of Queries data.
        /// </summary>
        internal ulong PendingValue;


        /// <summary>
        ///   Attempts to retrieve data from the in-flight GPU queries.
        /// </summary>
        /// <param name="dataArray">
        ///   An array of <see langword="long"/> values to be populated with the retrieved data. The array must have a length
        ///   equal to the number of queries performed (<see cref="QueryCount"/>).
        /// </param>
        /// <returns><see langword="true"/> if all data queries succeed; otherwise, <see langword="false"/>.</returns>
        /// <remarks>
        ///   This method tries to perform reads for the multiple GPU queries in the pool and populates the provided array
        ///   with the results. If any query fails, the method returns <see langword="false"/> and the array may contain
        ///   partial or uninitialized data.
        /// </remarks>
        public unsafe bool TryGetData(long[] dataArray)
        {
            HResult result;

            // If readback has completed, return the data from the staging buffer
            if (readbackFence->GetCompletedValue() == PendingValue)
            {
                CompletedValue = PendingValue;

                Silk.NET.Direct3D12.Range range = default;
                void* mappedData = null;

                result = readbackBuffer->Map(Subresource: 0, in range, ref mappedData);

                if (result.IsFailure)
                    result.Throw();

                ref var destData = ref dataArray.AsSpan().Cast<long, byte>().GetReference();
                ref var srcData = ref Unsafe.AsRef<byte>(mappedData);

                MemoryUtilities.CopyWithAlignmentFallback(ref destData, ref srcData, (uint) QueryCount * sizeof(long));

                readbackBuffer->Unmap(Subresource: 0, pWrittenRange: in range);
                return true;
            }

            // Otherwise, queue readback
            var commandList = GraphicsDevice.NativeCopyCommandList;

            lock (GraphicsDevice.NativeCopyCommandListLock)
            {
                var nullPipelineState = NullComPtr<ID3D12PipelineState>();
                result = commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, nullPipelineState);

                if (result.IsFailure)
                    result.Throw();

                commandList.ResolveQueryData(nativeQueryHeap, Silk.NET.Direct3D12.QueryType.Timestamp,
                                             StartIndex: 0, (uint) QueryCount, readbackBuffer, AlignedDestinationBufferOffset: 0);

                result = commandList.Close();

                if (result.IsFailure)
                    result.Throw();

                var copyCommandList = commandList.AsComPtr<ID3D12GraphicsCommandList, ID3D12CommandList>();
                var commandQueue = GraphicsDevice.NativeCommandQueue;
                commandQueue.ExecuteCommandLists(NumCommandLists: 1, ref copyCommandList);

                result = commandQueue.Signal(readbackFence, PendingValue);

                if (result.IsFailure)
                    result.Throw();
            }

            return false;
        }

        /// <summary>
        ///   Implementation in Direct3D 12 that recreates the queries in the pool.
        /// </summary>
        /// <exception cref="NotImplementedException">
        ///   Only GPU queries of type <see cref="QueryType.Timestamp"/> are supported.
        /// </exception>
        private unsafe partial void Recreate()
        {
            var description = new QueryHeapDesc
            {
                Count = (uint) QueryCount,
                Type = QueryType switch
                {
                    QueryType.Timestamp => QueryHeapType.Timestamp,

                    _ => throw new NotImplementedException($"Query type {QueryType} not supported")
                }
            };

            HResult result = NativeDevice.CreateQueryHeap(in description, out ComPtr<ID3D12QueryHeap> queryHeap);

            if (result.IsFailure)
                result.Throw();

            nativeQueryHeap = queryHeap;

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

            var heap = new HeapProperties { Type = HeapType.Readback };
            result = NativeDevice.CreateCommittedResource(in heap, HeapFlags.None, in readbackBufferDesc, ResourceStates.CopyDest,
                                                          pOptimizedClearValue: null, out ComPtr<ID3D12Resource> resource);
            if (result.IsFailure)
                result.Throw();

            readbackBuffer = resource;

            result = NativeDevice.CreateFence(InitialValue: 0, FenceFlags.None, out ComPtr<ID3D12Fence> fence);

            if (result.IsFailure)
                result.Throw();

            readbackFence = fence;

            CompletedValue = 0;
            PendingValue = 0;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            SafeRelease(ref nativeQueryHeap);
            SafeRelease(ref readbackBuffer);
            SafeRelease(ref readbackFence);

            base.OnDestroyed(immediately);
        }

        /// <summary>
        ///   Resets the internal state to ensure the completed value is ahead of the current readback fence value.
        /// </summary>
        internal void ResetInternal()
        {
            if (CompletedValue <= readbackFence->GetCompletedValue())
                CompletedValue = readbackFence->GetCompletedValue() + 1;
        }
    }
}

#endif
