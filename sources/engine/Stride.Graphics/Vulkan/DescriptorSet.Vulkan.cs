// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
            var write = new WriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DestinationSet = NativeDescriptorSet,
                DestinationBinding = (uint)slot,
                DestinationArrayElement = 0,
            };

            var texture = shaderResourceView as Texture;
            if (texture != null)
            {
                var imageInfo = new DescriptorImageInfo { VkImageView = texture.NativeImageView, ImageLayout = VkImageLayout.ShaderReadOnlyOptimal };

                write.VkDescriptorType = VkDescriptorType.SampledImage;
                write.ImageInfo = new IntPtr(&imageInfo);

                GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
            }
            else
            {
                var buffer = shaderResourceView as Buffer;
                if (buffer != null)
                {
                    var bufferViewCopy = buffer.NativeBufferView;

                    write.VkDescriptorType = VkDescriptorType.UniformTexelBuffer;
                    write.TexelBufferView = new IntPtr(&bufferViewCopy);

                    GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
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
            var imageInfo = new DescriptorImageInfo { Sampler = samplerState.NativeSampler };

            var write = new WriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DestinationSet = NativeDescriptorSet,
                DestinationBinding = (uint)slot,
                DestinationArrayElement = 0,
                VkDescriptorType = VkDescriptorType.Sampler,
                ImageInfo = new IntPtr(&imageInfo),
            };

            GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
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
            var bufferInfo = new DescriptorBufferInfo { Buffer = buffer.NativeBuffer, Offset = (ulong)offset, Range = (ulong)size };

            var write = new WriteDescriptorSet
            {
                sType = VkStructureType.WriteDescriptorSet,
                DescriptorCount = 1,
                DestinationSet = NativeDescriptorSet,
                DestinationBinding = (uint)slot,
                DestinationArrayElement = 0,
                VkDescriptorType = VkDescriptorType.UniformBuffer,
                BufferInfo = new IntPtr(&bufferInfo)
            };
            
            GraphicsDevice.NativeDevice.UpdateDescriptorSets(1, &write, 0, null);
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
