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
using System.Runtime.InteropServices;
using Stride.Core;

namespace Stride.Graphics
{
    /// <summary>
    /// A description for <see cref="Image"/>.
    /// </summary>
    [DataContract]
    [StructLayout(LayoutKind.Sequential)]
    public struct ImageDescription : IEquatable<ImageDescription>
    {
        /// <summary>
        /// The dimension of a texture.
        /// </summary>
        public TextureDimension Dimension;

        /// <summary>
        /// Width of the image in texels. The maximum supported texture width depends on the graphics device.
        /// </summary>
        /// <remarks>
        /// This field is valid for all textures: <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/>, <see cref="TextureDimension.Texture3D"/> and <see cref="TextureDimension.TextureCube"/>.
        /// </remarks>
        public int Width;

        /// <summary>
        /// Height of the image in texels. The maximum supported texture height depends on the graphics device.
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture2D"/>, <see cref="TextureDimension.Texture3D"/> and <see cref="TextureDimension.TextureCube"/>.
        /// </remarks>
        public int Height;

        /// <summary>
        /// Depth of the image in texels. The maximum supported texture depth depends on the graphics device.
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture3D"/>.
        /// </remarks>
        public int Depth;

        /// <summary>
        /// Number of images in the array. The maximum supported texture array size depends on the graphics device.
        /// </summary>
        /// <remarks>
        /// This field is only valid for <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/> and <see cref="TextureDimension.TextureCube"/>
        /// </remarks>
        /// <remarks>
        /// This field is only valid for textures: <see cref="TextureDimension.Texture1D"/>, <see cref="TextureDimension.Texture2D"/> and <see cref="TextureDimension.TextureCube"/>.
        /// </remarks>
        public int ArraySize;

        /// <summary>
        /// Number of mipmap levels in the image. Use 1 for a multisampled texture; or 0 to generate a full set of subtextures.
        /// See <see cref="Image.CalculateMipLevels(int, MipMapCount)"/> for mip level calculation.
        /// </summary>
        public int MipLevels;

        /// <summary>
        /// The image pixel format (see <see cref="PixelFormat"/>).
        /// </summary>
        public PixelFormat Format;

        public bool Equals(ImageDescription other)
        {
            return Dimension.Equals(other.Dimension) && Width == other.Width && Height == other.Height && Depth == other.Depth && ArraySize == other.ArraySize && MipLevels == other.MipLevels && Format.Equals(other.Format);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is ImageDescription && Equals((ImageDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Dimension.GetHashCode();
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ Depth;
                hashCode = (hashCode * 397) ^ ArraySize;
                hashCode = (hashCode * 397) ^ MipLevels;
                hashCode = (hashCode * 397) ^ Format.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(ImageDescription left, ImageDescription right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(ImageDescription left, ImageDescription right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Dimension: {0}, Width: {1}, Height: {2}, Depth: {3}, Format: {4}, ArraySize: {5}, MipLevels: {6}", Dimension, Width, Height, Depth, Format, ArraySize, MipLevels);
        }
    }
}
