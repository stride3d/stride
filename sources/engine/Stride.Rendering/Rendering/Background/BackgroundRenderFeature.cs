// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Rendering.Skyboxes;
using Stride.Streaming;

namespace Stride.Rendering.Background
{
    public class BackgroundRenderFeature : RootRenderFeature
    {
        private SpriteBatch spriteBatch;
        private DynamicEffectInstance background2DEffect;
        private DynamicEffectInstance backgroundCubemapEffect;
        private DynamicEffectInstance skyboxTextureEffect;
        private DynamicEffectInstance skyboxCubemapEffect;

        public override Type SupportedRenderObjectType => typeof(RenderBackground);

        public BackgroundRenderFeature()
        {
            // Background should render after most objects (to take advantage of early z depth test)
            SortKey = 192;
        }

        public override void Prepare(RenderDrawContext context)
        {
            base.Prepare(context);

            // Register resources usage
            foreach (var renderObject in RenderObjects)
            {
                var renderBackground = (RenderBackground)renderObject;
                Context.StreamingManager?.StreamResources(renderBackground.Texture, StreamingOptions.LoadAtOnce);
            }
        }

        public override void Draw(RenderDrawContext context, RenderView renderView, RenderViewStage renderViewStage, int startIndex, int endIndex)
        {
            for (int index = startIndex; index < endIndex; index++)
            {
                var renderNodeReference = renderViewStage.SortedRenderNodes[index].RenderNode;
                var renderNode = GetRenderNode(renderNodeReference);
                var renderBackground = (RenderBackground)renderNode.RenderObject;

                if (renderBackground.Texture == null)
                    continue;
                    
                if (renderBackground.Is2D)
                {
                    Draw2D(context, renderBackground);
                }
                else
                {
                    Draw3D(context, renderView, renderBackground);
                }
            }
        }

        protected override void InitializeCore()
        {
            background2DEffect = new DynamicEffectInstance("BackgroundShader");
            backgroundCubemapEffect = new DynamicEffectInstance("BackgroundCubemapShader");
            skyboxTextureEffect = new DynamicEffectInstance("SkyboxShaderTexture");
            skyboxCubemapEffect = new DynamicEffectInstance("SkyboxShaderCubemap");

            background2DEffect.Initialize(Context.Services);
            backgroundCubemapEffect.Initialize(Context.Services);
            skyboxTextureEffect.Initialize(Context.Services);
            skyboxCubemapEffect.Initialize(Context.Services);

            spriteBatch = new SpriteBatch(RenderSystem.GraphicsDevice) { VirtualResolution = new Vector3(1) };
        }

        private void Draw2D([NotNull] RenderDrawContext context, [NotNull] RenderBackground renderBackground)
        {
            var target = context.CommandList.RenderTarget;
            var graphicsDevice = context.GraphicsDevice;
            var destination = new RectangleF(0, 0, 1, 1);

            var texture = renderBackground.Texture;
            var textureIsLoading = texture.ViewType == ViewType.Full && texture.FullQualitySize.Width != texture.ViewWidth;
            var textureSize = textureIsLoading ? texture.FullQualitySize : new Size3(texture.ViewWidth, texture.ViewHeight, texture.ViewDepth);
            var imageBufferMinRatio = Math.Min(textureSize.Width / (float)target.ViewWidth, textureSize.Height / (float)target.ViewHeight);
            var sourceSize = new Vector2(target.ViewWidth * imageBufferMinRatio, target.ViewHeight * imageBufferMinRatio);
            var source = new RectangleF((textureSize.Width - sourceSize.X) / 2, (textureSize.Height - sourceSize.Y) / 2, sourceSize.X, sourceSize.Y);
            if (textureIsLoading)
            {
                var verticalRatio = texture.ViewHeight / (float)textureSize.Height;
                var horizontalRatio = texture.ViewWidth / (float)textureSize.Width;
                source.X *= horizontalRatio;
                source.Width *= horizontalRatio;
                source.Y *= verticalRatio;
                source.Height *= verticalRatio;
            }

            // Setup the effect depending on the type of texture
            if (renderBackground.Texture.ViewDimension == TextureDimension.Texture2D)
            {
                background2DEffect.UpdateEffect(graphicsDevice);
                spriteBatch.Begin(context.GraphicsContext, SpriteSortMode.FrontToBack, BlendStates.Opaque, graphicsDevice.SamplerStates.LinearClamp, DepthStencilStates.DepthRead, null, background2DEffect);
            }
            else if (renderBackground.Texture.ViewDimension == TextureDimension.TextureCube)
            {
                backgroundCubemapEffect.UpdateEffect(graphicsDevice);
                spriteBatch.Begin(context.GraphicsContext, SpriteSortMode.FrontToBack, BlendStates.Opaque, graphicsDevice.SamplerStates.LinearClamp, DepthStencilStates.DepthRead, null, backgroundCubemapEffect);
                spriteBatch.Parameters.Set(BackgroundCubemapShaderKeys.Cubemap, renderBackground.Texture);
            }
            else
            {
                return; // not supported for the moment.
            }

            spriteBatch.Parameters.Set(BackgroundShaderKeys.Intensity, renderBackground.Intensity);
            spriteBatch.Draw(texture, destination, source, Color.White, 0, Vector2.Zero, layerDepth: -0.5f);
            spriteBatch.End();
        }

        private void Draw3D([NotNull] RenderDrawContext context, [NotNull] RenderView renderView, [NotNull] RenderBackground renderBackground)
        {
            var graphicsDevice = context.GraphicsDevice;
            var destination = new RectangleF(0, 0, 1, 1);

            var texture = renderBackground.Texture;

            // Setup the effect depending on the type of texture
            if (renderBackground.Texture.ViewDimension == TextureDimension.Texture2D)
            {
                skyboxTextureEffect.UpdateEffect(graphicsDevice);
                spriteBatch.Begin(context.GraphicsContext, SpriteSortMode.FrontToBack, BlendStates.Opaque, null, DepthStencilStates.DepthRead, null, skyboxTextureEffect);
                spriteBatch.Parameters.Set(SkyboxShaderTextureKeys.Texture, renderBackground.Texture);
            }
            else if (renderBackground.Texture.ViewDimension == TextureDimension.TextureCube)
            {
                skyboxCubemapEffect.UpdateEffect(graphicsDevice);
                spriteBatch.Begin(context.GraphicsContext, SpriteSortMode.FrontToBack, BlendStates.Opaque, null, DepthStencilStates.DepthRead, null, skyboxCubemapEffect);
                spriteBatch.Parameters.Set(SkyboxShaderCubemapKeys.CubeMap, renderBackground.Texture);
            }
            else
            {
                return; // not supported for the moment.
            }
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.Intensity, renderBackground.Intensity);
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.ViewInverse, Matrix.Invert(renderView.View));
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.ProjectionInverse, Matrix.Invert(renderView.Projection));
            spriteBatch.Parameters.Set(SkyboxShaderBaseKeys.SkyMatrix, Matrix.Invert(Matrix.RotationQuaternion(renderBackground.Rotation)));
            spriteBatch.Draw(texture, destination, null, Color.White, 0, Vector2.Zero, layerDepth: -0.5f);
            spriteBatch.End();
        }
    }
}
