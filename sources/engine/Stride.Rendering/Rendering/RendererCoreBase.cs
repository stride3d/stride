// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Core.Serialization.Contents;
using Stride.Graphics;

using Buffer = Stride.Graphics.Buffer;

namespace Stride.Rendering
{
    /// <summary>
    /// Base implementation of <see cref="IGraphicsRenderer"/>
    /// </summary>
    [DataContract]
    public abstract class RendererCoreBase : ComponentBase, IGraphicsRendererCore
    {
        private bool isInDrawCore;
        private ProfilingKey profilingKey;
        private readonly List<GraphicsResource> scopedResources = new List<GraphicsResource>();
        private readonly List<IGraphicsRendererCore> subRenderersToUnload;

        /// <summary>
        /// Initializes a new instance of the <see cref="RendererBase"/> class.
        /// </summary>
        protected RendererCoreBase()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentBase" /> class.
        /// </summary>
        /// <param name="name">The name attached to this component</param>
        protected RendererCoreBase(string name)
            : base(name)
        {
            Enabled = true;
            subRenderersToUnload = new List<IGraphicsRendererCore>();
            Profiling = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="EntityComponentRendererBase"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        /// <userdoc>Enabled if checked, disabled otherwise</userdoc>
        [DataMember(-20)]
        [DefaultValue(true)]
        public virtual bool Enabled { get; set; }

        [DataMemberIgnore]
        public bool Profiling { get; set; }

        [DataMemberIgnore]
        public ProfilingKey ProfilingKey => profilingKey ?? (profilingKey = new ProfilingKey(Name));

        [DataMemberIgnore]
        protected RenderContext Context { get; private set; }

        /// <summary>
        /// Gets the <see cref="IServiceRegistry"/>.
        /// </summary>
        /// <value>The service registry.</value>
        [DataMemberIgnore]
        protected IServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets the <see cref="ContentManager"/>.
        /// </summary>
        /// <value>The asset manager.</value>
        [DataMemberIgnore]
        protected ContentManager Content { get; private set; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        [DataMemberIgnore]
        protected GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets the effect system.
        /// </summary>
        /// <value>The effect system.</value>
        [DataMemberIgnore]
        protected EffectSystem EffectSystem { get; private set; }

        public bool Initialized { get; private set; }

        public void Initialize(RenderContext context)
        {
            if (context == null) throw new ArgumentNullException("context");

            // Unload the previous context if any
            if (Context != null)
            {
                Unload();
            }

            Context = context;
            subRenderersToUnload.Clear();

            // Initialize most common services related to rendering
            Services = Context.Services;
            Content = Services.GetSafeServiceAs<ContentManager>();
            EffectSystem = Services.GetSafeServiceAs<EffectSystem>();
            GraphicsDevice = Services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;

            InitializeCore();

            Initialized = true;

            // Notify that a particular renderer has been initialized.
            context.OnRendererInitialized(this);
        }

        protected virtual void InitializeCore()
        {
        }

        /// <summary>
        /// Unloads this instance on dispose.
        /// </summary>
        protected virtual void Unload()
        {
            foreach (var drawEffect in subRenderersToUnload)
            {
                drawEffect.Dispose();
            }
            subRenderersToUnload.Clear();

            Context = null;
        }

        protected virtual void PreDrawCore(RenderDrawContext context)
        {
        }

        protected virtual void PostDrawCore(RenderDrawContext context)
        {
        }

        /// <summary>
        /// Gets a render target with the specified description, scoped for the duration of the <see cref="DrawEffect.DrawCore"/>.
        /// </summary>
        /// <param name="description">The description of the buffer to allocate</param>
        /// <param name="viewFormat">The pixel format seen in shader</param>
        /// <returns>A new instance of texture.</returns>
        protected Buffer NewScopedBuffer(BufferDescription description, PixelFormat viewFormat = PixelFormat.None)
        {
            CheckIsInDrawCore();
            return PushScopedResource(Context.Allocator.GetTemporaryBuffer(description, viewFormat));
        }

        /// <summary>
        /// Gets a render target with the specified description, scoped for the duration of the <see cref="DrawEffect.DrawCore"/>.
        /// </summary>
        /// <returns>A new instance of texture.</returns>
        protected Buffer NewScopedTypedBuffer(int count, PixelFormat viewFormat, bool isUnorderedAccess, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return NewScopedBuffer(new BufferDescription(count * viewFormat.SizeInBytes(), BufferFlags.ShaderResource | (isUnorderedAccess ? BufferFlags.UnorderedAccess : BufferFlags.None), usage), viewFormat);
        }

        /// <summary>
        /// Pushes a new scoped resource to the current Draw.
        /// </summary>
        /// <param name="resource">The scoped resource</param>
        /// <returns></returns>
        protected T PushScopedResource<T>(T resource) where T : GraphicsResource
        {
            scopedResources.Add(resource);
            return resource;
        }

        /// <summary>
        /// Checks that the current execution path is between a PreDraw/PostDraw sequence and throws and exception if not.
        /// </summary>
        protected void CheckIsInDrawCore()
        {
            if (!isInDrawCore)
            {
                throw new InvalidOperationException("The method execution path is not within a DrawCore operation");
            }
        }

        protected override void Destroy()
        {
            // If this instance is destroyed and not unload, force an unload before destryoing it completely
            if (Context != null)
            {
                Unload();
            }
            base.Destroy();
        }

        protected T ToLoadAndUnload<T>(T effect) where T : class, IGraphicsRendererCore
        {
            if (effect == null) throw new ArgumentNullException("effect");
            effect.Initialize(Context);
            subRenderersToUnload.Add(effect);
            return effect;
        }

        private void ReleaseAllScopedResources()
        {
            foreach (var scopedResource in scopedResources)
            {
                Context.Allocator.ReleaseReference(scopedResource);
            }
            scopedResources.Clear();
        }

        protected void PreDrawCoreInternal(RenderDrawContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }

            EnsureContext(context.RenderContext);

            if (ProfilingKey.Name != null && Profiling)
            {
                context.QueryManager.BeginProfile(Color.Green, ProfilingKey);
            }

            PreDrawCore(context);

            // Allow scoped allocation RenderTargets
            isInDrawCore = true;
        }

        protected void EnsureContext(RenderContext context)
        {
            if (Context == null)
            {
                Initialize(context);
            }
            else if (Context != context)
            {
                throw new InvalidOperationException("Cannot use a different context between Load and Draw");
            }
        }

        protected void PostDrawCoreInternal(RenderDrawContext context)
        {
            isInDrawCore = false;

            // Release scoped RenderTargets
            ReleaseAllScopedResources();

            PostDrawCore(context);

            if (ProfilingKey.Name != null && Profiling)
            {
                context.QueryManager.EndProfile(ProfilingKey);
            }
        }
    }
}
