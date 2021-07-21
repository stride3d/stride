// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace AnimatedModel
{
    public class RenderTextureSceneRenderer : SceneRendererBase
    {
        public Texture RenderTexture { get; set; }

        public ISceneRenderer Child { get; set; }

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);

            if (RenderTexture == null)
                return;

            using (context.SaveRenderOutputAndRestore())
            using (context.SaveViewportAndRestore())
            {
                context.RenderOutput.RenderTargetFormat0 = RenderTexture.ViewFormat;
                context.ViewportState.Viewport0 = new Viewport(0, 0, RenderTexture.ViewWidth, RenderTexture.ViewHeight);

                Child?.Collect(context);
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            if (RenderTexture == null)
                return;

            using (drawContext.PushRenderTargetsAndRestore())
            {
                var depthBuffer = PushScopedResource(context.Allocator.GetTemporaryTexture2D(RenderTexture.ViewWidth, RenderTexture.ViewHeight, drawContext.CommandList.DepthStencilBuffer.ViewFormat, TextureFlags.DepthStencil));
                drawContext.CommandList.SetRenderTargetAndViewport(depthBuffer, RenderTexture);

                Child?.Draw(drawContext);
            }
        }
    }
}
