// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Mathematics;
using Xunit;

#if STRIDE_GRAPHICS_API_VULKAN
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
#endif

namespace Stride.Graphics.Tests;

/// <summary>
/// <see cref="ExportableTexture"/>: a Vulkan Texture whose memory and semaphores another device imports (a UI compositor).
/// The importer here is a second, raw Vulkan device that imports the memory and the semaphores the way Avalonia's Vulkan
/// compositor does (same image parameters, transfer source layout, binary semaphores), and reads a pixel of each frame.
/// </summary>
public class TestExportableTexture : GraphicTestGameBase
{
    private const int Width = 64;
    private const int Height = 32;

    public TestExportableTexture()
    {
        // Before the device is created: only then does it enable the external memory and semaphore extensions
        ExportableTexture.EnableExternalObjects = true;
    }

    [SkippableFact]
    public void AnotherBackendCannotExport()
    {
        PerformTest(game =>
        {
            Skip.If(GraphicsDevice.Platform == GraphicsPlatform.Vulkan, "Vulkan exports textures.");

            Assert.False(ExportableTexture.IsSupported(game.GraphicsDevice));
            Assert.Throws<NotSupportedException>(() => ExportableTexture.Create(game.GraphicsDevice, Width, Height));
        });
    }

#if STRIDE_GRAPHICS_API_VULKAN
    [SkippableFact]
    public void TheImporterReadsEveryFrameInOrder()
    {
        PerformTest(game =>
        {
            var device = game.GraphicsDevice;
            Skip.IfNot(ExportableTexture.IsSupported(device), $"{device.Adapter.Description} has no VK_KHR_external_memory_fd and VK_KHR_external_semaphore_fd (lavapipe has none).");

            using var exportable = ExportableTexture.Create(device, Width, Height);
            Assert.Equal(Width, exportable.Texture.Width);
            Assert.Equal(PixelFormat.R8G8B8A8_UNorm, exportable.Texture.Format);
            Assert.True(exportable.MemorySize >= Width * Height * 4);

            using var renderTarget = Texture.New2D(device, Width, Height, PixelFormat.R8G8B8A8_UNorm, TextureFlags.RenderTarget | TextureFlags.ShaderResource);
            using var commandList = CommandList.New(device);
            // The importer owns the file descriptors it gets
            using var importer = new Importer(exportable.ExportMemory(), exportable.MemorySize, exportable.ExportReadySemaphore(), exportable.ExportReleasedSemaphore());

            var colors = new[] { new Color4(1, 0, 0, 1), new Color4(0, 1, 0, 1), new Color4(0, 0, 1, 1), new Color4(1, 1, 0, 1) };
            for (var frame = 0; frame < colors.Length; frame++)
            {
                // The importer must be done with the previous frame
                if (frame > 0)
                    exportable.WaitReleased();
                commandList.Clear(renderTarget, colors[frame]);
                commandList.Copy(renderTarget, exportable.Texture);
                // The importer reads the image in the transfer source layout
                commandList.ResourceBarrierTransition(exportable.Texture, BarrierLayout.CopySource);
                commandList.Flush();
                commandList.Reset();
                exportable.SignalReady();

                var pixel = importer.ReadFrame();
                var expected = (uint)((byte)(colors[frame].R * 255) | (byte)(colors[frame].G * 255) << 8 | (byte)(colors[frame].B * 255) << 16 | 0xFF << 24);
                Assert.True(pixel == expected, $"Frame {frame}: the importer read 0x{pixel:X8}, expected 0x{expected:X8}");
            }
        });
    }

    /// <summary>
    /// A second Vulkan device, which imports the exported memory and semaphores like a UI compositor does.
    /// </summary>
    private sealed unsafe class Importer : IDisposable
    {
        private readonly VkInstance instance;
        private readonly VkInstanceApi instanceApi;
        private readonly VkDevice device;
        private readonly VkDeviceApi api;
        private readonly VkQueue queue;
        private readonly VkImage image;
        private readonly VkDeviceMemory imageMemory;
        private readonly VkBuffer buffer;
        private readonly VkDeviceMemory bufferMemory;
        private readonly VkSemaphore ready;
        private readonly VkSemaphore released;
        private readonly VkCommandPool pool;
        private readonly VkCommandBuffer commandBuffer;
        private readonly VkFence fence;

