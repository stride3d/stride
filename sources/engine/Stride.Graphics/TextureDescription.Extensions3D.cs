// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

public partial struct TextureDescription
{
    /// <summary>
    ///   Creates a new <see cref="TextureDescription"/> for a three-dimensional (3D) <see cref="Texture"/> with a single mipmap.
    /// </summary>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="height">The height of the Texture in texels.</param>
    /// <param name="depth">The depth of the Texture in texels.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureFlags">
    ///   A combination of flags determining what kind of Texture and how the is should behave
    ///   (i.e. how it is bound, how can it be read / written, etc.).
    ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
    /// </param>
    /// <param name="usage">
    ///   A combination of flags determining how the Texture will be used during rendering.
    ///   The default is <see cref="GraphicsResourceUsage.Default"/>, meaning it will need read/write access by the GPU.
    /// </param>
    /// <returns>A new description for a three-dimensional Texture.</returns>
    public static TextureDescription New3D(int width, int height, int depth, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
    {
        return New3D(width, height, depth, MipMapCount.One, format, textureFlags, usage);
    }

    /// <summary>
    ///   Creates a new <see cref="TextureDescription"/> for a three-dimensional (3D) <see cref="Texture"/>.
    /// </summary>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="height">The height of the Texture in texels.</param>
    /// <param name="depth">The depth of the Texture in texels.</param>
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
    /// <param name="usage">
    ///   A combination of flags determining how the Texture will be used during rendering.
    ///   The default is <see cref="GraphicsResourceUsage.Default"/>, meaning it will need read/write access by the GPU.
    /// </param>
    /// <returns>A new description for a three-dimensional Texture.</returns>
    public static TextureDescription New3D(int width, int height, int depth, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
    {
        return New3D(width, height, depth, format, textureFlags, mipCount, usage);
    }

    private static TextureDescription New3D(int width, int height, int depth, PixelFormat format, TextureFlags flags, int mipCount, GraphicsResourceUsage usage)
    {
        var desc = new TextureDescription
        {
            Width = width,
            Height = height,
            Depth = depth,
            Flags = flags,
            Format = format,
            MipLevelCount = Texture.CalculateMipMapCount(mipCount, width, height, depth),
            Usage = Texture.GetUsageWithFlags(usage, flags),
            ArraySize = 1,
            Dimension = TextureDimension.Texture3D,
            MultisampleCount = MultisampleCount.None
        };
        return desc;
    }
}
