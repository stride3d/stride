// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Diagnostics;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;

namespace Stride.Graphics
{
    public readonly unsafe partial struct DescriptorSet
    {
        private readonly ComPtr<ID3D12Device> nativeDevice;

        // Mapping of binding slot -> offset from start handle
        private readonly ReadOnlySpan<int> BindingOffsets => IsValid ? Description!.BindingOffsets : default;

        internal readonly DescriptorSetLayout? Description;

        public readonly bool IsValid => Description is not null;

        internal readonly CpuDescriptorHandle SrvStart;
        internal readonly CpuDescriptorHandle SamplerStart;


        private DescriptorSet(GraphicsDevice graphicsDevice, DescriptorPool pool, DescriptorSetLayout layout)
        {
            Debug.Assert(pool is not null);
            Debug.Assert(layout is not null);

            // Not enough space in the pool for the new Descriptor Set?
            if (pool.SrvOffset + layout.SrvCount > pool.SrvCount ||
                pool.SamplerOffset + layout.SamplerCount > pool.SamplerCount)
            {
                // Early exit if OOM, IsValid should return false
                // TODO: different mechanism?
                nativeDevice = null;
                Description = null;
                SrvStart = default;
                SamplerStart = default;
                return;
            }

            Debug.Assert(graphicsDevice is not null);

            nativeDevice = graphicsDevice.NativeDevice;
            Description = layout;

            // Store starting CpuDescriptorHandle for SRVs, UAVs, etc.
            var startHandle = layout.SrvCount > 0
                ? pool.SrvStart.Ptr + (nuint) (graphicsDevice.SrvHandleIncrementSize * pool.SrvOffset)
                : 0;

            SrvStart = new CpuDescriptorHandle(startHandle);

            // Store starting CpuDescriptorHandle for Samplers
            startHandle = layout.SamplerCount > 0
                ? pool.SamplerStart.Ptr + (nuint) (graphicsDevice.SamplerHandleIncrementSize * pool.SamplerOffset)
                : 0;

            SamplerStart = new CpuDescriptorHandle(startHandle);

            // Allocation is done, bump offsets
            // TODO: D3D12: Thread safety?
            pool.SrvOffset += layout.SrvCount;
            pool.SamplerOffset += layout.SamplerCount;
        }


        /// <summary>
        /// Sets a descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="value">The descriptor.</param>
        public void SetValue(int slot, object value)
        {
            if (value is GraphicsResource srv)
            {
                SetShaderResourceView(slot, srv);
            }
            else if (value is SamplerState sampler)
            {
                SetSamplerState(slot, sampler);
            }
        }

        /// <summary>
        /// Sets a shader resource view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        public void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            if (shaderResourceView.NativeShaderResourceView.Ptr == 0)
                return;

            var destDescriptorRangeStart = new CpuDescriptorHandle(SrvStart.Ptr + (nuint) BindingOffsets[slot]);

            nativeDevice.CopyDescriptorsSimple(NumDescriptors: 1, destDescriptorRangeStart,
                                               shaderResourceView.NativeShaderResourceView, DescriptorHeapType.CbvSrvUav);
        }

        /// <summary>
        /// Sets a sampler state descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The sampler state.</param>
        public void SetSamplerState(int slot, SamplerState samplerState)
        {
            // For now, immutable Samplers appears in the Descriptor Set and should be ignored
            // TODO: GRAPHICS REFACTOR: Can't we just hide them somehow?
            var bindingSlot = BindingOffsets[slot];
            if (bindingSlot == DescriptorSetLayout.IMMUTABLE_SAMPLER_BINDING_OFFSET)
                return;

            var destDescriptorRangeStart = new CpuDescriptorHandle(SamplerStart.Ptr + (nuint) bindingSlot);

            nativeDevice.CopyDescriptorsSimple(NumDescriptors: 1, destDescriptorRangeStart,
                                               samplerState.NativeSampler, DescriptorHeapType.Sampler);
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
            // TODO: We should validate whether offset + size fits inside the Constant Buffer memory

            var cbufferViewDesc = new ConstantBufferViewDesc
            {
                BufferLocation = buffer.GPUVirtualAddress + (ulong) offset,

                // Constant Buffer size needs to be 256-byte aligned
                SizeInBytes = (uint) ((size + D3D12.ConstantBufferDataPlacementAlignment) / D3D12.ConstantBufferDataPlacementAlignment * D3D12.ConstantBufferDataPlacementAlignment)
            };

            var destDescriptorHandle = new CpuDescriptorHandle(SrvStart.Ptr + (nuint) BindingOffsets[slot]);

            nativeDevice.CreateConstantBufferView(in cbufferViewDesc, destDescriptorHandle);
        }

        /// <summary>
        /// Sets an unordered access view descriptor.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            // TODO: Why this throws, but the SRVs just return?

            if (unorderedAccessView.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException($"Resource '{unorderedAccessView}' has missing Unordered Access View.");

            var destDescriptorRangeStart = new CpuDescriptorHandle(SrvStart.Ptr + (nuint) BindingOffsets[slot]);

            nativeDevice.CopyDescriptorsSimple(NumDescriptors: 1, destDescriptorRangeStart,
                                               unorderedAccessView.NativeUnorderedAccessView, DescriptorHeapType.CbvSrvUav);
        }
    }
}

#endif
