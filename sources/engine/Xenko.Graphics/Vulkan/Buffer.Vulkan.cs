// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;

using SharpVulkan;
using Xenko.Core;

namespace Xenko.Graphics
{
    public partial class Buffer
    {
        internal SharpVulkan.Buffer NativeBuffer;
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

            if (NativeBufferView != BufferView.Null)
            {
                GraphicsDevice.Collect(NativeBufferView);
                NativeBufferView = BufferView.Null;
            }

            if (NativeBuffer != SharpVulkan.Buffer.Null)
            {
                GraphicsDevice.Collect(NativeBuffer);
                NativeBuffer = SharpVulkan.Buffer.Null;
            }

            if (NativeMemory != DeviceMemory.Null)
            {
                GraphicsDevice.Collect(NativeMemory);
                NativeMemory = DeviceMemory.Null;
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
                StructureType = StructureType.BufferCreateInfo,
                Size = (ulong)bufferDescription.SizeInBytes,
                Flags = BufferCreateFlags.None,
            };

            createInfo.Usage |= BufferUsageFlags.TransferSource;

            // We always fill using transfer
            //if (bufferDescription.Usage != GraphicsResourceUsage.Immutable)
                createInfo.Usage |= BufferUsageFlags.TransferDestination;

            if (Usage == GraphicsResourceUsage.Staging)
            {
                NativeAccessMask = AccessFlags.HostRead | AccessFlags.HostWrite;
                NativePipelineStageMask |= PipelineStageFlags.Host;
            }
            else
            {
                if ((ViewFlags & BufferFlags.VertexBuffer) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.VertexBuffer;
                    NativeAccessMask |= AccessFlags.VertexAttributeRead;
                    NativePipelineStageMask |= PipelineStageFlags.VertexInput;
                }

                if ((ViewFlags & BufferFlags.IndexBuffer) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.IndexBuffer;
                    NativeAccessMask |= AccessFlags.IndexRead;
                    NativePipelineStageMask |= PipelineStageFlags.VertexInput;
                }

                if ((ViewFlags & BufferFlags.ConstantBuffer) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.UniformBuffer;
                    NativeAccessMask |= AccessFlags.UniformRead;
                    NativePipelineStageMask |= PipelineStageFlags.VertexShader | PipelineStageFlags.FragmentShader;
                }

                if ((ViewFlags & BufferFlags.ShaderResource) != 0)
                {
                    createInfo.Usage |= BufferUsageFlags.UniformTexelBuffer;
                    NativeAccessMask |= AccessFlags.ShaderRead;
                    NativePipelineStageMask |= PipelineStageFlags.VertexShader | PipelineStageFlags.FragmentShader;

                    if ((ViewFlags & BufferFlags.UnorderedAccess) != 0)
                    {
                        createInfo.Usage |= BufferUsageFlags.StorageTexelBuffer;
                        NativeAccessMask |= AccessFlags.ShaderWrite;
                    }
                }
            }

            // Create buffer
            NativeBuffer = GraphicsDevice.NativeDevice.CreateBuffer(ref createInfo);

            // Allocate memory
            var memoryProperties = MemoryPropertyFlags.DeviceLocal;
            if (bufferDescription.Usage == GraphicsResourceUsage.Staging || Usage == GraphicsResourceUsage.Dynamic)
            { 
                memoryProperties = MemoryPropertyFlags.HostVisible | MemoryPropertyFlags.HostCoherent;
            }

            MemoryRequirements memoryRequirements;
            GraphicsDevice.NativeDevice.GetBufferMemoryRequirements(NativeBuffer, out memoryRequirements);

            AllocateMemory(memoryProperties, memoryRequirements);

            if (NativeMemory != DeviceMemory.Null)
            {
                GraphicsDevice.NativeDevice.BindBufferMemory(NativeBuffer, NativeMemory, 0);
            }

