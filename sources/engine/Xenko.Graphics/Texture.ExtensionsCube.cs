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
        /// Creates a new Cube <see cref="Texture" />.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="size">The size (in pixels) of the top-level faces of the cube texture.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">The texture flags.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of 2D <see cref="Texture" /> class.</returns>
        public static Texture NewCube(GraphicsDevice device, int size, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return NewCube(device, size, false, format, textureFlags, usage);
        }

        /// <summary>
        /// Creates a new Cube <see cref="Texture" />.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="size">The size (in pixels) of the top-level faces of the cube texture.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int &gt;=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">The texture flags.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of 2D <see cref="Texture" /> class.</returns>
        public static Texture NewCube(GraphicsDevice device, int size, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return new Texture(device).InitializeFrom(TextureDescription.NewCube(size, mipCount, format, textureFlags, usage));
        }

        /// <summary>
        /// Creates a new Cube <see cref="Texture" /> from a initial data..
        /// </summary>
        /// <typeparam name="T">Type of a pixel data</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="size">The size (in pixels) of the top-level faces of the cube texture.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureData">an array of 6 textures. See remarks</param>
        /// <param name="textureFlags">The texture flags.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of Cube <see cref="Texture" /> class.</returns>
        /// <exception cref="System.ArgumentException">Invalid texture datas. First dimension must be equal to 6;textureData</exception>
        /// <remarks>The first dimension of mipMapTextures describes the number of array (TextureCube Array), the second is the texture data for a particular cube face.</remarks>
        public static unsafe Texture NewCube<T>(GraphicsDevice device, int size, PixelFormat format, T[][] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable) where T : struct
        {
            if (textureData.Length != 6)
                throw new ArgumentException("Invalid texture datas. First dimension must be equal to 6", "textureData");

            var dataBoxes = new DataBox[6];

            dataBoxes[0] = GetDataBox(format, size, size, 1, textureData[0], (IntPtr)Interop.Fixed(textureData[0]));
            dataBoxes[1] = GetDataBox(format, size, size, 1, textureData[0], (IntPtr)Interop.Fixed(textureData[1]));
            dataBoxes[2] = GetDataBox(format, size, size, 1, textureData[0], (IntPtr)Interop.Fixed(textureData[2]));
            dataBoxes[3] = GetDataBox(format, size, size, 1, textureData[0], (IntPtr)Interop.Fixed(textureData[3]));
            dataBoxes[4] = GetDataBox(format, size, size, 1, textureData[0], (IntPtr)Interop.Fixed(textureData[4]));
            dataBoxes[5] = GetDataBox(format, size, size, 1, textureData[0], (IntPtr)Interop.Fixed(textureData[5]));

            return new Texture(device).InitializeFrom(TextureDescription.NewCube(size, format, textureFlags, usage), dataBoxes);
        }

        /// <summary>
        /// Creates a new Cube <see cref="Texture" /> from a initial data..
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="size">The size (in pixels) of the top-level faces of the cube texture.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureData">an array of 6 textures. See remarks</param>
        /// <param name="textureFlags">The texture flags.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of Cube <see cref="Texture" /> class.</returns>
        /// <exception cref="System.ArgumentException">Invalid texture datas. First dimension must be equal to 6;textureData</exception>
        /// <remarks>The first dimension of mipMapTextures describes the number of array (TextureCube Array), the second is the texture data for a particular cube face.</remarks>
        public static Texture NewCube(GraphicsDevice device, int size, PixelFormat format, DataBox[] textureData, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            if (textureData.Length != 6)
                throw new ArgumentException("Invalid texture datas. First dimension must be equal to 6", "textureData");

            return new Texture(device).InitializeFrom(TextureDescription.NewCube(size, format, textureFlags, usage), textureData);
        }
    }
}
