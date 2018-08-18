// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Core.Mathematics;

namespace Xenko.Graphics
{
    /// <summary>
    /// Performs resource bindings and primitive-based rendering. See the <see cref="GraphicsDevice"/> class to learn more about the class.
    /// </summary>
    public partial class CommandList : GraphicsResourceBase
    {
        private const int MaxRenderTargetCount = 8;
        private const int MaxViewportAndScissorRectangleCount = 16;
        private bool viewportDirty = false;

        private int boundViewportCount;
        private readonly Viewport[] viewports = new Viewport[MaxViewportAndScissorRectangleCount];

        private int boundScissorCount;
        private readonly Rectangle[] scissors = new Rectangle[MaxViewportAndScissorRectangleCount];
        private bool scissorsDirty = false;

        private Texture depthStencilBuffer;

        private Texture[] renderTargets = new Texture[MaxRenderTargetCount];
        private int renderTargetCount;

        /// <summary>
        ///     Gets the first viewport.
        /// </summary>
        /// <value>The first viewport.</value>
        public Viewport Viewport => viewports[0];

        /// <summary>
        ///     Gets the first scissor.
        /// </summary>
        /// <value>The first scissor.</value>
        public Rectangle Scissor => scissors[0];

        /// <summary>
        ///     Gets the depth stencil buffer currently sets on this instance.
        /// </summary>
        /// <value>
        ///     The depth stencil buffer currently sets on this instance.
        /// </value>
        public Texture DepthStencilBuffer => depthStencilBuffer;

        /// <summary>
        ///     Gets the render target buffer currently sets on this instance.
        /// </summary>
        /// <value>
        ///     The render target buffer currently sets on this instance.
        /// </value>
        public Texture RenderTarget => renderTargets[0];

        public Texture[] RenderTargets => renderTargets;

        public int RenderTargetCount => renderTargetCount;

        public Viewport[] Viewports => viewports;
        public int ViewportCount => boundViewportCount;

        /// <summary>
        /// Clears the state and restore the state of the device.
        /// </summary>
        public void ClearState()
        {
            ClearStateImpl();

            // Setup empty viewports
            for (int i = 0; i < viewports.Length; i++)
                viewports[i] = new Viewport();

            // Setup empty scissors
            scissorsDirty = true;
            for (int i = 0; i < viewports.Length; i++)
                scissors[i] = new Rectangle();

            // Setup the default render target
            var deviceDepthStencilBuffer = GraphicsDevice.Presenter?.DepthStencilBuffer;
            var deviceBackBuffer = GraphicsDevice.Presenter?.BackBuffer;
            SetRenderTargetAndViewport(deviceDepthStencilBuffer, deviceBackBuffer);
        }

        /// <summary>
        /// Unbinds all depth-stencil buffer and render targets from the output-merger stage.
        /// </summary>
        public void ResetTargets()
        {
            ResetTargetsImpl();

            depthStencilBuffer = null;
            for (int i = 0; i < renderTargets.Length; i++)
                renderTargets[i] = null;
        }

        /// <summary>
        /// Sets a viewport.
        /// </summary>
        /// <value>The viewport.</value>
        public void SetViewport(Viewport value)
        {
            viewportDirty |= boundViewportCount != 1;
            boundViewportCount = 1;
            if (viewports[0] != value)
            {
                viewportDirty = true;
                viewports[0] = value;
            }
        }

        /// <summary>
        /// Sets the viewports.
        /// </summary>
        /// <value>The viewport.</value>
        public void SetViewports(Viewport[] values)
        {
            SetViewports(values.Length, values);
        }

        /// <summary>
        /// Sets the viewports.
        /// </summary>
        /// <value>The viewport.</value>
        public void SetViewports(int viewportCount, Viewport[] values)
        {
            viewportDirty |= this.boundViewportCount != viewportCount;
            boundViewportCount = viewportCount;
            for (int i = 0; i < viewportCount; i++)
            {
                if (viewports[i] != values[i])
                {
                    viewportDirty = true;
                    viewports[i] = values[i];
                }
            }
        }

        /// <summary>
        /// Binds a single scissor rectangle to the rasterizer stage. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <param name="rectangle">The scissor rectangle.</param>
        public void SetScissorRectangle(Rectangle rectangle)
        {
            scissorsDirty = true;
            boundScissorCount = 1;
            scissors[0] = rectangle;
            SetScissorRectangleImpl(ref rectangle);
        }

