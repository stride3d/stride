// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Linq;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public partial class Texture
    {
        // Note: block size for compressed formats
        internal int TexturePixelSize => Format.SizeInBytes();

        internal const int TextureSubresourceAlignment = 4;
        internal const int TextureRowPitchAlignment = 1;

        internal VkImage NativeImage;
        internal VkBuffer NativeBuffer;
        internal VkImageView NativeColorAttachmentView;
        internal VkImageView NativeDepthStencilView;
        internal VkImageView NativeImageView;
        internal VkImageSubresourceRange NativeResourceRange;

        private bool isNotOwningResources;
        internal bool IsInitialized;

        internal VkFormat NativeFormat;
        internal bool HasStencil;

        internal VkImageLayout NativeLayout;
        internal VkAccessFlags NativeAccessMask;
        internal VkImageAspectFlags NativeImageAspect;

        public void Recreate(DataBox[] dataBoxes = null)
        {
            InitializeFromImpl(dataBoxes);
        }

        public static bool IsDepthStencilReadOnlySupported(GraphicsDevice device)
        {
            // TODO VULKAN
            return true;
        }

        internal void SwapInternal(Texture other)
        {
            Utilities.Swap(ref NativeImage, ref other.NativeImage);
            Utilities.Swap(ref NativeBuffer, ref other.NativeBuffer);
            Utilities.Swap(ref NativeColorAttachmentView, ref other.NativeColorAttachmentView);
            Utilities.Swap(ref NativeDepthStencilView, ref other.NativeDepthStencilView);
            Utilities.Swap(ref NativeImageView, ref other.NativeImageView);
            Utilities.Swap(ref NativeResourceRange, ref other.NativeResourceRange);
            Utilities.Swap(ref isNotOwningResources, ref other.isNotOwningResources);
            Utilities.Swap(ref IsInitialized, ref other.IsInitialized);
            Utilities.Swap(ref NativeFormat, ref other.NativeFormat);
            Utilities.Swap(ref HasStencil, ref other.HasStencil);
            Utilities.Swap(ref NativeLayout, ref other.NativeLayout);
            Utilities.Swap(ref NativeAccessMask, ref other.NativeAccessMask);
            Utilities.Swap(ref NativeImageAspect, ref other.NativeImageAspect);
            //
            Utilities.Swap(ref NativeMemory, ref other.NativeMemory);
            Utilities.Swap(ref StagingFenceValue, ref other.StagingFenceValue);
            Utilities.Swap(ref StagingBuilder, ref other.StagingBuilder);
            Utilities.Swap(ref NativePipelineStageMask, ref other.NativePipelineStageMask);
        }

        internal Texture InitializeFromPersistent(TextureDescription description, VkImage nativeImage)
        {
            NativeImage = nativeImage;

            return InitializeFrom(description);
        }

        internal Texture InitializeWithoutResources(TextureDescription description)
        {
            isNotOwningResources = true;
            return InitializeFrom(description);
        }

        internal void SetNativeHandles(VkImage image, VkImageView attachmentView)
        {
            NativeImage = image;
            NativeColorAttachmentView = attachmentView;
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            NativeFormat = VulkanConvertExtensions.ConvertPixelFormat(ViewFormat);
            HasStencil = IsStencilFormat(ViewFormat);
            
            NativeImageAspect = IsDepthStencil ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color;
            if (HasStencil)
                NativeImageAspect |= VkImageAspectFlags.Stencil;


            var arraySlice = ArraySlice;
            var mipLevel = MipLevel;
            GetViewSliceBounds(ViewType, ref arraySlice, ref mipLevel, out var arrayOrDepthCount, out var mipCount);
            var arrayCount = Dimension == TextureDimension.Texture3D ? 1 : arrayOrDepthCount;
            NativeResourceRange = new VkImageSubresourceRange(NativeImageAspect, (uint)mipLevel, (uint)mipCount, (uint)arraySlice, (uint)arrayCount);

            // For depth-stencil formats, automatically fall back to a supported one
            if (IsDepthStencil && HasStencil)
            {
                NativeFormat = GetFallbackDepthStencilFormat(GraphicsDevice, NativeFormat);
            }

            if (Usage == GraphicsResourceUsage.Staging)
            {
                if (NativeImage != VkImage.Null)
                    throw new InvalidOperationException();

                if (isNotOwningResources)
                    throw new InvalidOperationException();

                NativeAccessMask = VkAccessFlags.HostRead | VkAccessFlags.HostWrite;

                NativePipelineStageMask = VkPipelineStageFlags.Host;

                if (ParentTexture != null)
                {
                    // Create only a view
                    NativeBuffer = ParentTexture.NativeBuffer;
                    NativeMemory = ParentTexture.NativeMemory;
                }
                else
                {
                    CreateBuffer();

                    if (dataBoxes != null && dataBoxes.Length > 0)
                    {
                        InitializeData(dataBoxes);
                    }
                }
            }
            else
            {
                if (NativeImage != VkImage.Null)
                    throw new InvalidOperationException();

                NativeLayout =
                    IsRenderTarget ? VkImageLayout.ColorAttachmentOptimal :
                    IsDepthStencil ? VkImageLayout.DepthStencilAttachmentOptimal :
                    IsShaderResource ? VkImageLayout.ShaderReadOnlyOptimal :
                    VkImageLayout.General;

                if (NativeLayout == VkImageLayout.TransferDstOptimal)
                    NativeAccessMask = VkAccessFlags.TransferRead;

                if (NativeLayout == VkImageLayout.ColorAttachmentOptimal)
                    NativeAccessMask = VkAccessFlags.ColorAttachmentWrite;

                if (NativeLayout == VkImageLayout.DepthStencilAttachmentOptimal)
                    NativeAccessMask = VkAccessFlags.DepthStencilAttachmentWrite;

                if (NativeLayout == VkImageLayout.ShaderReadOnlyOptimal)
                    NativeAccessMask = VkAccessFlags.ShaderRead | VkAccessFlags.InputAttachmentRead;

                NativePipelineStageMask =
                    IsRenderTarget ? VkPipelineStageFlags.ColorAttachmentOutput :
                    IsDepthStencil ? VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests | VkPipelineStageFlags.LateFragmentTests :
                    IsShaderResource ? VkPipelineStageFlags.VertexInput | VkPipelineStageFlags.FragmentShader :
                    VkPipelineStageFlags.None;

                if (ParentTexture != null)
                {
                    // Create only a view
                    NativeImage = ParentTexture.NativeImage;
                    NativeMemory = ParentTexture.NativeMemory;
                }
                else
                {
                    if (!isNotOwningResources)
                    {
                        CreateImage();

                        InitializeData(dataBoxes);
                    }
                }

                if (!isNotOwningResources)
                {
                    NativeImageView = GetImageView(ViewType, ArraySlice, MipLevel);
                    NativeColorAttachmentView = GetColorAttachmentView(ViewType, ArraySlice, MipLevel);
                    NativeDepthStencilView = GetDepthStencilView();
                }
            }
        }

        private unsafe void CreateBuffer()
        {
            var createInfo = new VkBufferCreateInfo
            {
                sType = VkStructureType.BufferCreateInfo,
                flags = VkBufferCreateFlags.None
            };

            createInfo.size = (ulong)ComputeBufferTotalSize();

            createInfo.usage = VkBufferUsageFlags.TransferSrc | VkBufferUsageFlags.TransferDst;

            // Create buffer
            vkCreateBuffer(GraphicsDevice.NativeDevice, &createInfo, null, out NativeBuffer);

            // Allocate and bind memory
            vkGetBufferMemoryRequirements(GraphicsDevice.NativeDevice, NativeBuffer, out var memoryRequirements);

            AllocateMemory(VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent, memoryRequirements);

            if (NativeMemory != VkDeviceMemory.Null)
            {
                vkBindBufferMemory(GraphicsDevice.NativeDevice, NativeBuffer, NativeMemory, 0);
            }
        }

        private unsafe void CreateImage()
        {
            // Create a new image
            var createInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                arrayLayers = (uint)ArraySize,
                extent = new Vortice.Mathematics.Size3(Width, Height, Depth),
                mipLevels = (uint)MipLevels,
                samples = VkSampleCountFlags.Count1,
                format = NativeFormat,
                flags = VkImageCreateFlags.None,
                tiling = VkImageTiling.Optimal,
                initialLayout = VkImageLayout.Undefined
            };

            switch (Dimension)
            {
                case TextureDimension.Texture1D:
                    createInfo.imageType = VkImageType.Image1D;
                    break;
                case TextureDimension.Texture2D:
                    createInfo.imageType = VkImageType.Image2D;
                    break;
                case TextureDimension.Texture3D:
                    createInfo.imageType = VkImageType.Image3D;
                    break;
                case TextureDimension.TextureCube:
                    createInfo.imageType = VkImageType.Image2D;
                    createInfo.flags |= VkImageCreateFlags.CubeCompatible;
                    break;
            }

            // TODO VULKAN: Can we restrict more based on GraphicsResourceUsage? 
            createInfo.usage |= VkImageUsageFlags.TransferSrc | VkImageUsageFlags.TransferDst;

            if (IsRenderTarget)
                createInfo.usage |= VkImageUsageFlags.ColorAttachment;

            if (IsDepthStencil)
                createInfo.usage |= VkImageUsageFlags.DepthStencilAttachment;

            if (IsShaderResource)
                createInfo.usage |= VkImageUsageFlags.Sampled; // TODO VULKAN: Input attachments

            if (IsUnorderedAccess)
                createInfo.usage |= VkImageUsageFlags.Storage;

            var memoryProperties = VkMemoryPropertyFlags.DeviceLocal;

            // Create native image
            // TODO: Multisampling, flags, usage, etc.
            vkCreateImage(GraphicsDevice.NativeDevice, &createInfo, null, out NativeImage);

            // Allocate and bind memory
            vkGetImageMemoryRequirements(GraphicsDevice.NativeDevice, NativeImage, out var memoryRequirements);

            AllocateMemory(memoryProperties, memoryRequirements);

            if (NativeMemory != VkDeviceMemory.Null)
            {
                vkBindImageMemory(GraphicsDevice.NativeDevice, NativeImage, NativeMemory, 0);
            }
        }

        private unsafe void InitializeData(DataBox[] dataBoxes)
        {
            // Begin copy command buffer
            var commandBufferAllocateInfo = new VkCommandBufferAllocateInfo
            {
                sType = VkStructureType.CommandBufferAllocateInfo,
                commandPool = GraphicsDevice.NativeCopyCommandPools.Value,
                commandBufferCount = 1,
                level = VkCommandBufferLevel.Primary
            };
            VkCommandBuffer commandBuffer;

            vkAllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocateInfo, &commandBuffer);

            var beginInfo = new VkCommandBufferBeginInfo { sType = VkStructureType.CommandBufferBeginInfo, flags = VkCommandBufferUsageFlags.OneTimeSubmit };
            vkBeginCommandBuffer(commandBuffer, &beginInfo);

            if (dataBoxes != null && dataBoxes.Length > 0)
            {
                // Buffer-to-image copies need to be aligned to the pixel size and 4 (always a power of 2)
                var blockSize = Format.BlockSize();
                var alignmentMask = (blockSize < 4 ? 4 : blockSize) - 1;

                int totalSize = dataBoxes.Length * alignmentMask;
                for (int i = 0; i < dataBoxes.Length; i++)
                {
                    totalSize += dataBoxes[i].SlicePitch;
                }

                VkBuffer uploadResource;
                int uploadOffset;
                var uploadMemory = GraphicsDevice.AllocateUploadBuffer(totalSize, out uploadResource, out uploadOffset);

                // Upload buffer barrier
                var bufferBarriers = stackalloc VkBufferMemoryBarrier[2];
                bufferBarriers[0] = new VkBufferMemoryBarrier(uploadResource, VkAccessFlags.HostWrite, VkAccessFlags.TransferRead, (ulong)uploadOffset, (ulong)totalSize);

                if (Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[1] = new VkBufferMemoryBarrier(NativeBuffer, NativeAccessMask, VkAccessFlags.TransferWrite);
                    vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 2, bufferBarriers, 0, null);
                }
                else
                {
                    // Image barrier
                    var initialBarrier = new VkImageMemoryBarrier(NativeImage, new VkImageSubresourceRange(NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue), VkAccessFlags.None, VkAccessFlags.TransferWrite, VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal);
                    vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 1, bufferBarriers, 1, &initialBarrier);
                }

                // Copy data boxes to upload buffer
                for (int i = 0; i < dataBoxes.Length; i++)
                {
                    var slicePitch = dataBoxes[i].SlicePitch;

                    int arraySlice = i / MipLevels;
                    int mipSlice = i % MipLevels;
                    var mipMapDescription = GetMipMapDescription(mipSlice);

                    var alignment = ((uploadOffset + alignmentMask) & ~alignmentMask) - uploadOffset;
                    uploadMemory += alignment;
                    uploadOffset += alignment;

                    Utilities.CopyMemory(uploadMemory, dataBoxes[i].DataPointer, slicePitch);

                    if (Usage == GraphicsResourceUsage.Staging)
                    {
                        var copy = new VkBufferCopy
                        {
                            srcOffset = (ulong)uploadOffset,
                            dstOffset = (ulong)ComputeBufferOffset(i, 0),
                            size = (uint)ComputeSubresourceSize(i),
                        };

                        vkCmdCopyBuffer(commandBuffer, uploadResource, NativeBuffer, 1, &copy);
                    }
                    else
                    {
                        // TODO VULKAN: Check if pitches are valid
                        var copy = new VkBufferImageCopy
                        {
                            bufferOffset = (ulong)uploadOffset,
                            imageSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, (uint)mipSlice, (uint)arraySlice, 1),
                            bufferRowLength = (uint)(dataBoxes[i].RowPitch * Format.BlockWidth() / Format.BlockSize()),
                            bufferImageHeight = (uint)(dataBoxes[i].SlicePitch * Format.BlockHeight() / dataBoxes[i].RowPitch),
                            imageOffset = new Vortice.Mathematics.Point3(0, 0, 0),
                            imageExtent = new Vortice.Mathematics.Size3(mipMapDescription.Width, mipMapDescription.Height, mipMapDescription.Depth)
                        };

                        // Copy from upload buffer to image
                        vkCmdCopyBufferToImage(commandBuffer, uploadResource, NativeImage, VkImageLayout.TransferDstOptimal, 1, &copy);
                    }

                    uploadMemory += slicePitch;
                    uploadOffset += slicePitch;
                }

                if (Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[0] = new VkBufferMemoryBarrier(NativeBuffer, VkAccessFlags.TransferWrite, NativeAccessMask);
                    vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.AllCommands, VkDependencyFlags.None, 0, null, 1, bufferBarriers, 0, null);
                }

                IsInitialized = true;
            }

            if (Usage != GraphicsResourceUsage.Staging)
            {
                // Transition to default layout
                var imageMemoryBarrier = new VkImageMemoryBarrier(NativeImage,
                    new VkImageSubresourceRange(NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue),
                    dataBoxes == null || dataBoxes.Length == 0 ? VkAccessFlags.None : VkAccessFlags.TransferWrite, NativeAccessMask,
                    dataBoxes == null || dataBoxes.Length == 0 ? VkImageLayout.Undefined : VkImageLayout.TransferDstOptimal, NativeLayout);
                vkCmdPipelineBarrier(commandBuffer, VkPipelineStageFlags.Transfer, VkPipelineStageFlags.AllCommands, VkDependencyFlags.None, 0, null, 0, null, 1, &imageMemoryBarrier);
            }

            // Close and submit
            vkEndCommandBuffer(commandBuffer);

            var submitInfo = new VkSubmitInfo
            {
                sType = VkStructureType.SubmitInfo,
                commandBufferCount = 1,
                pCommandBuffers = &commandBuffer,
            };

            lock (GraphicsDevice.QueueLock)
            {
                vkQueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, VkFence.Null);
                vkQueueWaitIdle(GraphicsDevice.NativeCommandQueue);
            }

            vkFreeCommandBuffers(GraphicsDevice.NativeDevice, GraphicsDevice.NativeCopyCommandPools.Value, 1, &commandBuffer);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            if (ParentTexture != null || isNotOwningResources)
            {
                NativeImage = VkImage.Null;
                NativeMemory = VkDeviceMemory.Null;
            }

            if (!isNotOwningResources)
            {
                if (NativeMemory != VkDeviceMemory.Null)
                {
                    GraphicsDevice.Collect(NativeMemory);
                    NativeMemory = VkDeviceMemory.Null;
                }

                if (NativeImage != VkImage.Null)
                {
                    GraphicsDevice.Collect(NativeImage);
                    NativeImage = VkImage.Null;
                }

                if (NativeBuffer != VkBuffer.Null)
                {
                    GraphicsDevice.Collect(NativeBuffer);
                    NativeBuffer = VkBuffer.Null;
                }

                if (NativeImageView != VkImageView.Null)
                {
                    GraphicsDevice.Collect(NativeImageView);
                    NativeImageView = VkImageView.Null;
                }

                if (NativeColorAttachmentView != VkImageView.Null)
                {
                    GraphicsDevice.Collect(NativeColorAttachmentView);
                    NativeColorAttachmentView = VkImageView.Null;
                }

                if (NativeDepthStencilView != VkImageView.Null)
                {
                    GraphicsDevice.Collect(NativeDepthStencilView);
                    NativeDepthStencilView = VkImageView.Null;
                }
            }

            base.OnDestroyed();
        }

        private void OnRecreateImpl()
        {
            // Dependency: wait for underlying texture to be recreated
            if (ParentTexture != null && ParentTexture.LifetimeState != GraphicsResourceLifetimeState.Active)
                return;

            // Render Target / Depth Stencil are considered as "dynamic"
            if ((Usage == GraphicsResourceUsage.Immutable
                    || Usage == GraphicsResourceUsage.Default)
                && !IsRenderTarget && !IsDepthStencil)
                return;

            if (ParentTexture == null && GraphicsDevice != null)
            {
                GraphicsDevice.RegisterTextureMemoryUsage(-SizeInBytes);
            }

            InitializeFromImpl();
        }

        private unsafe VkImageView GetImageView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsShaderResource)
                return VkImageView.Null;

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException("ViewSlice.MipBand is not supported for render targets");

            int arrayOrDepthCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayOrDepthCount, out mipCount);

            var layerCount = Dimension == TextureDimension.Texture3D ? 1 : arrayOrDepthCount;

            var createInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                format = NativeFormat, //VulkanConvertExtensions.ConvertPixelFormat(ViewFormat),
                image = NativeImage,
                components = VkComponentMapping.Identity,
                subresourceRange = new VkImageSubresourceRange(IsDepthStencil ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color, (uint)mipIndex, (uint)mipCount, (uint)arrayOrDepthSlice, (uint)layerCount) // TODO VULKAN: Select between depth and stencil?
            };

            if (IsMultisample)
                throw new NotImplementedException();

            if (this.ArraySize > 1)
            {
                if (IsMultisample && Dimension != TextureDimension.Texture2D)
                    throw new NotSupportedException("Multisample is only supported for 2D Textures");

                if (Dimension == TextureDimension.Texture3D)
                    throw new NotSupportedException("Texture Array is not supported for Texture3D");

                switch (Dimension)
                {
                    case TextureDimension.Texture1D:
                        createInfo.viewType = VkImageViewType.Image1DArray;
                        break;
                    case TextureDimension.Texture2D:
                        createInfo.viewType = VkImageViewType.Image2DArray;
                        break;
                    case TextureDimension.TextureCube:
                        if (ArraySize % 6 != 0) throw new NotSupportedException("Texture cubes require an ArraySize which is a multiple of 6");

                        createInfo.viewType = ArraySize > 6 ? VkImageViewType.ImageCubeArray : VkImageViewType.ImageCube;
                        break;
                }
            }
            else
            {
                if (IsMultisample && Dimension != TextureDimension.Texture2D)
                    throw new NotSupportedException("Multisample is only supported for 2D RenderTarget Textures");

                if (Dimension == TextureDimension.TextureCube)
                    throw new NotSupportedException("TextureCube dimension is expecting an arraysize > 1");

                switch (Dimension)
                {
                    case TextureDimension.Texture1D:
                        createInfo.viewType = VkImageViewType.Image1D;
                        break;
                    case TextureDimension.Texture2D:
                        createInfo.viewType = VkImageViewType.Image2D;
                        break;
                    case TextureDimension.Texture3D:
                        createInfo.viewType = VkImageViewType.Image3D;
                        break;
                }
            }

            vkCreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out var imageView);
            return imageView;
        }

        private unsafe VkImageView GetColorAttachmentView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsRenderTarget)
                return VkImageView.Null;

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException("ViewSlice.MipBand is not supported for render targets");

            int arrayOrDepthCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayOrDepthCount, out mipCount);

            var createInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                viewType = VkImageViewType.Image2D,
                format = NativeFormat, // VulkanConvertExtensions.ConvertPixelFormat(ViewFormat),
                image = NativeImage,
                components = VkComponentMapping.Identity,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, (uint)mipIndex, (uint)mipCount, (uint)arrayOrDepthSlice, 1)
            };

            if (IsMultisample)
                throw new NotImplementedException();

            if (this.ArraySize > 1)
            {
                if (IsMultisample && Dimension != TextureDimension.Texture2D)
                    throw new NotSupportedException("Multisample is only supported for 2D Textures");

                if (Dimension == TextureDimension.Texture3D)
                    throw new NotSupportedException("Texture Array is not supported for Texture3D");
            }
            else
            {
                if (IsMultisample && Dimension != TextureDimension.Texture2D)
                    throw new NotSupportedException("Multisample is only supported for 2D RenderTarget Textures");

                if (Dimension == TextureDimension.TextureCube)
                    throw new NotSupportedException("TextureCube dimension is expecting an arraysize > 1");
            }

            vkCreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out var imageView);
            return imageView;
        }

        private unsafe VkImageView GetDepthStencilView()
        {
            if (!IsDepthStencil)
                return VkImageView.Null;

            // Check that the format is supported
            //if (ComputeShaderResourceFormatFromDepthFormat(ViewFormat) == PixelFormat.None)
            //    throw new NotSupportedException("Depth stencil format [{0}] not supported".ToFormat(ViewFormat));

            // Create a Depth stencil view on this texture2D
            var createInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                viewType = VkImageViewType.Image2D,
                format = NativeFormat, //VulkanConvertExtensions.ConvertPixelFormat(ViewFormat),
                image = NativeImage,
                components = VkComponentMapping.Identity,
                subresourceRange = new VkImageSubresourceRange(NativeImageAspect, 0, 1, 0, 1)
            };

            //if (IsDepthStencilReadOnly)
            //{
            //    if (!IsDepthStencilReadOnlySupported(GraphicsDevice))
            //        throw new NotSupportedException("Cannot instantiate ReadOnly DepthStencilBuffer. Not supported on this device.");

            //    // Create a Depth stencil view on this texture2D
            //    createInfo.SubresourceRange.AspectMask =  ? ;
            //    if (HasStencil)
            //        createInfo.Flags |= (int)AttachmentViewCreateFlags.AttachmentViewCreateReadOnlyStencilBit;
            //}

            vkCreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out var imageView);
            return imageView;
        }

        private bool IsFlipped()
        {
            return false;
        }

        internal static PixelFormat ComputeShaderResourceFormatFromDepthFormat(PixelFormat format)
        {
            return format;
        }

        /// <summary>
        /// Check and modify if necessary the mipmap levels of the image (Troubles with DXT images whose resolution in less than 4x4 in DX9.x).
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <param name="description">The texture description.</param>
        /// <returns>The updated texture description.</returns>
        private static TextureDescription CheckMipLevels(GraphicsDevice device, ref TextureDescription description)
        {
            if (device.Features.CurrentProfile < GraphicsProfile.Level_10_0 && (description.Flags & TextureFlags.DepthStencil) == 0 && description.Format.IsCompressed())
            {
                description.MipLevels = Math.Min(CalculateMipCount(description.Width, description.Height), description.MipLevels);
            }
            return description;
        }

        /// <summary>
        /// Calculates the mip level from a specified size.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="minimumSizeLastMip">The minimum size of the last mip.</param>
        /// <returns>The mip level.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Value must be > 0;size</exception>
        private static int CalculateMipCountFromSize(int size, int minimumSizeLastMip = 4)
        {
            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException("Value must be > 0", "size");
            }

            if (minimumSizeLastMip <= 0)
            {
                throw new ArgumentOutOfRangeException("Value must be > 0", "minimumSizeLastMip");
            }

            int level = 1;
            while ((size / 2) >= minimumSizeLastMip)
            {
                size = Math.Max(1, size / 2);
                level++;
            }
            return level;
        }

        /// <summary>
        /// Calculates the mip level from a specified width,height,depth.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="minimumSizeLastMip">The minimum size of the last mip.</param>
        /// <returns>The mip level.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">Value must be &gt; 0;size</exception>
        private static int CalculateMipCount(int width, int height, int minimumSizeLastMip = 4)
        {
            return Math.Min(CalculateMipCountFromSize(width, minimumSizeLastMip), CalculateMipCountFromSize(height, minimumSizeLastMip));
        }

        internal static VkFormat GetFallbackDepthStencilFormat(GraphicsDevice device, VkFormat format)
        {
            if (format == VkFormat.D16UNormS8UInt || format == VkFormat.D24UNormS8UInt || format == VkFormat.D32SFloatS8UInt)
            {
                var fallbackFormats = new[] { format, VkFormat.D32SFloatS8UInt, VkFormat.D24UNormS8UInt, VkFormat.D16UNormS8UInt };

                foreach (var fallbackFormat in fallbackFormats)
                {
                    vkGetPhysicalDeviceFormatProperties(device.NativePhysicalDevice, fallbackFormat, out var formatProperties);

                    if ((formatProperties.optimalTilingFeatures & VkFormatFeatureFlags.DepthStencilAttachment) != 0)
                    {
                        format = fallbackFormat;
                        break;
                    }
                }
            }

            return format;
        }

        internal static bool IsStencilFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R24G8_Typeless:
                case PixelFormat.D24_UNorm_S8_UInt:
                case PixelFormat.R32G8X24_Typeless:
                case PixelFormat.D32_Float_S8X24_UInt:
                    return true;
            }

            return false;
        }
    }
}
#endif
