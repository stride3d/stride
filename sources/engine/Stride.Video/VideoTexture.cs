using System;
using System.Collections.Generic;
using Xenko.Core;
using Xenko.Graphics;
using Xenko.Rendering;

namespace Xenko.Video
{
    public class VideoTexture : IDisposable
    {
        private List<Texture> renderTargetMipMaps = new List<Texture>();

        /// <summary>
        /// Effect used to copy the <see cref="decoderOutputTexture"/> into the <see cref="renderTargetTexture"/>.
        /// </summary>
        private EffectInstance effectDecoderTextureCopy;
        /// <summary>
        /// Effect used to create mipmap inside the <see cref="renderTargetTexture"/>
        /// </summary>
        private EffectInstance effectTexture2DCopy;

        private Texture originalTargetTexture;
        private Texture renderTargetTexture;

        private SamplerState minMagLinearMipPointSampler;

        public VideoTexture(GraphicsDevice graphicsDevice, IServiceRegistry serviceRegistry, int width, int height, int maxMipMapCount)
        {
            if (width <= 0 || height <= 0)
            {
                throw new InvalidOperationException("Invalid video resolution.");
            }

            if (maxMipMapCount < 0)
            {
                throw new InvalidOperationException("A negative number of mip maps is not allowed.");
            }

            var effectSystem = serviceRegistry.GetSafeServiceAs<EffectSystem>();

            // We want to sample mip maps using point filtering (to make sure nothing bleeds between mip maps).
            minMagLinearMipPointSampler = SamplerState.New(graphicsDevice, new SamplerStateDescription(TextureFilter.MinMagLinearMipPoint, TextureAddressMode.Clamp));

            // Allocate the effect for copying decoder output texture to our normal render texture
            effectDecoderTextureCopy = new EffectInstance(effectSystem.LoadEffect("SpriteEffectExtTexture").WaitForResult());
            effectDecoderTextureCopy.Parameters.Set(SpriteEffectExtTextureKeys.Gamma, 2.2f);
            effectDecoderTextureCopy.Parameters.Set(SpriteEffectExtTextureKeys.MipLevel, 0.0f);
            effectDecoderTextureCopy.Parameters.Set(SpriteEffectExtTextureKeys.Sampler, minMagLinearMipPointSampler);
            effectDecoderTextureCopy.UpdateEffect(graphicsDevice);

            // Allocate the effect for copying regular 2d textures:
            effectTexture2DCopy = new EffectInstance(effectSystem.LoadEffect("SpriteEffectExtTextureRegular").WaitForResult());
            effectTexture2DCopy.Parameters.Set(SpriteEffectExtTextureKeys.MipLevel, 0.0f);
            effectTexture2DCopy.Parameters.Set(SpriteEffectExtTextureRegularKeys.Sampler, minMagLinearMipPointSampler);
            effectTexture2DCopy.UpdateEffect(graphicsDevice);

            // Create a mip mapped texture with the same size as our video
            // Only generate up to "MaxMipMapCount" number of mip maps
            var mipMapCount = Math.Min(Texture.CountMips(Math.Max(width, height)), maxMipMapCount + 1);
            var textureDescription = TextureDescription.New2D(width, height, mipMapCount, PixelFormat.R8G8B8A8_UNorm_SRgb, TextureFlags.ShaderResource | TextureFlags.RenderTarget, 1, GraphicsResourceUsage.Dynamic);
            renderTargetTexture = Texture.New(graphicsDevice, textureDescription, null); // Supply no data. Create an empty texture.
        }

        /// <summary>
        /// Update the current target texture the video should be decoding into.
        /// </summary>
        /// <param name="newTargetTexture"></param>
        public void UpdateTargetTexture(Texture newTargetTexture)
        {
            if (originalTargetTexture == null) // the video is not playing or first frames have not been extracted yet.
                return;

            SetTargetContentToVideoStream(newTargetTexture);
        }

        ///<summary>
        /// Swap the mip mapped video texture with the one supplied. This way all references stay intact but the contents of the texture change.
        ///</summary>
        public void SetTargetContentToVideoStream(Texture newTargetTexture)
        {
            if (originalTargetTexture == newTargetTexture) // the target content is already set to the video stream
                return;  // -> nothing to do

            if (originalTargetTexture != null) // the target Texture changed, we need to revert the previous one 
                SetTargetContentToOriginalPlaceholder();

            if (newTargetTexture == null)
                return;

            originalTargetTexture = newTargetTexture;
            newTargetTexture.Swap(renderTargetTexture);
            AllocateTextureViewsForMipMaps(newTargetTexture);
        }

        ///<summary>
        /// Reverts the content of the target texture to the original placeholder.
        ///</summary>
        public void SetTargetContentToOriginalPlaceholder()
        {
            if (originalTargetTexture == null) // already reverted.
                return;

            renderTargetTexture.Swap(originalTargetTexture);
            originalTargetTexture = null;
        }

        public void UpdateTopLevelMipmapFromData(GraphicsContext context, VideoImage image)
        {
            // "videoComponent.Target" contains the mip mapped video texture at this point.
            // We now copy the new video frame directly into the video texture's first mip level:
            DataPointer dataPointer = new DataPointer(image.Buffer, image.BufferSize);
            renderTargetMipMaps[0].SetData(context.CommandList, dataPointer, 0, 0);
        }

