// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using System;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics;

/// <summary>
///   Builds and records pipeline barriers with synchronization2 (<c>vkCmdPipelineBarrier2</c>), where each barrier has
///   its own stages and accesses.
/// </summary>
internal static unsafe class PipelineBarriers
{
    /// <summary>
    ///   Gets a global memory barrier.
    /// </summary>
    public static VkMemoryBarrier2 MemoryBarrier(VkPipelineStageFlags srcStages, VkAccessFlags srcAccess,
                                                 VkPipelineStageFlags dstStages, VkAccessFlags dstAccess) => new()
    {
        sType = VkStructureType.MemoryBarrier2,
        srcStageMask = (VkPipelineStageFlags2) (uint) srcStages,
        srcAccessMask = (VkAccessFlags2) (uint) srcAccess,
        dstStageMask = (VkPipelineStageFlags2) (uint) dstStages,
        dstAccessMask = (VkAccessFlags2) (uint) dstAccess,
    };

    /// <summary>
    ///   Gets a barrier on a buffer, or on a range of it.
    /// </summary>
    public static VkBufferMemoryBarrier2 BufferBarrier(VkBuffer buffer,
                                                       VkPipelineStageFlags srcStages, VkAccessFlags srcAccess,
                                                       VkPipelineStageFlags dstStages, VkAccessFlags dstAccess,
                                                       ulong offset = 0, ulong size = VK_WHOLE_SIZE) => new()
    {
        sType = VkStructureType.BufferMemoryBarrier2,
        srcStageMask = (VkPipelineStageFlags2) (uint) srcStages,
        srcAccessMask = (VkAccessFlags2) (uint) srcAccess,
        dstStageMask = (VkPipelineStageFlags2) (uint) dstStages,
        dstAccessMask = (VkAccessFlags2) (uint) dstAccess,
        srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
        dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
        buffer = buffer,
        offset = offset,
        size = size,
    };

    /// <summary>
    ///   Gets a barrier on subresources of an image, which moves them between two layouts.
    /// </summary>
    public static VkImageMemoryBarrier2 ImageBarrier(VkImage image, in VkImageSubresourceRange range,
                                                     VkPipelineStageFlags srcStages, VkAccessFlags srcAccess, VkImageLayout oldLayout,
                                                     VkPipelineStageFlags dstStages, VkAccessFlags dstAccess, VkImageLayout newLayout) => new()
    {
        sType = VkStructureType.ImageMemoryBarrier2,
        srcStageMask = (VkPipelineStageFlags2) (uint) srcStages,
        srcAccessMask = (VkAccessFlags2) (uint) srcAccess,
        dstStageMask = (VkPipelineStageFlags2) (uint) dstStages,
        dstAccessMask = (VkAccessFlags2) (uint) dstAccess,
        oldLayout = oldLayout,
        newLayout = newLayout,
        srcQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
        dstQueueFamilyIndex = VK_QUEUE_FAMILY_IGNORED,
        image = image,
        subresourceRange = range,
    };

    /// <summary>
    ///   Records barriers in a command buffer, as one <c>vkCmdPipelineBarrier2</c> (or one <c>vkCmdPipelineBarrier</c>,
    ///   see <see cref="UseLegacyBarriers"/>).
    /// </summary>
    public static void Record(VkDeviceApi api, VkCommandBuffer commandBuffer,
                              ReadOnlySpan<VkMemoryBarrier2> memoryBarriers = default,
                              ReadOnlySpan<VkBufferMemoryBarrier2> bufferBarriers = default,
                              ReadOnlySpan<VkImageMemoryBarrier2> imageBarriers = default)
    {
        if (UseLegacyBarriers)
        {
            RecordLegacy(api, commandBuffer, memoryBarriers, bufferBarriers, imageBarriers);
            return;
        }

        fixed (VkMemoryBarrier2* pMemoryBarriers = memoryBarriers)
        fixed (VkBufferMemoryBarrier2* pBufferBarriers = bufferBarriers)
        fixed (VkImageMemoryBarrier2* pImageBarriers = imageBarriers)
        {
            var dependencyInfo = new VkDependencyInfo
            {
                sType = VkStructureType.DependencyInfo,
                memoryBarrierCount = (uint) memoryBarriers.Length,
                pMemoryBarriers = pMemoryBarriers,
                bufferMemoryBarrierCount = (uint) bufferBarriers.Length,
                pBufferMemoryBarriers = pBufferBarriers,
                imageMemoryBarrierCount = (uint) imageBarriers.Length,
                pImageMemoryBarriers = pImageBarriers,
            };
            api.vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
        }
    }

    /// <summary>
    ///   Whether barriers are recorded with the legacy <c>vkCmdPipelineBarrier</c>, on the Android emulator. Its Vulkan driver
    ///   (gfxstream) decompresses the ETC2 / ASTC textures it emulates in <c>vkCmdPipelineBarrier</c> only, so after a
    ///   <c>vkCmdPipelineBarrier2</c> they are sampled still compressed (fixed in gfxstream 9068c3aa80, not in emulator 37.2.12).
    /// </summary>
    internal static readonly bool UseLegacyBarriers = IsGfxstreamVulkanDriver();

#if STRIDE_PLATFORM_ANDROID
    [DllImport("libc.so")]
    private static extern int __system_property_get(string name, byte* value);

