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
        internal CommandBufferPool CommandBufferPool;

        private VkRenderPass activeRenderPass;
        private VkRenderPass previousRenderPass;
        private PipelineState activePipeline;
        private bool pipelineDirty = true;

        private readonly Dictionary<FramebufferKey, VkFramebuffer> framebuffers = new();
        private readonly VkImageView[] framebufferAttachments = new VkImageView[9];
        private int framebufferAttachmentCount;
        private bool framebufferDirty = true;
        private VkFramebuffer activeFramebuffer;

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

            CleanupRenderPass();
            boundDescriptorSets.Clear();

            framebuffers.Clear();
            framebufferDirty = true;

            currentCommandList.Builder = this;
            currentCommandList.NativeCommandBuffer = CommandBufferPool.GetObject(GraphicsDevice.CommandListFence.GetCompletedValue());
            currentCommandList.DescriptorPools = GraphicsDevice.DescriptorPoolLists.Acquire();
            currentCommandList.StagingResources = GraphicsDevice.StagingResourceLists.Acquire();

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
            // End active render pass
            CleanupRenderPass();

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
            var oldFramebufferAttachmentCount = framebufferAttachmentCount;
            framebufferAttachmentCount = renderTargets.Length;

            for (int i = 0; i < renderTargets.Length; i++)
            {
                if (renderTargets[i].NativeColorAttachmentView != framebufferAttachments[i])
                    framebufferDirty = true;

                framebufferAttachments[i] = renderTargets[i].NativeColorAttachmentView;
            }

            if (depthStencilBuffer != null)
            {
                if (depthStencilBuffer.NativeDepthStencilView != framebufferAttachments[renderTargets.Length])
                    framebufferDirty = true;

                framebufferAttachments[renderTargets.Length] = depthStencilBuffer.NativeDepthStencilView;
                framebufferAttachmentCount++;
            }

            if (framebufferAttachmentCount != oldFramebufferAttachmentCount)
                framebufferDirty = true;
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
            if (viewportDirty)
            {
                GraphicsDevice.NativeDeviceApi.vkCmdSetViewport(currentCommandList.NativeCommandBuffer, firstViewport: 0, viewportCount: 1, (VkViewport*) &viewportCopy);
                viewportDirty = false;
            }

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
            // Lazily set the render pass and frame buffer
            EnsureRenderPass();
            BindPipeline();
            BindDescriptorSets();
            SetViewportImpl();
            GraphicsDevice.NativeDeviceApi.vkCmdSetStencilReference(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, activeStencilReference ?? 0);
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
                            descriptorData->ImageInfo = new VkDescriptorImageInfo { imageView = texture?.NativeImageView ?? GraphicsDevice.EmptyTexture.NativeImageView, imageLayout = VkImageLayout.ShaderReadOnlyOptimal };
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
                        buffer = heapObject.Value as Buffer;
                        descriptorData->BufferView = buffer?.NativeBufferView ?? (mapping.ResourceElementIsInteger ? GraphicsDevice.EmptyTexelBufferInt.NativeBufferView : GraphicsDevice.EmptyTexelBufferFloat.NativeBufferView);
                        write->pTexelBufferView = &descriptorData->BufferView;
                        break;

                    case VkDescriptorType.StorageBuffer:
                        buffer = heapObject.Value as Buffer;
                        descriptorData->BufferInfo = new VkDescriptorBufferInfo { buffer = buffer?.NativeBuffer ?? VkBuffer.Null, offset = (ulong)heapObject.Offset, range = (ulong)(buffer?.SizeInBytes ?? 0)};
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

        public unsafe void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            if (resource is Texture texture)
            {
                if (texture.ParentTexture != null)
                    texture = texture.ParentTexture;

                // TODO VULKAN: Check for change

                var oldLayout = texture.NativeLayout;
                var oldAccessMask = texture.NativeAccessMask;

                var sourceStages = resource.NativePipelineStageMask;

                switch (newState)
                {
                    case GraphicsResourceState.RenderTarget:
                        texture.NativeLayout = VkImageLayout.ColorAttachmentOptimal;
                        texture.NativeAccessMask = VkAccessFlags.ColorAttachmentWrite;
                        texture.NativePipelineStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
                        break;
                    case GraphicsResourceState.Present:
                        texture.NativeLayout = VkImageLayout.PresentSrcKHR;
                        texture.NativeAccessMask = VkAccessFlags.MemoryRead;
                        texture.NativePipelineStageMask = VkPipelineStageFlags.BottomOfPipe;
                        break;
                    case GraphicsResourceState.DepthWrite:
                        texture.NativeLayout = VkImageLayout.DepthStencilAttachmentOptimal;
                        texture.NativeAccessMask = VkAccessFlags.DepthStencilAttachmentWrite;
                        texture.NativePipelineStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests | VkPipelineStageFlags.LateFragmentTests;
                        break;
                    case GraphicsResourceState.PixelShaderResource:
                        texture.NativeLayout = VkImageLayout.ShaderReadOnlyOptimal;
                        texture.NativeAccessMask = VkAccessFlags.ShaderRead;
                        texture.NativePipelineStageMask = VkPipelineStageFlags.FragmentShader | VkPipelineStageFlags.ComputeShader;
                        break;
                    case GraphicsResourceState.GenericRead:
                        texture.NativeLayout = VkImageLayout.General;
                        texture.NativeAccessMask = VkAccessFlags.ShaderRead | VkAccessFlags.TransferRead | VkAccessFlags.IndirectCommandRead | VkAccessFlags.ColorAttachmentRead | VkAccessFlags.DepthStencilAttachmentRead | VkAccessFlags.InputAttachmentRead | VkAccessFlags.VertexAttributeRead | VkAccessFlags.IndexRead | VkAccessFlags.UniformRead;
                        texture.NativePipelineStageMask = VkPipelineStageFlags.AllCommands;
                        break;
                    default:
                        texture.NativeLayout = VkImageLayout.General;
                        texture.NativeAccessMask = (VkAccessFlags)0x1FFFF; // TODO VULKAN: Don't hard-code this
                        texture.NativePipelineStageMask = VkPipelineStageFlags.AllCommands;
                        break;
                }

                if (oldLayout == texture.NativeLayout && oldAccessMask == texture.NativeAccessMask)
                    return;

                if (oldLayout == VkImageLayout.Undefined || oldLayout == VkImageLayout.PresentSrcKHR)
                    sourceStages = VkPipelineStageFlags.TopOfPipe;

                // End render pass, so barrier effects all commands in the buffer
                CleanupRenderPass();

                var memoryBarrier = new VkImageMemoryBarrier(texture.NativeImage, new VkImageSubresourceRange(texture.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue), oldAccessMask, texture.NativeAccessMask, oldLayout, texture.NativeLayout);
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceStages, texture.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);
            }
            else
            {
                throw new NotImplementedException();
            }
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
            CleanupRenderPass();
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
            CleanupRenderPass();
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
            if (argumentsBuffer == null) throw new ArgumentNullException("argumentsBuffer");

            PrepareDraw();

            throw new NotImplementedException();
            //NativeCommandBuffer.DrawIndirect(argumentsBuffer.NativeBuffer, (ulong) alignedByteOffsetForArgs, );
            //NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);

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

            GraphicsDevice.NativeDeviceApi.vkCmdDraw(currentCommandList.NativeCommandBuffer, (uint) vertexCountPerInstance, (uint) instanceCount, (uint) startVertexLocation, (uint) startVertexLocation);
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
            if (argumentsBuffer == null) throw new ArgumentNullException("argumentsBuffer");

            PrepareDraw();

            throw new NotImplementedException();
            //NativeDeviceContext.DrawIndexedInstancedIndirect(argumentsBuffer.NativeBuffer, alignedByteOffsetForArgs);

            GraphicsDevice.FrameDrawCalls++;
        }

        /// <summary>
        /// Begins profiling.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public unsafe void BeginProfile(Color4 profileColor, string name)
        {
            if (GraphicsDevice.IsProfilingSupported)
            {
                var bytes = System.Text.Encoding.ASCII.GetBytes(name);

                fixed (byte* bytesPointer = &bytes[0])
                {
                    var profileColorCopy = profileColor;
                    var debugMarkerInfo = new VkDebugMarkerMarkerInfoEXT
                    {
                        sType = VkStructureType.DebugMarkerMarkerInfoEXT,
                        pMarkerName = bytesPointer
                    };
                    GraphicsDevice.NativeDeviceApi.vkCmdDebugMarkerBeginEXT(currentCommandList.NativeCommandBuffer, &debugMarkerInfo);
                }
            }
        }

        /// <summary>
        /// Ends profiling.
        /// </summary>
        public void EndProfile()
        {
            if (GraphicsDevice.IsProfilingSupported)
            {
                GraphicsDevice.NativeDeviceApi.vkCmdDebugMarkerEndEXT(currentCommandList.NativeCommandBuffer);
            }
        }
        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="query">The timestamp query.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            GraphicsDevice.NativeDeviceApi.vkCmdWriteTimestamp(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.AllCommands, queryPool.NativeQueryPool, (uint) index);
        }

        public void ResetQueryPool(QueryPool queryPool)
        {
            GraphicsDevice.NativeDeviceApi.vkCmdResetQueryPool(currentCommandList.NativeCommandBuffer, queryPool.NativeQueryPool, firstQuery: 0, (uint) queryPool.QueryCount);
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
            // Barriers need to be global to command buffer
            CleanupRenderPass();

            var barrierRange = depthStencilBuffer.NativeResourceRange;

            // Adjust aspectMask to clear only the specified part (depth or stencil)
            var clearRange = depthStencilBuffer.NativeResourceRange;
            clearRange.aspectMask = VkImageAspectFlags.None;

            if ((options & DepthStencilClearOptions.DepthBuffer) != 0)
                clearRange.aspectMask |= VkImageAspectFlags.Depth & depthStencilBuffer.NativeImageAspect;

            if ((options & DepthStencilClearOptions.Stencil) != 0)
                clearRange.aspectMask |= VkImageAspectFlags.Stencil & depthStencilBuffer.NativeImageAspect;

            var memoryBarrier = new VkImageMemoryBarrier(depthStencilBuffer.NativeImage, barrierRange, depthStencilBuffer.NativeAccessMask, VkAccessFlags.TransferWrite, depthStencilBuffer.NativeLayout, VkImageLayout.TransferDstOptimal);
            GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, depthStencilBuffer.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 0, bufferMemoryBarriers: null, imageMemoryBarrierCount: 1, &memoryBarrier);

            var clearValue = new VkClearDepthStencilValue(depth, stencil);
            GraphicsDevice.NativeDeviceApi.vkCmdClearDepthStencilImage(currentCommandList.NativeCommandBuffer, depthStencilBuffer.NativeImage, VkImageLayout.TransferDstOptimal, &clearValue, rangeCount: 1, &clearRange);

            memoryBarrier = new VkImageMemoryBarrier(depthStencilBuffer.NativeImage, barrierRange, VkAccessFlags.TransferWrite, depthStencilBuffer.NativeAccessMask, VkImageLayout.TransferDstOptimal, depthStencilBuffer.NativeLayout);
            GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, depthStencilBuffer.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 0, bufferMemoryBarriers: null, imageMemoryBarrierCount: 1, &memoryBarrier);

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
            // TODO VULKAN: Detect if inside render pass. If so, NativeCommandBuffer.ClearAttachments()
            // Barriers need to be global to command buffer
            CleanupRenderPass();

            var clearRange = renderTarget.NativeResourceRange;

            var memoryBarrier = new VkImageMemoryBarrier(renderTarget.NativeImage, clearRange, renderTarget.NativeAccessMask, VkAccessFlags.TransferWrite, renderTarget.NativeLayout, VkImageLayout.TransferDstOptimal);
            GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, renderTarget.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 0, bufferMemoryBarriers: null, imageMemoryBarrierCount: 1, &memoryBarrier);

            GraphicsDevice.NativeDeviceApi.vkCmdClearColorImage(currentCommandList.NativeCommandBuffer, renderTarget.NativeImage, VkImageLayout.TransferDstOptimal, (VkClearColorValue*) &color, rangeCount: 1, &clearRange);

            memoryBarrier = new VkImageMemoryBarrier(renderTarget.NativeImage, clearRange, VkAccessFlags.TransferWrite, renderTarget.NativeAccessMask, VkImageLayout.TransferDstOptimal, renderTarget.NativeLayout);
            GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, renderTarget.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 0, bufferMemoryBarriers: null, imageMemoryBarrierCount: 1, &memoryBarrier);

            renderTarget.IsInitialized = true;
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
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
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;texture</exception>
        public void ClearReadWrite(Texture texture, Vector4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;texture</exception>
        public void ClearReadWrite(Texture texture, Int4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="ArgumentNullException">texture</exception>
        /// <exception cref="ArgumentException">Expecting buffer supporting UAV;texture</exception>
        public void ClearReadWrite(Texture texture, UInt4 value)
        {
            throw new NotImplementedException();
        }

        public unsafe void Copy(GraphicsResource source, GraphicsResource destination)
        {
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

                var imageBarriers = stackalloc VkImageMemoryBarrier[2];
                var bufferBarriers = stackalloc VkBufferMemoryBarrier[2];

                var sourceParent = sourceTexture.ParentTexture ?? sourceTexture;
                var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                uint bufferBarrierCount = 0;
                uint imageBarrierCount = 0;

                // Initial barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new VkBufferMemoryBarrier(sourceParent.NativeBuffer, sourceTexture.NativeAccessMask, VkAccessFlags.TransferRead);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(sourceParent.NativeImage, new VkImageSubresourceRange(sourceParent.NativeImageAspect, baseMipLevel: 0, levelCount: uint.MaxValue, baseArrayLayer: 0, layerCount: uint.MaxValue), sourceTexture.NativeAccessMask, VkAccessFlags.TransferRead, sourceTexture.NativeLayout, VkImageLayout.TransferSrcOptimal);
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new VkBufferMemoryBarrier(destinationParent.NativeBuffer, destinationTexture.NativeAccessMask, VkAccessFlags.TransferWrite);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(destinationParent.NativeImage, new VkImageSubresourceRange(destinationParent.NativeImageAspect, baseMipLevel: 0, levelCount: uint.MaxValue, baseArrayLayer: 0, layerCount: uint.MaxValue), destinationTexture.NativeAccessMask, VkAccessFlags.TransferWrite, destinationTexture.NativeLayout, VkImageLayout.TransferDstOptimal);
                }

                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);

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

                imageBarrierCount = 0;
                bufferBarrierCount = 0;

                // Final barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].srcAccessMask = VkAccessFlags.TransferRead;
                    bufferBarriers[bufferBarrierCount].dstAccessMask = sourceParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].oldLayout = VkImageLayout.TransferSrcOptimal;
                    imageBarriers[imageBarrierCount].newLayout = sourceParent.NativeLayout;
                    imageBarriers[imageBarrierCount].srcAccessMask = VkAccessFlags.TransferRead;
                    imageBarriers[imageBarrierCount].dstAccessMask = sourceParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].srcAccessMask = VkAccessFlags.TransferWrite;
                    bufferBarriers[bufferBarrierCount].dstAccessMask = destinationParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].oldLayout = VkImageLayout.TransferDstOptimal;
                    imageBarriers[imageBarrierCount].newLayout = destinationParent.NativeLayout;
                    imageBarriers[imageBarrierCount].srcAccessMask = VkAccessFlags.TransferWrite;
                    imageBarriers[imageBarrierCount].dstAccessMask = destinationParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);
            }
            else if (source is Buffer sourceBuffer && destination is Buffer destinationBuffer)
            {
                var bufferBarriers = stackalloc VkBufferMemoryBarrier[2];
                bufferBarriers[0] = new VkBufferMemoryBarrier(sourceBuffer.NativeBuffer, sourceBuffer.NativeAccessMask, VkAccessFlags.TransferRead);
                bufferBarriers[1] = new VkBufferMemoryBarrier(destinationBuffer.NativeBuffer, destinationBuffer.NativeAccessMask, VkAccessFlags.TransferWrite);
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceBuffer.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 2, bufferBarriers, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);

                var copy = new VkBufferCopy
                {
                    srcOffset = 0,
                    dstOffset = 0,
                    size = (uint) sourceBuffer.SizeInBytes
                };
                GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceBuffer.NativeBuffer, destinationBuffer.NativeBuffer, regionCount: 1, &copy);

                bufferBarriers[0] = new VkBufferMemoryBarrier(sourceBuffer.NativeBuffer, VkAccessFlags.TransferRead, sourceBuffer.NativeAccessMask);
                bufferBarriers[1] = new VkBufferMemoryBarrier(destinationBuffer.NativeBuffer, VkAccessFlags.TransferWrite, destinationBuffer.NativeAccessMask);
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, sourceBuffer.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 2, bufferBarriers, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);
            }
            else
            {
                throw new InvalidOperationException();
            }
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            throw new NotImplementedException();
        }

        public unsafe void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourceRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            // TODO VULKAN: One copy per mip level

            if (source is Texture sourceTexture && destination is Texture destinationTexture)
            {
                CleanupRenderPass();

                var mipmapDescription = sourceTexture.GetMipMapDescription(sourceSubresource % sourceTexture.MipLevelCount);

                var region = sourceRegion ?? new ResourceRegion(left: 0, top: 0, front: 0, mipmapDescription.Width, mipmapDescription.Height, mipmapDescription.Depth);

                var imageBarriers = stackalloc VkImageMemoryBarrier[2];
                var bufferBarriers = stackalloc VkBufferMemoryBarrier[2];

                var sourceParent = sourceTexture.ParentTexture ?? sourceTexture;
                var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                uint bufferBarrierCount = 0;
                uint imageBarrierCount = 0;

                // Initial barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new VkBufferMemoryBarrier(sourceParent.NativeBuffer, sourceParent.NativeAccessMask, VkAccessFlags.TransferRead);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(sourceParent.NativeImage, new VkImageSubresourceRange(sourceParent.NativeImageAspect, baseMipLevel: 0, levelCount: uint.MaxValue, baseArrayLayer: 0, layerCount: uint.MaxValue), sourceParent.NativeAccessMask, VkAccessFlags.TransferRead, sourceParent.NativeLayout, VkImageLayout.TransferSrcOptimal);
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new VkBufferMemoryBarrier(destinationParent.NativeBuffer, destinationParent.NativeAccessMask, VkAccessFlags.TransferWrite);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(destinationParent.NativeImage, new VkImageSubresourceRange(destinationParent.NativeImageAspect, baseMipLevel: 0, levelCount: uint.MaxValue, baseArrayLayer: 0, layerCount: uint.MaxValue), destinationParent.NativeAccessMask, VkAccessFlags.TransferWrite, destinationParent.NativeLayout, VkImageLayout.TransferDstOptimal);
                }

                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);

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
                            // Review: Method parameter is ignored, D3D12 doesn't do that and ignore texture view details
                            imageSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.MipLevel, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize),
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
                    // Review: Method parameter is ignored, D3D12 doesn't do that and ignore texture view details
                    var destinationSubresource = new VkImageSubresourceLayers(destinationParent.NativeImageAspect, (uint) destinationTexture.MipLevel, (uint) destinationTexture.ArraySlice, (uint) destinationTexture.ArraySize);

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
                            srcSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint) sourceTexture.MipLevel, (uint) sourceTexture.ArraySlice, (uint) sourceTexture.ArraySize),
                            srcOffset = new VkOffset3D(region.Left, region.Top, region.Front),
                            dstSubresource = destinationSubresource,
                            dstOffset = new VkOffset3D(dstX, dstY, dstZ),
                            extent = new VkExtent3D(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front)
                        };
                        GraphicsDevice.NativeDeviceApi.vkCmdCopyImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, regionCount: 1, &copy);
                    }
                }

                imageBarrierCount = 0;
                bufferBarrierCount = 0;

                // Final barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].srcAccessMask = VkAccessFlags.TransferRead;
                    bufferBarriers[bufferBarrierCount].dstAccessMask = sourceParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].oldLayout = VkImageLayout.TransferSrcOptimal;
                    imageBarriers[imageBarrierCount].newLayout = sourceParent.NativeLayout;
                    imageBarriers[imageBarrierCount].srcAccessMask = VkAccessFlags.TransferRead;
                    imageBarriers[imageBarrierCount].dstAccessMask = sourceParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].srcAccessMask = VkAccessFlags.TransferWrite;
                    bufferBarriers[bufferBarrierCount].dstAccessMask = destinationParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].oldLayout = VkImageLayout.TransferDstOptimal;
                    imageBarriers[imageBarrierCount].newLayout = destinationParent.NativeLayout;
                    imageBarriers[imageBarrierCount].srcAccessMask = VkAccessFlags.TransferWrite;
                    imageBarriers[imageBarrierCount].dstAccessMask = destinationParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);
            }
            else
            {
                throw new NotImplementedException();
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

            var uploadBufferMemoryBarrier = new VkBufferMemoryBarrier(uploadResource, VkAccessFlags.HostWrite, VkAccessFlags.TransferRead, (ulong) (uploadOffset + alignment), (ulong) lengthInBytes);

            if (texture != null)
            {
                var mipSlice = subResourceIndex % texture.MipLevelCount;
                var arraySlice = subResourceIndex / texture.MipLevelCount;
                var subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, (uint) mipSlice, levelCount: 1, (uint) arraySlice, 1);

                var memoryBarrier = new VkImageMemoryBarrier(texture.NativeImage, subresourceRange, texture.NativeAccessMask, VkAccessFlags.TransferWrite, texture.NativeLayout, VkImageLayout.TransferDstOptimal);
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, texture.NativePipelineStageMask | VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 1, &uploadBufferMemoryBarrier, imageMemoryBarrierCount: 1, &memoryBarrier);

                // TODO VULKAN: Handle depth-stencil (NOTE: only supported on graphics queue)
                // TODO VULKAN: Handle non-packed pitches
                var bufferCopy = new VkBufferImageCopy
                {
                    bufferOffset = (ulong) (uploadOffset + alignment),
                    imageSubresource = new VkImageSubresourceLayers { aspectMask = VkImageAspectFlags.Color, baseArrayLayer = (uint) arraySlice, layerCount = 1, mipLevel = (uint) mipSlice },
                    bufferRowLength = (uint) (databox.RowPitch * texture.Format.BlockWidth / texture.Format.BlockSize),
                    bufferImageHeight = (uint) (databox.SlicePitch * texture.Format.BlockHeight / databox.RowPitch),
                    imageOffset = new VkOffset3D(region.Left, region.Top, region.Front),
                    imageExtent = new VkExtent3D(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front)
                };
                GraphicsDevice.NativeDeviceApi.vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, uploadResource, texture.NativeImage, VkImageLayout.TransferDstOptimal, 1, &bufferCopy);

                memoryBarrier = new VkImageMemoryBarrier(texture.NativeImage, subresourceRange, VkAccessFlags.TransferWrite, texture.NativeAccessMask, VkImageLayout.TransferDstOptimal, texture.NativeLayout);
                GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, texture.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 0, bufferMemoryBarriers: null, imageMemoryBarrierCount: 1, &memoryBarrier);
            }
            else
            {
                if (resource is Buffer buffer)
                {
                    var memoryBarriers = stackalloc VkBufferMemoryBarrier[2];

                    var bufferCopy = new VkBufferCopy
                    {
                        srcOffset = (ulong) (uploadOffset + alignment),
                        dstOffset = (ulong) region.Left,
                        size = (ulong) lengthInBytes
                    };

                    memoryBarriers[0] = uploadBufferMemoryBarrier;
                    memoryBarriers[1] = new VkBufferMemoryBarrier(buffer.NativeBuffer, buffer.NativeAccessMask, VkAccessFlags.TransferWrite, bufferCopy.dstOffset, bufferCopy.size);
                    GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, buffer.NativePipelineStageMask | VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 2, memoryBarriers, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);

                    GraphicsDevice.NativeDeviceApi.vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, uploadResource, buffer.NativeBuffer, regionCount: 1, &bufferCopy);

                    var memoryBarrier = new VkBufferMemoryBarrier(buffer.NativeBuffer, VkAccessFlags.TransferWrite, buffer.NativeAccessMask, bufferCopy.dstOffset, bufferCopy.size);
                    GraphicsDevice.NativeDeviceApi.vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, buffer.NativePipelineStageMask, VkDependencyFlags.None, memoryBarrierCount: 0, memoryBarriers: null, bufferMemoryBarrierCount: 1, &memoryBarrier, imageMemoryBarrierCount: 0, imageMemoryBarriers: null);
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
        protected internal override bool OnRecreate()
        {
            Recreate();
            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed(bool immediately = false)
        {
            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkDeviceWaitIdle(GraphicsDevice.NativeDevice));

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
            if (activePipeline == null)
                return;

            var pipelineRenderPass = activePipeline.NativeRenderPass;

            // Reuse the Framebuffer if the VkRenderPass didn't change
            if (previousRenderPass != pipelineRenderPass)
                framebufferDirty = true;

            // Nothing to do. VkRenderPass and Framebuffer are still valid
            if (!framebufferDirty && activeRenderPass == pipelineRenderPass)
                return;

            // End old render pass
            CleanupRenderPass();

            if (pipelineRenderPass != VkRenderPass.Null)
            {
                var renderTarget = RenderTargetCount > 0 ? renderTargets[0] : depthStencilBuffer;

                if (framebufferDirty)
                {
                    // Create new frame buffer
                    fixed (VkImageView* attachmentsPointer = &framebufferAttachments[0])
                    {
                        var framebufferKey = new FramebufferKey(pipelineRenderPass, framebufferAttachmentCount, attachmentsPointer);

                        if (!framebuffers.TryGetValue(framebufferKey, out activeFramebuffer))
                        {
                            var framebufferCreateInfo = new VkFramebufferCreateInfo
                            {
                                sType = VkStructureType.FramebufferCreateInfo,
                                renderPass = pipelineRenderPass,
                                attachmentCount = (uint) framebufferAttachmentCount,
                                pAttachments = attachmentsPointer,
                                width = (uint) renderTarget.ViewWidth,
                                height = (uint) renderTarget.ViewHeight,
                                layers = 1 // TODO VULKAN: Use correct view depth/array size
                            };
                            GraphicsDevice.CheckResult(GraphicsDevice.NativeDeviceApi.vkCreateFramebuffer(GraphicsDevice.NativeDevice, &framebufferCreateInfo, null, out activeFramebuffer));
                            GraphicsDevice.Collect(activeFramebuffer);
                            framebuffers.Add(framebufferKey, activeFramebuffer);
                        }
                    }
                    framebufferDirty = false;
                }

                // Clear attachments if needed
                // TODO VULKAN: Can we use a custom render pass for this?
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

                // Start new render pass
                var renderPassBegin = new VkRenderPassBeginInfo
                {
                    sType = VkStructureType.RenderPassBeginInfo,
                    renderPass = pipelineRenderPass,
                    framebuffer = activeFramebuffer,
                    renderArea = new VkRect2D(0, 0, (uint)renderTarget.ViewWidth, (uint)renderTarget.ViewHeight)
                };
                GraphicsDevice.NativeDeviceApi.vkCmdBeginRenderPass(currentCommandList.NativeCommandBuffer, &renderPassBegin, VkSubpassContents.Inline);

                previousRenderPass = activeRenderPass = pipelineRenderPass;
            }
        }

        private unsafe void CleanupRenderPass()
        {
            if (activeRenderPass != VkRenderPass.Null)
            {
                GraphicsDevice.NativeDeviceApi.vkCmdEndRenderPass(currentCommandList.NativeCommandBuffer);
                activeRenderPass = VkRenderPass.Null;

                viewportDirty = true;
                scissorsDirty = true;
                pipelineDirty = true;
            }
        }

        private readonly struct FramebufferKey : IEquatable<FramebufferKey>
        {
            private readonly VkRenderPass renderPass;
            private readonly int attachmentCount;
            private readonly VkImageView attachment0;
            private readonly VkImageView attachment1;
            private readonly VkImageView attachment2;
            private readonly VkImageView attachment3;
            private readonly VkImageView attachment4;
            private readonly VkImageView attachment5;
            private readonly VkImageView attachment6;
            private readonly VkImageView attachment7;
            private readonly VkImageView attachment8;
            private readonly VkImageView attachment9;

            public unsafe FramebufferKey(VkRenderPass renderPass, int attachmentCount, VkImageView* attachments)
            {
                this.renderPass = renderPass;
                this.attachmentCount = attachmentCount;

                attachment0 = attachments[0];
                attachment1 = attachments[1];
                attachment2 = attachments[2];
                attachment3 = attachments[3];
                attachment4 = attachments[4];
                attachment5 = attachments[5];
                attachment6 = attachments[6];
                attachment7 = attachments[7];
                attachment8 = attachments[8];
                attachment9 = attachments[9];
            }

            public override unsafe int GetHashCode()
            {
                var hashcode = renderPass.GetHashCode();

                fixed (VkImageView* attachmentsPointer = &attachment0)
                {
                    for (int i = 0; i < attachmentCount; i++)
                    {
                        hashcode = attachmentsPointer[i].GetHashCode() ^ (hashcode * 397);
                    }
                }

                return hashcode;
            }

            public unsafe bool Equals(FramebufferKey other)
            {
                if (other.renderPass != renderPass || attachmentCount != other.attachmentCount)
                    return false;

                fixed (VkImageView* attachmentsPointer = &attachment0)
                {
                    var otherAttachmentsPointer = &other.attachment0;

                    for (int i = 0; i < attachmentCount; i++)
                    {
                        if (attachmentsPointer[i] != otherAttachmentsPointer[i])
                            return false;
                    }
                }

                return true;
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
