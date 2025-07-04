// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Diagnostics;

using Silk.NET.Core.Native;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;

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


        public static CommandList New(GraphicsDevice device)
        {
            return new CommandList(device);
        }

        private CommandList(GraphicsDevice device) : base(device)
        {
            Reset();
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            // Recycle heaps
            ResetSrvHeap(createNewHeap: false);
            ResetSamplerHeap(createNewHeap: false);

            // Available right now (NextFenceValue - 1)
            // TODO: Note that it won't be available right away because CommandAllocators is currently not using a PriorityQueue but a simple Queue
            if (currentCommandList.NativeCommandAllocator.IsNotNull())
            {
                GraphicsDevice.CommandAllocators.RecycleObject(GraphicsDevice.NextFenceValue - 1, currentCommandList.NativeCommandAllocator);
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

            base.OnDestroyed();
        }


        public partial void Reset()
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
            currentCommandList.NativeCommandAllocator = GraphicsDevice.CommandAllocators.GetObject();
            ResetCommandList();

            boundPipelineState = null;

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

        public void Flush()
        {
            var commandList = Close();
            GraphicsDevice.ExecuteCommandList(commandList);
        }

        /// <summary>
        /// Closes the command list for recording and returns an executable token.
        /// </summary>
        /// <returns>The executable command list.</returns>
        public partial CompiledCommandList Close()
        {
            FlushResourceBarriers();

            HResult result = currentCommandList.NativeCommandList.Close();

            if (result.IsFailure)
                result.Throw();

            // Staging resources not updated anymore
            foreach (var stagingResource in currentCommandList.StagingResources)
            {
                stagingResource.StagingBuilder = null;
            }

            // Recycle heaps
            ResetSrvHeap(createNewHeap: false);
            ResetSamplerHeap(createNewHeap: false);

            var commandList = currentCommandList;
            currentCommandList = default;
            return commandList;
        }

        /// <summary>
        /// Closes and executes the command list.
        /// </summary>


        private void FlushInternal(bool wait)
        {
            var commandList = Close();
            var fenceValue = GraphicsDevice.ExecuteCommandListInternal(commandList);

            if (wait)
                GraphicsDevice.WaitForFenceInternal(fenceValue);

            Reset();

            // Restore states
            if (boundPipelineState is not null)
                SetPipelineState(boundPipelineState);

            currentCommandList.NativeCommandList.SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        private partial void ClearStateImpl() { }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl() { }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="renderTargets">The render targets.</param>
        /// <exception cref="ArgumentNullException">renderTargetViews</exception>
        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, Texture[] renderTargets)
        {
            bool hasRenderTargetsToSet = renderTargetCount > 0 && renderTargets is { Length: > 0 };

            var renderTargetHandles = stackalloc CpuDescriptorHandle[renderTargetCount];

            for (int i = 0; i < renderTargetCount; ++i)
            {
                renderTargetHandles[i] = renderTargets[i].NativeRenderTargetView;
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
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
            // TODO: Implement stream output buffers
        }

        /// <summary>
        ///     Gets or sets the 1st viewport. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <value>The viewport.</value>
        private void SetViewportImpl()
        {
            if (!viewportDirty && !scissorsDirty)
                return;

            scoped ref var viewport = ref viewports[0];

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
                    scoped ref var scissor = ref scissors[0];
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
        ///     Prepares a draw call. This method is called before each Draw() method to setup the correct Primitive, InputLayout and VertexBuffers.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot GraphicsDevice.Draw*() without an effect being previously applied with Effect.Apply() method</exception>
        private void PrepareDraw()
        {
            FlushResourceBarriers();
            SetViewportImpl();
        }

        public void SetStencilReference(int stencilReference)
        {
            currentCommandList.NativeCommandList.OMSetStencilRef((uint) stencilReference);
        }

        public void SetBlendFactor(Color4 blendFactor)
        {
            scoped Span<float> blendFactorFloats = blendFactor.AsSpan<Color4, float>();

            currentCommandList.NativeCommandList.OMSetBlendFactor(blendFactorFloats);
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (boundPipelineState != pipelineState &&
                pipelineState is { CompiledState: not null })
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

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
        RestartWithNewHeap:
            var descriptorTableIndex = 0;

            for (int i = 0; i < descriptorSets.Length; ++i)
            {
                // Find what is already mapped
                scoped ref var descriptorSet = ref descriptorSets[i];

                var srvBindCount = boundPipelineState.SrvBindCounts[i];
                var samplerBindCount = boundPipelineState.SamplerBindCounts[i];

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
                srvHeap = GraphicsDevice.SrvHeaps.GetObject();
                srvHeapOffset = 0;
                srvMapping.Clear();
            }

            // Update the Descriptor heap array to reference the new active SRV heap
            descriptorHeaps[0] = srvHeap.Heap;
        }

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
                samplerHeap = GraphicsDevice.SamplerHeaps.GetObject();
                samplerHeapOffset = 0;
                samplerMapping.Clear();
            }

            // Update the Descriptor heap array to reference the new active Samplers heap
            descriptorHeaps[1] = samplerHeap.Heap;
        }

        /// <inheritdoc />
        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
            PrepareDraw(); // TODO: PrepareDraw for Compute dispatch?

            currentCommandList.NativeCommandList.Dispatch((uint) threadCountX, (uint) threadCountY, (uint) threadCountZ);
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            throw new NotImplementedException(); // TODO: Implement DispatchIndirect
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            currentCommandList.NativeCommandList.DrawInstanced((uint) vertexCount, InstanceCount: 1,
                                                               (uint) startVertexLocation, StartInstanceLocation: 0);

            GraphicsDevice.FrameTriangleCount += (uint)vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw geometry of an unknown size.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            throw new NotImplementedException(); // TODO: Implement DrawAuto
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

            currentCommandList.NativeCommandList.DrawIndexedInstanced((uint) indexCount, InstanceCount: 1,
                                                                      (uint) startIndexLocation, baseVertexLocation,
                                                                      StartInstanceLocation: 0);
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

            currentCommandList.NativeCommandList.DrawIndexedInstanced((uint) indexCountPerInstance,
                                                                      (uint) instanceCount,
                                                                      (uint) startIndexLocation, baseVertexLocation,
                                                                      (uint) startInstanceLocation);
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

            //NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);
            throw new NotImplementedException(); // TODO: Implement DrawIndexedInstancedIndirect

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

            currentCommandList.NativeCommandList.DrawInstanced((uint) vertexCountPerInstance, (uint) instanceCount,
                                                               (uint) startVertexLocation, (uint) startInstanceLocation);
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

            //NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);
            throw new NotImplementedException(); // TODO: Implement DrawInstancedIndirect

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Begins profiling.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            currentCommandList.NativeCommandList.EndQuery(queryPool.NativeQueryHeap, Silk.NET.Direct3D12.QueryType.Timestamp, (uint) index);

            queryPool.PendingValue = queryPool.CompletedValue + 1;
        }

        /// <summary>
        /// Ends profiling.
        /// </summary>
        public void BeginProfile(Color4 profileColor, string name)
        {
            //currentCommandList.NativeCommandList.BeginEvent();  // TODO: Implement profiling
        }

        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="index">The query index.</param>
        public void EndProfile()
        {
            //currentCommandList.NativeCommandList.EndEvent();  // TODO: Implement profiling
        }

        // TODO: Unused, remove?
        public void ResetQueryPool(QueryPool queryPool)
        {
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

            ResourceBarrierTransition(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsResourceState.DepthWrite);
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
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="ArgumentNullException">renderTarget</exception>
        public void Clear(Texture renderTarget, Color4 color)
        {
            ArgumentNullException.ThrowIfNull(renderTarget);

            ResourceBarrierTransition(renderTarget, GraphicsResourceState.RenderTarget);
            FlushResourceBarriers();

            scoped ref SilkBox2I nullRect = ref NullRef<SilkBox2I>();
            scoped ref var clearColorFloats = ref color.AsSpan<Color4, float>()[0];

            currentCommandList.NativeCommandList.ClearRenderTargetView(renderTarget.NativeRenderTargetView, ref clearColorFloats,
                                                                        NumRects: 0, in nullRect);
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
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
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
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
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
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
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting texture supporting UAV;texture</exception>
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
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting texture supporting UAV;texture</exception>
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
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting texture supporting UAV;texture</exception>
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
                    destinationParent.StagingFenceValue = null;
                    destinationParent.StagingBuilder = this;
                    currentCommandList.StagingResources.Add(destinationParent);
                }
                else // Copy Texture -> Texture
                {
                    CopyTextureToTexture(sourceTexture, destinationTexture);
                }
            }

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

                Core.Utilities.CopyWithAlignmentFallback(destinationMapped, sourceMapped, (uint) size);

                sourceTexture.NativeResource.Unmap(Subresource: 0, ref fullRange);
                destinationTexture.NativeResource.Unmap(Subresource: 0, ref fullRange);
            }

            void CopyTextureToStagingTexture(Texture sourceTexture, Texture sourceParent, Texture destinationTexture)
            {
                ResourceBarrierTransition(sourceTexture, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destinationTexture, GraphicsResourceState.CopyDestination);
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

            void CopyTextureToTexture(Texture sourceTexture, Texture destinationTexture)
            {
                ResourceBarrierTransition(sourceTexture, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destinationTexture, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyResource(destinationTexture.NativeResource, sourceTexture.NativeResource);
            }

            void CopyBetweenBuffers(Buffer sourceBuffer, Buffer destinationBuffer)
            {
                ResourceBarrierTransition(sourceBuffer, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destinationBuffer, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyResource(destinationBuffer.NativeResource, sourceBuffer.NativeResource);

                if (destinationBuffer.Usage == GraphicsResourceUsage.Staging)
                {
                    // Fence for host access
                    destinationBuffer.StagingFenceValue = null;
                    destinationBuffer.StagingBuilder = this;
                    currentCommandList.StagingResources.Add(destinationBuffer);
                }
            }
        }

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

            void CopyBetweenTextures(Texture sourceTexture, Texture destinationTexture)
            {
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging ||
                    destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    throw new NotImplementedException("Copy region of staging resources is not supported yet"); // TODO: Implement copy region for staging resources
                }

                ResourceBarrierTransition(source, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destination, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                var destRegion = new TextureCopyLocation
                {
                    PResource = destination.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) destinationSubResourceIndex
                };
                var srcRegion = new TextureCopyLocation
                {
                    PResource = source.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) sourceSubResourceIndex
                };

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

            void CopyBetweenBuffers(Buffer sourceBuffer, Buffer destinationBuffer)
            {
                ResourceBarrierTransition(source, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destination, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyBufferRegion(destinationBuffer.NativeResource, (ulong)dstX,
                                                                      sourceBuffer.NativeResource,
                                                                      SrcOffset: (ulong)(sourceRegion?.Left ?? 0),
                                                                      NumBytes: sourceRegion.HasValue
                                                                        ? (ulong)(sourceRegion.Value.Right - sourceRegion.Value.Left)
                                                                        : (ulong) sourceBuffer.SizeInBytes);
            }
        }

        /// <inheritdoc />
        public void CopyCount(Buffer sourceBuffer, Buffer destinationBuffer, int destinationOffsetInBytes)
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destinationBuffer);

            currentCommandList.NativeCommandList.CopyBufferRegion(destinationBuffer.NativeResource, (ulong) destinationOffsetInBytes,
                                                                  sourceBuffer.NativeResource, SrcOffset: 0, NumBytes: sizeof(uint));
        }

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

        internal unsafe void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData, ResourceRegion region)
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

                GraphicsDevice.TemporaryResources.Enqueue((GraphicsDevice.NextFenceValue, nativeUploadTexture));

                scoped ref var fullBox = ref NullRef<D3D12Box>();

                result = nativeUploadTexture.WriteToSubresource(DstSubresource: 0, in fullBox,
                                                                (void*) sourceData.DataPointer, (uint) sourceData.RowPitch, (uint) sourceData.SlicePitch);
                if (result.IsFailure)
                    result.Throw();

                // Trigger copy
                ResourceBarrierTransition(resource, GraphicsResourceState.CopyDestination);
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

            void UpdateBuffer()
            {
                var uploadSize = region.Right - region.Left;
                var uploadMemory = GraphicsDevice.AllocateUploadBuffer(uploadSize, out var uploadResource, out var uploadOffset);

                Core.Utilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) sourceData.DataPointer, (uint) uploadSize);

                ResourceBarrierTransition(resource, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList.CopyBufferRegion(pDstBuffer: resource.NativeResource, DstOffset: (ulong)region.Left,
                                                                      uploadResource, (ulong)uploadOffset, (ulong)uploadSize);
            }
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        /// <summary>
        /// Maps a subresource.
        /// </summary>
        /// <param name="resource">The resource.</param>
        /// <param name="subresourceIndex">Index of the sub resource.</param>
        /// <param name="mapMode">The map mode.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <param name="offsetInBytes">The offset information in bytes.</param>
        /// <param name="lengthInBytes">The length information in bytes.</param>
        /// <returns>Pointer to the sub resource to map.</returns>
        public MappedResource MapSubResource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            ArgumentNullException.ThrowIfNull(resource);

            var rowPitch = 0;
            var depthStride = 0;
            var usage = GraphicsResourceUsage.Default;

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
                // Is non-staging ever possible for Read/Write?
                if (usage != GraphicsResourceUsage.Staging)
                    throw new ArgumentException("Read/Write/ReadWrite is only supported for staging resources", nameof(mapMode));
            }
            else if (mapMode == MapMode.WriteDiscard)
            {
                throw new ArgumentException($"Can't use {nameof(MapMode)}.{nameof(MapMode.WriteDiscard)} on Graphics APIs that don't support renaming", nameof(mapMode));
            }

            if (mapMode != MapMode.WriteNoOverwrite)
            {
                // Need to wait?
                if (resource.StagingFenceValue is null || !GraphicsDevice.IsFenceCompleteInternal(resource.StagingFenceValue.Value))
                {
                    if (doNotWait)
                    {
                        return new MappedResource(resource, subResourceIndex, dataBox: default);
                    }

                    // Need to flush? (i.e. part of)
                    if (resource.StagingBuilder == this)
                        FlushInternal(false);

                    // If the resource is not being updated by this command list, we need to wait for the fence
                    if (resource.StagingFenceValue is null)
                        throw new InvalidOperationException("CommandList updating the staging resource has not been submitted");

                    GraphicsDevice.WaitForFenceInternal(resource.StagingFenceValue.Value);
                }
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
        public void UnmapSubResource(MappedResource unmapped)
        {
            scoped ref var fullRange = ref NullRef<D3D12Range>();

            unmapped.Resource.NativeResource.Unmap((uint) unmapped.SubResourceIndex, pWrittenRange: in fullRange);
        }

        #region DescriptorHeapWrapper structure

        /// <summary>
        ///   Contains a DescriptorHeap and cache its GPU and CPU pointers.
        /// </summary>
        private struct DescriptorHeapWrapper
        {
            public ComPtr<ID3D12DescriptorHeap> Heap;

            public CpuDescriptorHandle CPUDescriptorHandleForHeapStart;
            public GpuDescriptorHandle GPUDescriptorHandleForHeapStart;


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
    }
}

#endif
