// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN && STRIDE_PLATFORM_ANDROID
using System;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Stride.Graphics
{
    public partial class Texture
    {
        // Populated by NewFromAndroidHardwareBuffer below.
        internal VkSamplerYcbcrConversion NativeSamplerYcbcrConversion;
        private IntPtr androidHardwareBuffer;
        private bool isImportedImage;

        partial void TryInitializeImportedImage()
        {
            if (!isImportedImage)
                return;

            // Always 2D color; the factory has populated Native* fields already.
            HasStencil = false;
            NativeImageAspect = VkImageAspectFlags.Color;

            var arraySlice = ArraySlice;
            var mipLevel = MipLevel;
            GetViewSliceBounds(ViewType, ref arraySlice, ref mipLevel, out var arrayOrDepthCount, out var mipCount);
            NativeResourceRange = new VkImageSubresourceRange(NativeImageAspect, (uint)mipLevel, (uint)mipCount, (uint)arraySlice, (uint)arrayOrDepthCount);

            NativeLayout = VkImageLayout.ShaderReadOnlyOptimal;
            NativeAccessMask = VkAccessFlags.ShaderRead;
            NativePipelineStageMask = VkPipelineStageFlags.FragmentShader;
            LayoutTracker.Initialize(BarrierLayout.ShaderResource, ArraySize * MipLevelCount);
            IsInitialized = true;
            importedImageHandled = true;
        }

        partial void ReleaseImportedImageResources()
        {
            if (NativeSamplerYcbcrConversion != VkSamplerYcbcrConversion.Null)
            {
                GraphicsDevice.Collect(NativeSamplerYcbcrConversion);
                NativeSamplerYcbcrConversion = VkSamplerYcbcrConversion.Null;
            }
            if (androidHardwareBuffer != IntPtr.Zero)
            {
                AHardwareBuffer_release(androidHardwareBuffer);
                androidHardwareBuffer = IntPtr.Zero;
            }
        }

        /// <summary>Imports an <c>AHardwareBuffer</c> as a Vulkan-backed sampleable Texture.
        /// Acquires a reference released when the Texture is destroyed.</summary>
        public static unsafe Texture NewFromAndroidHardwareBuffer(GraphicsDevice device, IntPtr hardwareBuffer)
        {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (hardwareBuffer == IntPtr.Zero) throw new ArgumentNullException(nameof(hardwareBuffer));
            if (!device.HasAndroidHardwareBufferSupport)
                throw new InvalidOperationException("AHardwareBuffer import requires VK_ANDROID_external_memory_android_hardware_buffer.");

            AHardwareBuffer_acquire(hardwareBuffer);

            AHardwareBufferDesc desc;
            AHardwareBuffer_describe(hardwareBuffer, out desc);

            var formatInfo = new VkAndroidHardwareBufferFormatPropertiesANDROID
            {
                sType = VkStructureType.AndroidHardwareBufferFormatPropertiesAndroid,
            };
            var props = new VkAndroidHardwareBufferPropertiesANDROID
            {
                sType = VkStructureType.AndroidHardwareBufferPropertiesAndroid,
                pNext = &formatInfo,
            };
            device.CheckResult(device.NativeDeviceApi.vkGetAndroidHardwareBufferPropertiesANDROID(device.NativeDevice, hardwareBuffer, &props));

            // Non-zero externalFormat = buffer needs sampler-YCbCr conversion to read as RGB.
            // Zero = buffer is sampled directly via the returned VkFormat (RGBA AHardwareBuffers).
            var useExternalFormat = formatInfo.externalFormat != 0;

            VkSamplerYcbcrConversion ycbcrConversion = VkSamplerYcbcrConversion.Null;
            VkExternalFormatANDROID externalFormat = default;
            if (useExternalFormat)
            {
                externalFormat = new VkExternalFormatANDROID
                {
                    sType = VkStructureType.ExternalFormatAndroid,
                    externalFormat = formatInfo.externalFormat,
                };

                var conversionCreateInfo = new VkSamplerYcbcrConversionCreateInfo
                {
                    sType = VkStructureType.SamplerYcbcrConversionCreateInfo,
                    pNext = &externalFormat,
                    format = VkFormat.Undefined,
                    ycbcrModel = formatInfo.suggestedYcbcrModel,
                    ycbcrRange = formatInfo.suggestedYcbcrRange,
                    components = formatInfo.samplerYcbcrConversionComponents,
                    xChromaOffset = formatInfo.suggestedXChromaOffset,
                    yChromaOffset = formatInfo.suggestedYChromaOffset,
                    chromaFilter = VkFilter.Linear,
                    forceExplicitReconstruction = VkBool32.False,
                };
                device.CheckResult(device.NativeDeviceApi.vkCreateSamplerYcbcrConversion(device.NativeDevice, &conversionCreateInfo, null, out ycbcrConversion));
            }

            var externalMemoryImageInfo = new VkExternalMemoryImageCreateInfo
            {
                sType = VkStructureType.ExternalMemoryImageCreateInfo,
                pNext = useExternalFormat ? &externalFormat : null,
                handleTypes = VkExternalMemoryHandleTypeFlags.AndroidHardwareBufferAndroid,
            };

            var imageCreateInfo = new VkImageCreateInfo
            {
                sType = VkStructureType.ImageCreateInfo,
                pNext = &externalMemoryImageInfo,
                imageType = VkImageType.Image2D,
                // Per spec: must be VK_FORMAT_UNDEFINED when VkExternalFormatANDROID is present.
                format = useExternalFormat ? VkFormat.Undefined : formatInfo.format,
                extent = new VkExtent3D((int)desc.Width, (int)desc.Height, 1),
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.Count1,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined,
            };
            VkImage image;
            device.CheckResult(device.NativeDeviceApi.vkCreateImage(device.NativeDevice, &imageCreateInfo, null, &image));

            var dedicatedInfo = new VkMemoryDedicatedAllocateInfo
            {
                sType = VkStructureType.MemoryDedicatedAllocateInfo,
                image = image,
            };
            var importInfo = new VkImportAndroidHardwareBufferInfoANDROID
            {
                sType = VkStructureType.ImportAndroidHardwareBufferInfoAndroid,
                pNext = &dedicatedInfo,
                buffer = hardwareBuffer,
            };
            var allocateInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                pNext = &importInfo,
                allocationSize = props.allocationSize,
                memoryTypeIndex = FindMemoryTypeIndex(device, props.memoryTypeBits),
            };
            VkDeviceMemory memory;
            var allocResult = device.NativeDeviceApi.vkAllocateMemory(device.NativeDevice, &allocateInfo, null, &memory);
            if (allocResult != VkResult.Success)
            {
                device.NativeDeviceApi.vkDestroyImage(device.NativeDevice, image, null);
                if (ycbcrConversion != VkSamplerYcbcrConversion.Null)
                    device.NativeDeviceApi.vkDestroySamplerYcbcrConversion(device.NativeDevice, ycbcrConversion, null);
                AHardwareBuffer_release(hardwareBuffer);
                device.CheckResult(allocResult);
            }
            device.CheckResult(device.NativeDeviceApi.vkBindImageMemory(device.NativeDevice, image, memory, 0));

            VkSamplerYcbcrConversionInfo viewYcbcrInfo = default;
            if (ycbcrConversion != VkSamplerYcbcrConversion.Null)
            {
                viewYcbcrInfo = new VkSamplerYcbcrConversionInfo
                {
                    sType = VkStructureType.SamplerYcbcrConversionInfo,
                    conversion = ycbcrConversion,
                };
            }
            var viewCreateInfo = new VkImageViewCreateInfo
            {
                sType = VkStructureType.ImageViewCreateInfo,
                pNext = ycbcrConversion != VkSamplerYcbcrConversion.Null ? &viewYcbcrInfo : null,
                image = image,
                viewType = VkImageViewType.Image2D,
                format = useExternalFormat ? VkFormat.Undefined : formatInfo.format,
                components = VkComponentMapping.Identity,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, baseMipLevel: 0, levelCount: 1, baseArrayLayer: 0, layerCount: 1),
            };
            VkImageView imageView;
            device.CheckResult(device.NativeDeviceApi.vkCreateImageView(device.NativeDevice, &viewCreateInfo, null, &imageView));

            // Build a TextureDescription. For external/YUV formats Stride has no matching PixelFormat;
            // expose as R8G8B8A8_UNorm_SRgb so consumers can bind it like a normal sampled texture
            // (the YCbCr conversion in the view handles the actual decode).
            var description = TextureDescription.New2D(
                (int)desc.Width,
                (int)desc.Height,
                mipCount: 1,
                useExternalFormat ? PixelFormat.R8G8B8A8_UNorm_SRgb : VulkanConvertExtensions.ConvertPixelFormat(formatInfo.format),
                TextureFlags.ShaderResource);

            var texture = new Texture(device);
            texture.NativeImage = image;
            texture.NativeMemory = memory;
            texture.NativeImageView = imageView;
            texture.NativeSamplerYcbcrConversion = ycbcrConversion;
            texture.androidHardwareBuffer = hardwareBuffer;
            texture.NativeFormat = formatInfo.format;
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
            throw new InvalidOperationException("Vulkan: no memory type satisfies the AHardwareBuffer memoryTypeBits mask.");
        }

        // P/Invokes for libandroid AHardwareBuffer reference counting / description.
        // Available on Android API 26+ (already required by the rest of the platform).
        [DllImport("android")]
        private static extern void AHardwareBuffer_acquire(IntPtr buffer);

        [DllImport("android")]
        private static extern void AHardwareBuffer_release(IntPtr buffer);

        [DllImport("android")]
        private static extern void AHardwareBuffer_describe(IntPtr buffer, out AHardwareBufferDesc outDesc);

        [StructLayout(LayoutKind.Sequential)]
        private struct AHardwareBufferDesc
        {
            public uint Width;
            public uint Height;
            public uint Layers;
            public uint Format;
            public ulong Usage;
            public uint Stride;
            public uint Rfu0;
            public ulong Rfu1;
        }
    }
}
#endif
