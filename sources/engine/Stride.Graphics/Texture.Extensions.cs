// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.IO;

namespace Stride.Graphics;

public static class TextureExtensions
{
    /// <summary>
    ///   Creates a Shader Resource View for a <see cref="Texture"/>.
    /// </summary>
    /// <param name="texture">The Texture to create a Shader Resource View for.</param>
    /// <param name="viewType">
    ///   One of the values of <see cref="ViewType"/> indicating which sub-resources from the Texture's mip hierarchy
    ///   and array slices (if a Texture Array) the Shader Resource View can access.
    /// </param>
    /// <param name="arraySlice">
    ///   The index of the array slice. It is zero-based, so the first index is 0.
    ///   If the Texture is not a Texture Array, specify 0.
    /// </param>
    /// <param name="mipLevel">
    ///   The index of the mip level. It is zero-based, so the first index is 0.
    ///   If the Texture has no mip-chain, specify 0.
    /// </param>
    /// <returns>A new <see cref="Texture"/> representing the Texture View bound to <paramref name="texture"/>.</returns>
    public static Texture ToTextureView(this Texture texture, ViewType viewType, int arraySlice, int mipLevel)
    {
        var viewDescription = texture.ViewDescription;
        viewDescription.Type = viewType;
        viewDescription.ArraySlice = arraySlice;
        viewDescription.MipLevel = mipLevel;
        return texture.ToTextureView(viewDescription);
    }

    /// <summary>
    ///   Creates a Shader Resource View that is read-only on a Depth-Stencil Texture.
    /// </summary>
    /// <param name="texture">The Texture to create a read-only Depth-Stencil Texture View for.</param>
    /// <returns>A new <see cref="Texture"/> representing the Texture View bound to <paramref name="texture"/>.</returns>
    public static Texture ToDepthStencilReadOnlyTexture(this Texture texture)
    {
        if (!texture.IsDepthStencil)
            throw new NotSupportedException("This Texture is not a valid Depth-Stencil Texture");

        var viewDescription = texture.ViewDescription;
        viewDescription.Flags = TextureFlags.DepthStencilReadOnly;
        return texture.ToTextureView(viewDescription);
    }

    /// <summary>
    ///   Creates a Shader Resource View on a Depth-Stencil Texture.
    /// </summary>
    /// <param name="texture">The Texture to create a Depth-Stencil Texture View for.</param>
    /// <returns>A new <see cref="Texture"/> representing the Texture View bound to <paramref name="texture"/>.</returns>
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

    /// <summary>
    ///   Verifies that a given <see cref="Texture"/> is a Render Target.
    /// </summary>
    /// <param name="texture"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public static Texture EnsureRenderTarget(this Texture texture)
    {
        if (texture is not null && !texture.IsRenderTarget)
        {
            throw new ArgumentException("The Texture must be a Render Target", nameof(texture));
        }
        return texture;
    }

    /// <summary>
    ///   Creates a <see cref="Texture"/> from image file data.
    /// </summary>
    /// <param name="graphicsDevice">The graphics device in which to create the Texture.</param>
    /// <param name="data">The image file data.</param>
    /// <returns>The created Texture.</returns>
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
