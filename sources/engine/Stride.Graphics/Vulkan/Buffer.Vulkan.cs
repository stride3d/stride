// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
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
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected partial Buffer InitializeFromImpl(ref readonly BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
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
            GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes);

            if (NativeBufferView != VkBufferView.Null)
            {
                GraphicsDevice.Collect(NativeBufferView);
                NativeBufferView = VkBufferView.Null;
            }

            if (NativeBuffer != VkBuffer.Null)
            {
                GraphicsDevice.Collect(NativeBuffer);
                NativeBuffer = VkBuffer.Null;
            }

            if (NativeMemory != VkDeviceMemory.Null)
            {
                GraphicsDevice.Collect(NativeMemory);
                NativeMemory = VkDeviceMemory.Null;
            }

            base.OnDestroyed(immediately);
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage == GraphicsResourceUsage.Immutable
                || Description.Usage == GraphicsResourceUsage.Default)
                return false;

            Recreate(IntPtr.Zero);

            return true;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public unsafe void Recreate(IntPtr dataPointer)
        {
            var createInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.BufferCreateInfo,
                size = (ulong) bufferDescription.SizeInBytes,
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
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexShader | VkPipelineStageFlags.FragmentShader;
                }

                if ((ViewFlags & BufferFlags.StructuredBuffer) != 0)
                {
                    createInfo.usage |= VkBufferUsageFlags.StorageBuffer;
                    NativeAccessMask |= VkAccessFlags.UniformRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexShader | VkPipelineStageFlags.FragmentShader;

                    if ((ViewFlags & BufferFlags.UnorderedAccess) != 0)
                    {
                        NativeAccessMask |= VkAccessFlags.ShaderWrite;
                    }
                }

                if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                {
                    createInfo.usage |= VkBufferUsageFlags.UniformTexelBuffer;
                    NativeAccessMask |= VkAccessFlags.ShaderRead;
                    NativePipelineStageMask |= VkPipelineStageFlags.VertexShader | VkPipelineStageFlags.FragmentShader;

                    if ((ViewFlags & BufferFlags.UnorderedAccess) != 0)
                    {
                        createInfo.usage |= VkBufferUsageFlags.StorageTexelBuffer;
                        NativeAccessMask |= VkAccessFlags.ShaderWrite;
                    }
                }
            }

            // Create buffer
            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkCreateBuffer(GraphicsDevice.NativeDevice, &createInfo, null, out NativeBuffer));

            // Allocate memory
            var memoryProperties = VkMemoryPropertyFlags.DeviceLocal;
            if (bufferDescription.Usage == GraphicsResourceUsage.Staging || Usage == GraphicsResourceUsage.Dynamic)
            {
                memoryProperties = VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent;
            }

            GraphicsDevice.NativeDeviceApi.vkGetBufferMemoryRequirements(GraphicsDevice.NativeDevice, NativeBuffer, out var memoryRequirements);

            AllocateMemory(memoryProperties, memoryRequirements);

            if (NativeMemory != VkDeviceMemory.Null)
            {
                GraphicsDevice.NativeDeviceApi.vkBindBufferMemory(GraphicsDevice.NativeDevice, NativeBuffer, NativeMemory, 0);
            }

            if (SizeInBytes > 0)
            {
                var commandBuffer = GraphicsDevice.NativeCopyCommandPools.Value.GetObject(GraphicsDevice.CopyFence.GetCompletedValue());

                var beginInfo = new VkCommandBufferBeginInfo { sType = VkStructureType.CommandBufferBeginInfo, flags = VkCommandBufferUsageFlags.OneTimeSubmit };
                GraphicsDevice.NativeDeviceApi.vkBeginCommandBuffer(commandBuffer, &beginInfo);

                // Copy to upload buffer
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
                        var uploadMemory = GraphicsDevice.AllocateUploadBuffer(sizeInBytes, out var uploadResource, out var uploadOffset);

                        MemoryUtilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) dataPointer, (uint) sizeInBytes);

                        // Barrier
                        var memoryBarrier = new VkBufferMemoryBarrier(uploadResource, VkAccessFlags.HostWrite, VkAccessFlags.TransferRead, (ulong) uploadOffset, (ulong) sizeInBytes);
                        GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 1, &memoryBarrier, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);

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
                    GraphicsDevice.NativeDeviceApi.vkCmdFillBuffer(commandBuffer, NativeBuffer, 0, (uint) bufferDescription.SizeInBytes, 0);
                }

                // Barrier
                var bufferMemoryBarrier = new VkBufferMemoryBarrier(NativeBuffer, VkAccessFlags.TransferWrite, NativeAccessMask);
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.AllCommands, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 1, &bufferMemoryBarrier, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);

                // Close and submit
                GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkEndCommandBuffer(commandBuffer));

                var copyFenceValue = GraphicsDevice.ExecuteAndWaitCopyQueueGPU(commandBuffer);
                GraphicsDevice.NativeCopyCommandPools.Value.RecycleObject(GraphicsDevice.CopyFence.NextFenceValue, commandBuffer);

                // Make sure any subsequent CPU access (i.e. MapSubresource) will wait for copy command list to be finished
                CopyFenceValue = copyFenceValue;

                InitializeViews();
            }
        }

        /// <summary>
        /// Initializes the views.
        /// </summary>
        private void InitializeViews()
        {
            var viewFormat = ViewFormat;

            if ((ViewFlags & BufferFlags.RawBuffer) != 0)
            {
                viewFormat = PixelFormat.R32_Typeless;
            }

            if ((ViewFlags & (BufferFlags.ShaderResource | BufferFlags.UnorderedAccess)) != 0)
            {
                NativeBufferView = GetShaderResourceView(viewFormat);
            }
        }

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
