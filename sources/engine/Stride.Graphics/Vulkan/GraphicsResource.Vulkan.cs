// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using Silk.NET.Vulkan;
//using static Silk.NET.Vulkan.Vk;

namespace Stride.Graphics
{
    /// <summary>
    /// GraphicsResource class
    /// </summary>
    public abstract partial class GraphicsResource
    {
        internal DeviceMemory NativeMemory;
        internal long? StagingFenceValue;
        internal CommandList StagingBuilder;
        internal PipelineStageFlags NativePipelineStageMask;

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

        protected unsafe void AllocateMemory(MemoryPropertyFlags memoryProperties, MemoryRequirements memoryRequirements)
        {
            if (NativeMemory.Handle != 0)
                return;

            if (memoryRequirements.Size == 0)
                return;

            var allocateInfo = new MemoryAllocateInfo
            {
                SType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
            };

            Vk.GetApi().GetPhysicalDeviceMemoryProperties(GraphicsDevice.NativePhysicalDevice, out var physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.MemoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *(&physicalDeviceMemoryProperties.MemoryTypes.Element0 + i);
                    if ((memoryType.PropertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.MemoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            Vk.GetApi().AllocateMemory(GraphicsDevice.NativeDevice, &allocateInfo, null, out NativeMemory);
        }
    }
}

#endif
