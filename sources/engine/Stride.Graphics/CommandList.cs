// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    ///   Represents a list of graphics commands for playback, which can include resource binding, primitive-based rendering, etc.
    /// </summary>
    public partial class CommandList : GraphicsResourceBase
    {
        /// <summary>
        ///   The maximum number of Render Targets that can be bound simultaneously by Stride.
        /// </summary>
        public const int MaxRenderTargetCount = 8;
        /// <summary>
        ///   The maximum number of Viewports or Scissor Rectangles that can be set simultaneously by Stride.
        /// </summary>
        public const int MaxViewportAndScissorRectangleCount = 16;

        #region Viewports

        private bool viewportDirty = false;

        private int boundViewportCount;
        private readonly Viewport[] viewports = new Viewport[MaxViewportAndScissorRectangleCount];


        /// <summary>
        ///   Gets the first viewport bound to the rasterizer stage of the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set the viewport, use one of the <see cref="SetViewport"/> or <see cref="SetViewports"/> methods,
        ///   or the <see cref="SetRenderTargetAndViewport"/>, or <see cref="SetRenderTargetsAndViewport"/> methods.
        /// </remarks>
        public Viewport Viewport => viewports[0];

        /// <summary>
        ///   Gets the viewports currently bound to the rasterizer stage of the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set the viewport, use one of the <see cref="SetViewport"/> or <see cref="SetViewports"/> methods,
        ///   or the <see cref="SetRenderTargetAndViewport"/>, or <see cref="SetRenderTargetsAndViewport"/> methods.
        /// </remarks>
        public ReadOnlySpan<Viewport> Viewports => viewports.AsSpan(0, boundViewportCount);

        /// <summary>
        ///   Gets the number of viewports currently bound to the rasterizer stage of the pipeline.
        /// </summary>
        public int ViewportCount => boundViewportCount;


        /// <summary>
        ///   Binds a single viewport to the rasterizer stage of the pipeline.
        /// </summary>
        /// <value>The viewport to set.</value>
        public void SetViewport(in Viewport viewport)
        {
            viewportDirty |= boundViewportCount != 1;
            boundViewportCount = 1;

            if (viewports[0] != viewport)
            {
                viewportDirty = true;
                viewports[0] = viewport;
            }
        }

        /// <summary>
        ///   Binds an array of viewports to the rasterizer stage of the pipeline.
        /// </summary>
        /// <param name="viewports">The viewports to set.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="viewports"/> contains too many viewports to set.
        /// </exception>
        public void SetViewports(params ReadOnlySpan<Viewport> viewports)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(viewports.Length, MaxViewportAndScissorRectangleCount);

            viewportDirty |= boundViewportCount != viewports.Length;
            boundViewportCount = viewports.Length;

            for (int i = 0; i < viewports.Length; i++)
            {
                if (this.viewports[i] != viewports[i])
                {
                    viewportDirty = true;
                    this.viewports[i] = viewports[i];
                }
            }
        }

        #endregion

        #region Scissor rectangles

        private int boundScissorCount;
        private readonly Rectangle[] scissors = new Rectangle[MaxViewportAndScissorRectangleCount];

#pragma warning disable 414 // The field 'CommandList.scissorsDirty' is assigned but its value is never used
        // This field is used in CommandList.Direct3D12.cs and CommandList.Vulkan.cs
        private bool scissorsDirty = false;
