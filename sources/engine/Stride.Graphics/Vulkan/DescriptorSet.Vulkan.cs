// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN && !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
using System;
using System.Collections.Generic;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Shaders;

namespace Stride.Graphics
{
    public partial struct DescriptorSet
    {
        internal readonly VkDescriptorSet NativeDescriptorSet;
        internal readonly GraphicsDevice GraphicsDevice;
        
        public bool IsValid => NativeDescriptorSet != VkDescriptorSet.Null;

        private DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
        {
            GraphicsDevice = graphicsDevice;
            NativeDescriptorSet = pool.AllocateDescriptorSet(desc);
        }

        /// <summary>
        /// Sets a descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="value">The descriptor.</param>
        public void SetValue(int slot, object value)
        {
            var srv = value as GraphicsResource;
            if (srv != null)
            {
                SetShaderResourceView(slot, srv);
            }
            else
            {
                var sampler = value as SamplerState;
                if (sampler != null)
                {
                    SetSamplerState(slot, sampler);
                }
            }
        }

        /// <summary>
        /// Sets a shader resource view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        public unsafe void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                descriptorCount = 1,
                dstSet = NativeDescriptorSet,
                dstBinding = (uint)slot,
                dstArrayElement = 0,
            };

            var texture = shaderResourceView as Texture;
            if (texture != null)
            {
                var imageInfo = new VkDescriptorImageInfo { imageView = texture.NativeImageView, imageLayout = VkImageLayout.ShaderReadOnlyOptimal };

                write.descriptorType = VkDescriptorType.SampledImage;
                write.pImageInfo = &imageInfo;

                GraphicsDevice.NativeDeviceApi.vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, 1, &write, 0, null);
            }
            else
            {
                var buffer = shaderResourceView as Buffer;
                if (buffer != null)
                {
                    var bufferViewCopy = buffer.NativeBufferView;

                    write.descriptorType = VkDescriptorType.UniformTexelBuffer;
                    write.pTexelBufferView = &bufferViewCopy;

                    GraphicsDevice.NativeDeviceApi.vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, 1, &write, 0, null);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        /// <summary>
        /// Sets a sampler state descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The sampler state.</param>
        public unsafe void SetSamplerState(int slot, SamplerState samplerState)
        {
            var imageInfo = new VkDescriptorImageInfo { sampler = samplerState.NativeSampler };

            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                descriptorCount = 1,
                dstSet = NativeDescriptorSet,
                dstBinding = (uint)slot,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.Sampler,
                pImageInfo = &imageInfo,
            };

            GraphicsDevice.NativeDeviceApi.vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, 1, &write, 0, null);
        }

        /// <summary>
        /// Sets a constant buffer view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="buffer">The constant buffer.</param>
        /// <param name="offset">The constant buffer view start offset.</param>
        /// <param name="size">The constant buffer view size.</param>
        public unsafe void SetConstantBuffer(int slot, Buffer buffer, int offset, int size)
        {
            var bufferInfo = new VkDescriptorBufferInfo { buffer = buffer.NativeBuffer, offset = (ulong)offset, range = (ulong)size };

            var write = new VkWriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                descriptorCount = 1,
                dstSet = NativeDescriptorSet,
                dstBinding = (uint)slot,
                dstArrayElement = 0,
                descriptorType = VkDescriptorType.UniformBuffer,
                pBufferInfo = &bufferInfo
            };
            
            GraphicsDevice.NativeDeviceApi.vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, 1, &write, 0, null);
        }

        /// <summary>
        /// Sets an unordered access view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            // TODO D3D12
            throw new NotImplementedException();
        }
    }
}
#endif
