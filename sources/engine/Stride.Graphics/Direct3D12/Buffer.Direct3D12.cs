// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;

using Silk.NET.Core.Native;
using Silk.NET.Direct3D12;
using Silk.NET.DXGI;

using D3D12Range = Silk.NET.Direct3D12.Range;

using Stride.Core;
using Stride.Core.Mathematics;

using static System.Runtime.CompilerServices.Unsafe;

namespace Stride.Graphics
{
    public unsafe partial class Buffer
    {
        // Internal Direct3D 12 Resource description
        private ResourceDesc nativeDescription;

        internal ulong GPUVirtualAddress;


        /// <summary>
        ///   Initializes this <see cref="Buffer"/> instance with the provided options.
        /// </summary>
        /// <param name="description">A <see cref="BufferDescription"/> structure describing the buffer characteristics.</param>
        /// <param name="viewFlags">A combination of flags determining how the Views over this buffer should behave.</param>
        /// <param name="viewFormat">
        ///   View format used if the buffer is used as a Shader Resource View,
        ///   or <see cref="PixelFormat.None"/> if not.
        /// </param>
        /// <param name="dataPointer">The data pointer to the data to initialize the buffer with.</param>
        /// <returns>This same instance of <see cref="Buffer"/> already initialized.</returns>
        /// <exception cref="ArgumentException">
        ///   The Buffer is a Structured Buffer, but <c>StructureByteStride</c> is less than or equal to 0.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   A Buffer with <see cref="GraphicsResourceUsage.Staging"/> cannot be created with initial data
        ///   (<paramref name="dataPointer"/> is not <see cref="IntPtr.Zero"/>).
        /// </exception>
        protected partial Buffer InitializeFromImpl(ref readonly BufferDescription description, BufferFlags bufferFlags, PixelFormat viewFormat, IntPtr dataPointer)
        {
            bufferDescription = description;
            nativeDescription = ConvertToNativeDescription(in description);

            ViewFlags = bufferFlags;
            InitCountAndViewFormat(out elementCount, ref viewFormat);
            ViewFormat = viewFormat;

            Recreate(dataPointer);

            GraphicsDevice?.RegisterBufferMemoryUsage(SizeInBytes);

            return this;

            /// <summary>
            ///   Returns a Direct3D 12 Resource Description for the Buffer.
            /// </summary>
            static ResourceDesc ConvertToNativeDescription(ref readonly BufferDescription bufferDescription)
            {
                var flags = ResourceFlags.None;
                var size = bufferDescription.SizeInBytes;

                // TODO: D3D12: For now, ensure size is multiple of ConstantBufferDataPlacementAlignment (for cbuffer views)
                size = MathUtil.AlignUp(size, D3D12.DefaultResourcePlacementAlignment);

                if (bufferDescription.BufferFlags.HasFlag(BufferFlags.UnorderedAccess))
                    flags |= ResourceFlags.AllowUnorderedAccess;

                return new ResourceDesc
                {
                    Dimension = ResourceDimension.Buffer,
                    Width = (ulong) size,
                    Height = 1,
                    DepthOrArraySize = 1,
                    MipLevels = 1,
                    Alignment = D3D12.DefaultResourcePlacementAlignment,

                    SampleDesc = { Count = 1, Quality = 0 },

                    Format = Format.FormatUnknown,
                    Layout = TextureLayout.LayoutRowMajor,
                    Flags = flags
                };
            }

            /// <summary>
            ///   Determines the number of elements and the element format depending on the type of buffer and intended view format.
            /// </summary>
            void InitCountAndViewFormat(out int count, ref PixelFormat viewFormat)
            {
                if (Description.StructureByteStride == 0)
                {
                    // TODO: The way to calculate the count is not always correct depending on the ViewFlags...etc.
                    count = ViewFlags.HasFlag(BufferFlags.RawBuffer) ? Description.SizeInBytes / sizeof(int) :
                            ViewFlags.HasFlag(BufferFlags.ShaderResource) ? Description.SizeInBytes / viewFormat.SizeInBytes :
                            0;
                }
                else
                {
                    // Structured Buffer
                    count = Description.SizeInBytes / Description.StructureByteStride;
                    viewFormat = PixelFormat.None;
                }
            }
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            GraphicsDevice?.RegisterBufferMemoryUsage(-SizeInBytes);

            base.OnDestroyed(immediately);
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();

            if (Description.Usage is GraphicsResourceUsage.Immutable or GraphicsResourceUsage.Default)
                return false;

            Recreate(dataPointer: IntPtr.Zero);

            return true;
        }

