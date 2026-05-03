// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Rendering context used during <see cref="IGraphicsRendererBase.Draw(RenderDrawContext)"/>.
    /// </summary>
    public sealed class RenderDrawContext : ComponentBase
    {
        // States
        private int currentStateIndex = -1;
        private readonly List<RenderTargetsState> allocatedStates = new List<RenderTargetsState>(10);

        private readonly Dictionary<Type, DrawEffect> sharedEffects = new Dictionary<Type, DrawEffect>();

        public RenderDrawContext(IServiceRegistry services, RenderContext renderContext, GraphicsContext graphicsContext)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            RenderContext = renderContext;
            ResourceGroupAllocator = graphicsContext.ResourceGroupAllocator;
            GraphicsDevice = RenderContext.GraphicsDevice;
            GraphicsContext = graphicsContext;
            CommandList = graphicsContext.CommandList;
            Resolver = new ResourceResolver(this);
            QueryManager = new QueryManager(CommandList, renderContext.Allocator);
        }

        /// <summary>
        /// Gets the render context.
        /// </summary>
        public RenderContext RenderContext { get; }

        /// <summary>
        /// Gets the <see cref="ResourceGroup"/> allocator.
        /// </summary>
        public ResourceGroupAllocator ResourceGroupAllocator { get; }

        /// <summary>
        /// Gets the command list.
        /// </summary>
        public CommandList CommandList { get; }

        public GraphicsContext GraphicsContext { get; }

        public GraphicsDevice GraphicsDevice { get; }

        public ResourceResolver Resolver { get; }

        public QueryManager QueryManager { get; }

        /// <summary>
        /// Locks the command list until <see cref="IDisposable.Dispose()"/> is called on the returned value type.
        /// </summary>
        /// <returns></returns>
        /// This is necessary only during Collect(), Extract() and Prepare() phases, not during Draw().
        /// Some graphics API might not require actual locking, in which case this object might do nothing.
        public DefaultCommandListLock LockCommandList()
        {
            // TODO: Temporary, for now we use the CommandList itself as a lock
            return new DefaultCommandListLock(CommandList);
        }

        /// <summary>
        /// Pushes render targets and viewport state.
        /// </summary>
        public RenderTargetRestore PushRenderTargetsAndRestore()
        {
            // Check if we need to allocate a new StateAndTargets
            RenderTargetsState newState;
            currentStateIndex++;
            if (currentStateIndex == allocatedStates.Count)
            {
                newState = new RenderTargetsState();
                allocatedStates.Add(newState);
            }
            else
            {
                newState = allocatedStates[currentStateIndex];
            }
            newState.Capture(CommandList);

            return new RenderTargetRestore(this);
        }

        /// <summary>
        /// Restores render targets and viewport state.
        /// </summary>
        public void PopRenderTargets()
        {
            if (currentStateIndex < 0)
            {
                throw new InvalidOperationException("Cannot pop more than push");
            }

            var oldState = allocatedStates[currentStateIndex--];
            oldState.Restore(CommandList);
        }

        /// <summary>
        /// Gets or creates a shared effect.
        /// </summary>
        /// <typeparam name="T">Type of the shared effect (mush have a constructor taking a <see cref="Rendering.RenderContext"/></typeparam>
        /// <returns>A singleton instance of <typeparamref name="T"/></returns>
        public T GetSharedEffect<T>() where T : DrawEffect, new()
        {
            // TODO: Add a way to support custom constructor
            lock (sharedEffects)
            {
                DrawEffect effect;
                if (!sharedEffects.TryGetValue(typeof(T), out effect))
                {
                    effect = new T();
                    sharedEffects.Add(typeof(T), effect);
                    effect.Initialize(RenderContext);
                }

                return (T)effect;
            }
        }

        /// <summary>
        ///   Holds a captured snapshot of Viewports and Render Targets set in a Command List.
        /// </summary>
        private class RenderTargetsState
        {
            public int RenderTargetCount;
            public int ViewportCount;

            public readonly Viewport[] Viewports = new Viewport[CommandList.MaxViewportAndScissorRectangleCount];
            public readonly Texture[] RenderTargets = new Texture[CommandList.MaxRenderTargetCount];
            public Texture DepthStencilBuffer;

            public void Capture(CommandList commandList)
            {
                DepthStencilBuffer = commandList.DepthStencilBuffer;

                RenderTargetCount = commandList.RenderTargetCount;
                commandList.RenderTargets.CopyTo(RenderTargets);

                ViewportCount = commandList.ViewportCount;
                commandList.Viewports.CopyTo(Viewports);

                // TODO: Backup scissor rectangles and restore them
            }

            public void Restore(CommandList commandList)
            {
                scoped ReadOnlySpan<Texture> renderTargetsToRestore = RenderTargets.AsSpan(0, RenderTargetCount);
                commandList.SetRenderTargets(DepthStencilBuffer, renderTargetsToRestore);

                scoped ReadOnlySpan<Viewport> viewportsToRestore = Viewports.AsSpan(0, ViewportCount);
                commandList.SetViewports(viewportsToRestore);
            }
        }

        public struct RenderTargetRestore : IDisposable
        {
            private readonly RenderDrawContext context;

            public RenderTargetRestore(RenderDrawContext context)
            {
                this.context = context;
            }

            public void Dispose()
            {
                context.PopRenderTargets();
            }
        }
    }
}
