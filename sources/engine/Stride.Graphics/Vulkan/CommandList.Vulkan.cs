// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;
using Stride.Core;
using Stride.Core.Collections;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public partial class CommandList
    {
        internal CommandBufferPool CommandBufferPool;

        private VkRenderPass activeRenderPass;
        private VkRenderPass previousRenderPass;
        private PipelineState activePipeline;

        private readonly Dictionary<FramebufferKey, VkFramebuffer> framebuffers = new Dictionary<FramebufferKey, VkFramebuffer>();
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
            CommandBufferPool = new CommandBufferPool(GraphicsDevice);

            descriptorPool = GraphicsDevice.DescriptorPools.GetObject();
            allocatedTypeCounts = new uint[DescriptorSetLayout.DescriptorTypeCount];
            allocatedSetCount = 0;

            Reset();
        }

        public unsafe void Reset()
        {
            if (currentCommandList.Builder != null)
                return;

            CleanupRenderPass();
            boundDescriptorSets.Clear();

            framebuffers.Clear();
            framebufferDirty = true;

            currentCommandList.Builder = this;
            currentCommandList.NativeCommandBuffer = CommandBufferPool.GetObject();
            currentCommandList.DescriptorPools = GraphicsDevice.DescriptorPoolLists.Acquire();
            currentCommandList.StagingResources = GraphicsDevice.StagingResourceLists.Acquire();

            var beginInfo = new VkCommandBufferBeginInfo
            {
                sType = VkStructureType.CommandBufferBeginInfo,
                flags = VkCommandBufferUsageFlags.OneTimeSubmit,
            };
            vkBeginCommandBuffer(currentCommandList.NativeCommandBuffer, &beginInfo);

            activeStencilReference = null;
        }

        /// <summary>
        /// Closes the command list for recording and returns an executable token.
        /// </summary>
        /// <returns>The executable command list.</returns>
        public CompiledCommandList Close()
        {
            // End active render pass
            CleanupRenderPass();

            // Close
            vkEndCommandBuffer(currentCommandList.NativeCommandBuffer);

            // Staging resources not updated anymore
            foreach (var stagingResource in currentCommandList.StagingResources)
            {
                stagingResource.StagingBuilder = null;
            }

            activePipeline = null;

            var result = currentCommandList;
            currentCommandList = default(CompiledCommandList);
            return result;
        }

        /// <summary>
        /// Closes and executes the command list.
        /// </summary>
        public void Flush()
        {
            GraphicsDevice.ExecuteCommandList(Close());
        }

        private unsafe void FlushInternal(bool wait)
        {
            var fenceValue = GraphicsDevice.ExecuteCommandListInternal(Close());

            if (wait)
                GraphicsDevice.WaitForFenceInternal(fenceValue);

            Reset();

            // Restore states
            vkCmdSetStencilReference(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, activeStencilReference ?? 0);

            if (activePipeline != null)
            {
                vkCmdBindPipeline(currentCommandList.NativeCommandBuffer, VkPipelineBindPoint.Graphics, activePipeline.NativePipeline);
                var descriptorSetCopy = descriptorSet;
                vkCmdBindDescriptorSets(currentCommandList.NativeCommandBuffer, VkPipelineBindPoint.Graphics, activePipeline.NativeLayout, 0, 1, &descriptorSetCopy, 0, null);
            }
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
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, Texture[] renderTargets)
        {
            var oldFramebufferAttachmentCount = framebufferAttachmentCount;
            framebufferAttachmentCount = renderTargetCount;

            for (int i = 0; i < renderTargetCount; i++)
            {
                if (renderTargets[i].NativeColorAttachmentView != framebufferAttachments[i])
                    framebufferDirty = true;

                framebufferAttachments[i] = renderTargets[i].NativeColorAttachmentView;
            }

            if (depthStencilBuffer != null)
            {
                if (depthStencilBuffer.NativeDepthStencilView != framebufferAttachments[renderTargetCount])
                    framebufferDirty = true;

                framebufferAttachments[renderTargetCount] = depthStencilBuffer.NativeDepthStencilView;
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
                vkCmdSetViewport(currentCommandList.NativeCommandBuffer, 0, 1, (Vortice.Mathematics.Viewport*)&viewportCopy);
                viewportDirty = false;
            }

            if (activePipeline?.Description.RasterizerState.ScissorTestEnable ?? false)
            {
                if (scissorsDirty)
                {
                    // Use manual scissor
                    var scissor = scissors[0];
                    var nativeScissor = new Vortice.Mathematics.Rectangle(scissor.Left, scissor.Top, scissor.Width, scissor.Height);
                    vkCmdSetScissor(currentCommandList.NativeCommandBuffer, 0, 1, &nativeScissor);
                }
            }
            else
            {
                // Use viewport
                // Always update, because either scissor or viewport was dirty and we use viewport size
                var scissor = new Vortice.Mathematics.Rectangle((int)viewportCopy.X, (int)viewportCopy.Y, (int)viewportCopy.Width, (int)viewportCopy.Height);
                vkCmdSetScissor(currentCommandList.NativeCommandBuffer, 0, 1, &scissor);
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
        ///     Prepares a draw call. This method is called before each Draw() method to setup the correct Primitive, InputLayout and VertexBuffers.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">Cannot GraphicsDevice.Draw*() without an effect being previously applied with Effect.Apply() method</exception>
        private unsafe void PrepareDraw()
        {
            SetViewportImpl();

            if (!activeStencilReference.HasValue)
            {
                activeStencilReference = 0;
                vkCmdSetStencilReference(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, 0);
            }

            // Lazily set the render pass and frame buffer
            EnsureRenderPass();

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
                descriptorPool = GraphicsDevice.DescriptorPools.GetObject();

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
                pSetLayouts = &nativeDescriptorSetLayout,
            };

            VkDescriptorSet localDescriptorSet;
            vkAllocateDescriptorSets(GraphicsDevice.NativeDevice, &allocateInfo, &localDescriptorSet);
            this.descriptorSet = localDescriptorSet;

#if !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
            copies.Clear(true);

            foreach (var mapping in activePipeline.DescriptorBindingMapping)
            {
                copies.Add(new VkCopyDescriptorSet
                {
                    sType = VkStructureType.CopyDescriptorSet,
                    srcSet = boundDescriptorSets[mapping.SourceSet],
                    srcBinding = (uint)mapping.SourceBinding,
                    srcArrayElement = 0,
                    dstSet = localDescriptorSet,
                    dstBinding = (uint)mapping.DestinationBinding,
                    dstArrayElement = 0,
                    descriptorCount = 1
                });
            }

            GraphicsDevice.NativeDevice.UpdateDescriptorSets(0, null, (uint)copies.Count, copies.Count > 0 ? (CopyDescriptorSet*)Interop.Fixed(copies.Items) : null);
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
                    descriptorCount = 1,
                };

                switch (mapping.DescriptorType)
                {
                    case VkDescriptorType.SampledImage:
                        var texture = heapObject.Value as Texture;
                        descriptorData->ImageInfo = new VkDescriptorImageInfo { imageView = texture?.NativeImageView ?? GraphicsDevice.EmptyTexture.NativeImageView, imageLayout = VkImageLayout.ShaderReadOnlyOptimal };
                        write->pImageInfo = &descriptorData->ImageInfo;
                        break;

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

                    default:
                        throw new InvalidOperationException();
                }
            }

            vkUpdateDescriptorSets(GraphicsDevice.NativeDevice, (uint)bindingCount, writes, 0, null);
