// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Graphics;

namespace Stride.Rendering
{
    /// <summary>
    /// Resolves a render target from one render pass to be used as an input resource to another render pass
    /// </summary>
    public class ResourceResolver
    {
        private readonly RenderDrawContext renderContext;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="context"><see cref="RenderDrawContext"/> to which this resolver belongs</param>
        public ResourceResolver(RenderDrawContext context)
        {
            renderContext = context;
        }

        /// <summary>
        /// Returns a texture view which should be used as DepthStencil render target while SRV is also used
        /// </summary>
        /// <param name="texture">The depthStencil texture originally used for render target</param>
        /// <param name="readOnlyCached">The cached view for the texture resource</param>
        /// <returns>The texture view which should be used as DepthStencil render target while SRV is also used</returns>
        public Texture GetDepthStencilAsRenderTarget(Texture texture, Texture readOnlyCached)
        {
            if (!renderContext.GraphicsDevice.Features.HasDepthAsSRV || !renderContext.GraphicsDevice.Features.HasDepthAsReadOnlyRT)
                return texture;

            // Check if changed
            if (readOnlyCached != null && readOnlyCached.ParentTexture == texture)
                return readOnlyCached;

            return texture.ToDepthStencilReadOnlyTexture();
        }

        /// <summary>
        /// Returns a texture view which should be used as DepthStencil Shader Resource View. Can be <c>null</c> if not supported
        /// </summary>
        /// <param name="texture">The depthStencil texture originally used for render target</param>
        /// <returns>The texture view which should be used as DepthStencil SRV. Can be <c>null</c> if not supported</returns>
        public Texture GetDepthStenctilAsShaderResource(Texture texture)
        {
            if (!renderContext.GraphicsDevice.Features.HasDepthAsSRV)
                return null;

            if (renderContext.GraphicsDevice.Features.HasDepthAsReadOnlyRT)
                return texture;

            return GetDepthStenctilAsShaderResource_Copy(texture);
        }

        /// <summary>
        /// Frees previously acquired SRV texture. Should be called when the view is no longer needed
        /// </summary>
        /// <param name="depthAsSR">The previously acquired SRV texture</param>
        public void ReleaseDepthStenctilAsShaderResource(Texture depthAsSR)
        {
            // If no resources were allocated in the first place there is nothing to release
            if (depthAsSR == null || !renderContext.GraphicsDevice.Features.HasDepthAsSRV || renderContext.GraphicsDevice.Features.HasDepthAsReadOnlyRT)
                return;

            renderContext.RenderContext.Allocator.ReleaseReference(depthAsSR);
        }
        
        /// <summary>
        /// Gets a texture view which can be used to copy the depth buffer
        /// </summary>
        /// <param name="texture">The depthStencil texture originally used for render target</param>
        /// <returns>A texture view which can be used to copy the depth buffer</returns>
        private Texture GetDepthStenctilAsShaderResource_Copy(Texture texture)
        {
            var textureDescription = texture.Description;
            textureDescription.Flags = TextureFlags.ShaderResource;
            textureDescription.Format = Texture.ComputeShaderResourceFormatFromDepthFormat(textureDescription.Format);

            return renderContext.RenderContext.Allocator.GetTemporaryTexture2D(textureDescription);
        }

        /// <summary>
        /// Resolves the depth render target so it can be used as a shader resource view. Should only be called once per frame and acquired with <see cref="GetDepthStenctilAsShaderResource"/> after that
        /// </summary>
        /// <param name="texture">The depthStencil texture originally used for render target</param>
        /// <returns>The texture view which should be used as DepthStencil render target while SRV is also used</returns>
        public Texture ResolveDepthStencil(Texture texture)
        {
            if (!renderContext.GraphicsDevice.Features.HasDepthAsSRV)
                return null;

            if (renderContext.GraphicsDevice.Features.HasDepthAsReadOnlyRT)
                return texture;

            var depthStencil = GetDepthStenctilAsShaderResource_Copy(texture);

            renderContext.CommandList.Copy(texture, depthStencil);

            return depthStencil;
        }
    }
}
