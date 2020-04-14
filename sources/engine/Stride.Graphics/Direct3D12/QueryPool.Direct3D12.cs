// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D12
using System;
using SharpDX.Direct3D12;
using Stride.Core;

namespace Stride.Graphics
{
    public partial class QueryPool
    {
        private Resource readbackBuffer;
        private Fence readbackFence;

        internal long CompletedValue;
        internal long PendingValue;
        internal QueryHeap NativeQueryHeap;

        public unsafe bool TryGetData(long[] dataArray)
        {
            // If readback has completed, return the data from the staging buffer
            if (readbackFence.CompletedValue == PendingValue)
            {
                CompletedValue = PendingValue;

                var mappedData = readbackBuffer.Map(0);
                fixed (long* dataPointer = &dataArray[0])
                {
                    Utilities.CopyMemory(new IntPtr(dataPointer), mappedData, QueryCount * 8);
                }
                readbackBuffer.Unmap(0);
                return true;
            }

            // Otherwise, queue readback
            var commandList = GraphicsDevice.NativeCopyCommandList;

            commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, null);
            commandList.ResolveQueryData(NativeQueryHeap, SharpDX.Direct3D12.QueryType.Timestamp, 0, QueryCount, readbackBuffer, 0);
            commandList.Close();

            GraphicsDevice.NativeCommandQueue.ExecuteCommandList(GraphicsDevice.NativeCopyCommandList);
            GraphicsDevice.NativeCommandQueue.Signal(readbackFence, PendingValue);

            return false;
        }

        private void Recreate()
        {
            var description = new QueryHeapDescription { Count = QueryCount };

            switch (QueryType)
            {
                case QueryType.Timestamp:
                    description.Type = QueryHeapType.Timestamp;
                    break;

                default:
                    throw new NotImplementedException();
            }
           
            NativeQueryHeap = NativeDevice.CreateQueryHeap(description);
            readbackBuffer = NativeDevice.CreateCommittedResource(new HeapProperties(HeapType.Readback), HeapFlags.None, ResourceDescription.Buffer(QueryCount * 8), ResourceStates.CopyDestination);
            readbackFence = NativeDevice.CreateFence(0, FenceFlags.None);
            CompletedValue = 0;
            PendingValue = 0;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            NativeQueryHeap.Dispose();
            readbackBuffer.Dispose();
            readbackFence.Dispose();

            base.OnDestroyed();
        }

        internal void ResetInternal()
        {
            if (CompletedValue <= readbackFence.CompletedValue)
                CompletedValue = readbackFence.CompletedValue + 1;
        }
    }
}
#endif