        /// <summary>
        ///   Recreates this buffer explicitly with the provided data. Usually called after the <see cref="GraphicsDevice"/> has been reset.
        /// </summary>
        /// <param name="dataPointer">
        ///   The data pointer to the data to use to recreate the buffer with.
        ///   Specify <see cref="IntPtr.Zero"/> if no initial data is needed.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   The Buffer is a Structured Buffer, but <c>StructureByteStride</c> is less than or equal to 0.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   A Buffer with <see cref="GraphicsResourceUsage.Staging"/> cannot be created with initial data
        ///   (<paramref name="dataPointer"/> is not <see cref="IntPtr.Zero"/>).
        /// </exception>
        public void Recreate(IntPtr dataPointer)
        {
            bool hasInitData = dataPointer != IntPtr.Zero;

            // TODO: D3D12: Where should that go longer term? Should it be precomputed for future use? (cost would likely be additional check on SetDescriptorSets/Draw)
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
                // Per our own definition of staging resource (read-back only)
                if (hasInitData)
                    throw new InvalidOperationException("D3D12: Staging buffers can't be created with initial data.");

                heapType = HeapType.Readback;
                NativeResourceState = ResourceStates.CopyDest;
            }
            else if (Usage == GraphicsResourceUsage.Dynamic)
            {
                heapType = HeapType.Upload;
                NativeResourceState = ResourceStates.GenericRead;
            }

            // TODO: D3D12: Move to a global allocator in bigger committed resources
            var heap = new HeapProperties { Type = heapType };

            var initialResourceState = heapType != HeapType.Default ? NativeResourceState : ResourceStates.Common;

            // If the resource must be initialized with data, it is initially in the state
            // CopyDest so we can copy from an upload buffer
            //if (hasInitData)
            //    initialResourceState = ResourceStates.CopyDest;

            HResult result = GraphicsDevice.NativeDevice.CreateCommittedResource(in heap, HeapFlags.None, in nativeDescription,
                                                                                 initialResourceState, pOptimizedClearValue: null,
                                                                                 out ComPtr<ID3D12Resource> buffer);
            if (result.IsFailure)
                result.Throw();

            SetNativeDeviceChild(buffer.AsDeviceChild());
            GPUVirtualAddress = NativeResource.GetGPUVirtualAddress();

            if (heapType == HeapType.Upload)
            {
                if (hasInitData)
                {
                    // An upload (dynamic) Buffer: We map and write the initial data, but leave the
                    // buffer in the same state so it can be mapped again anytime
                    void* uploadMemory = null;
                    result = NativeResource.Map(Subresource: 0, ref NullRef<D3D12Range>(), ref uploadMemory);

                    if (result.IsFailure)
                        result.Throw();

                    MemoryUtilities.CopyWithAlignmentFallback(uploadMemory, (void*) dataPointer, (uint) SizeInBytes);

                    NativeResource.Unmap(Subresource: 0, pWrittenRange: ref NullRef<D3D12Range>());
                }
            }
            else if (heapType == HeapType.Default)
            {
                ComPtr<ID3D12Resource> uploadResource = default;
                int uploadOffset = 0;

                if (hasInitData)
                {
                    // Copy data to the upload heap for later inter-resource copy
                    // TODO: D3D12: Move that to a shared upload heap
                    var uploadMemory = GraphicsDevice.AllocateUploadBuffer(SizeInBytes, out uploadResource, out uploadOffset);
                    MemoryUtilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) dataPointer, (uint) SizeInBytes);
                }

                var commandList = GraphicsDevice.NativeCopyCommandList;

