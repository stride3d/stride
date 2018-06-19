// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Graphics;

namespace Xenko.Rendering
{
    /// <summary>
    /// Entry-point for implementing rendering feature.
    /// </summary>
    [DataContract(Inherited = true, DefaultMemberMode = DataMemberMode.Never)]
    public abstract class RenderFeature : ComponentBase, IGraphicsRendererCore
    {
        private RenderSystem renderSystem;

        protected RenderContext Context { get; private set; }

        public RenderSystem RenderSystem
        {
            get => renderSystem;
            internal set
            {
                renderSystem = value;
                OnRenderSystemChanged();
            }
        }

        public bool Initialized { get; private set; }

        public bool Enabled { get => true; set => throw new NotSupportedException(); }

        public void Initialize(RenderContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));

            if (Context != null)
            {
                Unload();
            }

            Context = context;

            InitializeCore();

            Initialized = true;

            // Notify that a particular renderer has been initialized.
            context.OnRendererInitialized(this);
        }

        public virtual void Unload()
        {
            Context = null;
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

        /// <summary>
        /// Initializes this instance.
        /// Query for specific cbuffer (either new one, like PerMaterial, or parts of an existing one, like PerObject=>Skinning)
        /// </summary>
        protected virtual void InitializeCore()
        {
        }

        /// <summary>
        /// Performs pipeline initialization, enumerates views and populates visibility groups.
        /// </summary>
        public virtual void Collect()
        {
        }

        /// <summary>
        /// Extract data from entities, should be as fast as possible to not block simulation loop. It should be mostly copies, and the actual processing should be part of Prepare().
        /// </summary>
        public virtual void Extract()
        {
        }

        /// <summary>
        /// Perform effect permutations, before <see cref="Prepare"/>.
        /// </summary>
        /// <param name="context"></param>
        public virtual void PrepareEffectPermutations(RenderDrawContext context)
        {
        }

        /// <summary>
        /// Performs most of the work (computation and resource preparation). Later game simulation might be running during that step.
        /// </summary>
        /// <param name="context"></param>
        public virtual void Prepare(RenderDrawContext context)
        {
        }

        /// <summary>
        /// Performs GPU updates and/or drawing, everytime a render view changes.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderView"></param>
        /// <param name="renderViewStage"></param>
        public virtual void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage)
        {
        }

        /// <summary>
        /// Performs GPU updates and/or draw.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderView"></param>
        /// <param name="renderViewStage"></param>
        /// <param name="startIndex"></param>
        /// <param name="endIndex"></param>
        public virtual void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
        }

        /// <summary>
        /// Releases temporary resources and cleans the state. Should be called once after all <see cref="Draw"/> calls have finished.
        /// </summary>
        /// <param name="context"></param>
        public virtual void Flush(RenderDrawContext context)
        {
        }

        protected virtual void OnRenderSystemChanged()
        {
        }
    }
}
