// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN && (STRIDE_PLATFORM_IOS || STRIDE_PLATFORM_MACOS)
using System;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    public partial class Texture
    {
        // Set when this Texture wraps an externally-owned IOSurface imported via VK_EXT_metal_objects.
        private IntPtr ioSurfaceRef;
        private bool isImportedImage;

        partial void TryInitializeImportedImage()
        {
            if (!isImportedImage)
                return;

            HasStencil = false;
            NativeImageAspect = VkImageAspectFlags.Color;

            var arraySlice = ArraySlice;
            var mipLevel = MipLevel;
            GetViewSliceBounds(ViewType, ref arraySlice, ref mipLevel, out var arrayOrDepthCount, out var mipCount);
            NativeResourceRange = new VkImageSubresourceRange(NativeImageAspect, (uint)mipLevel, (uint)mipCount, (uint)arraySlice, (uint)arrayOrDepthCount);

            // Image starts Undefined per vkCreateImage; Stride will emit the transition to
            // ShaderReadOnlyOptimal on first sampler bind (CommandList barrier path).
            NativeLayout = VkImageLayout.Undefined;
            NativeAccessMask = VkAccessFlags.None;
            NativePipelineStageMask = VkPipelineStageFlags.TopOfPipe;
            LayoutTracker.Initialize(BarrierLayout.Undefined, ArraySize * MipLevelCount);
            IsInitialized = true;
            importedImageHandled = true;
        }

        partial void ReleaseImportedImageResources()
        {
            if (ioSurfaceRef != IntPtr.Zero)
            {
                CFRelease(ioSurfaceRef);
                ioSurfaceRef = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Imports a CoreVideo IOSurface as a sampleable BGRA8_sRGB <see cref="Texture"/> via VK_EXT_metal_objects.
        /// Retains the IOSurface for the lifetime of the returned Texture; the caller is free to dispose its
        /// CVPixelBuffer once this call returns.
        /// </summary>
        public static unsafe Texture NewFromIOSurface(GraphicsDevice device, IntPtr ioSurface, int width, int height)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (ioSurface == IntPtr.Zero) throw new ArgumentNullException(nameof(ioSurface));
            if (!device.HasMetalObjectsSupport)
                throw new InvalidOperationException("IOSurface import requires VK_EXT_metal_objects.");

            CFRetain(ioSurface);

            var ioSurfaceInfo = new VkImportMetalIOSurfaceInfoEXT
            {
                sType = VkStructureType.ImportMetalIOSurfaceInfoEXT,
                ioSurface = ioSurface,
            };

            var imageCreateInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                pNext = &ioSurfaceInfo,
                imageType = VkImageType.Image2D,
                format = VkFormat.B8G8R8A8Srgb,
                extent = new VkExtent3D(width, height, 1),
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.Count1,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined,
            };
            VkImage image;
            var imageResult = device.NativeDeviceApi.vkCreateImage(device.NativeDevice, &imageCreateInfo, null, &image);
            if (imageResult != VkResult.Success)
            {
                CFRelease(ioSurface);
                device.CheckResult(imageResult);
            }

            // MoltenVK actually backs the image with the IOSurface, but the Vulkan spec still
            // requires vkBindImageMemory for the validation layer to be happy. Allocate a normal
            // VkDeviceMemory block sized to GetImageMemoryRequirements; MoltenVK ignores it as
            // soon as the IOSurface-backed MTLTexture is created on first use.
            device.NativeDeviceApi.vkGetImageMemoryRequirements(device.NativeDevice, image, out var memReq);
            var allocateInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memReq.size,
                memoryTypeIndex = FindMemoryTypeIndex(device, memReq.memoryTypeBits),
            };
            VkDeviceMemory memory;
            var allocResult = device.NativeDeviceApi.vkAllocateMemory(device.NativeDevice, &allocateInfo, null, &memory);
            if (allocResult != VkResult.Success)
            {
                device.NativeDeviceApi.vkDestroyImage(device.NativeDevice, image, null);
                CFRelease(ioSurface);
                device.CheckResult(allocResult);
            }
            device.CheckResult(device.NativeDeviceApi.vkBindImageMemory(device.NativeDevice, image, memory, 0));

            var viewCreateInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                image = image,
                viewType = VkImageViewType.Image2D,
                format = VkFormat.B8G8R8A8Srgb,
                components = VkComponentMapping.Identity,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1),
            };
            VkImageView imageView;
            var viewResult = device.NativeDeviceApi.vkCreateImageView(device.NativeDevice, &viewCreateInfo, null, &imageView);
            if (viewResult != VkResult.Success)
            {
                device.NativeDeviceApi.vkFreeMemory(device.NativeDevice, memory, null);
                device.NativeDeviceApi.vkDestroyImage(device.NativeDevice, image, null);
                CFRelease(ioSurface);
                device.CheckResult(viewResult);
            }

            var description = TextureDescription.New2D(width, height, mipCount: 1, PixelFormat.B8G8R8A8_UNorm_SRgb, TextureFlags.ShaderResource);

            var texture = new Texture(device);
            texture.NativeImage = image;
            texture.NativeMemory = memory;
            texture.NativeImageView = imageView;
            texture.NativeFormat = VkFormat.B8G8R8A8Srgb;
            texture.ioSurfaceRef = ioSurface;
            texture.isImportedImage = true;
            texture.InitializeFrom(description);
            return texture;
        }

        private static unsafe uint FindMemoryTypeIndex(GraphicsDevice device, uint memoryTypeBits)
        {
            device.NativeInstanceApi.vkGetPhysicalDeviceMemoryProperties(device.NativePhysicalDevice, out var memoryProperties);
            for (uint i = 0; i < memoryProperties.memoryTypeCount; i++)
            {
                if ((memoryTypeBits & (1u << (int)i)) != 0)
                    return i;
            }
            throw new InvalidOperationException("Vulkan: no memory type satisfies the IOSurface memoryTypeBits mask.");
        }

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern IntPtr CFRetain(IntPtr cf);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);
    }
}
#endif
