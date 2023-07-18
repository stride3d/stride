// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core.Mathematics;
using Stride.Shaders;

namespace Stride.Graphics
{
    public unsafe partial class CommandList
    {
        private const int ConstantBufferCount = D3D11.CommonshaderConstantBufferApiSlotCount; // 14 actually
        private const int SamplerStateCount = D3D11.CommonshaderSamplerSlotCount;
        private const int ShaderResourceViewCount = D3D11.CommonshaderInputResourceSlotCount;
        private const int SimultaneousRenderTargetCount = D3D11.SimultaneousRenderTargetCount;
        private const int StageCount = 6;
        private const int UnorderedAcccesViewCount = D3D11.D3D111UavSlotCount;

        private ID3D11DeviceContext* nativeDeviceContext;
        private ID3D11DeviceContext1* nativeDeviceContext1;
        private ID3DUserDefinedAnnotation* nativeDeviceProfiler;

        private readonly ID3D11RenderTargetView*[] currentRenderTargetViews = new ID3D11RenderTargetView*[SimultaneousRenderTargetCount];
        private int currentRenderTargetViewsActiveCount = 0;

        private readonly ID3D11UnorderedAccessView*[] currentUARenderTargetViews = new ID3D11UnorderedAccessView*[SimultaneousRenderTargetCount];
        private readonly ID3D11UnorderedAccessView*[] unorderedAccessViews = new ID3D11UnorderedAccessView*[UnorderedAcccesViewCount]; // Only CS

        private readonly Buffer[] constantBuffers = new Buffer[StageCount * ConstantBufferCount];
        private readonly SamplerState[] samplerStates = new SamplerState[StageCount * SamplerStateCount];

        private PipelineState currentPipelineState;

        public static CommandList New(GraphicsDevice device)
        {
            throw new InvalidOperationException("Can't create multiple command lists with D3D11");
        }

        internal CommandList(GraphicsDevice device) : base(device)
        {
            nativeDeviceContext = device.NativeDeviceContext;
            NativeDeviceChild = (ID3D11DeviceChild*) nativeDeviceContext;

            ID3D11DeviceContext1 nativeDeviceContext1;
            HResult result = nativeDeviceContext->QueryInterface(SilkMarshal.GuidPtrOf<ID3D11DeviceContext1>(), (void**) &nativeDeviceContext1);

            if (result.IsFailure)
                result.Throw();

            ID3DUserDefinedAnnotation* deviceProfiler = null;
            if (device.IsDebugMode)
            {
                result = nativeDeviceContext->QueryInterface(SilkMarshal.GuidPtrOf<ID3DUserDefinedAnnotation>(), (void**) &deviceProfiler);
                if (result.IsFailure)
                    deviceProfiler = null;
            }
            nativeDeviceProfiler = deviceProfiler;

            ClearState();
        }

        /// <summary>
        ///   Gets the native Direct3D 11 device context.
        /// </summary>
        internal ID3D11DeviceContext* NativeDeviceContext => nativeDeviceContext;

        internal ID3D11DeviceContext* ShaderStages => nativeDeviceContext;

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            if (nativeDeviceProfiler != null)
                nativeDeviceProfiler->Release();
            nativeDeviceProfiler = null;

            base.OnDestroyed();
        }

        public void Reset()
        {
        }

        public void Flush()
        {
        }

        public CompiledCommandList Close()
        {
            return default;
        }

