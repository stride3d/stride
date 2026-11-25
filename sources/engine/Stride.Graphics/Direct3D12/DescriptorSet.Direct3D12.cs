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

        // Where the Descriptors that come from a Graphics Resource are recorded, to rewrite them after the resource is recreated
        private readonly DescriptorPool? pool;
        private readonly int srvIndex;

        // Mapping of binding slot -> offset from start handle
        /// <inheritdoc cref="DescriptorSetLayout.BindingOffsets"/>
        private readonly ReadOnlySpan<int> BindingOffsets => IsValid ? Description!.BindingOffsets : default;

        /// <summary>
        ///   The description of the Descriptors in the Descriptor Set, their layout, and other associated metadata.
        /// </summary>
        internal readonly DescriptorSetLayout? Description;

        /// <summary>
        ///   Gets a value indicating if the Descriptor Set is valid.
        /// </summary>
        /// <remarks>
        ///   A Descriptor Set is considered valid if it is allocated in a Descriptor Pool.
        /// </remarks>
        public readonly bool IsValid => Description is not null;

        /// <summary>
        ///   A CPU-accessible handle to the Descriptors for Shader Resource Views (SRVs), Unordered Access Views (UAVs),
        ///   and Constant Buffer Views (CBVs), in the Descriptor Set.
        /// </summary>
        /// <remarks>
        ///   The handle points to the first allocated Descriptor. If more than one Descriptor is allocated, they will
        ///   be contiguous to the first one.
        /// </remarks>
        internal readonly CpuDescriptorHandle SrvStart;
        /// <summary>
        ///   A CPU-accessible handle to the Descriptors for Samplers in the Descriptor Set.
        /// </summary>
        /// <remarks>
        ///   The handle points to the first allocated Descriptor. If more than one Descriptor is allocated, they will
        ///   be contiguous to the first one.
        /// </remarks>
        internal readonly CpuDescriptorHandle SamplerStart;


        /// <summary>
        ///   Initializes a new instance of the <see cref="DescriptorSet"/> structure.
        /// </summary>
        /// <param name="graphicsDevice">The Graphics Device.</param>
        /// <param name="pool">The pool where Descriptor Sets are allocated.</param>
        /// <param name="layout">A description of the Graphics Resources in the Descriptor Set and their layout.</param>
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
                pool = null;
                srvIndex = 0;
                Description = null;
                SrvStart = default;
                SamplerStart = default;
                return;
            }

            Debug.Assert(graphicsDevice is not null);

            nativeDevice = graphicsDevice.NativeDevice;
            Description = layout;
            this.pool = pool;
            srvIndex = pool.SrvOffset;

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

            // Initialize all sampler slots with LinearClamp as a default fallback.
            // Unlike D3D11, D3D12 has no default sampler — uninitialized sampler descriptors cause undefined behavior.
            if (layout.SamplerCount > 0)
            {
                var defaultSampler = graphicsDevice.SamplerStates.LinearClamp.NativeSampler;
                for (int i = 0; i < layout.SamplerCount; i++)
                {
                    var dest = new CpuDescriptorHandle(SamplerStart.Ptr + (nuint)(i * graphicsDevice.SamplerHandleIncrementSize));
                    nativeDevice.CopyDescriptorsSimple(1, dest, defaultSampler, DescriptorHeapType.Sampler);
                }
            }
        }


        /// <summary>
        ///   Sets the Descriptor in a specific slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="value">The Descriptor to set.</param>
        public void SetValue(int slot, object value)
        {
            if (value is GraphicsResource resource)
            {
                // A RWBuffer / RWTexture slot needs the UAV
                if (Description!.UnorderedAccessSlots[slot])
                    SetUnorderedAccessView(slot, resource);
                else
                    SetShaderResourceView(slot, resource);
            }
            else if (value is SamplerState sampler)
            {
                SetSamplerState(slot, sampler);
            }
        }

        /// <summary>
        ///   Sets a Shader Resource View (SRV) Descriptor in a specific slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="shaderResourceView">The Shader Resource View (SRV) to set.</param>
        public void SetShaderResourceView(int slot, GraphicsResource shaderResourceView)
        {
            var bindingOffset = BindingOffsets[slot];
            Record(bindingOffset, shaderResourceView, DescriptorSourceKind.ShaderResourceView, offset: 0, size: 0);

            WriteShaderResourceView(bindingOffset, shaderResourceView);
        }

        private void WriteShaderResourceView(int bindingOffset, GraphicsResource shaderResourceView)
        {
            if (shaderResourceView.NativeShaderResourceView.Ptr == 0)
                return;

            var destDescriptorRangeStart = new CpuDescriptorHandle(SrvStart.Ptr + (nuint) bindingOffset);

            nativeDevice.CopyDescriptorsSimple(NumDescriptors: 1, destDescriptorRangeStart,
                                               shaderResourceView.NativeShaderResourceView, DescriptorHeapType.CbvSrvUav);
        }

        /// <summary>
        ///   Sets a Sampler State Descriptor in a specific slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="samplerState">The Sampler State to set.</param>
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
        ///   Sets a Constant Buffer View (CBV) Descriptor in a specific slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="buffer">The Constant Buffer View to set.</param>
        /// <param name="offset">The offset from the start of the Constant Buffer to create the View with.</param>
        /// <param name="size">The size of the Constant Buffer View.</param>
        public void SetConstantBuffer(int slot, Buffer buffer, int offset, int size)
        {
            // TODO: We should validate whether offset + size fits inside the Constant Buffer memory

            var bindingOffset = BindingOffsets[slot];
            Record(bindingOffset, buffer, DescriptorSourceKind.ConstantBuffer, offset, size);

            WriteConstantBuffer(bindingOffset, buffer, offset, size);
        }

        private void WriteConstantBuffer(int bindingOffset, Buffer buffer, int offset, int size)
        {
            var cbufferViewDesc = new ConstantBufferViewDesc
            {
                BufferLocation = buffer.GPUVirtualAddress + (ulong) offset,

                // Constant Buffer size needs to be 256-byte aligned
                SizeInBytes = (uint) ((size + D3D12.ConstantBufferDataPlacementAlignment) / D3D12.ConstantBufferDataPlacementAlignment * D3D12.ConstantBufferDataPlacementAlignment)
            };

            var destDescriptorHandle = new CpuDescriptorHandle(SrvStart.Ptr + (nuint) bindingOffset);

            nativeDevice.CreateConstantBufferView(in cbufferViewDesc, destDescriptorHandle);

            // Constant buffers are typically on upload heaps (GenericRead) and don't need transitions
        }

        /// <summary>
        ///   Sets a Unordered Access View (UAV) Descriptor in a specific slot.
        /// </summary>
        /// <param name="slot">The slot.</param>
        /// <param name="buffer">The Unordered Access View to set.</param>
        /// <exception cref="ArgumentException">The Graphics Resource does not have an Unordered Access View.</exception>
        public void SetUnorderedAccessView(int slot, GraphicsResource unorderedAccessView)
        {
            var bindingOffset = BindingOffsets[slot];
            Record(bindingOffset, unorderedAccessView, DescriptorSourceKind.UnorderedAccessView, offset: 0, size: 0);

            WriteUnorderedAccessView(bindingOffset, unorderedAccessView);
        }

        private void WriteUnorderedAccessView(int bindingOffset, GraphicsResource unorderedAccessView)
        {
            // TODO: Why this throws, but the SRVs just return?

            if (unorderedAccessView.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException($"Resource '{unorderedAccessView}' has missing Unordered Access View.");

            var destDescriptorRangeStart = new CpuDescriptorHandle(SrvStart.Ptr + (nuint) bindingOffset);

            nativeDevice.CopyDescriptorsSimple(NumDescriptors: 1, destDescriptorRangeStart,
                                               unorderedAccessView.NativeUnorderedAccessView, DescriptorHeapType.CbvSrvUav);
        }

        private void Record(int bindingOffset, GraphicsResource resource, DescriptorSourceKind kind, int offset, int size)
        {
            if (pool?.SrvSources is not { } sources)
                return;

            var index = srvIndex + bindingOffset / resource.GraphicsDevice.SrvHandleIncrementSize;
            sources[index] = new DescriptorSource(resource, resource.RenameVersion, kind, offset, size);
        }

        /// <summary>
        ///   Rewrites the Descriptors whose Graphics Resource was recreated after the Descriptor was written.
        /// </summary>
        internal void RefreshStaleDescriptors()
        {
            if (pool?.SrvSources is not { } sources)
                return;

            var srvCount = Description!.SrvCount;
            for (var i = 0; i < srvCount; i++)
            {
                var source = sources[srvIndex + i];
                var resource = source.Resource;
                if (resource is null || source.Version == resource.RenameVersion)
                    continue;

                // Command lists on several threads can bind the same Descriptor Set
                lock (sources)
                {
                    source = sources[srvIndex + i];
                    var version = resource.RenameVersion;
                    if (source.Version == version)
                        continue;

                    RewriteDescriptor(i, source);
                    sources[srvIndex + i] = source with { Version = version };
                }
            }
        }

        private void RewriteDescriptor(int index, DescriptorSource source)
        {
            var resource = source.Resource;
            var bindingOffset = index * resource.GraphicsDevice.SrvHandleIncrementSize;
            switch (source.Kind)
            {
                case DescriptorSourceKind.ShaderResourceView:
                    WriteShaderResourceView(bindingOffset, resource);
                    break;

                case DescriptorSourceKind.UnorderedAccessView:
                    WriteUnorderedAccessView(bindingOffset, resource);
                    break;

                case DescriptorSourceKind.ConstantBuffer:
                    WriteConstantBuffer(bindingOffset, (Buffer) resource, source.Offset, source.Size);
                    break;
            }
        }

        /// <summary>
        ///   Determines whether a Descriptor of the Descriptor Set was written from the specified Graphics Resource.
        /// </summary>
        internal bool UsesResource(GraphicsResource resource)
        {
            if (pool?.SrvSources is not { } sources)
                return false;

            var srvCount = Description!.SrvCount;
            for (var i = 0; i < srvCount; i++)
            {
                if (sources[srvIndex + i].Resource == resource)
                    return true;
            }

            return false;
        }
    }

    internal enum DescriptorSourceKind : byte
    {
        ShaderResourceView,
        UnorderedAccessView,
        ConstantBuffer
    }

    /// <summary>
    ///   The Graphics Resource a Descriptor was written from, and the <see cref="GraphicsResource.RenameVersion"/> it had then.
    /// </summary>
    internal readonly record struct DescriptorSource(GraphicsResource Resource, int Version, DescriptorSourceKind Kind, int Offset, int Size);
}

#endif
