// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D11

using System;
using System.Diagnostics;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D11;

using Stride.Core.Mathematics;
using Stride.Core.UnsafeExtensions;
using Stride.Shaders;

using SilkBox2I = Silk.NET.Maths.Box2D<int>;
using D3D11Viewport = Silk.NET.Direct3D11.Viewport;
using D3D11Box = Silk.NET.Direct3D11.Box;

using static System.Runtime.CompilerServices.Unsafe;
using static Stride.Graphics.ComPtrHelpers;

namespace Stride.Graphics
{
    public unsafe partial class CommandList
    {
        private const int ConstantBufferCount = D3D11.CommonshaderConstantBufferApiSlotCount; // 14 actually
        private const int SamplerStateCount = D3D11.CommonshaderSamplerSlotCount;
        private const int ShaderResourceViewCount = D3D11.CommonshaderInputResourceSlotCount; // TODO: Unused?
        private const int SimultaneousRenderTargetCount = D3D11.SimultaneousRenderTargetCount;
        private const int UnorderedAcccesViewCount = D3D11.D3D111UavSlotCount;

        private ID3D11DeviceContext* nativeDeviceContext;
        private ID3DUserDefinedAnnotation* nativeDeviceProfiler;

        private readonly ComPtr<ID3D11RenderTargetView>[] currentRenderTargetViews = new ComPtr<ID3D11RenderTargetView>[SimultaneousRenderTargetCount];
        private int currentRenderTargetViewsActiveCount = 0;

        private readonly ComPtr<ID3D11UnorderedAccessView>[] currentUARenderTargetViews = new ComPtr<ID3D11UnorderedAccessView>[SimultaneousRenderTargetCount];
        private readonly ComPtr<ID3D11UnorderedAccessView>[] unorderedAccessViews = new ComPtr<ID3D11UnorderedAccessView>[UnorderedAcccesViewCount]; // Only CS

        private const int StageCount = 6;

        private readonly Buffer[] constantBuffers = new Buffer[StageCount * ConstantBufferCount];
        private readonly SamplerState[] samplerStates = new SamplerState[StageCount * SamplerStateCount];

        private PipelineState currentPipelineState;


        /// <summary>
        ///   Gets the internal Direct3D 11 Device Context.
        /// </summary>
        /// <remarks>
        ///   If the reference is going to be kept, use <see cref="ComPtr{T}.AddRef()"/> to increment the internal
        ///   reference count, and <see cref="ComPtr{T}.Dispose()"/> when no longer needed to release the object.
        /// </remarks>
        internal ComPtr<ID3D11DeviceContext> NativeDeviceContext => ToComPtr(nativeDeviceContext);


        /// <summary>
        ///   Creates a new <see cref="CommandList"/>.
        /// </summary>
        /// <param name="device">The Graphics Device.</param>
        /// <returns>The new instance of <see cref="CommandList"/>.</returns>
        /// <exception cref="InvalidOperationException">Creation of additional Command Lists is not supported for Direct3D 11.</exception>
        public static CommandList New(GraphicsDevice device)
        {
            throw new InvalidOperationException("Creation of additional Command Lists is not supported for Direct3D 11");
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CommandList"/> class.
        /// </summary>
        /// <param name="device">The Graphics Device.</param>
        internal CommandList(GraphicsDevice device) : base(device)
        {
            // We just take ownership of the native device context. No need to call AddRef() on it
            nativeDeviceContext = device.NativeDeviceContext.Handle;
            SetNativeDeviceChild(NativeDeviceContext.AsDeviceChild());

            ComPtr<ID3DUserDefinedAnnotation> deviceProfiler = default;
            if (device.IsDebugMode)
            {
                HResult result = nativeDeviceContext->QueryInterface(out deviceProfiler);
                if (result.IsFailure)
                    deviceProfiler = null;
            }
            nativeDeviceProfiler = deviceProfiler;

            ClearState();
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            // Forget the native device context, as it will be released by the Graphics Device
            UnsetNativeDeviceChild();

            SafeRelease(ref nativeDeviceProfiler);

            base.OnDestroyed(immediately);
        }


        /// <summary>
        ///   Resets a Command List back to its initial state as if a new Command List was just created.
        /// </summary>
        /// <remarks>
        ///   Deferred execution of Command Lists is not supported for Direct3D 11. This method does nothing.
        /// </remarks>
        public unsafe partial void Reset()
        {
        }

        /// <summary>
        ///   Closes and executes the Command List.
        /// </summary>
        public partial void Flush()
        {
        }

        /// <summary>
        ///   Indicates that recording to the Command List has finished.
        /// </summary>
        /// <returns>
        ///   A <see cref="CompiledCommandList"/> representing the frozen list of recorded commands
        ///   that can be executed at a later time.
        /// </returns>
        /// <remarks>
        ///   Compiled Command Lists are not supported for Direct3D 11. This method returns a <see langword="default"/> empty one.
        /// </remarks>
        public partial CompiledCommandList Close()
        {
            return default;
        }

        /// <summary>
        ///   Direct3D 11 implementation that clears and restores the state of the Graphics Device.
        /// </summary>
        private partial void ClearStateImpl()
        {
            if (nativeDeviceContext is not null)
                nativeDeviceContext->ClearState();

            Array.Clear(samplerStates);
            Array.Clear(constantBuffers);

            Array.Clear(unorderedAccessViews);
            Array.Clear(currentRenderTargetViews);
            Array.Clear(currentUARenderTargetViews);

            // Since nothing can be drawn in default state, no need to set anything (another SetPipelineState should happen before)
            currentPipelineState = GraphicsDevice.DefaultPipelineState;
        }

        /// <summary>
        ///   Unbinds the Depth-Stencil Buffer and all the Render Targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl()
        {
            Array.Clear(currentRenderTargetViews);
            Array.Clear(currentUARenderTargetViews);

            // Reset all targets
            nativeDeviceContext->OMSetRenderTargets(NumViews: 0, ppRenderTargetViews: null, pDepthStencilView: null);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and a set of Render Targets to the output-merger stage.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetViews">
        ///   The set of Render Targets to bind.
        ///   Specify an empty collection to unbind the currently bound Render Targets.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> contains too many Render Targets to set.
        /// </exception>
        private partial void SetRenderTargetsImpl(Texture depthStencilView, ReadOnlySpan<Texture> renderTargetViews)
        {
            currentRenderTargetViewsActiveCount = renderTargetViews.Length;

            for (int i = 0; i < renderTargetViews.Length; i++)
                currentRenderTargetViews[i] = renderTargetViews[i].NativeRenderTargetView;

            ref var renderTargetsToSet = ref renderTargetViews.Length > 0
                ? ref currentRenderTargetViews.GetReference()
                : ref NullRef<ComPtr<ID3D11RenderTargetView>>();

            var depthStencilToSet = depthStencilView?.NativeDepthStencilView ?? NullComPtr<ID3D11DepthStencilView>();

            nativeDeviceContext->OMSetRenderTargets(NumViews: (uint) currentRenderTargetViewsActiveCount,
                                                    ref renderTargetsToSet,
                                                    depthStencilToSet);
        }

        /// <summary>
        ///   Direct3D 11 implementation that sets a scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangle">The scissor rectangle to set.</param>
        private unsafe partial void SetScissorRectangleImpl(ref readonly Rectangle scissorRectangle)
        {
            var scissorBox = new SilkBox2I(scissorRectangle.Left, scissorRectangle.Top, scissorRectangle.Right, scissorRectangle.Bottom);

            nativeDeviceContext->RSSetScissorRects(NumRects: 1, in scissorBox);
        }

        /// <summary>
        ///   Direct3D 11 implementation that sets one or more scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        private unsafe partial void SetScissorRectanglesImpl(ReadOnlySpan<Rectangle> scissorRectangles)
        {
            scoped Span<SilkBox2I> scissorRects = stackalloc SilkBox2I[scissorRectangles.Length];
            for (int i = 0; i < scissorRectangles.Length; i++)
                scissorRects[i] = new SilkBox2I(scissorRectangles[i].Left, scissorRectangles[i].Top, scissorRectangles[i].Right, scissorRectangles[i].Bottom);

            nativeDeviceContext->RSSetScissorRects((uint) scissorRectangles.Length, in scissorRects.GetReference());
        }

        /// <summary>
        ///   Sets the stream output Buffers.
        /// </summary>
        /// <param name="buffers">
        ///   The Buffers to set for stream output.
        ///   Specify <see langword="null"/> or an empty array to unset any bound output Buffer.
        /// </param>
        public void SetStreamTargets(params ReadOnlySpan<Buffer> buffers)
        {
            var numBuffers = buffers.Length;

            if (numBuffers > 0)
            {
                scoped Span<ComPtr<ID3D11Buffer>> streamOutputBuffers = stackalloc ComPtr<ID3D11Buffer>[numBuffers];
                scoped Span<uint> streamOutputOffsets = stackalloc uint[numBuffers];

                for (int i = 0; i < numBuffers; ++i)
                {
                    streamOutputBuffers[i] = buffers[i].NativeBuffer.Handle;
                    streamOutputOffsets[i] = 0;
                }
                nativeDeviceContext->SOSetTargets((uint) numBuffers,
                                                  ref streamOutputBuffers.GetReference(),
                                                  in streamOutputOffsets.GetReference());
            }
            else
            {
                nativeDeviceContext->SOSetTargets(NumBuffers: 0, ppSOTargets: null, pOffsets: null);
            }
        }

        /// <summary>
        ///   Sets the viewports to the rasterizer stage.
        /// </summary>
        private unsafe void SetViewportImpl()
        {
            if (!viewportDirty)
                return;

            viewportDirty = false;

            uint viewportCount = renderTargetCount > 0 ? (uint) renderTargetCount : 1;
            var viewportsToSet = viewports.AsSpan<Viewport, D3D11Viewport>();

            nativeDeviceContext->RSSetViewports(viewportCount, in viewportsToSet.GetReference());
        }

        /// <summary>
        ///   Unsets the Render Targets currently bound to the pipeline.
        /// </summary>
        public void UnsetRenderTargets()
        {
            nativeDeviceContext->OMSetRenderTargets(NumViews: 0, ppRenderTargetViews: null, pDepthStencilView: null);
        }

        /// <summary>
        ///   Sets a Constant Buffer to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="buffer">The Constant Buffer to set.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="stage"/> is <see cref="ShaderStage.None"/>. Cannot set a Constant Buffer to an invalid shader stage.
        /// </exception>
        internal void SetConstantBuffer(ShaderStage stage, int slot, Buffer buffer)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            int stageIndex = (int) stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;

            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;

                var nativeBuffer = buffer is not null ? buffer.NativeBuffer : default;

                switch (stage)
                {
                    case ShaderStage.Vertex: nativeDeviceContext->VSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Hull: nativeDeviceContext->HSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Domain: nativeDeviceContext->DSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Geometry: nativeDeviceContext->GSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Pixel: nativeDeviceContext->PSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Compute: nativeDeviceContext->CSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                }
            }
        }

