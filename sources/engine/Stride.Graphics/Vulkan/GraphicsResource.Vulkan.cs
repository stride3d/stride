// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
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
            //if (GraphicsDevice != null && GraphicsDevice.IsProfilingSupported)
            //{
            //    if (string.IsNullOrEmpty(Name))
            //        return;

            //    var bytes = System.Text.Encoding.ASCII.GetBytes(Name);

            //    fixed (byte* bytesPointer = &bytes[0])
            //    {
            //        var nameInfo = new DebugMarkerObjectNameInfo
            //        {
            //            sType = VkStructureType.DebugMarkerObjectNameInfo,
            //            Object = ,
            //            ObjectName = new IntPtr(bytesPointer),
            //            ObjectType =
            //        };
            //        GraphicsDevice.NativeDevice.DebugMarkerSetObjectName(ref nameInfo);
            //    }
            //}
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

            (CopyFenceValue, otherResource.CommandListFenceValue)              = (otherResource.CopyFenceValue, CommandListFenceValue);
            (CommandListFenceValue, otherResource.CommandListFenceValue)       = (otherResource.CommandListFenceValue, CommandListFenceValue);
            (UpdatingCommandList, otherResource.UpdatingCommandList)           = (otherResource.UpdatingCommandList, UpdatingCommandList);
            (NativeMemory, otherResource.NativeMemory)                         = (otherResource.NativeMemory, NativeMemory);
            (NativePipelineStageMask, otherResource.NativePipelineStageMask)   = (otherResource.NativePipelineStageMask, NativePipelineStageMask);
        }
    }
}

#endif
