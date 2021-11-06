// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;

using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
using Stride.Core;
using Vk = Silk.NET.Vulkan;


namespace Stride.Graphics
{
    public partial class Buffer
    {
        internal Vk.Buffer NativeBuffer;
        internal BufferView NativeBufferView;
        internal AccessFlags NativeAccessMask;

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected Buffer InitializeFromImpl(BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            //nativeDescription = ConvertToNativeDescription(Description);
            ViewFlags = viewFlags;
            InitCountAndViewFormat(out this.elementCount, ref viewFormat);
            ViewFormat = viewFormat;
            Recreate(dataPointer);

            if (GraphicsDevice != null)
            {
                GraphicsDevice.RegisterBufferMemoryUsage(SizeInBytes);
            }

            return this;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice.RegisterBufferMemoryUsage(-SizeInBytes);

            if (NativeBufferView.Handle != 0)
            {
                GraphicsDevice.Collect(NativeBufferView);
                NativeBufferView.Handle = 0;
            }

            if (NativeBuffer.Handle != 0)
            {
                GraphicsDevice.Collect(NativeBuffer);
                NativeBuffer.Handle = 0;
            }

            if (NativeMemory.Handle == 0)
            {
                GraphicsDevice.Collect(NativeMemory);
                NativeMemory.Handle = 0;
            }

            base.OnDestroyed();
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
            var createInfo = new BufferCreateInfo
            {
                SType = StructureType.BufferCreateInfo,
                Size = (ulong)bufferDescription.SizeInBytes,
                Flags = 0,
            };

            createInfo.Usage |= BufferUsageFlags.BufferUsageTransferSrcBit;

            // We always fill using transfer
            //if (bufferDescription.Usage != GraphicsResourceUsage.Immutable)
                createInfo.Usage |= BufferUsageFlags.BufferUsageTransferDstBit;

            if (Usage == GraphicsResourceUsage.Staging)
            {
                NativeAccessMask = AccessFlags.AccessHostReadBit | AccessFlags.AccessHostWriteBit;
                NativePipelineStageMask |= PipelineStageFlags.PipelineStageHostBit;
            }
            else
            {
                if ((ViewFlags & BufferFlags.VertexBuffer) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.BufferUsageVertexBufferBit;
                    NativeAccessMask |= AccessFlags.AccessVertexAttributeReadBit;
                    NativePipelineStageMask |= PipelineStageFlags.PipelineStageVertexInputBit;
                }

                if ((ViewFlags & BufferFlags.IndexBuffer) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.BufferUsageIndexBufferBit;
                    NativeAccessMask |= AccessFlags.AccessIndexReadBit;
                    NativePipelineStageMask |= PipelineStageFlags.PipelineStageVertexInputBit;
                }

                if ((ViewFlags & BufferFlags.ConstantBuffer) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.BufferUsageUniformBufferBit;
                    NativeAccessMask |= AccessFlags.AccessUniformReadBit;
                    NativePipelineStageMask |= PipelineStageFlags.PipelineStageVertexShaderBit | PipelineStageFlags.PipelineStageFragmentShaderBit;
                }

                if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.BufferUsageUniformTexelBufferBit;
                    NativeAccessMask |= AccessFlags.AccessShaderReadBit;
                    NativePipelineStageMask |= PipelineStageFlags.PipelineStageVertexShaderBit | PipelineStageFlags.PipelineStageFragmentShaderBit;

                    if ((ViewFlags & BufferFlags.UnorderedAccess) != 0)
                    {
                        createInfo.Usage |= BufferUsageFlags.BufferUsageStorageTexelBufferBit;
                        NativeAccessMask |= AccessFlags.AccessShaderWriteBit;
                    }
                }
            }

            // Create buffer
            GetApi().CreateBuffer(GraphicsDevice.NativeDevice, &createInfo, null, out NativeBuffer);

            // Allocate memory
            var memoryProperties = MemoryPropertyFlags.MemoryPropertyDeviceLocalBit;
            if (bufferDescription.Usage == GraphicsResourceUsage.Staging || Usage == GraphicsResourceUsage.Dynamic)
            { 
                memoryProperties = MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit;
            }

            GetApi().GetBufferMemoryRequirements(GraphicsDevice.NativeDevice, NativeBuffer, out var memoryRequirements);

            AllocateMemory(memoryProperties, memoryRequirements);

            if (NativeMemory.Handle != 0)
            {
                GetApi().BindBufferMemory(GraphicsDevice.NativeDevice, NativeBuffer, NativeMemory, 0);
            }

