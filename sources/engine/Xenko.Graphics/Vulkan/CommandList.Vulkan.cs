// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_GRAPHICS_API_VULKAN
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpVulkan;
using Xenko.Core;
using Xenko.Core.Collections;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    public partial class CommandList
    {
        internal CommandBufferPool CommandBufferPool;

        private RenderPass activeRenderPass;
        private RenderPass previousRenderPass;
        private PipelineState activePipeline;

        private readonly Dictionary<FramebufferKey, Framebuffer> framebuffers = new Dictionary<FramebufferKey, Framebuffer>();
        private readonly ImageView[] framebufferAttachments = new ImageView[9];
        private int framebufferAttachmentCount;
        private bool framebufferDirty = true;
        private Framebuffer activeFramebuffer;

        private SharpVulkan.DescriptorPool descriptorPool;
        private SharpVulkan.DescriptorSet descriptorSet;
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

        public void Reset()
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

            var beginInfo = new CommandBufferBeginInfo
            {
                StructureType = StructureType.CommandBufferBeginInfo,
                Flags = CommandBufferUsageFlags.OneTimeSubmit,
            };
            currentCommandList.NativeCommandBuffer.Begin(ref beginInfo);

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
            currentCommandList.NativeCommandBuffer.End();

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
            currentCommandList.NativeCommandBuffer.SetStencilReference(StencilFaceFlags.FrontAndBack, activeStencilReference ?? 0);

            if (activePipeline != null)
            {
                currentCommandList.NativeCommandBuffer.BindPipeline(PipelineBindPoint.Graphics, activePipeline.NativePipeline);
                var descriptorSetCopy = descriptorSet;
                currentCommandList.NativeCommandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, activePipeline.NativeLayout, 0, 1, &descriptorSetCopy, 0, null);
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
                currentCommandList.NativeCommandBuffer.SetViewport(0, 1, (SharpVulkan.Viewport*)&viewportCopy);
                viewportDirty = false;
            }

            if (activePipeline?.Description.RasterizerState.ScissorTestEnable ?? false)
            {
                if (scissorsDirty)
                {
                    // Use manual scissor
                    var scissor = scissors[0];
                    var nativeScissor = new Rect2D(scissor.Left, scissor.Top, (uint)scissor.Width, (uint)scissor.Height);
                    currentCommandList.NativeCommandBuffer.SetScissor(0, 1, &nativeScissor);
                }
            }
            else
            {
                // Use viewport
                // Always update, because either scissor or viewport was dirty and we use viewport size
                var scissor = new Rect2D((int)viewportCopy.X, (int)viewportCopy.Y, (uint)viewportCopy.Width, (uint)viewportCopy.Height);
                currentCommandList.NativeCommandBuffer.SetScissor(0, 1, &scissor);
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
                currentCommandList.NativeCommandBuffer.SetStencilReference(StencilFaceFlags.FrontAndBack, 0);
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
            var allocateInfo = new DescriptorSetAllocateInfo
            {
                StructureType = StructureType.DescriptorSetAllocateInfo,
                DescriptorPool = descriptorPool,
                DescriptorSetCount = 1,
                SetLayouts = new IntPtr(&nativeDescriptorSetLayout)
            };

            SharpVulkan.DescriptorSet localDescriptorSet;
            GraphicsDevice.NativeDevice.AllocateDescriptorSets(ref allocateInfo, &localDescriptorSet);
            this.descriptorSet = localDescriptorSet;

#if !XENKO_GRAPHICS_NO_DESCRIPTOR_COPIES
            copies.Clear(true);

            foreach (var mapping in activePipeline.DescriptorBindingMapping)
            {
                copies.Add(new CopyDescriptorSet
                {
                    StructureType = StructureType.CopyDescriptorSet,
                    SourceSet = boundDescriptorSets[mapping.SourceSet],
                    SourceBinding = (uint)mapping.SourceBinding,
                    SourceArrayElement = 0,
                    DestinationSet = localDescriptorSet,
                    DestinationBinding = (uint)mapping.DestinationBinding,
                    DestinationArrayElement = 0,
                    DescriptorCount = 1
                });
            }

            GraphicsDevice.NativeDevice.UpdateDescriptorSets(0, null, (uint)copies.Count, copies.Count > 0 ? (CopyDescriptorSet*)Interop.Fixed(copies.Items) : null);
#else
            var bindingCount = activePipeline.DescriptorBindingMapping.Count;
            var writes = stackalloc WriteDescriptorSet[bindingCount];
            var descriptorDatas = stackalloc DescriptorData[bindingCount];

            for (int index = 0; index < bindingCount; index++)
            {
                var mapping = activePipeline.DescriptorBindingMapping[index];
                var sourceSet = boundDescriptorSets[mapping.SourceSet];
                var heapObject = sourceSet.HeapObjects[sourceSet.DescriptorStartOffset + mapping.SourceBinding];

                var write = writes + index;
                var descriptorData = descriptorDatas + index;

                *write = new WriteDescriptorSet
                {
                    StructureType = StructureType.WriteDescriptorSet,
                    DescriptorType = mapping.DescriptorType,
                    DestinationSet = localDescriptorSet,
                    DestinationBinding = (uint)mapping.DestinationBinding,
                    DestinationArrayElement = 0,
                    DescriptorCount = 1,
                };

                switch (mapping.DescriptorType)
                {
                    case DescriptorType.SampledImage:
                        var texture = heapObject.Value as Texture;
                        descriptorData->ImageInfo = new DescriptorImageInfo { ImageView = texture?.NativeImageView ?? GraphicsDevice.EmptyTexture.NativeImageView, ImageLayout = ImageLayout.ShaderReadOnlyOptimal };
                        write->ImageInfo = new IntPtr(descriptorData);
                        break;

                    case DescriptorType.Sampler:
                        var samplerState = heapObject.Value as SamplerState;
                        descriptorData->ImageInfo = new DescriptorImageInfo { Sampler = samplerState?.NativeSampler ?? GraphicsDevice.SamplerStates.LinearWrap.NativeSampler };
                        write->ImageInfo = new IntPtr(descriptorData);
                        break;

                    case DescriptorType.UniformBuffer:
                        var buffer = heapObject.Value as Buffer;
                        descriptorData->BufferInfo = new DescriptorBufferInfo { Buffer = buffer?.NativeBuffer ?? SharpVulkan.Buffer.Null, Offset = (ulong)heapObject.Offset, Range = (ulong)heapObject.Size };
                        write->BufferInfo = new IntPtr(descriptorData);
                        break;

                    case DescriptorType.UniformTexelBuffer:
                        buffer = heapObject.Value as Buffer;
                        descriptorData->BufferView = buffer?.NativeBufferView ?? (mapping.ResourceElementIsInteger ? GraphicsDevice.EmptyTexelBufferInt.NativeBufferView : GraphicsDevice.EmptyTexelBufferFloat.NativeBufferView);
                        write->TexelBufferView = new IntPtr(descriptorData);
                        break;

                    default:
                        throw new InvalidOperationException();
                }
            }

            GraphicsDevice.NativeDevice.UpdateDescriptorSets((uint)bindingCount, writes, 0, null);
#endif
            currentCommandList.NativeCommandBuffer.BindDescriptorSets(PipelineBindPoint.Graphics, activePipeline.NativeLayout, 0, 1, &localDescriptorSet, 0, null);
        }

        private readonly FastList<CopyDescriptorSet> copies = new FastList<CopyDescriptorSet>();

        public void SetStencilReference(int stencilReference)
        {
            if (activeStencilReference != stencilReference)
            {
                activeStencilReference = (uint)stencilReference;
                currentCommandList.NativeCommandBuffer.SetStencilReference(StencilFaceFlags.FrontAndBack, activeStencilReference.Value);
            }
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            if (pipelineState == activePipeline)
                return;

            // If scissor state changed, force a refresh
            scissorsDirty |= (pipelineState?.Description.RasterizerState.ScissorTestEnable ?? false) != (activePipeline?.Description.RasterizerState.ScissorTestEnable ?? false);

            activePipeline = pipelineState;

            currentCommandList.NativeCommandBuffer.BindPipeline(PipelineBindPoint.Graphics, pipelineState.NativePipeline);
        }

        public unsafe void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            // TODO VULKAN API: Stride is part of Pipeline 

            // TODO VULKAN: Handle multiple buffers. Collect and apply before draw?
            //if (index != 0)
            //    throw new NotImplementedException();

            var bufferCopy = buffer.NativeBuffer;
            var offsetCopy = (ulong)offset;

            currentCommandList.NativeCommandBuffer.BindVertexBuffers((uint)index, 1, &bufferCopy, &offsetCopy);
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            currentCommandList.NativeCommandBuffer.BindIndexBuffer(buffer.NativeBuffer, (ulong)offset, is32bits ? IndexType.UInt32 : IndexType.UInt16);
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
                        texture.NativeLayout = ImageLayout.ColorAttachmentOptimal;
                        texture.NativeAccessMask = AccessFlags.ColorAttachmentWrite;
                        texture.NativePipelineStageMask = PipelineStageFlags.ColorAttachmentOutput;
                        break;
                    case GraphicsResourceState.Present:
                        texture.NativeLayout = ImageLayout.PresentSource;
                        texture.NativeAccessMask = AccessFlags.MemoryRead;
                        texture.NativePipelineStageMask = PipelineStageFlags.BottomOfPipe;
                        break;
                    case GraphicsResourceState.DepthWrite:
                        texture.NativeLayout = ImageLayout.DepthStencilAttachmentOptimal;
                        texture.NativeAccessMask = AccessFlags.DepthStencilAttachmentWrite;
                        texture.NativePipelineStageMask = PipelineStageFlags.ColorAttachmentOutput | PipelineStageFlags.EarlyFragmentTests | PipelineStageFlags.LateFragmentTests;
                        break;
                    case GraphicsResourceState.PixelShaderResource:
                        texture.NativeLayout = ImageLayout.ShaderReadOnlyOptimal;
                        texture.NativeAccessMask = AccessFlags.ShaderRead;
                        texture.NativePipelineStageMask = PipelineStageFlags.FragmentShader;
                        break;
                    case GraphicsResourceState.GenericRead:
                        texture.NativeLayout = ImageLayout.General;
                        texture.NativeAccessMask = AccessFlags.ShaderRead | AccessFlags.TransferRead | AccessFlags.IndirectCommandRead | AccessFlags.ColorAttachmentRead | AccessFlags.DepthStencilAttachmentRead | AccessFlags.InputAttachmentRead | AccessFlags.VertexAttributeRead | AccessFlags.IndexRead | AccessFlags.UniformRead;
                        texture.NativePipelineStageMask = PipelineStageFlags.AllCommands;
                        break;
                    default:
                        texture.NativeLayout = ImageLayout.General;
                        texture.NativeAccessMask = (AccessFlags)0x1FFFF; // TODO VULKAN: Don't hard-code this
                        texture.NativePipelineStageMask = PipelineStageFlags.AllCommands;
                        break;
                }

                if (oldLayout == texture.NativeLayout && oldAccessMask == texture.NativeAccessMask)
                    return;

                if (oldLayout == ImageLayout.Undefined || oldLayout == ImageLayout.PresentSource)
                    sourceStages = PipelineStageFlags.TopOfPipe;

                // End render pass, so barrier effects all commands in the buffer
                CleanupRenderPass();

                var memoryBarrier = new ImageMemoryBarrier(texture.NativeImage, oldLayout, texture.NativeLayout, oldAccessMask, texture.NativeAccessMask, new ImageSubresourceRange(texture.NativeImageAspect));
                currentCommandList.NativeCommandBuffer.PipelineBarrier(sourceStages, texture.NativePipelineStageMask, DependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);
            }
            else
            {
                throw new NotImplementedException();
            }
        }