#endif
            vkCmdBindDescriptorSets(currentCommandList.NativeCommandBuffer, VkPipelineBindPoint.Graphics, activePipeline.NativeLayout, 0, 1, &localDescriptorSet, 0, null);
        }

        private readonly FastList<VkCopyDescriptorSet> copies = new FastList<VkCopyDescriptorSet>();

        public void SetStencilReference(int stencilReference)
        {
            if (activeStencilReference != stencilReference)
            {
                activeStencilReference = (uint)stencilReference;
                vkCmdSetStencilReference(currentCommandList.NativeCommandBuffer, VkStencilFaceFlags.FrontAndBack, activeStencilReference.Value);
            }
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (pipelineState == activePipeline)
                return;

            // If scissor state changed, force a refresh
            scissorsDirty |= (pipelineState?.Description.RasterizerState.ScissorTestEnable ?? false) != (activePipeline?.Description.RasterizerState.ScissorTestEnable ?? false);

            activePipeline = pipelineState;

            vkCmdBindPipeline(currentCommandList.NativeCommandBuffer, VkPipelineBindPoint.Graphics, pipelineState.NativePipeline);
        }

        public unsafe void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            // TODO VULKAN API: Stride is part of Pipeline 

            // TODO VULKAN: Handle multiple buffers. Collect and apply before draw?
            //if (index != 0)
            //    throw new NotImplementedException();

            var bufferCopy = buffer.NativeBuffer;
            var offsetCopy = (ulong)offset;

            vkCmdBindVertexBuffers(currentCommandList.NativeCommandBuffer, (uint)index, 1, &bufferCopy, &offsetCopy);
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            vkCmdBindIndexBuffer(currentCommandList.NativeCommandBuffer, buffer.NativeBuffer, (ulong)offset, is32bits ? VkIndexType.Uint32 : VkIndexType.Uint16);
        }

        public unsafe void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            var texture = resource as Texture;
            if (texture != null)
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
                        texture.NativePipelineStageMask = VkPipelineStageFlags.FragmentShader;
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
                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceStages, texture.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

