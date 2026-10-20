// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using System;
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
    ///   Records barriers in a command buffer, as one <c>vkCmdPipelineBarrier2</c>.
    /// </summary>
    public static void Record(VkDeviceApi api, VkCommandBuffer commandBuffer,
                              ReadOnlySpan<VkMemoryBarrier2> memoryBarriers = default,
                              ReadOnlySpan<VkBufferMemoryBarrier2> bufferBarriers = default,
                              ReadOnlySpan<VkImageMemoryBarrier2> imageBarriers = default)
    {
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
}

#endif
