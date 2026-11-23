// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using Stride.Core;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    public partial class Buffer
    {
        internal VkBuffer NativeBuffer;
        internal VkBufferView NativeBufferView;
        internal VkAccessFlags NativeAccessMask;

        /// <summary>
        /// The number of frames a retired native allocation the GPU is done with may wait to be reused before it is destroyed.
        /// </summary>
        private const int MaxFreeAllocationAgeInFrames = 2 * GraphicsDevice.MaxFramesInFlight;

        private struct RetiredAllocation
        {
            public ulong FenceValue;
            public VkBuffer Buffer;
            public VkDeviceMemory Memory;
            public VkBufferView View;
        }

        // Allocations replaced by a discard, oldest first, each with the frame fence value after which the GPU is done with it
        private readonly Queue<RetiredAllocation> retiredAllocations = new();

        /// <summary>
        /// The number of native buffers and memory allocations this buffer created.
        /// </summary>
        internal int NativeAllocationCount;

        /// <summary>
        /// The size of the <see cref="NativeBuffer"/>: <see cref="SizeInBytes"/> rounded up to a multiple of 4, which <c>vkCmdFillBuffer</c> requires.
        /// </summary>
        internal ulong NativeSizeInBytes => ((ulong) SizeInBytes + 3) & ~3UL;

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected partial Buffer InitializeFromImpl(ref readonly BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            CheckRawBufferSize(description.SizeInBytes, viewFlags);

            bufferDescription = description;
            //nativeDescription = ConvertToNativeDescription(Description);
            ViewFlags = viewFlags;
            InitCountAndViewFormat(out elementCount, ref viewFormat);
            ViewFormat = viewFormat;
            Recreate(dataPointer);

            if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(SizeInBytes);
            }

            return this;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            lock (retiredAllocations)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes * retiredAllocations.Count);
                GraphicsDevice.RemoveBufferWithRetiredAllocations(this);

                while (retiredAllocations.Count > 0)
                {
                    var retired = retiredAllocations.Dequeue();
                    CollectNativeObjects(retired.View, retired.Buffer, retired.Memory);
                }
            }

            GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes);

            ReleaseNativeObjects();

            base.OnDestroyed(immediately);
        }

        /// <summary>
        /// Hands the native objects to the device to destroy once the GPU is done with them.
        /// </summary>
        private void ReleaseNativeObjects()
        {
            CollectNativeObjects(NativeBufferView, NativeBuffer, NativeMemory);

            NativeBufferView = VkBufferView.Null;
            NativeBuffer = VkBuffer.Null;
            NativeMemory = VkDeviceMemory.Null;
        }

        private void CollectNativeObjects(VkBufferView view, VkBuffer buffer, VkDeviceMemory memory)
        {
            if (view != VkBufferView.Null)
                GraphicsDevice.Collect(view);

            if (buffer != VkBuffer.Null)
                GraphicsDevice.Collect(buffer);

            if (memory != VkDeviceMemory.Null)
                GraphicsDevice.Collect(memory);
        }

        /// <summary>
        /// Gives the buffer new native memory with undefined contents, so that commands already recorded or in flight keep reading the old memory.
        /// </summary>
        /// <remarks>
        /// The old memory is reused by a later rename once the GPU is done with it.
        /// </remarks>
        internal void Rename()
        {
            var needsNewAllocation = true;

            lock (retiredAllocations)
            {
                // Same fence value as GraphicsDevice.Collect: the GPU is done with the old memory when the current frame completes
                var current = new RetiredAllocation
                {
                    FenceValue = GraphicsDevice.FrameFence.NextFenceValue,
                    Buffer = NativeBuffer,
                    Memory = NativeMemory,
                    View = NativeBufferView,
                };

                if (retiredAllocations.Count > 0 && GraphicsDevice.FrameFence.IsFenceCompleteInternal(retiredAllocations.Peek().FenceValue))
                {
                    var free = retiredAllocations.Dequeue();
                    NativeBuffer = free.Buffer;
                    NativeMemory = free.Memory;
                    NativeBufferView = free.View;
                    CopyFenceValue = null;
                    needsNewAllocation = false;
                }
                else
                {
                    NativeBuffer = VkBuffer.Null;
                    NativeMemory = VkDeviceMemory.Null;
                    NativeBufferView = VkBufferView.Null;
                    GraphicsDevice.RegisterBufferMemoryUsage(SizeInBytes);
                }

                retiredAllocations.Enqueue(current);

                if (retiredAllocations.Count == 1)
                    GraphicsDevice.AddBufferWithRetiredAllocations(this);

                if (!needsNewAllocation)
                    TrimFreeAllocationsLocked();
            }

            if (needsNewAllocation)
                Recreate(IntPtr.Zero, clearContents: false);
        }

        /// <summary>
        /// The number of retired native allocations the buffer keeps for reuse.
        /// </summary>
        internal int RetiredAllocationCount
        {
            get
            {
                lock (retiredAllocations)
                {
                    return retiredAllocations.Count;
                }
            }
        }

        /// <summary>
        /// Destroys the retired native allocations that stayed free for long: they are surplus, left by a burst of discards.
        /// </summary>
        /// <remarks>
        /// The oldest retired allocations are reused first.
        /// </remarks>
        internal void TrimFreeAllocations()
        {
            lock (retiredAllocations)
            {
                TrimFreeAllocationsLocked();
            }
        }

        private void TrimFreeAllocationsLocked()
        {
            var trimmed = false;
            while (retiredAllocations.Count > 0)
            {
                var oldest = retiredAllocations.Peek();
                if (oldest.FenceValue + MaxFreeAllocationAgeInFrames >= GraphicsDevice.FrameFence.LastCompletedFence)
                    break;

                retiredAllocations.Dequeue();
                CollectNativeObjects(oldest.View, oldest.Buffer, oldest.Memory);
                GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes);
                trimmed = true;
            }

            if (trimmed && retiredAllocations.Count == 0)
                GraphicsDevice.RemoveBufferWithRetiredAllocations(this);
        }

        /// <summary>
        /// Replaces the native buffer with a new one initialized with the provided data, keeping the description.
        /// </summary>
        /// <param name="dataPointer">The data to initialize the buffer with, or <see cref="IntPtr.Zero"/> for none.</param>
        public void Recreate(IntPtr dataPointer)
        {
            Recreate(dataPointer, clearContents: true);
        }

        private unsafe void Recreate(IntPtr dataPointer, bool clearContents)
        {
            var createInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.BufferCreateInfo,
                size = NativeSizeInBytes,
                flags = VkBufferCreateFlags.None
            };

            createInfo.usage |= VkBufferUsageFlags.TransferSrc;

            // We always fill using transfer
            //if (bufferDescription.Usage != GraphicsResourceUsage.Immutable)
                createInfo.usage |= VkBufferUsageFlags.TransferDst;

            if (Usage == GraphicsResourceUsage.Staging)
            {
                NativeAccessMask = VkAccessFlags.HostRead | VkAccessFlags.HostWrite;
                NativePipelineStageMask |= VkPipelineStageFlags.Host;
            }
            else
            {
                if ((ViewFlags & BufferFlags.VertexBuffer) != 0)
                {
                    createInfo.usage |= VkBufferUsageFlags.VertexBuffer;
                    NativeAccessMask |= VkAccessFlags.VertexAttributeRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexInput;
                }

                if ((ViewFlags & BufferFlags.IndexBuffer) != 0)
                {
                    createInfo.usage |= VkBufferUsageFlags.IndexBuffer;
                    NativeAccessMask |= VkAccessFlags.IndexRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexInput;
                }

                if ((ViewFlags & BufferFlags.ConstantBuffer) != 0)
                {
                    createInfo.usage |= VkBufferUsageFlags.UniformBuffer;
                    NativeAccessMask |= VkAccessFlags.UniformRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexShader | VkPipelineStageFlags.FragmentShader | VkPipelineStageFlags.ComputeShader;
                }

                // The usage follows the descriptor type the shader declares for the buffer (see VulkanConvertExtensions):
                // structured and raw buffers bind as storage buffers, typed buffers as texel buffers.
                if (IsStorageBuffer)
                {
                    createInfo.usage |= VkBufferUsageFlags.StorageBuffer;
                }
                else
                {
                    if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                        createInfo.usage |= VkBufferUsageFlags.UniformTexelBuffer;
                    if ((ViewFlags & BufferFlags.UnorderedAccess) != 0)
                        createInfo.usage |= VkBufferUsageFlags.StorageTexelBuffer;
                }

                if (IsStorageBuffer || (ViewFlags & (BufferFlags.ShaderResource | BufferFlags.UnorderedAccess)) != 0)
                {
                    NativeAccessMask |= VkAccessFlags.ShaderRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexShader | VkPipelineStageFlags.FragmentShader | VkPipelineStageFlags.ComputeShader;

                    if ((ViewFlags & BufferFlags.UnorderedAccess) != 0)
                        NativeAccessMask |= VkAccessFlags.ShaderWrite;
                }

                if ((ViewFlags & BufferFlags.ArgumentBuffer) != 0)
                {
                    createInfo.usage |= VkBufferUsageFlags.IndirectBuffer;
                    NativeAccessMask |= VkAccessFlags.IndirectCommandRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.DrawIndirect;
                }
            }

            NativeAllocationCount++;

            // Create buffer
            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkCreateBuffer(GraphicsDevice.NativeDevice, &createInfo, null, out NativeBuffer));
            SetDebugName();

            // Allocate memory
            var memoryProperties = VkMemoryPropertyFlags.DeviceLocal;
            if (bufferDescription.Usage == GraphicsResourceUsage.Staging || Usage == GraphicsResourceUsage.Dynamic)
            {
                memoryProperties = VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent;
                IsHostVisibleHeap = true;
            }

            GraphicsDevice.NativeDeviceApi.vkGetBufferMemoryRequirements(GraphicsDevice.NativeDevice, NativeBuffer, out var memoryRequirements);

            AllocateMemory(memoryProperties, memoryRequirements);

            if (NativeMemory != VkDeviceMemory.Null)
            {
                GraphicsDevice.NativeDeviceApi.vkBindBufferMemory(GraphicsDevice.NativeDevice, NativeBuffer, NativeMemory, 0);
            }

            // Without data or a clear, the contents stay undefined and nothing is submitted
            if (SizeInBytes > 0 && (dataPointer != IntPtr.Zero || clearContents))
            {
                var commandBuffer = GraphicsDevice.NativeCopyCommandPools.Value.GetObject(GraphicsDevice.CopyFence.GetCompletedValue());

                var beginInfo = new VkCommandBufferBeginInfo { sType = VkStructureType.CommandBufferBeginInfo, flags = VkCommandBufferUsageFlags.OneTimeSubmit };
                GraphicsDevice.NativeDeviceApi.vkBeginCommandBuffer(commandBuffer, &beginInfo);

                // Copy to upload buffer
                GraphicsDevice.UploadBufferLease uploadLease = null;
                if (dataPointer != IntPtr.Zero)
                {
                    if (Usage == GraphicsResourceUsage.Dynamic)
                    {
                        void* uploadMemory;
                        GraphicsDevice.NativeDeviceApi.vkMapMemory(GraphicsDevice.NativeDevice, NativeMemory, 0, (ulong) SizeInBytes, VkMemoryMapFlags.None, &uploadMemory);
                        MemoryUtilities.CopyWithAlignmentFallback(uploadMemory, (void*) dataPointer, (uint) SizeInBytes);
                        GraphicsDevice.NativeDeviceApi.vkUnmapMemory(GraphicsDevice.NativeDevice, NativeMemory);
                    }
                    else
                    {
                        var sizeInBytes = bufferDescription.SizeInBytes;
                        var uploadMemory = GraphicsDevice.AllocateUploadBuffer(sizeInBytes, out var uploadResource, out var uploadOffset, out uploadLease);

                        MemoryUtilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) dataPointer, (uint) sizeInBytes);

                        // Barrier
                        PipelineBarriers.Record(GraphicsDevice.NativeDeviceApi, commandBuffer, bufferBarriers: [
                            PipelineBarriers.BufferBarrier(uploadResource, VkPipelineStageFlags.Host, VkAccessFlags.HostWrite, VkPipelineStageFlags.Transfer, VkAccessFlags.TransferRead, (ulong) uploadOffset, (ulong) sizeInBytes)]);

                        // Copy
                        var bufferCopy = new VkBufferCopy
                        {
                            srcOffset = (uint) uploadOffset,
                            dstOffset = 0,
                            size = (uint) sizeInBytes
                        };
                        GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(commandBuffer, uploadResource, NativeBuffer, 1, &bufferCopy);
                    }
                }
                else
                {
                    GraphicsDevice.NativeDeviceApi.vkCmdFillBuffer(commandBuffer, NativeBuffer, 0, NativeSizeInBytes, 0);
                }

                // Barrier
                var dstStages = CommandList.FixStagesForAccess(NativePipelineStageMask, NativeAccessMask);
                PipelineBarriers.Record(GraphicsDevice.NativeDeviceApi, commandBuffer, bufferBarriers: [
                    PipelineBarriers.BufferBarrier(NativeBuffer, VkPipelineStageFlags.Transfer, VkAccessFlags.TransferWrite, dstStages, NativeAccessMask)]);

                // Close and submit
                GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkEndCommandBuffer(commandBuffer));

                var copyFenceValue = GraphicsDevice.ExecuteAndWaitCopyQueueGPU(commandBuffer);
                GraphicsDevice.NativeCopyCommandPools.Value.RecycleObject(GraphicsDevice.CopyFence.NextFenceValue, commandBuffer);

                // Done writing the upload buffer: a replaced one is collected only once the GPU is done with it
                if (uploadLease != null)
                    GraphicsDevice.ReleaseUploadBuffer(uploadLease);

                // Make sure any subsequent CPU access (i.e. MapSubresource) will wait for copy command list to be finished
                CopyFenceValue = copyFenceValue;
            }
            else
            {
                CopyFenceValue = null;
            }

            if (SizeInBytes > 0)
                InitializeViews();
        }

        /// <summary>
        /// Initializes the views.
        /// </summary>
        private void InitializeViews()
        {
            // Only typed buffers (Buffer<T>/RWBuffer<T>) need a VkBufferView: they bind as
            // VK_DESCRIPTOR_TYPE_*_TEXEL_BUFFER, which requires a real VkFormat. Storage buffers are bound directly.
            if (!IsStorageBuffer
                && (ViewFlags & (BufferFlags.ShaderResource | BufferFlags.UnorderedAccess)) != 0
                && ViewFormat != PixelFormat.None)
            {
                NativeBufferView = GetShaderResourceView(ViewFormat);
            }
        }

        /// <summary>
        /// Gets a value indicating whether the buffer binds as a <c>VK_DESCRIPTOR_TYPE_STORAGE_BUFFER</c>:
        /// structured buffers and raw buffers (<c>ByteAddressBuffer</c>), which have no texel format.
        /// </summary>
        private bool IsStorageBuffer => (ViewFlags & (BufferFlags.StructuredBuffer | BufferFlags.RawBuffer)) != 0;

        internal unsafe VkBufferView GetShaderResourceView(PixelFormat viewFormat)
        {
            var createInfo = new VkBufferViewCreateInfo
            {
                sType = VkStructureType.BufferViewCreateInfo,
                buffer = NativeBuffer,
                format = viewFormat == PixelFormat.None ? VkFormat.Undefined : VulkanConvertExtensions.ConvertPixelFormat(viewFormat),
                range = (ulong) SizeInBytes, // this.ElementCount
                //view = (Description.BufferFlags & BufferFlags.RawBuffer) != 0 ? VkBufferViewType.Raw : VkBufferViewType.Formatted,
            };

            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkCreateBufferView(GraphicsDevice.NativeDevice, &createInfo, allocator: null, out var bufferView));
            return bufferView;
        }

        private void InitCountAndViewFormat(out int count, ref PixelFormat viewFormat)
        {
            if (Description.StructureByteStride == 0)
            {
                // TODO: The way to calculate the count is not always correct depending on the ViewFlags...etc.
                if ((ViewFlags & BufferFlags.RawBuffer) != 0)
                {
                    count = Description.SizeInBytes / sizeof(int);
                }
                else if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                {
                    count = Description.SizeInBytes / viewFormat.SizeInBytes;
                }
                else
                {
                    count = 0;
                }
            }
            else
            {
                // For structured buffer
                count = Description.SizeInBytes / Description.StructureByteStride;
                viewFormat = PixelFormat.None;
            }
        }
    }
}
#endif
