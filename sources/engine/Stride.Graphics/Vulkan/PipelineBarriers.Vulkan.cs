// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_VULKAN

using Vortice.Vulkan;

namespace Stride.Graphics;

/// <summary>
///   Records pipeline barriers with synchronization2 (<c>vkCmdPipelineBarrier2</c>), where each barrier has its own
///   stages and accesses.
/// </summary>
internal static unsafe class PipelineBarriers
{
    /// <summary>
    ///   Records the barriers of a <c>vkCmdPipelineBarrier</c> call with <c>vkCmdPipelineBarrier2</c>: every barrier
    ///   gets the given stages.
    /// </summary>
    public static void Record(VkDeviceApi api, VkCommandBuffer commandBuffer,
                              VkPipelineStageFlags srcStageMask, VkPipelineStageFlags dstStageMask, VkDependencyFlags dependencyFlags,
                              uint memoryBarrierCount, VkMemoryBarrier* memoryBarriers,
                              uint bufferMemoryBarrierCount, VkBufferMemoryBarrier* bufferMemoryBarriers,
                              uint imageMemoryBarrierCount, VkImageMemoryBarrier* imageMemoryBarriers)
    {
        var srcStages = (VkPipelineStageFlags2) (uint) srcStageMask;
        var dstStages = (VkPipelineStageFlags2) (uint) dstStageMask;

        var memoryBarriers2 = stackalloc VkMemoryBarrier2[(int) memoryBarrierCount];
        for (int i = 0; i < memoryBarrierCount; i++)
        {
            ref var barrier = ref memoryBarriers[i];
            memoryBarriers2[i] = new VkMemoryBarrier2
            {
                sType = VkStructureType.MemoryBarrier2,
                srcStageMask = srcStages,
                srcAccessMask = (VkAccessFlags2) (uint) barrier.srcAccessMask,
                dstStageMask = dstStages,
                dstAccessMask = (VkAccessFlags2) (uint) barrier.dstAccessMask,
            };
        }

        var bufferBarriers2 = stackalloc VkBufferMemoryBarrier2[(int) bufferMemoryBarrierCount];
        for (int i = 0; i < bufferMemoryBarrierCount; i++)
        {
            ref var barrier = ref bufferMemoryBarriers[i];
            bufferBarriers2[i] = new VkBufferMemoryBarrier2
            {
                sType = VkStructureType.BufferMemoryBarrier2,
                srcStageMask = srcStages,
                srcAccessMask = (VkAccessFlags2) (uint) barrier.srcAccessMask,
                dstStageMask = dstStages,
                dstAccessMask = (VkAccessFlags2) (uint) barrier.dstAccessMask,
                srcQueueFamilyIndex = barrier.srcQueueFamilyIndex,
                dstQueueFamilyIndex = barrier.dstQueueFamilyIndex,
                buffer = barrier.buffer,
                offset = barrier.offset,
                size = barrier.size,
            };
        }

        var imageBarriers2 = stackalloc VkImageMemoryBarrier2[(int) imageMemoryBarrierCount];
        for (int i = 0; i < imageMemoryBarrierCount; i++)
        {
            ref var barrier = ref imageMemoryBarriers[i];
            imageBarriers2[i] = new VkImageMemoryBarrier2
            {
                sType = VkStructureType.ImageMemoryBarrier2,
                srcStageMask = srcStages,
                srcAccessMask = (VkAccessFlags2) (uint) barrier.srcAccessMask,
                dstStageMask = dstStages,
                dstAccessMask = (VkAccessFlags2) (uint) barrier.dstAccessMask,
                oldLayout = barrier.oldLayout,
                newLayout = barrier.newLayout,
                srcQueueFamilyIndex = barrier.srcQueueFamilyIndex,
                dstQueueFamilyIndex = barrier.dstQueueFamilyIndex,
                image = barrier.image,
                subresourceRange = barrier.subresourceRange,
            };
        }

        Record(api, commandBuffer, dependencyFlags, memoryBarrierCount, memoryBarriers2, bufferMemoryBarrierCount, bufferBarriers2, imageMemoryBarrierCount, imageBarriers2);
    }

    /// <summary>
    ///   Records synchronization2 barriers.
    /// </summary>
    public static void Record(VkDeviceApi api, VkCommandBuffer commandBuffer, VkDependencyFlags dependencyFlags,
                              uint memoryBarrierCount, VkMemoryBarrier2* memoryBarriers,
                              uint bufferMemoryBarrierCount, VkBufferMemoryBarrier2* bufferMemoryBarriers,
                              uint imageMemoryBarrierCount, VkImageMemoryBarrier2* imageMemoryBarriers)
    {
        var dependencyInfo = new VkDependencyInfo
        {
            sType = VkStructureType.DependencyInfo,
            dependencyFlags = dependencyFlags,
            memoryBarrierCount = memoryBarrierCount,
            pMemoryBarriers = memoryBarriers,
            bufferMemoryBarrierCount = bufferMemoryBarrierCount,
            pBufferMemoryBarriers = bufferMemoryBarriers,
            imageMemoryBarrierCount = imageMemoryBarrierCount,
            pImageMemoryBarriers = imageMemoryBarriers,
        };
        api.vkCmdPipelineBarrier2(commandBuffer, &dependencyInfo);
    }
}

#endif