        private void ClearStateImpl()
        {
            if (nativeDeviceContext != null)
                nativeDeviceContext->ClearState();

            samplerStates.AsSpan().Clear();
            constantBuffers.AsSpan().Clear();

            Array.Clear(unorderedAccessViews);
            Array.Clear(currentRenderTargetViews);
            Array.Clear(currentUARenderTargetViews);

            // Since nothing can be drawn in default state, no need to set anything (another SetPipelineState should happen before)
            currentPipelineState = GraphicsDevice.DefaultPipelineState;
        }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl()
        {
            Array.Clear(currentRenderTargetViews);
            Array.Clear(currentUARenderTargetViews);

            // Reset all targets
            nativeDeviceContext->OMSetRenderTargets(NumViews: 0, ppRenderTargetViews: null, pDepthStencilView: null);
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="renderTargetCount">The number of render targets.</param>
        /// <param name="renderTargets">The render targets.</param>
        /// <exception cref="ArgumentNullException">renderTargetViews</exception>
        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, Texture[] renderTargets)
        {
            currentRenderTargetViewsActiveCount = renderTargetCount;

            for (int i = 0; i < renderTargetCount; i++)
                currentRenderTargetViews[i] = renderTargets[i].NativeRenderTargetView;

            nativeDeviceContext->OMSetRenderTargets(NumViews: (uint) renderTargetCount,
                                                    ppRenderTargetViews: in currentRenderTargetViews[0],
                                                    pDepthStencilView: depthStencilBuffer != null ? depthStencilBuffer.NativeDepthStencilView : null);
        }

        unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle)
        {
            var scissorBox = Unsafe.As<Rectangle, Silk.NET.Maths.Box2D<int>>(ref scissorRectangle);

            nativeDeviceContext->RSSetScissorRects(1, scissorBox);
        }

        unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles)
        {
            ArgumentNullException.ThrowIfNull(scissorRectangles);

            var scissorBoxes = MemoryMarshal.Cast<Rectangle, Silk.NET.Maths.Box2D<int>>(scissorRectangles);

            nativeDeviceContext->RSSetScissorRects((uint) scissorCount, in scissorBoxes[0]);
        }

        /// <summary>
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
            var numBuffers = buffers?.Length ?? 0;

            if (numBuffers > 0)
            {
                var streamOutputBuffers = stackalloc nint[numBuffers];
                var streamOutputOffsets = stackalloc uint[numBuffers];

                for (int i = 0; i < numBuffers; ++i)
                {
                    streamOutputBuffers[i] = (nint) buffers[i].NativeBuffer;
                    streamOutputOffsets[i] = 0;
                }
                nativeDeviceContext->SOSetTargets((uint) numBuffers,
                                                  (ID3D11Buffer**) streamOutputBuffers,
                                                  streamOutputOffsets);
            }
            else
            {
                nativeDeviceContext->SOSetTargets(0, null, null);
            }
        }

        /// <summary>
        ///     Gets or sets the 1st viewport. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <value>The viewport.</value>
        private unsafe void SetViewportImpl()
        {
            if (!viewportDirty)
                return;

            viewportDirty = false;

            uint viewportCount = renderTargetCount > 0 ? (uint) renderTargetCount : 1;
            ref var viewportsToSet = ref Unsafe.As<Viewport, Silk.NET.Direct3D11.Viewport>(ref viewports[0]);

            nativeDeviceContext->RSSetViewports(viewportCount, in viewportsToSet);
        }

        /// <summary>
        /// Unsets the render targets.
        /// </summary>
        public void UnsetRenderTargets()
        {
            nativeDeviceContext->OMSetRenderTargets(0, null, null);
        }

        /// <summary>
        ///     Sets a constant buffer to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="buffer">The constant buffer to set.</param>
        internal void SetConstantBuffer(ShaderStage stage, int slot, Buffer buffer)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            int stageIndex = (int) stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;

            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;

                var nativeBuffer = buffer != null ? buffer.NativeBuffer : null;

                switch (stage)
                {
                    case ShaderStage.Vertex:
                        nativeDeviceContext->VSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Hull:
                        nativeDeviceContext->HSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Domain:
                        nativeDeviceContext->DSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Geometry:
                        nativeDeviceContext->GSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Pixel:
                        nativeDeviceContext->PSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Compute:
                        nativeDeviceContext->CSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                }
            }
        }

        /// <summary>
        ///     Sets a constant buffer to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="buffer">The constant buffer to set.</param>
        internal void SetConstantBuffer(ShaderStage stage, int slot, int offset, Buffer buffer)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            int stageIndex = (int) stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;

            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;

                var nativeBuffer = buffer != null ? buffer.NativeBuffer : null;

                switch (stage)
                {
                    case ShaderStage.Vertex:
                        nativeDeviceContext->VSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Hull:
                        nativeDeviceContext->HSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Domain:
                        nativeDeviceContext->DSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Geometry:
                        nativeDeviceContext->GSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Pixel:
                        nativeDeviceContext->PSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                    case ShaderStage.Compute:
                        nativeDeviceContext->CSSetConstantBuffers((uint) slot, NumBuffers: 1, in nativeBuffer);
                        break;
                }
            }
        }

        /// <summary>
        ///     Sets a sampler state to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="samplerState">The sampler state to set.</param>
        internal void SetSamplerState(ShaderStage stage, int slot, SamplerState samplerState)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            int stageIndex = (int) stage - 1;

            int slotIndex = stageIndex * SamplerStateCount + slot;

            if (samplerStates[slotIndex] != samplerState)
            {
                samplerStates[slotIndex] = samplerState;

                var nativeSampler = samplerState != null ? samplerState.NativeSamplerState : null;

                switch (stage)
                {
                    case ShaderStage.Vertex:
                        nativeDeviceContext->VSSetSamplers((uint) slot, NumSamplers: 1, in nativeSampler);
                        break;
                    case ShaderStage.Hull:
                        nativeDeviceContext->HSSetSamplers((uint) slot, NumSamplers: 1, in nativeSampler);
                        break;
                    case ShaderStage.Domain:
                        nativeDeviceContext->DSSetSamplers((uint) slot, NumSamplers: 1, in nativeSampler);
                        break;
                    case ShaderStage.Geometry:
                        nativeDeviceContext->GSSetSamplers((uint) slot, NumSamplers: 1, in nativeSampler);
                        break;
                    case ShaderStage.Pixel:
                        nativeDeviceContext->PSSetSamplers((uint) slot, NumSamplers: 1, in nativeSampler);
                        break;
                    case ShaderStage.Compute:
                        nativeDeviceContext->CSSetSamplers((uint) slot, NumSamplers: 1, in nativeSampler);
                        break;
                }
            }
        }

        /// <summary>
        ///     Sets a shader resource view to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        internal void SetShaderResourceView(ShaderStage stage, int slot, GraphicsResource shaderResourceView)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            var nativeShaderResourceView = shaderResourceView != null ? shaderResourceView.NativeShaderResourceView : null;

            switch (stage)
            {
                case ShaderStage.Vertex:
                    nativeDeviceContext->VSSetShaderResources((uint) slot, NumViews: 1, in nativeShaderResourceView);
                    break;
                case ShaderStage.Hull:
                    nativeDeviceContext->HSSetShaderResources((uint) slot, NumViews: 1, in nativeShaderResourceView);
                    break;
                case ShaderStage.Domain:
                    nativeDeviceContext->DSSetShaderResources((uint) slot, NumViews: 1, in nativeShaderResourceView);
                    break;
                case ShaderStage.Geometry:
                    nativeDeviceContext->GSSetShaderResources((uint) slot, NumViews: 1, in nativeShaderResourceView);
                    break;
                case ShaderStage.Pixel:
                    nativeDeviceContext->PSSetShaderResources((uint) slot, NumViews: 1, in nativeShaderResourceView);
                    break;
                case ShaderStage.Compute:
                    nativeDeviceContext->CSSetShaderResources((uint) slot, NumViews: 1, in nativeShaderResourceView);
                    break;
            }
        }

        /// <summary>
        ///     Sets an unordered access view to the shader pipeline, without affecting ones that are already set.
        /// </summary>
        /// <param name="slot">The binding slot.</param>
        /// <param name="shaderResourceView">The shader resource view.</param>
        /// <param name="view">The native unordered access view.</param>
        /// <param name="uavInitialOffset">The Append/Consume buffer offset. See SetUnorderedAccessView for more details.</param>
        internal unsafe void OMSetSingleUnorderedAccessView(int slot, ID3D11UnorderedAccessView* view, int uavInitialOffset)
        {
            currentUARenderTargetViews[slot] = view;

            int remainingSlots = currentUARenderTargetViews.Length - currentRenderTargetViewsActiveCount;

            var uavs = stackalloc ID3D11UnorderedAccessView*[remainingSlots];

            for (int fromIndex = currentRenderTargetViewsActiveCount, toIndex = 0;
                 toIndex < remainingSlots;
                 fromIndex++, toIndex++)
            {
                uavs[toIndex] = currentUARenderTargetViews[fromIndex];
            }

            var uavInitialCounts = stackalloc uint[remainingSlots];

            for (int i = 0; i < remainingSlots; i++)
                uavInitialCounts[i] = unchecked((uint) -1);

            uavInitialCounts[slot - currentRenderTargetViewsActiveCount] = (uint) uavInitialOffset;

            nativeDeviceContext->CSSetUnorderedAccessViews((uint) currentRenderTargetViewsActiveCount, NumUAVs: (uint) remainingSlots,
                                                           uavs, uavInitialCounts);
        }

        /// <summary>
        /// Sets an unordered access view to the shader pipeline.
        /// </summary>
        /// <param name="stage">The stage.</param>
        /// <param name="slot">The slot.</param>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        /// <param name="uavInitialOffset">The Append/Consume buffer offset. A value of -1 indicates the current offset
        ///     should be kept. Any other values set the hidden counter for that Appendable/Consumable
        ///     UAV. uavInitialCount is only relevant for UAVs which have the 'Append' or 'Counter' buffer
        ///     flag, otherwise the argument is ignored.</param>
        /// <exception cref="ArgumentException">Invalid stage.;stage</exception>
        internal void SetUnorderedAccessView(ShaderStage stage, int slot, GraphicsResource unorderedAccessView, int uavInitialOffset)
        {
            if (stage != ShaderStage.Compute && stage != ShaderStage.Pixel)
                throw new ArgumentException("Invalid stage", nameof(stage));

            var nativeUnorderedAccessView = unorderedAccessView != null ? unorderedAccessView.NativeUnorderedAccessView : null;

            if (stage == ShaderStage.Compute)
            {
                if (unorderedAccessViews[slot] != nativeUnorderedAccessView)
                {
                    unorderedAccessViews[slot] = nativeUnorderedAccessView;

                    nativeDeviceContext->CSSetUnorderedAccessViews((uint) slot, NumUAVs: 1, in nativeUnorderedAccessView, (uint*) &uavInitialOffset);
                }
            }
            else
            {
                if (currentUARenderTargetViews[slot] != nativeUnorderedAccessView)
                {
                    OMSetSingleUnorderedAccessView(slot, nativeUnorderedAccessView, uavInitialOffset);
                }
            }
        }

        /// <summary>
        /// Unsets an unordered access view from the shader pipeline.
        /// </summary>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        internal void UnsetUnorderedAccessView(GraphicsResource unorderedAccessView)
        {
            var nativeUav = unorderedAccessView is not null ? unorderedAccessView.NativeUnorderedAccessView : null;
            if (nativeUav == null)
                return;

            for (int slot = 0; slot < UnorderedAcccesViewCount; slot++)
            {
                if (unorderedAccessViews[slot] == nativeUav)
                {
                    unorderedAccessViews[slot] = null;
                    nativeDeviceContext->CSSetUnorderedAccessViews((uint) slot, NumUAVs: 1, ppUnorderedAccessViews: null, pUAVInitialCounts: null);
                }
            }
            for (int slot = 0; slot < SimultaneousRenderTargetCount; slot++)
            {
                if (currentUARenderTargetViews[slot] == nativeUav)
                {
                    OMSetSingleUnorderedAccessView(slot, view: null, uavInitialOffset: -1);
                }
            }
        }

        /// <summary>
        ///     Prepares a draw call. This method is called before each Draw() method to setup the correct Primitive, InputLayout and VertexBuffers.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot GraphicsDevice.Draw*() without an effect being previously applied with Effect.Apply() method</exception>
        private void PrepareDraw()
        {
            SetViewportImpl();
        }

        public void SetStencilReference(int stencilReference)
        {
            ID3D11DepthStencilState* depthStencilState;
            uint stencilRef;
            nativeDeviceContext->OMGetDepthStencilState(&depthStencilState, &stencilRef);
            nativeDeviceContext->OMSetDepthStencilState(depthStencilState, (uint) stencilReference);
        }

        public void SetBlendFactor(Color4 blendFactor)
        {
            ID3D11BlendState* blendState;
            float prevBlendFactor;
            uint sampleMask;
            nativeDeviceContext->OMGetBlendState(&blendState, &prevBlendFactor, &sampleMask);
            nativeDeviceContext->OMSetBlendState(blendState, (float*) &blendFactor, sampleMask);
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            var newPipelineState = pipelineState ?? GraphicsDevice.DefaultPipelineState;

            // Pipeline state
            if (newPipelineState != currentPipelineState)
            {
                newPipelineState.Apply(this, currentPipelineState);
                currentPipelineState = newPipelineState;
            }
        }

        public void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            var nativeVertexBuffer = buffer is not null ? buffer.NativeBuffer : null;

            nativeDeviceContext->IASetVertexBuffers((uint) index, NumBuffers: 1, in nativeVertexBuffer, (uint*) &stride, (uint*) &offset);
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            var indexFormat = is32bits ? Format.FormatR32Uint : Format.FormatR16Uint;

            var nativeIndexBuffer = buffer is not null ? buffer.NativeBuffer : null;

            nativeDeviceContext->IASetIndexBuffer(nativeIndexBuffer, indexFormat, (uint) offset);
        }

        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            // Nothing to do
        }

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
            // Bind resources
            currentPipelineState?.ResourceBinder.BindResources(this, descriptorSets);
        }

        /// <inheritdoc />
        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
            PrepareDraw();

            nativeDeviceContext->Dispatch((uint) threadCountX, (uint) threadCountY, (uint) threadCountZ);
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            PrepareDraw();

            ArgumentNullException.ThrowIfNull(indirectBuffer);

            nativeDeviceContext->DispatchIndirect(indirectBuffer.NativeBuffer, (uint) offsetInBytes);
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->Draw((uint) vertexCount, (uint) startVertexLocation);

            GraphicsDevice.FrameTriangleCount += (uint) vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw geometry of an unknown size.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            nativeDeviceContext->DrawAuto();

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw indexed, non-instanced primitives.
        /// </summary>
        /// <param name="indexCount">Number of indices to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        public void DrawIndexed(int indexCount, int startIndexLocation = 0, int baseVertexLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->DrawIndexed((uint) indexCount, (uint) startIndexLocation, baseVertexLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint) indexCount;
        }

        /// <summary>
        /// Draw indexed, instanced primitives.
        /// </summary>
        /// <param name="indexCountPerInstance">Number of indices read from the index buffer for each instance.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->DrawIndexedInstanced((uint) indexCountPerInstance, (uint) instanceCount, (uint) startIndexLocation, baseVertexLocation, (uint) startInstanceLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint) (indexCountPerInstance * instanceCount);
        }

        /// <summary>
        /// Draw indexed, instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">A buffer containing the GPU generated primitives.</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawIndexedInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            ArgumentNullException.ThrowIfNull(argumentsBuffer);

            PrepareDraw();

            nativeDeviceContext->DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, (uint) alignedByteOffsetForArgs);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw non-indexed, instanced primitives.
        /// </summary>
        /// <param name="vertexCountPerInstance">Number of vertices to draw.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a vertex buffer.</param>
        public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation = 0, int startInstanceLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->DrawInstanced((uint) vertexCountPerInstance, (uint) instanceCount, (uint) startVertexLocation, (uint) startInstanceLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint) (vertexCountPerInstance * instanceCount);
        }

        /// <summary>
        /// Draw instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">An arguments buffer</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            ArgumentNullException.ThrowIfNull(argumentsBuffer);

            PrepareDraw();

            nativeDeviceContext->DrawInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Submits a GPU timestamp query.
        /// </summary>
        /// <param name="queryPool">The <see cref="QueryPool"/> owning the query.</param>
        /// <param name="index">The query index.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            var query = (ID3D11Asynchronous*) queryPool.NativeQueries[index].Query;

            nativeDeviceContext->End(query);
        }

        /// <summary>
        /// Begins debug event.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public void BeginProfile(Color4 profileColor, string name)
        {
            nativeDeviceProfiler->BeginEvent(name);
        }

        /// <summary>
        /// Ends debug event.
        /// </summary>
        public void EndProfile()
        {
            nativeDeviceProfiler->EndEvent();
        }

        /// <summary>
        /// Clears the specified depth stencil buffer. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="options">The options.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="stencil">The stencil.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public void Clear(Texture depthStencilBuffer, DepthStencilClearOptions options, float depth = 1, byte stencil = 0)
        {
            ArgumentNullException.ThrowIfNull(depthStencilBuffer);

            var flags = options.HasFlag(DepthStencilClearOptions.DepthBuffer) ? ClearFlag.Depth : 0;

            // Check that the DepthStencilBuffer has a Stencil if Clear Stencil is requested
            if (options.HasFlag(DepthStencilClearOptions.Stencil))
            {
                if (!depthStencilBuffer.HasStencil)
                    throw new InvalidOperationException(string.Format(FrameworkResources.NoStencilBufferForDepthFormat, depthStencilBuffer.ViewFormat));

                flags |= ClearFlag.Stencil;
            }

            nativeDeviceContext->ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView, (uint) flags, depth, stencil);
        }

        /// <summary>
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="ArgumentNullException">renderTarget</exception>
        public unsafe void Clear(Texture renderTarget, Color4 color)
        {
            ArgumentNullException.ThrowIfNull(renderTarget);

            nativeDeviceContext->ClearRenderTargetView(renderTarget.NativeRenderTargetView, (float*) &color);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public unsafe void ClearReadWrite(Buffer buffer, Vector4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (buffer.NativeUnorderedAccessView == null)
                throw new ArgumentException("Expecting a buffer supporting UAV", nameof(buffer));

            nativeDeviceContext->ClearUnorderedAccessViewFloat(buffer.NativeUnorderedAccessView, (float*) &value);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public unsafe void ClearReadWrite(Buffer buffer, Int4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (buffer.NativeUnorderedAccessView == null)
                throw new ArgumentException("Expecting a buffer supporting UAV", nameof(buffer));

            nativeDeviceContext->ClearUnorderedAccessViewUint(buffer.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public unsafe void ClearReadWrite(Buffer buffer, UInt4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);
            if (buffer.NativeUnorderedAccessView == null)
                throw new ArgumentException("Expecting a buffer supporting UAV", nameof(buffer));

            nativeDeviceContext->ClearUnorderedAccessViewUint(buffer.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting texture supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, Vector4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);
            if (texture.NativeUnorderedAccessView == null)
                throw new ArgumentException("Expecting a texture supporting UAV", nameof(texture));

            nativeDeviceContext->ClearUnorderedAccessViewFloat(texture.NativeUnorderedAccessView, (float*) &value);
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting texture supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, Int4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);
            if (texture.NativeUnorderedAccessView == null)
                throw new ArgumentException("Expecting a texture supporting UAV", nameof(texture));

            nativeDeviceContext->ClearUnorderedAccessViewUint(texture.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting texture supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, UInt4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);
            if (texture.NativeUnorderedAccessView == null)
                throw new ArgumentException("Expecting a texture supporting UAV", nameof(texture));

            nativeDeviceContext->ClearUnorderedAccessViewUint(texture.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        /// Copy a texture. View is ignored and full underlying texture is copied.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <param name="destination">The destination texture.</param>
        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            nativeDeviceContext->CopyResource(destination.NativeResource, source.NativeResource);
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            ArgumentNullException.ThrowIfNull(sourceMultisampleTexture);
            ArgumentNullException.ThrowIfNull(destTexture);

            if (!sourceMultisampleTexture.IsMultisample)
                throw new ArgumentOutOfRangeException(nameof(sourceMultisampleTexture), "Source texture is not a MSAA texture");

            nativeDeviceContext->ResolveSubresource(destTexture.NativeResource, (uint) destSubResource,
                                                    sourceMultisampleTexture.NativeResource, (uint) sourceSubResource,
                                                    (Format)(format == PixelFormat.None ? destTexture.Format : format));
        }

        public void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourecRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            if (sourecRegion.HasValue)
            {
                var value = sourecRegion.Value;
                var pSourceBox = (Box*) &value;

                nativeDeviceContext->CopySubresourceRegion(destination.NativeResource, (uint) destinationSubResource, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                           source.NativeResource, (uint)sourceSubresource, pSourceBox);
            }
            else
            {
                nativeDeviceContext->CopySubresourceRegion(destination.NativeResource, (uint) destinationSubResource, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                           source.NativeResource, (uint) sourceSubresource, pSrcBox: null);
            }
        }

        /// <inheritdoc />
        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetInBytes)
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destBuffer);

            nativeDeviceContext->CopyStructureCount(destBuffer.NativeBuffer, (uint) offsetInBytes, sourceBuffer.NativeUnorderedAccessView);
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
            ArgumentNullException.ThrowIfNull(resource);

            nativeDeviceContext->UpdateSubresource(resource.NativeResource, (uint) subResourceIndex, pDstBox: null,
                                                   pSrcData: (void*) databox.DataPointer, (uint) databox.RowPitch, (uint) databox.SlicePitch);
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox, ResourceRegion region)
        {
            ArgumentNullException.ThrowIfNull(resource);

            Box* pDestBox = (Box*) &region;

            nativeDeviceContext->UpdateSubresource(resource.NativeResource, (uint) subResourceIndex, pDestBox,
                                                   pSrcData: (void*) databox.DataPointer, (uint) databox.RowPitch, (uint) databox.SlicePitch);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        /// <summary>
        /// Maps a subresource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="subResourceIndex">Index of the sub resource.</param>
        /// <param name="mapMode">The map mode.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <param name="offsetInBytes">The offset information in bytes.</param>
        /// <param name="lengthInBytes">The length information in bytes.</param>
        /// <returns>Pointer to the sub resource to map.</returns>
        public unsafe MappedResource MapSubresource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            ArgumentNullException.ThrowIfNull(resource);

            // This resource has just been recycled by the GraphicsResourceAllocator, we force a rename to avoid GPU => GPU sync point
            if (resource.DiscardNextMap && mapMode == MapMode.WriteNoOverwrite)
                mapMode = MapMode.WriteDiscard;

            var mapType = (Map) mapMode;
            var mapFlags = (uint) (doNotWait ? MapFlag.DONotWait : 0);

            MappedResource mappedResource = new(resource, subResourceIndex, dataBox: default);
            MappedSubresource* mappedSubresource = (MappedSubresource*) &mappedResource.DataBox;

            HResult result = nativeDeviceContext->Map(resource.NativeResource, (uint) subResourceIndex, mapType, mapFlags, mappedSubresource);

            const int DXGI_ERROR_WAS_STILL_DRAWING = unchecked((int) 0x887A000A);

            if (doNotWait && result == DXGI_ERROR_WAS_STILL_DRAWING)
                return default;

            if (result.IsFailure)
                result.Throw();

            if (!mappedResource.DataBox.IsEmpty)
            {
                mappedSubresource->PData = (byte*) mappedSubresource->PData + offsetInBytes;
            }
            return mappedResource;
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public void UnmapSubresource(MappedResource mappedResource)
        {
            nativeDeviceContext->Unmap(mappedResource.Resource.NativeResource, (uint) mappedResource.SubResourceIndex);
        }

        #region Ptr structure

        // Ancillary boxing structure to store D3D COM pointers without messing with reference-counting lifetime management
        private readonly record struct Ptr<T>(T* Pointer) where T : unmanaged
        {
            public static implicit operator T*(Ptr<T> ptr) => ptr.Pointer;
            public static implicit operator Ptr<T>(T* ptr) => new(ptr);
        }

        #endregion
    }
}

#endif