                lock (GraphicsDevice.NativeCopyCommandListLock)
                {
                    scoped ref var nullPipelineState = ref NullRef<ID3D12PipelineState>();
                    result = commandList.Reset(GraphicsDevice.NativeCopyCommandAllocator, pInitialState: ref nullPipelineState);

                    if (result.IsFailure)
                        result.Throw();

                    var resourceBarrier = new ResourceBarrier { Type = ResourceBarrierType.Transition };
                    resourceBarrier.Transition.PResource = NativeResource;
                    resourceBarrier.Transition.Subresource = 0;

                    if (hasInitData)
                    {
                        // Switch resource to CopyDest state
                        resourceBarrier.Transition.StateBefore = initialResourceState;
                        resourceBarrier.Transition.StateAfter = ResourceStates.CopyDest;
                        commandList.ResourceBarrier(NumBarriers: 1, in resourceBarrier);

                        // Copy from the upload heap to the actual resource
                        commandList.CopyBufferRegion(NativeResource, DstOffset: 0, uploadResource, (ulong) uploadOffset, (ulong) SizeInBytes);
                    }

                    // Once initialized, transition the Buffer to its final state
                    resourceBarrier.Transition.StateBefore = hasInitData ? ResourceStates.CopyDest : initialResourceState;
                    resourceBarrier.Transition.StateAfter = NativeResourceState;

                    commandList.ResourceBarrier(NumBarriers: 1, in resourceBarrier);

                    result = commandList.Close();

                    if (result.IsFailure)
                        result.Throw();

                    var copyFenceValue = GraphicsDevice.ExecuteAndWaitCopyQueueGPU();

                    // Make sure any subsequent CPU access (i.e. MapSubresource) will wait for copy command list to be finished
                    CopyFenceValue = copyFenceValue;
                }
            }

            NativeShaderResourceView = GetShaderResourceView(ViewFormat);
            NativeUnorderedAccessView = GetUnorderedAccessView(ViewFormat);
        }

        /// <summary>
        ///   Gets a <see cref="CpuDescriptorHandle"/> for a Shader Resource View over this Buffer
        ///   for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="viewFormat">The view format.</param>
        /// <returns>A <see cref="CpuDescriptorHandle"/> for the Shader Resource View.</returns>
        /// <remarks>
        ///   The <see cref="Buffer"/> must have been declared with <see cref="BufferFlags.ShaderResource"/>.
        ///   The Shader Resource View is kept by this Buffer and will be disposed when this Buffer is disposed.
        /// </remarks>
        internal CpuDescriptorHandle GetShaderResourceView(PixelFormat viewFormat)
        {
            CpuDescriptorHandle srv = default;

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

                srv = GraphicsDevice.ShaderResourceViewAllocator.Allocate();
                NativeDevice.CreateShaderResourceView(NativeResource, in description, srv);
            }
            return srv;
        }

        /// <summary>
        ///   Gets a <see cref="CpuDescriptorHandle"/> for a Unordered Access View over this Buffer
        ///   for a particular <see cref="PixelFormat"/>.
        /// </summary>
        /// <param name="pixelFormat">The view format.</param>
        /// <returns>A <see cref="CpuDescriptorHandle"/> for the Unordered Access View.</returns>
        /// <remarks>
        ///   The <see cref="Buffer"/> must have been declared with <see cref="BufferFlags.UnorderedAccess"/>.
        ///   The Render Target View is kept by this Buffer and will be disposed when this Buffer is disposed.
        /// </remarks>
        internal CpuDescriptorHandle GetUnorderedAccessView(PixelFormat viewFormat)
        {
            CpuDescriptorHandle uav = default;

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

                // TODO: Manage counter value here if Buffer has 'Counter' or 'Append' flag
                // if (Flags == BufferFlags.StructuredAppendBuffer || Flags == BufferFlags.StructuredCounterBuffer))
                NativeDevice.CreateUnorderedAccessView(NativeResource, pCounterResource: null, in description, uav);
            }
            return uav;
        }}
    }

#endif
