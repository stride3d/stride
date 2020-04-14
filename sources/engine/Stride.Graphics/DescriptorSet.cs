// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics
{
    /// <summary>
    /// Contains a list descriptors (such as textures) that can be bound together to the graphics pipeline.
    /// </summary>
    public partial struct DescriptorSet
    {
        public static DescriptorSet New(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
        {
            return new DescriptorSet(graphicsDevice, pool, desc);
        }

#if STRIDE_GRAPHICS_API_DIRECT3D11 || STRIDE_GRAPHICS_API_OPENGL || (STRIDE_GRAPHICS_API_VULKAN && STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES)
        internal readonly DescriptorSetEntry[] HeapObjects;
        internal readonly int DescriptorStartOffset;

        private DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
        {
            this.HeapObjects = pool.Entries;
            this.DescriptorStartOffset = pool.Allocate(desc.ElementCount);
        }

        public bool IsValid => DescriptorStartOffset != -1;

        /// <summary>
        /// Sets a descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="value">The descriptor.</param>
        public void SetValue(int slot, object value)
        {
            HeapObjects[DescriptorStartOffset + slot].Value = value;
        }

        /// <summary>
        /// Sets a shader resource view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        public void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            HeapObjects[DescriptorStartOffset + slot].Value = shaderResourceView;
        }

        /// <summary>
        /// Sets a sampler state descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The sampler state.</param>
        public void SetSamplerState(int slot, SamplerState samplerState)
        {
            HeapObjects[DescriptorStartOffset + slot].Value = samplerState;
        }

        /// <summary>
        /// Sets a constant buffer view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="buffer">The constant buffer.</param>
        /// <param name="offset">The constant buffer view start offset.</param>
        /// <param name="size">The constant buffer view size.</param>
        public void SetConstantBuffer(int slot, Buffer buffer, int offset, int size)
        {
            HeapObjects[DescriptorStartOffset + slot] = new DescriptorSetEntry(buffer, offset, size);
        }

        /// <summary>
        /// Sets an unordered access view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            ref var heapObject = ref HeapObjects[DescriptorStartOffset + slot];
            heapObject.Value = unorderedAccessView;
            heapObject.Offset = (unorderedAccessView as Buffer)?.InitialCounterOffset ?? -1;
        }
#endif
    }
}
