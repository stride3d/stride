// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Core.UnsafeExtensions;

using D3D12Box = Silk.NET.Direct3D12.Box;
using D3D12Range = Silk.NET.Direct3D12.Range;
using D3D12Viewport = Silk.NET.Direct3D12.Viewport;
using SilkBox2I = Silk.NET.Maths.Box2D<int>;

using static System.Runtime.CompilerServices.Unsafe;

namespace Stride.Graphics
{
    public unsafe partial class CommandList
    {
        // Descriptor heap for Shader Resource Views (SRV), Constant Buffers (CBV), and Unordered Access Views (UAV)
        private DescriptorHeapWrapper srvHeap;
        private int srvHeapOffset = GraphicsDevice.SrvHeapSize;

        // Descriptor heap for Samplers
        private DescriptorHeapWrapper samplerHeap;
        private int samplerHeapOffset = GraphicsDevice.SamplerHeapSize;

        private PipelineState boundPipelineState;
        private readonly ID3D12DescriptorHeap*[] descriptorHeaps = new ID3D12DescriptorHeap*[2];
        private readonly List<ResourceBarrier> resourceBarriers = new(16);

        // Mappings from CPU-side Descriptor Handles to GPU-side Descriptor Handles
        private readonly Dictionary<nuint, GpuDescriptorHandle> srvMapping = [];
        private readonly Dictionary<nuint, GpuDescriptorHandle> samplerMapping = [];

        internal readonly Queue<ComPtr<ID3D12GraphicsCommandList>> NativeCommandLists = new();

        private CompiledCommandList currentCommandList;

        private bool IsComputePipelineStateBound => boundPipelineState?.IsCompute is true;


        /// <summary>
        ///   Creates a new <see cref="CommandList"/>.
        /// </summary>
        /// <param name="device">The Graphics Device.</param>
        /// <returns>The new instance of <see cref="CommandList"/>.</returns>
        public static CommandList New(GraphicsDevice device)
        {
            return new CommandList(device);
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="CommandList"/> class.
        /// </summary>
        /// <param name="device">The Graphics Device.</param>
        private CommandList(GraphicsDevice device) : base(device)
        {
            Reset();
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            // Recycle heaps
            ResetSrvHeap(createNewHeap: false);
            ResetSamplerHeap(createNewHeap: false);

            // Available right now (NextFenceValue - 1)
            // TODO: Note that it won't be available right away because CommandAllocators is currently not using a PriorityQueue but a simple Queue
            if (currentCommandList.NativeCommandAllocator.IsNotNull())
            {
                GraphicsDevice.CommandAllocators.RecycleObject(GraphicsDevice.CommandListFence.NextFenceValue - 1, currentCommandList.NativeCommandAllocator);
                currentCommandList.NativeCommandAllocator = null;
            }

            if (currentCommandList.NativeCommandList.IsNotNull())
            {
                NativeCommandLists.Enqueue(currentCommandList.NativeCommandList);
                currentCommandList.NativeCommandList = null;
            }

            while (NativeCommandLists.Count > 0)
            {
                var commandList = NativeCommandLists.Dequeue();
                commandList.Release();
            }

            base.OnDestroyed(immediately);
        }


        /// <summary>
        ///   Resets a Command List back to its initial state as if a new Command List was just created.
        /// </summary>
        public unsafe partial void Reset()
        {
            if (currentCommandList.Builder is not null)
                return;

            FlushResourceBarriers();
            ResetSrvHeap(createNewHeap: true);
            ResetSamplerHeap(createNewHeap: true);

            // Clear descriptor mappings
            srvMapping.Clear();
            samplerMapping.Clear();

            currentCommandList.Builder = this;
            currentCommandList.SrvHeaps = GraphicsDevice.DescriptorHeapLists.Acquire();
            currentCommandList.SamplerHeaps = GraphicsDevice.DescriptorHeapLists.Acquire();
            currentCommandList.StagingResources = GraphicsDevice.StagingResourceLists.Acquire();

            // Get a new allocator and unused command list
            currentCommandList.NativeCommandAllocator = GraphicsDevice.CommandAllocators.GetObject(GraphicsDevice.CommandListFence.Fence.GetCompletedValue());
            ResetCommandList();

            boundPipelineState = null;

            //
            // Reset the command list state.
            //
            void ResetCommandList()
            {
                scoped ref var nullInitialPipelineState = ref NullRef<ID3D12PipelineState>();

                if (NativeCommandLists.TryDequeue(out ComPtr<ID3D12GraphicsCommandList> nativeCommandList))
                {
                    currentCommandList.NativeCommandList = nativeCommandList;

                    HResult result = currentCommandList.NativeCommandList.Reset(currentCommandList.NativeCommandAllocator, ref nullInitialPipelineState);

                    if (result.IsFailure)
                        result.Throw();
                }
                else
                {
                    var commandAllocator = currentCommandList.NativeCommandAllocator;
                    HResult result = NativeDevice.CreateCommandList(nodeMask: 0, CommandListType.Direct, commandAllocator, ref nullInitialPipelineState,
                                                                    out ComPtr<ID3D12GraphicsCommandList> commandList);
                    if (result.IsFailure)
                        result.Throw();

                    currentCommandList.NativeCommandList = commandList;
                }

                currentCommandList.NativeCommandList.SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
            }
        }

        /// <summary>
        ///   Closes and executes the Command List.
        /// </summary>
        public partial void Flush()
        {
            FlushResourceBarriers();

            var commandList = Close();
            GraphicsDevice.ExecuteCommandList(commandList);
        }

        /// <summary>
        ///   Indicates that recording to the Command List has finished.
        /// </summary>
        /// <returns>
        ///   A <see cref="CompiledCommandList"/> representing the frozen list of recorded commands
        ///   that can be executed at a later time.
        /// </returns>
        public partial CompiledCommandList Close()
        {
            FlushResourceBarriers();

            HResult result = currentCommandList.NativeCommandList.Close();

            if (result.IsFailure)
                result.Throw();

            // Staging resources not updated anymore
            foreach (var stagingResource in currentCommandList.StagingResources)
            {
                stagingResource.UpdatingCommandList = null;
            }

            // Recycle heaps
            ResetSrvHeap(createNewHeap: false);
            ResetSamplerHeap(createNewHeap: false);

            var commandList = currentCommandList;
            currentCommandList = default;
            return commandList;
        }

        /// <summary>
        ///   Flushes the current Command List and optionally waits for its completion.
        /// </summary>
        /// <param name="wait">
        ///   A value indicating whether to wait for the Command List execution to complete.
        ///   <see langword="true"/> to wait for completion; otherwise, <see langword="false"/>.
        /// </param>
        /// <remarks>
        ///   This method finalizes the current Command List, submits it for execution, and resets
        ///   the state for future commands. If <paramref name="wait"/> is <see langword="true"/>,
        ///   the method will block until the Command List execution is finished.
        /// </remarks>
        private void FlushInternal(bool wait)
        {
            var commandList = Close();
            var commandListFenceValue = GraphicsDevice.ExecuteCommandListInternal(commandList);

            if (wait)
                GraphicsDevice.CommandListFence.WaitForFenceCPUInternal(commandListFenceValue);

            Reset();

            // Restore states
            if (boundPipelineState is not null)
                SetPipelineState(boundPipelineState);

            currentCommandList.NativeCommandList.SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
            SetRenderTargetsImpl(depthStencilBuffer, RenderTargets);
        }

        /// <summary>
        ///   Direct3D 12 implementation that clears and restores the state of the Graphics Device.
        /// </summary>
        private partial void ClearStateImpl() { }

        /// <summary>
        ///   Unbinds the Depth-Stencil Buffer and all the Render Targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl() { }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and a set of Render Targets to the output-merger stage.
        /// </summary>
        /// <param name="depthStencilBuffer">The Depth-Stencil Buffer to bind.</param>
        /// <param name="renderTargetViews">The Render Targets to bind.</param>
        private partial void SetRenderTargetsImpl(Texture depthStencilBuffer, ReadOnlySpan<Texture> renderTargetViews)
        {
            int renderTargetCount = renderTargetViews.Length;

            var renderTargetHandles = stackalloc CpuDescriptorHandle[renderTargetCount];

            for (int i = 0; i < renderTargetCount; ++i)
            {
                renderTargetHandles[i] = renderTargetViews[i].NativeRenderTargetView;
            }

            bool hasDepthStencilTargetToSet = depthStencilBuffer is not null;
            scoped ref var depthStencilView = ref hasDepthStencilTargetToSet
                ? ref depthStencilBuffer.NativeDepthStencilView
                : ref NullRef<CpuDescriptorHandle>();

            currentCommandList.NativeCommandList.OMSetRenderTargets((uint) renderTargetCount, in renderTargetHandles[0],
                                                                    RTsSingleHandleToDescriptorRange: false,
                                                                    in depthStencilView);
        }

        /// <summary>
        ///   Sets the stream output Buffers.
        /// </summary>
        /// <param name="buffers">
        ///   The Buffers to set for stream output.
        ///   Specify <see langword="null"/> or an empty array to unset any bound output Buffer.
        /// </param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
            // TODO: Implement stream output buffers
        }

