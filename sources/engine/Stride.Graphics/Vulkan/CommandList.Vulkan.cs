// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Stride.Graphics
{
    public partial class CommandList
    {
        /// <summary>
        ///   Unique ID for this command list, incremented on each Reset.
        ///   Used to detect when a resource is first used on a different command list,
        ///   requiring barriers to be re-issued even if the layout already matches.
        /// </summary>
        internal int CommandListId;

        internal CommandBufferPool CommandBufferPool;

        private PipelineState activePipeline;
        private bool pipelineDirty = true;

        /// <summary>
        ///   The number of <c>vkCmdBindPipeline</c> calls recorded by this command list (checked by the tests).
        /// </summary>
        internal int NativePipelineBindCount;

        // Per-CB layout state, per subresource. A texture's shared LayoutTracker is mutated by every
        // CB's ResourceBarrierTransition calls in arbitrary order, which disagrees with Vulkan's
        // per-execution-order layout tracking when CBs record concurrently. This map remembers what
        // layouts THIS CB believes each texture holds for the draws it records.
        // Cleared on Reset. The first touch of a texture in a CB falls back to the shared tracker; the
        // layout changes are also recorded, so submission corrects that guess (SubmittedLayoutReplay).
        private readonly Dictionary<Texture, SubresourceLayoutTracker> currentCbLayouts = new();

        // Scratch lists for the barriers of one call. Reused to avoid per-call allocs.
        private readonly List<SubresourceTransition> barrierTransitions = new(16);
        private readonly List<SubresourceTransition> sourceTransferTransitions = new(16);
        private readonly List<SubresourceTransition> destinationTransferTransitions = new(16);

        // Barriers not in the command buffer yet: they go in together, merged, before the next command that needs
        // them (a draw, dispatch, copy, clear...), as on Direct3D 12. See FlushBarriers.
        private readonly List<VkMemoryBarrier2> pendingMemoryBarriers = new(4);
        private readonly List<VkBufferMemoryBarrier2> pendingBufferBarriers = new(16);
        private readonly List<VkImageMemoryBarrier2> pendingImageBarriers = new(16);

        /// <summary>
        ///   The number of <c>vkCmdPipelineBarrier2</c> calls recorded by this command list, and of barriers in them
        ///   (checked by the tests).
        /// </summary>
        internal int NativeBarrierCommandCount, NativeBarrierCount;

        /// <summary>
        ///   The last access this command buffer synchronised each buffer against.
        /// </summary>
        private readonly Dictionary<Buffer, BarrierLayout> currentCbBufferLayouts = new();

        // Dynamic rendering state: whether a render pass instance (vkCmdBeginRendering) is active,
        // whether the bound attachments changed since it began, and which depth layout it declared.
        private bool activeRendering;
        private bool renderingDirty = true;
        private bool activeDepthReadOnly;

        private VkDescriptorPool descriptorPool;
        private VkDescriptorSet descriptorSet;
        private uint[] allocatedTypeCounts;
        private uint allocatedSetCount;

        private uint? activeStencilReference = 0;

        private CompiledCommandList currentCommandList;

        public static CommandList New(GraphicsDevice device)
        {
            return new CommandList(device);
        }

        private CommandList(GraphicsDevice device) : base(device)
        {
            Recreate();
        }

        private void Recreate()
        {
            CommandBufferPool = new CommandBufferPool(GraphicsDevice, false);

            descriptorPool = GraphicsDevice.DescriptorPools.GetObject(GraphicsDevice.CommandListFence.GetCompletedValue());
            allocatedTypeCounts = new uint[DescriptorSetLayout.DescriptorTypeCount];
            allocatedSetCount = 0;

            Reset();
        }

        /// <summary>
        ///   Resets a Command List back to its initial state as if a new Command List was just created.
        /// </summary>
        public unsafe partial void Reset()
        {
            if (currentCommandList.Builder != null)
                return;

            // New command list ID so resources know they need to re-issue barriers
            CommandListId = System.Threading.Interlocked.Increment(ref GraphicsDevice.NextCommandListId);

            DebugScopeResetForNewRecording();
            CleanupRenderPass();
            boundDescriptorSets.Clear();
            currentCbLayouts.Clear();
            currentCbBufferLayouts.Clear();
            pendingMemoryBarriers.Clear();
            pendingBufferBarriers.Clear();
            pendingImageBarriers.Clear();

            renderingDirty = true;

            currentCommandList.Builder = this;
            currentCommandList.NativeCommandBuffer = CommandBufferPool.GetObject(GraphicsDevice.CommandListFence.GetCompletedValue());
            currentCommandList.DescriptorPools = GraphicsDevice.DescriptorPoolLists.Acquire();
            currentCommandList.StagingResources = GraphicsDevice.StagingResourceLists.Acquire();
            currentCommandList.QueryPools = GraphicsDevice.QueryPoolLists.Acquire();
            currentCommandList.RecordedTransitions = GraphicsDevice.RecordedTransitionLists.Acquire();

            var beginInfo = new VkCommandBufferBeginInfo
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.OneTimeSubmit
            };
            GraphicsDevice.NativeDeviceApi.vkBeginCommandBuffer(currentCommandList.NativeCommandBuffer, &beginInfo);

            pipelineDirty = true;
            viewportDirty = true;
            scissorsDirty = true;

            activeStencilReference = null;
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
            // End active render pass, record the barriers still pending (such as the transition to present)
            BeginCommandOutsideRenderPass();

            // Close
            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkEndCommandBuffer(currentCommandList.NativeCommandBuffer));

            // Staging resources not updated anymore
            foreach (var stagingResource in currentCommandList.StagingResources)
            {
                stagingResource.UpdatingCommandList = null;
            }

            activePipeline = null;

            var result = currentCommandList;
            currentCommandList = default;
            return result;
        }

        /// <summary>
        ///   Closes and executes the Command List.
        /// </summary>
        public partial void Flush()
        {
            GraphicsDevice.ExecuteCommandList(Close());
        }

        private unsafe void FlushInternal(bool wait)
        {
            var commandListFenceValue = GraphicsDevice.ExecuteCommandListInternal(Close());

            if (wait)
                GraphicsDevice.CommandListFence.WaitForFenceCPUInternal(commandListFenceValue);

            Reset();
        }

        /// <summary>
        ///   Vulkan-specific implementation that clears and restores the state of the Graphics Device.
        /// </summary>
        private partial void ClearStateImpl()
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
        private partial void SetRenderTargetsImpl(Texture depthStencilBuffer, ReadOnlySpan<Texture> renderTargets)
        {
            // The base class updated the bound targets; the next draw restarts the render pass
            // instance with them if they changed
            renderingDirty = true;
        }

        /// <summary>
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
        }

        private unsafe void BindPipeline()
        {
            if (!pipelineDirty)
                return;

            GraphicsDevice.NativeDeviceApi.vkCmdBindPipeline(currentCommandList.NativeCommandBuffer, activePipeline.IsCompute ? VkPipelineBindPoint.Compute : VkPipelineBindPoint.Graphics, activePipeline.NativePipeline);
            // Bound until SetPipelineState changes it or the command buffer is reset; it stays across render pass instances
            pipelineDirty = false;
            NativePipelineBindCount++;
        }

        /// <summary>
        ///     Gets or sets the 1st viewport. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <value>The viewport.</value>
        private unsafe void SetViewportImpl()
        {
            if (!viewportDirty && !scissorsDirty)
                return;

            //// TODO D3D12 Hardcoded for one viewport
            var viewportCopy = Viewport;
            if (activePipeline?.Description.RasterizerState.ScissorTestEnable ?? false)
            {
                if (scissorsDirty)
                {
                    // Use manual scissor
                    var scissor = scissors[0];
                    var nativeScissor = new VkRect2D(scissor.Left, scissor.Top, (uint)scissor.Width, (uint)scissor.Height);
                    GraphicsDevice.NativeDeviceApi.vkCmdSetScissor(currentCommandList.NativeCommandBuffer, firstScissor: 0, scissorCount: 1, &nativeScissor);
                }
            }
            else
            {
                // Use viewport
                // Always update, because either scissor or viewport was dirty and we use viewport size
                var scissor = new VkRect2D((int) viewportCopy.X, (int) viewportCopy.Y, (uint) viewportCopy.Width, (uint) viewportCopy.Height);
                GraphicsDevice.NativeDeviceApi.vkCmdSetScissor(currentCommandList.NativeCommandBuffer, firstScissor: 0, scissorCount: 1, &scissor);
            }
            scissorsDirty = false;

            // Since Vulkan 1.1, we can use negative viewport instead of doing gl_Position.y = -gl_Position.y in the shader
            // Note: we mutate viewportCopy _after_ vkCmdSetScissor has been called
            viewportCopy.Y = viewportCopy.Y + viewportCopy.Height;
            viewportCopy.Height = -viewportCopy.Height;
            if (viewportDirty)
            {
                GraphicsDevice.NativeDeviceApi.vkCmdSetViewport(currentCommandList.NativeCommandBuffer, firstViewport: 0, viewportCount: 1, (VkViewport*)&viewportCopy);
                viewportDirty = false;
            }
        }

        /// <summary>
        /// Unsets the render targets.
        /// </summary>
        public void UnsetRenderTargets()
        {
        }

        /// <summary>
        ///   Vulkan implementation that sets a scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangle">The scissor rectangle to set.</param>
        private unsafe partial void SetScissorRectangleImpl(ref readonly Rectangle scissorRectangle)
        {
            // Do nothing. Vulkan already sets the scissor rectangle as part of PrepareDraw()
        }

        /// <summary>
        ///   Vulkan implementation that sets one or more scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorCount">The number of scissor rectangles to bind.</param>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        private unsafe partial void SetScissorRectanglesImpl(ReadOnlySpan<Rectangle> scissorRectangles)
        {
            // Do nothing. Vulkan already sets the scissor rectangles as part of PrepareDraw()
        }

        /// <summary>
        ///     Prepares a draw call. This method is called before each Draw() method to setup the correct Primitive, InputLayout and VertexBuffers.
        /// </summary>
        /// <exception cref="InvalidOperationException">Cannot GraphicsDevice.Draw*() without an effect being previously applied with Effect.Apply() method</exception>
        private unsafe void PrepareDraw()
        {
            RecordDebugCounter(DebugCounterKind.Draw);

            // Barriers aren't allowed inside a render pass: when some are pending, end it and record them before the draw
            FlushBarriers();

            // Lazily set the render pass and frame buffer
            EnsureRenderPass();
            BindPipeline();
            BindDescriptorSets();
            SetViewportImpl();
            GraphicsDevice.NativeDeviceApi.vkCmdSetStencilReference(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, activeStencilReference ?? 0);

            // A read-only depth view drops depth and stencil writes instead of being invalid
            // usage, matching D3D12's read-only depth-stencil view behavior
            var depthStencilState = activePipeline.Description.DepthStencilState;
            bool depthWritesAllowed = !(depthStencilBuffer != null && (depthStencilBuffer.IsDepthStencilReadOnly || DepthStencilAccess == DepthStencilAccess.Read));
            GraphicsDevice.NativeDeviceApi.vkCmdSetDepthWriteEnable(currentCommandList.NativeCommandBuffer, depthStencilState.DepthBufferWriteEnable && depthWritesAllowed);
            GraphicsDevice.NativeDeviceApi.vkCmdSetStencilWriteMask(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, depthWritesAllowed ? depthStencilState.StencilWriteMask : 0u);
        }

        private unsafe void BindDescriptorSets()
        {
            // Keep track of descriptor pool usage
            bool isPoolExhausted = ++allocatedSetCount > GraphicsDevice.MaxDescriptorSetCount;
            for (int i = 0; i < DescriptorSetLayout.DescriptorTypeCount; i++)
            {
                allocatedTypeCounts[i] += activePipeline.DescriptorTypeCounts[i];
                if (allocatedTypeCounts[i] > GraphicsDevice.MaxDescriptorTypeCounts[i])
                {
                    isPoolExhausted = true;
                    break;
                }
            }

            if (isPoolExhausted)
            {
                // Retrieve a new pool
                currentCommandList.DescriptorPools.Add(descriptorPool);
                descriptorPool = GraphicsDevice.DescriptorPools.GetObject(GraphicsDevice.CommandListFence.GetCompletedValue());

                allocatedSetCount = 1;
                for (int i = 0; i < DescriptorSetLayout.DescriptorTypeCount; i++)
                {
                    allocatedTypeCounts[i] = activePipeline.DescriptorTypeCounts[i];
                }
            }

            // Allocate descriptor set
            var nativeDescriptorSetLayout = activePipeline.NativeDescriptorSetLayout;
            var allocateInfo = new VkDescriptorSetAllocateInfo
            {
                sType = VkStructureType.DescriptorSetAllocateInfo,
                descriptorPool = descriptorPool,
                descriptorSetCount = 1,
                pSetLayouts = &nativeDescriptorSetLayout
            };

            VkDescriptorSet localDescriptorSet;
            GraphicsDevice.NativeDeviceApi.vkAllocateDescriptorSets(GraphicsDevice.NativeDevice, &allocateInfo, &localDescriptorSet);
            descriptorSet = localDescriptorSet;

#if !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
            copies.Clear(true);

            foreach (var mapping in activePipeline.DescriptorBindingMapping)
            {
                copies.Add(new VkCopyDescriptorSet
                {
                    sType = VkStructureType.CopyDescriptorSet,
                    srcSet = boundDescriptorSets[mapping.SourceSet],
                    srcBinding = (uint) mapping.SourceBinding,
                    srcArrayElement = 0,
                    dstSet = localDescriptorSet,
                    dstBinding = (uint) mapping.DestinationBinding,
                    dstArrayElement = 0,
                    descriptorCount = 1
                });
            }

            fixed (VkCopyDescriptorSet* fCopiesItems = copies.Items)
                GraphicsDevice.NativeDeviceApi.vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, 0, null, (uint) copies.Count, fCopiesItems);
