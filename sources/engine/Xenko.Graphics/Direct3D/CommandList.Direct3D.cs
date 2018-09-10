// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_DIRECT3D11
using System;
using SharpDX.Mathematics.Interop;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Shaders;

namespace Xenko.Graphics
{
    public partial class CommandList
    {
        private const int ConstantBufferCount = SharpDX.Direct3D11.CommonShaderStage.ConstantBufferApiSlotCount; // 14 actually
        private const int SamplerStateCount = SharpDX.Direct3D11.CommonShaderStage.SamplerSlotCount;
        private const int ShaderResourceViewCount = SharpDX.Direct3D11.CommonShaderStage.InputResourceSlotCount;
        private const int SimultaneousRenderTargetCount = SharpDX.Direct3D11.OutputMergerStage.SimultaneousRenderTargetCount;
        private const int StageCount = 6;
        private const int UnorderedAcccesViewCount = SharpDX.Direct3D11.ComputeShaderStage.UnorderedAccessViewSlotCount;

        private SharpDX.Direct3D11.DeviceContext nativeDeviceContext;
        private SharpDX.Direct3D11.DeviceContext1 nativeDeviceContext1;
        private SharpDX.Direct3D11.UserDefinedAnnotation nativeDeviceProfiler;

        private SharpDX.Direct3D11.InputAssemblerStage inputAssembler;
        private SharpDX.Direct3D11.OutputMergerStage outputMerger;

        private readonly SharpDX.Direct3D11.RenderTargetView[] currentRenderTargetViews = new SharpDX.Direct3D11.RenderTargetView[SimultaneousRenderTargetCount];
        private readonly SharpDX.Direct3D11.CommonShaderStage[] shaderStages = new SharpDX.Direct3D11.CommonShaderStage[StageCount];
        private readonly Buffer[] constantBuffers = new Buffer[StageCount * ConstantBufferCount];
        private readonly SamplerState[] samplerStates = new SamplerState[StageCount * SamplerStateCount];
        private readonly SharpDX.Direct3D11.UnorderedAccessView[] unorderedAccessViews = new SharpDX.Direct3D11.UnorderedAccessView[UnorderedAcccesViewCount]; // Only CS

        private PipelineState currentPipelineState;

        public static CommandList New(GraphicsDevice device)
        {
            throw new InvalidOperationException("Can't create multiple command lists with D3D11");
        }

        internal CommandList(GraphicsDevice device) : base(device)
        {
            nativeDeviceContext = device.NativeDeviceContext;
            NativeDeviceChild = nativeDeviceContext;
            nativeDeviceContext1 = new SharpDX.Direct3D11.DeviceContext1(nativeDeviceContext.NativePointer);
            nativeDeviceProfiler = device.IsDebugMode ? SharpDX.ComObject.QueryInterfaceOrNull<SharpDX.Direct3D11.UserDefinedAnnotation>(nativeDeviceContext.NativePointer) : null;

            InitializeStages();

            ClearState();
        }

        /// <summary>
        /// Gets the native device context.
        /// </summary>
        /// <value>The native device context.</value>
        internal SharpDX.Direct3D11.DeviceContext NativeDeviceContext => nativeDeviceContext;

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            Utilities.Dispose(ref nativeDeviceProfiler);

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
            return default(CompiledCommandList);
        }

        private void ClearStateImpl()
        {
            NativeDeviceContext.ClearState();

            for (int i = 0; i < samplerStates.Length; ++i)
                samplerStates[i] = null;
            for (int i = 0; i < constantBuffers.Length; ++i)
                constantBuffers[i] = null;
            for (int i = 0; i < unorderedAccessViews.Length; ++i)
                unorderedAccessViews[i] = null;
            for (int i = 0; i < currentRenderTargetViews.Length; i++)
                currentRenderTargetViews[i] = null;

            // Since nothing can be drawn in default state, no need to set anything (another SetPipelineState should happen before)
            currentPipelineState = GraphicsDevice.DefaultPipelineState;
        }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl()
        {
            for (int i = 0; i < currentRenderTargetViews.Length; i++)
                currentRenderTargetViews[i] = null;
            outputMerger.ResetTargets();
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="renderTargetCount">The number of render targets.</param>
        /// <param name="renderTargets">The render targets.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, Texture[] renderTargets)
        {
            for (int i = 0; i < renderTargetCount; i++)
                currentRenderTargetViews[i] = renderTargets[i].NativeRenderTargetView;

            outputMerger.SetTargets(depthStencilBuffer != null ? depthStencilBuffer.NativeDepthStencilView : null, renderTargetCount, currentRenderTargetViews);
        }

        unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle)
        {
            NativeDeviceContext.Rasterizer.SetScissorRectangle(scissorRectangle.Left, scissorRectangle.Top, scissorRectangle.Right, scissorRectangle.Bottom);
        }

        unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles)
        {
            if (scissorRectangles == null) throw new ArgumentNullException("scissorRectangles");
            var localScissorRectangles = new RawRectangle[scissorCount];
            for (int i = 0; i < scissorCount; i++)
            {
                localScissorRectangles[i] = new RawRectangle(scissorRectangles[i].X, scissorRectangles[i].Y, scissorRectangles[i].Right, scissorRectangles[i].Bottom);
            }
            NativeDeviceContext.Rasterizer.SetScissorRectangles(localScissorRectangles);
        }

        /// <summary>
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
            SharpDX.Direct3D11.StreamOutputBufferBinding[] streamOutputBufferBindings;

            if (buffers != null)
            {
                streamOutputBufferBindings = new SharpDX.Direct3D11.StreamOutputBufferBinding[buffers.Length];
                for (int i = 0; i < buffers.Length; ++i)
                    streamOutputBufferBindings[i].Buffer = buffers[i].NativeBuffer;
            }
            else
            {
                streamOutputBufferBindings = null;
            }

            NativeDeviceContext.StreamOutput.SetTargets(streamOutputBufferBindings);
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

            fixed (Viewport* viewportsPtr = viewports)
            {
                nativeDeviceContext.Rasterizer.SetViewports((RawViewportF*)viewportsPtr, renderTargetCount > 0 ? renderTargetCount : 1);
            }
        }

        /// <summary>
        /// Unsets the render targets.
        /// </summary>
        public void UnsetRenderTargets()
        {
            NativeDeviceContext.OutputMerger.ResetTargets();
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
                throw new ArgumentException("Cannot use Stage.None", "stage");

            int stageIndex = (int)stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;
            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;
                shaderStages[stageIndex].SetConstantBuffer(slot, buffer != null ? buffer.NativeBuffer : null);
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
                throw new ArgumentException("Cannot use Stage.None", "stage");

            int stageIndex = (int)stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;
            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;
                shaderStages[stageIndex].SetConstantBuffer(slot, buffer != null ? buffer.NativeBuffer : null);
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
                throw new ArgumentException("Cannot use Stage.None", "stage");
            int stageIndex = (int)stage - 1;

            int slotIndex = stageIndex * SamplerStateCount + slot;
            if (samplerStates[slotIndex] != samplerState)
            {
                samplerStates[slotIndex] = samplerState;
                shaderStages[stageIndex].SetSampler(slot, samplerState != null ? (SharpDX.Direct3D11.SamplerState)samplerState.NativeDeviceChild : null);
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
            shaderStages[(int)stage - 1].SetShaderResource(slot, shaderResourceView != null ? shaderResourceView.NativeShaderResourceView : null);
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
        /// <exception cref="System.ArgumentException">Invalid stage.;stage</exception>
        internal void SetUnorderedAccessView(ShaderStage stage, int slot, GraphicsResource unorderedAccessView, int uavInitialOffset)
        {
            if (stage != ShaderStage.Compute)
                throw new ArgumentException("Invalid stage.", "stage");

            var view = unorderedAccessView?.NativeUnorderedAccessView;
            if (unorderedAccessViews[slot] != view)
            {
                unorderedAccessViews[slot] = view;
                NativeDeviceContext.ComputeShader.SetUnorderedAccessView(slot, view, uavInitialOffset);
            }
        }

        /// <summary>
        /// Unsets an unordered access view from the shader pipeline.
        /// </summary>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        internal void UnsetUnorderedAccessView(GraphicsResource unorderedAccessView)
        {
            var view = unorderedAccessView?.NativeUnorderedAccessView;
            if (view == null)
                return;

            for (int slot = 0; slot < UnorderedAcccesViewCount; slot++)
            {
                if (unorderedAccessViews[slot] == view)
                {
                    unorderedAccessViews[slot] = null;
                    NativeDeviceContext.ComputeShader.SetUnorderedAccessView(slot, null);
                }
            }
        }

        /// <summary>
        ///     Prepares a draw call. This method is called before each Draw() method to setup the correct Primitive, InputLayout and VertexBuffers.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot GraphicsDevice.Draw*() without an effect being previously applied with Effect.Apply() method</exception>
        private void PrepareDraw()
        {
            SetViewportImpl();
        }

        public void SetStencilReference(int stencilReference)
        {
            nativeDeviceContext.OutputMerger.DepthStencilReference = stencilReference;
        }

        public void SetBlendFactor(Color4 blendFactor)
        {
            nativeDeviceContext.OutputMerger.BlendFactor = ColorHelper.Convert(blendFactor);
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
            inputAssembler.SetVertexBuffers(index, new SharpDX.Direct3D11.VertexBufferBinding(buffer.NativeBuffer, stride, offset));
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            inputAssembler.SetIndexBuffer(buffer != null ? buffer.NativeBuffer : null, is32bits ? SharpDX.DXGI.Format.R32_UInt : SharpDX.DXGI.Format.R16_UInt, offset);
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

            NativeDeviceContext.Dispatch(threadCountX, threadCountY, threadCountZ);
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            PrepareDraw();

            if (indirectBuffer == null) throw new ArgumentNullException("indirectBuffer");
            NativeDeviceContext.DispatchIndirect(indirectBuffer.NativeBuffer, offsetInBytes);
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            NativeDeviceContext.Draw(vertexCount, startVertexLocation);

            GraphicsDevice.FrameTriangleCount += (uint)vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw geometry of an unknown size.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            NativeDeviceContext.DrawAuto();

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

            NativeDeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint)indexCount;
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

            NativeDeviceContext.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint)(indexCountPerInstance * instanceCount);
        }

        /// <summary>
        /// Draw indexed, instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">A buffer containing the GPU generated primitives.</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawIndexedInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            if (argumentsBuffer == null) throw new ArgumentNullException("argumentsBuffer");

            PrepareDraw();

            NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);

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

            NativeDeviceContext.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint)(vertexCountPerInstance * instanceCount);
        }

        /// <summary>
        /// Draw instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">An arguments buffer</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            if (argumentsBuffer == null) throw new ArgumentNullException("argumentsBuffer");

            PrepareDraw();

            NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Submits a GPU timestamp query.
        /// </summary>
        /// <param name="queryPool">The <see cref="QueryPool"/> owning the query.</param>
        /// <param name="index">The query index.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            nativeDeviceContext.End(queryPool.NativeQueries[index]);    
        }

        /// <summary>
        /// Begins debug event.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public void BeginProfile(Color4 profileColor, string name)
        {
            nativeDeviceProfiler?.BeginEvent(name);
        }

        /// <summary>
        /// Ends debug event.
        /// </summary>
        public void EndProfile()
        {
            nativeDeviceProfiler?.EndEvent();
        }

        /// <summary>
        /// Clears the specified depth stencil buffer. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="options">The options.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="stencil">The stencil.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
        public void Clear(Texture depthStencilBuffer, DepthStencilClearOptions options, float depth = 1, byte stencil = 0)
        {
            if (depthStencilBuffer == null) throw new ArgumentNullException("depthStencilBuffer");

            var flags = ((options & DepthStencilClearOptions.DepthBuffer) != 0) ? SharpDX.Direct3D11.DepthStencilClearFlags.Depth : 0;

            // Check that the DepthStencilBuffer has a Stencil if Clear Stencil is requested
            if ((options & DepthStencilClearOptions.Stencil) != 0)
            {
                if (!depthStencilBuffer.HasStencil)
                    throw new InvalidOperationException(string.Format(FrameworkResources.NoStencilBufferForDepthFormat, depthStencilBuffer.ViewFormat));
                flags |= SharpDX.Direct3D11.DepthStencilClearFlags.Stencil;
            }

            NativeDeviceContext.ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView, flags, depth, stencil);
        }

        /// <summary>
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="System.ArgumentNullException">renderTarget</exception>
        public unsafe void Clear(Texture renderTarget, Color4 color)
        {
            if (renderTarget == null) throw new ArgumentNullException("renderTarget");

            NativeDeviceContext.ClearRenderTargetView(renderTarget.NativeRenderTargetView, *(RawColor4*)&color);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public unsafe void ClearReadWrite(Buffer buffer, Vector4 value)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));

            NativeDeviceContext.ClearUnorderedAccessView(buffer.NativeUnorderedAccessView, *(RawVector4*)&value);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public unsafe void ClearReadWrite(Buffer buffer, Int4 value)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));

            NativeDeviceContext.ClearUnorderedAccessView(buffer.NativeUnorderedAccessView, *(RawInt4*)&value);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public unsafe void ClearReadWrite(Buffer buffer, UInt4 value)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (buffer.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));

            NativeDeviceContext.ClearUnorderedAccessView(buffer.NativeUnorderedAccessView, *(RawInt4*)&value);
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">texture</exception>
        /// <exception cref="System.ArgumentException">Expecting texture supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, Vector4 value)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (texture.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            NativeDeviceContext.ClearUnorderedAccessView(texture.NativeUnorderedAccessView, *(RawVector4*)&value);
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">texture</exception>
        /// <exception cref="System.ArgumentException">Expecting texture supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, Int4 value)
        {
             if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (texture.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            NativeDeviceContext.ClearUnorderedAccessView(texture.NativeUnorderedAccessView, *(RawInt4*)&value);
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">texture</exception>
        /// <exception cref="System.ArgumentException">Expecting texture supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, UInt4 value)
        {
            if (texture == null) throw new ArgumentNullException(nameof(texture));
            if (texture.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            NativeDeviceContext.ClearUnorderedAccessView(texture.NativeUnorderedAccessView, *(RawInt4*)&value);
        }

        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            NativeDeviceContext.CopyResource(source.NativeResource, destination.NativeResource);
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            if (sourceMultisampleTexture == null) throw new ArgumentNullException(nameof(sourceMultisampleTexture));
            if (destTexture == null) throw new ArgumentNullException("destTexture");
            if (!sourceMultisampleTexture.IsMultisample) throw new ArgumentOutOfRangeException(nameof(sourceMultisampleTexture), "Source texture is not a MSAA texture");

            NativeDeviceContext.ResolveSubresource(sourceMultisampleTexture.NativeResource, sourceSubResource, destTexture.NativeResource, destSubResource, (SharpDX.DXGI.Format)(format == PixelFormat.None ? destTexture.Format : format));
        }

        public void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourecRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            var nullableSharpDxRegion = new SharpDX.Direct3D11.ResourceRegion?();

            if (sourecRegion.HasValue)
            {
                var value = sourecRegion.Value;
                nullableSharpDxRegion = new SharpDX.Direct3D11.ResourceRegion(value.Left, value.Top, value.Front, value.Right, value.Bottom, value.Back);
            }

            NativeDeviceContext.CopySubresourceRegion(source.NativeResource, sourceSubresource, nullableSharpDxRegion, destination.NativeResource, destinationSubResource, dstX, dstY, dstZ);
        }

        /// <inheritdoc />
        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetInBytes)
        {
            if (sourceBuffer == null) throw new ArgumentNullException("sourceBuffer");
            if (destBuffer == null) throw new ArgumentNullException("destBuffer");
            NativeDeviceContext.CopyStructureCount(destBuffer.NativeBuffer, offsetInBytes, sourceBuffer.NativeUnorderedAccessView);
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
            if (resource == null) throw new ArgumentNullException("resource");
            NativeDeviceContext.UpdateSubresource(*(SharpDX.DataBox*)Interop.Cast(ref databox), resource.NativeResource, subResourceIndex);
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox, ResourceRegion region)
        {
            if (resource == null) throw new ArgumentNullException("resource");
            NativeDeviceContext.UpdateSubresource(*(SharpDX.DataBox*)Interop.Cast(ref databox), resource.NativeResource, subResourceIndex, *(SharpDX.Direct3D11.ResourceRegion*)Interop.Cast(ref region));
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
            if (resource == null) throw new ArgumentNullException("resource");

            // This resource has just been recycled by the GraphicsResourceAllocator, we force a rename to avoid GPU=>GPU sync point
            if (resource.DiscardNextMap && mapMode == MapMode.WriteNoOverwrite)
                mapMode = MapMode.WriteDiscard;

            SharpDX.DataBox dataBox = NativeDeviceContext.MapSubresource(resource.NativeResource, subResourceIndex, (SharpDX.Direct3D11.MapMode)mapMode, doNotWait ? SharpDX.Direct3D11.MapFlags.DoNotWait : SharpDX.Direct3D11.MapFlags.None);
            var databox = *(DataBox*)Interop.Cast(ref dataBox);
            if (!dataBox.IsEmpty)
            {
                databox.DataPointer = (IntPtr)((byte*)databox.DataPointer + offsetInBytes);
            }
            return new MappedResource(resource, subResourceIndex, databox);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public void UnmapSubresource(MappedResource unmapped)
        {
            NativeDeviceContext.UnmapSubresource(unmapped.Resource.NativeResource, unmapped.SubResourceIndex);
        }

        private void InitializeStages()
        {
            inputAssembler = nativeDeviceContext.InputAssembler;
            outputMerger = nativeDeviceContext.OutputMerger;
            shaderStages[(int)ShaderStage.Vertex - 1] = nativeDeviceContext.VertexShader;
            shaderStages[(int)ShaderStage.Hull - 1] = nativeDeviceContext.HullShader;
            shaderStages[(int)ShaderStage.Domain - 1] = nativeDeviceContext.DomainShader;
            shaderStages[(int)ShaderStage.Geometry - 1] = nativeDeviceContext.GeometryShader;
            shaderStages[(int)ShaderStage.Pixel - 1] = nativeDeviceContext.PixelShader;
            shaderStages[(int)ShaderStage.Compute - 1] = nativeDeviceContext.ComputeShader;
        }
    }
}
 
#endif 
