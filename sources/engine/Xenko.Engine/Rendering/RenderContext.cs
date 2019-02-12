// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Engine;
using Xenko.Games;
using Xenko.Graphics;
using Xenko.Streaming;
using ComponentBase = Xenko.Core.ComponentBase;
using IServiceRegistry = Xenko.Core.IServiceRegistry;

namespace Xenko.Rendering
{
    /// <summary>
    /// Rendering context.
    /// </summary>
    public sealed class RenderContext : ComponentBase
    {
        private const string SharedImageEffectContextKey = "__SharedRenderContext__";
        private readonly ThreadLocal<RenderDrawContext> threadContext;
        private readonly object threadContextLock = new object();

        // Used for API that don't support multiple command lists
        internal CommandList SharedCommandList;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext" /> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        internal RenderContext(IServiceRegistry services)
        {
            Services = services ?? throw new ArgumentNullException(nameof(services));
            Effects = services.GetSafeServiceAs<EffectSystem>();
            GraphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            Allocator = services.GetSafeServiceAs<GraphicsContext>().Allocator ?? new GraphicsResourceAllocator(GraphicsDevice).DisposeBy(GraphicsDevice);
            StreamingManager = services.GetService<StreamingManager>();

            threadContext = new ThreadLocal<RenderDrawContext>(() =>
            {
                lock (threadContextLock)
                {
                    return new RenderDrawContext(Services, this, new GraphicsContext(GraphicsDevice, Allocator));
                }
            }, true);
        }

        /// <summary>
        /// Occurs when a renderer is initialized.
        /// </summary>
        public event Action<IGraphicsRendererCore> RendererInitialized;

        /// <summary>
        /// Gets the content manager.
        /// </summary>
        /// <value>The content manager.</value>
        public EffectSystem Effects { get; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; }

        /// <summary>
        /// Gets the services registry.
        /// </summary>
        /// <value>The services registry.</value>
        public IServiceRegistry Services { get; }

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <value>The time.</value>
        public GameTime Time { get; internal set; }

        /// <summary>
        /// Gets the <see cref="GraphicsResource"/> allocator.
        /// </summary>
        /// <value>The allocator.</value>
        public GraphicsResourceAllocator Allocator { get; }

        /// <summary>
        /// The current render system.
        /// </summary>
        public RenderSystem RenderSystem { get; set; }

        /// <summary>
        /// The current visibility group from the <see cref="SceneInstance"/> and <see cref="RenderSystem"/>.
        /// </summary>
        public VisibilityGroup VisibilityGroup { get; set; }

        /// <summary>
        /// The current render output format (used during the collect phase).
        /// </summary>
        public RenderOutputDescription RenderOutput;

        /// <summary>
        /// The current render output format (used during the collect phase).
        /// </summary>
        public ViewportState ViewportState;

        /// <summary>
        /// The current render view.
        /// </summary>
        public RenderView RenderView { get; set; }

        /// <summary>
        /// The streaming manager.
        /// </summary>
        [CanBeNull]
        public StreamingManager StreamingManager { get; set; }

        protected override void Destroy()
        {
            foreach (var renderDrawContext in threadContext.Values)
            {
                renderDrawContext.Dispose();
            }
            threadContext.Dispose();

            base.Destroy();
        }

        /// <summary>
        /// Gets a global shared context.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>RenderContext.</returns>
        public static RenderContext GetShared(IServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            // Store RenderContext shared into the GraphicsDevice
            var graphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            return graphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, SharedImageEffectContextKey, d => new RenderContext(services));
        }

        /// <summary>
        /// Saves a viewport state and restores after using it.
        /// </summary>
        public ViewportRestore SaveViewportAndRestore()
        {
            return new ViewportRestore(this);
        }

        /// <summary>
        /// Saves a viewport and restores it after using it.
        /// </summary>
        public RenderOutputRestore SaveRenderOutputAndRestore()
        {
            return new RenderOutputRestore(this);
        }

        /// <summary>
        /// Pushes a render view and restores it after using it.
        /// </summary>
        /// <param name="renderView">The render view.</param>
        public RenderViewRestore PushRenderViewAndRestore(RenderView renderView)
        {
            var result = new RenderViewRestore(this);
            RenderView = renderView;
            return result;
        }

        public RenderDrawContext GetThreadContext() => threadContext.Value;

        public void Reset()
        {
            foreach (var context in threadContext.Values)
            {
                context.ResourceGroupAllocator.Reset(context.CommandList);
            }
        }

        public void Flush()
        {
            foreach (var context in threadContext.Values)
            {
                context.ResourceGroupAllocator.Flush();
                context.QueryManager.Flush();
            }
        }

        internal void OnRendererInitialized(IGraphicsRendererCore obj)
        {
            RendererInitialized?.Invoke(obj);
        }

        public struct ViewportRestore : IDisposable
        {
            private readonly RenderContext context;
            private readonly ViewportState previousValue;

            public ViewportRestore(RenderContext context)
            {
                this.context = context;
                this.previousValue = context.ViewportState;
            }

            public void Dispose()
            {
                context.ViewportState = previousValue;
            }
        }

        public struct RenderOutputRestore : IDisposable
        {
            private readonly RenderContext context;
            private readonly RenderOutputDescription previousValue;

            public RenderOutputRestore(RenderContext context)
            {
                this.context = context;
                this.previousValue = context.RenderOutput;
            }

            public void Dispose()
            {
                context.RenderOutput = previousValue;
            }
        }

        public struct RenderViewRestore : IDisposable
        {
            private readonly RenderContext context;
            private readonly RenderView previousValue;

            public RenderViewRestore(RenderContext context)
            {
                this.context = context;
                this.previousValue = context.RenderView;
            }

            public void Dispose()
            {
                context.RenderView = previousValue;
            }
        }
    }
}
