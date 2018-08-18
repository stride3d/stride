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
        /// Creates a new 3D <see cref="Texture"/> with a single mipmap.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <returns>
        /// A new instance of 3D <see cref="Texture"/> class.
        /// </returns>
        public static Texture New3D(GraphicsDevice device, int width, int height, int depth, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New3D(device, width, height, depth, false, format, textureFlags, usage);
        }

        /// <summary>
        /// Creates a new 3D <see cref="Texture"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <returns>
        /// A new instance of 3D <see cref="Texture"/> class.
        /// </returns>
        public static Texture New3D(GraphicsDevice device, int width, int height, int depth, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return new Texture(device).InitializeFrom(TextureDescription.New3D(width, height, depth, mipCount, format, textureFlags, usage));
        }

        /// <summary>
        /// Creates a new 3D <see cref="Texture" /> with texture data for the firs map.
        /// </summary>
        /// <typeparam name="T">Type of the data to upload to the texture</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="textureData">The texture data, width * height * depth datas </param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <returns>A new instance of 3D <see cref="Texture" /> class.</returns>
        /// <remarks>
        /// The first dimension of mipMapTextures describes the number of is an array ot Texture3D Array
        /// </remarks>
        public static unsafe Texture New3D<T>(GraphicsDevice device, int width, int height, int depth, PixelFormat format, T[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : struct
        {
            return New3D(device, width, height, depth, 1, format, new[] { GetDataBox(format, width, height, depth, textureData, (IntPtr)Interop.Fixed(textureData)) }, textureFlags, usage);
        }

        /// <summary>
        /// Creates a new 3D <see cref="Texture"/>.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int >=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="usage">The usage.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <returns>
        /// A new instance of 3D <see cref="Texture"/> class.
        /// </returns>
        public static Texture New3D(GraphicsDevice device, int width, int height, int depth, MipMapCount mipCount, PixelFormat format, DataBox[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return new Texture(device).InitializeFrom(TextureDescription.New3D(width, height, depth, mipCount, format, textureFlags, usage), textureData);
        }
    }
}