        public Importer(int memoryFd, ulong memorySize, int readyFd, int releasedFd)
        {
            vkInitialize().CheckResult();
            var appInfo = new VkApplicationInfo { apiVersion = new VkVersion(1, 3, 0) };
            var instanceInfo = new VkInstanceCreateInfo { pApplicationInfo = &appInfo };
            vkCreateInstance(&instanceInfo, out instance).CheckResult();
            instanceApi = GetApi(instance);

            uint count = 0;
            instanceApi.vkEnumeratePhysicalDevices(instance, &count, null).CheckResult();
            var physicalDevices = new VkPhysicalDevice[count];
            instanceApi.vkEnumeratePhysicalDevices(instance, physicalDevices).CheckResult();
            var physical = physicalDevices[0];

            var priority = 1.0f;
            var queueInfo = new VkDeviceQueueCreateInfo { queueFamilyIndex = 0, queueCount = 1, pQueuePriorities = &priority };
            using var extensions = new VkStringArray(new List<VkUtf8String> { VK_KHR_EXTERNAL_MEMORY_FD_EXTENSION_NAME, VK_KHR_EXTERNAL_SEMAPHORE_FD_EXTENSION_NAME });
            var deviceInfo = new VkDeviceCreateInfo
            {
                queueCreateInfoCount = 1,
                pQueueCreateInfos = &queueInfo,
                enabledExtensionCount = extensions.Length,
                ppEnabledExtensionNames = extensions,
            };
            instanceApi.vkCreateDevice(physical, &deviceInfo, null, out device).CheckResult();
            api = new VkDeviceApi(instanceApi, in device);
            api.vkGetDeviceQueue(device, 0, 0, out queue);
            instanceApi.vkGetPhysicalDeviceMemoryProperties(physical, out var memoryProperties);

            // The image as Avalonia's Vulkan compositor imports it: mutable format, a dedicated allocation bound to the fd
            var external = new VkExternalMemoryImageCreateInfo { handleTypes = VkExternalMemoryHandleTypeFlags.OpaqueFD };
            var imageInfo = new VkImageCreateInfo
            {
                pNext = &external,
                flags = VkImageCreateFlags.MutableFormat,
                imageType = VkImageType.Image2D,
                format = VkFormat.R8G8B8A8Unorm,
                extent = new VkExtent3D(Width, Height, 1),
                mipLevels = 1,
                arrayLayers = 1,
                samples = VkSampleCountFlags.Count1,
                tiling = VkImageTiling.Optimal,
                usage = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst | VkImageUsageFlags.TransferSrc | VkImageUsageFlags.Sampled,
                sharingMode = VkSharingMode.Exclusive,
                initialLayout = VkImageLayout.Undefined,
            };
            api.vkCreateImage(device, &imageInfo, null, out image).CheckResult();
            api.vkGetImageMemoryRequirements(device, image, out var imageRequirements);
            var dedicated = new VkMemoryDedicatedAllocateInfo { image = image };
            var import = new VkImportMemoryFdInfoKHR { pNext = &dedicated, handleType = VkExternalMemoryHandleTypeFlags.OpaqueFD, fd = memoryFd };
            var imageAllocation = new VkMemoryAllocateInfo
            {
                pNext = &import,
                allocationSize = memorySize,
                memoryTypeIndex = FindMemoryType(memoryProperties, imageRequirements.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal),
            };
            api.vkAllocateMemory(device, &imageAllocation, null, out imageMemory).CheckResult();
            api.vkBindImageMemory(device, image, imageMemory, 0).CheckResult();

            ready = ImportSemaphore(readyFd);
            released = ImportSemaphore(releasedFd);

            var bufferInfo = new VkBufferCreateInfo { size = Width * Height * 4, usage = VkBufferUsageFlags.TransferDst, sharingMode = VkSharingMode.Exclusive };
            api.vkCreateBuffer(device, &bufferInfo, null, out buffer).CheckResult();
            api.vkGetBufferMemoryRequirements(device, buffer, out var bufferRequirements);
            var bufferAllocation = new VkMemoryAllocateInfo
            {
                allocationSize = bufferRequirements.size,
                memoryTypeIndex = FindMemoryType(memoryProperties, bufferRequirements.memoryTypeBits, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent),
            };
            api.vkAllocateMemory(device, &bufferAllocation, null, out bufferMemory).CheckResult();
            api.vkBindBufferMemory(device, buffer, bufferMemory, 0).CheckResult();

            var poolInfo = new VkCommandPoolCreateInfo { flags = VkCommandPoolCreateFlags.ResetCommandBuffer, queueFamilyIndex = 0 };
            api.vkCreateCommandPool(device, &poolInfo, null, out pool).CheckResult();
            var allocateInfo = new VkCommandBufferAllocateInfo { commandPool = pool, level = VkCommandBufferLevel.Primary, commandBufferCount = 1 };
            VkCommandBuffer allocated;
            api.vkAllocateCommandBuffers(device, &allocateInfo, &allocated).CheckResult();
            commandBuffer = allocated;
            var fenceInfo = new VkFenceCreateInfo();
            api.vkCreateFence(device, &fenceInfo, null, out fence).CheckResult();
        }

