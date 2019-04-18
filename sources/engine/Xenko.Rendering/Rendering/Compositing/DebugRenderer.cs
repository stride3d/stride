// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core;

namespace Xenko.Rendering.Compositing
{
    [Display("Debug renderer")]
    public class DebugRenderer : SceneRendererBase, ISharedRenderer
    {
        public List<RenderStage> DebugRenderStages { get; } = new List<RenderStage>();

        protected override void CollectCore(RenderContext context)
        {
            foreach (var renderStage in DebugRenderStages)
            {
                if (renderStage == null)
                    continue;

                renderStage.Output = context.RenderOutput;
                context.RenderView.RenderStages.Add(renderStage);
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            foreach (var renderStage in DebugRenderStages)
            {
                if (renderStage == null)
                    continue;

                drawContext.RenderContext.RenderSystem.Draw(drawContext, drawContext.RenderContext.RenderView, renderStage);
            }
        }
    }
}
