// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;
using Stride.Rendering.Compositing;
using Stride.Graphics;
using System;

namespace CustomEffect
{
    /// <summary>
    /// The renderer in charge of drawing the custom effect.
    /// </summary>
    public class CustomEffectRenderer : SceneRendererBase
    {
        private Effect customEffect;
        private SpriteBatch spriteBatch;
        private EffectInstance customEffectInstance;
        private DelegateSceneRenderer renderer;
        private SamplerState samplerState;

        public Texture Background;
        public Texture Logo;

        protected override void InitializeCore()
        {
            base.InitializeCore();

            customEffect = EffectSystem.LoadEffect("Effect").WaitForResult();
            customEffectInstance = new EffectInstance(customEffect);

            spriteBatch = new SpriteBatch(GraphicsDevice) { VirtualResolution = new Vector3(1) };

            // set fixed parameters once
            customEffectInstance.Parameters.Set(TexturingKeys.Sampler, samplerState);
            customEffectInstance.Parameters.Set(EffectKeys.Center, new Vector2(0.5f, 0.5f));
            customEffectInstance.Parameters.Set(EffectKeys.Frequency, 40);
            customEffectInstance.Parameters.Set(EffectKeys.Spread, 0.5f);
            customEffectInstance.Parameters.Set(EffectKeys.Amplitude, 0.015f);
            customEffectInstance.Parameters.Set(EffectKeys.InvAspectRatio, GraphicsDevice.Presenter.BackBuffer.Height / (float)GraphicsDevice.Presenter.BackBuffer.Width);

            // NOTE: Linear-Wrap sampling is not available for non-square non-power-of-two textures on opengl es 2.0
            samplerState = SamplerState.New(GraphicsDevice, new SamplerStateDescription(TextureFilter.Linear, TextureAddressMode.Clamp));
        }

        protected override void DrawCore(RenderContext context, RenderDrawContext drawContext)
        {
            // Clear
            drawContext.CommandList.Clear(drawContext.CommandList.RenderTarget, Color.Green);
            drawContext.CommandList.Clear(drawContext.CommandList.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            customEffectInstance.Parameters.Set(EffectKeys.Phase, -3 * (float)context.Time.Total.TotalSeconds);

            spriteBatch.Begin(drawContext.GraphicsContext, blendState: BlendStates.NonPremultiplied, depthStencilState: DepthStencilStates.None, effect: customEffectInstance);

            // Draw background
            var target = drawContext.CommandList.RenderTarget;
            var imageBufferMinRatio = Math.Min(Background.ViewWidth / (float)target.ViewWidth, Background.ViewHeight / (float)target.ViewHeight);
            var sourceSize = new Vector2(target.ViewWidth * imageBufferMinRatio, target.ViewHeight * imageBufferMinRatio);
            var source = new RectangleF((Background.ViewWidth - sourceSize.X) / 2, (Background.ViewHeight - sourceSize.Y) / 2, sourceSize.X, sourceSize.Y);
            spriteBatch.Draw(Background, new RectangleF(0, 0, 1, 1), source, Color.White, 0, Vector2.Zero);


            spriteBatch.Draw(Logo, new RectangleF(0, 0, 1, 1), Color.White);
            spriteBatch.End();
        }
    }
}