#pragma warning restore 414


        /// <summary>
        ///   Gets the first scissor rectangle bound to the rasterizer stage of the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set the scissor rectangles, use one of the <see cref="SetScissorRectangle"/> or
        ///   <see cref="SetScissorRectangles"/> methods.
        /// </remarks>
        public Rectangle Scissor => scissors[0];

        /// <summary>
        ///   Gets scissor rectangles currently bound to the rasterizer stage of the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set the scissor rectangles, use one of the <see cref="SetScissorRectangle"/> or
        ///   <see cref="SetScissorRectangles"/> methods.
        /// </remarks>
        public ReadOnlySpan<Rectangle> Scissors => scissors.AsSpan(0, boundScissorCount);

        /// <summary>
        ///   Gets the number of scissor rectangles currently bound to the rasterizer stage of the pipeline.
        /// </summary>
        public int ScissorCount => boundScissorCount;


        /// <summary>
        ///   Binds a single scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="rectangle">The scissor rectangle to set.</param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetScissorRectangle(in Rectangle rectangle)
        {
            scissorsDirty = true;
            boundScissorCount = 1;
            scissors[0] = rectangle;
            SetScissorRectangleImpl(in rectangle);
        }

        /// <summary>
        ///   Binds a set of scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="scissorRectangles"/> contains too many scissor rectangles to set.
        /// </exception>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetScissorRectangles(params ReadOnlySpan<Rectangle> scissorRectangles)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(scissorRectangles.Length, MaxViewportAndScissorRectangleCount);

            boundScissorCount = scissorRectangles.Length;
            scissorRectangles.CopyTo(scissors);

            if (scissorRectangles.Length == 1)
            {
                SetScissorRectangleImpl(in scissors[0]);
            }
            else if (scissorRectangles.Length > 1)
            {
                SetScissorRectanglesImpl(scissorRectangles);
            }
        }


        /// <summary>
        ///   Platform-specific implementation that sets a scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangle">The scissor rectangle to set.</param>
        private unsafe partial void SetScissorRectangleImpl(ref readonly Rectangle scissorRectangle);

        /// <summary>
        ///   Platform-specific implementation that sets one or more scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        private unsafe partial void SetScissorRectanglesImpl(ReadOnlySpan<Rectangle> scissorRectangles);

        #endregion

        #region Render Targets / Depth-Stencil Buffer

        private Texture depthStencilBuffer;

        private readonly Texture[] renderTargets = new Texture[MaxRenderTargetCount];
        private int renderTargetCount;


        /// <summary>
        ///   Gets the Depth-Stencil Buffer (Z-Buffer) currently bound to the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set the Depth-Stencil Buffer, use one of the <see cref="SetRenderTargetAndViewport"/>,
        ///   or <see cref="SetRenderTargetsAndViewport"/>, or <see cref="SetRenderTargets"/> methods.
        /// </remarks>
        public Texture DepthStencilBuffer => depthStencilBuffer;

        /// <summary>
        ///   Gets the first Render Target currently bound to the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set Render Targets, use one of the <see cref="SetRenderTargetAndViewport"/>,
        ///   or <see cref="SetRenderTargetsAndViewport"/>, or <see cref="SetRenderTargets"/> methods.
        /// </remarks>
        public Texture RenderTarget => renderTargets[0];

        /// <summary>
        ///   Gets the set of Render Targets currently bound to the pipeline.
        /// </summary>
        /// <remarks>
        ///   To set Render Targets, use one of the <see cref="SetRenderTargetAndViewport"/>,
        ///   or <see cref="SetRenderTargetsAndViewport"/>, or <see cref="SetRenderTargets"/> methods.
        /// </remarks>
        public ReadOnlySpan<Texture> RenderTargets => renderTargets.AsSpan(0, renderTargetCount);

        /// <summary>
        ///   Gets the number of Render Targets currently bound to the pipeline.
        /// </summary>
        public int RenderTargetCount => renderTargetCount;


        /// <summary>
        ///   Unbinds all the Render Targets and the Depth-Stencil Buffer from the output-merger stage.
        /// </summary>
        public void ResetTargets()
        {
            ResetTargetsImpl();

            depthStencilBuffer = null;
            renderTargets.AsSpan().Clear();
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and a single Render Target to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetView">
        ///   A view of the Render Target to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargetAndViewport(Texture depthStencilView, Texture renderTargetView)
        {
            depthStencilBuffer = depthStencilView;

            renderTargets[0] = renderTargetView;  // TODO: Should we clear the other entries?
            renderTargetCount = renderTargetView is not null ? 1 : 0;

            SetRenderTargetsAndViewportImpl(depthStencilBuffer, renderTargetCount > 0 ? [ renderTargetView ] : []);
        }

        /// <summary>
        ///   Binds one or more Render Targets atomically to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
        ///   Also unbinds the current Depth-Stencil Buffer.
        /// </summary>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify an empty collection to unbind the currently bound Render Targets.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> contains too many Render Targets to set.
        /// </exception>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargetsAndViewport(ReadOnlySpan<Texture> renderTargetViews)
        {
            depthStencilBuffer = null;

            renderTargetCount = renderTargetViews.Length;
            renderTargetViews.CopyTo(renderTargets);

            SetRenderTargetsAndViewportImpl(depthStencilView: null, renderTargetViews);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and one or more Render Targets atomically to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify an empty collection to unbind the currently bound Render Targets.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> contains too many Render Targets to set.
        /// </exception>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargetsAndViewport(Texture depthStencilView, params ReadOnlySpan<Texture> renderTargetViews)
        {
            depthStencilBuffer = depthStencilView;

            renderTargetCount = renderTargetViews.Length;
            renderTargetViews.CopyTo(renderTargets);  // TODO: Should we clear the other entries?

            SetRenderTargetsAndViewportImpl(depthStencilBuffer, renderTargetViews);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and a single Render Target to the output-merger stage.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetView">
        ///   A view of the Render Target to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTarget(Texture depthStencilView, Texture renderTargetView)
        {
            depthStencilBuffer = depthStencilView;

            renderTargetCount = renderTargetView is not null ? 1 : 0;
            renderTargets[0] = renderTargetView;  // TODO: Should we clear the other entries?

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount > 0 ? [ renderTargetView ] : []);
        }

        /// <summary>
        ///   Binds one or more Render Targets atomically to the output-merger stage,
        ///   and unbinds any Depth-Stencil Buffer.
        /// </summary>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify an empty collection to unbind the currently bound Render Targets.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> contains too many Render Targets to set.
        /// </exception>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargets(ReadOnlySpan<Texture> renderTargetViews)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(renderTargetViews.Length, MaxRenderTargetCount);

            depthStencilBuffer = null;

            renderTargetCount = renderTargetViews.Length;
            renderTargetViews.CopyTo(renderTargets);  // TODO: Should we clear the other entries?

            SetRenderTargetsImpl(depthStencilView: null, renderTargetViews);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and one or more Render Targets atomically to the output-merger stage.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify an empty collection to unbind the currently bound Render Targets.
        /// </param>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> contains too many Render Targets to set.
        /// </exception>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargets(Texture depthStencilView, params ReadOnlySpan<Texture> renderTargetViews)
        {
            ArgumentOutOfRangeException.ThrowIfGreaterThan(renderTargetViews.Length, MaxRenderTargetCount);

            depthStencilBuffer = depthStencilView;

            renderTargetCount = renderTargetViews.Length;
            renderTargetViews.CopyTo(renderTargets);  // TODO: Should we clear the other entries?

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetViews);
        }


        /// <summary>
        ///   Binds a Depth-Stencil Buffer and one or more Render Targets atomically to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
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
        private void SetRenderTargetsAndViewportImpl(Texture depthStencilView, ReadOnlySpan<Texture> renderTargetViews)
        {
            if (depthStencilView is not null)
            {
                SetViewport(new Viewport(x: 0, y: 0, depthStencilView.ViewWidth, depthStencilView.ViewHeight));
            }
            else if (!renderTargetViews.IsEmpty)
            {
                ArgumentOutOfRangeException.ThrowIfGreaterThan(renderTargetViews.Length, MaxRenderTargetCount);

                // Setup the viewport from the first Render Target View
                var rtv = renderTargetViews[0];
                SetViewport(new Viewport(x: 0, y: 0, rtv.ViewWidth, rtv.ViewHeight));
            }

            SetRenderTargetsImpl(depthStencilView, renderTargetViews);
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
        private partial void SetRenderTargetsImpl(Texture depthStencilView, ReadOnlySpan<Texture> renderTargetViews);

        #endregion

        /// <summary>
        ///   Resets a Command List back to its initial state as if a new Command List was just created.
        /// </summary>
        public unsafe partial void Reset();

        /// <summary>
        ///   Closes and executes the Command List.
        /// </summary>
        public partial void Flush();

        /// <summary>
        ///   Indicates that recording to the Command List has finished.
        /// </summary>
        /// <returns>
        ///   A <see cref="CompiledCommandList"/> representing the frozen list of recorded commands
        ///   that can be executed at a later time.
        /// </returns>
        public partial CompiledCommandList Close();

        /// <summary>
        ///   Clears and restores the state of the Graphics Device.
        /// </summary>
        public void ClearState()
        {
            ClearStateImpl();

            // Setup empty viewports
            Array.Clear(viewports);

            // Setup empty scissors
            scissorsDirty = true;
            Array.Clear(scissors);

            // Setup the default Render Target and Depth-Stencil Buffer
            var deviceDepthStencilBuffer = GraphicsDevice.Presenter?.DepthStencilBuffer;
            var deviceBackBuffer = GraphicsDevice.Presenter?.BackBuffer;
            SetRenderTargetAndViewport(deviceDepthStencilBuffer, deviceBackBuffer);
        }

        /// <summary>
        ///   Platform-specific implementation that clears and restores the state of the Graphics Device.
        /// </summary>
        private partial void ClearStateImpl();


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
        /// <exception cref="ArgumentNullException"><paramref name="resource"/> is <see langword="null"/>.</exception
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="resource"/> is a <see cref="Texture"/>, but its <see cref="Texture.Dimension"/> is not one of the supported types.
        /// </exception
        /// <exception cref="InvalidOperationException"><paramref name="resource"/> is of an unknown type and cannot be updated.</exception>
        /// <inheritdoc cref="UpdateSubResource(GraphicsResource, int, ReadOnlySpan{byte}, ResourceRegion)" path="/remarks" />
        internal unsafe partial void UpdateSubResource(GraphicsResource resource, int subResourceIndex, DataBox sourceData, ResourceRegion region);

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
        public unsafe partial MappedResource MapSubResource(GraphicsResource resource, int subResourceIndex, MapMode mapMode, bool doNotWait = false, int offsetInBytes = 0, int lengthInBytes = 0);

        // TODO GRAPHICS REFACTOR what should we do with this?
        /// <summary>
        ///   Unmaps a sub-resource of a Graphics Resource, which was previously mapped to CPU memory with <see cref="MapSubResource"/>,
        ///   and in the process re-enables the GPU access to that sub-resource.
        /// </summary>
        /// <param name="mappedResource">
        ///   A <see cref="MappedResource"/> structure identifying the sub-resource to unmap.
        /// </param>
        public unsafe partial void UnmapSubResource(MappedResource mappedResource);
    }
}
