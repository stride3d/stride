// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_DIRECT3D12

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Silk.NET.Core.Native;
using Silk.NET.Maths;
using Silk.NET.DXGI;
using Silk.NET.Direct3D12;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public unsafe partial class CommandList
    {
        private DescriptorHeapCache srvHeap;
        private int srvHeapOffset = GraphicsDevice.SrvHeapSize;
        private DescriptorHeapCache samplerHeap;
        private int samplerHeapOffset = GraphicsDevice.SamplerHeapSize;

        private PipelineState boundPipelineState;
        private readonly ID3D12DescriptorHeap*[] descriptorHeaps = new ID3D12DescriptorHeap*[2];
        private readonly List<ResourceBarrier> resourceBarriers = new(16);

        private readonly Dictionary<nuint, GpuDescriptorHandle> srvMapping = new();
        private readonly Dictionary<nuint, GpuDescriptorHandle> samplerMapping = new();

        internal readonly Queue<CommandListPtr> NativeCommandLists = new();

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

        private void ResetCommandList()
        {
            if (NativeCommandLists.Count > 0)
            {
                currentCommandList.NativeCommandList = NativeCommandLists.Dequeue();

                HResult result = currentCommandList.NativeCommandList->Reset(currentCommandList.NativeCommandAllocator, pInitialState: null);

                if (result.IsFailure)
                    result.Throw();
            }
            else
            {
                ID3D12GraphicsCommandList* commandList;
                var commandAllocator = currentCommandList.NativeCommandAllocator;
                HResult result = NativeDevice->CreateCommandList(nodeMask: 0, CommandListType.Direct, commandAllocator,
                                                                 pInitialState: null, SilkMarshal.GuidPtrOf<ID3D12GraphicsCommandList>(),
                                                                 (void**) &commandList);
                if (result.IsFailure)
                    result.Throw();

                currentCommandList.NativeCommandList = commandList;
            }

            currentCommandList.NativeCommandList->SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            // Recycle heaps
            ResetSrvHeap(createNewHeap: false);
            ResetSamplerHeap(createNewHeap: false);

            // Available right now (NextFenceValue - 1)
            // TODO: Note that it won't be available right away because CommandAllocators is currently not using a PriorityQueue but a simple Queue
            if (currentCommandList.NativeCommandAllocator != null)
            {
                GraphicsDevice.CommandAllocators.RecycleObject(GraphicsDevice.NextFenceValue - 1, currentCommandList.NativeCommandAllocator);
                currentCommandList.NativeCommandAllocator = null;
            }

            if (currentCommandList.NativeCommandList != null)
            {
                NativeCommandLists.Enqueue(currentCommandList.NativeCommandList);
                currentCommandList.NativeCommandList = null;
            }

            while (NativeCommandLists.Count > 0)
            {
                var commandList = NativeCommandLists.Dequeue().CommandList;
                commandList->Release();
            }

            base.OnDestroyed();
        }

        public void Reset()
        {
            if (currentCommandList.Builder != null)
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
        }

        /// <summary>
        /// Closes the command list for recording and returns an executable token.
        /// </summary>
        /// <returns>The executable command list.</returns>
        public CompiledCommandList Close()
        {
            FlushResourceBarriers();

            HResult result = currentCommandList.NativeCommandList->Close();

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
        public void Flush()
        {
            var commandList = Close();
            GraphicsDevice.ExecuteCommandList(commandList);
        }

        private void FlushInternal(bool wait)
        {
            var commandList = Close();
            var fenceValue = GraphicsDevice.ExecuteCommandListInternal(commandList);

            if (wait)
                GraphicsDevice.WaitForFenceInternal(fenceValue);

            Reset();

            // Restore states
            if (boundPipelineState != null)
                SetPipelineState(boundPipelineState);

            currentCommandList.NativeCommandList->SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        private void ClearStateImpl()
        {
        }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl()
        {
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="renderTargets">The render targets.</param>
        /// <exception cref="ArgumentNullException">renderTargetViews</exception>
        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, Texture[] renderTargets)
        {
            var renderTargetHandles = stackalloc CpuDescriptorHandle[renderTargetCount];

            for (int i = 0; i < renderTargetCount; ++i)
            {
                renderTargetHandles[i] = renderTargets[i].NativeRenderTargetView;
            }

            var depthStencilView = depthStencilBuffer is null ? default : depthStencilBuffer.NativeDepthStencilView;

            currentCommandList.NativeCommandList->OMSetRenderTargets(NumRenderTargetDescriptors: (uint) renderTargetCount, renderTargetHandles,
                                                                     RTsSingleHandleToDescriptorRange: false,
                                                                     depthStencilView);
        }

        /// <summary>
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
        }

        /// <summary>
        ///     Gets or sets the 1st viewport. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <value>The viewport.</value>
        private void SetViewportImpl()
        {
            if (!viewportDirty && !scissorsDirty)
                return;

            ref var viewport = ref viewports[0];

            if (viewportDirty)
            {
                // NOTE: We assume the same layout and size as DIRECT3D12_VIEWPORT struct
                ref var d3dViewport = ref Unsafe.As<Viewport, Silk.NET.Direct3D12.Viewport>(ref viewport);
                currentCommandList.NativeCommandList->RSSetViewports(NumViewports: 1, in d3dViewport);

                var scissorRect = new Box2D<int>
                {
                    Min = { X = (int) viewport.X, Y = (int) viewport.Y },
                    Max = { X = (int) (viewport.X + viewport.Width), Y = (int) (viewport.Y + viewport.Height) }
                };
                currentCommandList.NativeCommandList->RSSetScissorRects(NumRects: 1, scissorRect);
                viewportDirty = false;
            }

            if (boundPipelineState?.HasScissorEnabled ?? false)
            {
                if (scissorsDirty)
                {
                    // Use manual scissor
                    ref var scissor = ref scissors[0];
                    var scissorRect = new Box2D<int>
                    {
                        Min = { X = scissor.X, Y = scissor.Y },
                        Max = { X = scissor.Right, Y = scissor.Bottom }
                    };
                    currentCommandList.NativeCommandList->RSSetScissorRects(NumRects: 1, scissorRect);
                }
            }
            else
            {
                // Use viewport
                // Always update, because either scissor or viewport was dirty and we use viewport size
                var scissorRect = new Box2D<int>
                {
                    Min = { X = (int) viewport.X, Y = (int) viewport.Y },
                    Max = { X = (int) (viewport.X + viewport.Width), Y = (int) (viewport.Y + viewport.Height) }
                };
                currentCommandList.NativeCommandList->RSSetScissorRects(NumRects: 1, scissorRect);
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
            currentCommandList.NativeCommandList->OMSetStencilRef((uint) stencilReference);
        }

        public void SetBlendFactor(Color4 blendFactor)
        {
            var pBlendFactor = (float*) &blendFactor;
            currentCommandList.NativeCommandList->OMSetBlendFactor(pBlendFactor);
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (boundPipelineState != pipelineState &&
                pipelineState != null && pipelineState.CompiledState != null)
            {
                // If scissor state changed, force a refresh
                scissorsDirty |= (boundPipelineState?.HasScissorEnabled ?? false) != pipelineState.HasScissorEnabled;

                currentCommandList.NativeCommandList->SetPipelineState(pipelineState.CompiledState);

                if (pipelineState.IsCompute)
                    currentCommandList.NativeCommandList->SetComputeRootSignature(pipelineState.RootSignature);
                else
                    currentCommandList.NativeCommandList->SetGraphicsRootSignature(pipelineState.RootSignature);

                boundPipelineState = pipelineState;
                currentCommandList.NativeCommandList->IASetPrimitiveTopology(pipelineState.PrimitiveTopology);
            }
        }

        public void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            if (buffer is null)
            {
                currentCommandList.NativeCommandList->IASetVertexBuffers(StartSlot: (uint) index, NumViews: 1, null);
                return;
            }

            VertexBufferView vbView = new()
            {
                BufferLocation = buffer.NativeResource->GetGPUVirtualAddress() + (ulong) offset,
                StrideInBytes = (uint) stride,
                SizeInBytes = (uint) (buffer.SizeInBytes - offset)
            };
            currentCommandList.NativeCommandList->IASetVertexBuffers(StartSlot: (uint) index, NumViews: 1, in vbView);
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32Bits)
        {
            if (buffer is null)
            {
                currentCommandList.NativeCommandList->IASetIndexBuffer(null);
                return;
            }

            IndexBufferView ibView = new()
            {
                BufferLocation = buffer.NativeResource->GetGPUVirtualAddress() + (ulong) offset,
                Format = is32Bits ? Format.FormatR32Uint : Format.FormatR16Uint,
                SizeInBytes = (uint) (buffer.SizeInBytes - offset)
            };
            currentCommandList.NativeCommandList->IASetIndexBuffer(ibView);
        }

        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            // Find parent resource
            if (resource.ParentResource != null)
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

            var barriers = stackalloc ResourceBarrier[count];
            for (int i = 0; i < count; i++)
                barriers[i] = resourceBarriers[i];

            resourceBarriers.Clear();

            currentCommandList.NativeCommandList->ResourceBarrier(NumBarriers: (uint) count, barriers);
        }

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
        RestartWithNewHeap:
            var descriptorTableIndex = 0;

            for (int i = 0; i < descriptorSets.Length; ++i)
            {
                // Find what is already mapped
                ref var descriptorSet = ref descriptorSets[i];

                var srvBindCount = boundPipelineState.SrvBindCounts[i];
                var samplerBindCount = boundPipelineState.SamplerBindCounts[i];

                if (srvBindCount > 0 && descriptorSet.SrvStart.Ptr != 0)
                {
                    // Check if we need to copy them to shader visible descriptor heap
                    if (!srvMapping.TryGetValue(descriptorSet.SrvStart.Ptr, out var gpuSrvStart))
                    {
                        var srvCount = descriptorSet.Description.SrvCount;

                        // Make sure heap is big enough
                        if (srvHeapOffset + srvCount > GraphicsDevice.SrvHeapSize)
                        {
                            ResetSrvHeap(createNewHeap: true);

                            currentCommandList.NativeCommandList->SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
                            goto RestartWithNewHeap;
                        }

                        // Copy
                        var destHandle = new CpuDescriptorHandle(srvHeap.CPUDescriptorHandleForHeapStart.Ptr +
                                                                 (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                        NativeDevice->CopyDescriptorsSimple((uint) srvCount, destHandle, descriptorSet.SrvStart, DescriptorHeapType.CbvSrvUav);

                        // Store mapping
                        gpuSrvStart = new GpuDescriptorHandle(srvHeap.GPUDescriptorHandleForHeapStart.Ptr +
                                                              (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                        srvMapping.Add(descriptorSet.SrvStart.Ptr, gpuSrvStart);

                        // Bump
                        srvHeapOffset += srvCount;
                    }

                    // Bind resource tables (note: once per using stage, until we solve how to choose shader registers effect-wide at compile time)
                    if (IsComputePipelineStateBound)
                    {
                        for (int j = 0; j < srvBindCount; ++j)
                            currentCommandList.NativeCommandList->SetComputeRootDescriptorTable((uint) descriptorTableIndex++, gpuSrvStart);
                    }
                    else
                    {
                        for (int j = 0; j < srvBindCount; ++j)
                            currentCommandList.NativeCommandList->SetGraphicsRootDescriptorTable((uint) descriptorTableIndex++, gpuSrvStart);
                    }
                }

                if (samplerBindCount > 0 && descriptorSet.SamplerStart.Ptr != 0)
                {
                    // Check if we need to copy them to shader visible descriptor heap
                    if (!samplerMapping.TryGetValue(descriptorSet.SamplerStart.Ptr, out var gpuSamplerStart))
                    {
                        var samplerCount = descriptorSet.Description.SamplerCount;

                        // Make sure heap is big enough
                        if (samplerHeapOffset + samplerCount > GraphicsDevice.SamplerHeapSize)
                        {
                            ResetSamplerHeap(createNewHeap: true);

                            currentCommandList.NativeCommandList->SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
                            goto RestartWithNewHeap;
                        }

                        // Copy
                        var destHandle = new CpuDescriptorHandle(samplerHeap.CPUDescriptorHandleForHeapStart.Ptr +
                                                                 (nuint) (samplerHeapOffset * GraphicsDevice.SamplerHandleIncrementSize));

                        NativeDevice->CopyDescriptorsSimple((uint) samplerCount, destHandle, descriptorSet.SamplerStart, DescriptorHeapType.Sampler);

                        // Store mapping
                        gpuSamplerStart = new GpuDescriptorHandle(samplerHeap.GPUDescriptorHandleForHeapStart.Ptr +
                                                                  (nuint) (samplerHeapOffset * GraphicsDevice.SamplerHandleIncrementSize));

                        samplerMapping.Add(descriptorSet.SamplerStart.Ptr, gpuSamplerStart);

                        // Bump
                        samplerHeapOffset += samplerCount;
                    }

                    // Bind resource tables (note: once per using stage, until we solve how to choose shader registers effect-wide at compile time)
                    if (IsComputePipelineStateBound)
                    {
                        for (int j = 0; j < samplerBindCount; ++j)
                            currentCommandList.NativeCommandList->SetComputeRootDescriptorTable((uint) descriptorTableIndex++, gpuSamplerStart);
                    }
                    else
                    {
                        for (int j = 0; j < samplerBindCount; ++j)
                            currentCommandList.NativeCommandList->SetGraphicsRootDescriptorTable((uint) descriptorTableIndex++, gpuSamplerStart);
                    }
                }
            }
        }

        private void ResetSrvHeap(bool createNewHeap)
        {
            if (srvHeap.Heap != null)
            {
                currentCommandList.SrvHeaps.Add(srvHeap.Heap);
                srvHeap.Heap = null;
            }

            if (createNewHeap)
            {
                srvHeap = new DescriptorHeapCache(GraphicsDevice.SrvHeaps.GetObject());
                srvHeapOffset = 0;
                srvMapping.Clear();
            }

            descriptorHeaps[0] = srvHeap.Heap;
        }

        private void ResetSamplerHeap(bool createNewHeap)
        {
            if (samplerHeap.Heap != null)
            {
                currentCommandList.SamplerHeaps.Add(samplerHeap.Heap);
                samplerHeap.Heap = null;
            }

            if (createNewHeap)
            {
                samplerHeap = new DescriptorHeapCache(GraphicsDevice.SamplerHeaps.GetObject());
                samplerHeapOffset = 0;
                samplerMapping.Clear();
            }

            descriptorHeaps[1] = samplerHeap.Heap;
        }

        /// <inheritdoc />
        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
            PrepareDraw();

            currentCommandList.NativeCommandList->Dispatch((uint) threadCountX, (uint) threadCountY, (uint) threadCountZ);
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            currentCommandList.NativeCommandList->DrawInstanced((uint) vertexCount, InstanceCount: 1,
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

            throw new NotImplementedException();
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

            currentCommandList.NativeCommandList->DrawIndexedInstanced((uint) indexCount, InstanceCount: 1,
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

            currentCommandList.NativeCommandList->DrawIndexedInstanced((uint) indexCountPerInstance,
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
            throw new NotImplementedException();

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

            currentCommandList.NativeCommandList->DrawInstanced((uint) vertexCountPerInstance, (uint) instanceCount,
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
            throw new NotImplementedException();

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Begins profiling.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public void BeginProfile(Color4 profileColor, string name)
        {
            //currentCommandList.NativeCommandList.BeginEvent();
        }

        /// <summary>
        /// Ends profiling.
        /// </summary>
        public void EndProfile()
        {
            //currentCommandList.NativeCommandList.EndEvent();
        }

        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="index">The query index.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            currentCommandList.NativeCommandList->EndQuery(queryPool.NativeQueryHeap, Silk.NET.Direct3D12.QueryType.Timestamp, (uint) index);

            queryPool.PendingValue = queryPool.CompletedValue + 1;
        }

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
            ResourceBarrierTransition(GraphicsDevice.Presenter.DepthStencilBuffer, GraphicsResourceState.DepthWrite);
            FlushResourceBarriers();

            currentCommandList.NativeCommandList->ClearDepthStencilView(depthStencilBuffer.NativeDepthStencilView,
                                                                        (ClearFlags) options, depth, stencil,
                                                                        NumRects: 0, pRects: null);
        }

        /// <summary>
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="ArgumentNullException">renderTarget</exception>
        public void Clear(Texture renderTarget, Color4 color)
        {
            ResourceBarrierTransition(renderTarget, GraphicsResourceState.RenderTarget);
            FlushResourceBarriers();

            var clearColor = (float*) &color;

            currentCommandList.NativeCommandList->ClearRenderTargetView(renderTarget.NativeRenderTargetView, clearColor,
                                                                        NumRects: 0, pRects: null);
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
                throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));

            var cpuHandle = buffer.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            var clearValue = (float*) &value;

            currentCommandList.NativeCommandList->ClearUnorderedAccessViewFloat(gpuHandle, cpuHandle, buffer.NativeResource,
                                                                                clearValue, NumRects: 0, pRects: null);
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
                throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));

            var cpuHandle = buffer.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            var clearValue = (uint*) &value;

            currentCommandList.NativeCommandList->ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, buffer.NativeResource,
                                                                               clearValue, NumRects: 0, pRects: null);
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
                throw new ArgumentException("Expecting buffer supporting UAV", nameof(buffer));

            var cpuHandle = buffer.NativeUnorderedAccessView;
            var gpuHandle = GetGpuDescriptorHandle(cpuHandle);

            var clearValue = (uint*) &value;

            currentCommandList.NativeCommandList->ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, buffer.NativeResource,
                                                                               clearValue, NumRects: 0, pRects: null);
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

            var clearValue = (float*) &value;

            currentCommandList.NativeCommandList->ClearUnorderedAccessViewFloat(gpuHandle, cpuHandle, texture.NativeResource,
                                                                                clearValue, NumRects: 0, pRects: null);
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

            var clearValue = (uint*) &value;

            currentCommandList.NativeCommandList->ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, texture.NativeResource,
                                                                               clearValue, NumRects: 0, pRects: null);
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

            var clearValue = (uint*) &value;

            currentCommandList.NativeCommandList->ClearUnorderedAccessViewUint(gpuHandle, cpuHandle, texture.NativeResource,
                                                                               clearValue, NumRects: 0, pRects: null);
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

                    currentCommandList.NativeCommandList->SetDescriptorHeaps(NumDescriptorHeaps: 2, in descriptorHeaps[0]);
                }

                // Copy
                var destHandle = new CpuDescriptorHandle(srvHeap.CPUDescriptorHandleForHeapStart.Ptr +
                                                         (nuint) (srvHeapOffset * GraphicsDevice.SrvHandleIncrementSize));

                NativeDevice->CopyDescriptorsSimple((uint) srvCount, destHandle, cpuHandle, DescriptorHeapType.CbvSrvUav);

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
            // Copy texture -> texture
            if (source is Texture sourceTexture &&
                destination is Texture destinationTexture)
            {
                var sourceParent = sourceTexture.ParentTexture ?? sourceTexture;
                var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    // Copy staging texture -> staging texture
                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        var size = destinationTexture.ComputeBufferTotalSize();

                        void* destinationMapped;
                        HResult result = destinationTexture.NativeResource->Map(Subresource: 0, pReadRange: null, &destinationMapped);

                        if (result.IsFailure)
                            result.Throw();

                        void* sourceMapped;
                        var sourceRange = new Silk.NET.Direct3D12.Range { Begin = 0, End = (nuint) size };
                        result = sourceTexture.NativeResource->Map(Subresource: 0, sourceRange, &sourceMapped);

                        if (result.IsFailure)
                            result.Throw();

                        Core.Utilities.CopyWithAlignmentFallback(destinationMapped, sourceMapped, (uint) size);

                        sourceTexture.NativeResource->Unmap(Subresource: 0, pWrittenRange: null);
                        destinationTexture.NativeResource->Unmap(Subresource: 0, pWrittenRange: null);
                    }
                    else
                    {
                        ResourceBarrierTransition(sourceTexture, GraphicsResourceState.CopySource);
                        ResourceBarrierTransition(destinationTexture, GraphicsResourceState.CopyDestination);
                        FlushResourceBarriers();

                        int copyOffset = 0;
                        for (int arraySlice = 0; arraySlice < sourceParent.ArraySize; ++arraySlice)
                        {
                            for (int mipLevel = 0; mipLevel < sourceParent.MipLevels; ++mipLevel)
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
                                    SubresourceIndex = (uint) (arraySlice * sourceParent.MipLevels + mipLevel)
                                };

                                currentCommandList.NativeCommandList->CopyTextureRegion(destRegion, DstX: 0, DstY: 0, DstZ: 0,
                                                                                        srcRegion, pSrcBox: null);

                                copyOffset += destinationTexture.ComputeSubresourceSize(mipLevel);
                            }
                        }
                    }

                    // Fence for host access
                    destinationParent.StagingFenceValue = null;
                    destinationParent.StagingBuilder = this;
                    currentCommandList.StagingResources.Add(destinationParent);
                }
                else
                {
                    ResourceBarrierTransition(sourceTexture, GraphicsResourceState.CopySource);
                    ResourceBarrierTransition(destinationTexture, GraphicsResourceState.CopyDestination);
                    FlushResourceBarriers();

                    currentCommandList.NativeCommandList->CopyResource(destinationTexture.NativeResource, sourceTexture.NativeResource);
                }
            }
            // Copy buffer -> buffer
            else if (source is Buffer sourceBuffer &&
                     destination is Buffer destinationBuffer)
            {
                ResourceBarrierTransition(sourceBuffer, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destinationBuffer, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList->CopyResource(destinationBuffer.NativeResource, sourceBuffer.NativeResource);

                if (destinationBuffer.Usage == GraphicsResourceUsage.Staging)
                {
                    // Fence for host access
                    destinationBuffer.StagingFenceValue = null;
                    destinationBuffer.StagingBuilder = this;
                    currentCommandList.StagingResources.Add(destinationBuffer);
                }
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            ArgumentNullException.ThrowIfNull(sourceMultisampleTexture);
            ArgumentNullException.ThrowIfNull(destTexture);

            if (!sourceMultisampleTexture.IsMultisample)
                throw new ArgumentOutOfRangeException(nameof(sourceMultisampleTexture), "Source texture is not a MSAA texture");

            currentCommandList.NativeCommandList->ResolveSubresource(sourceMultisampleTexture.NativeResource, (uint) sourceSubResource,
                                                                     destTexture.NativeResource, (uint) destSubResource,
                                                                     (Format)(format == PixelFormat.None ? destTexture.Format : format));
        }

        public void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourceRegion, GraphicsResource destination, int destinationSubresource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            if (source is Texture sourceTexture &&
                destination is Texture destinationTexture)
            {
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging ||
                    destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    throw new NotImplementedException("Copy region of staging resources is not supported yet");
                }

                ResourceBarrierTransition(source, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destination, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                var destRegion = new TextureCopyLocation
                {
                    PResource = destination.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) destinationSubresource
                };
                var srcRegion = new TextureCopyLocation
                {
                    PResource = source.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) sourceSubresource
                };

                if (sourceRegion is ResourceRegion srcResourceRegion)
                {
                    // NOTE: We assume the same layout and size as D3D12_BOX
                    var srcBox = (Box*) &srcResourceRegion;

                    currentCommandList.NativeCommandList->CopyTextureRegion(destRegion, (uint)dstX, (uint)dstY, (uint)dstZ,
                                                        srcRegion, srcBox);
                }
                else
                {
                    currentCommandList.NativeCommandList->CopyTextureRegion(destRegion, (uint) dstX, (uint) dstY, (uint) dstZ,
                                                                            srcRegion, pSrcBox: null);
                }
            }
            else if (source is Buffer sourceBuffer &&
                     destination is Buffer)
            {
                ResourceBarrierTransition(source, GraphicsResourceState.CopySource);
                ResourceBarrierTransition(destination, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList->CopyBufferRegion(destination.NativeResource, (ulong) dstX,
                                                                       source.NativeResource,
                                                                       SrcOffset: (ulong) (sourceRegion?.Left ?? 0),
                                                                       NumBytes: sourceRegion.HasValue
                                                                        ? (ulong) (sourceRegion.Value.Right - sourceRegion.Value.Left)
                                                                        : (ulong) sourceBuffer.SizeInBytes);
            }
            else
            {
                throw new InvalidOperationException("Cannot copy data between buffer and texture.");
            }
        }

        /// <inheritdoc />
        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetInBytes)
        {
            ArgumentNullException.ThrowIfNull(sourceBuffer);
            ArgumentNullException.ThrowIfNull(destBuffer);

            currentCommandList.NativeCommandList->CopyBufferRegion(destBuffer.NativeResource, (ulong) offsetInBytes,
                                                                   sourceBuffer.NativeResource, SrcOffset: 0, NumBytes: sizeof(uint));
        }

        internal void UpdateSubresource(GraphicsResource resource, int subresourceIndex, DataBox databox)
        {
            ResourceRegion region = resource switch
            {
                Texture texture => new ResourceRegion(left: 0, top: 0, front: 0, texture.Width, texture.Height, texture.Depth),
                Buffer buffer => new ResourceRegion(left: 0, top: 0, front: 0, buffer.SizeInBytes, bottom: 1, back: 1),

                _ => throw new InvalidOperationException("Unknown resource type")
            };

            UpdateSubresource(resource, subresourceIndex, databox, region);
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subresourceIndex, DataBox databox, ResourceRegion region)
        {
            if (resource is Texture texture)
            {
                var width = region.Right - region.Left;
                var height = region.Bottom - region.Top;
                var depth = region.Back - region.Front;

                ResourceDesc resourceDescription;
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
                        throw new ArgumentOutOfRangeException();
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

                ID3D12Resource* nativeUploadTexture;
                HResult result = NativeDevice->CreateCommittedResource(heap, HeapFlags.None,
                                                                       resourceDescription, ResourceStates.GenericRead,
                                                                       pOptimizedClearValue: null, SilkMarshal.GuidPtrOf<ID3D12Resource>(),
                                                                       (void**) &nativeUploadTexture);
                if (result.IsFailure)
                    result.Throw();

                GraphicsDevice.TemporaryResources.Enqueue((GraphicsDevice.NextFenceValue, new ResourcePtr(nativeUploadTexture)));

                result = nativeUploadTexture->WriteToSubresource(DstSubresource: 0, pDstBox: null,
                                                                 (void*) databox.DataPointer, (uint) databox.RowPitch, (uint) databox.SlicePitch);
                if (result.IsFailure)
                    result.Throw();

                // Trigger copy
                ResourceBarrierTransition(resource, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                var destRegion = new TextureCopyLocation
                {
                    PResource = resource.NativeResource,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = (uint) subresourceIndex
                };
                var srcRegion = new TextureCopyLocation
                {
                    PResource = nativeUploadTexture,
                    Type = TextureCopyType.SubresourceIndex,
                    SubresourceIndex = 0
                };
                currentCommandList.NativeCommandList->CopyTextureRegion(destRegion, (uint) region.Left, (uint) region.Top, (uint) region.Front,
                                                                        srcRegion, pSrcBox: null);
            }
            else if (resource is Buffer)
            {
                var uploadSize = region.Right - region.Left;
                var uploadMemory = GraphicsDevice.AllocateUploadBuffer(region.Right - region.Left, out var uploadResource, out var uploadOffset);

                Unsafe.CopyBlockUnaligned((void*) uploadMemory, (void*) databox.DataPointer, (uint) uploadSize);

                ResourceBarrierTransition(resource, GraphicsResourceState.CopyDestination);
                FlushResourceBarriers();

                currentCommandList.NativeCommandList->CopyBufferRegion(pDstBuffer: resource.NativeResource, DstOffset: (ulong) region.Left,
                                                                       uploadResource, (ulong) uploadOffset, (ulong) uploadSize);
            }
            else
            {
                if (resource is Buffer)
                {
                    var uploadSize = region.Right - region.Left;
                    var uploadMemory = GraphicsDevice.AllocateUploadBuffer(uploadSize, out var uploadResource, out var uploadOffset);

                    Core.Utilities.CopyWithAlignmentFallback((void*) uploadMemory, (void*) databox.DataPointer, (uint) uploadSize);

                    ResourceBarrierTransition(resource, GraphicsResourceState.CopyDestination);
                    FlushResourceBarriers();

                    currentCommandList.NativeCommandList.CopyBufferRegion(resource.NativeResource, region.Left, uploadResource, uploadOffset, uploadSize);
                }
                else
                {
                    throw new InvalidOperationException("Unknown resource type");
                }
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
        public MappedResource MapSubresource(GraphicsResource resource, int subresourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            ArgumentNullException.ThrowIfNull(resource);

            var rowPitch = 0;
            var depthStride = 0;
            var usage = GraphicsResourceUsage.Default;

            if (resource is Texture texture)
            {
                usage = texture.Usage;

                if (lengthInBytes == 0)
                    lengthInBytes = texture.ComputeSubresourceSize(subresourceIndex);

                rowPitch = texture.ComputeRowPitch(subresourceIndex % texture.MipLevels);
                depthStride = texture.ComputeSlicePitch(subresourceIndex % texture.MipLevels);

                if (usage == GraphicsResourceUsage.Staging)
                {
                    // Internally it's a buffer, so adapt resource index and offset
                    offsetInBytes = texture.ComputeBufferOffset(subresourceIndex, depthSlice: 0);
                    subresourceIndex = 0;
                }
            }
            else if (resource is Buffer buffer)
            {
                usage = buffer.Usage;

                if (lengthInBytes == 0)
                    lengthInBytes = buffer.SizeInBytes;
            }

            if (mapMode is MapMode.Read or MapMode.ReadWrite or MapMode.Write)
            {
                // Is non-staging ever possible for Read/Write?
                if (usage != GraphicsResourceUsage.Staging)
                    throw new InvalidOperationException();
            }
            else if (mapMode == MapMode.WriteDiscard)
            {
                throw new InvalidOperationException("Can't use WriteDiscard on Graphics API that don't support renaming");
            }

            if (mapMode != MapMode.WriteNoOverwrite)
            {
                // Need to wait?
                if (resource.StagingFenceValue is null || !GraphicsDevice.IsFenceCompleteInternal(resource.StagingFenceValue.Value))
                {
                    if (doNotWait)
                    {
                        return new MappedResource(resource, subresourceIndex, dataBox: default);
                    }

                    // Need to flush? (i.e. part of)
                    if (resource.StagingBuilder == this)
                        FlushInternal(false);

                    if (!resource.StagingFenceValue.HasValue)
                        throw new InvalidOperationException("CommandList updating the staging resource has not been submitted");

                    GraphicsDevice.WaitForFenceInternal(resource.StagingFenceValue.Value);
                }
            }

            void* mappedMemory;
            HResult result = resource.NativeResource->Map((uint) subresourceIndex, pReadRange: null, &mappedMemory);

            if (result.IsFailure)
                result.Throw();

            var mappedData = new DataBox((IntPtr)((byte*) mappedMemory + offsetInBytes), rowPitch, depthStride);
            return new MappedResource(resource, subresourceIndex, mappedData, offsetInBytes, lengthInBytes);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public void UnmapSubresource(MappedResource unmapped)
        {
            unmapped.Resource.NativeResource->Unmap((uint) unmapped.SubResourceIndex, pWrittenRange: null);
        }

        /// <summary>
        ///   Contains a DescriptorHeap and cache its GPU and CPU pointers.
        /// </summary>
        private struct DescriptorHeapCache
        {
            public DescriptorHeapCache(ID3D12DescriptorHeap* heap) : this()
            {
                Heap = heap;
                if (heap != null)
                {
                    CPUDescriptorHandleForHeapStart = heap->GetCPUDescriptorHandleForHeapStart();
                    GPUDescriptorHandleForHeapStart = heap->GetGPUDescriptorHandleForHeapStart();
                }
            }

            public ID3D12DescriptorHeap* Heap;
            public CpuDescriptorHandle CPUDescriptorHandleForHeapStart;
            public GpuDescriptorHandle GPUDescriptorHandleForHeapStart;
        }
    }
}

#endif
