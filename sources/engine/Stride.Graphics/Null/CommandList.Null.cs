// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_GRAPHICS_API_NULL

using System;
using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    public partial class CommandList
    {
        /// <summary>
        /// New command list for <param name="device"/>.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        /// <returns>New CommandList instance.</returns>
        public static CommandList New(GraphicsDevice device)
        {
            NullHelper.ToImplement();
            return new CommandList(device);
        }

        /// <summary>
        /// Initializes a new instance of CommandList for <param name="device"/>.
        /// </summary>
        /// <param name="device">The graphics device.</param>
        private CommandList(GraphicsDevice device) : base(device)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Resets a Command List back to its initial state as if a new Command List was just created.
        /// </summary>
        public unsafe partial void Reset()
        {
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
            return default;
        }

        /// <summary>
        ///   Closes and executes the Command List.
        /// </summary>
        public partial void Flush()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Platform-specific implementation that clears and restores the state of the Graphics Device.
        /// </summary>
        private partial void ClearStateImpl()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        private void ResetTargetsImpl()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilBuffer">The depth stencil buffer.</param>
        /// <param name="renderTargets">The render targets.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
        private void SetRenderTargetsImpl(Texture depthStencilBuffer, int renderTargetCount, Texture[] renderTargets)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Platform-specific implementation that sets a scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangle">The scissor rectangle to set.</param>
        private unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Platform-specific implementation that sets one or more scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorCount">The number of scissor rectangles to bind.</param>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        private unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Sets the stream targets.
        /// </summary>
        /// <param name="buffers">The buffers.</param>
        public void SetStreamTargets(params Buffer[] buffers)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Unset the read/write buffers.
        /// </summary>
        public void UnsetReadWriteBuffers()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Unset the render targets.
        /// </summary>
        public void UnsetRenderTargets()
        {
            NullHelper.ToImplement();
        }

        public void SetStencilReference(int stencilReference)
        {
            NullHelper.ToImplement();
        }

        public void SetPipelineState(PipelineState pipelineState)
        {
            NullHelper.ToImplement();
        }

        public void SetVertexBuffer(int index, Buffer buffer, int offset, int stride)
        {
            NullHelper.ToImplement();
        }

        public void SetIndexBuffer(Buffer buffer, int offset, bool is32bits)
        {
            NullHelper.ToImplement();
        }

        public void ResourceBarrierTransition(GraphicsResource resource, GraphicsResourceState newState)
        {
            NullHelper.ToImplement();
        }

        public void SetDescriptorSets(int index, DescriptorSet[] descriptorSets)
        {
            NullHelper.ToImplement();
        }

        /// <inheritdoc />
        public void Dispatch(int threadCountX, int threadCountY, int threadCountZ)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Dispatches the specified indirect buffer.
        /// </summary>
        /// <param name="indirectBuffer">The indirect buffer.</param>
        /// <param name="offsetInBytes">The offset information bytes.</param>
        public void Dispatch(Buffer indirectBuffer, int offsetInBytes)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Draw non-indexed, non-instanced primitives.
        /// </summary>
        /// <param name="vertexCount">Number of vertices to draw.</param>
        /// <param name="startVertexLocation">Index of the first vertex, which is usually an offset in a vertex buffer; it could also be used as the first vertex id generated for a shader parameter marked with the <strong>SV_TargetId</strong> system-value semantic.</param>
        public void Draw(int vertexCount, int startVertexLocation = 0)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Draw geometry of an unknown size.
        /// </summary>
        public void DrawAuto()
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Draw indexed, non-instanced primitives.
        /// </summary>
        /// <param name="indexCount">Number of indices to draw.</param>
        /// <param name="startIndexLocation">The location of the first index read by the GPU from the index buffer.</param>
        /// <param name="baseVertexLocation">A value added to each index before reading a vertex from the vertex buffer.</param>
        public void DrawIndexed(int indexCount, int startIndexLocation = 0, int baseVertexLocation = 0)
        {
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Draw indexed, instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">A buffer containing the GPU generated primitives.</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawIndexedInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Draw instanced, GPU-generated primitives.
        /// </summary>
        /// <param name="argumentsBuffer">An arguments buffer</param>
        /// <param name="alignedByteOffsetForArgs">Offset in <em>pBufferForArgs</em> to the start of the GPU generated primitives.</param>
        public void DrawInstanced(Buffer argumentsBuffer, int alignedByteOffsetForArgs = 0)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Begins profiling.
        /// </summary>
        /// <param name="profileColor">Color of the profile.</param>
        /// <param name="name">The name.</param>
        public void BeginProfile(Color4 profileColor, string name)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Ends profiling.
        /// </summary>
        public void EndProfile()
        {
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
        }

        /// <summary>
        /// Clears the specified render target. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <param name="color">The color.</param>
        /// <exception cref="System.ArgumentNullException">renderTarget</exception>
        public void Clear(Texture renderTarget, Color4 color)
        {
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
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
            NullHelper.ToImplement();
        }

        public void Copy(GraphicsResource source, GraphicsResource destination)
        {
        }

        public void CopyMultisample(Texture sourceMultisampleTexture, int sourceSubResource, Texture destTexture, int destSubResource, PixelFormat format = PixelFormat.None)
        {
            NullHelper.ToImplement();
        }

        public void CopyRegion(GraphicsResource source, int sourceSubresource, ResourceRegion? sourecRegion, GraphicsResource destination, int destinationSubResource, int dstX = 0, int dstY = 0, int dstZ = 0)
        {
            NullHelper.ToImplement();
        }

        public void CopyCount(Buffer sourceBuffer, Buffer destBuffer, int offsetInBytes)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, ReadOnlySpan<byte> sourceData)
        {
            NullHelper.ToImplement();
        }

        /// <summary>
        ///   Copies data from memory to a sub-resource created in non-mappable memory.
        /// </summary>
        /// <param name="resource">The destination Graphics Resource to copy data to.</param>
        /// <param name="subResourceIndex">The sub-resource index of <paramref name="resource"/> to copy data to.</param>
        /// <param name="sourceData">The source data in CPU memory to copy.</param>
        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData)
        {
            NullHelper.ToImplement();
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
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
        internal void UpdateSubResource(GraphicsResource resource, int subResourceIndex, ReadOnlySpan<byte> sourceData, ResourceRegion region)
        {
            NullHelper.ToImplement();
        }

        // TODO GRAPHICS REFACTOR what should we do with this?
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
        internal unsafe partial void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData, ResourceRegion region)
        {
            NullHelper.ToImplement();
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
        public unsafe partial MappedResource MapSubResource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0)
        {
            NullHelper.ToImplement();
            return default;
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
            NullHelper.ToImplement();
        }
    }
}

#endif