        /// <summary>
        ///   Sets the viewports to the rasterizer stage.
        /// </summary>
        private void SetViewportImpl()
        {
            if (!viewportDirty && !scissorsDirty)
                return;

            scoped ref var viewport = ref viewports.GetReference();

            if (viewportDirty)
            {
                // NOTE: We assume the same layout and size as DIRECT3D12_VIEWPORT struct
                Debug.Assert(sizeof(Viewport) == sizeof(D3D12Viewport));
                scoped ref var d3dViewport = ref As<Viewport, D3D12Viewport>(ref viewport);

                currentCommandList.NativeCommandList.RSSetViewports(NumViewports: 1, in d3dViewport);

                var scissorRect = new SilkBox2I
                {
                    Min = { X = (int) viewport.X, Y = (int) viewport.Y },
                    Max = { X = (int) (viewport.X + viewport.Width), Y = (int) (viewport.Y + viewport.Height) }
                };
                currentCommandList.NativeCommandList.RSSetScissorRects(NumRects: 1, in scissorRect);
                viewportDirty = false;
            }

            if (boundPipelineState?.HasScissorEnabled ?? false)
            {
                if (scissorsDirty)
                {
                    // Use manual scissor
                    scoped ref var scissor = ref scissors.GetReference();
                    var scissorRect = new SilkBox2I
                    {
                        Min = { X = scissor.X, Y = scissor.Y },
                        Max = { X = scissor.Right, Y = scissor.Bottom }
                    };
                    currentCommandList.NativeCommandList.RSSetScissorRects(NumRects: 1, in scissorRect);
                }
            }
            else
            {
                // Use viewport
                // Always update, because either scissor or viewport was dirty and we use viewport size
                var scissorRect = new SilkBox2I
                {
                    Min = { X = (int) viewport.X, Y = (int) viewport.Y },
                    Max = { X = (int) (viewport.X + viewport.Width), Y = (int) (viewport.Y + viewport.Height) }
                };
                currentCommandList.NativeCommandList.RSSetScissorRects(NumRects: 1, in scissorRect);
            }

            scissorsDirty = false;
        }

        /// <summary>
        ///   Direct3D 12 implementation that sets a scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangle">The scissor rectangle to set.</param>
        private unsafe partial void SetScissorRectangleImpl(ref readonly Rectangle scissorRectangle)
        {
            // Do nothing. Direct3D 12 already sets the scissor rectangle as part of PrepareDraw()
        }

        /// <summary>
        ///   Direct3D 12 implementation that sets one or more scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        private unsafe partial void SetScissorRectanglesImpl(ReadOnlySpan<Rectangle> scissorRectangles)
        {
            // Do nothing. Direct3D 12 already sets the scissor rectangles as part of PrepareDraw()
        }

        /// <summary>
        ///   Prepares the Command List for a subsequent draw command.
        /// </summary>
        /// <remarks>
        ///    This method is called before each Draw() method to setup the correct Viewport.
        /// </remarks>
        private void PrepareDraw()
        {
            FlushResourceBarriers();
            SetViewportImpl();
        }

        /// <summary>
        ///   Sets the reference value for Depth-Stencil tests.
        /// </summary>
        /// <param name="stencilReference">Reference value to perform against when doing a Depth-Stencil test.</param>
        /// <seealso cref="SetPipelineState(PipelineState)"/>
        public void SetStencilReference(int stencilReference)
        {
            currentCommandList.NativeCommandList.OMSetStencilRef((uint) stencilReference);
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
            scoped Span<float> blendFactorFloats = blendFactor.AsSpan<Color4, float>();

            currentCommandList.NativeCommandList.OMSetBlendFactor(blendFactorFloats);
        }

