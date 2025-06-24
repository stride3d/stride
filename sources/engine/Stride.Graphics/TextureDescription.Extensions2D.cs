// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public partial struct TextureDescription
{
    /// <summary>
    ///   Creates a new <see cref="TextureDescription"/> for a two-dimensional (2D) <see cref="Texture"/> with a single mipmap.
    /// </summary>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="height">The height of the Texture in texels.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureFlags">
    ///   A combination of flags determining what kind of Texture and how the is should behave
    ///   (i.e. how it is bound, how can it be read / written, etc.).
    ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
    /// </param>
    /// <param name="arraySize">
    ///   The number of array slices for the Texture.
    ///   The default value is 1, indicating the Texture is not a Texture Array.
    /// </param>
    /// <param name="usage">
    ///   A combination of flags determining how the Texture will be used during rendering.
    ///   The default is <see cref="GraphicsResourceUsage.Default"/>, meaning it will need read/write access by the GPU.
    /// </param>
    /// <param name="textureOptions">
    ///   A combination of flags indicating options about the creation of the Texture, like creating it as a shared resource.
    ///   The default value is <see cref="TextureOptions.None"/>.
    /// </param>
    /// <returns>A new description for a two-dimensional Texture.</returns>
    public static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, TextureOptions textureOptions = TextureOptions.None)
    {
        return New2D(width, height, MipMapCount.One, format, textureFlags, arraySize, usage, MultisampleCount.None, textureOptions);
    }

    /// <summary>
    ///   Creates a new <see cref="TextureDescription"/> for a two-dimensional (2D) <see cref="Texture"/>.
    /// </summary>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="height">The height of the Texture in texels.</param>
    /// <param name="mipCount">
    ///   <para>
    ///     A <see cref="MipMapCount"/> structure describing the number of mipmaps for the Texture.
    ///     Specify <see cref="MipMapCount.Auto"/> to have <strong>all mipmaps</strong>, or
    ///     <see cref="MipMapCount.One"/> to indicate a <strong>single mipmap</strong>, or
    ///     any number greater than 1 for a particular mipmap count.
    ///   </para>
    ///   <para>
    ///     You can also specify a number (which will be converted implicitly) or a <see cref="bool"/>.
    ///     See <see cref="MipMapCount"/> for more information about accepted values.
    ///   </para>
    /// </param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureFlags">
    ///   A combination of flags determining what kind of Texture and how the is should behave
    ///   (i.e. how it is bound, how can it be read / written, etc.).
    ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
    /// </param>
    /// <param name="arraySize">
    ///   The number of array slices for the Texture.
    ///   The default value is 1, indicating the Texture is not a Texture Array.
    /// </param>
    /// <param name="usage">
    ///   A combination of flags determining how the Texture will be used during rendering.
    ///   The default is <see cref="GraphicsResourceUsage.Default"/>, meaning it will need read/write access by the GPU.
    /// </param>
    /// <param name="multisampleCount">
    ///   The number of samples per texel the Texture will have.
    ///   The default value is <see cref="MultisampleCount.None"/>, indicating a non-multisampled Texture.
    /// </param>
    /// <param name="textureOptions">
    ///   A combination of flags indicating options about the creation of the Texture, like creating it as a shared resource.
    ///   The default value is <see cref="TextureOptions.None"/>.
    /// </param>
    /// <returns>A new description for a two-dimensional Texture.</returns>
    public static TextureDescription New2D(int width, int height, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, MultisampleCount multisampleCount = MultisampleCount.None, TextureOptions textureOptions = TextureOptions.None)
    {
        return New2D(width, height, format, textureFlags, mipCount, arraySize, usage, multisampleCount, textureOptions);
    }

    private static TextureDescription New2D(int width, int height, PixelFormat format, TextureFlags textureFlags, int mipCount, int arraySize, GraphicsResourceUsage usage, MultisampleCount multisampleCount, TextureOptions textureOptions = TextureOptions.None)
    {
        if (textureFlags.HasFlag(TextureFlags.UnorderedAccess))
            usage = GraphicsResourceUsage.Default;

        var desc = new TextureDescription
        {
            Dimension = TextureDimension.Texture2D,
            Width = width,
            Height = height,
            Depth = 1,
            ArraySize = arraySize,
            MultisampleCount = multisampleCount,
            Flags = textureFlags,
            Format = format,
            MipLevelCount = Texture.CalculateMipMapCount(mipCount, width, height),
            Usage = Texture.GetUsageWithFlags(usage, textureFlags),
            Options = textureOptions
        };
        return desc;
    }
}
