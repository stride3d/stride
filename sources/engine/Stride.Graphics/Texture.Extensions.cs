// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

namespace Stride.Graphics;

public static class TextureExtensions
{
    public static Texture ToTextureView(this Texture texture, ViewType viewType, int arraySlice, int mipLevel)
    {
        var viewDescription = texture.ViewDescription;
        viewDescription.Type = viewType;
        viewDescription.ArraySlice = arraySlice;
        viewDescription.MipLevel = mipLevel;
        return texture.ToTextureView(viewDescription);
    }

        /// <summary>
        /// Gets a view on this depth stencil texture as a readonly depth stencil texture.
        /// </summary>
        /// <returns>A new texture object that is bouded to the requested view.</returns>
    public static Texture ToDepthStencilReadOnlyTexture(this Texture texture)
    {
        if (!texture.IsDepthStencil)
            throw new NotSupportedException("This Texture is not a valid Depth-Stencil Texture");

        var viewDescription = texture.ViewDescription;
        viewDescription.Flags = TextureFlags.DepthStencilReadOnly;
        return texture.ToTextureView(viewDescription);
    }

        /// <summary>
        /// Creates a new texture that can be used as a ShaderResource from an existing depth texture.
        /// </summary>
        /// <returns></returns>
    public static Texture CreateDepthTextureCompatible(this Texture texture)
    {
        if (!texture.IsDepthStencil)
            throw new NotSupportedException("This Texture is not a valid Depth-Stencil Texture");

        var description = texture.Description;
        description.Format = Texture.ComputeShaderResourceFormatFromDepthFormat(description.Format); // TODO: review this
        if (description.Format == PixelFormat.None)
            throw new NotSupportedException("This Depth-Stencil format is not supported");

        description.Flags = TextureFlags.ShaderResource;
        return Texture.New(texture.GraphicsDevice, description);
    }

    public static Texture EnsureRenderTarget(this Texture texture)
    {
        if (texture is not null && !texture.IsRenderTarget)
        {
            throw new ArgumentException("The Texture must be a Render Target", nameof(texture));
        }
        return texture;
    }

        /// <summary>
        /// Creates a texture from an image file data (png, dds, ...).
        /// </summary>
        /// <param name="graphicsDevice">The graphics device in which to create the texture</param>
        /// <param name="data">The image file data</param>
        /// <returns>The texture</returns>
    public static Texture FromFileData(GraphicsDevice graphicsDevice, byte[] data)
    {
        Texture result;

        var loadAsSRgb = graphicsDevice.ColorSpace == ColorSpace.Linear;

        using (var imageStream = new MemoryStream(data))
        {
            using var image = Image.Load(imageStream, loadAsSRgb);
            result = Texture.New(graphicsDevice, image);
        }

        result.Reload = (graphicsResource, services) =>
        {
            using var imageStream = new MemoryStream(data);
            using var image = Image.Load(imageStream, loadAsSRgb);

            ((Texture)graphicsResource).Recreate(image.ToDataBox());
        };

        return result;
    }
}
