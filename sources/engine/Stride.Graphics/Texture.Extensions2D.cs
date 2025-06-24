// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Stride.Graphics;

public partial class Texture
{
    /// <summary>
    ///   Creates a new two-dimensional (2D) <see cref="Texture"/> with a single mipmap.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
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
    /// <param name="options">
    ///   A combination of flags indicating options about the creation of the Texture, like creating it as a shared resource.
    ///   The default value is <see cref="TextureOptions.None"/>.
    /// </param>
    /// <returns>A new two-dimensional Texture.</returns>
    public static Texture New2D(GraphicsDevice device, int width, int height, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, TextureOptions options = TextureOptions.None)
    {
        return New2D(device, width, height, MipMapCount.One, format, textureFlags, arraySize, usage, options);
    }

    /// <summary>
    ///   Creates a new two-dimensional (2D) <see cref="Texture"/>.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="height">The height of the Texture in texels.</param>
    /// <param name="format">The format to use.</param>
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
    /// <param name="options">
    ///   A combination of flags indicating options about the creation of the Texture, like creating it as a shared resource.
    ///   The default value is <see cref="TextureOptions.None"/>.
    /// </param>
    /// <returns>A new two-dimensional Texture.</returns>
    public static Texture New2D(GraphicsDevice device, int width, int height, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default, TextureOptions options = TextureOptions.None)
    {
        var description = TextureDescription.New2D(width, height, mipCount, format, textureFlags, arraySize, usage, MultisampleCount.None, options);

        return new Texture(device).InitializeFrom(description);
    }

    /// <summary>
    ///   Creates a new two-dimensional (2D) <see cref="Texture"/> with initial data and a single mipmap.
    /// </summary>
    /// <typeparam name="T">Type of the initial data to upload to the Texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="height">The height of the Texture in texels.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureData">
    ///   <para>
    ///     The initial data array to upload to the Texture for a single mipmap and a single array slice.
    ///     It must have a size (in bytes, not elements) equal to the size of the <paramref name="format"/> times
    ///     (<paramref name="width"/> * <paramref name="height"/>).
    ///   </para>
    ///   <para>
    ///     See <see cref="PixelFormatExtensions.SizeInBytes(PixelFormat)"/> for calculating the size of a pixel format.
    ///   </para>
    ///   <para>
    ///     Each value in the data array will be a texel in the destination Texture.
    ///   </para>
    /// </param>
    /// <param name="textureFlags">
    ///   A combination of flags determining what kind of Texture and how the is should behave
    ///   (i.e. how it is bound, how can it be read / written, etc.).
    ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
    /// </param>
    /// <param name="usage">
    ///   A combination of flags determining how the Texture will be used during rendering.
    ///   The default is <see cref="GraphicsResourceUsage.Immutable"/>, meaning it will need read access by the GPU.
    /// </param>
    /// <param name="options">
    ///   A combination of flags indicating options about the creation of the Texture, like creating it as a shared resource.
    ///   The default value is <see cref="TextureOptions.None"/>.
    /// </param>
    /// <returns>A new two-dimensional Texture.</returns>
    public static unsafe Texture New2D<T>(GraphicsDevice device, int width, int height, PixelFormat format, T[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable, TextureOptions options = TextureOptions.None) where T : unmanaged
    {
        fixed (T* texture = textureData)
        {
            var dataBox = GetDataBox(format, width, height, depth: 1, textureData, (nint) texture);
            var description = TextureDescription.New1D(width, format, textureFlags, usage);

            return New2D(device, width, height, MipMapCount.One, format, [dataBox], textureFlags, arraySize: 1, usage, MultisampleCount.None, options);
        }
    }

    /// <summary>
    ///   Creates a new two-dimensional (2D) <see cref="Texture"/> with initial data.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
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
    /// <param name="textureData">
    ///   An array of <see cref="DataBox"/> pointing to the initial Texture data for each of the mipmaps.
    /// </param>
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
    /// <param name="options">
    ///   A combination of flags indicating options about the creation of the Texture, like creating it as a shared resource.
    ///   The default value is <see cref="TextureOptions.None"/>.
    /// </param>
    /// <returns>A new two-dimensional Texture.</returns>
    public static Texture New2D(
        GraphicsDevice device,
        int width,
        int height,
        MipMapCount mipCount,
        PixelFormat format,
        DataBox[] textureData,
        TextureFlags textureFlags = TextureFlags.ShaderResource,
        int arraySize = 1,
        GraphicsResourceUsage usage = GraphicsResourceUsage.Default,
        MultisampleCount multisampleCount = MultisampleCount.None,
        TextureOptions options = TextureOptions.None)
    {
        var description = TextureDescription.New2D(width, height, mipCount, format, textureFlags, arraySize, usage, multisampleCount, options);

        return new Texture(device).InitializeFrom(description, textureData);
    }
}