        /// <summary>
        ///   Sets a Constant Buffer to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="buffer">The Constant Buffer to set.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="stage"/> is <see cref="ShaderStage.None"/>. Cannot set a Constant Buffer to an invalid shader stage.
        /// </exception>
        internal void SetConstantBuffer(ShaderStage stage, int slot, int offset, Buffer buffer)
        {
            // TODO: offset param is not used; This method is exactly the same as the one above!

            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            int stageIndex = (int) stage - 1;

            int slotIndex = stageIndex * ConstantBufferCount + slot;

            if (constantBuffers[slotIndex] != buffer)
            {
                constantBuffers[slotIndex] = buffer;

                var nativeBuffer = buffer is not null ? buffer.NativeBuffer : default;

                switch (stage)
                {
                    case ShaderStage.Vertex: nativeDeviceContext->VSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Hull: nativeDeviceContext->HSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Domain: nativeDeviceContext->DSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Geometry: nativeDeviceContext->GSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Pixel: nativeDeviceContext->PSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                    case ShaderStage.Compute: nativeDeviceContext->CSSetConstantBuffers((uint) slot, NumBuffers: 1, ref nativeBuffer); break;
                }
            }
        }

        /// <summary>
        ///   Sets a Sampler State to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="samplerState">The Sampler State to set.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="stage"/> is <see cref="ShaderStage.None"/>. Cannot set a Sampler State to an invalid shader stage.
        /// </exception>
        internal void SetSamplerState(ShaderStage stage, int slot, SamplerState samplerState)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            int stageIndex = (int) stage - 1;

            int slotIndex = stageIndex * SamplerStateCount + slot;

            if (samplerStates[slotIndex] != samplerState)
            {
                samplerStates[slotIndex] = samplerState;

                var nativeSampler = samplerState is not null ? samplerState.NativeSamplerState : default;

                switch (stage)
                {
                    case ShaderStage.Vertex: nativeDeviceContext->VSSetSamplers((uint) slot, NumSamplers: 1, ref nativeSampler); break;
                    case ShaderStage.Hull: nativeDeviceContext->HSSetSamplers((uint) slot, NumSamplers: 1, ref nativeSampler); break;
                    case ShaderStage.Domain: nativeDeviceContext->DSSetSamplers((uint) slot, NumSamplers: 1, ref nativeSampler); break;
                    case ShaderStage.Geometry: nativeDeviceContext->GSSetSamplers((uint) slot, NumSamplers: 1, ref nativeSampler); break;
                    case ShaderStage.Pixel: nativeDeviceContext->PSSetSamplers((uint) slot, NumSamplers: 1, ref nativeSampler); break;
                    case ShaderStage.Compute: nativeDeviceContext->CSSetSamplers((uint) slot, NumSamplers: 1, ref nativeSampler); break;
                }
            }
        }

        /// <summary>
        ///   Sets a Shader Resource View to the shader pipeline.
        /// </summary>
        /// <param name="stage">The shader stage.</param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="shaderResourceView">The Shader Resource View to set.</param>
        /// <exception cref="ArgumentException">
        ///   <paramref name="stage"/> is <see cref="ShaderStage.None"/>. Cannot set a Shader Resource View to an invalid shader stage.
        /// </exception>
        internal void SetShaderResourceView(ShaderStage stage, int slot, GraphicsResource shaderResourceView)
        {
            if (stage == ShaderStage.None)
                throw new ArgumentException($"Cannot use {nameof(ShaderStage)}.{nameof(ShaderStage.None)}", nameof(stage));

            var nativeShaderResourceView = shaderResourceView is not null ? shaderResourceView.NativeShaderResourceView : default;

            switch (stage)
            {
                case ShaderStage.Vertex: nativeDeviceContext->VSSetShaderResources((uint) slot, NumViews: 1, ref nativeShaderResourceView); break;
                case ShaderStage.Hull: nativeDeviceContext->HSSetShaderResources((uint) slot, NumViews: 1, ref nativeShaderResourceView); break;
                case ShaderStage.Domain: nativeDeviceContext->DSSetShaderResources((uint) slot, NumViews: 1, ref nativeShaderResourceView); break;
                case ShaderStage.Geometry: nativeDeviceContext->GSSetShaderResources((uint) slot, NumViews: 1, ref nativeShaderResourceView); break;
                case ShaderStage.Pixel: nativeDeviceContext->PSSetShaderResources((uint) slot, NumViews: 1, ref nativeShaderResourceView); break;
                case ShaderStage.Compute: nativeDeviceContext->CSSetShaderResources((uint) slot, NumViews: 1, ref nativeShaderResourceView); break;
            }
        }

