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
    ///   Creates a new cube-map composed of six two-dimensional (2D) <see cref="Texture"/>s.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="size">
    ///   The size in texels of the faces of the cube Texture.
    ///   As the Texture is a cube, this single value specifies both the width and the height.
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
    /// <returns>A new cube-map Texture.</returns>
    public static Texture NewCube(GraphicsDevice device, int size, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
    {
        return NewCube(device, size, MipMapCount.One, format, textureFlags, usage);
    }

    /// <summary>
    ///   Creates a new cube-map composed of six two-dimensional (2D) <see cref="Texture"/>s.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="size">
    ///   The size in texels of the faces of the cube Texture.
    ///   As the Texture is a cube, this single value specifies both the width and the height.
    /// </param>
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
    /// <returns>A new cube-map Texture.</returns>
    public static Texture NewCube(GraphicsDevice device, int size, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
    {
        var description = TextureDescription.NewCube(size, mipCount, format, textureFlags, usage);

        return new Texture(device).InitializeFrom(description);
    }

    /// <summary>
    ///   Creates a new cube-map composed of six two-dimensional (2D) <see cref="Texture"/>s from a initial data.
    /// </summary>
    /// <typeparam name="T">Type of the initial data to upload to the Texture.</typeparam>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="size">
    ///   The size in texels of the faces of the cube Texture.
    ///   As the Texture is a cube, this single value specifies both the width and the height.
    /// </param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureData">
    ///   <para>
    ///     The initial data array to upload to the Texture for a single mipmap. It is an array of arrays (two dimensions):
    ///     <list type="bullet">
    ///       <item>The first dimension of the array is the index of the cube face. <strong>There must be exactly six</strong>.</item>
    ///       <item>
    ///         The second dimension is the data for each of the cube faces. It must have a size (in bytes, not elements) equal to the size of the <paramref name="format"/> times
    ///         (<paramref name="size"/> * <paramref name="size"/>).
    ///       </item>
    ///     </list>
    ///   </para>
    ///   <para>
    ///     See <see cref="PixelFormatExtensions.SizeInBytes(PixelFormat)"/> for calculating the size of a pixel format.
    ///   </para>
    ///   <para>
    ///     Each value in the data array of each of the cube faces will be a texel in the destination Texture.
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
    /// <returns>A new cube-map Texture.</returns>
    /// <exception cref="ArgumentException">
    ///   The Texture data is invalid. The first dimension of <paramref name="textureData"/> array must be equal to 6.
    /// </exception>
    public static unsafe Texture NewCube<T>(GraphicsDevice device, int size, PixelFormat format, T[][] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : unmanaged
    {
        if (textureData.Length != 6)
            throw new ArgumentException("Invalid texture datas. First dimension must be equal to 6", nameof(textureData));

        var dataBoxes = new DataBox[6];

        for (var i = 0; i < 6; i++)
        {
            fixed (void* texture = textureData[i])
                dataBoxes[i] = GetDataBox(format, size, size, 1, textureData[0], (nint)texture);
        }

        var description = TextureDescription.NewCube(size, format, textureFlags, usage);

        return new Texture(device).InitializeFrom(description, dataBoxes);
    }

    /// <summary>
    ///   Creates a new cube-map composed of six two-dimensional (2D) <see cref="Texture"/>s from a initial data.
    /// </summary>
    /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
    /// <param name="size">
    ///   The size in texels of the faces of the cube Texture.
    ///   As the Texture is a cube, this single value specifies both the width and the height.
    /// </param>
    /// <param name="format">The format to use.</param>
    /// <param name="textureData">
    ///   An array of <see cref="DataBox"/> pointing to the initial Texture data for each of cube faces.
    ///   There must be exactly six.
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
    /// <returns>A new cube-map Texture.</returns>
    /// <exception cref="ArgumentException">
    ///   The Texture data is invalid. There must be exactly six elements in the array.
    /// </exception>
    /// <remarks>The first dimension of mipMapTextures describes the number of array (TextureCube Array), the second is the texture data for a particular cube face.</remarks>
    public static Texture NewCube(GraphicsDevice device, int size, PixelFormat format, DataBox[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
    {
        if (textureData.Length != 6)
            throw new ArgumentException("Invalid texture datas. First dimension must be equal to 6", nameof(textureData));

        var description = TextureDescription.NewCube(size, format, textureFlags, usage);

        return new Texture(device).InitializeFrom(description, textureData);
    }
}