#if !STRIDE_GRAPHICS_NO_DESCRIPTOR_COPIES
        private readonly FastList<VkDescriptorSet> boundDescriptorSets = new FastList<VkDescriptorSet>();
#else
        private readonly FastList<DescriptorSet> boundDescriptorSets = new FastList<DescriptorSet>();
#endif

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
            if (index != 0)
                throw new NotImplementedException();

            boundDescriptorSets.Clear(true);
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
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            PrepareDraw();

            vkCmdDraw(currentCommandList.NativeCommandBuffer, (uint)vertexCount, 1, (uint)startVertexLocation, 0);

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

            vkCmdDrawIndexed(currentCommandList.NativeCommandBuffer, (uint)indexCount, 1, (uint)startIndexLocation, baseVertexLocation, 0);

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

            vkCmdDrawIndexed(currentCommandList.NativeCommandBuffer, (uint)indexCountPerInstance, (uint)instanceCount, (uint)startIndexLocation, baseVertexLocation, (uint)startInstanceLocation);
            //NativeCommandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);

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

            throw new NotImplementedException();
            //NativeCommandBuffer.DrawIndirect(argumentsBuffer.NativeBuffer, (ulong)alignedByteOffsetForArgs, );
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

            vkCmdDraw(currentCommandList.NativeCommandBuffer, (uint)vertexCountPerInstance, (uint)instanceCount, (uint)startVertexLocation, (uint)startVertexLocation);
            //NativeCommandList.DrawInstanced(vertexCountPerInstance, instanceCount, startVertexLocation, startInstanceLocation);

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
                        pMarkerName = bytesPointer,
                    };
                    vkCmdDebugMarkerBeginEXT(currentCommandList.NativeCommandBuffer, &debugMarkerInfo);
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
                vkCmdDebugMarkerEndEXT(currentCommandList.NativeCommandBuffer);
            }
        }
        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="query">The timestamp query.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            vkCmdWriteTimestamp(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.AllCommands, queryPool.NativeQueryPool, (uint)index);
        }

        public void ResetQueryPool(QueryPool queryPool)
        {
            vkCmdResetQueryPool(currentCommandList.NativeCommandBuffer, queryPool.NativeQueryPool, 0, (uint)queryPool.QueryCount);
        }

        /// <summary>
        /// Clears the specified depth stencil buffer. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="options">The options.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="stencil">The stencil.</param>
        /// <exception cref="System.InvalidOperationException"></exception>
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
            vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, depthStencilBuffer.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

            var clearValue = new VkClearDepthStencilValue(depth, stencil);
            vkCmdClearDepthStencilImage(currentCommandList.NativeCommandBuffer, depthStencilBuffer.NativeImage, VkImageLayout.TransferDstOptimal, &clearValue, 1, &clearRange);

            memoryBarrier = new VkImageMemoryBarrier(depthStencilBuffer.NativeImage, barrierRange, VkAccessFlags.TransferWrite, depthStencilBuffer.NativeAccessMask, VkImageLayout.TransferDstOptimal, depthStencilBuffer.NativeLayout);
            vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, depthStencilBuffer.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

            depthStencilBuffer.IsInitialized = true;
        }

        /// <summary>
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="System.ArgumentNullException">renderTarget</exception>
        public unsafe void Clear(Texture renderTarget, Color4 color)
        {
            // TODO VULKAN: Detect if inside render pass. If so, NativeCommandBuffer.ClearAttachments()
            // Barriers need to be global to command buffer
            CleanupRenderPass();

            var clearRange = renderTarget.NativeResourceRange;

            var memoryBarrier = new VkImageMemoryBarrier(renderTarget.NativeImage, clearRange, renderTarget.NativeAccessMask, VkAccessFlags.TransferWrite, renderTarget.NativeLayout, VkImageLayout.TransferDstOptimal);
            vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, renderTarget.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

            vkCmdClearColorImage(currentCommandList.NativeCommandBuffer, renderTarget.NativeImage, VkImageLayout.TransferDstOptimal, (VkClearColorValue*)&color, 1, &clearRange);

            memoryBarrier = new VkImageMemoryBarrier(renderTarget.NativeImage, clearRange, VkAccessFlags.TransferWrite, renderTarget.NativeAccessMask, VkImageLayout.TransferDstOptimal, renderTarget.NativeLayout);
            vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, renderTarget.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

            renderTarget.IsInitialized = true;
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public void ClearReadWrite(Buffer buffer, Vector4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public void ClearReadWrite(Buffer buffer, Int4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Buffer. This buffer must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="buffer">The buffer.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">buffer</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;buffer</exception>
        public void ClearReadWrite(Buffer buffer, UInt4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">texture</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;texture</exception>
        public void ClearReadWrite(Texture texture, Vector4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">texture</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;texture</exception>
        public void ClearReadWrite(Texture texture, Int4 value)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Clears a read-write Texture. This texture must have been created with read-write/unordered access.
        /// </summary>
        /// <param name="texture">The texture.</param>
        /// <param name="value">The value.</param>
        /// <exception cref="System.ArgumentNullException">texture</exception>
        /// <exception cref="System.ArgumentException">Expecting buffer supporting UAV;texture</exception>
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
                    sourceTexture.MipLevels != destinationTexture.MipLevels)
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
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(sourceParent.NativeImage, new VkImageSubresourceRange(sourceParent.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue), sourceTexture.NativeAccessMask, VkAccessFlags.TransferRead, sourceTexture.NativeLayout, VkImageLayout.TransferSrcOptimal);
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new VkBufferMemoryBarrier(destinationParent.NativeBuffer, destinationTexture.NativeAccessMask, VkAccessFlags.TransferWrite);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(destinationParent.NativeImage, new VkImageSubresourceRange(destinationParent.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue), destinationTexture.NativeAccessMask, VkAccessFlags.TransferWrite, destinationTexture.NativeLayout, VkImageLayout.TransferDstOptimal);
                }

                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);

                for (var subresource = 0; subresource < sourceTexture.MipLevels * sourceTexture.ArraySize; ++subresource)
                {
                    var arraySlice = subresource / sourceTexture.MipLevels;
                    var mipLevel = subresource % sourceTexture.MipLevels;

                    var sourceOffset = sourceTexture.ComputeBufferOffset(subresource, 0);
                    var destinationOffset = destinationTexture.ComputeBufferOffset(subresource, 0);
                    var size = sourceTexture.ComputeSubresourceSize(subresource);

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
                                srcOffset = (ulong)sourceOffset,
                                dstOffset = (ulong)destinationOffset,
                                size = (ulong)size,
                            };
                            vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeBuffer, 1, &copy);
                        }
                        else
                        {
                            var copy = new VkBufferImageCopy
                            {
                                imageSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)mipLevel, (uint)arraySlice, 1),
                                imageExtent = new Vortice.Mathematics.Size3(width, height, depth),
                                bufferOffset = (ulong)destinationOffset,
                            };
                            vkCmdCopyImageToBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeBuffer, 1, &copy);
                        }

                        // VkFence for host access
                        destinationParent.StagingFenceValue = null;
                        destinationParent.StagingBuilder = this;
                        currentCommandList.StagingResources.Add(destinationParent);
                    }
                    else
                    {
                        var destinationSubresource = new VkImageSubresourceLayers(destinationParent.NativeImageAspect, (uint)mipLevel, (uint)arraySlice, (uint)destinationTexture.ArraySize);

                        if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                        {
                            var copy = new VkBufferImageCopy
                            {
                                imageSubresource = destinationSubresource,
                                imageExtent = new Vortice.Mathematics.Size3(width, height, depth),
                                bufferOffset = (ulong)sourceOffset,
                            };
                            vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, 1, &copy);
                        }
                        else
                        {
                            var copy = new VkImageCopy
                            {
                                srcSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)mipLevel, (uint)arraySlice, (uint)sourceTexture.ArraySize),
                                dstSubresource = destinationSubresource,
                                extent = new Vortice.Mathematics.Size3(width, height, depth),
                            };
                            vkCmdCopyImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, 1, &copy);
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

                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkDependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);
            }
            else if (source is Buffer sourceBuffer && destination is Buffer destinationBuffer)
            {
                var bufferBarriers = stackalloc VkBufferMemoryBarrier[2];
                bufferBarriers[0] = new VkBufferMemoryBarrier(sourceBuffer.NativeBuffer, sourceBuffer.NativeAccessMask, VkAccessFlags.TransferRead);
                bufferBarriers[1] = new VkBufferMemoryBarrier(destinationBuffer.NativeBuffer, destinationBuffer.NativeAccessMask, VkAccessFlags.TransferWrite);
                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceBuffer.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 2, bufferBarriers, 0, null);

                var copy = new VkBufferCopy
                {
                    srcOffset = 0,
                    dstOffset = 0,
                    size = (uint)sourceBuffer.SizeInBytes
                };
                vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceBuffer.NativeBuffer, destinationBuffer.NativeBuffer, 1, &copy);

                bufferBarriers[0] = new VkBufferMemoryBarrier(sourceBuffer.NativeBuffer, VkAccessFlags.TransferRead, sourceBuffer.NativeAccessMask);
                bufferBarriers[1] = new VkBufferMemoryBarrier(destinationBuffer.NativeBuffer, VkAccessFlags.TransferWrite, destinationBuffer.NativeAccessMask);
                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, sourceBuffer.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 2, bufferBarriers, 0, null);
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

            var sourceTexture = source as Texture;
            var destinationTexture = destination as Texture;

            if (sourceTexture != null && destinationTexture != null)
            {
                CleanupRenderPass();

                var mipmapDescription = sourceTexture.GetMipMapDescription(sourceSubresource % sourceTexture.MipLevels);

                var region = sourceRegion ?? new ResourceRegion(0, 0, 0, mipmapDescription.Width, mipmapDescription.Height, mipmapDescription.Depth);

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
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(sourceParent.NativeImage, new VkImageSubresourceRange(sourceParent.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue), sourceParent.NativeAccessMask, VkAccessFlags.TransferRead, sourceParent.NativeLayout, VkImageLayout.TransferSrcOptimal);
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new VkBufferMemoryBarrier(destinationParent.NativeBuffer, destinationParent.NativeAccessMask, VkAccessFlags.TransferWrite);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new VkImageMemoryBarrier(destinationParent.NativeImage, new VkImageSubresourceRange(destinationParent.NativeImageAspect, 0, uint.MaxValue, 0, uint.MaxValue), destinationParent.NativeAccessMask, VkAccessFlags.TransferWrite, destinationParent.NativeLayout, VkImageLayout.TransferDstOptimal);
                }

                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);

                // Copy
                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    throw new NotImplementedException();
                    //if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    //{
                    //    var copy = new VkBufferCopy
                    //    {
                    //        SourceOffset = 0,
                    //        DestinationOffset = 0,
                    //        Size = (uint)(sourceParent.ViewWidth * sourceParent.ViewHeight * sourceParent.ViewDepth * sourceParent.ViewFormat.SizeInBytes())
                    //    };
                    //    vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeBuffer, 1, &copy);
                    //}
                    //else
                    //{
                    //    var copy = new VkBufferImageCopy
                    //    {
                    //        ImageSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize, (uint)sourceTexture.MipLevel),
                    //        ImageExtent = new Vortice.Mathematics.Size3((uint)destinationTexture.Width, (uint)destinationTexture.Height, (uint)destinationTexture.Depth)
                    //    };
                    //    vkCmdCopyImageToBuffer(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeBuffer, 1, &copy);
                    //}

                    //// VkFence for host access
                    //destinationParent.StagingFenceValue = null;
                    //destinationParent.StagingBuilder = this;
                    //currentCommandList.StagingResources.Add(destinationParent);
                }
                else
                {
                    var destinationSubresource = new VkImageSubresourceLayers(destinationParent.NativeImageAspect, (uint)destinationTexture.MipLevel, (uint)destinationTexture.ArraySlice, (uint)destinationTexture.ArraySize);

                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        if (region.Left != 0 || region.Top != 0 || region.Front != 0
                            && region.Right != mipmapDescription.Width || region.Bottom != mipmapDescription.Height || region.Back != mipmapDescription.Depth)
                            throw new NotImplementedException("Copy from Staging doesn't support source region other than full texture");

                        var copy = new VkBufferImageCopy
                        {
                            imageSubresource = destinationSubresource,
                            bufferOffset = (ulong)sourceTexture.ComputeBufferOffset(sourceSubresource, 0),
                            bufferImageHeight = (uint)sourceTexture.Height,
                            bufferRowLength = (uint)sourceTexture.Width,
                            imageOffset = new Vortice.Mathematics.Point3(dstX, dstY, dstZ),
                            imageExtent = new Vortice.Mathematics.Size3(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front),
                        };
                        vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeBuffer, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, 1, &copy);
                    }
                    else
                    {
                        var copy = new VkImageCopy
                        {
                            srcSubresource = new VkImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.MipLevel, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize),
                            srcOffset = new Vortice.Mathematics.Point3(region.Left, region.Top, region.Front),
                            dstSubresource = destinationSubresource,
                            dstOffset = new Vortice.Mathematics.Point3(dstX, dstY, dstZ),
                            extent = new Vortice.Mathematics.Size3(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front),
                        };
                        vkCmdCopyImage(currentCommandList.NativeCommandBuffer, sourceParent.NativeImage, VkImageLayout.TransferSrcOptimal, destinationParent.NativeImage, VkImageLayout.TransferDstOptimal, 1, &copy);
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

                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, VkDependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);
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

        internal void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox)
        {
            var texture = resource as Texture;
            if (texture != null)
            {
                var mipLevel = subResourceIndex % texture.MipLevels;

                var width = Texture.CalculateMipSize(texture.Width, mipLevel);
                var height = Texture.CalculateMipSize(texture.Height, mipLevel);
                var depth = Texture.CalculateMipSize(texture.Depth, mipLevel);

                UpdateSubresource(resource, subResourceIndex, databox, new ResourceRegion(0, 0, 0, width, height, depth));
            }
            else
            {
                var buffer = resource as Buffer;
                if (buffer != null)
                {
                    UpdateSubresource(resource, subResourceIndex, databox, new ResourceRegion(0, 0, 0, buffer.SizeInBytes, 1, 1));
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        internal unsafe void UpdateSubresource(GraphicsResource resource, int subResourceIndex, DataBox databox, ResourceRegion region)
        {
            // Barriers need to be global to command buffer
            CleanupRenderPass();

            int lengthInBytes = 0;

            var texture = resource as Texture;
            int blockSize;
            if (texture != null)
            {
                lengthInBytes = databox.SlicePitch * (region.Back - region.Front);
                blockSize = texture.Format.BlockSize();
            }
            else
            {
                lengthInBytes = region.Right - region.Left;
                blockSize = 4;
            }

            // Buffer-to-image copies need to be aligned to the pixel size and 4 (always a power of 2)
            var alignmentMask = (blockSize < 4 ? 4 : blockSize) - 1;

            VkBuffer uploadResource;
            int uploadOffset;
            var uploadMemory = GraphicsDevice.AllocateUploadBuffer(lengthInBytes + alignmentMask, out uploadResource, out uploadOffset);
            var alignment = ((uploadOffset + alignmentMask) & ~alignmentMask) - uploadOffset;

            Utilities.CopyMemory(uploadMemory + alignment, databox.DataPointer, lengthInBytes);

            var uploadBufferMemoryBarrier = new VkBufferMemoryBarrier(uploadResource, VkAccessFlags.HostWrite, VkAccessFlags.TransferRead, (ulong)(uploadOffset + alignment), (ulong)lengthInBytes);

            if (texture != null)
            {
                var mipSlice = subResourceIndex % texture.MipLevels;
                var arraySlice = subResourceIndex / texture.MipLevels;
                var subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, (uint)mipSlice, 1, (uint)arraySlice, 1);

                var memoryBarrier = new VkImageMemoryBarrier(texture.NativeImage, subresourceRange, texture.NativeAccessMask, VkAccessFlags.TransferWrite, texture.NativeLayout, VkImageLayout.TransferDstOptimal);
                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, texture.NativePipelineStageMask | VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 1, &uploadBufferMemoryBarrier, 1, &memoryBarrier);

                // TODO VULKAN: Handle depth-stencil (NOTE: only supported on graphics queue)
                // TODO VULKAN: Handle non-packed pitches
                var bufferCopy = new VkBufferImageCopy
                {
                    bufferOffset = (ulong)(uploadOffset + alignment),
                    imageSubresource = new VkImageSubresourceLayers { aspectMask = VkImageAspectFlags.Color, baseArrayLayer = (uint)arraySlice, layerCount = 1, mipLevel = (uint)mipSlice },
                    bufferRowLength = (uint)(databox.RowPitch * texture.Format.BlockWidth() / texture.Format.BlockSize()),
                    bufferImageHeight = (uint)(databox.SlicePitch * texture.Format.BlockHeight() / databox.RowPitch),
                    imageOffset = new Vortice.Mathematics.Point3(region.Left, region.Top, region.Front),
                    imageExtent = new Vortice.Mathematics.Size3(region.Right - region.Left, region.Bottom - region.Top, region.Back - region.Front),
                };
                vkCmdCopyBufferToImage(currentCommandList.NativeCommandBuffer, uploadResource, texture.NativeImage, VkImageLayout.TransferDstOptimal, 1, &bufferCopy);

                memoryBarrier = new VkImageMemoryBarrier(texture.NativeImage, subresourceRange, VkAccessFlags.TransferWrite, texture.NativeAccessMask, VkImageLayout.TransferDstOptimal, texture.NativeLayout);
                vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, texture.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);
            }
            else
            {
                var buffer = resource as Buffer;
                if (buffer != null)
                {
                    var memoryBarriers = stackalloc VkBufferMemoryBarrier[2];

                    var bufferCopy = new VkBufferCopy
                    {
                        srcOffset = (ulong)(uploadOffset + alignment),
                        dstOffset = (ulong)region.Left,
                        size = (ulong)lengthInBytes,
                    };

                    memoryBarriers[0] = uploadBufferMemoryBarrier;
                    memoryBarriers[1] = new VkBufferMemoryBarrier(buffer.NativeBuffer, buffer.NativeAccessMask, VkAccessFlags.TransferWrite, bufferCopy.dstOffset, bufferCopy.size);
                    vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, buffer.NativePipelineStageMask | VkPipelineStageFlags.Host, VkPipelineStageFlags.Transfer, VkDependencyFlags.None, 0, null, 2, memoryBarriers, 0, null);

                    vkCmdCopyBuffer(currentCommandList.NativeCommandBuffer, uploadResource, buffer.NativeBuffer, 1, &bufferCopy);

                    var memoryBarrier = new VkBufferMemoryBarrier(buffer.NativeBuffer, VkAccessFlags.TransferWrite, buffer.NativeAccessMask, bufferCopy.dstOffset, bufferCopy.size);
                    vkCmdPipelineBarrier(currentCommandList.NativeCommandBuffer, VkPipelineStageFlags.Transfer, buffer.NativePipelineStageMask, VkDependencyFlags.None, 0, null, 1, &memoryBarrier, 0, null);
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
        public unsafe MappedResource MapSubresource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            if (resource == null) throw new ArgumentNullException("resource");

            var rowPitch = 0;
            var texture = resource as Texture;
            var buffer = resource as Buffer;
            var usage = GraphicsResourceUsage.Default;

            if (texture != null)
            {
                usage = texture.Usage;
                if (lengthInBytes == 0)
                    lengthInBytes = texture.ComputeSubresourceSize(subResourceIndex);
                rowPitch = texture.ComputeRowPitch(subResourceIndex % texture.MipLevels);

                offsetInBytes += texture.ComputeBufferOffset(subResourceIndex, 0);
            }
            else
            {
                if (buffer != null)
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

            if (mapMode == MapMode.WriteDiscard)
            {
                throw new InvalidOperationException("Can't use WriteDiscard on Graphics API that doesn't support renaming");
            }

            if (mapMode != MapMode.WriteNoOverwrite && mapMode != MapMode.Write)
            {
                // Need to wait?
                if (!resource.StagingFenceValue.HasValue || !GraphicsDevice.IsFenceCompleteInternal(resource.StagingFenceValue.Value))
                {
                    if (doNotWait)
                    {
                        return new MappedResource(resource, subResourceIndex, new DataBox(IntPtr.Zero, 0, 0));
                    }

                    // This will be set only if need to flush (due to a previous Copy)
                    if (resource.StagingBuilder != null)
                    {
                        // Need to flush; check if part of current command list
                        if (resource.StagingBuilder == this)
                            FlushInternal(false);

                        if (!resource.StagingFenceValue.HasValue)
                            throw new InvalidOperationException("CommandList updating the staging resource has not been submitted");

                        GraphicsDevice.WaitForFenceInternal(resource.StagingFenceValue.Value);
                    }
                }
            }

            void* mappedMemory;
            vkMapMemory(GraphicsDevice.NativeDevice, resource.NativeMemory, (ulong)offsetInBytes, (ulong)lengthInBytes, VkMemoryMapFlags.None, &mappedMemory);
            return new MappedResource(resource, subResourceIndex, new DataBox((IntPtr)mappedMemory, rowPitch, 0), offsetInBytes, lengthInBytes);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public void UnmapSubresource(MappedResource unmapped)
        {
            vkUnmapMemory(GraphicsDevice.NativeDevice, unmapped.Resource.NativeMemory);
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            Recreate();
            return true;
        }

        /// <inheritdoc/>
        protected internal override void OnDestroyed()
        {
            vkDeviceWaitIdle(GraphicsDevice.NativeDevice);

            if (descriptorPool != VkDescriptorPool.Null)
            {
                GraphicsDevice.DescriptorPools.RecycleObject(GraphicsDevice.NextFenceValue - 1, descriptorPool);
                descriptorPool = VkDescriptorPool.Null;
            }

            CommandBufferPool.Dispose();

            base.OnDestroyed();
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
                                attachmentCount = (uint)framebufferAttachmentCount,
                                pAttachments = attachmentsPointer,
                                width = (uint)renderTarget.ViewWidth,
                                height = (uint)renderTarget.ViewHeight,
                                layers = 1, // TODO VULKAN: Use correct view depth/array size
                            };
                            vkCreateFramebuffer(GraphicsDevice.NativeDevice, &framebufferCreateInfo, null, out activeFramebuffer);
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
                    renderArea = new Vortice.Mathematics.Rectangle(0, 0, renderTarget.ViewWidth, renderTarget.ViewHeight),
                };
                vkCmdBeginRenderPass(currentCommandList.NativeCommandBuffer, &renderPassBegin, VkSubpassContents.Inline);

                previousRenderPass = activeRenderPass = pipelineRenderPass;
            }
        }

        private unsafe void CleanupRenderPass()
        {
            if (activeRenderPass != VkRenderPass.Null)
            {
                vkCmdEndRenderPass(currentCommandList.NativeCommandBuffer);
                activeRenderPass = VkRenderPass.Null;
            }
        }

        private struct FramebufferKey : IEquatable<FramebufferKey>
        {
            private VkRenderPass renderPass;
            private int attachmentCount;
            private VkImageView attachment0;
            private VkImageView attachment1;
            private VkImageView attachment2;
            private VkImageView attachment3;
            private VkImageView attachment4;
            private VkImageView attachment5;
            private VkImageView attachment6;
            private VkImageView attachment7;
            private VkImageView attachment8;
            private VkImageView attachment9;

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
                if (other.renderPass != this.renderPass || attachmentCount != other.attachmentCount)
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