        /// <summary>
        /// Binds a set of scissor rectangles to the rasterizer stage. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        public void SetScissorRectangles(Rectangle[] scissorRectangles)
        {
            SetScissorRectangles(scissorRectangles.Length, scissorRectangles);
        }

        /// <summary>
        /// Binds a set of scissor rectangles to the rasterizer stage. See <see cref="Render+states"/> to learn how to use it.
        /// </summary>
        /// <param name="scissorCount">The number of scissor rectangles to bind.</param>
        /// <param name="scissorRectangles">The set of scissor rectangles to bind.</param>
        public void SetScissorRectangles(int scissorCount, Rectangle[] scissorRectangles)
        {
            scissorsDirty = true;
            boundScissorCount = scissorCount;
            for (int i = 0; i < scissorCount; ++i)
            {
                scissors[i] = scissorRectangles[i];
            }
            SetScissorRectanglesImpl(scissorCount, scissorRectangles);
        }

        /// <summary>
        /// Binds a depth-stencil buffer and a single render target to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetView">A view of the render target to bind.</param>
        public void SetRenderTargetAndViewport(Texture depthStencilView, Texture renderTargetView)
        {
            depthStencilBuffer = depthStencilView;
            renderTargets[0] = renderTargetView;
            renderTargetCount = renderTargetView != null ? 1 : 0;

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
        }
 
        public void SetRenderTargetAndViewport(Texture depthStencilView, Texture renderTargetView, Texture secondRenderTarget)
        {
            depthStencilBuffer = depthStencilView;
            renderTargets[0] = renderTargetView;
            renderTargets[1] = secondRenderTarget;
            renderTargetCount = renderTargetView != null ? 1 : 0;

            if (secondRenderTarget != null)
                ++renderTargetCount;

            CommonSetRenderTargetsAndViewport(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///     <p>Bind one or more render targets atomically and the depth-stencil buffer to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.</p>
        /// </summary>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        public void SetRenderTargetsAndViewport(Texture[] renderTargetViews)
        {
            SetRenderTargetsAndViewport(null, renderTargetViews);
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

            if (renderTargetViews != null)
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
            renderTargetCount = renderTargetView != null ? 1 : 0;

            SetRenderTargetsImpl(depthStencilBuffer, renderTargetCount, renderTargets);
        }

        /// <summary>
        ///     <p>Bind one or more render targets atomically and the depth-stencil buffer to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.</p>
        /// </summary>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        public void SetRenderTargets(Texture[] renderTargetViews)
        {
            SetRenderTargets(null, renderTargetViews);
        }
        
        /// <summary>
        /// Binds a depth-stencil buffer and a set of render targets to the output-merger stage. See <see cref="Textures+and+render+targets"/> to learn how to use it.
        /// </summary>
        /// <param name="depthStencilView">A view of the depth-stencil buffer to bind.</param>
        /// <param name="renderTargetViewCount">The number of render target in <paramref name="renderTargetViews"/>.</param>
        /// <param name="renderTargetViews">A set of render target views to bind.</param>
        /// <exception cref="System.ArgumentNullException">renderTargetViews</exception>
        public void SetRenderTargets(Texture depthStencilView, int renderTargetViewCount, Texture[] renderTargetViews)
        {
            depthStencilBuffer = depthStencilView;

            renderTargetCount = renderTargetViewCount;
            for (int i = 0; i < renderTargetCount; i++)
            {
                renderTargets[i] = renderTargetViews[i];
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

            if (renderTargetViews != null)
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
            if (depthStencilView != null)
            {
                SetViewport(new Viewport(0, 0, depthStencilView.ViewWidth, depthStencilView.ViewHeight));
            }
            else if (currentRenderTargetCount > 0)
            {
                // Setup the viewport from the rendertarget view
                var rtv = renderTargetViews[0];
                SetViewport(new Viewport(0, 0, rtv.ViewWidth, rtv.ViewHeight));
            }

            SetRenderTargetsImpl(depthStencilView, currentRenderTargetCount, renderTargetViews);
        }

        unsafe partial void SetScissorRectangleImpl(ref Rectangle scissorRectangle);

        unsafe partial void SetScissorRectanglesImpl(int scissorCount, Rectangle[] scissorRectangles);
    }
}
