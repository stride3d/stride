// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResource
    {
        /// <summary>
        /// Fence value used with <see cref="GraphicsDevice.CopyFence"/> during resource initialization. Need to be waited on for CPU access.
        /// </summary>
        internal ulong? CopyFenceValue;
        /// <summary>
        /// Fence value used with <see cref="GraphicsDevice.CommandListFence"/> when resource is being written by a command list (i.e. <see cref="CommandList.Copy(GraphicsResource, GraphicsResource)"/>). Need to be waited on for CPU access.
        /// </summary>
        internal ulong? CommandListFenceValue;
        /// <summary>
        /// Command list which updated the resource (i.e. <see cref="CommandList.Copy(GraphicsResource, GraphicsResource)"/>) before it has been submitted. Will become <see cref="CommandListFenceValue"/> when command list is submitted.
        /// </summary>
        internal CommandList UpdatingCommandList;

        /// <summary>
        ///   ID of the command list that last recorded a barrier transition for this resource.
        ///   Used to detect when a resource is first used on a different command list, in which
        ///   case the barrier must be re-issued even if the layout already matches.
        /// </summary>
        internal int LastBarrierCommandListId;

        internal VkDeviceMemory NativeMemory;
        internal VkPipelineStageFlags NativePipelineStageMask;

        protected bool IsDebugMode
        {
            get
            {
                return GraphicsDevice != null && GraphicsDevice.IsDebugMode;
            }
        }

        protected override unsafe void OnNameChanged()
        {
            base.OnNameChanged();
            if (GraphicsDevice is not { IsProfilingSupported: true } || string.IsNullOrEmpty(Name))
                return;

            // A texture view shares its parent's image: naming it would rename the parent
            var (objectType, objectHandle) = this switch
            {
                Texture { ParentTexture: null } texture when texture.NativeImage != VkImage.Null => (VkObjectType.Image, texture.NativeImage.Handle),
                Buffer buffer when buffer.NativeBuffer != VkBuffer.Null => (VkObjectType.Buffer, buffer.NativeBuffer.Handle),
                _ => (VkObjectType.Unknown, 0UL),
            };
            if (objectHandle == 0)
                return;

            var name = Encoding.UTF8.GetBytes(Name + "\0");
            fixed (byte* namePointer = name)
            {
                var nameInfo = new VkDebugUtilsObjectNameInfoEXT
                {
                    sType = VkStructureType.DebugUtilsObjectNameInfoEXT,
                    objectType = objectType,
                    objectHandle = objectHandle,
                    pObjectName = namePointer,
                };
                GraphicsDevice.NativeInstanceApi.vkSetDebugUtilsObjectNameEXT(GraphicsDevice.NativeDevice, &nameInfo);
            }
        }

        protected unsafe void AllocateMemory(VkMemoryPropertyFlags memoryProperties, VkMemoryRequirements memoryRequirements)
        {
            if (NativeMemory != VkDeviceMemory.Null)
                return;

            if (memoryRequirements.size == 0)
                return;

            var allocateInfo = new VkMemoryAllocateInfo
            {
                sType = VkStructureType.MemoryAllocateInfo,
                allocationSize = memoryRequirements.size,
            };

            GraphicsDevice.NativeInstanceApi.vkGetPhysicalDeviceMemoryProperties(GraphicsDevice.NativePhysicalDevice, out var physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.memoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.memoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *(&physicalDeviceMemoryProperties.memoryTypes[0] + i);
                    if ((memoryType.propertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.memoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            GraphicsDevice.NativeDeviceApi.vkAllocateMemory(GraphicsDevice.NativeDevice, &allocateInfo, null, out NativeMemory);
        }

        /// <inheritdoc/>
        internal override void SwapInternal(GraphicsResourceBase other)
        {
            var otherResource = (GraphicsResource)other;

            base.SwapInternal(other);

            (CopyFenceValue, otherResource.CopyFenceValue)                     = (otherResource.CopyFenceValue, CopyFenceValue);
            (CommandListFenceValue, otherResource.CommandListFenceValue)       = (otherResource.CommandListFenceValue, CommandListFenceValue);
            (UpdatingCommandList, otherResource.UpdatingCommandList)           = (otherResource.UpdatingCommandList, UpdatingCommandList);
            (NativeMemory, otherResource.NativeMemory)                         = (otherResource.NativeMemory, NativeMemory);
            (NativePipelineStageMask, otherResource.NativePipelineStageMask)   = (otherResource.NativePipelineStageMask, NativePipelineStageMask);
            (IsHostVisibleHeap, otherResource.IsHostVisibleHeap)               = (otherResource.IsHostVisibleHeap, IsHostVisibleHeap);
            (LayoutTracker, otherResource.LayoutTracker)                       = (otherResource.LayoutTracker, LayoutTracker);
        }
    }
}

#endif
