// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.ComponentModel;
using Stride.Engine;
using Stride.Graphics;

namespace Stride.Rendering.Compositing
{
    public class ForceAspectRatioSceneRenderer : SceneRendererBase
    {
        public const float DefaultAspectRatio = 16.0f / 9.0f;

        public ISceneRenderer Child { get; set; }

        /// <summary>
        /// Gets or sets the aspect ratio.
        /// </summary>
        /// <value>
        /// The aspect ratio.
        /// </value>
        /// <userdoc>The aspect ratio used if Add Letterbox/Pillarbox is checked.</userdoc>
        [DefaultValue(DefaultAspectRatio)]
        public float FixedAspectRatio { get; set; } = DefaultAspectRatio;

        /// <summary>
        /// Gets or sets a value wether to edit the Viewport to force the aspect ratio and add letterboxes or pillarboxes where needed
        /// </summary>
        /// <userdoc>If checked and the viewport will be modified to fit the aspect ratio of Default Back Buffer Width and Default Back Buffer Height and letterboxes/pillarboxes might be added.</userdoc>
        public bool ForceAspectRatio { get; set; } = true;

        /// <inheritdoc/>
        protected override void CollectCore(RenderContext context)
        {
            using (context.SaveViewportAndRestore())
            {
                if (ForceAspectRatio)
                    UpdateViewport(ref context.ViewportState.Viewport0, FixedAspectRatio);

                Child?.Collect(context);
            }
        }

        /// <inheritdoc/>
        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            using (drawContext.PushRenderTargetsAndRestore())
            {
                if (ForceAspectRatio)
                {
                    var viewport = drawContext.CommandList.Viewport;
                    UpdateViewport(ref viewport, FixedAspectRatio);
                    drawContext.CommandList.SetViewport(viewport);
                }

                Child?.Draw(drawContext);
            }
        }

        private static void UpdateViewport(ref Viewport currentViewport, float fixedAspectRatio)
        {
            var currentAr = currentViewport.Width / currentViewport.Height;
            var requiredAr = fixedAspectRatio;

            // Pillarbox 
            if (currentAr > requiredAr)
            {
                var newWidth = (float)Math.Max(1.0f, Math.Round(currentViewport.Height * requiredAr));
                var adjX = (float)Math.Round(0.5f * (currentViewport.Width - newWidth));
                currentViewport = new Viewport(currentViewport.X + (int)adjX, currentViewport.Y, (int)newWidth, currentViewport.Height);
            }
            // Letterbox
            else
            {
                var newHeight = (float)Math.Max(1.0f, Math.Round(currentViewport.Width / requiredAr));
                var adjY = (float)Math.Round(0.5f * (currentViewport.Height - newHeight));
                currentViewport = new Viewport(currentViewport.X, currentViewport.Y + (int)adjY, currentViewport.Width, (int)newHeight);
            }
        }
    }
}