            if (SizeInBytes > 0)
            {
                // Begin copy command buffer
                var commandBufferAllocateInfo = new CommandBufferAllocateInfo
                {
                    StructureType = StructureType.CommandBufferAllocateInfo,
                    CommandPool = GraphicsDevice.NativeCopyCommandPool,
                    CommandBufferCount = 1,
                    Level = CommandBufferLevel.Primary
                };
                CommandBuffer commandBuffer;

                lock (GraphicsDevice.QueueLock)
                {
                    GraphicsDevice.NativeDevice.AllocateCommandBuffers(ref commandBufferAllocateInfo, &commandBuffer);
                }

                var beginInfo = new CommandBufferBeginInfo { StructureType = StructureType.CommandBufferBeginInfo, Flags = CommandBufferUsageFlags.OneTimeSubmit };
                commandBuffer.Begin(ref beginInfo);

                // Copy to upload buffer
                if (dataPointer != IntPtr.Zero)
                {
                    if (Usage == GraphicsResourceUsage.Dynamic)
                    {
                        var uploadMemory = GraphicsDevice.NativeDevice.MapMemory(NativeMemory, 0, (ulong)SizeInBytes, MemoryMapFlags.None);
                        Utilities.CopyMemory(uploadMemory, dataPointer, SizeInBytes);
                        GraphicsDevice.NativeDevice.UnmapMemory(NativeMemory);
                    }
                    else
                    {
                        var sizeInBytes = bufferDescription.SizeInBytes;
                        SharpVulkan.Buffer uploadResource;
                        int uploadOffset;
                        var uploadMemory = GraphicsDevice.AllocateUploadBuffer(sizeInBytes, out uploadResource, out uploadOffset);

                        Utilities.CopyMemory(uploadMemory, dataPointer, sizeInBytes);

                        // Barrier
                        var memoryBarrier = new BufferMemoryBarrier(uploadResource, AccessFlags.HostWrite, AccessFlags.TransferRead, (ulong)uploadOffset, (ulong)sizeInBytes);
                        commandBuffer.PipelineBarrier(PipelineStageFlags.Host, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, 1, &memoryBarrier, 0, null);

                        // Copy
                        var bufferCopy = new BufferCopy
                        {
                            SourceOffset = (uint)uploadOffset,
                            DestinationOffset = 0,
                            Size = (uint)sizeInBytes
                        };
                        commandBuffer.CopyBuffer(uploadResource, NativeBuffer, 1, &bufferCopy);
                    }
                }
                else
                {
                    commandBuffer.FillBuffer(NativeBuffer, 0, (uint)bufferDescription.SizeInBytes, 0);
                }

                // Barrier
                var bufferMemoryBarrier = new BufferMemoryBarrier(NativeBuffer, AccessFlags.TransferWrite, NativeAccessMask);
                commandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, PipelineStageFlags.AllCommands, DependencyFlags.None, 0, null, 1, &bufferMemoryBarrier, 0, null);

                // Close and submit
                commandBuffer.End();

                var submitInfo = new SubmitInfo
                {
                    StructureType = StructureType.SubmitInfo,
                    CommandBufferCount = 1,
                    CommandBuffers = new IntPtr(&commandBuffer),
                };

                lock (GraphicsDevice.PresentLock) {
                    lock (GraphicsDevice.QueueLock)
                    {
                        GraphicsDevice.NativeCommandQueue.Submit(1, &submitInfo, Fence.Null);
                        GraphicsDevice.NativeCommandQueue.WaitIdle();
                        GraphicsDevice.NativeDevice.FreeCommandBuffers(GraphicsDevice.NativeCopyCommandPool, 1, &commandBuffer);
                    }
                }

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
                StructureType = StructureType.BufferViewCreateInfo,
                Buffer = NativeBuffer,
                Format = viewFormat == PixelFormat.None ? Format.Undefined : VulkanConvertExtensions.ConvertPixelFormat(viewFormat),
                Range = (ulong)SizeInBytes, // this.ElementCount
                //View = (Description.BufferFlags & BufferFlags.RawBuffer) != 0 ? BufferViewType.Raw : BufferViewType.Formatted
            };

            return GraphicsDevice.NativeDevice.CreateBufferView(ref createInfo);
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