        /// <summary>
        ///   Sets an Unordered Access View to the shader pipeline in the output-merger state
        ///   without affecting ones that are already set, so it can be used as Render Targets in Pixel Shaders.
        /// </summary>
        /// <param name="slot">The binding slot.</param>
        /// <param name="view">The Unordered Access View to set.</param>
        /// <param name="uavInitialOffset">
        ///   The Append/Consume Buffer offset.
        ///   <list type="bullet">
        ///     <item>A value of <c>-1</c> indicates the current offset should be kept.</item>
        ///     <item>
        ///       Any other value sets the hidden counter for that Appendable/Consumable UAV.
        ///       flag, otherwise the argument is ignored.
        ///     </item>
        ///   </list>
        ///   This parameter is only relevant for UAVs which have the <see cref="BufferFlags.StructuredAppendBuffer"/> or
        ///   <see cref="BufferFlags.StructuredCounterBuffer"/> Buffer flags.
        /// </param>
        private void OMSetSingleUnorderedAccessView(int slot, ComPtr<ID3D11UnorderedAccessView> view, int uavInitialOffset)
        {
            currentUARenderTargetViews[slot] = view;

            int remainingSlots = currentUARenderTargetViews.Length - currentRenderTargetViewsActiveCount;

            scoped var uavs = currentUARenderTargetViews.AsSpan(start: currentRenderTargetViewsActiveCount, length: remainingSlots);
            scoped ref var uavsRef = ref uavs.GetReference();

            scoped Span<uint> uavInitialCounts = stackalloc uint[remainingSlots];
            uavInitialCounts.Fill(unchecked((uint)-1));
            uavInitialCounts[slot - currentRenderTargetViewsActiveCount] = (uint) uavInitialOffset;
            scoped ref readonly var uavInitialCountsRef = ref uavInitialCounts.GetReference();

            const uint D3D11_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL = 0xffffffff;

            ref var noRenderTargets = ref NullRef<ComPtr<ID3D11RenderTargetView>>();
            var noDepthStencilView = NullComPtr<ID3D11DepthStencilView>();

            nativeDeviceContext->OMSetRenderTargetsAndUnorderedAccessViews(
                D3D11_KEEP_RENDER_TARGETS_AND_DEPTH_STENCIL, ref noRenderTargets, noDepthStencilView,
                UAVStartSlot: (uint) currentRenderTargetViewsActiveCount, NumUAVs: (uint) remainingSlots,
                ref uavsRef, in uavInitialCountsRef);
        }

        /// <summary>
        ///   Sets an Unordered Access View to the shader pipeline.
        /// </summary>
        /// <param name="stage">
        ///   The shader stage. Only valid options are <see cref="ShaderStage.Compute"/> and <see cref="ShaderStage.Pixel"/>.
        /// </param>
        /// <param name="slot">The binding slot.</param>
        /// <param name="unorderedAccessView">The Unordered Access View to set.</param>
        /// <param name="uavInitialOffset">
        ///   The Append/Consume Buffer offset.
        ///   <list type="bullet">
        ///     <item>A value of <c>-1</c> indicates the current offset should be kept.</item>
        ///     <item>
        ///       Any other value sets the hidden counter for that Appendable/Consumable UAV.
        ///       flag, otherwise the argument is ignored.
        ///     </item>
        ///   </list>
        ///   This parameter is only relevant for UAVs which have the <see cref="BufferFlags.StructuredAppendBuffer"/> or
        ///   <see cref="BufferFlags.StructuredCounterBuffer"/> Buffer flags.
        /// </param>
        /// <exception cref="ArgumentException">
        ///   Invalid <paramref name="stage"/>. Only valid options are <see cref="ShaderStage.Compute"/> and <see cref="ShaderStage.Pixel"/>.
        /// </exception>
        internal void SetUnorderedAccessView(ShaderStage stage, int slot, GraphicsResource unorderedAccessView, int uavInitialOffset)
        {
            if (stage is not ShaderStage.Compute and not ShaderStage.Pixel)
                throw new ArgumentException("Invalid shader stage", nameof(stage));

            var nativeUnorderedAccessView = unorderedAccessView is not null ? unorderedAccessView.NativeUnorderedAccessView : default;

            if (stage == ShaderStage.Compute)
            {
                if (unorderedAccessViews[slot].Handle != nativeUnorderedAccessView.Handle)
                {
                    unorderedAccessViews[slot] = nativeUnorderedAccessView;

                    nativeDeviceContext->CSSetUnorderedAccessViews((uint) slot, NumUAVs: 1, ref nativeUnorderedAccessView, (uint*) &uavInitialOffset);
                }
            }
            else // stage == ShaderStage.Pixel
            {
                if (currentUARenderTargetViews[slot].Handle != nativeUnorderedAccessView.Handle)
                {
                    OMSetSingleUnorderedAccessView(slot, nativeUnorderedAccessView, uavInitialOffset);
                }
            }
        }

        /// <summary>
        ///   Unsets an Unordered Access View from the shader pipeline.
        /// </summary>
        /// <param name="unorderedAccessView">The Unordered Access View to unset.</param>
        internal void UnsetUnorderedAccessView(GraphicsResource unorderedAccessView)
        {
            var nativeUav = unorderedAccessView is not null ? unorderedAccessView.NativeUnorderedAccessView : default;
            if (nativeUav.IsNull())
                return;

            for (int slot = 0; slot < UnorderedAcccesViewCount; slot++)
            {
                if (unorderedAccessViews[slot].Handle == nativeUav.Handle)
                {
                    var nullUav = NullComPtr<ID3D11UnorderedAccessView>();
                    unorderedAccessViews[slot] = nullUav;
                    NativeDeviceContext.CSSetUnorderedAccessViews((uint) slot, NumUAVs: 1, ppUnorderedAccessViews: ref nullUav, pUAVInitialCounts: null);
                }
            }
            for (int slot = 0; slot < SimultaneousRenderTargetCount; slot++)
            {
                if (currentUARenderTargetViews[slot].Handle == nativeUav.Handle)
                {
                    OMSetSingleUnorderedAccessView(slot, NullComPtr<ID3D11UnorderedAccessView>(), uavInitialOffset: -1);
                }
            }
        }

        /// <summary>
        ///   Prepares the Command List for a subsequent draw command.
        /// </summary>
        /// <remarks>
        ///    This method is called before each Draw() method to setup the correct Viewport.
        /// </remarks>
        private void PrepareDraw()
        {
            SetViewportImpl();
        }

        /// <summary>
        ///   Sets the reference value for Depth-Stencil tests.
        /// </summary>
        /// <param name="stencilReference">Reference value to perform against when doing a Depth-Stencil test.</param>
        /// <seealso cref="SetPipelineState(PipelineState)"/>
        public void SetStencilReference(int stencilReference)
        {
            SkipInit(out ComPtr<ID3D11DepthStencilState> depthState);

            nativeDeviceContext->OMGetDepthStencilState(ref depthState, pStencilRef: null); // AddRef() on depthState
            nativeDeviceContext->OMSetDepthStencilState(depthState, (uint) stencilReference);

            SafeRelease(ref depthState);
        }