#if !XENKO_GRAPHICS_NO_DESCRIPTOR_COPIES
        private readonly FastList<SharpVulkan.DescriptorSet> boundDescriptorSets = new FastList<SharpVulkan.DescriptorSet>();
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
#if !XENKO_GRAPHICS_NO_DESCRIPTOR_COPIES
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

            currentCommandList.NativeCommandBuffer.Draw((uint)vertexCount, 1, (uint)startVertexLocation, 0);

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

            currentCommandList.NativeCommandBuffer.DrawIndexed((uint)indexCount, 1, (uint)startIndexLocation, baseVertexLocation, 0);

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

            currentCommandList.NativeCommandBuffer.DrawIndexed((uint)indexCountPerInstance, (uint)instanceCount, (uint)startIndexLocation, baseVertexLocation, (uint)startInstanceLocation);
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

            currentCommandList.NativeCommandBuffer.Draw((uint)vertexCountPerInstance, (uint)instanceCount, (uint)startVertexLocation, (uint)startVertexLocation);
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
                    var debugMarkerInfo = new DebugMarkerMarkerInfo
                    {
                        StructureType = StructureType.DebugMarkerMarkerInfo,
                        MarkerName = new IntPtr(bytesPointer),
                        Color = *(DebugMarkerMarkerInfo.ColorArray*)&profileColor
                    };
                    GraphicsAdapterFactory.GetInstance(GraphicsDevice.IsDebugMode).BeginDebugMarker(currentCommandList.NativeCommandBuffer, &debugMarkerInfo);
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
                GraphicsAdapterFactory.GetInstance(GraphicsDevice.IsDebugMode).EndDebugMarker(currentCommandList.NativeCommandBuffer);
            }
        }
        /// <summary>
        /// Submit a timestamp query.
        /// </summary>
        /// <param name="queryPool">The QueryPool owning the query.</param>
        /// <param name="query">The timestamp query.</param>
        public void WriteTimestamp(QueryPool queryPool, int index)
        {
            currentCommandList.NativeCommandBuffer.WriteTimestamp(PipelineStageFlags.AllCommands, queryPool.NativeQueryPool, (uint)index);
        }

        public void ResetQueryPool(QueryPool queryPool)
        {
            currentCommandList.NativeCommandBuffer.ResetQueryPool(queryPool.NativeQueryPool, 0, (uint)queryPool.QueryCount);
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

            var clearRange = new ImageSubresourceRange(ImageAspectFlags.None, (uint)depthStencilBuffer.ArraySlice, (uint)depthStencilBuffer.ArraySize, (uint)depthStencilBuffer.MipLevel, (uint)depthStencilBuffer.MipLevels);
            var barrierRange = clearRange;
            barrierRange.AspectMask = depthStencilBuffer.NativeImageAspect;

            if ((options & DepthStencilClearOptions.DepthBuffer) != 0)
                clearRange.AspectMask |= ImageAspectFlags.Depth & depthStencilBuffer.NativeImageAspect;

            if ((options & DepthStencilClearOptions.Stencil) != 0)
                clearRange.AspectMask |= ImageAspectFlags.Stencil & depthStencilBuffer.NativeImageAspect;

            var memoryBarrier = new ImageMemoryBarrier(depthStencilBuffer.NativeImage, depthStencilBuffer.NativeLayout, ImageLayout.TransferDestinationOptimal, depthStencilBuffer.NativeAccessMask, AccessFlags.TransferWrite, barrierRange);
            currentCommandList.NativeCommandBuffer.PipelineBarrier(depthStencilBuffer.NativePipelineStageMask, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

            currentCommandList.NativeCommandBuffer.ClearDepthStencilImage(depthStencilBuffer.NativeImage, ImageLayout.TransferDestinationOptimal, new ClearDepthStencilValue(depth, stencil), 1, &clearRange);

            memoryBarrier = new ImageMemoryBarrier(depthStencilBuffer.NativeImage, ImageLayout.TransferDestinationOptimal, depthStencilBuffer.NativeLayout, AccessFlags.TransferWrite, depthStencilBuffer.NativeAccessMask, barrierRange);
            currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, depthStencilBuffer.NativePipelineStageMask, DependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

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

            var clearRange = new ImageSubresourceRange(ImageAspectFlags.Color, (uint)renderTarget.ArraySlice, (uint)renderTarget.ArraySize, (uint)renderTarget.MipLevel, (uint)renderTarget.MipLevels);

            var memoryBarrier = new ImageMemoryBarrier(renderTarget.NativeImage, renderTarget.NativeLayout, ImageLayout.TransferDestinationOptimal, renderTarget.NativeAccessMask, AccessFlags.TransferWrite, clearRange);
            currentCommandList.NativeCommandBuffer.PipelineBarrier(renderTarget.NativePipelineStageMask, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

            currentCommandList.NativeCommandBuffer.ClearColorImage(renderTarget.NativeImage, ImageLayout.TransferDestinationOptimal, ColorHelper.Convert(color), 1, &clearRange);

            memoryBarrier = new ImageMemoryBarrier(renderTarget.NativeImage, ImageLayout.TransferDestinationOptimal, renderTarget.NativeLayout, AccessFlags.TransferWrite, renderTarget.NativeAccessMask, clearRange);
            currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, renderTarget.NativePipelineStageMask, DependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);

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

            var sourceTexture = source as Texture;
            var destinationTexture = destination as Texture;

            if (sourceTexture != null && destinationTexture != null)
            {
                CleanupRenderPass();

                var imageBarriers = stackalloc ImageMemoryBarrier[2];
                var bufferBarriers = stackalloc BufferMemoryBarrier[2];

                var sourceParent = sourceTexture.ParentTexture ?? sourceTexture;
                var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                uint bufferBarrierCount = 0;
                uint imageBarrierCount = 0;

                // Initial barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new BufferMemoryBarrier(sourceParent.NativeBuffer, sourceTexture.NativeAccessMask, AccessFlags.TransferRead);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new ImageMemoryBarrier(sourceParent.NativeImage, sourceTexture.NativeLayout, ImageLayout.TransferSourceOptimal, sourceTexture.NativeAccessMask, AccessFlags.TransferRead, new ImageSubresourceRange(sourceParent.NativeImageAspect));
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new BufferMemoryBarrier(destinationParent.NativeBuffer, destinationTexture.NativeAccessMask, AccessFlags.TransferWrite);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new ImageMemoryBarrier(destinationParent.NativeImage, destinationTexture.NativeLayout, ImageLayout.TransferDestinationOptimal, destinationTexture.NativeAccessMask, AccessFlags.TransferWrite, new ImageSubresourceRange(destinationParent.NativeImageAspect));
                }

                currentCommandList.NativeCommandBuffer.PipelineBarrier(sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);

                // Copy
                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        var copy = new BufferCopy
                        {
                            SourceOffset = 0,
                            DestinationOffset = 0,
                            Size = (uint)(sourceParent.ViewWidth * sourceParent.ViewHeight * sourceParent.ViewDepth * sourceParent.ViewFormat.SizeInBytes())
                        };
                        currentCommandList.NativeCommandBuffer.CopyBuffer(sourceParent.NativeBuffer, destinationParent.NativeBuffer, 1, &copy);
                    }
                    else
                    {
                        var copy = new BufferImageCopy
                        {
                            ImageSubresource = new ImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize, (uint)sourceTexture.MipLevel),
                            ImageExtent = new Extent3D((uint)destinationTexture.Width, (uint)destinationTexture.Height, (uint)destinationTexture.Depth)
                        };
                        currentCommandList.NativeCommandBuffer.CopyImageToBuffer(sourceParent.NativeImage, ImageLayout.TransferSourceOptimal, destinationParent.NativeBuffer, 1, &copy);
                    }

                    // Fence for host access
                    destinationParent.StagingFenceValue = null;
                    destinationParent.StagingBuilder = this;
                    currentCommandList.StagingResources.Add(destinationParent);
                }
                else
                {
                    var destinationSubresource = new ImageSubresourceLayers(destinationParent.NativeImageAspect, (uint)destinationTexture.ArraySlice, (uint)destinationTexture.ArraySize, (uint)destinationTexture.MipLevel);

                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        var copy = new BufferImageCopy
                        {
                            ImageSubresource = destinationSubresource,
                            ImageExtent = new Extent3D((uint)destinationTexture.Width, (uint)destinationTexture.Height, (uint)destinationTexture.Depth)
                        };
                        currentCommandList.NativeCommandBuffer.CopyBufferToImage(sourceParent.NativeBuffer, destinationParent.NativeImage, ImageLayout.TransferDestinationOptimal, 1, &copy);
                    }
                    else
                    {
                        var copy = new ImageCopy
                        {
                            SourceSubresource = new ImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize, (uint)sourceTexture.MipLevel),
                            DestinationSubresource = destinationSubresource,
                            Extent = new Extent3D((uint)sourceTexture.ViewWidth, (uint)sourceTexture.ViewHeight, (uint)sourceTexture.ViewDepth),
                        };
                        currentCommandList.NativeCommandBuffer.CopyImage(sourceParent.NativeImage, ImageLayout.TransferSourceOptimal, destinationParent.NativeImage, ImageLayout.TransferDestinationOptimal, 1, &copy);
                    }
                }

                imageBarrierCount = 0;
                bufferBarrierCount = 0;

                // Final barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].SourceAccessMask = AccessFlags.TransferRead;
                    bufferBarriers[bufferBarrierCount].DestinationAccessMask = sourceParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].OldLayout = ImageLayout.TransferSourceOptimal;
                    imageBarriers[imageBarrierCount].NewLayout = sourceParent.NativeLayout;
                    imageBarriers[imageBarrierCount].SourceAccessMask = AccessFlags.TransferRead;
                    imageBarriers[imageBarrierCount].DestinationAccessMask = sourceParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].SourceAccessMask = AccessFlags.TransferWrite;
                    bufferBarriers[bufferBarrierCount].DestinationAccessMask = destinationParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].OldLayout = ImageLayout.TransferDestinationOptimal;
                    imageBarriers[imageBarrierCount].NewLayout = destinationParent.NativeLayout;
                    imageBarriers[imageBarrierCount].SourceAccessMask = AccessFlags.TransferWrite;
                    imageBarriers[imageBarrierCount].DestinationAccessMask = destinationParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, DependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);
            }
            else
            {
                var sourceBuffer = source as Buffer;
                var destinationBuffer = destination as Buffer;

                if (sourceBuffer != null && destinationBuffer != null)
                {
                    var bufferBarriers = stackalloc BufferMemoryBarrier[2];
                    bufferBarriers[0] = new BufferMemoryBarrier(sourceBuffer.NativeBuffer, sourceBuffer.NativeAccessMask, AccessFlags.TransferRead);
                    bufferBarriers[1] = new BufferMemoryBarrier(destinationBuffer.NativeBuffer, destinationBuffer.NativeAccessMask, AccessFlags.TransferWrite);
                    currentCommandList.NativeCommandBuffer.PipelineBarrier(sourceBuffer.NativePipelineStageMask, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, 2, bufferBarriers, 0, null);

                    var copy = new BufferCopy
                    {
                        SourceOffset = 0,
                        DestinationOffset = 0,
                        Size = (uint)sourceBuffer.SizeInBytes
                    };
                    currentCommandList.NativeCommandBuffer.CopyBuffer(sourceBuffer.NativeBuffer, destinationBuffer.NativeBuffer, 1, &copy);

                    bufferBarriers[0] = new BufferMemoryBarrier(sourceBuffer.NativeBuffer, AccessFlags.TransferRead, sourceBuffer.NativeAccessMask);
                    bufferBarriers[1] = new BufferMemoryBarrier(destinationBuffer.NativeBuffer, AccessFlags.TransferWrite, destinationBuffer.NativeAccessMask);
                    currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, sourceBuffer.NativePipelineStageMask, DependencyFlags.None, 0, null, 2, bufferBarriers, 0, null);
                }
                else
                {
                    throw new InvalidOperationException();
                }
            }
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            throw new NotImplementedException();
        }

        public unsafe void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourecRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            // TODO VULKAN: One copy per mip level

            var sourceTexture = source as Texture;
            var destinationTexture = destination as Texture;

            if (sourceTexture != null && destinationTexture != null)
            {
                CleanupRenderPass();

                var region = sourecRegion ?? new ResourceRegion(0, 0, 0, sourceTexture.Width, sourceTexture.Height, sourceTexture.Depth);

                var imageBarriers = stackalloc ImageMemoryBarrier[2];
                var bufferBarriers = stackalloc BufferMemoryBarrier[2];

                var sourceParent = sourceTexture.ParentTexture ?? sourceTexture;
                var destinationParent = destinationTexture.ParentTexture ?? destinationTexture;

                uint bufferBarrierCount = 0;
                uint imageBarrierCount = 0;

                // Initial barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new BufferMemoryBarrier(sourceParent.NativeBuffer, sourceParent.NativeAccessMask, AccessFlags.TransferRead);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new ImageMemoryBarrier(sourceParent.NativeImage, sourceParent.NativeLayout, ImageLayout.TransferSourceOptimal, sourceParent.NativeAccessMask, AccessFlags.TransferRead, new ImageSubresourceRange(sourceParent.NativeImageAspect));
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount++] = new BufferMemoryBarrier(destinationParent.NativeBuffer, destinationParent.NativeAccessMask, AccessFlags.TransferWrite);
                }
                else
                {
                    imageBarriers[imageBarrierCount++] = new ImageMemoryBarrier(destinationParent.NativeImage, destinationParent.NativeLayout, ImageLayout.TransferDestinationOptimal, destinationParent.NativeAccessMask, AccessFlags.TransferWrite, new ImageSubresourceRange(destinationParent.NativeImageAspect));
                }

                currentCommandList.NativeCommandBuffer.PipelineBarrier(sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);

                // Copy
                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    throw new NotImplementedException();
                    //if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    //{
                    //    var copy = new BufferCopy
                    //    {
                    //        SourceOffset = 0,
                    //        DestinationOffset = 0,
                    //        Size = (uint)(sourceParent.ViewWidth * sourceParent.ViewHeight * sourceParent.ViewDepth * sourceParent.ViewFormat.SizeInBytes())
                    //    };
                    //    currentCommandList.NativeCommandBuffer.CopyBuffer(sourceParent.NativeBuffer, destinationParent.NativeBuffer, 1, &copy);
                    //}
                    //else
                    //{
                    //    var copy = new BufferImageCopy
                    //    {
                    //        ImageSubresource = new ImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize, (uint)sourceTexture.MipLevel),
                    //        ImageExtent = new Extent3D((uint)destinationTexture.Width, (uint)destinationTexture.Height, (uint)destinationTexture.Depth)
                    //    };
                    //    currentCommandList.NativeCommandBuffer.CopyImageToBuffer(sourceParent.NativeImage, ImageLayout.TransferSourceOptimal, destinationParent.NativeBuffer, 1, &copy);
                    //}

                    //// Fence for host access
                    //destinationParent.StagingFenceValue = null;
                    //destinationParent.StagingBuilder = this;
                    //currentCommandList.StagingResources.Add(destinationParent);
                }
                else
                {
                    var destinationSubresource = new ImageSubresourceLayers(destinationParent.NativeImageAspect, (uint)destinationTexture.ArraySlice, (uint)destinationTexture.ArraySize, (uint)destinationTexture.MipLevel);

                    if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                    {
                        var copy = new BufferImageCopy
                        {
                            ImageSubresource = destinationSubresource,
                            BufferOffset = (ulong)sourceTexture.ComputeBufferOffset(sourceSubresource, 0),
                            BufferImageHeight = (uint)sourceTexture.Height,
                            BufferRowLength = (uint)sourceTexture.Width,
                            ImageOffset = new Offset3D(dstX, dstY, dstZ),
                            ImageExtent = new Extent3D((uint)(region.Right - region.Left), (uint)(region.Bottom - region.Top), (uint)(region.Back - region.Front))
                        };
                        currentCommandList.NativeCommandBuffer.CopyBufferToImage(sourceParent.NativeBuffer, destinationParent.NativeImage, ImageLayout.TransferDestinationOptimal, 1, &copy);
                    }
                    else
                    {
                        throw new NotImplementedException();
                        //var copy = new ImageCopy
                        //{
                        //    SourceSubresource = new ImageSubresourceLayers(sourceParent.NativeImageAspect, (uint)sourceTexture.ArraySlice, (uint)sourceTexture.ArraySize, (uint)sourceTexture.MipLevel),
                        //    DestinationSubresource = destinationSubresource,
                        //    Extent = new Extent3D((uint)sourceTexture.ViewWidth, (uint)sourceTexture.ViewHeight, (uint)sourceTexture.ViewDepth),
                        //};
                        //currentCommandList.NativeCommandBuffer.CopyImage(sourceParent.NativeImage, ImageLayout.TransferSourceOptimal, destinationParent.NativeImage, ImageLayout.TransferDestinationOptimal, 1, &copy);
                    }
                }

                imageBarrierCount = 0;
                bufferBarrierCount = 0;

                // Final barriers
                if (sourceTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].SourceAccessMask = AccessFlags.TransferRead;
                    bufferBarriers[bufferBarrierCount].DestinationAccessMask = sourceParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].OldLayout = ImageLayout.TransferSourceOptimal;
                    imageBarriers[imageBarrierCount].NewLayout = sourceParent.NativeLayout;
                    imageBarriers[imageBarrierCount].SourceAccessMask = AccessFlags.TransferRead;
                    imageBarriers[imageBarrierCount].DestinationAccessMask = sourceParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                if (destinationTexture.Usage == GraphicsResourceUsage.Staging)
                {
                    bufferBarriers[bufferBarrierCount].SourceAccessMask = AccessFlags.TransferWrite;
                    bufferBarriers[bufferBarrierCount].DestinationAccessMask = destinationParent.NativeAccessMask;
                    bufferBarrierCount++;
                }
                else
                {
                    imageBarriers[imageBarrierCount].OldLayout = ImageLayout.TransferDestinationOptimal;
                    imageBarriers[imageBarrierCount].NewLayout = destinationParent.NativeLayout;
                    imageBarriers[imageBarrierCount].SourceAccessMask = AccessFlags.TransferWrite;
                    imageBarriers[imageBarrierCount].DestinationAccessMask = destinationParent.NativeAccessMask;
                    imageBarrierCount++;
                }

                currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, sourceTexture.NativePipelineStageMask | destinationParent.NativePipelineStageMask, DependencyFlags.None, 0, null, bufferBarrierCount, bufferBarriers, imageBarrierCount, imageBarriers);
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
                UpdateSubresource(resource, subResourceIndex, databox, new ResourceRegion(0, 0, 0, texture.Width, texture.Height, texture.Depth));
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
                blockSize = texture.Format.IsCompressed() ? texture.NativeFormat.BlockSizeInBytes() : texture.TexturePixelSize;
            }
            else
            {
                lengthInBytes = region.Right - region.Left;
                blockSize = 4;
            }

            // Buffer-to-image copies need to be aligned to the pixel size and 4 (always a power of 2)
            var alignmentMask = (blockSize < 4 ? 4 : blockSize) - 1;

            SharpVulkan.Buffer uploadResource;
            int uploadOffset;
            var uploadMemory = GraphicsDevice.AllocateUploadBuffer(lengthInBytes + alignmentMask, out uploadResource, out uploadOffset);
            var alignment = ((uploadOffset + alignmentMask) & ~alignmentMask) - uploadOffset;

            Utilities.CopyMemory(uploadMemory + alignment, databox.DataPointer, lengthInBytes);

            var uploadBufferMemoryBarrier = new BufferMemoryBarrier(uploadResource, AccessFlags.HostWrite, AccessFlags.TransferRead, (ulong)(uploadOffset + alignment), (ulong)lengthInBytes);

            if (texture != null)
            {
                var mipSlice = subResourceIndex % texture.MipLevels;
                var arraySlice = subResourceIndex / texture.MipLevels;
                var subresourceRange = new ImageSubresourceRange(ImageAspectFlags.Color, (uint)arraySlice, 1, (uint)mipSlice, 1);

                var memoryBarrier = new ImageMemoryBarrier(texture.NativeImage, texture.NativeLayout, ImageLayout.TransferDestinationOptimal, texture.NativeAccessMask, AccessFlags.TransferWrite, subresourceRange);
                currentCommandList.NativeCommandBuffer.PipelineBarrier(texture.NativePipelineStageMask | PipelineStageFlags.Host, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, 1, &uploadBufferMemoryBarrier, 1, &memoryBarrier);

                // TODO VULKAN: Handle depth-stencil (NOTE: only supported on graphics queue)
                // TODO VULKAN: Handle non-packed pitches
                var bufferCopy = new BufferImageCopy
                {
                    BufferOffset = (ulong)(uploadOffset + alignment),
                    ImageSubresource = new ImageSubresourceLayers { AspectMask = ImageAspectFlags.Color, BaseArrayLayer = (uint)arraySlice, LayerCount = 1, MipLevel = (uint)mipSlice },
                    BufferRowLength = 0, //(uint)databox.RowPitch / ...,
                    BufferImageHeight = 0, //(uint)databox.SlicePitch / ...,
                    ImageOffset = new Offset3D(region.Left, region.Top, region.Front),
                    ImageExtent = new Extent3D((uint)(region.Right - region.Left), (uint)(region.Bottom - region.Top), (uint)(region.Back - region.Front))
                };
                currentCommandList.NativeCommandBuffer.CopyBufferToImage(uploadResource, texture.NativeImage, ImageLayout.TransferDestinationOptimal, 1, &bufferCopy);

                memoryBarrier = new ImageMemoryBarrier(texture.NativeImage, ImageLayout.TransferDestinationOptimal, texture.NativeLayout, AccessFlags.TransferWrite, texture.NativeAccessMask, subresourceRange);
                currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, texture.NativePipelineStageMask, DependencyFlags.None, 0, null, 0, null, 1, &memoryBarrier);
            }
            else
            {
                var buffer = resource as Buffer;
                if (buffer != null)
                {
                    var memoryBarriers = stackalloc BufferMemoryBarrier[2];

                    var bufferCopy = new BufferCopy
                    {
                        SourceOffset = (ulong)(uploadOffset + alignment),
                        DestinationOffset = (ulong)region.Left,
                        Size = (ulong)lengthInBytes,
                    };

                    memoryBarriers[0] = uploadBufferMemoryBarrier;
                    memoryBarriers[1] = new BufferMemoryBarrier(buffer.NativeBuffer, buffer.NativeAccessMask, AccessFlags.TransferWrite, bufferCopy.DestinationOffset, bufferCopy.Size);
                    currentCommandList.NativeCommandBuffer.PipelineBarrier(buffer.NativePipelineStageMask | PipelineStageFlags.Host, PipelineStageFlags.Transfer, DependencyFlags.None, 0, null, 2, memoryBarriers, 0, null);

                    currentCommandList.NativeCommandBuffer.CopyBuffer(uploadResource, buffer.NativeBuffer, 1, &bufferCopy);

                    var memoryBarrier = new BufferMemoryBarrier(buffer.NativeBuffer, AccessFlags.TransferWrite, buffer.NativeAccessMask, bufferCopy.DestinationOffset, bufferCopy.Size);
                    currentCommandList.NativeCommandBuffer.PipelineBarrier(PipelineStageFlags.Transfer, buffer.NativePipelineStageMask, DependencyFlags.None, 0, null, 1, &memoryBarrier, 0, null);
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
        public MappedResource MapSubresource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
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
                    lengthInBytes = texture.ViewWidth * texture.ViewHeight * texture.ViewDepth * texture.ViewFormat.SizeInBytes();
                rowPitch = texture.RowStride;
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

                    // Need to flush (part of current command list)
                    if (resource.StagingBuilder == this)
                        FlushInternal(false);

                    if (!resource.StagingFenceValue.HasValue)
                        throw new InvalidOperationException("CommandList updating the staging resource has not been submitted");

                    GraphicsDevice.WaitForFenceInternal(resource.StagingFenceValue.Value);
                }
            }

            if (texture != null)
            {
                var mipLevel = subResourceIndex % texture.MipLevels;
                var arraySlice = subResourceIndex / texture.MipLevels;

                for (int i = 0; i < texture.MipLevels; i++)
                {
                    var slices = i < mipLevel ? arraySlice + 1 : arraySlice;
                    var mipmap = texture.GetMipMapDescription(i);
                    offsetInBytes += mipmap.DepthStride * mipmap.Depth * arraySlice;
                }
            }

            var mappedMemory = GraphicsDevice.NativeDevice.MapMemory(resource.NativeMemory, (ulong)offsetInBytes, (ulong)lengthInBytes, MemoryMapFlags.None);
            return new MappedResource(resource, subResourceIndex, new DataBox(mappedMemory, rowPitch, 0), offsetInBytes, lengthInBytes);
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
        public void UnmapSubresource(MappedResource unmapped)
        {
            GraphicsDevice.NativeDevice.UnmapMemory(unmapped.Resource.NativeMemory);
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
            GraphicsDevice.NativeDevice.WaitIdle();

            if (descriptorPool != SharpVulkan.DescriptorPool.Null)
            {
                GraphicsDevice.DescriptorPools.RecycleObject(GraphicsDevice.NextFenceValue - 1, descriptorPool);
                descriptorPool = SharpVulkan.DescriptorPool.Null;
            }

            CommandBufferPool.Dispose();

            base.OnDestroyed();
        }

        private unsafe void EnsureRenderPass()
        {
            if (activePipeline == null)
                return;

            var pipelineRenderPass = activePipeline.NativeRenderPass;

            // Reuse the Framebuffer if the RenderPass didn't change
            if (previousRenderPass != pipelineRenderPass)
                framebufferDirty = true;

            // Nothing to do. RenderPass and Framebuffer are still valid
            if (!framebufferDirty && activeRenderPass == pipelineRenderPass)
                return;

            // End old render pass
            CleanupRenderPass();

            if (pipelineRenderPass != RenderPass.Null)
            {
                var renderTarget = RenderTargetCount > 0 ? renderTargets[0] : depthStencilBuffer;

                if (framebufferDirty)
                {
                    // Create new frame buffer
                    fixed (ImageView* attachmentsPointer = &framebufferAttachments[0])
                    {
                        var framebufferKey = new FramebufferKey(pipelineRenderPass, framebufferAttachmentCount, attachmentsPointer);

                        if (!framebuffers.TryGetValue(framebufferKey, out activeFramebuffer))
                        {
                            var framebufferCreateInfo = new FramebufferCreateInfo
                            {
                                StructureType = StructureType.FramebufferCreateInfo,
                                RenderPass = pipelineRenderPass,
                                AttachmentCount = (uint)framebufferAttachmentCount,
                                Attachments = new IntPtr(attachmentsPointer),
                                Width = (uint)renderTarget.ViewWidth,
                                Height = (uint)renderTarget.ViewHeight,
                                Layers = 1, // TODO VULKAN: Use correct view depth/array size
                            };
                            activeFramebuffer = GraphicsDevice.NativeDevice.CreateFramebuffer(ref framebufferCreateInfo);
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
                var renderPassBegin = new RenderPassBeginInfo
                {
                    StructureType = StructureType.RenderPassBeginInfo,
                    RenderPass = pipelineRenderPass,
                    Framebuffer = activeFramebuffer,
                    RenderArea = new Rect2D(0, 0, (uint)renderTarget.ViewWidth, (uint)renderTarget.ViewHeight)
                };
                currentCommandList.NativeCommandBuffer.BeginRenderPass(ref renderPassBegin, SubpassContents.Inline);

                previousRenderPass = activeRenderPass = pipelineRenderPass;
            }
        }

        private unsafe void CleanupRenderPass()
        {
            if (activeRenderPass != RenderPass.Null)
            {
                currentCommandList.NativeCommandBuffer.EndRenderPass();
                activeRenderPass = RenderPass.Null;
            }
        }

        private struct FramebufferKey : IEquatable<FramebufferKey>
        {
            private RenderPass renderPass;
            private int attachmentCount;
            private ImageView attachment0;
            private ImageView attachment1;
            private ImageView attachment2;
            private ImageView attachment3;
            private ImageView attachment4;
            private ImageView attachment5;
            private ImageView attachment6;
            private ImageView attachment7;
            private ImageView attachment8;
            private ImageView attachment9;

            public unsafe FramebufferKey(RenderPass renderPass, int attachmentCount, ImageView* attachments)
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

                fixed (ImageView* attachmentsPointer = &attachment0)
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

                fixed (ImageView* attachmentsPointer = &attachment0)
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
            public DescriptorBufferInfo BufferInfo;

            [FieldOffset(0)]
            public DescriptorImageInfo ImageInfo;

            [FieldOffset(0)]
            public BufferView BufferView;
        }
    }
}

#endif
