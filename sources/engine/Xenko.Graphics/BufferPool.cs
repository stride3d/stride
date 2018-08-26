// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Runtime.InteropServices;

namespace Xenko.Graphics
{
    public class BufferPool : IDisposable
    {
#if XENKO_GRAPHICS_API_DIRECT3D12
        private const bool UseBufferOffsets = true;
#elif XENKO_GRAPHICS_API_VULKAN
        private const bool UseBufferOffsets = true;
#else
        private const bool UseBufferOffsets = false;
#endif

        private int constantBufferAlignment;
        public int Size;
        public IntPtr Data;

        private readonly GraphicsResourceAllocator allocator;
        private Buffer constantBuffer;
        private MappedResource mappedConstantBuffer;
        private CommandList commandList;

        private int bufferAllocationOffset;

        internal BufferPool(GraphicsResourceAllocator allocator, GraphicsDevice graphicsDevice, int size)
        {
            constantBufferAlignment = graphicsDevice.ConstantBufferDataPlacementAlignment;
            if (size % constantBufferAlignment != 0)
                throw new ArgumentException($"size needs to be a multiple of constant buffer alignment ({constantBufferAlignment})", nameof(size));

            this.allocator = allocator;

            Size = size;
            if (!UseBufferOffsets)
                Data = Marshal.AllocHGlobal(size);

            Reset();
        }

        public static BufferPool New(GraphicsResourceAllocator allocator, GraphicsDevice graphicsDevice, int size)
        {
            return new BufferPool(allocator, graphicsDevice, size);
        }

        public void Dispose()
        {
            if (UseBufferOffsets)
                allocator.ReleaseReference(constantBuffer);
            else
                Marshal.FreeHGlobal(Data);
            Data = IntPtr.Zero;
        }

        public void Map(CommandList commandList)
        {
            if (UseBufferOffsets)
            {
                using (new DefaultCommandListLock(commandList))
                {
                    this.commandList = commandList;
                    mappedConstantBuffer = commandList.MapSubresource(constantBuffer, 0, MapMode.WriteNoOverwrite);
                    Data = mappedConstantBuffer.DataBox.DataPointer;
                }
            }
        }

        public void Unmap()
        {
            if (UseBufferOffsets && mappedConstantBuffer.Resource != null)
            {
                using (new DefaultCommandListLock(commandList))
                {
                    commandList.UnmapSubresource(mappedConstantBuffer);
                    mappedConstantBuffer = new MappedResource();
                }
            }
        }

        public void Reset()
        {
            if (UseBufferOffsets)
            {
                // Release previous buffer
                if (constantBuffer != null)
                    allocator.ReleaseReference(constantBuffer);

                constantBuffer = allocator.GetTemporaryBuffer(new BufferDescription(Size, BufferFlags.ConstantBuffer, GraphicsResourceUsage.Dynamic));
            }

            bufferAllocationOffset = 0;
        }

        public bool CanAllocate(int size)
        {
            return bufferAllocationOffset + size <= Size;
        }

        public void Allocate(GraphicsDevice graphicsDevice, int size, BufferPoolAllocationType type, ref BufferPoolAllocationResult bufferPoolAllocationResult)
        {
            var result = bufferAllocationOffset;
            bufferAllocationOffset += size;

            // Align next allocation
            // Note: total Size should be a multiple of alignment, so that CanAllocate() and Allocate() Size check matches
            bufferAllocationOffset = (bufferAllocationOffset + constantBufferAlignment - 1) / constantBufferAlignment * constantBufferAlignment;

            if (bufferAllocationOffset > Size)
                throw new InvalidOperationException();

            // Map (if needed)
            if (UseBufferOffsets && mappedConstantBuffer.Resource == null)
                Map(commandList);

            bufferPoolAllocationResult.Data = Data + result;
            bufferPoolAllocationResult.Size = size;

            if (UseBufferOffsets)
            {
                bufferPoolAllocationResult.Uploaded = true;
                bufferPoolAllocationResult.Offset = result;
                bufferPoolAllocationResult.Buffer = constantBuffer;
            }
            else
            {
                bufferPoolAllocationResult.Uploaded = false;

                if (type == BufferPoolAllocationType.UsedMultipleTime)
                {
                    if (bufferPoolAllocationResult.Buffer == null || bufferPoolAllocationResult.Buffer.SizeInBytes != size)
                    {
                        // Release old buffer in case size changed
                        if (bufferPoolAllocationResult.Buffer != null)
                            bufferPoolAllocationResult.Buffer.Dispose();

                        bufferPoolAllocationResult.Buffer = Buffer.Constant.New(graphicsDevice, size, graphicsDevice.Features.HasResourceRenaming ? GraphicsResourceUsage.Dynamic : GraphicsResourceUsage.Default);
                        //bufferPoolAllocationResult.Buffer = Buffer.New(graphicsDevice, size, BufferFlags.ConstantBuffer);
                    }
                }
            }
        }
    }

    public enum BufferPoolAllocationType
    {
        /// <summary>
        /// Notify the allocator that this buffer won't be reused for much more than 1 (or few) draw calls.
        /// In practice, on older D3D11 (not 11.1) and OpenGL ES 2.0 hardware, we won't use a dedicated cbuffer.
        /// This has no effect on new API where we can bind cbuffer offsets.
        /// </summary>
        UsedOnce,

        /// <summary>
        /// Notify the allocator that this buffer will be reused for many draw calls.
        /// In practice, on older D3D11 (not 11.1) and OpenGL ES 2.0 hardware, we will use a dedicated cbuffer.
        /// This has no effect on new API where we can bind cbuffer offsets.
        /// </summary>
        UsedMultipleTime,
    }
}
