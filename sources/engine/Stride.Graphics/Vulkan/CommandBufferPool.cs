// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using VK = Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
using Silk.NET.Vulkan;

namespace Stride.Graphics
{
    internal class CommandBufferPool : ResourcePool<VK.CommandBuffer>
    {
        private readonly VK.CommandPool commandPool;
        private readonly Vk vk;

        public unsafe CommandBufferPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            vk = GetApi();
            var commandPoolCreateInfo = new VK.CommandPoolCreateInfo
            {
                SType = VK.StructureType.CommandPoolCreateInfo,
                QueueFamilyIndex = 0, //device.NativeCommandQueue.FamilyIndex
                Flags = VK.CommandPoolCreateFlags.ResetCommandBufferBit
            };

            vk.CreateCommandPool(graphicsDevice.NativeDevice, &commandPoolCreateInfo, null, out commandPool);
        }

        protected override unsafe VK.CommandBuffer CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var commandBufferAllocationInfo = new VK.CommandBufferAllocateInfo
            {
                SType = VK.StructureType.CommandBufferAllocateInfo,
                Level = VK.CommandBufferLevel.Primary,
                CommandPool = commandPool,
                CommandBufferCount = 1,
            };

            VK.CommandBuffer commandBuffer;
            vk.AllocateCommandBuffers(GraphicsDevice.NativeDevice, &commandBufferAllocationInfo, &commandBuffer);
            return commandBuffer;
        }

        protected override void ResetObject(VK.CommandBuffer obj)
        {
            vk.ResetCommandBuffer(obj, VK.CommandBufferResetFlags.ReleaseResourcesBit);
        }

        protected override unsafe void Destroy()
        {
            base.Destroy();

            vk.DestroyCommandPool(GraphicsDevice.NativeDevice, commandPool, null);
        }
    }
}
#endif
