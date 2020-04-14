// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Graphics;
using Stride.Input;

namespace TouchInputs
{
    public class TouchInputsRenderer : SceneRendererBase
    {
        private SpriteBatch spriteBatch;

        private Vector2 virtualResolution = new Vector2(1920, 1080);

        public Texture Background;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            // create the SpriteBatch used to render them
            spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = new Vector3(virtualResolution, 1000) };
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            // Clear
            drawContext.CommandList.Clear(drawContext.CommandList.RenderTarget, Color.Green);
            drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            // Draw background
            spriteBatch.Begin(drawContext.GraphicsContext);
            var target = drawContext.CommandList.RenderTarget;
            var imageBufferMinRatio = Math.Min(Background.ViewWidth / (float)target.ViewWidth, Background.ViewHeight / (float)target.ViewHeight);
            var sourceSize = new Vector2(target.ViewWidth * imageBufferMinRatio, target.ViewHeight * imageBufferMinRatio);
            var source = new RectangleF((Background.ViewWidth - sourceSize.X) / 2, (Background.ViewHeight - sourceSize.Y) / 2, sourceSize.X, sourceSize.Y);
            spriteBatch.Draw(Background, new RectangleF(0, 0, virtualResolution.X, virtualResolution.Y), source, Color.White, 0, Vector2.Zero);
            spriteBatch.End();

            // Draw touch inputs
            var entity = SceneInstance.GetCurrent(context).RootScene.Entities[0]; // Note: there's only one entity in our scene
            entity.Get<TouchInputsScript>().Render(drawContext, spriteBatch);
        }
    }
}
