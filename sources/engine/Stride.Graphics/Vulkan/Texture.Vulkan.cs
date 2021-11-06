// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Linq;
using Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
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

        internal Silk.NET.Vulkan.Image NativeImage;
        internal Silk.NET.Vulkan.Buffer NativeBuffer;
        internal ImageView NativeColorAttachmentView;
        internal ImageView NativeDepthStencilView;
        internal ImageView NativeImageView;
        internal ImageSubresourceRange NativeResourceRange;

        private bool isNotOwningResources;
        internal bool IsInitialized;

        internal Format NativeFormat;
        internal bool HasStencil;

        internal ImageLayout NativeLayout;
        internal AccessFlags NativeAccessMask;
        internal ImageAspectFlags NativeImageAspect;

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

        internal Texture InitializeFromPersistent(TextureDescription description, Silk.NET.Vulkan.Image nativeImage)
        {
            NativeImage = nativeImage;

            return InitializeFrom(description);
        }

        internal Texture InitializeWithoutResources(TextureDescription description)
        {
            isNotOwningResources = true;
            return InitializeFrom(description);
        }

        internal void SetNativeHandles(Silk.NET.Vulkan.Image image, ImageView attachmentView)
        {
            NativeImage = image;
            NativeColorAttachmentView = attachmentView;
        }

        private void InitializeFromImpl(DataBox[] dataBoxes = null)
        {
            NativeFormat = VulkanConvertExtensions.ConvertPixelFormat(ViewFormat);
            HasStencil = IsStencilFormat(ViewFormat);
            
            NativeImageAspect = IsDepthStencil ? ImageAspectFlags.ImageAspectDepthBit : ImageAspectFlags.ImageAspectColorBit;
            if (HasStencil)
                NativeImageAspect |= ImageAspectFlags.ImageAspectStencilBit;


            var arraySlice = ArraySlice;
            var mipLevel = MipLevel;
            GetViewSliceBounds(ViewType, ref arraySlice, ref mipLevel, out var arrayOrDepthCount, out var mipCount);
            var arrayCount = Dimension == TextureDimension.Texture3D ? 1 : arrayOrDepthCount;
            NativeResourceRange = new ImageSubresourceRange(NativeImageAspect, (uint)mipLevel, (uint)mipCount, (uint)arraySlice, (uint)arrayCount);

            // For depth-stencil formats, automatically fall back to a supported one
            if (IsDepthStencil && HasStencil)
            {
                NativeFormat = GetFallbackDepthStencilFormat(GraphicsDevice, NativeFormat);
            }

            if (Usage == GraphicsResourceUsage.Staging)
            {
                if (NativeImage.Handle != 0)
                    throw new InvalidOperationException();

                if (isNotOwningResources)
                    throw new InvalidOperationException();

                NativeAccessMask = AccessFlags.AccessHostReadBit | AccessFlags.AccessHostWriteBit;

                NativePipelineStageMask = PipelineStageFlags.PipelineStageHostBit;

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
                if (NativeImage.Handle != 0)
                    throw new InvalidOperationException();

                NativeLayout =
                    IsRenderTarget ? ImageLayout.ColorAttachmentOptimal :
                    IsDepthStencil ? ImageLayout.DepthStencilAttachmentOptimal :
                    IsShaderResource ? ImageLayout.ShaderReadOnlyOptimal :
                    ImageLayout.General;

                if (NativeLayout == ImageLayout.TransferDstOptimal)
                    NativeAccessMask = AccessFlags.AccessTransferReadBit;

                if (NativeLayout == ImageLayout.ColorAttachmentOptimal)
                    NativeAccessMask = AccessFlags.AccessColorAttachmentWriteBit;

                if (NativeLayout == ImageLayout.DepthStencilAttachmentOptimal)
                    NativeAccessMask = AccessFlags.AccessDepthStencilAttachmentWriteBit;

                if (NativeLayout == ImageLayout.ShaderReadOnlyOptimal)
                    NativeAccessMask = AccessFlags.AccessShaderReadBit | AccessFlags.AccessInputAttachmentReadBit;

                NativePipelineStageMask =
                    IsRenderTarget ? PipelineStageFlags.PipelineStageColorAttachmentOutputBit :
                    IsDepthStencil ? PipelineStageFlags.PipelineStageColorAttachmentOutputBit | PipelineStageFlags.PipelineStageEarlyFragmentTestsBit | PipelineStageFlags.PipelineStageLateFragmentTestsBit :
                    IsShaderResource ? PipelineStageFlags.PipelineStageVertexInputBit | PipelineStageFlags.PipelineStageFragmentShaderBit :
                    0;

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
            var createInfo = new BufferCreateInfo
            {
                SType = StructureType.BufferCreateInfo,
                Flags = 0
            };

            createInfo.Size = (ulong)ComputeBufferTotalSize();

            createInfo.Usage = BufferUsageFlags.BufferUsageTransferSrcBit | BufferUsageFlags.BufferUsageTransferDstBit;

            // Create buffer
            GetApi().CreateBuffer(GraphicsDevice.NativeDevice, &createInfo, null, (Silk.NET.Vulkan.Buffer*)NativeBuffer.Handle);

            // Allocate and bind memory
            GetApi().GetBufferMemoryRequirements(GraphicsDevice.NativeDevice, NativeBuffer, out var memoryRequirements);

            AllocateMemory(MemoryPropertyFlags.MemoryPropertyHostVisibleBit | MemoryPropertyFlags.MemoryPropertyHostCoherentBit, memoryRequirements);

            if (NativeMemory.Handle != 0)
            {
                GetApi().BindBufferMemory(GraphicsDevice.NativeDevice, NativeBuffer, NativeMemory, 0);
            }
        }

        private unsafe void CreateImage()
        {
            // Create a new image
            var createInfo = new ImageCreateInfo
            {
                SType = StructureType.ImageCreateInfo,
                ArrayLayers = (uint)ArraySize,
                Extent = new Silk.NET.Vulkan.Extent3D((uint)Width, (uint)Height, (uint)Depth),
                MipLevels = (uint)MipLevels,
                Samples = SampleCountFlags.SampleCount1Bit,
                Format = NativeFormat,
                Flags = 0,
                Tiling = ImageTiling.Optimal,
                InitialLayout = ImageLayout.Undefined
            };

            switch (Dimension)
            {
                case TextureDimension.Texture1D:
                    createInfo.ImageType = ImageType.ImageType1D;
                    break;
                case TextureDimension.Texture2D:
                    createInfo.ImageType = ImageType.ImageType2D;
                    break;
                case TextureDimension.Texture3D:
                    createInfo.ImageType = ImageType.ImageType3D;
                    break;
                case TextureDimension.TextureCube:
                    createInfo.ImageType = ImageType.ImageType2D;
                    createInfo.Flags |= ImageCreateFlags.ImageCreateCubeCompatibleBit;
                    break;
            }

            // TODO VULKAN: Can we restrict more based on GraphicsResourceUsage? 
            createInfo.Usage |= ImageUsageFlags.ImageUsageTransferSrcBit | ImageUsageFlags.ImageUsageTransferDstBit;

            if (IsRenderTarget)
                createInfo.Usage |= ImageUsageFlags.ImageUsageColorAttachmentBit;

            if (IsDepthStencil)
                createInfo.Usage |= ImageUsageFlags.ImageUsageDepthStencilAttachmentBit;

            if (IsShaderResource)
                createInfo.Usage |= ImageUsageFlags.ImageUsageSampledBit; // TODO VULKAN: Input attachments

            if (IsUnorderedAccess)
                createInfo.Usage |= ImageUsageFlags.ImageUsageStorageBit;

            var memoryProperties = MemoryPropertyFlags.MemoryPropertyDeviceLocalBit;

            // Create native image
            // TODO: Multisampling, flags, usage, etc.
            GetApi().CreateImage(GraphicsDevice.NativeDevice, &createInfo, null, out NativeImage);

            // Allocate and bind memory
            GetApi().GetImageMemoryRequirements(GraphicsDevice.NativeDevice, NativeImage, out var memoryRequirements);

            AllocateMemory(memoryProperties, memoryRequirements);

            if (NativeMemory.Handle != 0)
            {
                GetApi().BindImageMemory(GraphicsDevice.NativeDevice, NativeImage, NativeMemory, 0);
            }
        }

        private unsafe void InitializeData(DataBox[] dataBoxes)
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

                Silk.NET.Vulkan.Buffer uploadResource;
                int uploadOffset;
                var uploadMemory = GraphicsDevice.AllocateUploadBuffer(totalSize, out uploadResource, out uploadOffset);

                // Upload buffer barrier
                var bufferBarriers = stackalloc BufferMemoryBarrier[2];
                bufferBarriers[0] = new BufferMemoryBarrier
                {
                    Buffer = uploadResource,
                    SrcAccessMask = AccessFlags.AccessHostWriteBit,
                    DstAccessMask = AccessFlags.AccessTransferReadBit,
                    SrcQueueFamilyIndex = (uint)uploadOffset,
                    DstQueueFamilyIndex = (uint)totalSize
                };

                if (Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[1] = new BufferMemoryBarrier
                    {
                        Buffer = NativeBuffer, 
                        SrcAccessMask = NativeAccessMask, 
                        DstAccessMask = AccessFlags.AccessTransferWriteBit
                    };
                    GetApi().CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageHostBit, PipelineStageFlags.PipelineStageTransferBit, 0, 0, null, 2, bufferBarriers, 0, null);
                }
                else
                {
                    // Image barrier
                    var initialBarrier = new ImageMemoryBarrier
                    {
                        Image = NativeImage, 
                        SubresourceRange = new ImageSubresourceRange(NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue),
                        SrcAccessMask = AccessFlags.AccessNoneKhr, 
                        DstAccessMask = AccessFlags.AccessTransferWriteBit, 
                        OldLayout = ImageLayout.Undefined, 
                        NewLayout = ImageLayout.TransferDstOptimal
                    };
                    GetApi().CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageHostBit, PipelineStageFlags.PipelineStageTransferBit, 0, 0, null, 1, bufferBarriers, 1, &initialBarrier);
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
                        var copy = new BufferCopy
                        {
                            SrcOffset = (ulong)uploadOffset,
                            DstOffset = (ulong)ComputeBufferOffset(i, 0),
                            Size = (uint)ComputeSubresourceSize(i),
                        };

                        GetApi().CmdCopyBuffer(commandBuffer, uploadResource, NativeBuffer, 1, &copy);
                    }
                    else
                    {
                        // TODO VULKAN: Check if pitches are valid
                        var copy = new BufferImageCopy
                        {
                            BufferOffset = (ulong)uploadOffset,
                            ImageSubresource = new ImageSubresourceLayers(ImageAspectFlags.ImageAspectColorBit, (uint)mipSlice, (uint)arraySlice, 1),
                            BufferRowLength = (uint)(dataBoxes[i].RowPitch * Format.BlockWidth() / Format.BlockSize()),
                            BufferImageHeight = (uint)(dataBoxes[i].SlicePitch * Format.BlockHeight() / dataBoxes[i].RowPitch),
                            ImageOffset = new Silk.NET.Vulkan.Offset3D(0, 0, 0),
                            ImageExtent = new Silk.NET.Vulkan.Extent3D((uint)mipMapDescription.Width, (uint)mipMapDescription.Height, (uint)mipMapDescription.Depth)
                        };

                        // Copy from upload buffer to image
                        GetApi().CmdCopyBufferToImage(commandBuffer, uploadResource, NativeImage, ImageLayout.TransferDstOptimal, 1, &copy);
                    }

                    uploadMemory += slicePitch;
                    uploadOffset += slicePitch;
                }

                if (Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[0] = new BufferMemoryBarrier
                    {
                        Buffer = NativeBuffer, 
                        SrcAccessMask = AccessFlags.AccessTransferWriteBit, 
                        DstAccessMask = NativeAccessMask
                    };
                    GetApi().CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageAllCommandsBit, 0, 0, null, 1, bufferBarriers, 0, null);
                }

                IsInitialized = true;
            }

            if (Usage != GraphicsResourceUsage.Staging)
            {
                // Transition to default layout
                var imageMemoryBarrier = new ImageMemoryBarrier
                {
                    Image = NativeImage,
                    SubresourceRange = new ImageSubresourceRange
                    {
                        AspectMask = NativeImageAspect,
                        BaseMipLevel = 0,
                        LevelCount = uint.MaxValue,
                        BaseArrayLayer = 0,
                        LayerCount = uint.MaxValue
                    },
                    SrcAccessMask = dataBoxes == null || dataBoxes.Length == 0 ? 0 : AccessFlags.AccessTransferWriteBit,
                    DstAccessMask = NativeAccessMask,
                    OldLayout = dataBoxes == null || dataBoxes.Length == 0 ? ImageLayout.Undefined : ImageLayout.TransferDstOptimal,
                    NewLayout = NativeLayout
                };
                GetApi().CmdPipelineBarrier(commandBuffer, PipelineStageFlags.PipelineStageTransferBit, PipelineStageFlags.PipelineStageAllCommandsBit, 0, 0, null, 0, null, 1, &imageMemoryBarrier);
            }

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
                GetApi().QueueSubmit(GraphicsDevice.NativeCommandQueue, 1, &submitInfo, new Fence(0));
                GetApi().QueueWaitIdle(GraphicsDevice.NativeCommandQueue);
            }

            GetApi().FreeCommandBuffers(GraphicsDevice.NativeDevice, GraphicsDevice.NativeCopyCommandPools.Value, 1, &commandBuffer);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            if (ParentTexture != null || isNotOwningResources)
            {
                NativeImage = new Silk.NET.Vulkan.Image(0);
                NativeMemory = new DeviceMemory(0);
            }

            if (!isNotOwningResources)
            {
                if (NativeMemory.Handle != 0)
                {
                    GraphicsDevice.Collect(NativeMemory);
                    NativeMemory = new DeviceMemory(0);
                }

                if (NativeImage.Handle != 0)
                {
                    GraphicsDevice.Collect(NativeImage);
                    NativeImage = new Silk.NET.Vulkan.Image(0);
                }

                if (NativeBuffer.Handle != 0)
                {
                    GraphicsDevice.Collect(NativeBuffer);
                    NativeBuffer = new Silk.NET.Vulkan.Buffer(0);
                }

                if (NativeImageView.Handle != 0)
                {
                    GraphicsDevice.Collect(NativeImageView);
                    NativeImageView = new ImageView(0);
                }

                if (NativeColorAttachmentView.Handle != 0)
                {
                    GraphicsDevice.Collect(NativeColorAttachmentView);
                    NativeColorAttachmentView = new ImageView(0);
                }

                if (NativeDepthStencilView.Handle != 0)
                {
                    GraphicsDevice.Collect(NativeDepthStencilView);
                    NativeDepthStencilView = new ImageView(0);
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

        private unsafe ImageView GetImageView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsShaderResource)
                return new ImageView(0);

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException("ViewSlice.MipBand is not supported for render targets");

            int arrayOrDepthCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayOrDepthCount, out mipCount);

            var layerCount = Dimension == TextureDimension.Texture3D ? 1 : arrayOrDepthCount;

            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                Format = NativeFormat, //VulkanConvertExtensions.ConvertPixelFormat(ViewFormat),
                Image = NativeImage,
                Components = new ComponentMapping
                    {
                        B = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        R = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity
                    },
                SubresourceRange = new ImageSubresourceRange(IsDepthStencil ? ImageAspectFlags.ImageAspectDepthBit : ImageAspectFlags.ImageAspectColorBit, (uint)mipIndex, (uint)mipCount, (uint)arrayOrDepthSlice, (uint)layerCount) // TODO VULKAN: Select between depth and stencil?
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
                        createInfo.ViewType = ImageViewType.ImageViewType1DArray;
                        break;
                    case TextureDimension.Texture2D:
                        createInfo.ViewType = ImageViewType.ImageViewType2DArray;
                        break;
                    case TextureDimension.TextureCube:
                        if (ArraySize % 6 != 0) throw new NotSupportedException("Texture cubes require an ArraySize which is a multiple of 6");

                        createInfo.ViewType = ArraySize > 6 ? ImageViewType.CubeArray: ImageViewType.Cube;
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
                        createInfo.ViewType = ImageViewType.ImageViewType1D;
                        break;
                    case TextureDimension.Texture2D:
                        createInfo.ViewType = ImageViewType.ImageViewType2D;
                        break;
                    case TextureDimension.Texture3D:
                        createInfo.ViewType = ImageViewType.ImageViewType3D;
                        break;
                }
            }

            GetApi().CreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out var imageView);
            return imageView;
        }

        private unsafe ImageView GetColorAttachmentView(ViewType viewType, int arrayOrDepthSlice, int mipIndex)
        {
            if (!IsRenderTarget)
                return new ImageView(0);

            if (viewType == ViewType.MipBand)
                throw new NotSupportedException("ViewSlice.MipBand is not supported for render targets");

            int arrayOrDepthCount;
            int mipCount;
            GetViewSliceBounds(viewType, ref arrayOrDepthSlice, ref mipIndex, out arrayOrDepthCount, out mipCount);

            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                ViewType = ImageViewType.ImageViewType2D,
                Format = NativeFormat, // VulkanConvertExtensions.ConvertPixelFormat(ViewFormat),
                Image = NativeImage,
                Components = new ComponentMapping
                    {
                        B = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        R = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity
                    },
                SubresourceRange = new ImageSubresourceRange(ImageAspectFlags.ImageAspectColorBit, (uint)mipIndex, (uint)mipCount, (uint)arrayOrDepthSlice, 1)
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

            GetApi().CreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out var imageView);
            return imageView;
        }

        private unsafe ImageView GetDepthStencilView()
        {
            if (!IsDepthStencil)
                return new ImageView(0);

            // Check that the format is supported
            //if (ComputeShaderResourceFormatFromDepthFormat(ViewFormat) == PixelFormat.None)
            //    throw new NotSupportedException("Depth stencil format [{0}] not supported".ToFormat(ViewFormat));

            // Create a Depth stencil view on this texture2D
            var createInfo = new ImageViewCreateInfo
            {
                SType = StructureType.ImageViewCreateInfo,
                ViewType = ImageViewType.ImageViewType2D,
                Format = NativeFormat, //VulkanConvertExtensions.ConvertPixelFormat(ViewFormat),
                Image = NativeImage,
                Components = new ComponentMapping
                    {
                        B = ComponentSwizzle.Identity,
                        G = ComponentSwizzle.Identity,
                        R = ComponentSwizzle.Identity,
                        A = ComponentSwizzle.Identity
                    },
                SubresourceRange = new ImageSubresourceRange(NativeImageAspect, 0, 1, 0, 1)
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

            GetApi().CreateImageView(GraphicsDevice.NativeDevice, &createInfo, null, out var imageView);
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

        internal static Format GetFallbackDepthStencilFormat(GraphicsDevice device, Format format)
        {
            if (format == Silk.NET.Vulkan.Format.D16UnormS8Uint || format == Silk.NET.Vulkan.Format.D24UnormS8Uint || format == Silk.NET.Vulkan.Format.D32SfloatS8Uint)
            {
                var fallbackFormats = new[] { format, Silk.NET.Vulkan.Format.D32SfloatS8Uint, Silk.NET.Vulkan.Format.D24UnormS8Uint, Silk.NET.Vulkan.Format.D16UnormS8Uint };

                foreach (var fallbackFormat in fallbackFormats)
                {
                    GetApi().GetPhysicalDeviceFormatProperties(device.NativePhysicalDevice, fallbackFormat, out var formatProperties);

                    if ((formatProperties.OptimalTilingFeatures & FormatFeatureFlags.FormatFeatureDepthStencilAttachmentBit) != 0)
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
