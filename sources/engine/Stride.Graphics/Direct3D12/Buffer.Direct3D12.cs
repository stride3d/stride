// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public unsafe partial class Buffer
    {
        private ResourceDesc nativeDescription;
        internal ulong GPUVirtualAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="Buffer" /> class.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <param name="viewFlags">Type of the buffer.</param>
        /// <param name="viewFormat">The view format.</param>
        /// <param name="dataPointer">The data pointer.</param>
        protected Buffer InitializeFromImpl(BufferDescription description, BufferFlags viewFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            nativeDescription = ConvertToNativeDescription(GraphicsDevice, Description);

            ViewFlags = viewFlags;
            InitCountAndViewFormat(out elementCount, ref viewFormat);
            ViewFormat = viewFormat;

            Recreate(dataPointer);

            GraphicsDevice?.RegisterBufferMemoryUsage(SizeInBytes);

            return this;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            GraphicsDevice?.RegisterBufferMemoryUsage(-SizeInBytes);

            base.OnDestroyed();
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage is GraphicsResourceUsage.Immutable or GraphicsResourceUsage.Default)
                return false;

            Recreate(IntPtr.Zero);

            return true;
        }

        /// <summary>
        /// Explicitly recreate buffer with given data. Usually called after a <see cref="GraphicsDevice"/> reset.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dataPointer"></param>
        public void Recreate(IntPtr dataPointer)
        {
            // TODO D3D12 where should that go longer term? should it be precomputed for future use? (cost would likely be additional check on SetDescriptorSets/Draw)
            NativeResourceState = ResourceStates.Common;
            var bufferFlags = bufferDescription.BufferFlags;

            if (bufferFlags.HasFlag(BufferFlags.ConstantBuffer))
                NativeResourceState |= ResourceStates.VertexAndConstantBuffer;

            if (bufferFlags.HasFlag(BufferFlags.IndexBuffer))
                NativeResourceState |= ResourceStates.IndexBuffer;

            if (bufferFlags.HasFlag(BufferFlags.VertexBuffer))
                NativeResourceState |= ResourceStates.VertexAndConstantBuffer;

            if (bufferFlags.HasFlag(BufferFlags.ShaderResource))
                NativeResourceState |= ResourceStates.PixelShaderResource | ResourceStates.NonPixelShaderResource;

            if (bufferFlags.HasFlag(BufferFlags.StructuredBuffer))
            {
                if (bufferDescription.StructureByteStride <= 0)
                    throw new ArgumentException("Element size cannot be less or equal 0 for structured buffer");
            }

            if (bufferFlags.HasFlag(BufferFlags.ArgumentBuffer))
                NativeResourceState |= ResourceStates.IndirectArgument;

            var heapType = HeapType.Default;
            if (Usage == GraphicsResourceUsage.Staging)
            {
                if (dataPointer != IntPtr.Zero)
                    throw new NotImplementedException("D3D12: Staging buffers can't be created with initial data.");

                heapType = HeapType.Readback;
                NativeResourceState = ResourceStates.CopyDest;
            }
            else if (Usage == GraphicsResourceUsage.Dynamic)
            {
                heapType = HeapType.Upload;
                NativeResourceState = ResourceStates.GenericRead;
            }

            // TODO D3D12 move that to a global allocator in bigger committed resources
            ID3D12Resource* resource;
            var heap = new HeapProperties { Type = heapType };
            var initialState = dataPointer != IntPtr.Zero ? ResourceStates.CopyDest : NativeResourceState;

            HResult result = GraphicsDevice.NativeDevice->CreateCommittedResource(in heap, HeapFlags.None, nativeDescription, initialState, pOptimizedClearValue: null, SilkMarshal.GuidPtrOf<ID3D12Resource>(), (void**) &resource);

            if (result.IsFailure)
                result.Throw();

            NativeDeviceChild = (ID3D12DeviceChild*) resource;
            GPUVirtualAddress = NativeResource->GetGPUVirtualAddress();

            if (dataPointer != IntPtr.Zero)
            {
                if (heapType == HeapType.Upload)
                {
                    void* uploadMemory;
                    result = NativeResource->Map(Subresource: 0, pReadRange: null, &uploadMemory);

                    if (result.IsFailure)
                        result.Throw();

					Core.Utilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) dataPointer, (uint) SizeInBytes);

                    NativeResource->Unmap(Subresource: 0, pWrittenRange: null);
                }
                else
                {
                    // Copy data in upload heap for later copy
                    // TODO D3D12 move that to a shared upload heap
                    var uploadMemory = GraphicsDevice.AllocateUploadBuffer(SizeInBytes, out var uploadResource, out var uploadOffset);

					Core.Utilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) dataPointer, (uint) SizeInBytes);

                    // TODO D3D12 lock NativeCopyCommandList usages
                    var commandList = GraphicsDevice.NativeCopyCommandList;
                    result = commandList->Reset(GraphicsDevice.NativeCopyCommandAllocator, pInitialState: null);

                    if (result.IsFailure)
                        result.Throw();

                    // Copy from upload heap to actual resource
                    commandList->CopyBufferRegion(NativeResource, DstOffset: 0, uploadResource, (ulong) uploadOffset, (ulong) SizeInBytes);

                    // Switch resource to proper read state
                    var resourceBarrier = new ResourceBarrier { Type = ResourceBarrierType.Transition };
                    resourceBarrier.Transition.PResource = NativeResource;
                    resourceBarrier.Transition.Subresource = 0;
                    resourceBarrier.Transition.StateBefore = ResourceStates.CopyDest;
                    resourceBarrier.Transition.StateAfter = NativeResourceState;

                    commandList->ResourceBarrier(NumBarriers: 1, in resourceBarrier);

                    result = commandList->Close();

                    if (result.IsFailure)
                        result.Throw();

                    GraphicsDevice.WaitCopyQueue();
                }
            }

            NativeShaderResourceView = GetShaderResourceView(ViewFormat);
            NativeUnorderedAccessView = GetUnorderedAccessView(ViewFormat);
        }

        /// <summary>
        /// Gets a <see cref="ShaderResourceView"/> for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="viewFormat">The view format.</param>
        /// <returns>A <see cref="ShaderResourceView"/> for the particular view format.</returns>
        /// <remarks>
        /// The buffer must have been declared with <see cref="Graphics.BufferFlags.ShaderResource"/>.
        /// The ShaderResourceView instance is kept by this buffer and will be disposed when this buffer is disposed.
        /// </remarks>
        internal CpuDescriptorHandle GetShaderResourceView(PixelFormat viewFormat)
        {
            var srv = new CpuDescriptorHandle();

            if (ViewFlags.HasFlag(BufferFlags.ShaderResource))
            {
                var description = new ShaderResourceViewDesc
                {
                    Shader4ComponentMapping = 0x00001688,
                    Format = (Format) viewFormat,
                    ViewDimension = SrvDimension.Buffer,
                    Buffer = new()
                    {
                        NumElements = (uint) ElementCount,
                        FirstElement = 0,
                        Flags = BufferSrvFlags.None,
                        StructureByteStride = (uint) StructureByteStride
                    }
                };

                if (ViewFlags.HasFlag(BufferFlags.RawBuffer))
                    description.Buffer.Flags |= BufferSrvFlags.Raw;

                srv = GraphicsDevice.ShaderResourceViewAllocator.Allocate(1);
                NativeDevice->CreateShaderResourceView(NativeResource, in description, srv);
            }
            return srv;
        }

        internal CpuDescriptorHandle GetUnorderedAccessView(PixelFormat viewFormat)
        {
            var uav = new CpuDescriptorHandle();

            if (ViewFlags.HasFlag(BufferFlags.UnorderedAccess))
            {
                var description = new UnorderedAccessViewDesc
                {
                    Format = (Format) viewFormat,
                    ViewDimension = UavDimension.Buffer,
                    Buffer = new()
                    {
                        NumElements = (uint) ElementCount,
                        FirstElement = 0,
                        Flags = BufferUavFlags.None,
                        StructureByteStride = (uint) StructureByteStride,
                        CounterOffsetInBytes = 0
                    }
                };

                if (ViewFlags.HasFlag(BufferFlags.RawBuffer))
                {
                    description.Buffer.Flags |= BufferUavFlags.Raw;
                    description.Format = Format.FormatR32Typeless;
                }

                uav = GraphicsDevice.UnorderedAccessViewAllocator.Allocate(1);

                // TODO: manage counter value here if buffer has 'Counter' or 'Append' flag
                // if (Flags == BufferFlags.StructuredAppendBuffer || Flags == BufferFlags.StructuredCounterBuffer))
                NativeDevice->CreateUnorderedAccessView(NativeResource, pCounterResource: null, in description, uav);
            }
            return uav;
        }

        private void InitCountAndViewFormat(out int count, ref PixelFormat viewFormat)
        {
            if (Description.StructureByteStride == 0)
            {
                // TODO: The way to calculate the count is not always correct depending on the ViewFlags...etc.
                if (ViewFlags.HasFlag(BufferFlags.RawBuffer))
                {
                    count = Description.SizeInBytes / sizeof(int);
                }
                else if (ViewFlags.HasFlag(BufferFlags.ShaderResource))
                {
                    count = Description.SizeInBytes / viewFormat.SizeInBytes();
                }
                else
                {
                    count = 0;
                }
            }
            else
            {
                // For structured buffer
                count = Description.SizeInBytes / Description.StructureByteStride;
                viewFormat = PixelFormat.None;
            }
        }

        private static ResourceDesc ConvertToNativeDescription(GraphicsDevice graphicsDevice, BufferDescription bufferDescription)
        {
            var flags = ResourceFlags.None;
            var size = bufferDescription.SizeInBytes;

            // TODO D3D12 for now, ensure size is multiple of ConstantBufferDataPlacementAlignment (for cbuffer views)
            size = MathUtil.AlignUp(size, D3D12.ConstantBufferDataPlacementAlignment);

            if (bufferDescription.BufferFlags.HasFlag(BufferFlags.UnorderedAccess))
                flags |= ResourceFlags.AllowUnorderedAccess;

            return new ResourceDesc
            {
                Dimension = ResourceDimension.Buffer,
                Width = (ulong) size,
                Height = 1,
                DepthOrArraySize = 1,
                MipLevels = 1,
                Alignment = 0,

                SampleDesc = { Count = 1, Quality = 0 },

                Format = Format.FormatUnknown,
                Layout = TextureLayout.LayoutRowMajor,
                Flags = flags
            };
        }
    }
}

#endif
