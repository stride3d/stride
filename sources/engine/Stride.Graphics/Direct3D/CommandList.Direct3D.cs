// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_DIRECT3D11
using System;
using System.Linq;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using Silk.NET.DXGI;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics.Direct3D;
using Stride.Graphics.Direct3D.Extensions;
using Stride.Shaders;

namespace Stride.Graphics
{
    public partial class CommandList
    {
        private const int ConstantBufferCount = D3D11.CommonshaderConstantBufferApiSlotCount;
        private const int SamplerStateCount = D3D11.CommonshaderSamplerSlotCount;
        private const int ShaderResourceViewCount = D3D11.CommonshaderInputResourceSlotCount;
        private const int SimultaneousRenderTargetCount = D3D11.SimultaneousRenderTargetCount; 
        private const int StageCount = 6;
        private const int UnorderedAcccesViewCount = D3D11.D3D111UavSlotCount;

        private ComPtr<ID3D11DeviceContext> nativeDeviceContext;
        private ComPtr<ID3D11DeviceContext1> nativeDeviceContext1;
        private ComPtr<ID3DUserDefinedAnnotation> nativeDeviceProfiler;

        private ComPtr<ID3D11DeviceContext> inputAssembler { get { return nativeDeviceContext; } }
        private ComPtr<ID3D11DeviceContext> outputMerger { get { return nativeDeviceContext; } }

        private readonly ComPtr<ID3D11RenderTargetView>[] currentRenderTargetViews = new ComPtr<ID3D11RenderTargetView>[SimultaneousRenderTargetCount];
        private          int currentRenderTargetViewsActiveCount = 0;
        private readonly ComPtr<ID3D11UnorderedAccessView>[] currentUARenderTargetViews = new ComPtr<ID3D11UnorderedAccessView>[SimultaneousRenderTargetCount];
        private readonly ComPtr<IUnknown>[] shaderStages = new ComPtr<IUnknown>[StageCount];
        private readonly Buffer[] constantBuffers = new Buffer[StageCount * ConstantBufferCount];
        private readonly SamplerState[] samplerStates = new SamplerState[StageCount * SamplerStateCount];
        private readonly unsafe ComPtr<ID3D11UnorderedAccessView>[] unorderedAccessViews = new ComPtr<ID3D11UnorderedAccessView> [UnorderedAcccesViewCount]; // Only CS

        private PipelineState currentPipelineState;

        public static CommandList New(GraphicsDevice device)
        {
            throw new InvalidOperationException("Can't create multiple command lists with D3D11");
        }

        internal CommandList(GraphicsDevice device) : base(device)
        {
            nativeDeviceContext = device.NativeDeviceContext;
            unsafe { NativeDeviceChild = (ID3D11DeviceChild*)nativeDeviceContext.Handle; }
            unsafe
            {
                
                    ID3D11DeviceContext1* dc = null;
                    SilkMarshal.ThrowHResult(NativeDeviceChild.Get().QueryInterface(SilkMarshal.GuidPtrOf<ID3D11DeviceContext1>(), (void**)&dc));
                    nativeDeviceContext1.Handle = dc;
            }
            InitializeStages();

            ClearState();
        }

        /// <summary>
        /// Gets the native device context.
        /// </summary>
        /// <value>The native device context.</value>
        internal ComPtr<ID3D11DeviceContext> NativeDeviceContext => nativeDeviceContext;

        internal ComPtr<ID3D11DeviceContext> ShaderStages => nativeDeviceContext;

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            nativeDeviceProfiler.Release();
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
            NativeDeviceContext.Get().ClearState();
            for (int i = 0; i < samplerStates.Length; ++i)
                samplerStates[i] = null;
            for (int i = 0; i < constantBuffers.Length; ++i)
                constantBuffers[i] = null;
            unsafe
            {
                
                for (int i = 0; i < unorderedAccessViews.Length; ++i)
                    unorderedAccessViews[i] = null;
                for (int i = 0; i < currentRenderTargetViews.Length; i++)
                    currentRenderTargetViews[i] = null;
                for (int i = 0; i < currentUARenderTargetViews.Length; i++)
                    currentUARenderTargetViews[i] = null;
            }
            

