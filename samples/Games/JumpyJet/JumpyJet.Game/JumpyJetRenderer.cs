// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace JumpyJet
{
    public class JumpyJetRenderer : SceneRendererBase
    {
        // Entities' depth
        private const int Pal0Depth = 0;
        private const int Pal1Depth = 1;
        private const int Pal2Depth = 2;
        private const int Pal3Depth = 3;

        private SpriteBatch spriteBatch;

        private readonly List<BackgroundSection> backgroundParallax = new List<BackgroundSection>();


        public SpriteSheet ParallaxBackgrounds;

        /// <summary>
        /// The main render stage for opaque geometry.
        /// </summary>
        public RenderStage OpaqueRenderStage { get; set; }

        /// <summary>
        /// The transparent render stage for transparent geometry.
        /// </summary>
        public RenderStage TransparentRenderStage { get; set; }

        public void StartScrolling()
        {
            EnableAllParallaxesUpdate(true);
        }

        public void StopScrolling()
        {
            EnableAllParallaxesUpdate(false);
        }

        private void EnableAllParallaxesUpdate(bool isEnable)
        {
            foreach (var pallarax in backgroundParallax)
            {
                pallarax.IsUpdating = isEnable;
            }
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            var virtualResolution = new Vector3(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height, 20f);

            // Create Parallax Background
            backgroundParallax.Add(new BackgroundSection(ParallaxBackgrounds.Sprites[0], virtualResolution, 100 * GameGlobals.GameSpeed / 4f, Pal0Depth));
            backgroundParallax.Add(new BackgroundSection(ParallaxBackgrounds.Sprites[1], virtualResolution, 100 * GameGlobals.GameSpeed / 3f, Pal1Depth));
            backgroundParallax.Add(new BackgroundSection(ParallaxBackgrounds.Sprites[2], virtualResolution, 100 * GameGlobals.GameSpeed / 1.5f, Pal2Depth));

            // For pal3Sprite: Ground, move it downward so that its bottom edge is at the bottom screen.
            var screenHeight = virtualResolution.Y;
            var pal3Height = ParallaxBackgrounds.Sprites[3].SizeInPixels.Y;
            backgroundParallax.Add(new BackgroundSection(ParallaxBackgrounds.Sprites[3], virtualResolution, 100 * GameGlobals.GameSpeed, Pal3Depth, Vector2.UnitY * (screenHeight - pal3Height) / 2));

            // allocate the sprite batch in charge of drawing the backgrounds.
            spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = virtualResolution };
        }

        protected override void CollectCore(RenderContext context)
        {
            // Setup pixel formats for RenderStage
            using (context.SaveRenderOutputAndRestore())
            {
                // Fill RenderStage formats and register render stages to main view
                if (OpaqueRenderStage != null)
                {
                    context.RenderView.RenderStages.Add(OpaqueRenderStage);
                    OpaqueRenderStage.Output = context.RenderOutput;
                }
                if (TransparentRenderStage != null)
                {
                    context.RenderView.RenderStages.Add(TransparentRenderStage);
                    TransparentRenderStage.Output = context.RenderOutput;
                }
            }
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            var renderSystem = context.RenderSystem;

            // Clear
            drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            // Draw parallax background
            spriteBatch.Begin(drawContext.GraphicsContext);

            float elapsedTime = (float)context.Time.Elapsed.TotalSeconds;
            foreach (var pallaraxBackground in backgroundParallax)
                pallaraxBackground.DrawSprite(elapsedTime, spriteBatch);

            spriteBatch.End();

            // Draw [main view | main stage]
            if (OpaqueRenderStage != null)
                renderSystem.Draw(drawContext, context.RenderView, OpaqueRenderStage);

            // Draw [main view | transparent stage]
            if (TransparentRenderStage != null)
                renderSystem.Draw(drawContext, context.RenderView, TransparentRenderStage);
        }
    }
}