        /// <summary>
        /// Waits for the ready semaphore, copies the image to a buffer, signals the released semaphore and returns the
        /// pixel at (0, 0), as the bytes R, G, B, A.
        /// </summary>
        public uint ReadFrame()
        {
            api.vkResetCommandBuffer(commandBuffer, VkCommandBufferResetFlags.None).CheckResult();
            var begin = new VkCommandBufferBeginInfo { flags = VkCommandBufferUsageFlags.OneTimeSubmit };
            api.vkBeginCommandBuffer(commandBuffer, &begin).CheckResult();
            var region = new VkBufferImageCopy
            {
                imageSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
                imageExtent = new VkExtent3D(Width, Height, 1),
            };
            api.vkCmdCopyImageToBuffer(commandBuffer, image, VkImageLayout.TransferSrcOptimal, buffer, 1, &region);
            api.vkEndCommandBuffer(commandBuffer).CheckResult();

            var waitStage = VkPipelineStageFlags.Transfer;
            var readySemaphore = ready;
            var releasedSemaphore = released;
            var submitCommandBuffer = commandBuffer;
            var submit = new VkSubmitInfo
            {
                waitSemaphoreCount = 1,
                pWaitSemaphores = &readySemaphore,
                pWaitDstStageMask = &waitStage,
                commandBufferCount = 1,
                pCommandBuffers = &submitCommandBuffer,
                signalSemaphoreCount = 1,
                pSignalSemaphores = &releasedSemaphore,
            };
            var submitFence = fence;
            api.vkQueueSubmit(queue, 1, &submit, submitFence).CheckResult();
            api.vkWaitForFences(device, 1, &submitFence, true, ulong.MaxValue).CheckResult();
            api.vkResetFences(device, 1, &submitFence).CheckResult();

            void* data;
            api.vkMapMemory(device, bufferMemory, 0, Width * Height * 4, VkMemoryMapFlags.None, &data).CheckResult();
            var pixel = *(uint*)data;
            api.vkUnmapMemory(device, bufferMemory);
            return pixel;
        }

        public void Dispose()
        {
            api.vkDeviceWaitIdle(device);
            api.vkDestroyFence(device, fence, null);
            api.vkDestroyCommandPool(device, pool, null);
            api.vkDestroyBuffer(device, buffer, null);
            api.vkFreeMemory(device, bufferMemory, null);
            api.vkDestroySemaphore(device, ready, null);
            api.vkDestroySemaphore(device, released, null);
            api.vkDestroyImage(device, image, null);
            api.vkFreeMemory(device, imageMemory, null);
            api.vkDestroyDevice(device, null);
            instanceApi.vkDestroyInstance(instance, null);
        }

        private VkSemaphore ImportSemaphore(int fd)
        {
            var createInfo = new VkSemaphoreCreateInfo();
            api.vkCreateSemaphore(device, &createInfo, null, out var semaphore).CheckResult();
            var importInfo = new VkImportSemaphoreFdInfoKHR { semaphore = semaphore, handleType = VkExternalSemaphoreHandleTypeFlags.OpaqueFD, fd = fd };
            api.vkImportSemaphoreFdKHR(device, &importInfo).CheckResult();
            return semaphore;
        }

        private static uint FindMemoryType(VkPhysicalDeviceMemoryProperties properties, uint typeBits, VkMemoryPropertyFlags flags)
        {
            for (var i = 0; i < properties.memoryTypeCount; i++)
            {
                if ((typeBits & (1u << i)) != 0 && (properties.memoryTypes[i].propertyFlags & flags) == flags)
                    return (uint)i;
            }
            throw new InvalidOperationException("No memory type fits the imported image.");
        }
    }
#endif
}
