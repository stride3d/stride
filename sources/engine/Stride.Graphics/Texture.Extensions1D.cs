// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Xenko.Core;

namespace Xenko.Graphics
{
    public partial class Texture
    {
        /// <summary>
        /// Creates a new 1D <see cref="Texture"/> with a single mipmap.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>
        /// A new instance of 1D <see cref="Texture"/> class.
        /// </returns>
        public static Texture New1D(GraphicsDevice device, int width, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New1D(device, width, false, format, textureFlags, arraySize, usage);
        }

        /// <summary>
        /// Creates a new 1D <see cref="Texture"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>
        /// A new instance of 1D <see cref="Texture"/> class.
        /// </returns>
        public static Texture New1D(GraphicsDevice device, int width, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New(device, TextureDescription.New1D(width, mipCount, format, textureFlags, arraySize, usage));
        }

        /// <summary>
        /// Creates a new 1D <see cref="Texture" /> with a single level of mipmap.
        /// </summary>
        /// <typeparam name="T">Type of the initial data to upload to the texture</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureData">Texture data. Size of must be equal to sizeof(Format) * width </param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of <see cref="Texture" /> class.</returns>
        /// <remarks>
        /// The first dimension of mipMapTextures describes the number of array (Texture Array), second dimension is the mipmap, the third is the texture data for a particular mipmap.
        /// </remarks>
        public static unsafe Texture New1D<T>(GraphicsDevice device, int width, PixelFormat format, T[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : struct
        {
            return New(device, TextureDescription.New1D(width, format, textureFlags, usage), new[] { GetDataBox(format, width, 1, 1, textureData, (IntPtr)Interop.Fixed(textureData)) });
        }

        /// <summary>
        /// Creates a new 1D <see cref="Texture" /> with a single level of mipmap.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="width">The width.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="dataPtr">Data ptr</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of 1D <see cref="Texture" /> class.</returns>
        /// <remarks>The first dimension of mipMapTextures describes the number of array (Texture Array), second dimension is the mipmap, the third is the texture data for a particular mipmap.</remarks>
        public static Texture New1D(GraphicsDevice device, int width, PixelFormat format, IntPtr dataPtr, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            return New(device, TextureDescription.New1D(width, format, textureFlags, usage), new[] { new DataBox(dataPtr, 0, 0), });
        }
    }
}
