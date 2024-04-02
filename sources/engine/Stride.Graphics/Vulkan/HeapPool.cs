// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System.Linq;

using VK = Silk.NET.Vulkan;
using static Silk.NET.Vulkan.Vk;
using Silk.NET.Vulkan;

namespace Stride.Graphics
{
    internal class HeapPool : ResourcePool<VK.DescriptorPool>
    {
        private readonly Vk vk;

        public HeapPool(GraphicsDevice graphicsDevice) : base(graphicsDevice)
        {
            vk = GetApi();
        }

        protected override unsafe VK.DescriptorPool CreateObject()
        {
            // No allocator ready to be used, let's create a new one
            var poolSizes = GraphicsDevice.MaxDescriptorTypeCounts
                .Select((count, index) => new VK.DescriptorPoolSize { Type = (VK.DescriptorType)index, DescriptorCount = count })
                .Where(size => size.DescriptorCount > 0)
                .ToArray();

            fixed (VK.DescriptorPoolSize* fPoolSizes = poolSizes) { // null if array is empty or null
                var descriptorPoolCreateInfo = new VK.DescriptorPoolCreateInfo
                {
                    SType = VK.StructureType.DescriptorPoolCreateInfo,
                    PoolSizeCount = (uint)poolSizes.Length,
                    PPoolSizes = fPoolSizes,
                    MaxSets = GraphicsDevice.MaxDescriptorSetCount,
                };
                vk.CreateDescriptorPool(GraphicsDevice.NativeDevice, &descriptorPoolCreateInfo, null, out var descriptorPool);
                return descriptorPool;
            }
        }

        protected override void ResetObject(VK.DescriptorPool obj)
        {
            vk.ResetDescriptorPool(GraphicsDevice.NativeDevice, obj, 0);
        }

        protected override unsafe void DestroyObject(VK.DescriptorPool obj)
        {
            vk.DestroyDescriptorPool(GraphicsDevice.NativeDevice, obj, null);
        }
    }
}
#endif
