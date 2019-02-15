// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core;
using Xenko.Core.Annotations;

namespace Xenko.Rendering.Compositing
{
    /// <summary>
    /// Describes the code part of a <see cref="GraphicsCompositor"/>.
    /// </summary>
    [DataContract(Inherited = true)]
    public abstract class SceneRendererBase : RendererCoreBase, ISceneRenderer
    {
        /// <inheritdoc/>
        [DataMember(-100), Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        protected SceneRendererBase()
        {
            Id = Guid.NewGuid();
        }

        /// <inheritdoc/>
        public void Collect(RenderContext context)
        {
            EnsureContext(context);

            CollectCore(context);
        }

        /// <inheritdoc/>
        public void Draw(RenderDrawContext context)
        {
            if (Enabled)
            {
                PreDrawCoreInternal(context);
                DrawCore(context.RenderContext, context);
                PostDrawCoreInternal(context);
            }
        }

        /// <summary>
        /// Main collect method.
        /// </summary>
        /// <param name="context"></param>
        protected virtual void CollectCore(RenderContext context)
        {
        }

        /// <summary>
        /// Main drawing method for this renderer that must be implemented. 
        /// </summary>
        /// <param name="context"></param>
        /// <param name="drawContext"></param>
        protected abstract void DrawCore(RenderContext context, RenderDrawContext drawContext);
    }
}
