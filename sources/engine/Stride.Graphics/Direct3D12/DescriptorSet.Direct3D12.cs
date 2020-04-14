// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D12
using System;
using SharpDX;
using SharpDX.Direct3D12;
using Stride.Shaders;

namespace Stride.Graphics
{
    public partial struct DescriptorSet
    {
        internal readonly GraphicsDevice Device;
        internal readonly int[] BindingOffsets;
        internal readonly DescriptorSetLayout Description;

        internal readonly CpuDescriptorHandle SrvStart;
        internal readonly CpuDescriptorHandle SamplerStart;

        public bool IsValid => Description != null;

        private DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout desc)
        {
            if (pool.SrvOffset + desc.SrvCount > pool.SrvCount || pool.SamplerOffset + desc.SamplerCount > pool.SamplerCount)
            {
                // Eearly exit if OOM, IsValid should return false (TODO: different mechanism?)
                Device = null;
                BindingOffsets = null;
                Description = null;
                SrvStart = new CpuDescriptorHandle();
                SamplerStart = new CpuDescriptorHandle();
                return;
            }

            Device = graphicsDevice;
            BindingOffsets = desc.BindingOffsets;
            Description = desc;

            // Store start CpuDescriptorHandle
            SrvStart = desc.SrvCount > 0 ? (pool.SrvStart + graphicsDevice.SrvHandleIncrementSize * pool.SrvOffset) : new CpuDescriptorHandle();
            SamplerStart = desc.SamplerCount > 0 ? (pool.SamplerStart + graphicsDevice.SamplerHandleIncrementSize * pool.SamplerOffset) : new CpuDescriptorHandle();

            // Allocation is done, bump offsets
            // TODO D3D12 thread safety?
            pool.SrvOffset += desc.SrvCount;
            pool.SamplerOffset += desc.SamplerCount;
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
        public void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            if (shaderResourceView.NativeShaderResourceView.Ptr == PointerSize.Zero)
                return;
            Device.NativeDevice.CopyDescriptorsSimple(1, SrvStart + BindingOffsets[slot], shaderResourceView.NativeShaderResourceView, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        }

        /// <summary>
        /// Sets a sampler state descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The sampler state.</param>
        public void SetSamplerState(int slot, SamplerState samplerState)
        {
            // For now, immutable samplers appears in the descriptor set and should be ignored
            // TODO GRAPHICS REFACTOR can't we just hide them somehow?
            var bindingSlot = BindingOffsets[slot];
            if (bindingSlot == -1)
                return;

            Device.NativeDevice.CopyDescriptorsSimple(1, SamplerStart + BindingOffsets[slot], samplerState.NativeSampler, DescriptorHeapType.Sampler);
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
            var constantBufferDataPlacementAlignment = Device.ConstantBufferDataPlacementAlignment;
            Device.NativeDevice.CreateConstantBufferView(new ConstantBufferViewDescription
            {
                BufferLocation = buffer.GPUVirtualAddress + offset,
                SizeInBytes = (size + constantBufferDataPlacementAlignment) / constantBufferDataPlacementAlignment * constantBufferDataPlacementAlignment, // CB size needs to be 256-byte aligned
            }, SrvStart + BindingOffsets[slot]);
        }

        /// <summary>
        /// Sets an unordered access view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            if (unorderedAccessView.NativeUnorderedAccessView.Ptr == PointerSize.Zero)
                throw new ArgumentException($"Resource \'{unorderedAccessView}\' has missing Unordered Access View.");
            Device.NativeDevice.CopyDescriptorsSimple(1, SrvStart + BindingOffsets[slot], unorderedAccessView.NativeUnorderedAccessView, DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        }
    }
}
#endif
