// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using SharpVulkan;

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
            //            StructureType = StructureType.DebugMarkerObjectNameInfo,
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
            if (NativeMemory != DeviceMemory.Null)
                return;

            if (memoryRequirements.Size == 0)
                return;

            var allocateInfo = new MemoryAllocateInfo
            {
                StructureType = StructureType.MemoryAllocateInfo,
                AllocationSize = memoryRequirements.Size,
            };

            PhysicalDeviceMemoryProperties physicalDeviceMemoryProperties;
            GraphicsDevice.NativePhysicalDevice.GetMemoryProperties(out physicalDeviceMemoryProperties);
            var typeBits = memoryRequirements.MemoryTypeBits;
            for (uint i = 0; i < physicalDeviceMemoryProperties.MemoryTypeCount; i++)
            {
                if ((typeBits & 1) == 1)
                {
                    // Type is available, does it match user properties?
                    var memoryType = *((MemoryType*)&physicalDeviceMemoryProperties.MemoryTypes + i);
                    if ((memoryType.PropertyFlags & memoryProperties) == memoryProperties)
                    {
                        allocateInfo.MemoryTypeIndex = i;
                        break;
                    }
                }
                typeBits >>= 1;
            }

            NativeMemory = GraphicsDevice.NativeDevice.AllocateMemory(ref allocateInfo);
        }
    }
}

#endif