        /// <summary>
        ///   Sets the configuration of the graphics pipeline which, among other things, control the shaders, input layout,
        ///   render states, and output settings.
        /// </summary>
        /// <param name="pipelineState">The Pipeline State object to set. Specify <see langword="null"/> to use the default one.</param>
        /// <seealso cref="PipelineState"/>
        public void SetPipelineState(PipelineState pipelineState)
        {
            if (boundPipelineState != pipelineState &&
                pipelineState is { CompiledState.Handle: not null })
            {
                // If scissor state changed, force a refresh
                scissorsDirty |= (boundPipelineState?.HasScissorEnabled ?? false) != pipelineState.HasScissorEnabled;

                currentCommandList.NativeCommandList.SetPipelineState(pipelineState.CompiledState);

                if (pipelineState.IsCompute)
                    currentCommandList.NativeCommandList.SetComputeRootSignature(pipelineState.RootSignature);
                else
                    currentCommandList.NativeCommandList.SetGraphicsRootSignature(pipelineState.RootSignature);

                boundPipelineState = pipelineState;
                currentCommandList.NativeCommandList.IASetPrimitiveTopology(pipelineState.PrimitiveTopology);
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
            if (buffer is null)
            {
                scoped ref var nullVertexBufferView = ref NullRef<VertexBufferView>();

                // Unset the Vertex Buffer from the slot
                currentCommandList.NativeCommandList.IASetVertexBuffers(StartSlot: (uint) index, NumViews: 1, in nullVertexBufferView);
                return;
            }

            var vertexBufferView = new VertexBufferView
            {
                BufferLocation = buffer.NativeResource.GetGPUVirtualAddress() + (ulong) offset,
                StrideInBytes = (uint) stride,
                SizeInBytes = (uint) (buffer.SizeInBytes - offset)
            };
            currentCommandList.NativeCommandList.IASetVertexBuffers(StartSlot: (uint) index, NumViews: 1, in vertexBufferView);
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
        public void SetIndexBuffer(Buffer buffer, int offset, bool is32Bits) // TODO: Use IndexElementSize?
        {
            if (buffer is null)
            {
                scoped ref var nullIndexBufferView = ref NullRef<IndexBufferView>();

                currentCommandList.NativeCommandList.IASetIndexBuffer(in nullIndexBufferView);
                return;
            }

            var indexBufferView = new IndexBufferView
            {
                BufferLocation = buffer.NativeResource.GetGPUVirtualAddress() + (ulong) offset,
                Format = is32Bits ? Format.FormatR32Uint : Format.FormatR16Uint,
                SizeInBytes = (uint) (buffer.SizeInBytes - offset)
            };
            currentCommandList.NativeCommandList.IASetIndexBuffer(in indexBufferView);
        }

        /// <summary>
        ///   Inserts a barrier that transitions a Graphics Resource to a new state, ensuring proper synchronization
        ///   between different GPU operations accessing the resource.
        /// </summary>
        /// <param name="resource">The Graphics Resource to transition to a different state.</param>
        /// <param name="newState">The new state of <paramref name="resource"/>.</param>
        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            Debug.Assert(resource is not null, "Resource must not be null.");

            // Find parent resource
            if (resource.ParentResource is not null)
                resource = resource.ParentResource;

            var targetState = (ResourceStates) newState;

            if (resource.IsTransitionNeeded(targetState))
            {
                var transitionBarrier = new ResourceBarrier
                {
                    Type = ResourceBarrierType.Transition,
                    Flags = ResourceBarrierFlags.None,

                    Transition = new ResourceTransitionBarrier
                    {
                        PResource = resource.NativeResource,
                        Subresource = uint.MaxValue,
                        StateBefore = resource.NativeResourceState,
                        StateAfter = targetState
                    }
                };

                resourceBarriers.Add(transitionBarrier);
                resource.NativeResourceState = targetState;
            }
        }

        /// <summary>
        ///   Flushes all pending Graphics Resource barriers.
        /// </summary>
        /// <remarks>
        ///   This method processes all pending resource barriers, applying them. This is to to ensure
        ///   that all queued resource transitions are executed.
        /// </remarks>
        private unsafe void FlushResourceBarriers()
        {
            int count = resourceBarriers.Count;
            if (count == 0)
                return;

            scoped Span<ResourceBarrier> barriers = stackalloc ResourceBarrier[count];
            resourceBarriers.CopyTo(barriers);

            resourceBarriers.Clear();

            currentCommandList.NativeCommandList.ResourceBarrier(NumBarriers: (uint) count, barriers);
        }

        /// <summary>
        ///   Transitions the specified Graphics Resource to a new state and returns an object that
        ///   can restore the resource to its previous state.
        /// </summary>
        /// <param name="resource">The Graphics Resource to transition. Cannot be <see langword="null"/>.</param>
        /// <param name="newState">The state to which the resource will be transitioned.</param>
        /// <returns>
        ///   A <see cref="ResourceBarrierTransitionRestore"/> object that can be used to restore
        ///   the resource to its original state.
        /// </returns>
        private ResourceBarrierTransitionRestore ResourceBarrierTransitionAndRestore(GraphicsResource resource, GraphicsResourceState newState)
        {
            var currentState = resource.NativeResourceState;
            ResourceBarrierTransition(resource, newState);

            return new ResourceBarrierTransitionRestore(this, resource, (GraphicsResourceState) currentState);
        }

        /// <summary>
        ///   Binds an array of Descriptor Sets at the specified index in the current pipeline's Root Signature,
        ///   making shader resources available for rendering operations.
        /// </summary>
        /// <param name="index">
        ///   The starting slot where the Descriptor Sets will be bound.
        /// </param>
        /// <param name="descriptorSets">
        ///   An array of Descriptor Sets containing resource bindings (such as Textures, Samplers, and Constant Buffers)
        ///   to be used by the currently active Pipeline State.
        /// </param>
        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
        RestartWithNewHeap:
            var descriptorTableIndex = 0;

            for (int i = 0; i < descriptorSets.Length; ++i)
            {
                // Find what is already mapped
                scoped ref var descriptorSet = ref descriptorSets[i];

                var srvBindCount = boundPipelineState.SrvBindCountPerLayout[i];
                var samplerBindCount = boundPipelineState.SamplerBindCountPerLayout[i];

                // Descriptors for SRVs, UAVs, and CBVs
                if (srvBindCount > 0 && descriptorSet.SrvStart.Ptr != 0)
                {
                    // Check if we need to copy them to Shader-visible Descriptor heap
                    if (!srvMapping.TryGetValue(descriptorSet.SrvStart.Ptr, out GpuDescriptorHandle gpuSrvStart))
                    {
                        var srvCount = descriptorSet.Description.SrvCount;

                        // Make sure heap is big enough
                        if (srvHeapOffset + srvCount > GraphicsDevice.SrvHeapSize)
                        {
                            ResetSrvHeap(createNewHeap: true);

                            currentCommandList.NativeCommandList.SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
                            goto RestartWithNewHeap;
                        }

                        // Copy
                        var destHandle = new CpuDescriptorHandle(srvHeap.CPUDescriptorHandleForHeapStart.Ptr +
                                                                 (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                        NativeDevice.CopyDescriptorsSimple((uint) srvCount, destHandle, descriptorSet.SrvStart, DescriptorHeapType.CbvSrvUav);

                        // Store mapping
                        gpuSrvStart = new GpuDescriptorHandle(srvHeap.GPUDescriptorHandleForHeapStart.Ptr +
                                                              (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                        srvMapping.Add(descriptorSet.SrvStart.Ptr, gpuSrvStart);

                        // Bump
                        srvHeapOffset += srvCount;
                    }

                    // Bind resource tables
                    // NOTE: Done once per stage, until we solve how to choose shader registers effect-wide at compile time (TODO)
                    if (IsComputePipelineStateBound)
                    {
                        for (int j = 0; j < srvBindCount; ++j)
                            currentCommandList.NativeCommandList.SetComputeRootDescriptorTable((uint) descriptorTableIndex++, gpuSrvStart);
                    }
                    else
                    {
                        for (int j = 0; j < srvBindCount; ++j)
                            currentCommandList.NativeCommandList.SetGraphicsRootDescriptorTable((uint) descriptorTableIndex++, gpuSrvStart);
                    }
                }

                // Descriptors for Samplers
                if (samplerBindCount > 0 && descriptorSet.SamplerStart.Ptr != 0)
                {
                    // Check if we need to copy them to Shader-visible Descriptor heap
                    if (!samplerMapping.TryGetValue(descriptorSet.SamplerStart.Ptr, out var gpuSamplerStart))
                    {
                        var samplerCount = descriptorSet.Description.SamplerCount;

                        // Make sure heap is big enough
                        if (samplerHeapOffset + samplerCount > GraphicsDevice.SamplerHeapSize)
                        {
                            ResetSamplerHeap(createNewHeap: true);

                            currentCommandList.NativeCommandList.SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
                            goto RestartWithNewHeap;
                        }

                        // Copy
                        var destHandle = new CpuDescriptorHandle(samplerHeap.CPUDescriptorHandleForHeapStart.Ptr +
                                                                 (nuint) (samplerHeapOffset * GraphicsDevice.SamplerHandleIncrementSize));

                        NativeDevice.CopyDescriptorsSimple((uint) samplerCount, destHandle, descriptorSet.SamplerStart, DescriptorHeapType.Sampler);

                        // Store mapping
                        gpuSamplerStart = new GpuDescriptorHandle(samplerHeap.GPUDescriptorHandleForHeapStart.Ptr +
                                                                  (nuint) (samplerHeapOffset * GraphicsDevice.SamplerHandleIncrementSize));

                        samplerMapping.Add(descriptorSet.SamplerStart.Ptr, gpuSamplerStart);

                        // Bump
                        samplerHeapOffset += samplerCount;
                    }

                    // Bind resource tables
                    // NOTE: Done once per stage, until we solve how to choose shader registers effect-wide at compile time (TODO)
                    if (IsComputePipelineStateBound)
                    {
                        for (int j = 0; j < samplerBindCount; ++j)
                            currentCommandList.NativeCommandList.SetComputeRootDescriptorTable((uint) descriptorTableIndex++, gpuSamplerStart);
                    }
                    else
                    {
                        for (int j = 0; j < samplerBindCount; ++j)
                            currentCommandList.NativeCommandList.SetGraphicsRootDescriptorTable((uint) descriptorTableIndex++, gpuSamplerStart);
                    }
                }
            }
        }

        /// <summary>
        ///   Resets the current Descriptor heap for Shader Resource Views (SRV), optionally creating a new heap.
        /// </summary>
        /// <param name="createNewHeap">
        ///   A value indicating whether to create a new SRV heap.
        ///   If <see langword="true"/>, a new heap is retrieved from the pool; otherwise, the current heap is recycled.
        /// </param>
        /// <remarks>
        ///   This method manages the lifecycle of the SRV heap by either recycling the existing heap or
        ///   creating a new one from the pool.
        ///   The current heap is cleared and its resources are prepared for reuse if applicable.
        ///   The method also updates the Descriptor heap array to reference the new active SRV heap.
        /// </remarks>
        private void ResetSrvHeap(bool createNewHeap)
        {
            // If there is currently a heap, recycle it
            if (srvHeap.Heap.IsNotNull())
            {
                currentCommandList.SrvHeaps.Add(srvHeap);
                srvHeap.Heap = default;
            }

            // If we need to create a new heap, get one from the pool
            if (createNewHeap)
            {
                srvHeap = GraphicsDevice.SrvHeaps.GetObject(GraphicsDevice.CommandListFence.Fence.GetCompletedValue());
                srvHeapOffset = 0;
                srvMapping.Clear();
            }

            // Update the Descriptor heap array to reference the new active SRV heap
            descriptorHeaps[0] = srvHeap.Heap;
        }

        /// <summary>
        ///   Resets the current Descriptor heap for Samplers, optionally creating a new heap.
        /// </summary>
        /// <param name="createNewHeap">
        ///   A value indicating whether to create a new Samplers heap.
        ///   If <see langword="true"/>, a new heap is retrieved from the pool; otherwise, the current heap is recycled.
        /// </param>
        /// <remarks>
        ///   This method manages the lifecycle of the Samplers heap by either recycling the existing heap or
        ///   creating a new one from the pool.
        ///   The current heap is cleared and its resources are prepared for reuse if applicable.
        ///   The method also updates the Descriptor heap array to reference the new active Samplers heap.
        /// </remarks>
        private void ResetSamplerHeap(bool createNewHeap)
        {
            // If there is currently a heap, recycle it
            if (samplerHeap.Heap.IsNotNull())
            {
                currentCommandList.SamplerHeaps.Add(samplerHeap);
                samplerHeap.Heap = default;
            }

            // If we need to create a new heap, get one from the pool
            if (createNewHeap)
            {
                samplerHeap = GraphicsDevice.SamplerHeaps.GetObject(GraphicsDevice.CommandListFence.Fence.GetCompletedValue());
                samplerHeapOffset = 0;
                samplerMapping.Clear();
            }

            // Update the Descriptor heap array to reference the new active Samplers heap
            descriptorHeaps[1] = samplerHeap.Heap;
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

            currentCommandList.NativeCommandList.Dispatch((uint) threadCountX, (uint) threadCountY, (uint) threadCountZ);
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
            throw new NotImplementedException(); // TODO: Implement DispatchIndirect
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

            currentCommandList.NativeCommandList.DrawInstanced((uint) vertexCount, InstanceCount: 1,
                                                               (uint) startVertexLocation, StartInstanceLocation: 0);

            GraphicsDevice.FrameTriangleCount += (uint)vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        ///   Issues a draw call for geometry of unknown size, typically used with Vertex or Index Buffers populated via Stream Output.
        ///   The vertex count is inferred from the data written by the GPU.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            throw new NotImplementedException(); // TODO: Implement DrawAuto
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

            currentCommandList.NativeCommandList.DrawIndexedInstanced((uint) indexCount, InstanceCount: 1,
                                                                      (uint) startIndexLocation, baseVertexLocation,
                                                                      StartInstanceLocation: 0);
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

            currentCommandList.NativeCommandList.DrawIndexedInstanced((uint) indexCountPerInstance,
                                                                      (uint) instanceCount,
                                                                      (uint) startIndexLocation, baseVertexLocation,
                                                                      (uint) startInstanceLocation);
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

            //NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);
            throw new NotImplementedException(); // TODO: Implement DrawIndexedInstancedIndirect

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

            currentCommandList.NativeCommandList.DrawInstanced((uint) vertexCountPerInstance, (uint) instanceCount,
                                                               (uint) startVertexLocation, (uint) startInstanceLocation);
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

            //NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);
            throw new NotImplementedException(); // TODO: Implement DrawInstancedIndirect

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        ///   Submits a GPU timestamp Query.
        /// </summary>
        /// <param name="queryPool">The <see cref="QueryPool"/> owning the Query.</param>
        /// <param name="index">The index of the Query to write.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            currentCommandList.NativeCommandList.EndQuery(queryPool.NativeQueryHeap, Silk.NET.Direct3D12.QueryType.Timestamp, (uint) index);

            queryPool.PendingValue = queryPool.CompletedValue + 1;
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
            if (IsDebugMode)
                WinPixNative.PIXBeginEventOnCommandList(currentCommandList.NativeCommandList, profileColor, name);
        }

        /// <summary>
        ///   Marks the end of a profile section previously started by a call to <see cref="BeginProfile"/>.
        /// </summary>
        /// <inheritdoc cref="BeginProfile(Color4, string)" path="/remarks"/>
        public void EndProfile()
        {
            if (IsDebugMode)
                WinPixNative.PIXEndEventOnCommandList(currentCommandList.NativeCommandList);
        }

        // TODO: Unused, remove?
        public void ResetQueryPool(QueryPool queryPool)
        {
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

            using var _ = ResourceBarrierTransitionAndRestore(depthStencilBuffer, GraphicsResourceState.DepthWrite);
            FlushResourceBarriers();

            // Check that the Depth-Stencil Buffer has a Stencil if Clear Stencil is requested
            if (options.HasFlag(DepthStencilClearOptions.Stencil))
            {
                if (!depthStencilBuffer.HasStencil)
                    throw new InvalidOperationException(string.Format(FrameworkResources.NoStencilBufferForDepthFormat, depthStencilBuffer.ViewFormat));
            }

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();

            currentCommandList.NativeCommandList.ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView,
                                                                       (ClearFlags) options, depth, stencil,
                                                                       NumRects: 0, in nullRect);
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

            using var _ = ResourceBarrierTransitionAndRestore(renderTarget, GraphicsResourceState.RenderTarget);
            FlushResourceBarriers();

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearColorFloats = ref color.AsSpan<Color4, float>()[0];

            currentCommandList.NativeCommandList.ClearRenderTargetView(renderTarget.NativeRenderTargetView, ref clearColorFloats,
                                                                        NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Clears a Read-Write Buffer.
        /// </summary>
        /// <param name="buffer">The Buffer to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> must support Unordered Access.</exception>
        public void ClearReadWrite(Buffer buffer, Vector4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            var cpuHandle = buffer.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearValue = ref value.AsSpan<Vector4, float>()[0];

            currentCommandList.NativeCommandList.ClearUnorderedAccessViewFloat(gpuHandle, cpuHandle, buffer.NativeResource,
                                                                               ref clearValue, NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Clears a Read-Write Buffer.
        /// </summary>
        /// <param name="buffer">The Buffer to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> must support Unordered Access.</exception>
        public void ClearReadWrite(Buffer buffer, Int4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            var cpuHandle = buffer.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearValue = ref value.AsSpan<Int4, uint>()[0];

            currentCommandList.NativeCommandList.ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, buffer.NativeResource,
                                                                              ref clearValue, NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Clears a Read-Write Buffer.
        /// </summary>
        /// <param name="buffer">The Buffer to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Buffer.</param>
        /// <exception cref="ArgumentNullException"><paramref name="buffer"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="buffer"/> must support Unordered Access.</exception>
        public void ClearReadWrite(Buffer buffer, UInt4 value)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (buffer.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            var cpuHandle = buffer.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearValue = ref value.AsSpan<UInt4, uint>()[0];

            currentCommandList.NativeCommandList.ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, buffer.NativeResource,
                                                                              ref clearValue, NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Clears a Read-Write Texture.
        /// </summary>
        /// <param name="texture">The Texture to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Texture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="texture"/> must support Unordered Access.</exception>
        public void ClearReadWrite(Texture texture, Vector4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);

            if (texture.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            var cpuHandle = texture.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearValue = ref value.AsSpan<Vector4, float>()[0];

            currentCommandList.NativeCommandList.ClearUnorderedAccessViewFloat(gpuHandle, cpuHandle, texture.NativeResource,
                                                                               ref clearValue, NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Clears a Read-Write Texture.
        /// </summary>
        /// <param name="texture">The Texture to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Texture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="texture"/> must support Unordered Access.</exception>
        public void ClearReadWrite(Texture texture, Int4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);

            if (texture.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            var cpuHandle = texture.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearValue = ref value.AsSpan<Int4, uint>()[0];

            currentCommandList.NativeCommandList.ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, texture.NativeResource,
                                                                              ref clearValue, NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Clears a Read-Write Texture.
        /// </summary>
        /// <param name="texture">The Texture to clear. It must have been created with read-write / unordered access flags.</param>
        /// <param name="value">The value to use to clear the Texture.</param>
        /// <exception cref="ArgumentNullException"><paramref name="texture"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="texture"/> must support Unordered Access.</exception>
        public void ClearReadWrite(Texture texture, UInt4 value)
        {
            ArgumentNullException.ThrowIfNull(texture);

            if (texture.NativeUnorderedAccessView.Ptr == 0)
                throw new ArgumentException("Expecting texture supporting UAV", nameof(texture));

            var cpuHandle = texture.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearValue = ref value.AsSpan<UInt4, uint>()[0];

            currentCommandList.NativeCommandList.ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, texture.NativeResource,
                                                                              ref clearValue, NumRects: 0, in nullRect);
        }

        /// <summary>
        ///   Retrieves the GPU Descriptor handle corresponding to the specified CPU Descriptor handle.
        /// </summary>
        /// <param name="cpuHandle">The CPU descriptor handle for which the GPU descriptor handle is requested.</param>
        /// <returns>
        ///   A <see cref="GpuDescriptorHandle"/> that corresponds to the provided <paramref name="cpuHandle"/>.
        ///   If the mapping does not already exist, a new GPU Descriptor handle is created, and the mapping is stored.
        /// </returns>
        private GpuDescriptorHandle GetGpuDescriptorHandle(CpuDescriptorHandle cpuHandle)
        {
            if (!srvMapping.TryGetValue(cpuHandle.Ptr, out var resultGpuHandle))
            {
                var srvCount = 1;

                // Make sure heap is big enough
                if (srvHeapOffset + srvCount > GraphicsDevice.SrvHeapSize)
                {
                    ResetSrvHeap(createNewHeap: true);

                    currentCommandList.NativeCommandList.SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
                }

                // Copy
                var destHandle = new CpuDescriptorHandle(srvHeap.CPUDescriptorHandleForHeapStart.Ptr +
                                                         (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                NativeDevice.CopyDescriptorsSimple((uint) srvCount, destHandle, cpuHandle, DescriptorHeapType.CbvSrvUav);

                // Store mapping
                resultGpuHandle = new GpuDescriptorHandle(srvHeap.GPUDescriptorHandleForHeapStart.Ptr +
                                                          (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                srvMapping.Add(cpuHandle.Ptr, resultGpuHandle);

                // Bump
                srvHeapOffset += srvCount;
            }

            return resultGpuHandle;
        }

        /// <summary>
        ///   Copies the data from a Graphics Resource to another.
        ///   Views are ignored and the full underlying data is copied.
        /// </summary>
        /// <param name="source">The source Graphics Resource.</param>
        /// <param name="destination">The destination Graphics Resource.</param>
        /// <exception cref="ArgumentNullException"><paramref name="source"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="destination"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        ///   The source and destination Graphics Resources are of incompatible types.
        /// </exception>
        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
            ArgumentNullException.ThrowIfNull(source);
            ArgumentNullException.ThrowIfNull(destination);

            // Copy Texture -> Texture
            if (source is Texture sourceTexture &&
                destination is Texture destinationTexture)
            {
                CopyBetweenTextures(sourceTexture, destinationTexture);
            }
            // Copy Buffer -> Buffer
            else if (source is Buffer sourceBuffer &&
                     destination is Buffer destinationBuffer)
            {
                CopyBetweenBuffers(sourceBuffer, destinationBuffer);
            }
            else throw new InvalidOperationException($"Cannot copy data between GraphicsResources of types [{source.GetType()}] and [{destination.GetType()}].");

            //
            // Copies the data from a Texture to another Texture.
            //
            void CopyBetweenTextures(Texture sourceTexture, Texture destinationTexture)
            {
                // Get the parent Textures in case these are Texture Views
                var sourceParent = sourceTexture.ParentTexture ?? sourceTexture;
                var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    // Copy staging Texture -> staging Texture
                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        CopyStagingTextureToStagingTexture(sourceTexture, destinationTexture);
                    }
                    else // Copy Texture -> staging Texture
                    {
                        CopyTextureToStagingTexture(sourceTexture, sourceParent, destinationTexture);
                    }

                    // Fence for host access
                    destinationParent.CommandListFenceValue = null;
                    destinationParent.UpdatingCommandList = this;
                    currentCommandList.StagingResources.Add(destinationParent);
                }
                else // Copy Texture -> Texture
                {
                    CopyTextureToTexture(sourceTexture, destinationTexture);
                }
            }

            //
            // Copies the data from a staging Texture to another staging Texture.
            //
            void CopyStagingTextureToStagingTexture(Texture sourceTexture, Texture destinationTexture)
            {
                var size = destinationTexture.ComputeBufferTotalSize();

                scoped ref var fullRange = ref NullRef<D3D12Range>();

                void* destinationMapped = null;
                HResult result = destinationTexture.NativeResource.Map(Subresource: 0, in fullRange, ref destinationMapped);

                if (result.IsFailure)
                    result.Throw();

                void* sourceMapped = null;
                var sourceRange = new D3D12Range { Begin = 0, End = (nuint) size };
                result = sourceTexture.NativeResource.Map(Subresource: 0, in sourceRange, ref sourceMapped);

                if (result.IsFailure)
                    result.Throw();

                MemoryUtilities.CopyWithAlignmentFallback(destinationMapped, sourceMapped, (uint) size);

                sourceTexture.NativeResource.Unmap(Subresource: 0, ref fullRange);
                destinationTexture.NativeResource.Unmap(Subresource: 0, ref fullRange);
            }

            //
            // Copies the data from a Texture to a staging Texture.
            //
            void CopyTextureToStagingTexture(Texture sourceTexture, Texture sourceParent, Texture destinationTexture)
            {
                using var _1 = ResourceBarrierTransitionAndRestore(sourceTexture, GraphicsResourceState.CopySource);
                using var _2 = ResourceBarrierTransitionAndRestore(destinationTexture, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                int copyOffset = 0;
                for (int arraySlice = 0; arraySlice < sourceParent.ArraySize; ++arraySlice)
                {
                    for (int mipLevel = 0; mipLevel < sourceParent.MipLevelCount; ++mipLevel)
                    {
                        var destRegion = new TextureCopyLocation
                        {
                            PResource = destinationTexture.NativeResource,
                            Type = TextureCopyType.PlacedFootprint,
                            PlacedFootprint = new()
                            {
                                Offset = (ulong) copyOffset,
                                Footprint =
                                        {
                                            Width = (uint) Texture.CalculateMipSize(destinationTexture.Width, mipLevel),
                                            Height = (uint) Texture.CalculateMipSize(destinationTexture.Height, mipLevel),
                                            Depth = (uint) Texture.CalculateMipSize(destinationTexture.Depth, mipLevel),
                                            Format = (Format) destinationTexture.Format,
                                            RowPitch = (uint) destinationTexture.ComputeRowPitch(mipLevel)
                                        }
                            }
                        };
                        var srcRegion = new TextureCopyLocation
                        {
                            PResource = sourceTexture.NativeResource,
                            Type = TextureCopyType.SubresourceIndex,
                            SubresourceIndex = (uint) (arraySlice * sourceParent.MipLevelCount + mipLevel)
                        };

                        currentCommandList.NativeCommandList.CopyTextureRegion(in destRegion, DstX: 0, DstY: 0, DstZ: 0,
                                                                               in srcRegion, pSrcBox: null);

                        copyOffset += destinationTexture.ComputeSubResourceSize(mipLevel);
                    }
                }
            }

            //
            // Copies the data from a Texture to another Texture.
            //
            void CopyTextureToTexture(Texture sourceTexture, Texture destinationTexture)
            {
                using var _1 = ResourceBarrierTransitionAndRestore(sourceTexture, GraphicsResourceState.CopySource);
                using var _2 = ResourceBarrierTransitionAndRestore(destinationTexture, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyResource(destinationTexture.NativeResource, sourceTexture.NativeResource);
            }

            //
            // Copies the data from a Buffer to another Buffer.
            //
            void CopyBetweenBuffers(Buffer sourceBuffer, Buffer destinationBuffer)
            {
                using var _1 = ResourceBarrierTransitionAndRestore(sourceBuffer, GraphicsResourceState.CopySource);
                using var _2 = ResourceBarrierTransitionAndRestore(destinationBuffer, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyResource(destinationBuffer.NativeResource, sourceBuffer.NativeResource);

                if (destinationBuffer.Usage == GraphicsResourceUsage.Staging)
                {
                    // Fence for host access
                    destinationBuffer.CommandListFenceValue = null;
                    destinationBuffer.UpdatingCommandList = this;
                    currentCommandList.StagingResources.Add(destinationBuffer);
                }
            }
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

            currentCommandList.NativeCommandList.ResolveSubresource(sourceMultiSampledTexture.NativeResource, (uint) sourceSubResourceIndex,
                                                                    destinationTexture.NativeResource, (uint) destinationSubResourceIndex,
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
        /// <exception cref="NotImplementedException">Copying regions of staging resources is not currently supported.</exception>
        /// <exception cref="InvalidOperationException">
        ///   The Graphics Resources are incompatible. Cannot copy data between Buffers and Textures.
        /// </exception>
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
            if (source is Texture sourceTexture &&
                destination is Texture destinationTexture)
            {
                CopyBetweenTextures(sourceTexture, destinationTexture);
            }
            else if (source is Buffer sourceBuffer &&
                     destination is Buffer destinationBuffer)
            {
                CopyBetweenBuffers(sourceBuffer, destinationBuffer);
            }
            else throw new InvalidOperationException("Cannot copy data between Buffers and Textures.");

            //
            // Copies a region from a Texture to another Texture.
            //
            void CopyBetweenTextures(Texture sourceTexture, Texture destinationTexture)
            {
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging &&
                    destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    throw new NotImplementedException("Copy region of staging resources is not supported yet"); // TODO: Implement copy region for staging resources
                }

                using var _1 = ResourceBarrierTransitionAndRestore(source, GraphicsResourceState.CopySource);
                using var _2 = ResourceBarrierTransitionAndRestore(destination, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    throw new NotImplementedException("Copy region from staging texture is not supported yet");
                }

                var srcRegion = new TextureCopyLocation
                {
                    PResource = source.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) sourceSubResourceIndex
                };

                SkipInit(out TextureCopyLocation destRegion);

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                    SkipInit(out PlacedSubresourceFootprint footprint);
                    uint numRows = 0;
                    ulong rowSizeInBytes = 0;
                    ulong totalBytes = 0;
                    NativeDevice.GetCopyableFootprints(ref destinationTexture.NativeTextureDescription,
                                                       (uint) destinationSubResourceIndex, NumSubresources: 1, BaseOffset: 0,
                                                       ref footprint, ref numRows, ref rowSizeInBytes, ref totalBytes);

                    destRegion = new TextureCopyLocation
                    {
                        PResource = destinationTexture.NativeResource,
                        Type = TextureCopyType.PlacedFootprint,
                        PlacedFootprint = footprint
                    };

                    // Fence for host access
                    destinationParent.CommandListFenceValue = null;
                    destinationParent.UpdatingCommandList = this;
                    currentCommandList.StagingResources.Add(destinationParent);
                }
                else // Regular Texture
                {
                    destRegion = new TextureCopyLocation
                    {
                        PResource = destination.NativeResource,
                        Type = TextureCopyType.SubresourceIndex,
                        SubresourceIndex = (uint) destinationSubResourceIndex
                    };
                }

                if (sourceRegion is ResourceRegion srcResourceRegion)
                {
                    // NOTE: We assume the same layout and size as D3D12_BOX
                    Debug.Assert(sizeof(D3D12Box) == sizeof(ResourceRegion));
                    var sourceBox = srcResourceRegion.BitCast<ResourceRegion, D3D12Box>();

                    currentCommandList.NativeCommandList.CopyTextureRegion(in destRegion, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                                           in srcRegion, in sourceBox);
                }
                else
                {
                    currentCommandList.NativeCommandList.CopyTextureRegion(in destRegion, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                                           in srcRegion, pSrcBox: null);
                }
            }

            //
            // Copies a region from a Buffer to another Buffer.
            //
            void CopyBetweenBuffers(Buffer sourceBuffer, Buffer destinationBuffer)
            {
                using var _1 = ResourceBarrierTransitionAndRestore(source, GraphicsResourceState.CopySource);
                using var _2 = ResourceBarrierTransitionAndRestore(destination, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyBufferRegion(destinationBuffer.NativeResource, (ulong)dstX,
                                                                      sourceBuffer.NativeResource,
                                                                      SrcOffset: (ulong)(sourceRegion?.Left ?? 0),
                                                                      NumBytes: sourceRegion.HasValue
                                                                        ? (ulong)(sourceRegion.Value.Right - sourceRegion.Value.Left)
                                                                        : (ulong) sourceBuffer.SizeInBytes);
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

            currentCommandList.NativeCommandList.CopyBufferRegion(destinationBuffer.NativeResource, (ulong) destinationOffsetInBytes,
                                                                  sourceBuffer.NativeResource, SrcOffset: 0, NumBytes: sizeof(uint));
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        ///   Only <see cref="Texture"/>s and <see cref="Buffer"/>s are supported.
        /// </exception>
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

            fixed (byte* sourceDataPtr = sourceData)
            {
                var sourceDataBox = new DataBox((nint) sourceDataPtr, rowPitch: 0, slicePitch: 0);
                UpdateSubResource(resource, subResourceIndex, sourceDataBox);
            }
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <exception cref="InvalidOperationException">
        ///   Only <see cref="Texture"/>s and <see cref="Buffer"/>s are supported.
        /// </exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte})" path="/remarks" />
        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData)
        {
            ResourceRegion region = resource switch
            {
                Texture texture => new ResourceRegion(left: 0, top: 0, front: 0, texture.Width, texture.Height, texture.Depth),
                Buffer buffer => new ResourceRegion(left: 0, top: 0, front: 0, buffer.SizeInBytes, bottom: 1, back: 1),

                _ => throw new InvalidOperationException("Unknown resource type")
            };

            UpdateSubResource(resource, subResourceIndex, sourceData, region);
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
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="resource"/> is a <see cref="Texture"/>, but its <see cref="Texture.Dimension"/> is not one of the supported types.
        /// </exception
        /// <exception cref="InvalidOperationException"><paramref name="resource"/> is of an unknown type and cannot be updated.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte})" path="/remarks" />
        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, ReadOnlySpan<byte> sourceData, ResourceRegion region)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (sourceData.IsEmpty)
                return;

            fixed (byte* sourceDataPtr = sourceData)
            {
                var sourceDataBox = new DataBox((nint) sourceDataPtr, rowPitch: 0, slicePitch: 0);
                UpdateSubResource(resource, subResourceIndex, sourceDataBox, region);
            }
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
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="resource"/> is a <see cref="Texture"/>, but its <see cref="Texture.Dimension"/> is not one of the supported types.
        /// </exception
        /// <exception cref="InvalidOperationException"><paramref name="resource"/> is of an unknown type and cannot be updated.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte}, ResourceRegion)" path="/remarks" />
        internal unsafe partial void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData, ResourceRegion region)
        {
            if (resource is Texture texture)
            {
                UpdateTexture(texture);
            }
            else if (resource is Buffer)
            {
                UpdateBuffer();
            }
            else throw new InvalidOperationException("Unknown type of Graphics Resource");

            //
            // Updates a Texture with data from CPU memory.
            //
            void UpdateTexture(Texture texture)
            {
                var width = region.Right - region.Left;
                var height = region.Bottom - region.Top;
                var depth = region.Back - region.Front;

                SkipInit(out ResourceDesc resourceDescription);
                switch (texture.Dimension)
                {
                    case TextureDimension.Texture1D:
                        resourceDescription = texture.ConvertToNativeDescription1D();
                        resourceDescription.Width = (ulong) width;
                        resourceDescription.DepthOrArraySize = 1;
                        resourceDescription.MipLevels = 1;
                        break;

                    case TextureDimension.Texture2D:
                    case TextureDimension.TextureCube:
                        resourceDescription = texture.ConvertToNativeDescription2D();
                        resourceDescription.Width = (ulong) width;
                        resourceDescription.Height = (uint) height;
                        resourceDescription.DepthOrArraySize = 1;
                        resourceDescription.MipLevels = 1;
                        break;

                    case TextureDimension.Texture3D:
                        resourceDescription = texture.ConvertToNativeDescription3D();
                        resourceDescription.Width = (ulong) width;
                        resourceDescription.Height = (uint) height;
                        resourceDescription.DepthOrArraySize = (ushort) depth;
                        resourceDescription.MipLevels = 1;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(texture), "The Graphics Resource is a Texture, but its dimension is not one of the supported types.");
                }

                // TODO D3D12 allocate in upload heap (placed resources?)
                var heap = new HeapProperties
                {
                    CPUPageProperty = CpuPageProperty.WriteBack,
                    MemoryPoolPreference = MemoryPool.L0,
                    CreationNodeMask = 1,
                    VisibleNodeMask = 1,
                    Type = HeapType.Custom
                };

                HResult result = NativeDevice.CreateCommittedResource(in heap, HeapFlags.None,
                                                                      in resourceDescription, ResourceStates.GenericRead,
                                                                      pOptimizedClearValue: null,
                                                                      out ComPtr<ID3D12Resource> nativeUploadTexture);
                if (result.IsFailure)
                    result.Throw();

                lock (GraphicsDevice.TemporaryResources)
                    GraphicsDevice.TemporaryResources.Enqueue((GraphicsDevice.FrameFence.NextFenceValue, nativeUploadTexture));

                scoped ref var fullBox = ref NullRef<D3D12Box>();

                result = nativeUploadTexture.WriteToSubresource(DstSubresource: 0, in fullBox,
                                                                (void*) sourceData.DataPointer, (uint) sourceData.RowPitch, (uint) sourceData.SlicePitch);
                if (result.IsFailure)
                    result.Throw();

                // Trigger copy
                using var _ = ResourceBarrierTransitionAndRestore(resource, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                var destRegion = new TextureCopyLocation
                {
                    PResource = resource.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) subResourceIndex
                };
                var srcRegion = new TextureCopyLocation
                {
                    PResource = nativeUploadTexture,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = 0
                };
                currentCommandList.NativeCommandList.CopyTextureRegion(in destRegion, (uint) region.Left, (uint) region.Top, (uint) region.Front,
                                                                       in srcRegion, in fullBox);
            }

            //
            // Updates a Buffer with data from CPU memory.
            //
            void UpdateBuffer()
            {
                var uploadSize = region.Right - region.Left;
                var uploadMemory = GraphicsDevice.AllocateUploadBuffer(uploadSize, out var uploadResource, out var uploadOffset);

                MemoryUtilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) sourceData.DataPointer, (uint) uploadSize);

                using var _ = ResourceBarrierTransitionAndRestore(resource, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyBufferRegion(pDstBuffer: resource.NativeResource, DstOffset: (ulong)region.Left,
                                                                      uploadResource, (ulong)uploadOffset, (ulong)uploadSize);
            }
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
        /// <exception cref="ArgumentException">
        ///   Cannot map a sub-resource of a Graphics Resource that is not a <see cref="Buffer"/> or a <see cref="Texture"/>.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        ///   The Graphics Resource is being updated by other Command List, but it is not yet finished.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Cannot use <see cref="MapMode.WriteDiscard"/>. Direct3D 12 does not support this mode.
        /// </exception>
        /// <exception cref="ArgumentException">
        ///   Only CPU-accessible staging Graphics Resources admit reading / writing modes (<see cref="MapMode.Read"/>,
        ///   <see cref="MapMode.Write"/>, or <see cref="MapMode.ReadWrite"/>).
        /// </exception>
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
            ArgumentNullException.ThrowIfNull(resource);

            var rowPitch = 0;
            var depthStride = 0;
            GraphicsResourceUsage usage;

            if (resource is Texture texture)
            {
                usage = texture.Usage;

                if (lengthInBytes == 0)
                    lengthInBytes = texture.ComputeSubResourceSize(subResourceIndex);

                rowPitch = texture.ComputeRowPitch(subResourceIndex % texture.MipLevelCount);
                depthStride = texture.ComputeSlicePitch(subResourceIndex % texture.MipLevelCount);

                if (usage == GraphicsResourceUsage.Staging)
                {
                    // Internally it's a Buffer, so adapt resource index and offset
                    offsetInBytes = texture.ComputeBufferOffset(subResourceIndex, depthSlice: 0);
                    subResourceIndex = 0;
                }
            }
            else if (resource is Buffer buffer)
            {
                usage = buffer.Usage;

                if (lengthInBytes == 0)
                    lengthInBytes = buffer.SizeInBytes;
            }
            else throw new ArgumentException("Only Buffers and Textures can be mapped", nameof(resource));

            if (mapMode is MapMode.Read or MapMode.ReadWrite or MapMode.Write)
            {
                // Is non-staging even possible for Read/Write?
                if (usage != GraphicsResourceUsage.Staging)
                    throw new ArgumentException("Read / Write / ReadWrite is only supported for staging resources", nameof(mapMode));
            }

            // NOTE: This path is quite slow (it creates a new resource).
            //       Once we switch to D3D12 / Vulkan only, we should probably get rid of this use case
            //       by pooling and reusing buffers internally, or explicitly managed at the caller side
            if (mapMode == MapMode.WriteDiscard)
            {
                // Mark old resource for deletion once Command List are executed
                resource.OnDestroyed();

                // Create new resource
                resource.OnRecreate();
            }
            else if (mapMode != MapMode.WriteNoOverwrite)   // Write / Read / ReadWrite
            {
                // Need to wait?
                if (
                    // used in command list which hasn't be submitted yet? (only valid if our own, checked later)
                    resource.UpdatingCommandList is not null
                    // updated in a previous command list which hasn't be finished yet
                    || (resource.CommandListFenceValue is not null && !GraphicsDevice.CommandListFence.IsFenceCompleteInternal(resource.CommandListFenceValue.Value)))
                {
                    if (doNotWait)
                    {
                        return new MappedResource(resource, subResourceIndex, dataBox: default);
                    }

                    if (resource.UpdatingCommandList == this)
                        // Need to flush? (check if part of current Command List)
                        // resource.CommandListFenceValue should be set after
                        FlushInternal(wait: false);
                    else if (resource.UpdatingCommandList is not null)
                        throw new InvalidOperationException("CommandList updating the staging resource has not been submitted");

                    if (resource.CommandListFenceValue is null)
                        throw new InvalidOperationException($"Invalid state for {resource.CommandListFenceValue}");

                    GraphicsDevice.CommandListFence.WaitForFenceCPUInternal(resource.CommandListFenceValue.Value);

                    // We're now up to date, remove command list fence value (if any)
                    resource.CommandListFenceValue = null;
                }
            }

            // Also make sure all copy queues are done
            // (important for all cases, since it uploads initial data and also set resource barrier)
            if (resource.CopyFenceValue.HasValue)
            {
                GraphicsDevice.CopyFence.WaitForFenceCPUInternal(resource.CopyFenceValue.Value);
                resource.CopyFenceValue = null;
            }

            scoped ref var fullRange = ref NullRef<D3D12Range>();

            void* mappedMemory = null;
            HResult result = resource.NativeResource.Map((uint) subResourceIndex, pReadRange: in fullRange, ref mappedMemory);

            if (result.IsFailure)
                result.Throw();

            var mappedData = new DataBox((IntPtr)((byte*) mappedMemory + offsetInBytes), rowPitch, depthStride);
            return new MappedResource(resource, subResourceIndex, mappedData, offsetInBytes, lengthInBytes);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        /// <summary>
        ///   Unmaps a sub-resource of a Graphics Resource, which was previously mapped to CPU memory with <see cref="MapSubResource"/>,
        ///   and in the process re-enables the GPU access to that sub-resource.
        /// </summary>
        /// <param name="mappedResource">
        ///   A <see cref="MappedResource"/> structure identifying the sub-resource to unmap.
        /// </param>
        public unsafe partial void UnmapSubResource(MappedResource unmapped)
        {
            scoped ref var fullRange = ref NullRef<D3D12Range>();

            unmapped.Resource.NativeResource.Unmap((uint) unmapped.SubResourceIndex, pWrittenRange: in fullRange);
        }

        #region DescriptorHeapWrapper structure

        /// <summary>
        ///   Internal structure that wraps a <see cref="ID3D12DescriptorHeap"/> and its cached GPU and CPU pointers.
        /// </summary>
        private struct DescriptorHeapWrapper
        {
            public ComPtr<ID3D12DescriptorHeap> Heap;

            public CpuDescriptorHandle CPUDescriptorHandleForHeapStart;
            public GpuDescriptorHandle GPUDescriptorHandleForHeapStart;


            /// <summary>
            ///   Initializes a new instance of the <see cref="DescriptorHeapWrapper"/> struct with the specified heap.
            /// </summary>
            /// <param name="heap">The Descriptor heap to wrap.</param>
            public DescriptorHeapWrapper(ComPtr<ID3D12DescriptorHeap> heap)
            {
                Heap = heap;

                if (heap.IsNotNull())
                {
                    CPUDescriptorHandleForHeapStart = heap.GetCPUDescriptorHandleForHeapStart();
                    GPUDescriptorHandleForHeapStart = heap.GetGPUDescriptorHandleForHeapStart();
                }
            }

            public static implicit operator ComPtr<ID3D12DescriptorHeap>(DescriptorHeapWrapper wrapper) => wrapper.Heap;
            public static implicit operator DescriptorHeapWrapper(ComPtr<ID3D12DescriptorHeap> heap) => new DescriptorHeapWrapper(heap);
        }

        #endregion

        #region ResourceBarrierTransitionRestore structure

        /// <summary>
        ///   Provides a mechanism to temporarily transition a Graphics Resource to a new state
        ///   and automatically restore its previous state when disposed.
        /// </summary>
        /// <remarks>
        ///   This structure is typically used in conjunction with a <see langword="using"/> statement
        ///   to guarantee state restoration even if an exception occurs.
        ///   If not used with an <see langword="using"/> statement, the caller is responsible for calling
        ///   <see cref="Dispose"/> to restore the resource state.
        /// </remarks>
        /// <param name="commandList">The Command List used to record the resource state transition operations.</param>
        /// <param name="Resource">The Graphics Resource to transition between states.</param>
        /// <param name="OldState">The original state to which the resource will be restored when this instance is disposed.</param>
        private readonly struct ResourceBarrierTransitionRestore(CommandList commandList, GraphicsResource Resource, GraphicsResourceState OldState) : IDisposable
        {
            /// <inheritdoc/>
            public readonly void Dispose()
            {
                commandList.ResourceBarrierTransition(Resource, OldState);
            }
        }

        #endregion
    }
}

#endif
