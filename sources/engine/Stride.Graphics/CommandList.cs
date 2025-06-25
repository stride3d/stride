// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

using Stride.Core.Mathematics;

namespace Stride.Graphics
{
    /// <summary>
    /// Performs resource bindings and primitive-based rendering. See the <see cref="GraphicsDevice"/> class to learn more about the class.
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
        /// Sets a viewport.
        /// </summary>
        /// <value>The viewport.</value>
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
        /// Sets the viewports.
        /// </summary>
        /// <value>The viewport.</value>
        public void SetViewports(Viewport[] viewports)
        {
            SetViewports(viewports.Length, viewports);
        }

        /// <summary>
        /// Sets the viewports.
        /// </summary>
        /// <value>The viewport.</value>
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
        /// Binds a single scissor rectangle to the rasterizer stage.
        /// See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        /// in the manual for more information.
        /// </summary>
        /// <param name="rectangle">The scissor rectangle.</param>
        public Rectangle[] Scissors => scissors;

        /// <summary>
        ///   Gets the number of scissor rectangles currently bound to the rasterizer stage of the pipeline.
        /// </summary>
        public int ScissorCount => boundScissorCount;

        public void SetScissorRectangle(Rectangle rectangle)
        {
            scissorsDirty = true;
            boundScissorCount = 1;
            scissors[0] = rectangle;
            SetScissorRectangleImpl(ref rectangle);
        }

        /// <summary>
        /// Binds a set of scissor rectangles to the rasterizer stage.
        /// See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        /// in the manual for more information.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        public void SetScissorRectangles(Rectangle[] scissorRectangles)
        {
            SetScissorRectangles(scissorRectangles.Length, scissorRectangles);
        }

        /// <summary>
        /// Binds a set of scissor rectangles to the rasterizer stage.
        /// See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-set-the-scissor">Set the scissor</see>
        /// in the manual for more information.
        /// </summary>
        /// <param name="scissorCount">The number of scissor rectangles to bind.</param>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
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

        private unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle);

        /// <summary>
        /// Binds a depth-stencil buffer and a single render target to the output-merger stage.
        /// See <see href="https://doc.stride3d.net/latest/en/manual/graphics/low-level-api/textures-and-render-textures.html#code-use-a-render-target">Use a render target</see>
        /// in the manual for more information.
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


        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetView">A view of the render target to bind.</param>
        public void ResetTargets()
        {
            ResetTargetsImpl();

            depthStencilBuffer = null;
            for (int i = 0; i < renderTargets.Length; i++)
                renderTargets[i] = null;
        }

        public void SetRenderTargetAndViewport(Texture depthStencilView, Texture renderTargetView)
        {
            depthStencilBuffer = depthStencilView;
            renderTargets[0] = renderTargetView;
            renderTargetCount = renderTargetView is not null ? 1 : 0;

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
        }

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
        ///     <p>Bind one or more render targets atomically and the depth-stencil buffer to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.</p>
        /// </summary>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        public void SetRenderTargetsAndViewport(Texture[] renderTargetViews)
        {
            SetRenderTargetsAndViewport(depthStencilView: null, renderTargetViews);
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetViewCount">The number of render target in <paramref name="renderTargetViews"/>.</param>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
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
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
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
        /// Binds a depth-stencil buffer and a single render target to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetView">A view of the render target to bind.</param>
        public void SetRenderTarget(Texture depthStencilView, Texture renderTargetView)
        {
            depthStencilBuffer = depthStencilView;
            renderTargets[0] = renderTargetView;
            renderTargetCount = renderTargetView is not null ? 1 : 0;

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///     <p>Bind one or more render targets atomically and the depth-stencil buffer to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.</p>
        /// </summary>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        public void SetRenderTargets(Texture[] renderTargetViews)
        {
            SetRenderTargets(depthStencilView: null, renderTargetViews);
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetViewCount">The number of render target in <paramref name="renderTargetViews"/>.</param>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
        public void SetRenderTargets(Texture depthStencilView, int renderTargetViewCount, Span<Texture> renderTargetViews)
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
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
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

        public partial void Reset();

        public partial void Flush();

        public partial CompiledCommandList Close();

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

        private partial void ClearStateImpl();
    }
}
