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

using System;

namespace Stride.Graphics;

public partial class Texture
{
    /// <summary>
    ///   Creates a new one-dimensional (1D) <see cref="Texture"/> with a single mipmap.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="width">The width of the Texture in texels.</param>
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
    /// <returns>A new one-dimensional Texture.</returns>
    public static Texture New1D(GraphicsDevice device, int width, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
    {
        return New1D(device, width, MipMapCount.One, format, textureFlags, arraySize, usage);
    }

    /// <summary>
    ///   Creates a new one-dimensional (1D) <see cref="Texture"/>.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="width">The width of the Texture in texels.</param>
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
    /// <returns>A new one-dimensional Texture.</returns>
    public static Texture New1D(GraphicsDevice device, int width, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
    {
        var description = TextureDescription.New1D(width, mipCount, format, textureFlags, arraySize, usage);

        return New(device, description);
    }

    /// <summary>
    ///   Creates a new one-dimensional (1D) <see cref="Texture"/> with initial data and a single mipmap.
    /// </summary>
    /// <typeparam name="T">Type of the initial data to upload to the Texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureData">
    ///   <para>
    ///     The initial data array to upload to the Texture.
    ///     It must have a size (in bytes, not elements) equal to the size of the <paramref name="format"/> times the <paramref name="width"/>.
    ///   </para>
    ///   <para>
    ///     See <see cref="PixelFormatExtensions.SizeInBytes(PixelFormat)"/> for calculating the size of a pixel format.
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
    /// <returns>A new one-dimensional Texture.</returns>
    public static unsafe Texture New1D<T>(GraphicsDevice device, int width, PixelFormat format, T[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : unmanaged
    {
        fixed (T* texture = textureData)
        {
            var dataBox = GetDataBox(format, width, height: 1, depth: 1, textureData, (nint) texture);
            var description = TextureDescription.New1D(width, format, textureFlags, usage);

            return New(device, description, [dataBox]);
        }
    }

    /// <summary>
    ///   Creates a new one-dimensional (1D) <see cref="Texture"/> with initial data and a single mipmap.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="width">The width of the Texture in texels.</param>
    /// <param name="format">The format to use.</param>
    /// <param name="dataPtr">A pointer to the data to upload to the Texture.</param>
    /// <param name="textureFlags">
    ///   A combination of flags determining what kind of Texture and how the is should behave
    ///   (i.e. how it is bound, how can it be read / written, etc.).
    ///   By default, it is <see cref="TextureFlags.ShaderResource"/>.
    /// </param>
    /// <param name="usage">
    ///   A combination of flags determining how the Texture will be used during rendering.
    ///   The default is <see cref="GraphicsResourceUsage.Immutable"/>, meaning it will need read access by the GPU.
    /// </param>
    /// <returns>A new one-dimensional Texture.</returns>
    public static Texture New1D(GraphicsDevice device, int width, PixelFormat format, IntPtr dataPtr, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
    {
        var dataBox = new DataBox(dataPtr, rowPitch: 0, slicePitch: 0);
        var description = TextureDescription.New1D(width, format, textureFlags, usage);

        return New(device, description, [dataBox]);
    }
}
