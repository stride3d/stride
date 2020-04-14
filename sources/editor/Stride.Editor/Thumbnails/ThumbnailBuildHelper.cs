// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Threading;
using Stride.Core.Diagnostics;
using Stride.Core.Mathematics;
using Stride.Editor.Resources;
using Stride.Graphics;

namespace Stride.Editor.Thumbnails
{
    /// This should be used for overlaying build status icon on top of thumbnail.
    public class ThumbnailBuildHelper : IDisposable
    {
        private static readonly object thumbnailLock = new object();
        private static GraphicsDevice staticGraphicsDevice;
        private static SpriteBatch staticSpriteBatch;

        private static Texture staticRenderTarget;
        private static Texture staticRenderTargetStaging;

        private static Texture errorTexture;
        private static Texture warningTexture;

        public GraphicsDevice GraphicsDevice;
        public GraphicsContext GraphicsContext;
        public SpriteBatch SpriteBatch;
        public Texture RenderTarget;
        public Texture RenderTargetStaging;

        bool lockWasTaken;

        public ThumbnailBuildHelper()
        {
            Monitor.Enter(thumbnailLock, ref lockWasTaken);

            // Initialize device
            InitializeDevice();
        }

        public void Dispose()
        {
            GraphicsContext.ResourceGroupAllocator.Dispose();

            if (lockWasTaken)
                Monitor.Exit(thumbnailLock);
        }

        private void InitializeDevice()
        {
            // If first time, let's create graphics device and resources
            if (staticGraphicsDevice == null)
            {
                staticGraphicsDevice = GraphicsDevice.New();
                staticSpriteBatch = new SpriteBatch(staticGraphicsDevice);
            }

            GraphicsDevice = staticGraphicsDevice;
            GraphicsContext = new GraphicsContext(staticGraphicsDevice);
            SpriteBatch = staticSpriteBatch;
        }

        public void InitializeRenderTargets(PixelFormat format, int width, int height)
        {
            // Create render target of appropriate size (we expect it to always be the same, otherwise this might need some improvement)
            if (staticRenderTarget == null || (staticRenderTarget.Width != width || staticRenderTarget.Height != height || staticRenderTarget.Format != format))
            {
                if (staticRenderTarget != null)
                {
                    staticRenderTarget.Dispose();
                    staticRenderTargetStaging.Dispose();
                }

                staticRenderTarget = Texture.New2D(staticGraphicsDevice, width, height, format, TextureFlags.RenderTarget);
                staticRenderTargetStaging = staticRenderTarget.ToStaging();
            }

            RenderTarget = staticRenderTarget;
            RenderTargetStaging = staticRenderTargetStaging;
        }

        /// <summary>
        /// Applies the build status on top of a thumbnail image (using overlay icons).
        /// </summary>
        /// <param name="thumbnailImage">The thumbnail image.</param>
        /// <param name="dependencyBuildStatus">The dependency build status.</param>
        public static void ApplyThumbnailStatus(Image thumbnailImage, LogMessageType dependencyBuildStatus)
        {
            // No warning or error, nothing to do (or maybe we should display a logo for "info"?)
            if (dependencyBuildStatus < LogMessageType.Warning)
                return;

            using (var thumbnailBuilderHelper = new ThumbnailBuildHelper())
            {
                if (errorTexture == null)
                {
                    // Load status textures
                    errorTexture = TextureExtensions.FromFileData(thumbnailBuilderHelper.GraphicsDevice, DefaultThumbnails.ThumbnailDependencyError);
                    warningTexture = TextureExtensions.FromFileData(thumbnailBuilderHelper.GraphicsDevice, DefaultThumbnails.ThumbnailDependencyWarning);
                }

                var texture = dependencyBuildStatus == LogMessageType.Warning ? warningTexture : errorTexture;
                using (var thumbnailTexture = Texture.New(thumbnailBuilderHelper.GraphicsDevice, thumbnailImage))
                {
                    thumbnailBuilderHelper.CombineTextures(thumbnailTexture, texture, thumbnailImage.Description.Width - texture.Width - 4, thumbnailImage.Description.Height - texture.Height - 4);
                }

                // Read back result to image
                thumbnailBuilderHelper.RenderTarget.GetData(thumbnailBuilderHelper.GraphicsContext.CommandList, thumbnailBuilderHelper.RenderTargetStaging, new DataPointer(thumbnailImage.PixelBuffer[0].DataPointer, thumbnailImage.PixelBuffer[0].BufferStride));
                thumbnailImage.Description.Format = thumbnailBuilderHelper.RenderTarget.Format; // In case channels are swapped
            }
        }

        public void Combine(Texture texture, Image image)
        {
            lock (thumbnailLock)
            {
                using (var texture2 = Texture.New(GraphicsDevice, image))
                {
                    CombineTextures(texture, texture2, 0, 0);
                }

                // Read back result to image
                RenderTarget.GetData(GraphicsContext.CommandList, RenderTargetStaging, new DataPointer(image.PixelBuffer[0].DataPointer, image.PixelBuffer[0].BufferStride));
                image.Description.Format = RenderTarget.Format; // In case channels are swapped
            }
        }

        private void CombineTextures(Texture texture1, Texture texture2, int positionX, int positionY)
        {
            InitializeRenderTargets(PixelFormat.R8G8B8A8_UNorm, texture1.Description.Width, texture1.Description.Height);

            // Generate thumbnail with status icon
            // Clear (transparent)
            GraphicsContext.CommandList.Clear(RenderTarget, new Color4());
            GraphicsContext.CommandList.SetRenderTargetAndViewport(null, staticRenderTarget);

            // Render thumbnail and status sprite
            SpriteBatch.Begin(GraphicsContext);
            SpriteBatch.Draw(texture1, Vector2.Zero);
            SpriteBatch.Draw(texture2, new Vector2(positionX, positionY));
            SpriteBatch.End();
        }
    }
}