            // Since nothing can be drawn in default state, no need to set anything (another SetPipelineState should happen before)
            currentPipelineState = GraphicsDevice.DefaultPipelineState;
        }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl()
        {
            unsafe
            {
                for (int i = 0; i < currentRenderTargetViews.Length; i++)
                    currentRenderTargetViews[i] = null;
                for (int i = 0; i < currentUARenderTargetViews.Length; i++)
                    currentUARenderTargetViews[i] = null;
                outputMerger.Get().OMSetRenderTargets(0, null, null);
            }
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
            currentRenderTargetViewsActiveCount = renderTargetCount;
            unsafe
            {
                for (int i = 0; i < renderTargetCount; i++)
                    currentRenderTargetViews[i] = renderTargets[i].NativeID3D11RenderTargetView;
                //var rtvs = currentRenderTargetViews.Select(x => (IntPtr)x.Handle).ToArray();
                ID3D11RenderTargetView*[] rtvs = new ID3D11RenderTargetView*[currentRenderTargetViews.Length];
                for (int i = 0; i < renderTargetCount; i++)
                    rtvs[i] = currentRenderTargetViews[i].Handle;
                if(depthStencilBuffer != null)
                    fixed(ID3D11RenderTargetView** r = rtvs)
                        outputMerger.Get().OMSetRenderTargets((uint)depthStencilBuffer.GetViewCount(), r, depthStencilBuffer.NativeDepthStencilView.Handle);
            }
            
        }

        unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle)
        {
            //NativeDeviceContext.Rasterizer.SetScissorRectangle();
            var rect = new Silk.NET.Maths.Rectangle<int>(
                    new Silk.NET.Maths.Vector2D<int>(scissorRectangle.X, scissorRectangle.Y),
                    new Silk.NET.Maths.Vector2D<int>(scissorRectangle.Width, scissorRectangle.Height)
            );
            NativeDeviceContext.Get().RSSetScissorRects(1,&rect);
        }

        unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles)
        {
            if (scissorRectangles == null) throw new ArgumentNullException("scissorRectangles");
            var localScissorRectangles = new Silk.NET.Maths.Rectangle<int>[scissorCount];
            for (int i = 0; i < scissorCount; i++)
            {
                localScissorRectangles[i] = new Silk.NET.Maths.Rectangle<int>(new Silk.NET.Maths.Vector2D<int>(scissorRectangles[i].X, scissorRectangles[i].Y), new Silk.NET.Maths.Vector2D<int>(scissorRectangles[i].Width, scissorRectangles[i].Height));
            }
            fixed(Silk.NET.Maths.Rectangle<int>* rs = localScissorRectangles)
                NativeDeviceContext.Get().RSSetScissorRects((uint)scissorCount,rs);
        }

        /// <summary>
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
            ID3D11Buffer[] streamOutputBufferBindings;

            if (buffers != null)
            {
                streamOutputBufferBindings = new ID3D11Buffer[buffers.Length];
                for (int i = 0; i < buffers.Length; ++i)
                    streamOutputBufferBindings[i] = buffers[i].NativeBuffer.Get();
            }
            else
            {
                streamOutputBufferBindings = null;
            }
            unsafe
            {
                uint l = (uint)buffers.Length;
                fixed (ID3D11Buffer* buffs = streamOutputBufferBindings)
                    NativeDeviceContext.Get().SOSetTargets(l, &buffs,null);
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

            fixed (Viewport* viewportsPtr = viewports)
            {
                nativeDeviceContext.Get().RSSetViewports(renderTargetCount > 0 ? (uint)renderTargetCount : 1, (Silk.NET.Direct3D11.Viewport*)viewportsPtr);
            }
        }

        /// <summary>
        /// Unsets the render targets.
        /// </summary>
        public void UnsetRenderTargets()
        {
            unsafe
            {
                NativeDeviceContext.Get().OMSetRenderTargets(0, null, null);
            }
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
            unsafe
            {
                
                if (constantBuffers[slotIndex] != buffer)
                {
                    constantBuffers[slotIndex] = buffer;
                    var buffs = new ID3D11Buffer*[] { buffer.NativeBuffer.Handle };
                    fixed(ID3D11Buffer** b = buffs)
                    switch (stage)
                    {
                        case ShaderStage.Vertex:
                            GraphicsDevice.NativeDeviceContext.Get().VSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Hull:
                            GraphicsDevice.NativeDeviceContext.Get().HSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Domain:
                            GraphicsDevice.NativeDeviceContext.Get().DSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Geometry:
                            GraphicsDevice.NativeDeviceContext.Get().GSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Pixel:
                            GraphicsDevice.NativeDeviceContext.Get().PSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Compute:
                            GraphicsDevice.NativeDeviceContext.Get().CSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        default:
                            break;
                    }
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
                throw new ArgumentException("Cannot use Stage.None", "stage");

            int stageIndex = (int)stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;
            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;
                unsafe
                {
                    var buffs = new ID3D11Buffer*[] { buffer.NativeBuffer.Handle };
                    fixed (ID3D11Buffer** b = buffs)
                    switch (stage)
                    {
                        case ShaderStage.Vertex:
                            GraphicsDevice.NativeDeviceContext.Get().VSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Hull:
                            GraphicsDevice.NativeDeviceContext.Get().HSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Domain:
                            GraphicsDevice.NativeDeviceContext.Get().DSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Geometry:
                            GraphicsDevice.NativeDeviceContext.Get().GSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Pixel:
                            GraphicsDevice.NativeDeviceContext.Get().PSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        case ShaderStage.Compute:
                            GraphicsDevice.NativeDeviceContext.Get().CSSetConstantBuffers((uint)slot, (uint)buffer.ElementCount, b);
                            break;
                        default:
                            break;
                    }
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
                throw new ArgumentException("Cannot use Stage.None", "stage");
            int stageIndex = (int)stage - 1;

            int slotIndex = stageIndex * SamplerStateCount + slot;
            if (samplerStates[slotIndex] != samplerState)
            {
                samplerStates[slotIndex] = samplerState;
                //shaderStages[stageIndex].SetSampler(slot, samplerState != null ? (SharpDX.Direct3D11.SamplerState)samplerState.NativeDeviceChild : null);
                unsafe
                {
                    ID3D11SamplerState* ss = null;
                    switch (stage)
                    {
                        case ShaderStage.Vertex:
                            GraphicsDevice.NativeDeviceContext.Get().VSSetSamplers((uint)slot, 1, &ss);
                            break;
                        case ShaderStage.Hull:
                            GraphicsDevice.NativeDeviceContext.Get().HSSetSamplers((uint)slot, 1, &ss);
                            break;
                        case ShaderStage.Domain:
                            GraphicsDevice.NativeDeviceContext.Get().DSSetSamplers((uint)slot, 1, &ss);
                            break;
                        case ShaderStage.Geometry:
                            GraphicsDevice.NativeDeviceContext.Get().GSSetSamplers((uint)slot, 1, &ss);
                            break;
                        case ShaderStage.Pixel:
                            GraphicsDevice.NativeDeviceContext.Get().PSSetSamplers((uint)slot, 1, &ss);
                            break;
                        case ShaderStage.Compute:
                            GraphicsDevice.NativeDeviceContext.Get().CSSetSamplers((uint)slot, 1, &ss);
                            break;
                        default:
                            break;
                    }
                    samplerState.NativeDeviceChild = new ComPtr<ID3D11DeviceChild>((ID3D11DeviceChild*)ss);
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
            //shaderStages[(int)stage - 1].SetShaderResource(slot, shaderResourceView != null ? shaderResourceView.NativeShaderResourceView : null);
            unsafe
            {
                ID3D11ShaderResourceView* srv = null;
                switch (stage)
                {
                    case ShaderStage.Vertex:
                        GraphicsDevice.NativeDeviceContext.Get().VSSetShaderResources((uint)slot, 1, &srv);
                        break;
                    case ShaderStage.Hull:
                        GraphicsDevice.NativeDeviceContext.Get().HSSetShaderResources((uint)slot, 1, &srv);
                        break;
                    case ShaderStage.Domain:
                        GraphicsDevice.NativeDeviceContext.Get().DSSetShaderResources((uint)slot, 1, &srv);
                        break;
                    case ShaderStage.Geometry:
                        GraphicsDevice.NativeDeviceContext.Get().GSSetShaderResources((uint)slot, 1, &srv);
                        break;
                    case ShaderStage.Pixel:
                        GraphicsDevice.NativeDeviceContext.Get().PSSetShaderResources((uint)slot, 1, &srv);
                        break;
                    case ShaderStage.Compute:
                        GraphicsDevice.NativeDeviceContext.Get().CSSetShaderResources((uint)slot, 1, &srv);
                        break;
                    default:
                        break;
                }
                shaderResourceView.NativeShaderResourceView = new ComPtr<ID3D11ShaderResourceView>(srv);
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
            unsafe
            {

                currentUARenderTargetViews[slot] = view;

                int remainingSlots = currentUARenderTargetViews.Length - currentRenderTargetViewsActiveCount;

                var uavs = new ID3D11UnorderedAccessView*[remainingSlots];
                Array.Copy(currentUARenderTargetViews, currentRenderTargetViewsActiveCount, uavs, 0, remainingSlots);

                var uavInitialCounts = new uint[remainingSlots];
                for (int i = 0; i < remainingSlots; i++)
                    unchecked { uavInitialCounts[i] = (uint)-1;}
                uavInitialCounts[slot - currentRenderTargetViewsActiveCount] = (uint)uavInitialOffset;
                fixed (ID3D11UnorderedAccessView** puavs = uavs)
                fixed (uint* initCounts = uavInitialCounts)
                    outputMerger.Get().CSSetUnorderedAccessViews(0,(uint)currentRenderTargetViewsActiveCount, puavs, initCounts);
            }
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
            if (stage != ShaderStage.Compute && stage != ShaderStage.Pixel)
                throw new ArgumentException("Invalid stage.", "stage");

            unsafe
            {


                //var view = unorderedAccessView != null? unorderedAccessView.NativeUnorderedAccessView : new ID3D11UnorderedAccessView();
                //fixed(ID3D11UnorderedAccessView* view = &unorderedAccessView.unorderedAccessView)
                if (stage == ShaderStage.Compute)
                {
                    if (unorderedAccessViews[slot].Handle != unorderedAccessView.NativeUnorderedAccessView.Handle)
                    {
                        unorderedAccessViews[slot] = unorderedAccessView.NativeUnorderedAccessView;
                        var count = (uint)unorderedAccessViews.Length;
                        var vs = new ID3D11UnorderedAccessView*[unorderedAccessViews.Length];
                        for (int i = 0; i < unorderedAccessViews.Length; i++)
                            vs[i] = unorderedAccessViews[i].Handle;
                        fixed(ID3D11UnorderedAccessView** v = vs)
                            NativeDeviceContext.Get().CSSetUnorderedAccessViews((uint)slot, count, v, (uint*)&uavInitialOffset);
                        
                    }
                }
                else
                {
                    if (currentUARenderTargetViews[slot] != unorderedAccessView.NativeUnorderedAccessView.Handle)
                    {
                        OMSetSingleUnorderedAccessView(slot, unorderedAccessView.NativeUnorderedAccessView.Handle, uavInitialOffset);
                    }
                }
            }
        }

        /// <summary>
        /// Unsets an unordered access view from the shader pipeline.
        /// </summary>
        /// <param name="unorderedAccessView">The unordered access view.</param>
        internal void UnsetUnorderedAccessView(GraphicsResource unorderedAccessView)
        {
            unsafe
            {
                

                if (unorderedAccessView == null)
                    return;
                var view = unorderedAccessView.NativeUnorderedAccessView;
            

                for (int slot = 0; slot < UnorderedAcccesViewCount; slot++)
                {
                    if (unorderedAccessViews[slot].Handle == view.Handle)
                    {
                        unorderedAccessViews[slot] = null;
                        NativeDeviceContext.Get().CSSetUnorderedAccessViews((uint)slot, 0, null, null);
                    }
                }
                for (int slot = 0; slot < SimultaneousRenderTargetCount; slot++)
                {
                    if (currentUARenderTargetViews[slot].Handle == view.Handle)
                    {
                        OMSetSingleUnorderedAccessView(slot, null, -1);
                    }
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
            unsafe
            {
                ID3D11DepthStencilState* dss = null;
                nativeDeviceContext.Get().OMGetDepthStencilState(&dss, null);
                nativeDeviceContext.Get().OMSetDepthStencilState(dss, (uint)stencilReference);
            }
        }

        public void SetBlendFactor(Color4 blendFactor)
        {
            unsafe
            {
                ID3D11BlendState* bs = null;
                uint* smask = null;
                float[] bf = blendFactor.ToVector4().ToArray();
                nativeDeviceContext.Get().OMGetBlendState(&bs, null,smask);
                fixed(float* bfp = bf)
                    nativeDeviceContext.Get().OMSetBlendState(bs, bfp, *smask);
            }
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
            unsafe
            {
                var count = buffer.ElementCount;
                var buffs = new ID3D11Buffer*[] { buffer.NativeBuffer.Handle };
                fixed(ID3D11Buffer** b = buffs)
                    inputAssembler.Get().IASetVertexBuffers((uint)index, (uint)count, b, (uint*)&stride, (uint*)&offset);
            }
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            unsafe
            {
                inputAssembler.Get().IASetIndexBuffer(buffer.NativeBuffer.Handle, Silk.NET.DXGI.Format.FormatR32Uint, (uint)offset);
            }
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

            NativeDeviceContext.Get().Dispatch((uint)threadCountX, (uint)threadCountY, (uint)threadCountZ);
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
            unsafe
            {
                NativeDeviceContext.Get().DispatchIndirect(indirectBuffer.NativeBuffer.Handle, (uint)offsetInBytes);
            }
            
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            NativeDeviceContext.Get().Draw((uint)vertexCount, (uint)startVertexLocation);

            GraphicsDevice.FrameTriangleCount += (uint)vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw geometry of an unknown size.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            NativeDeviceContext.Get().DrawAuto();

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

            NativeDeviceContext.Get().DrawIndexed((uint)indexCount, (uint)startIndexLocation, baseVertexLocation);

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

            NativeDeviceContext.Get().DrawIndexedInstanced((uint)indexCountPerInstance, (uint)instanceCount, (uint)startIndexLocation, baseVertexLocation, (uint)startInstanceLocation);

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
            unsafe
            {
                NativeDeviceContext.Get().DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer.Handle, (uint)alignedByteOffsetForArgs);
            }
            

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

            NativeDeviceContext.Get().DrawInstanced((uint)vertexCountPerInstance, (uint)instanceCount, (uint)startVertexLocation, (uint)startInstanceLocation);

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
            unsafe
            {
                NativeDeviceContext.Get().DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer.Handle, (uint)alignedByteOffsetForArgs);
            }

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Submits a GPU timestamp query.
        /// </summary>
        /// <param name="queryPool">The <see cref="QueryPool"/> owning the query.</param>
        /// <param name="index">The query index.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            unsafe
            {
                nativeDeviceContext.Get().End((ID3D11Asynchronous*)queryPool.NativeQueries[index].Handle);
            }
        }

        /// <summary>
        /// Begins debug event.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public void BeginProfile(Color4 profileColor, string name)
        {
            nativeDeviceProfiler.Get().BeginEvent(name);
        }

        /// <summary>
        /// Ends debug event.
        /// </summary>
        public void EndProfile()
        {
            nativeDeviceProfiler.Get().EndEvent();
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

            var flags = ((options & DepthStencilClearOptions.DepthBuffer) != 0) ? ClearFlag.ClearDepth : 0;

            // Check that the DepthStencilBuffer has a Stencil if Clear Stencil is requested
            if ((options & DepthStencilClearOptions.Stencil) != 0)
            {
                if (!depthStencilBuffer.HasStencil)
                    throw new InvalidOperationException(string.Format(FrameworkResources.NoStencilBufferForDepthFormat, depthStencilBuffer.ViewFormat));
                flags |= ClearFlag.ClearStencil;
            }
            unsafe
            {
                NativeDeviceContext.Get().ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView, (uint)flags, depth, stencil);
            }
            
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
            unsafe
            {
                fixed (float* col = color.ToArray())
                    NativeDeviceContext.Get().ClearRenderTargetView(renderTarget.NativeID3D11RenderTargetView, col);
            }
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
            if (buffer.NativeUnorderedAccessView.Handle == null) throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));
            unsafe
            {
                fixed (float* f4 = value.ToArray())
                    NativeDeviceContext.Get().ClearUnorderedAccessViewFloat(buffer.NativeUnorderedAccessView, f4);
            }
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
            if (buffer.NativeUnorderedAccessView.Handle == null) throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));
            unsafe
            {
                fixed (uint* i4 = new uint[] { (uint)value.X,(uint)value.Y,(uint)value.Z,(uint)value.W})
                    NativeDeviceContext.Get().ClearUnorderedAccessViewUint(buffer.NativeUnorderedAccessView, i4);
            }
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
            if (buffer.NativeUnorderedAccessView.Handle == null) throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));
            
            unsafe
            {
                
                fixed (uint* ui = value.ToArray())
                    NativeDeviceContext.Get().ClearUnorderedAccessViewUint(buffer.NativeUnorderedAccessView, ui);
            }
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
            //if (texture.NativeUnorderedAccessView == null) throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));
            unsafe
            {
                fixed (float* f4 = value.ToArray())
                    NativeDeviceContext.Get().ClearUnorderedAccessViewFloat(texture.NativeUnorderedAccessView, f4);
            }
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
            if (texture.NativeUnorderedAccessView.Handle == null) throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            unsafe
            {
                fixed (uint* i4 = new uint[] { (uint)value.X, (uint)value.Y, (uint)value.Z, (uint)value.W })
                    NativeDeviceContext.Get().ClearUnorderedAccessViewUint(texture.NativeUnorderedAccessView, i4);
            }
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
            if (texture.NativeUnorderedAccessView.Handle == null) throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            unsafe
            {
                fixed (uint* ui = value.ToArray())
                    NativeDeviceContext.Get().ClearUnorderedAccessViewUint(texture.NativeUnorderedAccessView, ui);
            }
        }

        /// <summary>
        /// Copy a texture. View is ignored and full underlying texture is copied.
        /// </summary>
        /// <param name="source">The source texture.</param>
        /// <param name="destination">The destination texture.</param>
        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");
            unsafe
            {
                NativeDeviceContext.Get().CopyResource(source.NativeResource, destination.NativeResource);
            }
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            if (sourceMultisampleTexture == null) throw new ArgumentNullException(nameof(sourceMultisampleTexture));
            if (destTexture == null) throw new ArgumentNullException("destTexture");
            if (!sourceMultisampleTexture.IsMultisample) throw new ArgumentOutOfRangeException(nameof(sourceMultisampleTexture), "Source texture is not a MSAA texture");
            unsafe
            {
                NativeDeviceContext.Get().ResolveSubresource(sourceMultisampleTexture.NativeResource, (uint)sourceSubResource, destTexture.NativeResource, (uint)destSubResource, (Format)(format == PixelFormat.None ? destTexture.Format : format));
            }
        }

        public void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourecRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            if (source == null) throw new ArgumentNullException("source");
            if (destination == null) throw new ArgumentNullException("destination");

            var nullableBox = new Box();

            if (sourecRegion.HasValue)
            {
                var value = sourecRegion.Value;
                nullableBox = new Box((uint)value.Left, (uint)value.Top,(uint) value.Front,(uint) value.Right,(uint) value.Bottom, (uint)value.Back);
            }
            unsafe
            {
                ID3D11Resource* destRes = null;
                destination.NativeShaderResourceView.Get().GetResource(&destRes);
                ID3D11Resource* srcRes = null;
                source.NativeShaderResourceView.Get().GetResource(&srcRes);
                NativeDeviceContext.Get().CopySubresourceRegion(srcRes, (uint)destinationSubResource, (uint)dstX, (uint)dstY, (uint)dstZ, destRes, (uint)sourceSubresource, &nullableBox);

            }
        }

        /// <inheritdoc />
        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetInBytes)
        {
            if (sourceBuffer == null) throw new ArgumentNullException("sourceBuffer");
            if (destBuffer == null) throw new ArgumentNullException("destBuffer");
            unsafe
            {
                NativeDeviceContext.Get().CopyStructureCount(destBuffer.NativeBuffer, (uint)offsetInBytes, sourceBuffer.NativeUnorderedAccessView);
            }
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
            if (resource == null) throw new ArgumentNullException("resource");
            unsafe
            {
                NativeDeviceContext.Get().UpdateSubresource(resource.NativeResource, (uint)subResourceIndex, (Box*)&databox,null,0,0);
            }
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox, ResourceRegion region)
        {
            if (resource == null) throw new ArgumentNullException("resource");
            unsafe
            {
                NativeDeviceContext.Get().UpdateSubresource(resource.NativeResource, (uint)subResourceIndex, (Box*)&databox, (Box*)&region, 0, 0);
            }
            //NativeDeviceContext.UpdateSubresource(*(SharpDX.DataBox*)Interop.Cast(ref databox), resource.NativeResource, subResourceIndex, *(SharpDX.Direct3D11.ResourceRegion*)Interop.Cast(ref region));
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
            MappedSubresource dataBox = new MappedSubresource();
            unsafe
            {
                NativeDeviceContext.Get().Map(resource.NativeResource, (uint)subResourceIndex, (Map)(uint)mapMode, doNotWait ? (uint)MapFlag.MapFlagDONotWait : 0, &dataBox);
                var databox = *(DataBox*)Interop.Cast(ref dataBox);
                
                if (!(databox.DataPointer == IntPtr.Zero && databox.RowPitch == 0 && databox.SlicePitch == 0))
                {
                    databox.DataPointer = (IntPtr)((byte*)databox.DataPointer + offsetInBytes);
                }
                return new MappedResource(resource, subResourceIndex, databox);
            }

        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public void UnmapSubresource(MappedResource unmapped)
        {
            unsafe
            {
                NativeDeviceContext.Get().Unmap(unmapped.Resource.NativeResource, (uint)unmapped.SubResourceIndex);
            }
        }

        private void InitializeStages()
        { 
            unsafe
            {
                ComPtr<ID3D11VertexShader> v = new ComPtr<ID3D11VertexShader>();
                nativeDeviceContext.Get().VSGetShader(&v.Handle, null, null);
                ComPtr<ID3D11PixelShader> p = new ComPtr<ID3D11PixelShader>();
                nativeDeviceContext.Get().PSGetShader(&p.Handle, null, null);
                ComPtr<ID3D11HullShader> h = new ComPtr<ID3D11HullShader>();
                nativeDeviceContext.Get().HSGetShader(&h.Handle, null, null);
                ComPtr<ID3D11DomainShader> d = new ComPtr<ID3D11DomainShader>();
                nativeDeviceContext.Get().DSGetShader(&d.Handle, null, null);
                ComPtr<ID3D11ComputeShader> c= new ComPtr<ID3D11ComputeShader>();
                nativeDeviceContext.Get().CSGetShader(&c.Handle, null, null);
                ComPtr<ID3D11GeometryShader> g = new ComPtr<ID3D11GeometryShader>();
                nativeDeviceContext.Get().GSGetShader(&g.Handle, null, null);

                shaderStages[(int)ShaderStage.Vertex - 1] = new ComPtr<IUnknown>((IUnknown*)v.Handle);
                shaderStages[(int)ShaderStage.Hull - 1] = new ComPtr<IUnknown>((IUnknown*)h.Handle);
                shaderStages[(int)ShaderStage.Domain - 1] = new ComPtr<IUnknown>((IUnknown*)d.Handle);
                shaderStages[(int)ShaderStage.Geometry - 1] = new ComPtr<IUnknown>((IUnknown*)g.Handle);
                shaderStages[(int)ShaderStage.Pixel - 1] = new ComPtr<IUnknown>((IUnknown*)p.Handle);
                shaderStages[(int)ShaderStage.Compute - 1] = new ComPtr<IUnknown>((IUnknown*)c.Handle);

            }
        }
    }
}
 
#endif 