            if (SizeInBytes > 0)
            {
                // Begin copy command buffer
                var commandBufferAllocateInfo = new CommandBufferAllocateInfo
                {
                    SType = StructureType.CommandBufferAllocateInfo,
                    CommandPool = GraphicsDevice.NativeCopyCommandPools.Value,
                    CommandBufferCount = 1,
                    Level = CommandBufferLevel.Primary
                };
                CommandBuffer commandBuffer;

                GetApi().AllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocateInfo, &commandBuffer);

                var beginInfo = new CommandBufferBeginInfo { SType = StructureType.CommandBufferBeginInfo, Flags = CommandBufferUsageFlags.CommandBufferUsageOneTimeSubmitBit };
                GetApi().BeginCommandBuffer(commandBuffer, &beginInfo);

                // Copy to upload buffer
                if (dataPointer != IntPtr.Zero)
                {
                    if (Usage == GraphicsResourceUsage.Dynamic)
                    {
                        void* uploadMemory;
                        GetApi().MapMemory(GraphicsDevice.NativeDevice, NativeMemory, 0, (ulong)SizeInBytes, 0, &uploadMemory);
                        Utilities.CopyMemory((IntPtr)uploadMemory, dataPointer, SizeInBytes);
                        GetApi().UnmapMemory(GraphicsDevice.NativeDevice, NativeMemory);
                    }
                    else
                    {
                        var sizeInBytes = bufferDescription.SizeInBytes;
                        Vk.Buffer uploadResource;
                        int uploadOffset;
                        var uploadMemory = GraphicsDevice.AllocateUploadBuffer(sizeInBytes, out uploadResource, out uploadOffset);

                        Utilities.CopyMemory(uploadMemory, dataPointer, sizeInBytes);

                        // Barrier
                        var memoryBarrier = new BufferMemoryBarrier
                        {
                            Buffer = uploadResource,
                            SrcAccessMask = AccessFlags.AccessHostWriteBit,
                            DstAccessMask = AccessFlags.AccessTransferReadBit,
                            Offset = (ulong)uploadOffset,
                            Size = (ulong)sizeInBytes
                        };
                        GetApi().CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageHostBit, PipelineStageFlags.PipelineStageTransferBit, 0, 0, null, 1, &memoryBarrier, 0, null);

                        // Copy
                        var bufferCopy = new BufferCopy
                        {
                            SrcOffset = (uint)uploadOffset,
                            DstOffset = 0,
                            Size = (uint)sizeInBytes
                        };
                        GetApi().CmdCopyBuffer(commandBuffer, uploadResource, NativeBuffer, 1, &bufferCopy);
                    }
                }
                else
                {
                    GetApi().CmdFillBuffer(commandBuffer, NativeBuffer, 0, (uint)bufferDescription.SizeInBytes, 0);
                }

                // Barrier
                var bufferMemoryBarrier = new BufferMemoryBarrier
                {
                    Buffer = NativeBuffer, 
                    SrcAccessMask = AccessFlags.AccessTransferWriteBit, 
                    DstAccessMask = NativeAccessMask
                };
                GetApi().CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageAllCommandsBit, 0, 0, null, 1, &bufferMemoryBarrier, 0, null);

                // Close and submit
                GetApi().EndCommandBuffer(commandBuffer);

                var submitInfo = new SubmitInfo
                {
                    SType = StructureType.SubmitInfo,
                    CommandBufferCount = 1,
                    PCommandBuffers = &commandBuffer,
                };

                lock (GraphicsDevice.QueueLock)
                {
                    GetApi().QueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, new Fence());
                    GetApi().QueueWaitIdle(GraphicsDevice.NativeCommandQueue);
                    //commandBuffer.Reset(CommandBufferResetFlags.None);
                }

                GetApi().FreeCommandBuffers(GraphicsDevice.NativeDevice, GraphicsDevice.NativeCopyCommandPools.Value, 1, &commandBuffer);

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

        internal unsafe BufferView GetShaderResourceView(PixelFormat viewFormat)
        {
            var createInfo = new BufferViewCreateInfo
            {
                SType = StructureType.BufferViewCreateInfo,
                Buffer = NativeBuffer,
                Format = viewFormat == PixelFormat.None ? Format.Undefined : VulkanConvertExtensions.ConvertPixelFormat(viewFormat),
                Range = (ulong)SizeInBytes, // this.ElementCount
                //view = (Description.BufferFlags & BufferFlags.RawBuffer) != 0 ? BufferViewType.Raw : BufferViewType.Formatted,
            };

            GetApi().CreateBufferView(GraphicsDevice.NativeDevice, &createInfo, null, out var bufferView);
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
                    count = Description.SizeInBytes / viewFormat.SizeInBytes();
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