        /// <summary>
        ///   Sets the blend factors for blending each of the RGBA components.
        /// </summary>
        /// <param name="blendFactor">
        ///   <para>
        ///     A <see cref="Color4"/> representing the blend factors for each RGBA component.
        ///     The blend factors modulate values for the pixel Shader, Render Target, or both.
        ///   </para>
        ///   <para>
        ///     If you have configured the Blend-State object with <see cref="Blend.BlendFactor"/> or <see cref="Blend.InverseBlendFactor"/>,
        ///     the blending stage uses the blend factors specified by <paramref name="blendFactor"/>.
        ///     Otherwise, the blend factors will not be taken into account for the blend stage.
        ///   </para>
        /// </param>
        /// <seealso cref="SetPipelineState(PipelineState)"/>
        public void SetBlendFactor(Color4 blendFactor)
        {
            SkipInit(out uint sampleMask);
            SkipInit(out ComPtr<ID3D11BlendState> blendState);

            scoped Span<float> blendFactorFloats = blendFactor.AsSpan<Color4, float>();

            nativeDeviceContext->OMGetBlendState(ref blendState, BlendFactor: null, ref sampleMask);
            nativeDeviceContext->OMSetBlendState(blendState, ref blendFactorFloats.GetReference(), sampleMask);
        }

        /// <summary>
        ///   Sets the configuration of the graphics pipeline which, among other things, control the shaders, input layout,
        ///   render states, and output settings.
        /// </summary>
        /// <param name="pipelineState">The Pipeline State object to set. Specify <see langword="null"/> to use the default one.</param>
        /// <seealso cref="PipelineState"/>
        public void SetPipelineState(PipelineState pipelineState)
        {
            var newPipelineState = pipelineState ?? GraphicsDevice.DefaultPipelineState;

            if (newPipelineState != currentPipelineState)
            {
                newPipelineState.Apply(this, currentPipelineState);
                currentPipelineState = newPipelineState;
            }
        }

        /// <summary>
        ///   Sets a Vertex Buffer for the input assembler stage of the pipeline.
        /// </summary>
        /// <param name="index">
        ///   The input slot for binding the Vertex Buffer;
        ///   The maximum number of input slots available depends on platform and graphics profile, usually 16 or 32.
        /// </param>
        /// <param name="buffer">
        ///   The Vertex Buffer to set. It must have been created with <see cref="BufferFlags.VertexBuffer"/>.
        /// </param>
        /// <param name="offset">
        ///   The number of bytes between the first element of the Vertex Buffer and the first element that will be used.
        /// </param>
        /// <param name="stride">
        ///   The size (in bytes) of the elements that are to be used from the Vertex Buffer.
        /// </param>
        public void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            var nativeVertexBuffer = buffer?.NativeBuffer ?? default;

