// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Presentation.AssetEditors.Gizmos
{
    /// <summary>
    /// Used by <see cref="SpaceMarker"/> and <see cref="CameraOrientationGizmo"/> to draw in a sub-viewport using a custom camera.
    /// </summary>
    class GizmoViewportRenderer : SceneRendererBase
    {
        /// <summary>
        /// The inner compositor to draw inside the viewport.
        /// </summary>
        public new ISceneRenderer Content { get; set; }

        /// <summary>
        /// The camera to use.
        /// </summary>
        public CameraComponent Camera { get; set; }

        /// <summary>
        /// The render view created and used by this compositor.
        /// </summary>
        public RenderView RenderView { get; } = new RenderView();

        /// <summary>
        /// The desired viewport size.
        /// </summary>
        public int ViewportSize { get; set; }

        /// <summary>
        /// Where the viewport is located compared to its parent viewport.
        /// </summary>
        public Vector2 ViewportPosition { get; set; }

        public Viewport Viewport;
        public Vector2 OutputSize;

        protected override void CollectCore(RenderContext context)
        {
            base.CollectCore(context);

            using (context.PushTagAndRestore(CameraComponentRendererExtensions.Current, Camera))
            {
                var oldRenderView = context.RenderView;

                context.RenderSystem.Views.Add(RenderView);
                context.RenderView = RenderView;

                OutputSize = context.ViewportState.Viewport0.Size;
                Viewport = new Viewport((context.ViewportState.Viewport0.Width - ViewportSize) * ViewportPosition.X, (context.ViewportState.Viewport0.Height - ViewportSize) * ViewportPosition.Y, ViewportSize, ViewportSize);
                using (context.SaveViewportAndRestore())
                {
                    context.ViewportState = new ViewportState { Viewport0 = Viewport };
                    SceneCameraRenderer.UpdateCameraToRenderView(context, RenderView, Camera);
                    Content.Collect(context);
                }

                context.RenderView = oldRenderView;
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            using (drawContext.PushTagAndRestore(CameraComponentRendererExtensions.Current, Camera))
            using (context.PushRenderViewAndRestore(RenderView))
            {
                var oldViewport = drawContext.CommandList.Viewport;

                drawContext.CommandList.SetViewport(Viewport);
                Content.Draw(drawContext);

                drawContext.CommandList.SetViewport(oldViewport);
            }
        }
    }
}