    private static bool IsGfxstreamVulkanDriver()
    {
        // Android loads vulkan.<ro.hardware.vulkan>.so: gfxstream's guest driver is vulkan.ranchu.so
        const int MaxPropertyValueLength = 92;
        var value = stackalloc byte[MaxPropertyValueLength];
        int length = __system_property_get("ro.hardware.vulkan", value);
        return length > 0 && new ReadOnlySpan<byte>(value, length).SequenceEqual("ranchu"u8);
    }
#else
    private static bool IsGfxstreamVulkanDriver() => false;
#endif

    // Records the barriers as one legacy vkCmdPipelineBarrier: the stages of all barriers merge into one source and one destination mask
    private static void RecordLegacy(VkDeviceApi api, VkCommandBuffer commandBuffer,
                                     ReadOnlySpan<VkMemoryBarrier2> memoryBarriers,
                                     ReadOnlySpan<VkBufferMemoryBarrier2> bufferBarriers,
                                     ReadOnlySpan<VkImageMemoryBarrier2> imageBarriers)
    {
        VkPipelineStageFlags srcStages = 0, dstStages = 0;
        var legacyMemoryBarriers = new VkMemoryBarrier[memoryBarriers.Length];
        var legacyBufferBarriers = new VkBufferMemoryBarrier[bufferBarriers.Length];
        var legacyImageBarriers = new VkImageMemoryBarrier[imageBarriers.Length];

        for (int i = 0; i < memoryBarriers.Length; i++)
        {
            ref readonly var barrier = ref memoryBarriers[i];
            srcStages |= (VkPipelineStageFlags) (uint) barrier.srcStageMask;
            dstStages |= (VkPipelineStageFlags) (uint) barrier.dstStageMask;
            legacyMemoryBarriers[i] = new VkMemoryBarrier
            {
                sType = VkStructureType.MemoryBarrier,
                srcAccessMask = (VkAccessFlags) (uint) barrier.srcAccessMask,
                dstAccessMask = (VkAccessFlags) (uint) barrier.dstAccessMask,
            };
        }
        for (int i = 0; i < bufferBarriers.Length; i++)
        {
            ref readonly var barrier = ref bufferBarriers[i];
            srcStages |= (VkPipelineStageFlags) (uint) barrier.srcStageMask;
            dstStages |= (VkPipelineStageFlags) (uint) barrier.dstStageMask;
            legacyBufferBarriers[i] = new VkBufferMemoryBarrier
            {
                sType = VkStructureType.BufferMemoryBarrier,
                srcAccessMask = (VkAccessFlags) (uint) barrier.srcAccessMask,
                dstAccessMask = (VkAccessFlags) (uint) barrier.dstAccessMask,
                srcQueueFamilyIndex = barrier.srcQueueFamilyIndex,
                dstQueueFamilyIndex = barrier.dstQueueFamilyIndex,
                buffer = barrier.buffer,
                offset = barrier.offset,
                size = barrier.size,
            };
        }
        for (int i = 0; i < imageBarriers.Length; i++)
        {
            ref readonly var barrier = ref imageBarriers[i];
            srcStages |= (VkPipelineStageFlags) (uint) barrier.srcStageMask;
            dstStages |= (VkPipelineStageFlags) (uint) barrier.dstStageMask;
            legacyImageBarriers[i] = new VkImageMemoryBarrier
            {
                sType = VkStructureType.ImageMemoryBarrier,
                srcAccessMask = (VkAccessFlags) (uint) barrier.srcAccessMask,
                dstAccessMask = (VkAccessFlags) (uint) barrier.dstAccessMask,
                oldLayout = barrier.oldLayout,
                newLayout = barrier.newLayout,
                srcQueueFamilyIndex = barrier.srcQueueFamilyIndex,
                dstQueueFamilyIndex = barrier.dstQueueFamilyIndex,
                image = barrier.image,
                subresourceRange = barrier.subresourceRange,
            };
        }

        // No stage to wait for, or none to block: the legacy stages that mean the same as an empty mask
        if (srcStages == 0)
            srcStages = VkPipelineStageFlags.TopOfPipe;
        if (dstStages == 0)
            dstStages = VkPipelineStageFlags.BottomOfPipe;

        fixed (VkMemoryBarrier* pMemoryBarriers = legacyMemoryBarriers)
        fixed (VkBufferMemoryBarrier* pBufferBarriers = legacyBufferBarriers)
        fixed (VkImageMemoryBarrier* pImageBarriers = legacyImageBarriers)
        {
            api.vkCmdPipelineBarrier(commandBuffer, srcStages, dstStages, VkDependencyFlags.None,
                                     (uint) legacyMemoryBarriers.Length, pMemoryBarriers,
                                     (uint) legacyBufferBarriers.Length, pBufferBarriers,
                                     (uint) legacyImageBarriers.Length, pImageBarriers);
        }
    }
}

#endif