#else
            var bindingCount = activePipeline.DescriptorBindingMapping.Count;
            var writes = stackalloc VkWriteDescriptorSet[bindingCount];
            var descriptorDatas = stackalloc DescriptorData[bindingCount];

            for (int index = 0; index < bindingCount; index++)
            {
                var mapping = activePipeline.DescriptorBindingMapping[index];
                var sourceSet = boundDescriptorSets[mapping.SourceSet];
                var heapObject = sourceSet.HeapObjects[sourceSet.DescriptorStartOffset + mapping.SourceBinding];

                var write = writes + index;
                var descriptorData = descriptorDatas + index;

                *write = new VkWriteDescriptorSet
                {
                    sType = VkStructureType.WriteDescriptorSet,
                    descriptorType = mapping.DescriptorType,
                    dstSet = localDescriptorSet,
                    dstBinding = (uint)mapping.DestinationBinding,
                    dstArrayElement = 0,
                    descriptorCount = 1
                };

                switch (mapping.DescriptorType)
                {
                    case VkDescriptorType.SampledImage:
                        {
                            var texture = heapObject.Value as Texture;
                            // The descriptor's layout field must match the image's actual layout as
                            // this CB understands it, for the subresources the view reads. Use the per-CB
                            // map rather than the shared tracker (which can be mutated by other CBs
                            // recording concurrently).
                            var parent = texture?.RootTexture;
                            BarrierLayout? perCb = null;
                            if (parent != null && currentCbLayouts.TryGetValue(parent, out var cbTracker))
                            {
                                var viewRange = texture.GetViewSubResourceRange();
                                perCb = cbTracker.Get((uint) parent.GetSubResourceIndex(viewRange.ArraySliceStart, viewRange.MipStart));
                            }
                            // Sampling the depth buffer while it is bound as a read-only attachment:
                            // the image rides in DepthStencilReadOnlyOptimal, including on worker
                            // command lists that did not record the transition themselves.
                            bool sampledBoundReadOnlyDepth = depthStencilBuffer != null
                                && (depthStencilBuffer.IsDepthStencilReadOnly || DepthStencilAccess == DepthStencilAccess.Read)
                                && parent == depthStencilBuffer.RootTexture;
                            var imageLayout = perCb == BarrierLayout.DepthStencilRead || (perCb == null && sampledBoundReadOnlyDepth)
                                ? VkImageLayout.DepthStencilReadOnlyOptimal
                                : VkImageLayout.ShaderReadOnlyOptimal;
                            descriptorData->ImageInfo = new VkDescriptorImageInfo { imageView = texture?.NativeImageView ?? GraphicsDevice.EmptyTexture.NativeImageView, imageLayout = imageLayout };
                            write->pImageInfo = &descriptorData->ImageInfo;
                            break;
                        }

                    case VkDescriptorType.StorageImage:
                        {
                            var texture = heapObject.Value as Texture;
                            descriptorData->ImageInfo = new VkDescriptorImageInfo { imageView = texture?.NativeImageView ?? GraphicsDevice.EmptyTexture.NativeImageView, imageLayout = VkImageLayout.General };
                            write->pImageInfo = &descriptorData->ImageInfo;
                            break;
                        }

                    case VkDescriptorType.Sampler:
                        var samplerState = heapObject.Value as SamplerState;
                        descriptorData->ImageInfo = new VkDescriptorImageInfo { sampler = samplerState?.NativeSampler ?? GraphicsDevice.SamplerStates.LinearClamp.NativeSampler };
                        write->pImageInfo = &descriptorData->ImageInfo;
                        break;

                    case VkDescriptorType.UniformBuffer:
                        var buffer = heapObject.Value as Buffer;
                        descriptorData->BufferInfo = new VkDescriptorBufferInfo { buffer = buffer?.NativeBuffer ?? VkBuffer.Null, offset = (ulong)heapObject.Offset, range = (ulong)heapObject.Size };
                        write->pBufferInfo = &descriptorData->BufferInfo;
                        break;

                    case VkDescriptorType.UniformTexelBuffer:
                    case VkDescriptorType.StorageTexelBuffer:
                        buffer = heapObject.Value as Buffer;
                        descriptorData->BufferView = buffer?.NativeBufferView ?? (mapping.ResourceElementIsInteger ? GraphicsDevice.EmptyTexelBufferInt.NativeBufferView : GraphicsDevice.EmptyTexelBufferFloat.NativeBufferView);
                        write->pTexelBufferView = &descriptorData->BufferView;
                        break;

                    case VkDescriptorType.StorageBuffer:
                        buffer = heapObject.Value as Buffer;
                        // heapObject.Offset is repurposed to carry InitialCounterOffset for UAVs
                        // (defaults to -1) and is not the descriptor binding offset; structured /
                        // raw buffer descriptors always bind from offset 0 over the whole buffer.
                        descriptorData->BufferInfo = new VkDescriptorBufferInfo { buffer = buffer?.NativeBuffer ?? VkBuffer.Null, offset = 0, range = (ulong)(buffer?.SizeInBytes ?? 0)};
                        write->pBufferInfo = &descriptorData->BufferInfo;
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            GraphicsDevice.NativeDeviceApi.vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, (uint)bindingCount, writes, descriptorCopyCount: 0, descriptorCopies: null);
#endif
            GraphicsDevice.NativeDeviceApi.vkCmdBindDescriptorSets(currentCommandList.NativeCommandBuffer, activePipeline.IsCompute ? VkPipelineBindPoint.Compute : VkPipelineBindPoint.Graphics, activePipeline.NativeLayout, firstSet: 0, descriptorSetCount: 1, &localDescriptorSet, dynamicOffsetCount: 0, dynamicOffsets: null);
        }

        private readonly FastList<VkCopyDescriptorSet> copies = new();

        public void SetStencilReference(int stencilReference)
        {
            if (activeStencilReference != stencilReference)
            {
                activeStencilReference = (uint) stencilReference;
                GraphicsDevice.NativeDeviceApi.vkCmdSetStencilReference(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, activeStencilReference.Value);
            }
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (pipelineState == activePipeline)
                return;

            viewportDirty = true;
            // If scissor state changed, force a refresh
            scissorsDirty |= (pipelineState?.Description.RasterizerState.ScissorTestEnable ?? false) != (activePipeline?.Description.RasterizerState.ScissorTestEnable ?? false);

            activePipeline = pipelineState;
            pipelineDirty = true;
        }

        public unsafe void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            // TODO VULKAN API: Stride is part of Pipeline

            // TODO VULKAN: Handle multiple buffers. Collect and apply before draw?
            //if (index != 0)
            //    throw new NotImplementedException();

            var bufferCopy = buffer.NativeBuffer;
            var offsetCopy = (ulong) offset;

            GraphicsDevice.NativeDeviceApi.vkCmdBindVertexBuffers(currentCommandList.NativeCommandBuffer, (uint) index, bindingCount: 1, &bufferCopy, &offsetCopy);
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            GraphicsDevice.NativeDeviceApi.vkCmdBindIndexBuffer(currentCommandList.NativeCommandBuffer, buffer.NativeBuffer, (ulong) offset, is32bits ? VkIndexType.Uint32 : VkIndexType.Uint16);
        }

        /// <summary>
        ///   Inserts a barrier that transitions a Graphics Resource to a new layout, ensuring proper synchronization
        ///   between the GPU operations accessing it before and after.
        /// </summary>
        /// <param name="resource">
        ///   The Graphics Resource to transition. For a Texture View, only the mip-levels and array slices it covers move,
        ///   so a mip chain can read one mip-level while it writes the next. A View of a depth-stencil Texture moves the
        ///   whole Texture.
        /// </param>
        /// <param name="newLayout">The new layout of <paramref name="resource"/>.</param>
        /// <param name="subresource">
        ///   The sub-Resource to transition, numbered in the Texture as <see cref="Texture.GetSubResourceIndex"/> does (also
        ///   when <paramref name="resource"/> is a View of it), or <see cref="uint.MaxValue"/> for the whole
        ///   <paramref name="resource"/>.
        /// </param>
        /// <remarks>
        ///   For a <see cref="Buffer"/>, <see cref="Buffer.NativeAccessMask"/> and
        ///   <see cref="Buffer.NativePipelineStageMask"/> are read as the source but never written.
        ///   The copy and upload paths rely on them staying the set of every legal usage.
        /// </remarks>
        public unsafe void ResourceBarrierTransition(GraphicsResource resource, BarrierLayout newLayout, uint subresource = uint.MaxValue)
        {
            RecordDebugCounter(DebugCounterKind.Barrier);

            if (resource is Texture texture)
            {
                // Texture views share the image of their parent; barrier the parent instead, and only the
                // subresources the view covers: a mip chain reads one mip while it writes the next
                var viewRange = subresource == uint.MaxValue ? texture.GetBarrierRange() : null;
                texture = texture.RootTexture;

                TransitionTexture(texture, viewRange, subresource, newLayout, barrierTransitions);
            }
            else if (resource is Buffer buffer)
            {
                VkAccessFlags oldAccessMask;
                VkPipelineStageFlags sourceStages;
                if (currentCbBufferLayouts.TryGetValue(buffer, out var fromLayout))
                {
                    if (fromLayout == newLayout)
                        return;

                    oldAccessMask = BufferAccessFlags(buffer, fromLayout);
                    sourceStages = BufferPipelineStageFlags(buffer, fromLayout);
                }
                else
                {
                    oldAccessMask = buffer.NativeAccessMask;
                    sourceStages = buffer.NativePipelineStageMask;
                }

                var newAccessMask = BufferAccessFlags(buffer, newLayout);
                var newStages = BufferPipelineStageFlags(buffer, newLayout);

                sourceStages = FixStagesForAccess(sourceStages, oldAccessMask);
                newStages = FixStagesForAccess(newStages, newAccessMask);

                if (sourceStages == VkPipelineStageFlags.None)
                    sourceStages = VkPipelineStageFlags.TopOfPipe;
                if (newStages == VkPipelineStageFlags.None)
                    newStages = VkPipelineStageFlags.BottomOfPipe;

                currentCbBufferLayouts[buffer] = newLayout;

                AddPendingBarrier(PipelineBarriers.BufferBarrier(buffer.NativeBuffer, sourceStages, oldAccessMask, newStages, newAccessMask));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        ///   The access of a buffer in a layout. In <see cref="BarrierLayout.Common"/> a buffer is back to its usual use,
        ///   such as vertex fetch for a vertex buffer, so its barrier covers the accesses of its usage flags.
        /// </summary>
        private static VkAccessFlags BufferAccessFlags(Buffer buffer, BarrierLayout layout)
            => layout == BarrierLayout.Common ? buffer.NativeAccessMask : BarrierMapping.ToVkAccessFlags(layout);

        /// <summary>
        ///   The pipeline stages of a buffer in a layout. See <see cref="BufferAccessFlags"/>.
        /// </summary>
        private static VkPipelineStageFlags BufferPipelineStageFlags(Buffer buffer, BarrierLayout layout)
            => layout == BarrierLayout.Common ? buffer.NativePipelineStageMask : BarrierMapping.ToVkPipelineStageFlags(layout);

        /// <summary>
        ///   Makes the unordered access writes to a Graphics Resource visible to the next unordered accesses to it, such as
        ///   a dispatch that writes a buffer, then a dispatch that reads or writes it.
        /// </summary>
        /// <param name="resource">The Graphics Resource written as unordered access.</param>
        /// <remarks>
        ///   A transition to <see cref="BarrierLayout.UnorderedAccess"/> can't do this: it does nothing when the resource
        ///   already has that layout. The resource must already be in <see cref="BarrierLayout.UnorderedAccess"/>: this
        ///   doesn't transition it.
        /// </remarks>
        public unsafe void UnorderedAccessBarrier(GraphicsResource resource)
        {
            ArgumentNullException.ThrowIfNull(resource);

            var stages = BarrierMapping.ToVkPipelineStageFlags(BarrierLayout.UnorderedAccess);
            const VkAccessFlags AccessBefore = VkAccessFlags.ShaderWrite;
            const VkAccessFlags AccessAfter = VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite;

            if (resource is Texture texture)
            {
                // A texture view covers only some subresources of its parent, the others may have other layouts
                var viewRange = texture.GetBarrierRange();
                texture = texture.RootTexture;

                var range = viewRange is { } r
                    ? new VkImageSubresourceRange(texture.NativeImageAspect, (uint) r.MipStart, (uint) r.MipCount, (uint) r.ArraySliceStart, (uint) r.ArraySliceCount)
                    : new VkImageSubresourceRange(texture.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue);
                var layout = BarrierMapping.ToVkImageLayout(BarrierLayout.UnorderedAccess);
                AddPendingBarrier(PipelineBarriers.ImageBarrier(texture.NativeImage, range, stages, AccessBefore, layout, stages, AccessAfter, layout));
            }
            else if (resource is Buffer buffer)
            {
                AddPendingBarrier(PipelineBarriers.BufferBarrier(buffer.NativeBuffer, stages, AccessBefore, stages, AccessAfter));
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        /// <summary>
        ///   Makes all unordered access writes visible to the next unordered accesses, such as a pass that writes several
        ///   buffers, then a pass that reads them.
        /// </summary>
        /// <remarks>
        ///   The resources stay in <see cref="BarrierLayout.UnorderedAccess"/>: transition them first if they aren't.
        /// </remarks>
        public unsafe void UnorderedAccessBarrier()
        {
            var stages = BarrierMapping.ToVkPipelineStageFlags(BarrierLayout.UnorderedAccess);
            pendingMemoryBarriers.Add(PipelineBarriers.MemoryBarrier(stages, VkAccessFlags.ShaderWrite, stages, VkAccessFlags.ShaderRead | VkAccessFlags.ShaderWrite));
        }

        /// <summary>
        ///   Adds a barrier to the pending ones. A barrier on the same buffer range as a pending one merges with it into
        ///   one that waits for the accesses of both and makes them visible to both: a chain (A to B, then B to C, with no
        ///   command in between) or two accesses asked at once (a copy that reads and writes the same buffer).
        /// </summary>
        private void AddPendingBarrier(in VkBufferMemoryBarrier2 barrier)
        {
            var pendingBarriers = CollectionsMarshal.AsSpan(pendingBufferBarriers);
            for (int i = 0; i < pendingBarriers.Length; i++)
            {
                ref var pending = ref pendingBarriers[i];
                if (pending.buffer != barrier.buffer)
                    continue;

                if (pending.offset == barrier.offset && pending.size == barrier.size)
                {
                    pending.srcStageMask |= barrier.srcStageMask;
                    pending.srcAccessMask |= barrier.srcAccessMask;
                    pending.dstStageMask |= barrier.dstStageMask;
                    pending.dstAccessMask |= barrier.dstAccessMask;
                    return;
                }

                // Barriers on overlapping ranges are ordered: record the pending ones first
                if (RangesOverlap(pending.offset, pending.size, barrier.offset, barrier.size))
                {
                    FlushBarriers();
                    break;
                }
            }
            pendingBufferBarriers.Add(barrier);
        }

        /// <summary>
        ///   Adds a barrier to the pending ones. A barrier that continues a pending one on the same subresources merges
        ///   with it: no command ran in between, so A to B then B to C is A to C.
        /// </summary>
        private void AddPendingBarrier(in VkImageMemoryBarrier2 barrier)
        {
            var pendingBarriers = CollectionsMarshal.AsSpan(pendingImageBarriers);
            for (int i = 0; i < pendingBarriers.Length; i++)
            {
                ref var pending = ref pendingBarriers[i];
                if (pending.image != barrier.image)
                    continue;

                ref readonly var a = ref pending.subresourceRange;
                ref readonly var b = ref barrier.subresourceRange;
                if (pending.newLayout == barrier.oldLayout &&
                    a.aspectMask == b.aspectMask && a.baseMipLevel == b.baseMipLevel && a.levelCount == b.levelCount &&
                    a.baseArrayLayer == b.baseArrayLayer && a.layerCount == b.layerCount)
                {
                    pending.dstStageMask = barrier.dstStageMask;
                    pending.dstAccessMask = barrier.dstAccessMask;
                    pending.newLayout = barrier.newLayout;
                    return;
                }

                // Other barriers on overlapping subresources are ordered: record the pending ones first
                if (RangesOverlap(a.baseMipLevel, a.levelCount, b.baseMipLevel, b.levelCount) &&
                    RangesOverlap(a.baseArrayLayer, a.layerCount, b.baseArrayLayer, b.layerCount))
                {
                    FlushBarriers();
                    break;
                }
            }
            pendingImageBarriers.Add(barrier);
        }

        // Whether two ranges overlap; a count of all ones (VK_REMAINING_* or VK_WHOLE_SIZE) goes to the end
        private static bool RangesOverlap(ulong startA, ulong countA, ulong startB, ulong countB)
        {
            var endA = countA == uint.MaxValue || countA == ulong.MaxValue ? ulong.MaxValue : startA + countA;
            var endB = countB == uint.MaxValue || countB == ulong.MaxValue ? ulong.MaxValue : startB + countB;
            return startA < endB && startB < endA;
        }

        /// <summary>
        ///   Records the pending barriers in the command buffer, ending the render pass when there are some (barriers
        ///   aren't allowed inside it). Called before each command that needs them.
        /// </summary>
        private void FlushBarriers()
        {
            if (!HasPendingBarriers)
                return;

            CleanupRenderPass();

            PipelineBarriers.Record(GraphicsDevice.NativeDeviceApi, currentCommandList.NativeCommandBuffer,
                                    CollectionsMarshal.AsSpan(pendingMemoryBarriers), CollectionsMarshal.AsSpan(pendingBufferBarriers),
                                    CollectionsMarshal.AsSpan(pendingImageBarriers));

            NativeBarrierCommandCount++;
            NativeBarrierCount += pendingMemoryBarriers.Count + pendingBufferBarriers.Count + pendingImageBarriers.Count;

            pendingMemoryBarriers.Clear();
            pendingBufferBarriers.Clear();
            pendingImageBarriers.Clear();
        }

        private bool HasPendingBarriers => pendingMemoryBarriers.Count > 0 || pendingBufferBarriers.Count > 0 || pendingImageBarriers.Count > 0;

        /// <summary>
        ///   Prepares a command that runs outside a render pass (a dispatch, copy, clear...): ends the render pass and
        ///   records the pending barriers.
        /// </summary>
        private void BeginCommandOutsideRenderPass()
        {
            CleanupRenderPass();
            FlushBarriers();
        }

        /// <summary>
        ///   Gets this command list's layout tracker of a Texture (not a View), seeded from the Texture's shared one
        ///   the first time the command list uses it.
        /// </summary>
        private ref SubresourceLayoutTracker GetCbLayoutTracker(Texture texture)
        {
            ref var tracker = ref CollectionsMarshal.GetValueRefOrAddDefault(currentCbLayouts, texture, out bool exists);
            if (!exists)
            {
                tracker = texture.LayoutTracker.Clone();
                SubmittedLayoutReplay.RecordAssumed(currentCommandList.RecordedTransitions, texture, tracker);
            }
            return ref tracker;
        }

        /// <summary>
        ///   Moves subresources of a Texture (not a View) to a layout in this command list's tracker and the shared one,
        ///   and adds the barriers it needs to the pending ones. <paramref name="transitions"/> keeps them, so a copy can
        ///   put the subresources back with <see cref="RestoreAfterTransfer"/>.
        /// </summary>
        /// <param name="range">The subresources to move, or <see langword="null"/> for <paramref name="subresource"/>.</param>
        /// <param name="subresource">The subresource to move, or <see cref="uint.MaxValue"/> for the whole Texture.</param>
        private void TransitionTexture(Texture texture, TextureSubresourceRange? range, uint subresource, BarrierLayout newLayout, List<SubresourceTransition> transitions)
        {
            transitions.Clear();

            ref var tracker = ref GetCbLayoutTracker(texture);
            if (range is { } viewRange)
                tracker.Transition(viewRange, texture.MipLevelCount, newLayout, transitions);
            else
                tracker.Transition(subresource, newLayout, transitions);

            if (transitions.Count == 0)
                return;

            // Mirrored to the shared tracker, for the next command list that uses the texture
            texture.LayoutTracker = tracker;
            foreach (var transition in transitions)
            {
                AddPendingBarrier(ImageBarrier(texture, transition.Subresource, transition.LayoutBefore, newLayout));
                currentCommandList.RecordedTransitions.Add(new RecordedTransition(texture, transition.Subresource, transition.LayoutBefore, newLayout));
            }
        }

        /// <summary>
        ///   After a copy, moves each subresource a transfer moved back to the layout it had. One that had no defined
        ///   layout keeps the transfer one: Vulkan can't transition to <see cref="VkImageLayout.Undefined"/>.
        /// </summary>
        private void RestoreAfterTransfer(Texture texture, List<SubresourceTransition> transferTransitions, BarrierLayout transferLayout)
        {
            ref var tracker = ref GetCbLayoutTracker(texture);
            foreach (var transition in transferTransitions)
            {
                if (transition.LayoutBefore == BarrierLayout.Undefined)
                    continue;

                tracker.Set(transition.Subresource, transition.LayoutBefore);
                AddPendingBarrier(ImageBarrier(texture, transition.Subresource, transferLayout, transition.LayoutBefore));
                currentCommandList.RecordedTransitions.Add(new RecordedTransition(texture, transition.Subresource, transferLayout, transition.LayoutBefore));
            }
            tracker.CollapseIfUniform();
            texture.LayoutTracker = tracker;
        }

        /// <summary>
        ///   Records the barriers before a copy between two Textures (not Views): a staging one is a buffer, another one
        ///   moves to the transfer layout.
        /// </summary>
        private void BeginTransfers(Texture source, Texture destination)
        {
            if (source.Usage == GraphicsResourceUsage.Staging)
            {
                BeginBufferTransfer(source.NativeBuffer, source.NativePipelineStageMask, source.NativeAccessMask, VkAccessFlags.TransferRead);
            }
            else
            {
                TransitionTexture(source, range: null, uint.MaxValue, BarrierLayout.CopySource, sourceTransferTransitions);
            }

            if (destination.Usage == GraphicsResourceUsage.Staging)
            {
                BeginBufferTransfer(destination.NativeBuffer, destination.NativePipelineStageMask, destination.NativeAccessMask, VkAccessFlags.TransferWrite);
            }
            else
            {
                TransitionTexture(destination, range: null, uint.MaxValue, BarrierLayout.CopyDest, destinationTransferTransitions);
            }

            // The copy comes next
            BeginCommandOutsideRenderPass();
        }

        /// <summary>
        ///   Records the barriers after a copy started with <see cref="BeginTransfers"/>: back to the accesses and
        ///   layouts before it.
        /// </summary>
        private void EndTransfers(Texture source, Texture destination)
        {
            if (source.Usage == GraphicsResourceUsage.Staging)
                EndBufferTransfer(source.NativeBuffer, source.NativePipelineStageMask, source.NativeAccessMask, VkAccessFlags.TransferRead);
            else
                RestoreAfterTransfer(source, sourceTransferTransitions, BarrierLayout.CopySource);

            if (destination.Usage == GraphicsResourceUsage.Staging)
                EndBufferTransfer(destination.NativeBuffer, destination.NativePipelineStageMask, destination.NativeAccessMask, VkAccessFlags.TransferWrite);
            else
                RestoreAfterTransfer(destination, destinationTransferTransitions, BarrierLayout.CopyDest);
        }

        /// <summary>
        ///   Adds the barrier before a copy reads or writes a buffer (or a range of it): from its usual accesses to the
        ///   transfer one.
        /// </summary>
        private void BeginBufferTransfer(VkBuffer buffer, VkPipelineStageFlags stages, VkAccessFlags access, VkAccessFlags transferAccess,
                                         ulong offset = 0, ulong size = VK_WHOLE_SIZE)
        {
            AddPendingBarrier(PipelineBarriers.BufferBarrier(buffer, FixStagesForAccess(stages, access), access,
                                                             VkPipelineStageFlags.Transfer, transferAccess, offset, size));
        }

        /// <summary>
        ///   Adds the barrier after a copy read or wrote a buffer (or a range of it): back to its usual accesses.
        /// </summary>
        private void EndBufferTransfer(VkBuffer buffer, VkPipelineStageFlags stages, VkAccessFlags access, VkAccessFlags transferAccess,
                                       ulong offset = 0, ulong size = VK_WHOLE_SIZE)
        {
            AddPendingBarrier(PipelineBarriers.BufferBarrier(buffer, VkPipelineStageFlags.Transfer, transferAccess,
                                                             FixStagesForAccess(stages, access), access, offset, size));
        }

        /// <summary>
        ///   Gets the barrier that moves a subresource of a Texture (not a View), or the whole Texture with
        ///   <see cref="uint.MaxValue"/>, between two layouts, with the stages and accesses of each.
        /// </summary>
        internal static VkImageMemoryBarrier2 ImageBarrier(Texture texture, uint subresource, BarrierLayout layoutBefore, BarrierLayout layoutAfter)
        {
            var mipLevelCount = (uint) texture.MipLevelCount;
            var range = subresource == uint.MaxValue
                ? new VkImageSubresourceRange(texture.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue)
                : new VkImageSubresourceRange(texture.NativeImageAspect, subresource % mipLevelCount, levelCount: 1, subresource / mipLevelCount, layerCount: 1);

            // Nothing to wait for on an image with no defined content, or one the presentation engine gave back
            bool fromNothing = layoutBefore is BarrierLayout.Undefined or BarrierLayout.Present;
            var srcStages = fromNothing ? VkPipelineStageFlags.TopOfPipe : BarrierMapping.ToVkPipelineStageFlags(layoutBefore);
            var srcAccess = fromNothing ? VkAccessFlags.None : BarrierMapping.ToVkAccessFlags(layoutBefore);

            return PipelineBarriers.ImageBarrier(texture.NativeImage, range,
                                                 srcStages, srcAccess, BarrierMapping.ToVkImageLayout(layoutBefore),
                                                 BarrierMapping.ToVkPipelineStageFlags(layoutAfter), BarrierMapping.ToVkAccessFlags(layoutAfter), BarrierMapping.ToVkImageLayout(layoutAfter));
        }

        [Obsolete("Use BarrierLayout overload instead.")]
        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            var layout = (int)newState switch
            {
                0x04 => BarrierLayout.RenderTarget,
                0x08 => BarrierLayout.UnorderedAccess,
                0x10 => BarrierLayout.DepthStencilWrite,
                0x20 => BarrierLayout.DepthStencilRead,
                0x80 => BarrierLayout.ShaderResource,
                0xC0 => BarrierLayout.ShaderResource,
                0x200 => BarrierLayout.IndirectArgument,
                0x400 => BarrierLayout.CopyDest,
                0x800 => BarrierLayout.CopySource,
                0x1000 => BarrierLayout.ResolveDest,
                0x2000 => BarrierLayout.ResolveSource,
                _ => BarrierLayout.Common,
            };
            ResourceBarrierTransition(resource, layout);
        }

        /// <summary>
        ///   Ensures the pipeline stage mask is compatible with the access flags in a buffer barrier.
        ///   HOST_READ/HOST_WRITE access requires VK_PIPELINE_STAGE_HOST_BIT, which is not included
        ///   in VK_PIPELINE_STAGE_ALL_COMMANDS_BIT.
        /// </summary>
        internal static VkPipelineStageFlags FixStagesForAccess(VkPipelineStageFlags stages, VkAccessFlags access)
        {
            if ((access & (VkAccessFlags.HostRead | VkAccessFlags.HostWrite)) != 0)
                stages |= VkPipelineStageFlags.Host;
            return stages;
        }

#if !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
        private readonly FastList<VkDescriptorSet> boundDescriptorSets = new FastList<VkDescriptorSet>();
#else
        private readonly FastList<DescriptorSet> boundDescriptorSets = new();
#endif

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
            if (index != 0)
                throw new NotImplementedException();

            boundDescriptorSets.Clear(fastClear: true);
            for (int i = 0; i < descriptorSets.Length; i++)
            {
#if !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
                boundDescriptorSets.Add(descriptorSets[i].NativeDescriptorSet);
#else
                boundDescriptorSets.Add(descriptorSets[i]);
#endif
            }
        }

        /// <inheritdoc />
        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
            RecordDebugCounter(DebugCounterKind.Dispatch);
            BeginCommandOutsideRenderPass();
            BindPipeline();
            BindDescriptorSets();
            GraphicsDevice.NativeDeviceApi.vkCmdDispatch(currentCommandList.NativeCommandBuffer, (uint)threadCountX, (uint)threadCountY, (uint)threadCountZ);
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            ArgumentNullException.ThrowIfNull(indirectBuffer);

            RecordDebugCounter(DebugCounterKind.Dispatch);
            BeginCommandOutsideRenderPass();
            BindPipeline();
            BindDescriptorSets();
            GraphicsDevice.NativeDeviceApi.vkCmdDispatchIndirect(currentCommandList.NativeCommandBuffer, indirectBuffer.NativeBuffer, (ulong)offsetInBytes);
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            GraphicsDevice.NativeDeviceApi.vkCmdDraw(currentCommandList.NativeCommandBuffer, (uint) vertexCount, instanceCount: 1, (uint) startVertexLocation, firstInstance: 0);

            GraphicsDevice.FrameTriangleCount += (uint) vertexCount;
            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Draw geometry of an unknown size.
        /// </summary>
        public void DrawAuto()
        {
            PrepareDraw();

            throw new NotImplementedException();
            //NativeDeviceContext.DrawAuto();

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

            GraphicsDevice.NativeDeviceApi.vkCmdDrawIndexed(currentCommandList.NativeCommandBuffer, (uint) indexCount, instanceCount: 1, (uint) startIndexLocation, baseVertexLocation, firstInstance: 0);

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

            GraphicsDevice.NativeDeviceApi.vkCmdDrawIndexed(currentCommandList.NativeCommandBuffer, (uint) indexCountPerInstance, (uint) instanceCount, (uint) startIndexLocation, baseVertexLocation, (uint) startInstanceLocation);
            //NativeCommandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);

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

            GraphicsDevice.NativeDeviceApi.vkCmdDrawIndexedIndirect(currentCommandList.NativeCommandBuffer, argumentsBuffer.NativeBuffer, (ulong) alignedByteOffsetForArgs, drawCount: 1, stride: 0);

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

            GraphicsDevice.NativeDeviceApi.vkCmdDraw(currentCommandList.NativeCommandBuffer, (uint) vertexCountPerInstance, (uint) instanceCount, (uint) startVertexLocation, (uint) startInstanceLocation);
            //NativeCommandList.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);

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

            GraphicsDevice.NativeDeviceApi.vkCmdDrawIndirect(currentCommandList.NativeCommandBuffer, argumentsBuffer.NativeBuffer, (ulong) alignedByteOffsetForArgs, drawCount: 1, stride: 0);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Begins profiling.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public unsafe void BeginProfile(Color4 profileColor, string name)
        {
            GraphicsDevice.PushDebugScope(name);
            if (GraphicsDevice.IsProfilingSupported)
            {
                // pLabelName is a null-terminated string, as in the Direct3D 12 BeginEvent fallback
                var maxBytes = System.Text.Encoding.ASCII.GetMaxByteCount(name.Length) + 1;
                var nameBytes = stackalloc byte[maxBytes];
                int written = System.Text.Encoding.ASCII.GetBytes(name, new Span<byte>(nameBytes, maxBytes));
                nameBytes[written] = 0;

                var labelInfo = new VkDebugUtilsLabelEXT
                {
                    sType = VkStructureType.DebugUtilsLabelEXT,
                    pLabelName = nameBytes
                };
                // The barriers asked for before the scope belong to the previous one
                FlushBarriers();
                GraphicsDevice.NativeInstanceApi.vkCmdBeginDebugUtilsLabelEXT(currentCommandList.NativeCommandBuffer, &labelInfo);
            }
        }

        /// <summary>
        /// Ends profiling.
        /// </summary>
        public void EndProfile()
        {
            GraphicsDevice.PopDebugScope();
            if (GraphicsDevice.IsProfilingSupported)
            {
                FlushBarriers();
                GraphicsDevice.NativeInstanceApi.vkCmdEndDebugUtilsLabelEXT(currentCommandList.NativeCommandBuffer);
            }
        }
        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="index">The index of the query to write.</param>
        /// <remarks>
        ///   The first write after the pool is created or read back resets all its queries first, as Vulkan requires
        ///   (outside a render pass: it ends the current one). Read the pool back before writing a query again.
        /// </remarks>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            if (queryPool.NeedsReset)
                ResetQueryPool(queryPool);

            // The barriers asked for before the timestamp count in the time before it
            FlushBarriers();
            GraphicsDevice.NativeDeviceApi.vkCmdWriteTimestamp(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.AllCommands, queryPool.NativeQueryPool, (uint) index);

            if (queryPool.UpdatingCommandList != this)
            {
                queryPool.UpdatingCommandList = this;
                currentCommandList.QueryPools.Add(queryPool);
            }
        }

        /// <summary>
        ///   Resets all the queries of a pool, so they can be written again.
        /// </summary>
        /// <param name="queryPool">The QueryPool to reset.</param>
        public void ResetQueryPool(QueryPool queryPool)
        {
            // Not allowed inside a render pass
            BeginCommandOutsideRenderPass();
            GraphicsDevice.NativeDeviceApi.vkCmdResetQueryPool(currentCommandList.NativeCommandBuffer, queryPool.NativeQueryPool, firstQuery: 0, (uint) queryPool.QueryCount);

            queryPool.NeedsReset = false;
        }

        /// <summary>
        /// Clears the specified depth stencil buffer. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="options">The options.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="stencil">The stencil.</param>
        /// <exception cref="InvalidOperationException"></exception>
        public unsafe void Clear(Texture depthStencilBuffer, DepthStencilClearOptions options, float depth = 1, byte stencil = 0)
        {
            RecordDebugCounter(DebugCounterKind.Clear);

            // Adjust aspectMask to clear only the specified part (depth or stencil)
            var clearRange = depthStencilBuffer.NativeResourceRange;
            clearRange.aspectMask = VkImageAspectFlags.None;

            if ((options & DepthStencilClearOptions.DepthBuffer) != 0)
                clearRange.aspectMask |= VkImageAspectFlags.Depth & depthStencilBuffer.NativeImageAspect;

            if ((options & DepthStencilClearOptions.Stencil) != 0)
                clearRange.aspectMask |= VkImageAspectFlags.Stencil & depthStencilBuffer.NativeImageAspect;

            // Clear requires TransferDstOptimal; leave the buffer in the depth write layout
            // afterwards, matching the Direct3D 12 behavior.
            ResourceBarrierTransition(depthStencilBuffer, BarrierLayout.CopyDest);
            BeginCommandOutsideRenderPass();

            var clearValue = new VkClearDepthStencilValue(depth, stencil);
            GraphicsDevice.NativeDeviceApi.vkCmdClearDepthStencilImage(currentCommandList.NativeCommandBuffer, depthStencilBuffer.NativeImage, VkImageLayout.TransferDstOptimal, &clearValue, rangeCount: 1, &clearRange);

            ResourceBarrierTransition(depthStencilBuffer, BarrierLayout.DepthStencilWrite);

            depthStencilBuffer.IsInitialized = true;
        }

        /// <summary>
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="ArgumentNullException">renderTarget</exception>
        public unsafe void Clear(Texture renderTarget, Color4 color)
        {
            RecordDebugCounter(DebugCounterKind.Clear);
            // TODO VULKAN: Detect if inside render pass. If so, NativeCommandBuffer.ClearAttachments()

            var clearRange = renderTarget.NativeResourceRange;

            // Clear requires TransferDstOptimal; leave the target in the render target layout
            // afterwards, matching the Direct3D 12 behavior.
            ResourceBarrierTransition(renderTarget, BarrierLayout.CopyDest);
            BeginCommandOutsideRenderPass();

            GraphicsDevice.NativeDeviceApi.vkCmdClearColorImage(currentCommandList.NativeCommandBuffer, renderTarget.NativeImage, VkImageLayout.TransferDstOptimal, (VkClearColorValue*) &color, rangeCount: 1, &clearRange);

            ResourceBarrierTransition(renderTarget, BarrierLayout.RenderTarget);

            renderTarget.IsInitialized = true;
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        /// <exception cref="NotSupportedException">
        ///   The buffer is raw, structured or of an integer format (clear it with integer values), or its elements can't
        ///   be written by repeating one 32-bit word (see <see cref="ClearReadWrite(Buffer, UInt4)"/>).
        /// </exception>
        public void ClearReadWrite(Buffer buffer, Vector4 value)
        {
            ClearReadWriteImpl(buffer, GetClearWord(buffer, default, value));
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        /// <exception cref="NotSupportedException">
        ///   The elements can't be written by repeating one 32-bit word (see <see cref="ClearReadWrite(Buffer, UInt4)"/>).
        /// </exception>
        public void ClearReadWrite(Buffer buffer, Int4 value)
        {
            ClearReadWriteImpl(buffer, GetClearWord(buffer, new UInt4((uint) value.X, (uint) value.Y, (uint) value.Z, (uint) value.W), floatValue: null));
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">buffer</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        /// <exception cref="NotSupportedException">
        ///   The elements can't be written by repeating one 32-bit word: an 8 or 16-byte element (such as
        ///   <see cref="PixelFormat.R32G32B32A32_UInt"/>) whose 32-bit words differ, or a format other than 8, 16 or 32-bit
        ///   RGBA channels. Clear it with a value that repeats every 4 bytes (such as 0), or in a compute shader.
        /// </exception>
        /// <remarks>
        ///   As on Direct3D, a raw or structured Buffer gets <c>X</c> in every 32-bit word, and a typed Buffer gets the low
        ///   bits of each channel, without conversion.
        /// </remarks>
        public void ClearReadWrite(Buffer buffer, UInt4 value)
        {
            ClearReadWriteImpl(buffer, GetClearWord(buffer, value, floatValue: null));
        }

        private unsafe void ClearReadWriteImpl(Buffer buffer, uint word)
        {
            RecordDebugCounter(DebugCounterKind.Clear);

            // The fill is a transfer, synchronized like the Texture clear; leave the buffer in the unordered access
            // layout afterwards, matching the Direct3D 12 behavior
            ResourceBarrierTransition(buffer, BarrierLayout.CopyDest);

            // Not allowed inside a render pass, after the transition to a copy destination
            BeginCommandOutsideRenderPass();
            GraphicsDevice.NativeDeviceApi.vkCmdFillBuffer(currentCommandList.NativeCommandBuffer, buffer.NativeBuffer, dstOffset: 0, (ulong) (buffer.SizeInBytes & ~3), word);

            ResourceBarrierTransition(buffer, BarrierLayout.UnorderedAccess);
        }

        /// <summary>
        ///   Gets the 32-bit word that <c>vkCmdFillBuffer</c> repeats to write a clear value in every element of a Buffer,
        ///   as Direct3D's <c>ClearUnorderedAccessViewUint</c> / <c>Float</c> write it.
        /// </summary>
        /// <param name="buffer">The Buffer to clear.</param>
        /// <param name="bits">The integer value: the low bits of each channel are copied.</param>
        /// <param name="floatValue">The float value, converted to the format; <see langword="null"/> for an integer value.</param>
        private static uint GetClearWord(Buffer buffer, UInt4 bits, Vector4? floatValue)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if ((buffer.Flags & BufferFlags.UnorderedAccess) == 0)
                throw new ArgumentException("Expecting a Buffer supporting UAV", nameof(buffer));

            // Raw and structured views: X in every 32-bit word
            if ((buffer.Flags & (BufferFlags.RawBuffer | BufferFlags.StructuredBuffer)) != 0)
            {
                if (floatValue is not null)
                    throw new NotSupportedException("A raw or structured Buffer is cleared with an integer value (UInt4 or Int4), as on Direct3D.");
                return bits.X;
            }

            var format = buffer.ViewFormat;
            var (channelCount, channelBits, kind) = GetClearFormat(format)
                ?? throw new NotSupportedException($"Clearing a Buffer of format {format} is not supported on Vulkan: only 8, 16 or 32-bit RGBA channels are.");

            if (floatValue is not null && kind is ClearChannelKind.UInt or ClearChannelKind.SInt)
                throw new NotSupportedException($"A Buffer of integer format {format} is cleared with an integer value (UInt4 or Int4), as on Direct3D.");

            // One element, little-endian channels
            Span<uint> bitsPerChannel = [bits.X, bits.Y, bits.Z, bits.W];
            Span<byte> element = stackalloc byte[16];
            element.Clear();
            int channelSize = channelBits / 8;
            for (int channel = 0; channel < channelCount; channel++)
            {
                uint channelValue = floatValue is Vector4 floats
                    ? ConvertChannel(floats[channel], channelBits, kind)
                    : bitsPerChannel[channel];

                for (int i = 0; i < channelSize; i++)
                    element[channel * channelSize + i] = (byte) (channelValue >> (8 * i));
            }

            // The fill repeats one 32-bit word: a smaller element is repeated in it, a larger one must be one word repeated
            int elementSize = channelCount * channelSize;
            Span<byte> word = stackalloc byte[4];
            for (int i = 0; i < 4; i++)
                word[i] = element[i % elementSize];

            for (int i = 4; i < elementSize; i++)
            {
                if (element[i] != word[i % 4])
                    throw new NotSupportedException($"Vulkan clears a Buffer by repeating one 32-bit word, and a {format} element of this value has different words. Clear it with a value that repeats every 4 bytes (such as 0), or in a compute shader.");
            }

            return BitConverter.ToUInt32(word);

            static uint ConvertChannel(float value, int channelBits, ClearChannelKind kind) => kind switch
            {
                ClearChannelKind.Float when channelBits == 32 => BitConverter.SingleToUInt32Bits(value),
                ClearChannelKind.Float => BitConverter.HalfToUInt16Bits((System.Half) value),
                ClearChannelKind.UNorm => (uint) MathF.Round(Math.Clamp(value, 0, 1) * ((1u << channelBits) - 1)),
                ClearChannelKind.SNorm => (uint) (int) MathF.Round(Math.Clamp(value, -1, 1) * ((1 << (channelBits - 1)) - 1)),
                _ => throw new ArgumentOutOfRangeException(nameof(kind))
            };
        }

        private enum ClearChannelKind { Float, UNorm, SNorm, UInt, SInt }

        /// <summary>
        ///   Gets the channels of a format a Buffer clear can write, or <see langword="null"/> if it can't.
        /// </summary>
        private static (int ChannelCount, int ChannelBits, ClearChannelKind Kind)? GetClearFormat(PixelFormat format) => format switch
        {
            PixelFormat.R32G32B32A32_Float => (4, 32, ClearChannelKind.Float),
            PixelFormat.R32G32B32A32_UInt => (4, 32, ClearChannelKind.UInt),
            PixelFormat.R32G32B32A32_SInt => (4, 32, ClearChannelKind.SInt),
            PixelFormat.R32G32B32_Float => (3, 32, ClearChannelKind.Float),
            PixelFormat.R32G32B32_UInt => (3, 32, ClearChannelKind.UInt),
            PixelFormat.R32G32B32_SInt => (3, 32, ClearChannelKind.SInt),
            PixelFormat.R32G32_Float => (2, 32, ClearChannelKind.Float),
            PixelFormat.R32G32_UInt => (2, 32, ClearChannelKind.UInt),
            PixelFormat.R32G32_SInt => (2, 32, ClearChannelKind.SInt),
            PixelFormat.R32_Float => (1, 32, ClearChannelKind.Float),
            PixelFormat.R32_UInt => (1, 32, ClearChannelKind.UInt),
            PixelFormat.R32_SInt => (1, 32, ClearChannelKind.SInt),

            PixelFormat.R16G16B16A16_Float => (4, 16, ClearChannelKind.Float),
            PixelFormat.R16G16B16A16_UNorm => (4, 16, ClearChannelKind.UNorm),
            PixelFormat.R16G16B16A16_UInt => (4, 16, ClearChannelKind.UInt),
            PixelFormat.R16G16B16A16_SNorm => (4, 16, ClearChannelKind.SNorm),
            PixelFormat.R16G16B16A16_SInt => (4, 16, ClearChannelKind.SInt),
            PixelFormat.R16G16_Float => (2, 16, ClearChannelKind.Float),
            PixelFormat.R16G16_UNorm => (2, 16, ClearChannelKind.UNorm),
            PixelFormat.R16G16_UInt => (2, 16, ClearChannelKind.UInt),
            PixelFormat.R16G16_SNorm => (2, 16, ClearChannelKind.SNorm),
            PixelFormat.R16G16_SInt => (2, 16, ClearChannelKind.SInt),
            PixelFormat.R16_Float => (1, 16, ClearChannelKind.Float),
            PixelFormat.R16_UNorm => (1, 16, ClearChannelKind.UNorm),
            PixelFormat.R16_UInt => (1, 16, ClearChannelKind.UInt),
            PixelFormat.R16_SNorm => (1, 16, ClearChannelKind.SNorm),
            PixelFormat.R16_SInt => (1, 16, ClearChannelKind.SInt),

            PixelFormat.R8G8B8A8_UNorm => (4, 8, ClearChannelKind.UNorm),
            PixelFormat.R8G8B8A8_UInt => (4, 8, ClearChannelKind.UInt),
            PixelFormat.R8G8B8A8_SNorm => (4, 8, ClearChannelKind.SNorm),
            PixelFormat.R8G8B8A8_SInt => (4, 8, ClearChannelKind.SInt),
            PixelFormat.R8G8_UNorm => (2, 8, ClearChannelKind.UNorm),
            PixelFormat.R8G8_UInt => (2, 8, ClearChannelKind.UInt),
            PixelFormat.R8G8_SNorm => (2, 8, ClearChannelKind.SNorm),
            PixelFormat.R8G8_SInt => (2, 8, ClearChannelKind.SInt),
            PixelFormat.R8_UNorm => (1, 8, ClearChannelKind.UNorm),
            PixelFormat.R8_UInt => (1, 8, ClearChannelKind.UInt),
            PixelFormat.R8_SNorm => (1, 8, ClearChannelKind.SNorm),
            PixelFormat.R8_SInt => (1, 8, ClearChannelKind.SInt),

            _ => null
        };

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;texture</exception>
        public unsafe void ClearReadWrite(Texture texture, Vector4 value)
        {
            var clearValue = new VkClearColorValue(value.X, value.Y, value.Z, value.W);
            ClearReadWriteImpl(texture, clearValue);
        }

        public unsafe void ClearReadWrite(Texture texture, Int4 value)
        {
            // D3D11 ClearUnorderedAccessViewUint writes raw integer values.
            // Vulkan vkCmdClearColorImage expects float values for UNORM/SNORM formats, not raw integers.
            // Convert to float to match D3D11 behavior.
            if (texture.ViewFormat.IsUNorm)
            {
                float scale = 1.0f / texture.ViewFormat.UNormMaxValue;
                ClearReadWrite(texture, new Vector4(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale));
                return;
            }
            var clearValue = new VkClearColorValue(*(uint*)&value.X, *(uint*)&value.Y, *(uint*)&value.Z, *(uint*)&value.W);
            ClearReadWriteImpl(texture, clearValue);
        }

        public unsafe void ClearReadWrite(Texture texture, UInt4 value)
        {
            if (texture.ViewFormat.IsUNorm)
            {
                float scale = 1.0f / texture.ViewFormat.UNormMaxValue;
                ClearReadWrite(texture, new Vector4(value.X * scale, value.Y * scale, value.Z * scale, value.W * scale));
                return;
            }
            var clearValue = new VkClearColorValue(value.X, value.Y, value.Z, value.W);
            ClearReadWriteImpl(texture, clearValue);
        }

        private unsafe void ClearReadWriteImpl(Texture texture, VkClearColorValue clearValue)
        {
            ArgumentNullException.ThrowIfNull(texture);
            RecordDebugCounter(DebugCounterKind.Clear);

            var clearRange = texture.NativeResourceRange;

            // Clear requires TransferDstOptimal; leave the texture in the unordered access layout
            // afterwards, matching the Direct3D 12 behavior.
            ResourceBarrierTransition(texture, BarrierLayout.CopyDest);
            BeginCommandOutsideRenderPass();

            GraphicsDevice.NativeDeviceApi.vkCmdClearColorImage(currentCommandList.NativeCommandBuffer, texture.NativeImage, VkImageLayout.TransferDstOptimal, &clearValue, 1, &clearRange);

            ResourceBarrierTransition(texture, BarrierLayout.UnorderedAccess);
        }

        public unsafe void Copy(GraphicsResource source, GraphicsResource destination)
        {
            RecordDebugCounter(DebugCounterKind.Copy);
            // TODO VULKAN: One copy per mip level

            if (source is Texture sourceTexture && destination is Texture destinationTexture)
            {
                if (sourceTexture.Width != destinationTexture.Width ||
                    sourceTexture.Height != destinationTexture.Height ||
                    sourceTexture.Depth != destinationTexture.Depth ||
                    sourceTexture.ArraySize != destinationTexture.ArraySize ||
                    sourceTexture.MipLevelCount != destinationTexture.MipLevelCount)
                    throw new InvalidOperationException($"{nameof(source)} and {nameof(destination)} textures don't match");

                CleanupRenderPass();

                var sourceParent = sourceTexture.RootTexture;
                var destinationParent = destinationTexture.RootTexture;
                BeginTransfers(sourceParent, destinationParent);

                // TODO: compute all regions at once in a single call
                for (var subresource = 0; subresource < sourceTexture.MipLevelCount * sourceTexture.ArraySize; ++subresource)
                {
                    var arraySlice = subresource / sourceTexture.MipLevelCount;
                    var mipLevel = subresource % sourceTexture.MipLevelCount;

                    var sourceOffset = sourceTexture.ComputeBufferOffset(subresource, depthSlice: 0);
                    var destinationOffset = destinationTexture.ComputeBufferOffset(subresource, depthSlice: 0);
                    var size = sourceTexture.ComputeSubResourceSize(subresource);

                    var width = Texture.CalculateMipSize(sourceTexture.Width, mipLevel);
                    var height = Texture.CalculateMipSize(sourceTexture.Height, mipLevel);
                    var depth = Texture.CalculateMipSize(sourceTexture.Depth, mipLevel);

                    // Copy
                    if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                        {
                            var copy = new VkBufferCopy
                            {
                                srcOffset = (ulong) sourceOffset,
                                dstOffset = (ulong) destinationOffset,
                                size = (ulong) size,
                            };
                            GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeBuffer, regionCount: 1, &copy);
                        }
                        else
                        {
                            var copy = new VkBufferImageCopy
                            {
                                imageSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint) mipLevel, (uint) arraySlice, layerCount: 1),
                                imageExtent = new VkExtent3D(width, height, depth),
                                bufferOffset = (ulong) destinationOffset
                            };
                            GraphicsDevice.NativeDeviceApi.vkCmdCopyImageToBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeBuffer, regionCount: 1, &copy);
                        }

                        // VkFence for host access
                        destinationParent.CommandListFenceValue = null;
                        destinationParent.UpdatingCommandList = this;
                        currentCommandList.StagingResources.Add(destinationParent);
                    }
                    else
                    {
                        var destinationSubresource = new VkImageSubresourceLayers(destinationParent.NativeImageAspect, (uint) mipLevel, (uint) arraySlice, layerCount: 1);

                        if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                        {
                            var copy = new VkBufferImageCopy
                            {
                                imageSubresource = destinationSubresource,
                                imageExtent = new VkExtent3D(width, height, depth),
                                bufferOffset = (ulong) sourceOffset
                            };
                            GraphicsDevice.NativeDeviceApi.vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, regionCount: 1, &copy);
                        }
                        else
                        {
                            // Image to image copy: process array all at once
                            destinationSubresource.layerCount = (uint)destinationTexture.ArraySize;
                            if (arraySlice == 0)
                            {
                                var copy = new VkImageCopy
                                {
                                    srcSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)mipLevel, (uint)arraySlice, (uint)sourceTexture.ArraySize),
                                    dstSubresource = destinationSubresource,
                                    extent = new VkExtent3D(width, height, depth)
                                };
                                GraphicsDevice.NativeDeviceApi.vkCmdCopyImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, regionCount: 1, &copy);
                            }
                        }
                    }
                }

                EndTransfers(sourceParent, destinationParent);
            }
            else if (source is Buffer sourceBuffer && destination is Buffer destinationBuffer)
            {
                BeginBufferTransfer(sourceBuffer.NativeBuffer, sourceBuffer.NativePipelineStageMask, sourceBuffer.NativeAccessMask, VkAccessFlags.TransferRead);
                BeginBufferTransfer(destinationBuffer.NativeBuffer, destinationBuffer.NativePipelineStageMask, destinationBuffer.NativeAccessMask, VkAccessFlags.TransferWrite);
                BeginCommandOutsideRenderPass();

                var copy = new VkBufferCopy
                {
                    srcOffset = 0,
                    dstOffset = 0,
                    size = (uint)sourceBuffer.SizeInBytes
                };
                GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceBuffer.NativeBuffer, destinationBuffer.NativeBuffer, regionCount: 1, &copy);

                if (destinationBuffer.Usage == GraphicsResourceUsage.Staging)
                {
                    // VkFence for host access
                    destinationBuffer.CommandListFenceValue = null;
                    destinationBuffer.UpdatingCommandList = this;
                    currentCommandList.StagingResources.Add(destinationBuffer);
                }

                EndBufferTransfer(sourceBuffer.NativeBuffer, sourceBuffer.NativePipelineStageMask, sourceBuffer.NativeAccessMask, VkAccessFlags.TransferRead);
                EndBufferTransfer(destinationBuffer.NativeBuffer, destinationBuffer.NativePipelineStageMask, destinationBuffer.NativeAccessMask, VkAccessFlags.TransferWrite);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        /// <remarks>
        ///   Vulkan resolves between images of the same format: <paramref name="format"/> is ignored.
        /// </remarks>
        public unsafe void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            ArgumentNullException.ThrowIfNull(sourceMultisampleTexture);
            ArgumentNullException.ThrowIfNull(destTexture);

            RecordDebugCounter(DebugCounterKind.Copy);

            if (!sourceMultisampleTexture.IsMultiSampled)
                throw new ArgumentException("Source Texture is not a MSAA Texture", nameof(sourceMultisampleTexture));

            ResourceBarrierTransition(sourceMultisampleTexture, BarrierLayout.ResolveSource);
            ResourceBarrierTransition(destTexture, BarrierLayout.ResolveDest);
            BeginCommandOutsideRenderPass();

            var source = sourceMultisampleTexture.RootTexture;
            var destination = destTexture.RootTexture;
            var mipLevel = destSubResource % destination.MipLevelCount;
            var region = new VkImageResolve
            {
                srcSubresource = GetSubresourceLayers(source, source.NativeImageAspect, sourceSubResource),
                dstSubresource = GetSubresourceLayers(destination, destination.NativeImageAspect, destSubResource),
                extent = new VkExtent3D(Texture.CalculateMipSize(destination.Width, mipLevel), Texture.CalculateMipSize(destination.Height, mipLevel), 1),
            };
            GraphicsDevice.NativeDeviceApi.vkCmdResolveImage(currentCommandList.NativeCommandBuffer, source.NativeImage, VkImageLayout.TransferSrcOptimal, destination.NativeImage, VkImageLayout.TransferDstOptimal, regionCount: 1, &region);
        }

        /// <summary>
        ///   Resolves a subresource index into the Vulkan subresource layers it addresses.
        /// </summary>
        /// <param name="subresourceIndex">
        ///   The subresource index, encoded as <c>arraySlice * MipLevelCount + mipLevel</c>
        ///   (see <see cref="Texture.GetSubResourceIndex"/>).
        /// </param>
        private static VkImageSubresourceLayers GetSubresourceLayers(Texture texture, VkImageAspectFlags imageAspect, int subresourceIndex)
        {
            return new VkImageSubresourceLayers(imageAspect,
                                                mipLevel: (uint) (subresourceIndex % texture.MipLevelCount),
                                                baseArrayLayer: (uint) (subresourceIndex / texture.MipLevelCount),
                                                layerCount: 1);
        }

        public unsafe void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourceRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            RecordDebugCounter(DebugCounterKind.Copy);
            // TODO VULKAN: One copy per mip level

            if (source is Texture sourceTexture && destination is Texture destinationTexture)
            {
                CleanupRenderPass();

                var mipmapDescription = sourceTexture.GetMipMapDescription(sourceSubresource % sourceTexture.MipLevelCount);

                var region = sourceRegion ?? new ResourceRegion(left: 0, top: 0, front: 0, mipmapDescription.Width, mipmapDescription.Height, mipmapDescription.Depth);

                var sourceParent = sourceTexture.RootTexture;
                var destinationParent = destinationTexture.RootTexture;
                BeginTransfers(sourceParent, destinationParent);

                // Copy
                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        throw new NotImplementedException();
                        //var copy = new VkBufferCopy
                        //{
                        //    sourceOffset = 0,
                        //    destinationOffset = 0,
                        //    size = (uint) (sourceParent.ViewWidth * sourceParent.ViewHeight * sourceParent.ViewDepth * sourceParent.ViewFormat.SizeInBytes())
                        //};
                        //vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeBuffer, 1, &copy);
                    }
                    else
                    {
                        var copy = new VkBufferImageCopy
                        {
                            bufferOffset = (ulong)destinationTexture.ComputeBufferOffset(destinationSubResource, 0),
                            bufferImageHeight = (uint)destinationTexture.Height,
                            bufferRowLength = (uint)destinationTexture.Width,
                            imageSubresource = GetSubresourceLayers(sourceTexture, sourceParent.NativeImageAspect, sourceSubresource),
                            imageOffset = new VkOffset3D(region.Left, region.Top, region.Front),
                            imageExtent = new VkExtent3D((uint)(region.Right - region.Left), (uint)(region.Bottom - region.Top), (uint)(region.Back - region.Front))
                        };
                        GraphicsDevice.NativeDeviceApi.vkCmdCopyImageToBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeBuffer, 1, &copy);
                    }

                    //// VkFence for host access
                    destinationParent.CommandListFenceValue = null;
                    destinationParent.UpdatingCommandList = this;
                    currentCommandList.StagingResources.Add(destinationParent);
                }
                else
                {
                    var destinationSubresource = GetSubresourceLayers(destinationTexture, destinationParent.NativeImageAspect, destinationSubResource);

                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        if (region.Left != 0 || region.Top != 0 || region.Front != 0
                            && region.Right != mipmapDescription.Width || region.Bottom != mipmapDescription.Height || region.Back != mipmapDescription.Depth)
                            throw new NotImplementedException("Copy from Staging doesn't support source region other than full texture");

                        var copy = new VkBufferImageCopy
                        {
                            imageSubresource = destinationSubresource,
                            bufferOffset = (ulong) sourceTexture.ComputeBufferOffset(sourceSubresource, 0),
                            bufferImageHeight = (uint) sourceTexture.Height,
                            bufferRowLength = (uint) sourceTexture.Width,
                            imageOffset = new VkOffset3D(dstX, dstY, dstZ),
                            imageExtent = new VkExtent3D(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front)
                        };
                        GraphicsDevice.NativeDeviceApi.vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, regionCount: 1, &copy);
                    }
                    else
                    {
                        var copy = new VkImageCopy
                        {
                            srcSubresource = GetSubresourceLayers(sourceTexture, sourceParent.NativeImageAspect, sourceSubresource),
                            srcOffset = new VkOffset3D(region.Left, region.Top, region.Front),
                            dstSubresource = destinationSubresource,
                            dstOffset = new VkOffset3D(dstX, dstY, dstZ),
                            extent = new VkExtent3D(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front)
                        };
                        GraphicsDevice.NativeDeviceApi.vkCmdCopyImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, regionCount: 1, &copy);
                    }
                }

                EndTransfers(sourceParent, destinationParent);
            }
            else if (source is Buffer sourceBuffer && destination is Buffer destinationBuffer)
            {
                var srcOffset = (ulong)(sourceRegion?.Left ?? 0);
                var size = sourceRegion is { } region2
                    ? (ulong)(region2.Right - region2.Left)
                    : (ulong)sourceBuffer.SizeInBytes;
                var dstOffset = (ulong)dstX;

                BeginBufferTransfer(sourceBuffer.NativeBuffer, sourceBuffer.NativePipelineStageMask, sourceBuffer.NativeAccessMask, VkAccessFlags.TransferRead);
                BeginBufferTransfer(destinationBuffer.NativeBuffer, destinationBuffer.NativePipelineStageMask, destinationBuffer.NativeAccessMask, VkAccessFlags.TransferWrite);
                BeginCommandOutsideRenderPass();

                var copy = new VkBufferCopy { srcOffset = srcOffset, dstOffset = dstOffset, size = size };
                GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceBuffer.NativeBuffer, destinationBuffer.NativeBuffer, regionCount: 1, &copy);

                if (destinationBuffer.Usage == GraphicsResourceUsage.Staging)
                {
                    destinationBuffer.CommandListFenceValue = null;
                    destinationBuffer.UpdatingCommandList = this;
                    currentCommandList.StagingResources.Add(destinationBuffer);
                }

                EndBufferTransfer(sourceBuffer.NativeBuffer, sourceBuffer.NativePipelineStageMask, sourceBuffer.NativeAccessMask, VkAccessFlags.TransferRead);
                EndBufferTransfer(destinationBuffer.NativeBuffer, destinationBuffer.NativePipelineStageMask, destinationBuffer.NativeAccessMask, VkAccessFlags.TransferWrite);
            }
            else
            {
                throw new InvalidOperationException("Cannot copy data between Buffers and Textures.");
            }
        }

        /// <inheritdoc />
        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetInBytes)
        {
            throw new NotImplementedException();
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

            fixed (byte* sourceDataPtr = sourceData)
            {
                UpdateSubResource(resource, subResourceIndex, new DataBox((nint) sourceDataPtr, sourceData.Length, 0));
            }
        }

        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
            if (resource is Texture texture)
            {
                var mipLevel = subResourceIndex % texture.MipLevelCount;

                var width = Texture.CalculateMipSize(texture.Width, mipLevel);
                var height = Texture.CalculateMipSize(texture.Height, mipLevel);
                var depth = Texture.CalculateMipSize(texture.Depth, mipLevel);

                UpdateSubResource(resource, subResourceIndex, databox, new ResourceRegion(left: 0, top: 0, front: 0, width, height, depth));
            }
            else
            {
                if (resource is Buffer buffer)
                {
                    UpdateSubResource(resource, subResourceIndex, databox, new ResourceRegion(left: 0, top: 0, front: 0, buffer.SizeInBytes, bottom: 1, back: 1));
                }
                else
                {
                    throw new InvalidOperationException();
                }
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
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte})" path="/remarks" />
        internal unsafe void UpdateSubResource(GraphicsResource resource, int subResourceIndex, ReadOnlySpan<byte> sourceData, ResourceRegion region)
        {
            ArgumentNullException.ThrowIfNull(resource);

            if (sourceData.IsEmpty)
                return;

            fixed (byte* sourceDataPtr = sourceData)
            {
                UpdateSubResource(resource, subResourceIndex, new DataBox((nint)sourceDataPtr, sourceData.Length, 0), region);
            }
        }

        internal unsafe partial void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox databox, ResourceRegion region)
        {
            // Barriers need to be global to command buffer
            CleanupRenderPass();

            int lengthInBytes = 0;

            int blockSize;
            var texture = resource as Texture;
            if (texture != null)
            {
                lengthInBytes = databox.SlicePitch * (region.Back - region.Front);
                blockSize = texture.Format.BlockSize;
            }
            else
            {
                lengthInBytes = region.Right - region.Left;
                blockSize = 4;
            }

            // Buffer-to-image copies need to be aligned to the pixel size and 4 (always a power of 2)
            var alignmentMask = (blockSize < 4 ? 4 : blockSize) - 1;

            var uploadMemory = GraphicsDevice.AllocateUploadBuffer(lengthInBytes + alignmentMask, out var uploadResource, out var uploadOffset);
            var alignment = ((uploadOffset + alignmentMask) & ~alignmentMask) - uploadOffset;

            MemoryUtilities.CopyWithAlignmentFallback((void*) (uploadMemory + alignment), (void*) databox.DataPointer, (uint) lengthInBytes);

            // The host wrote the upload buffer, the copy reads it
            AddPendingBarrier(PipelineBarriers.BufferBarrier(uploadResource, VkPipelineStageFlags.Host, VkAccessFlags.HostWrite, VkPipelineStageFlags.Transfer, VkAccessFlags.TransferRead,
                                                             (ulong) (uploadOffset + alignment), (ulong) lengthInBytes));

            if (texture != null)
            {
                texture = texture.RootTexture;

                var mipSlice = subResourceIndex % texture.MipLevelCount;
                var arraySlice = subResourceIndex / texture.MipLevelCount;

                TransitionTexture(texture, range: null, (uint) subResourceIndex, BarrierLayout.CopyDest, destinationTransferTransitions);

                // TODO VULKAN: Handle depth-stencil (NOTE: only supported on graphics queue)
                // TODO VULKAN: Handle non-packed pitches
                var bufferCopy = new VkBufferImageCopy
                {
                    bufferOffset = (ulong) (uploadOffset + alignment),
                    imageSubresource = new VkImageSubresourceLayers { aspectMask = texture.NativeImageAspect, baseArrayLayer = (uint) arraySlice, layerCount = 1, mipLevel = (uint) mipSlice },
                    bufferRowLength = (uint) (databox.RowPitch * texture.Format.BlockWidth / texture.Format.BlockSize),
                    bufferImageHeight = (uint) (databox.SlicePitch * texture.Format.BlockHeight / databox.RowPitch),
                    imageOffset = new VkOffset3D(region.Left, region.Top, region.Front),
                    imageExtent = new VkExtent3D(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front)
                };
                BeginCommandOutsideRenderPass();
                GraphicsDevice.NativeDeviceApi.vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, uploadResource, texture.NativeImage, VkImageLayout.TransferDstOptimal, 1, &bufferCopy);

                RestoreAfterTransfer(texture, destinationTransferTransitions, BarrierLayout.CopyDest);
            }
            else
            {
                if (resource is Buffer buffer)
                {
                    var bufferCopy = new VkBufferCopy
                    {
                        srcOffset = (ulong) (uploadOffset + alignment),
                        dstOffset = (ulong) region.Left,
                        size = (ulong) lengthInBytes
                    };

                    BeginBufferTransfer(buffer.NativeBuffer, buffer.NativePipelineStageMask, buffer.NativeAccessMask, VkAccessFlags.TransferWrite, bufferCopy.dstOffset, bufferCopy.size);
                    BeginCommandOutsideRenderPass();

                    GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, uploadResource, buffer.NativeBuffer, regionCount: 1, &bufferCopy);

                    EndBufferTransfer(buffer.NativeBuffer, buffer.NativePipelineStageMask, buffer.NativeAccessMask, VkAccessFlags.TransferWrite, bufferCopy.dstOffset, bufferCopy.size);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
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
        public unsafe partial MappedResource MapSubResource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            if (resource == null) throw new ArgumentNullException("resource");

            var rowPitch = 0;
            var usage = GraphicsResourceUsage.Default;

            if (resource is Texture texture)
            {
                usage = texture.Usage;
                if (lengthInBytes == 0)
                    lengthInBytes = texture.ComputeSubResourceSize(subResourceIndex);
                rowPitch = texture.ComputeRowPitch(subResourceIndex % texture.MipLevelCount);

                offsetInBytes += texture.ComputeBufferOffset(subResourceIndex, depthSlice: 0);
            }
            else
            {
                if (resource is Buffer buffer)
                {
                    usage = buffer.Usage;
                    if (lengthInBytes == 0)
                        lengthInBytes = buffer.SizeInBytes;
                }
            }

            if (mapMode == MapMode.Read || mapMode == MapMode.ReadWrite || mapMode == MapMode.Write)
            {
                // Is non-staging ever possible for Read/Write?
                if (usage != GraphicsResourceUsage.Staging)
                    throw new InvalidOperationException();
            }

            if (mapMode != MapMode.WriteNoOverwrite && mapMode != MapMode.Write)
            {
                // Need to wait?
                if (
                    // used in command list which hasn't be submitted yet? (only valid if our own, checked later)
                    resource.UpdatingCommandList is not null
                    // updated in a previous command list which hasn't be finished yet
                    || (resource.CommandListFenceValue is not null && !GraphicsDevice.CommandListFence.IsFenceCompleteInternal(resource.CommandListFenceValue.Value)))
                {
                    // User told us not to wait, return right away
                    if (doNotWait)
                    {
                        return new MappedResource(resource, subResourceIndex, dataBox: default);
                    }

                    if (resource.UpdatingCommandList == this)
                        // Need to flush? (check if part of current command list)
                        // resource.CommandListFenceValue should be set after
                        FlushInternal(false);
                    else if (resource.UpdatingCommandList is not null)
                        // Another command list updated this resource, but it's not been submitted (otherwise it would be stored in resource.CommandListFenceValue
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

            void* mappedMemory;
            GraphicsDevice.NativeDeviceApi.vkMapMemory(GraphicsDevice.NativeDevice, resource.NativeMemory, (ulong) offsetInBytes, (ulong) lengthInBytes, VkMemoryMapFlags.None, &mappedMemory);
            return new MappedResource(resource, subResourceIndex, new DataBox((IntPtr) mappedMemory, rowPitch, slicePitch: 0), offsetInBytes, lengthInBytes);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public unsafe partial void UnmapSubResource(MappedResource unmapped)
        {
            GraphicsDevice.NativeDeviceApi.vkUnmapMemory(GraphicsDevice.NativeDevice, unmapped.Resource.NativeMemory);
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            GraphicsDevice.WaitIdle();

            if (descriptorPool != VkDescriptorPool.Null)
            {
                GraphicsDevice.DescriptorPools.RecycleObject(GraphicsDevice.CommandListFence.NextFenceValue - 1, descriptorPool);
                descriptorPool = VkDescriptorPool.Null;
            }

            CommandBufferPool.Dispose();

            base.OnDestroyed(immediately);
        }

        private unsafe void EnsureRenderPass()
        {
            if (activePipeline == null || activePipeline.NativePipeline == VkPipeline.Null)
                return;

            // The depth attachment layout follows the bound view and the declared stage access:
            // a read-only depth keeps the image in DepthStencilReadOnlyOptimal so it can be
            // sampled at the same time
            bool depthReadOnly = depthStencilBuffer != null && (depthStencilBuffer.IsDepthStencilReadOnly || DepthStencilAccess == DepthStencilAccess.Read);

            // The render pass instance stays active across pipeline changes as long as the
            // attachments and the depth layout stay the same
            if (activeRendering && !renderingDirty && activeDepthReadOnly == depthReadOnly)
                return;

            // End old render pass instance
            CleanupRenderPass();

            var renderTarget = RenderTargetCount > 0 ? renderTargets[0] : depthStencilBuffer;
            if (renderTarget == null)
                return;

            // An attachment mismatch with the pipeline's declared formats is undefined behavior that
            // loses the device on strict drivers; fail loud in debug (only, to not break drivers that
            // tolerate it) instead.
            if (GraphicsDevice.IsDebugMode)
            {
                var output = activePipeline.Description.Output;
                if (RenderTargetCount != output.RenderTargetCount || (depthStencilBuffer != null) != (output.DepthStencilFormat != PixelFormat.None))
                    throw new InvalidOperationException(
                        $"Bound render targets ({RenderTargetCount} color, depth: {depthStencilBuffer != null}) do not match the active pipeline's output " +
                        $"({output.RenderTargetCount} color, depth: {output.DepthStencilFormat != PixelFormat.None}). The render targets bound on the command list must match the pipeline's Output description.");
            }

            // Clear attachments if needed
            for (int index = 0; index < RenderTargetCount; index++)
            {
                if (!renderTarget.IsInitialized)
                {
                    Clear(renderTargets[index], Color.Transparent);
                }
            }

            if (depthStencilBuffer != null && !depthStencilBuffer.IsInitialized)
            {
                Clear(depthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
            }

            // Start new render pass instance
            var colorAttachments = stackalloc VkRenderingAttachmentInfo[RenderTargetCount > 0 ? RenderTargetCount : 1];
            for (int i = 0; i < RenderTargetCount; i++)
            {
                colorAttachments[i] = new VkRenderingAttachmentInfo
                {
                    sType = VkStructureType.RenderingAttachmentInfo,
                    imageView = renderTargets[i].NativeColorAttachmentView,
                    imageLayout = VkImageLayout.ColorAttachmentOptimal,
                    loadOp = VkAttachmentLoadOp.Load,
                    storeOp = VkAttachmentStoreOp.Store,
                };
            }

            var depthAttachment = new VkRenderingAttachmentInfo
            {
                sType = VkStructureType.RenderingAttachmentInfo,
                imageView = depthStencilBuffer?.NativeDepthStencilView ?? VkImageView.Null,
                imageLayout = depthReadOnly ? VkImageLayout.DepthStencilReadOnlyOptimal : VkImageLayout.DepthStencilAttachmentOptimal,
                loadOp = VkAttachmentLoadOp.Load,
                // A read-only depth is never written, so storing it would count as a write access
                storeOp = depthReadOnly ? VkAttachmentStoreOp.None : VkAttachmentStoreOp.Store,
            };

            bool hasStencil = depthStencilBuffer != null && (depthStencilBuffer.NativeImageAspect & VkImageAspectFlags.Stencil) != 0;

            var renderingInfo = new VkRenderingInfo
            {
                sType = VkStructureType.RenderingInfo,
                renderArea = new VkRect2D(0, 0, (uint)renderTarget.ViewWidth, (uint)renderTarget.ViewHeight),
                layerCount = 1, // TODO VULKAN: Use correct view depth/array size
                colorAttachmentCount = (uint)RenderTargetCount,
                pColorAttachments = colorAttachments,
                pDepthAttachment = depthStencilBuffer != null ? &depthAttachment : null,
                pStencilAttachment = hasStencil ? &depthAttachment : null,
            };

            // The barriers of the draws to come (and of the attachment clears above) go before the render pass
            FlushBarriers();
            GraphicsDevice.NativeDeviceApi.vkCmdBeginRendering(currentCommandList.NativeCommandBuffer, &renderingInfo);

            activeRendering = true;
            activeDepthReadOnly = depthReadOnly;
            renderingDirty = false;
        }

        private unsafe void CleanupRenderPass()
        {
            if (activeRendering)
            {
                GraphicsDevice.NativeDeviceApi.vkCmdEndRendering(currentCommandList.NativeCommandBuffer);
                activeRendering = false;
            }
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct DescriptorData
        {
            [FieldOffset(0)]
            public VkDescriptorBufferInfo BufferInfo;

            [FieldOffset(0)]
            public VkDescriptorImageInfo ImageInfo;

            [FieldOffset(0)]
            public VkBufferView BufferView;
        }
    }
}

#endif