            nativeDeviceContext->IASetVertexBuffers((uint) index, NumBuffers: 1, ref nativeVertexBuffer, (uint*) &stride, (uint*) &offset);
        }

        /// <summary>
        ///   Sets the Index Buffer for the input assembler stage of the pipeline.
        /// </summary>
        /// <param name="buffer">
        ///   The Index Buffer to set. It must have been created with <see cref="BufferFlags.IndexBuffer"/>.
        /// </param>
        /// <param name="offset">Offset (in bytes) from the start of the Index Buffer to the first index to use.</param>
        /// <param name="is32bits">
        ///   A value indicating if the Index Buffer elements are 32-bit indices (<see langword="true"/>), or 16-bit (<see langword="false"/>).
        /// </param>
        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            var indexFormat = is32bits ? Format.FormatR32Uint : Format.FormatR16Uint;

            var nativeIndexBuffer = buffer?.NativeBuffer ?? default;

            NativeDeviceContext.IASetIndexBuffer(nativeIndexBuffer, indexFormat, (uint) offset);
        }

        /// <summary>
        ///   Inserts a barrier that transitions a Graphics Resource to a new state, ensuring proper synchronization
        ///   between different GPU operations accessing the resource.
        /// </summary>
        /// <param name="resource">The Graphics Resource to transition to a different state.</param>
        /// <param name="newState">The new state of <paramref name="resource"/>.</param>
        /// <remarks>
        ///   The Direct3D 11 implementation does not have synchronization barriers for Graphics Resource transitions.
        /// </remarks>
        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            // Nothing to do
        }

        /// <summary>
        ///   Binds an array of Descriptor Sets at the specified index in the current pipeline's Root Signature,
        ///   making shader resources available for rendering operations.
        /// </summary>
        /// <param name="index">
        ///   The starting slot where the Descriptor Sets will be bound. This is not used in the Direct3D 11 implementation.
        /// </param>
        /// <param name="descriptorSets">
        ///   An array of Descriptor Sets containing resource bindings (such as Textures, Samplers, and Constant Buffers)
        ///   to be used by the currently active Pipeline State.
        /// </param>
        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
            currentPipelineState?.ResourceBinder.BindResources(this, descriptorSets);
        }

        /// <summary>
        ///   Dispatches a Compute Shader workload with the specified number of thread groups in each dimension.
        /// </summary>
        /// <param name="threadCountX">Number of thread groups in the X dimension.</param>
        /// <param name="threadCountY">Number of thread groups in the Y dimension.</param>
        /// <param name="threadCountZ">Number of thread groups in the Z dimension.</param>
        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
            PrepareDraw(); // TODO: PrepareDraw for Compute dispatch?

            nativeDeviceContext->Dispatch((uint) threadCountX, (uint) threadCountY, (uint) threadCountZ);
        }

        /// <summary>
        ///   Dispatches a Compute Shader workload using an Indirect Buffer, allowing the thread group count to be determined at runtime.
        /// </summary>
        /// <param name="indirectBuffer">
        ///   A Buffer containing the dispatch parameters, structured as <c>(threadCountX, threadCountY, threadCountZ)</c>.
        /// </param>
        /// <param name="offsetInBytes">
        ///   The byte offset within the <paramref name="indirectBuffer"/> where the dispatch parameters are located.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="indirectBuffer"/> is <see langword="null"/>.</exception>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            ArgumentNullException.ThrowIfNull(indirectBuffer);

            PrepareDraw(); // TODO: PrepareDraw for Compute dispatch?

            nativeDeviceContext->DispatchIndirect(indirectBuffer.NativeBuffer, (uint) offsetInBytes);
        }

        /// <summary>
        ///   Issues a non-indexed draw call, rendering a sequence of vertices directly from the bound Vertex Buffer.
        /// </summary>
        /// <param name="vertexCount">The number of vertices to draw.</param>
        /// <param name="startVertexLocation">
        ///   Index of the first vertex in the Vertex Buffer to begin rendering from;
        ///   it could also be used as the first vertex id generated for a shader parameter marked with the <c>SV_TargetId</c> system-value semantic.
        /// </param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->Draw((uint) vertexCount, (uint) startVertexLocation);

            GraphicsDevice.FrameTriangleCount += (uint) vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        ///   Issues a draw call for geometry of unknown size, typically used with Vertex or Index Buffers populated via Stream Output.
        ///   The vertex count is inferred from the data written by the GPU.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            nativeDeviceContext->DrawAuto();

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        ///   Issues an indexed non-instanced draw call using the currently bound Index Buffer.
        /// </summary>
        /// <param name="indexCount">Number of indices to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the Index Buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the Vertex Buffer.</param>
        public void DrawIndexed(int indexCount, int startIndexLocation = 0, int baseVertexLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->DrawIndexed((uint) indexCount, (uint) startIndexLocation, baseVertexLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint) indexCount;
        }

        /// <summary>
        ///   Issues an indexed instanced draw call, rendering multiple instances of indexed geometry.
        /// </summary>
        /// <param name="indexCountPerInstance">Number of indices read from the Index Buffer for each instance.</param>
        /// <param name="instanceCount">Number of instances to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the Index Buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the Vertex Buffer.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a Vertex Buffer.</param>
        public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation = 0, int baseVertexLocation = 0, int startInstanceLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->DrawIndexedInstanced((uint) indexCountPerInstance, (uint) instanceCount, (uint) startIndexLocation, baseVertexLocation, (uint) startInstanceLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint) (indexCountPerInstance * instanceCount);
        }

        /// <summary>
        ///   Issues an indexed instanced draw call using an Indirect Arguments Buffer, allowing parameters to be determined at runtime.
        /// </summary>
        /// <param name="argumentsBuffer">A Buffer containing the draw parameters (index count, instance count, etc.).</param>
        /// <param name="alignedByteOffsetForArgs">
        ///   Byte offset within the <paramref name="argumentsBuffer"/> where the draw arguments are located.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="argumentsBuffer"/> is <see langword="null"/>.</exception>
        public void DrawIndexedInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            ArgumentNullException.ThrowIfNull(argumentsBuffer);

            PrepareDraw();

            nativeDeviceContext->DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, (uint) alignedByteOffsetForArgs);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        ///   Issues a non-indexed instanced draw call, rendering multiple instances of geometry using a Vertex Buffer.
        /// </summary>
        /// <param name="vertexCountPerInstance">The number of vertices to draw per instance.</param>
        /// <param name="instanceCount">The number of instances to draw.</param>
        /// <param name="startVertexLocation">The index of the first vertex in the Vertex Buffer.</param>
        /// <param name="startInstanceLocation">A value added to each index before reading per-instance data from a Vertex Buffer.</param>
        public void DrawInstanced(int vertexCountPerInstance, int instanceCount, int startVertexLocation = 0, int startInstanceLocation = 0)
        {
            PrepareDraw();

            nativeDeviceContext->DrawInstanced((uint) vertexCountPerInstance, (uint) instanceCount, (uint) startVertexLocation, (uint) startInstanceLocation);

            GraphicsDevice.FrameDrawCalls++;
            GraphicsDevice.FrameTriangleCount += (uint) (vertexCountPerInstance * instanceCount);
        }

        /// <summary>
        ///   Issues a non-indexed instanced draw call using an Indirect Arguments Buffer, allowing parameters to be determined at runtime.
        /// </summary>
        /// <param name="argumentsBuffer">A Buffer containing the draw parameters (vertex count, instance count, etc.).</param>
        /// <param name="alignedByteOffsetForArgs">
        ///   Byte offset within the <paramref name="argumentsBuffer"/> where the draw arguments are located.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="argumentsBuffer"/> is <see langword="null"/>.</exception>
        public void DrawInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            ArgumentNullException.ThrowIfNull(argumentsBuffer);

            PrepareDraw();

            nativeDeviceContext->DrawInstancedIndirect(argumentsBuffer.NativeBuffer, (uint) alignedByteOffsetForArgs);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        ///   Submits a GPU timestamp Query.
        /// </summary>
        /// <param name="queryPool">The <see cref="QueryPool"/> owning the Query.</param>
        /// <param name="index">The index of the Query to write.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            var query = queryPool.NativeQueries[index];

            nativeDeviceContext->End(query);
        }

        /// <summary>
        ///   Marks the beginning of a profile section.
        /// </summary>
        /// <param name="profileColor">A color that a profiling tool can use to display the event.</param>
        /// <param name="name">A descriptive name for the profile section.</param>
        /// <remarks>
        ///   <para>
        ///     Each call to <see cref="BeginProfile"/> must be matched with a corresponding call to <see cref="EndProfile"/>,
        ///     which defines a region of code that can be identified with a name and possibly a color in a compatible
        ///     profiling tool.
        ///   </para>
        ///   <para>
        ///     A pair of calls to <see cref="BeginProfile"/> and <see cref="EndProfile"/> can be nested inside a call
        ///     to <see cref="BeginProfile"/> and <see cref="EndProfile"/> in an upper level in the call stack.
        ///     This allows to form a hierarchy of profile sections.
        ///   </para>
        /// </remarks>
        public void BeginProfile(Color4 profileColor, string name)
        {
            // TODO: We only initialize `nativeDeviceProfiler` in debug mode. Should BeginProfile() check this?
            if (IsDebugMode)
                nativeDeviceProfiler->BeginEvent(name);
        }

        /// <summary>
        ///   Marks the end of a profile section previously started by a call to <see cref="BeginProfile"/>.
        /// </summary>
        /// <inheritdoc cref="BeginProfile(Color4, string)" path="/remarks"/>
        public void EndProfile()
        {
            // TODO: We only initialize `nativeDeviceProfiler` in debug mode. Should BeginProfile() check this?
            if (IsDebugMode)
                nativeDeviceProfiler->EndEvent();
        }

        /// <summary>
        ///   Clears the specified Depth-Stencil Buffer.
        /// </summary>
        /// <param name="depthStencilBuffer">The Depth-Stencil Buffer to clear.</param>
        /// <param name="options">
        ///   A combination of <see cref="DepthStencilClearOptions"/> flags identifying what parts of the Depth-Stencil Buffer to clear.
        /// </param>
        /// <param name="depth">The depth value to use for clearing the Depth Buffer.</param>
        /// <param name="stencil">The stencil value to use for clearing the Stencil Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="depthStencilBuffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">Cannot clear a Stencil Buffer without a Stencil Buffer format.</exception>
        public void Clear(Texture depthStencilBuffer, DepthStencilClearOptions options, float depth = 1, byte stencil = 0)
        {
            ArgumentNullException.ThrowIfNull(depthStencilBuffer);

            var flags = options.HasFlag(DepthStencilClearOptions.DepthBuffer) ? ClearFlag.Depth : 0;

            // Check that the Depth-Stencil Buffer has a Stencil if Clear Stencil is requested
            if (options.HasFlag(DepthStencilClearOptions.Stencil))
            {
                if (!depthStencilBuffer.HasStencil)
                    throw new InvalidOperationException(string.Format(FrameworkResources.NoStencilBufferForDepthFormat, depthStencilBuffer.ViewFormat));

                flags |= ClearFlag.Stencil;
            }

            nativeDeviceContext->ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView, (uint) flags, depth, stencil);
        }

        /// <summary>
        ///   Clears the specified Render Target.
        /// </summary>
        /// <param name="renderTarget">The Render Target to clear.</param>
        /// <param name="color">The color to use to clear the Render Target.</param>
        /// <exception cref="ArgumentNullException"><paramref name="renderTarget"/> is <see langword="null"/>.</exception>
        public void Clear(Texture renderTarget, Color4 color)
        {
            ArgumentNullException.ThrowIfNull(renderTarget);

            scoped Span<float> colorFloats = color.AsSpan<Color4, float>(elementCount: 4);
            nativeDeviceContext->ClearRenderTargetView(renderTarget.NativeRenderTargetView, ref colorFloats.GetReference());
        }

        /// <summary>
        ///   Clears a Read-Write Buffer.
        /// </summary>
        /// <param name="buffer">The Buffer to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> must support Unordered Access.</exception>
        public unsafe void ClearReadWrite(Buffer buffer, Vector4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.NativeUnorderedAccessView.IsNull())
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            nativeDeviceContext->ClearUnorderedAccessViewFloat(buffer.NativeUnorderedAccessView, (float*) &value);
        }

        /// <summary>
        ///   Clears a Read-Write Buffer.
        /// </summary>
        /// <param name="buffer">The Buffer to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> must support Unordered Access.</exception>
        public unsafe void ClearReadWrite(Buffer buffer, Int4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.NativeUnorderedAccessView.IsNull())
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            nativeDeviceContext->ClearUnorderedAccessViewUint(buffer.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        ///   Clears a Read-Write Buffer.
        /// </summary>
        /// <param name="buffer">The Buffer to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> must support Unordered Access.</exception>
        public unsafe void ClearReadWrite(Buffer buffer, UInt4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.NativeUnorderedAccessView.IsNull())
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            nativeDeviceContext->ClearUnorderedAccessViewUint(buffer.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        ///   Clears a Read-Write Texture.
        /// </summary>
        /// <param name="texture">The Texture to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Texture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="texture"/> must support Unordered Access.</exception>
        public unsafe void ClearReadWrite(Texture texture, Vector4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);

            if (texture.NativeUnorderedAccessView.IsNull())
                throw new ArgumentException("Expecting a Texture supporting UAV", nameof(texture));

            nativeDeviceContext->ClearUnorderedAccessViewFloat(texture.NativeUnorderedAccessView, (float*) &value);
        }

        /// <summary>
        ///   Clears a Read-Write Texture.
        /// </summary>
        /// <param name="texture">The Texture to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Texture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="texture"/> must support Unordered Access.</exception>
        public unsafe void ClearReadWrite(Texture texture, Int4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);

            if (texture.NativeUnorderedAccessView.IsNull())
                throw new ArgumentException("Expecting a Texture supporting UAV", nameof(texture));

            nativeDeviceContext->ClearUnorderedAccessViewUint(texture.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        ///   Clears a Read-Write Texture.
        /// </summary>
        /// <param name="texture">The Texture to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Texture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="texture"/> must support Unordered Access.</exception>
        public unsafe void ClearReadWrite(Texture texture, UInt4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);

            if (texture.NativeUnorderedAccessView.IsNull())
                throw new ArgumentException("Expecting a Texture supporting UAV", nameof(texture));

            nativeDeviceContext->ClearUnorderedAccessViewUint(texture.NativeUnorderedAccessView, (uint*) &value);
        }

        /// <summary>
        ///   Copies the data from a Graphics Resource to another.
        ///   Views are ignored and the full underlying data is copied.
        /// </summary>
        /// <param name="source">The source Graphics Resource.</param>
        /// <param name="destination">The destination Graphics Resource.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            nativeDeviceContext->CopyResource(destination.NativeResource, source.NativeResource);
        }

        /// <summary>
        ///   Copies the data from a multi-sampled Texture (which is resolved) to another Texture.
        /// </summary>
        /// <param name="sourceMultiSampledTexture">The source multi-sampled Texture.</param>
        /// <param name="sourceSubResourceIndex">The sub-resource index of the source Texture.</param>
        /// <param name="destinationTexture">The destination Texture.</param>
        /// <param name="destinationSubResourceIndex">The sub-resource index of the destination Texture.</param>
        /// <param name="format">
        ///   A <see cref="PixelFormat"/> that indicates how the multi-sampled Texture will be resolved to a single-sampled resource.
        /// </param>
        /// <exception cref="ArgumentException"><paramref name="sourceMultiSampledTexture"/> is not a multi-sampled Texture.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="sourceMultiSampledTexture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationTexture"/> is <see langword="null"/>.</exception>
        /// <remarks>
        ///   The <paramref name="sourceMultiSampledTexture"/> and <paramref name="destinationTexture"/> must have the same dimensions.
        ///   In addition, they must have compatible formats. There are three scenarios for this:
        ///   <list type="table">
        ///    <listheader>
        ///      <term>Scenario</term>
        ///      <description>Requirements</description>
        ///    </listheader>
        ///    <item>
        ///      <term>Source and destination are prestructured and typed</term>
        ///      <description>
        ///        Both the source and destination must have identical formats and that format must be specified in the <paramref name="format"/> parameter.
        ///      </description>
        ///    </item>
        ///    <item>
        ///      <term>One resource is prestructured and typed and the other is prestructured and typeless</term>
        ///      <description>
        ///        The typed resource must have a format that is compatible with the typeless resource (i.e. the typed resource is <see cref="PixelFormat.R32_Float"/>
        ///        and the typeless resource is <see cref="PixelFormat.R32_Typeless"/>).
        ///        The format of the typed resource must be specified in the <paramref name="format"/> parameter.
        ///      </description>
        ///    </item>
        ///    <item>
        ///      <term>Source and destination are prestructured and typeless</term>
        ///      <description>
        ///        <para>
        ///          Both the source and destination must have the same typeless format (i.e. both must have <see cref="PixelFormat.R32_Typeless"/>), and the
        ///          <paramref name="format"/> parameter must specify a format that is compatible with the source and destination
        ///          (i.e. if both are <see cref="PixelFormat.R32_Typeless"/> then <see cref="PixelFormat.R32_Float"/> could be specified in the
        ///          <paramref name="format"/> parameter).
        ///        </para>
        ///        <para>
        ///          For example, given the <see cref="PixelFormat.R16G16B16A16_Typeless"/> format:
        ///          <list type="bullet">
        ///            <item>The source (or destination) format could be <see cref="PixelFormat.R16G16B16A16_UNorm"/>.</item>
        ///            <item>The destination (or source) format could be <see cref="PixelFormat.R16G16B16A16_Float"/>.</item>
        ///          </list>
        ///        </para>
        ///      </description>
        ///    </item>
        ///   </list>
        /// </remarks>
        public void CopyMultisample(Texture sourceMultiSampledTexture, int sourceSubResourceIndex,
                                     Texture destinationTexture, int destinationSubResourceIndex,
                                     PixelFormat format = PixelFormat.None)
        {
            ArgumentNullException.ThrowIfNull(sourceMultiSampledTexture);
            ArgumentNullException.ThrowIfNull(destinationTexture);

            if (!sourceMultiSampledTexture.IsMultiSampled)
                throw new ArgumentException("Source Texture is not a MSAA Texture", nameof(sourceMultiSampledTexture));

            nativeDeviceContext->ResolveSubresource(destinationTexture.NativeResource, (uint) destinationSubResourceIndex,
                                                    sourceMultiSampledTexture.NativeResource, (uint) sourceSubResourceIndex,
                                                    (Format)(format == PixelFormat.None ? destinationTexture.Format : format));
        }

        /// <summary>
        ///   Copies a region from a source Graphics Resource to a destination Graphics Resource.
        /// </summary>
        /// <param name="source">The source Graphics Resource to copy from.</param>
        /// <param name="sourceSubResourceIndex">The index of the sub-resource of <paramref name="source"/> to copy from.</param>
        /// <param name="sourceRegion">
        ///   <para>
        ///     An optional <see cref="ResourceRegion"/> that defines the source sub-resource to copy from.
        ///     Specify <see langword="null"/> the entire source sub-resource is copied.
        ///   </para>
        ///   <para>
        ///     An empty region makes this method to not perform a copy operation.
        ///     It is considered empty if the top value is greater than or equal to the bottom value,
        ///     or the left value is greater than or equal to the right value, or the front value is greater than or equal to the back value.
        ///   </para>
        /// </param>
        /// <param name="destination">The destination Graphics Resource to copy to.</param>
        /// <param name="destinationSubResourceIndex">The index of the sub-resource of <paramref name="destination"/> to copy to.</param>
        /// <param name="dstX">The X-coordinate of the upper left corner of the destination region.</param>
        /// <param name="dstY">The Y-coordinate of the upper left corner of the destination region. For a 1D sub-resource, this must be zero.</param>
        /// <param name="dstZ">The Z-coordinate of the upper left corner of the destination region. For a 1D or 2D sub-resource, this must be zero.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
        /// <remarks>
        ///   <para>
        ///     The <paramref name="sourceRegion"/> must be within the size of the source resource.
        ///     The destination offsets, (<paramref name="dstX"/>, <paramref name="dstY"/>, and <paramref name="dstZ"/>),
        ///     allow the source region to be offset when writing into the destination resource;
        ///     however, the dimensions of the source region and the offsets must be within the size of the resource.
        ///   </para>
        ///   <para>
        ///     If the resources are Buffers, all coordinates are in bytes; if the resources are Textures, all coordinates are in texels.
        ///   </para>
        ///   <para>
        ///     <see cref="CopyRegion"/> performs the copy on the GPU (similar to a <c>memcpy</c> by the CPU). As a consequence,
        ///     the source and destination resources:
        ///     <list type="bullet">
        ///       <item>Must be different sub-resources (although they can be from the same Graphics Resource).</item>
        ///       <item>Must be the same type.</item>
        ///       <item>
        ///         Must have compatible formats (identical or from the same type group). For example, a <see cref="PixelFormat.R32G32B32_Float"/> Texture
        ///         can be copied to a <see cref="PixelFormat.R32G32B32_UInt"/> Texture since both of these formats are in the
        ///         <see cref="PixelFormat.R32G32B32_Typeless"/> group.
        ///       </item>
        ///       <item>May not be currently mapped with <see cref="MapSubResource(GraphicsResource, int, MapMode, bool, int, int)"/>.</item>
        ///     </list>
        ///   </para>
        ///   <para>
        ///     <see cref="CopyRegion"/> only supports copy; it doesn't support any stretch, color key, or blend.
        ///   </para>
        /// </remarks>
        public void CopyRegion(GraphicsResource source, int sourceSubResourceIndex, ResourceRegion? sourceRegion,
                               GraphicsResource destination, int destinationSubResourceIndex, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            if (sourceRegion is ResourceRegion srcResourceRegion)
            {
                // NOTE: We assume the same layout and size as D3D11_BOX
                Debug.Assert(sizeof(D3D11Box) == sizeof(ResourceRegion));
                var sourceBox = srcResourceRegion.BitCast<ResourceRegion, D3D11Box>();

                nativeDeviceContext->CopySubresourceRegion(destination.NativeResource, (uint) destinationSubResourceIndex, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                           source.NativeResource, (uint) sourceSubResourceIndex, in sourceBox);
            }
            else
            {
                nativeDeviceContext->CopySubresourceRegion(destination.NativeResource, (uint) destinationSubResourceIndex, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                           source.NativeResource, (uint) sourceSubResourceIndex, pSrcBox: null);
            }
        }

        /// <summary>
        ///   Copies data from a Buffer holding variable length data to another Buffer.
        /// </summary>
        /// <param name="sourceBuffer">
        ///   A Structured Buffer created with either <see cref="BufferFlags.StructuredAppendBuffer"/> or <see cref="BufferFlags.StructuredCounterBuffer"/>.
        ///   These types of resources have hidden counters tracking "how many" records have been written.
        /// </param>
        /// <param name="destinationBuffer">
        ///   A Buffer resource to copy the data to. Any Buffer that other copy commands, such as <see cref="Copy"/> or <see cref="CopyRegion"/>, are able to write to.
        /// </param>
        /// <param name="destinationOffsetInBytes">
        ///   The offset in bytes from the start of <paramref name="destinationBuffer"/> to write 32-bit <see langword="uint"/>
        ///   structure (vertex) count from <paramref name="sourceBuffer"/>.
        ///   The offset must be aligned to 4 bytes.
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="sourceBuffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destinationBuffer"/> is <see langword="null"/>.</exception>
        public void CopyCount(Buffer sourceBuffer, Buffer destinationBuffer, int destinationOffsetInBytes)
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destinationBuffer);

            nativeDeviceContext->CopyStructureCount(destinationBuffer.NativeBuffer, (uint) destinationOffsetInBytes, sourceBuffer.NativeUnorderedAccessView);
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <remarks>
        ///   <para>
        ///     If <paramref name="resource"/> is a Constant Buffer, it must be updated in full.
        ///     It is not possible to use this method to partially update a Constant Buffer.
        ///   </para>
        ///   <para>
        ///     A Graphics Resource cannot be used as a destination if:
        ///     <list type="bullet">
        ///       <item>The resource was created with <see cref="GraphicsResourceUsage.Immutable"/> or <see cref="GraphicsResourceUsage.Dynamic"/>.</item>
        ///       <item>The resource was created as a Depth-Stencil Buffer.</item>
        ///       <item>The resource is a Texture created with multi-sampling capability (see <see cref="TextureDescription.MultisampleCount"/>).</item>
        ///     </list>
        ///   </para>
        ///   <para>
        ///     When <see cref="UpdateSubResource"/> returns, the application is free to change or even free the data pointed to by
        ///     <paramref name="sourceData"/> because the method has already copied/snapped away the original contents.
        ///   </para>
        /// </remarks>
        internal unsafe void UpdateSubResource(GraphicsResource resource, int subResourceIndex, ReadOnlySpan<byte> sourceData)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (sourceData.IsEmpty)
                return;

            nativeDeviceContext->UpdateSubresource(resource.NativeResource, (uint)subResourceIndex, pDstBox: null,
                                                   sourceData.GetPointer(), SrcRowPitch: 0, SrcDepthPitch: 0);
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte})" path="/remarks" />
        internal unsafe void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData)
        {
            ArgumentNullException.ThrowIfNull(resource);

            nativeDeviceContext->UpdateSubresource(resource.NativeResource, (uint) subResourceIndex, pDstBox: null,
                                                   pSrcData: (void*) sourceData.DataPointer, (uint) sourceData.RowPitch, (uint) sourceData.SlicePitch);
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        /// <param name="region">
        ///   <para>
        ///     A <see cref="ResourceRegion"/> that defines the portion of the destination sub-resource to copy the resource data into.
        ///     Coordinates are in bytes for Buffers and in texels for Textures.
        ///     The dimensions of the source must fit the destination.
        ///   </para>
        ///   <para>
        ///     An empty region makes this method to not perform a copy operation.
        ///     It is considered empty if the top value is greater than or equal to the bottom value,
        ///     or the left value is greater than or equal to the right value, or the front value is greater than or equal to the back value.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte})" path="/remarks" />
        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, ReadOnlySpan<byte> sourceData, ResourceRegion region)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (sourceData.IsEmpty)
                return;

            ref Box destBox = ref region.As<ResourceRegion, Box>();

            nativeDeviceContext->UpdateSubresource(resource.NativeResource, (uint) subResourceIndex, in destBox,
                                                   sourceData.GetPointer(), SrcRowPitch: 0, SrcDepthPitch: 0);
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        /// <param name="region">
        ///   <para>
        ///     A <see cref="ResourceRegion"/> that defines the portion of the destination sub-resource to copy the resource data into.
        ///     Coordinates are in bytes for Buffers and in texels for Textures.
        ///     The dimensions of the source must fit the destination.
        ///   </para>
        ///   <para>
        ///     An empty region makes this method to not perform a copy operation.
        ///     It is considered empty if the top value is greater than or equal to the bottom value,
        ///     or the left value is greater than or equal to the right value, or the front value is greater than or equal to the back value.
        ///   </para>
        /// </param>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte})" path="/remarks" />
        internal unsafe partial void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData, ResourceRegion region)
        {
            ArgumentNullException.ThrowIfNull(resource);

            ref Box destBox = ref region.As<ResourceRegion, Box>();

            nativeDeviceContext->UpdateSubresource(resource.NativeResource, (uint) subResourceIndex, in destBox,
                                                   pSrcData: (void*) sourceData.DataPointer, (uint) sourceData.RowPitch, (uint) sourceData.SlicePitch);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        /// <summary>
        ///   Maps a sub-resource of a Graphics Resource to be accessible from CPU memory, and in the process denies the GPU access to that sub-resource.
        /// </summary>
        /// <param name="resource">The Graphics Resource to map to CPU memory.</param>
        /// <param name="subResourceIndex">The index of the sub-resource to get access to.</param>
        /// <param name="mapMode">A value of <see cref="MapMode"/> indicating the way the Graphics Resource should be mapped to CPU memory.</param>
        /// <param name="doNotWait">
        ///   A value indicating if this method will return immediately if the Graphics Resource is still being used by the GPU for writing
        ///   <see langword="true"/>. The default value is <see langword="false"/>, which means the method will wait until the GPU is done.
        /// </param>
        /// <param name="offsetInBytes">
        ///   The offset in bytes from the beginning of the mapped memory of the sub-resource.
        ///   Defaults to 0, which means it is mapped from the beginning.
        /// </param>
        /// <param name="lengthInBytes">
        ///   The length in bytes of the memory to map from the sub-resource.
        ///   Defaults to 0, which means the entire sub-resource is mapped.
        /// </param>
        /// <returns>A <see cref="MappedResource"/> structure pointing to the GPU resource mapped for CPU access.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <remarks>
        ///   For <see cref="Buffer"/>s:
        ///   <para>
        ///     Usage Instructions:
        ///     <list type="bullet">
        ///       <item>
        ///         Ensure the <paramref name="resource"/> was created with the correct usage.
        ///         For example, you should specify <see cref="GraphicsResourceUsage.Dynamic"/> if you plan to update its contents frequently.
        ///       </item>
        ///       <item>This method can be called multiple times, and nested calls are supported.</item>
        ///       <item>
        ///         Use appropriate <see cref="MapMode"/> values when calling <see cref="MapSubResource"/>.
        ///         For example, <see cref="MapMode.WriteDiscard"/> indicates that the old data in the Buffer can be discarded.
        ///       </item>
        ///     </list>
        ///   </para>
        ///   <para>
        ///     Restrictions:
        ///     <list type="bullet">
        ///       <item>
        ///         The <see cref="MappedResource"/> returned by <see cref="MapSubResource"/> is not guaranteed to be consistent across different calls.
        ///         Applications should not rely on the address being the same unless <see cref="MapSubResource"/> is persistently nested.
        ///       </item>
        ///       <item><see cref="MapSubResource"/> may invalidate the CPU cache to ensure that CPU reads reflect any modifications made by the GPU.</item>
        ///       <item>If your graphics API supports them, use fences for synchronization to ensure proper coordination between the CPU and GPU.</item>
        ///       <item>Ensure that the Buffer data is properly aligned to meet the requirements of your graphics API.</item>
        ///       <item>
        ///         Stick to simple usage models (e.g., <see cref="GraphicsResourceUsage.Dynamic"/> for <strong>upload</strong>, <see cref="GraphicsResourceUsage.Default"/>,
        ///         <see cref="GraphicsResourceUsage.Staging"/> for <strong>readback</strong>) unless advanced models are necessary for your application.
        ///       </item>
        ///     </list>
        ///   </para>
        ///
        ///   For <see cref="Texture"/>s:
        ///   <para>
        ///     Usage Instructions:
        ///     <list type="bullet">
        ///       <item>
        ///         Ensure to use the correct data format when writing data to the Texture.
        ///       </item>
        ///       <item>Textures can have multiple mipmap levels. You must specify which level you want to map with <paramref name="subResourceIndex"/>.</item>
        ///       <item>
        ///         Use appropriate <see cref="MapMode"/> values when calling <see cref="MapSubResource"/>.
        ///         For example, <see cref="MapMode.WriteDiscard"/> is usually used to update dynamic Textures.
        ///       </item>
        ///     </list>
        ///   </para>
        ///   <para>
        ///     Restrictions:
        ///     <list type="bullet">
        ///       <item>
        ///         Not all <see cref="PixelFormat"/>s are compatible with mapping operations.
        ///       </item>
        ///       <item>Concurrent access to a Texture both from the CPU and the GPU may not be allowed and might require careful synchronization.</item>
        ///       <item>Ensure that the Texture data is properly aligned to meet the requirements of your graphics API and the <see cref="Texture.Format"/>.</item>
        ///     </list>
        ///   </para>
        ///
        ///   For <strong>State Objects</strong> (like <see cref="PipelineState"/>, <see cref="SamplerState"/>, etc):
        ///   <para>
        ///     Restrictions:
        ///     <list type="bullet">
        ///       <item>
        ///         State Objects are not usually mapped nor directly updated. They are created with specific configurations and are treated
        ///         as immutable from now on. Instead, if you need changes, you can create a new State Object with the updated settings.
        ///       </item>
        ///     </list>
        ///   </para>
        ///
        ///   After updating the <paramref name="resource"/>, call <see cref="UnmapSubResource"/> to release the CPU pointer and allow the GPU to access the updated data.
        /// </remarks>
        public unsafe partial MappedResource MapSubResource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            // TODO: D3D 11 does not support lengthInBytes, we should throw an exception if it is not 0?
            // TODO: Also, even if not used, as we are returning it in MappedResource, shouldn't we compute it the same as in D3D 12?

            ArgumentNullException.ThrowIfNull(resource);

            // This resource has just been recycled by the GraphicsResourceAllocator, we force a rename to avoid GPU => GPU sync point
            if (resource.DiscardNextMap && mapMode == MapMode.WriteNoOverwrite)
                mapMode = MapMode.WriteDiscard;

            var mapType = (Map) mapMode;
            var mapFlags = (uint) (doNotWait ? MapFlag.DONotWait : 0);

            MappedResource mappedResource = new(resource, subResourceIndex, dataBox: default);
            ref var mappedSubresource = ref UnsafeUtilities.AsRef<DataBox, MappedSubresource>(in mappedResource.DataBox);

            HResult result = nativeDeviceContext->Map(resource.NativeResource, (uint) subResourceIndex, mapType, mapFlags, ref mappedSubresource);

            if (doNotWait && result == DxgiConstants.ErrorWasStillDrawing)
                return default;

            if (result.IsFailure)
                result.Throw();

            if (!mappedResource.DataBox.IsEmpty)
            {
                mappedSubresource.PData = (byte*) mappedSubresource.PData + offsetInBytes;
            }
            return mappedResource;
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        /// <summary>
        ///   Unmaps a sub-resource of a Graphics Resource, which was previously mapped to CPU memory with <see cref="MapSubResource"/>,
        ///   and in the process re-enables the GPU access to that sub-resource.
        /// </summary>
        /// <param name="mappedResource">
        ///   A <see cref="MappedResource"/> structure identifying the sub-resource to unmap.
        /// </param>
        public unsafe partial void UnmapSubResource(MappedResource mappedResource)
        {
            nativeDeviceContext->Unmap(mappedResource.Resource.NativeResource, (uint) mappedResource.SubResourceIndex);
        }
    }
}

#endif
