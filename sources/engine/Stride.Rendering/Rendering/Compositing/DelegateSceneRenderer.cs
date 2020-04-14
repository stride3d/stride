// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Rendering.Compositing
{
    /// <summary>
    /// A renderer which can provide <see cref="RendererBase.Draw"/> implementation with a delegate.
    /// </summary>
    public partial class DelegateSceneRenderer : SceneRendererBase
    {
        private readonly Action<RenderDrawContext> drawAction;

        public DelegateSceneRenderer(Action<RenderDrawContext> drawAction)
        {
            this.drawAction = drawAction;
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            drawAction(drawContext);
        }
    }
}
