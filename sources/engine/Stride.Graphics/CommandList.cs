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
        private const int MaxRenderTargetCount = 8;
        internal const int MaxViewportAndScissorRectangleCount = 16;

        #region Viewports

        private bool viewportDirty = false;

        private int boundViewportCount;
        private readonly Viewport[] viewports = new Viewport[MaxViewportAndScissorRectangleCount];


        /// <summary>
        ///   Gets the first viewport bound to the rasterizer stage of the pipeline.
        /// </summary>
        public Viewport Viewport => viewports[0];

        /// <summary>
        ///   Gets the array of viewports bound to the rasterizer stage of the pipeline.
        /// </summary>
        public Viewport[] Viewports => viewports;

        /// <summary>
        ///   Gets the number of viewports currently bound to the rasterizer stage of the pipeline.
        /// </summary>
        public int ViewportCount => boundViewportCount;

        /// <summary>
        ///   Binds a single viewport to the rasterizer stage of the pipeline.
        /// </summary>
        /// <value>The viewport to set.</value>
        public void SetViewport(Viewport viewport)
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
        /// <param name="viewports">The array of viewports to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="viewports"/> is <see langword="null"/>.</exception>
        public void SetViewports(Viewport[] viewports)
        {
            SetViewports(viewports.Length, viewports);
        }

        /// <summary>
        ///   Binds an array of viewports to the rasterizer stage of the pipeline.
        /// </summary>
        /// <param name="viewportCount">The number of viewports to set.</param>
        /// <param name="viewports">The array of viewports to set.</param>
        /// <exception cref="ArgumentNullException"><paramref name="viewports"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="viewportCount"/> is greater than the number of items in <paramref name="viewports"/>.
        /// </exception>
        public void SetViewports(int viewportCount, Viewport[] viewports)
        {
            ArgumentNullException.ThrowIfNull(viewports);
            ArgumentOutOfRangeException.ThrowIfLessThan(viewports.Length, viewportCount);

            viewportDirty |= boundViewportCount != viewportCount;
            boundViewportCount = viewportCount;

            for (int i = 0; i < viewportCount; i++)
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
        public Rectangle Scissor => scissors[0];

        /// <summary>
        ///   Gets the array of scissor rectangles bound to the rasterizer stage of the pipeline.
        /// </summary>
        public Rectangle[] Scissors => scissors;

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
        public void SetScissorRectangle(Rectangle rectangle)
        {
            scissorsDirty = true;
            boundScissorCount = 1;
            scissors[0] = rectangle;
            SetScissorRectangleImpl(ref rectangle);
        }

        /// <summary>
        ///   Binds a set of scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        ///   in the manual for more information.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="scissorRectangles"/> is <see langword="null"/>.</exception>
        public void SetScissorRectangles(Rectangle[] scissorRectangles)
        {
            SetScissorRectangles(scissorRectangles.Length, scissorRectangles);
        }

        /// <summary>
        ///   Binds a set of scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorCount">The number of scissor rectangles to bind.</param>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        ///   in the manual for more information.
        /// </remarks>
        /// <exception cref="ArgumentNullException"><paramref name="scissorRectangles"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="scissorCount"/> is greater than the number of items in <paramref name="scissorRectangles"/>.
        /// </exception>
        public void SetScissorRectangles(int scissorCount, Rectangle[] scissorRectangles)
        {
            ArgumentNullException.ThrowIfNull(scissorRectangles);
            ArgumentOutOfRangeException.ThrowIfLessThan(scissorRectangles.Length, scissorCount);

            scissorsDirty = true;
            boundScissorCount = scissorCount;
            for (int i = 0; i < scissorCount; ++i)
            {
                scissors[i] = scissorRectangles[i];
            }
            SetScissorRectanglesImpl(scissorCount, scissorRectangles);
        }


        /// <summary>
        ///   Platform-specific implementation that sets a scissor rectangle to the rasterizer stage.
        /// </summary>
        /// <param name="scissorRectangle">The scissor rectangle to set.</param>
        private unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle);

        /// <summary>
        ///   Platform-specific implementation that sets one or more scissor rectangles to the rasterizer stage.
        /// </summary>
        /// <param name="scissorCount">The number of scissor rectangles to bind.</param>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        private unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles);

        #endregion

        #region Render Targets / Depth-Stencil Buffer

        private Texture depthStencilBuffer;

        private Texture[] renderTargets = new Texture[MaxRenderTargetCount];
        private int renderTargetCount;


        /// <summary>
        ///   Gets the Depth-Stencil Buffer (Z-Buffer) currently bound to the pipeline.
        /// </summary>
        public Texture DepthStencilBuffer => depthStencilBuffer;

        /// <summary>
        ///   Gets the first Render Target currently bound to the pipeline.
        /// </summary>
        public Texture RenderTarget => renderTargets[0];

        /// <summary>
        ///   Gets the set of Render Targets currently bound to the pipeline.
        /// </summary>
        public Texture[] RenderTargets => renderTargets;

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
            for (int i = 0; i < renderTargets.Length; i++)
                renderTargets[i] = null;
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
            renderTargets[0] = renderTargetView;
            renderTargetCount = renderTargetView is not null ? 1 : 0;

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and two Render Targets to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetView">
        ///   A view of the first Render Target to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound first Render Target.
        /// </param>
        /// <param name="secondRenderTarget">
        ///   A view of a second Render Target to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound second Render Target.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargetAndViewport(Texture depthStencilView, Texture renderTargetView, Texture secondRenderTarget)
        {
            depthStencilBuffer = depthStencilView;
            renderTargets[0] = renderTargetView;
            renderTargets[1] = secondRenderTarget;
            renderTargetCount = renderTargetView is not null ? 1 : 0;

            if (secondRenderTarget is not null)
                ++renderTargetCount;

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///   Binds one or more Render Targets atomically to the output-merger stage,
        ///   setting also the viewport according to their dimensions. Also unbinds the current Depth-Stencil Buffer.
        /// </summary>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargetsAndViewport(Texture[] renderTargetViews)
        {
            SetRenderTargetsAndViewport(depthStencilView: null, renderTargetViews);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and one or more Render Targets atomically to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetViewCount">
        ///   The current number of Render Targets to bind.
        ///   Specify <c>0</c> to unbind the currently bound Render Targets.
        /// </param>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="renderTargetViewCount"/> is not zero, but <paramref name="renderTargetViews"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> has less elements than what <paramref name="renderTargetViewCount"/> specifies.
        /// </exception>
        public void SetRenderTargetsAndViewport(Texture depthStencilView, int renderTargetViewCount, Texture[] renderTargetViews)
        {
            depthStencilBuffer = depthStencilView;

            renderTargetCount = renderTargetViewCount;
            for (int i = 0; i < renderTargetCount; i++)
            {
                renderTargets[i] = renderTargetViews[i];
            }

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
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
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargetsAndViewport(Texture depthStencilView, Texture[] renderTargetViews)
        {
            depthStencilBuffer = depthStencilView;

            if (renderTargetViews is not null)
            {
                renderTargetCount = renderTargetViews.Length;
                for (int i = 0; i < renderTargetViews.Length; i++)
                {
                    renderTargets[i] = renderTargetViews[i];
                }
            }
            else
            {
                renderTargetCount = 0;
            }

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
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
            renderTargets[0] = renderTargetView;
            renderTargetCount = renderTargetView is not null ? 1 : 0;

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///   Binds one or more Render Targets atomically to the output-merger stage, and unbinds any Depth-Stencil Buffer.
        /// </summary>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargets(Texture[] renderTargetViews)
        {
            SetRenderTargets(depthStencilView: null, renderTargetViews);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and one or more Render Targets atomically to the output-merger stage.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="renderTargetViewCount">
        ///   The current number of Render Targets to bind.
        ///   Specify <c>0</c> to unbind the currently bound Render Targets.
        /// </param>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        /// <exception cref="ArgumentNullException">
        ///   <paramref name="currentRenderTargetCount"/> is not zero, but <paramref name="renderTargetViews"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="renderTargetViews"/> has less elements than what <paramref name="currentRenderTargetCount"/> specifies.
        /// </exception>
        public void SetRenderTargets(Texture depthStencilView, int renderTargetViewCount, Texture[] renderTargetViews)
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
        //public void SetRenderTargets(Texture depthStencilView, int renderTargetViewCount, Span<Texture> renderTargetViews)
        {
            depthStencilBuffer = depthStencilView;

            renderTargetCount = renderTargetViewCount;
            if (renderTargetViewCount > 0)
            {
                ArgumentNullException.ThrowIfNull(renderTargetViews);
                ArgumentOutOfRangeException.ThrowIfLessThan(renderTargetViews.Length, renderTargetViewCount);

                for (int i = 0; i < renderTargetCount; i++)
                {
                    renderTargets[i] = renderTargetViews[i];
                }
            }

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
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
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        /// <remarks>
        ///   See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a Render Target</see>
        ///   in the manual for more information.
        /// </remarks>
        public void SetRenderTargets(Texture depthStencilView, Texture[] renderTargetViews)
        {
            depthStencilBuffer = depthStencilView;

            if (renderTargetViews is not null)
            {
                renderTargetCount = renderTargetViews.Length;
                for (var i = 0; i < renderTargetViews.Length; i++)
                {
                    renderTargets[i] = renderTargetViews[i];
                }
            }
            else
            {
                renderTargetCount = 0;
            }

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///   Binds a Depth-Stencil Buffer and one or more Render Targets atomically to the output-merger stage,
        ///   setting also the viewport according to their dimensions.
        /// </summary>
        /// <param name="depthStencilView">
        ///   A view of the Depth-Stencil Buffer to bind.
        ///   Specify <see langword="null"/> to unbind the currently bound Depth-Stencil Buffer.
        /// </param>
        /// <param name="currentRenderTargetCount">
        ///   The current number of Render Targets to bind.
        ///   Specify <c>0</c> to unbind the currently bound Render Targets.
        /// </param>
        /// <param name="renderTargetViews">
        ///   A set of Render Targets to bind.
        ///   Specify <see langword="null"/> or an empty array to unbind the currently bound Render Targets.
        /// </param>
        private void CommonSetRenderTargetsAndViewport(Texture depthStencilView, int currentRenderTargetCount, Texture[] renderTargetViews)
        {
            if (depthStencilView is not null)
            {
                SetViewport(new Viewport(0, 0, depthStencilView.ViewWidth, depthStencilView.ViewHeight));
            }
            else if (currentRenderTargetCount > 0)
            {
                ArgumentNullException.ThrowIfNull(renderTargetViews);
                ArgumentOutOfRangeException.ThrowIfLessThan(renderTargetViews.Length, currentRenderTargetCount);

                // Setup the viewport from the Render Target View
                var rtv = renderTargetViews[0];
                SetViewport(new Viewport(0, 0, rtv.ViewWidth, rtv.ViewHeight));
            }

            SetRenderTargetsImpl(depthStencilView, currentRenderTargetCount, renderTargetViews);
        }

        #endregion

        /// <summary>
        ///   Resets a Command List back to its initial state as if a new Command List was just created.
        /// </summary>
        public partial void Reset();

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
            for (int i = 0; i < viewports.Length; i++)
                viewports[i] = new Viewport();

            // Setup empty scissors
            scissorsDirty = true;
            for (int i = 0; i < scissors.Length; i++)
                scissors[i] = new Rectangle();

            // Setup the default Render Target
            var deviceDepthStencilBuffer = GraphicsDevice.Presenter?.DepthStencilBuffer;
            var deviceBackBuffer = GraphicsDevice.Presenter?.BackBuffer;
            SetRenderTargetAndViewport(deviceDepthStencilBuffer, deviceBackBuffer);
        }

        /// <summary>
        ///   Platform-specific implementation that clears and restores the state of the Graphics Device.
        /// </summary>
        private partial void ClearStateImpl();
    }
}