        public void CopyDecoderOutputToTopLevelMipmap(GraphicsContext context, Texture decoderOutputTexture)
        {
            //using (drawContext.PushRenderTargetsAndRestore()) // TODO: STABILITY: Use this instead of manually storing and restoring the render targets.
            //{
            // Fill the video Texture Target (by drawing the extracted video frame into the Texture)
            var previousDepthStencilBuffer = context.CommandList.DepthStencilBuffer;
            var previousRenderTarget = context.CommandList.RenderTarget;

            // Use the OES texture copy effect on Android:
            CopyTexture(context,
                        effectDecoderTextureCopy,
                        decoderOutputTexture, // Use the inputTexture as the input texture.
                        renderTargetMipMaps[0], // Set the highest mip map level as the render target.
                        SpriteEffectExtTextureKeys.XenkoInternal_TextureExt0,
                        SpriteEffectExtTextureKeys.MipLevel);

            // Restore the original framebuffer configuration:
            context.CommandList.SetRenderTargetAndViewport(previousDepthStencilBuffer, previousRenderTarget); // TODO: STABILITY: This wont work if we're using MRT!
            //}
        }

        public void GenerateMipMaps(GraphicsContext graphicsContext)
        {
            if (renderTargetMipMaps.Count < 2) // avoid the change of render target when there is no mipmaps to generate.
                return;

            //using (drawContext.PushRenderTargetsAndRestore()) // TODO: STABILITY: Use this instead of manually storing and restoring the render targets.
            {
                // Fill the video Texture Target (by drawing the extracted video frame into the Texture)
                var previousDepthStencilBuffer = graphicsContext.CommandList.DepthStencilBuffer;
                var previousRenderTarget = graphicsContext.CommandList.RenderTarget;

                // Generate mip maps (start from level 1 because we just generated  level 0 above):
                for (int i = 1; i < renderTargetMipMaps.Count; ++i)
                {
                    CopyTexture(graphicsContext,
                                effectTexture2DCopy,
                                renderTargetMipMaps[i - 1], // Use the parent mip map level as the input texture.
                                renderTargetMipMaps[i], // Set the child mip map level as the render target.
                                SpriteEffectExtTextureRegularKeys.TextureRegular,
                                SpriteEffectExtTextureRegularKeys.MipLevel);
                }

                // Restore the original framebuffer configuration:
                graphicsContext.CommandList.SetRenderTargetAndViewport(previousDepthStencilBuffer, previousRenderTarget); // TODO: STABILITY: This wont work if we're using MRT!
            }
        }

        public void Dispose()
        {
            DeallocateTextureViewsForMipMaps();
            renderTargetMipMaps = null;

            effectDecoderTextureCopy?.Dispose();
            effectDecoderTextureCopy = null;

            effectTexture2DCopy?.Dispose();
            effectTexture2DCopy = null;

            renderTargetTexture?.Dispose();
            renderTargetTexture = null;

            minMagLinearMipPointSampler?.Dispose();
            minMagLinearMipPointSampler = null;
        }

        private static void CopyTexture(GraphicsContext graphicsContext, EffectInstance effectInstance, Texture input, Texture output,
                                 ObjectParameterKey<Texture> inputTextureKey, ValueParameterKey<float> mipLevelKey)
        {
            // Set the "input" texture as the texture that we will copy to "output":
            effectInstance.Parameters.Set(inputTextureKey, input); // TODO: STABILITY: Supply the parent texture instead? I mean here we're using SampleLOD in the shader because texture views are basically being ignored during sampling on OpenGL/ES.

            // Set the mipmap level of the input texture we want to sample:
            effectInstance.Parameters.Set(mipLevelKey, input.MipLevel);  // TODO: STABILITY: Manually pass the mip level?

            // Set the "output" texture as the render target (the copy destination):
            graphicsContext.CommandList.SetRenderTargetAndViewport(null, output);

            // Perform the actual draw call to filter and copy the texture:
            graphicsContext.DrawQuad(effectInstance);
        }

        private void AllocateTextureViewsForMipMaps(Texture parentTexture)
        {
            // Create a texture view for every mip map of the texture that we use for displaying the video in the scene:
            DeallocateTextureViewsForMipMaps();

            for (int i = 0; i < parentTexture.MipLevels; ++i)
            {
                var renderTargetMipMapTextureViewDescription = new TextureViewDescription
                {
                    Type = ViewType.Single,
                    MipLevel = i,
                    Format = parentTexture.Format,
                    ArraySlice = 0,
                    Flags = parentTexture.Flags,
                };

                Texture renderTargetMipMapTextureView = parentTexture.ToTextureView(renderTargetMipMapTextureViewDescription);
                renderTargetMipMaps.Add(renderTargetMipMapTextureView);
            }
        }
        private void DeallocateTextureViewsForMipMaps()
        {
            if (renderTargetMipMaps == null)
                return;

            foreach (var mipmap in renderTargetMipMaps)
                mipmap.Dispose();

            renderTargetMipMaps.Clear();
        }
    }
}
