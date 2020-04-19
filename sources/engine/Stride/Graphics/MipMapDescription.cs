// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

namespace Stride.Graphics
{
    /// <summary>
    /// Describes a mipmap.
    /// </summary>
    public class MipMapDescription : IEquatable<MipMapDescription>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MipMapDescription" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowStride">The row stride.</param>
        /// <param name="depthStride">The depth stride.</param>
        public MipMapDescription(int width, int height, int depth, int rowStride, int depthStride, int widthPacked, int heightPacked)
        {
            Width = width;
            Height = height;
            Depth = depth;
            RowStride = rowStride;
            DepthStride = depthStride;
            MipmapSize = depthStride * depth;
            WidthPacked = widthPacked;
            HeightPacked = heightPacked;
        }

        /// <summary>
        /// Width of this mipmap.
        /// </summary>
        public readonly int Width;

        /// <summary>
        /// Height of this mipmap.
        /// </summary>
        public readonly int Height;

        /// <summary>
        /// Width of this mipmap.
        /// </summary>
        public readonly int WidthPacked;

        /// <summary>
        /// Height of this mipmap.
        /// </summary>
        public readonly int HeightPacked;

        /// <summary>
        /// Depth of this mipmap.
        /// </summary>
        public readonly int Depth;

        /// <summary>
        /// RowStride of this mipmap (number of bytes per row).
        /// </summary>
        public readonly int RowStride;

        /// <summary>
        /// DepthStride of this mipmap (number of bytes per depth slice).
        /// </summary>
        public readonly int DepthStride;

        /// <summary>
        /// Size in bytes of this whole mipmap.
        /// </summary>
        public readonly int MipmapSize;

        public bool Equals(MipMapDescription other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return this.Width == other.Width && this.Height == other.Height && this.WidthPacked == other.WidthPacked && this.HeightPacked == other.HeightPacked && this.Depth == other.Depth && this.RowStride == other.RowStride && this.MipmapSize == other.MipmapSize && this.DepthStride == other.DepthStride;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != this.GetType())
                return false;
            return Equals((MipMapDescription)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = this.Width;
                hashCode = (hashCode * 397) ^ this.Height;
                hashCode = (hashCode * 397) ^ this.WidthPacked;
                hashCode = (hashCode * 397) ^ this.HeightPacked;
                hashCode = (hashCode * 397) ^ this.Depth;
                hashCode = (hashCode * 397) ^ this.RowStride;
                hashCode = (hashCode * 397) ^ this.MipmapSize;
                hashCode = (hashCode * 397) ^ this.DepthStride;
                return hashCode;
            }
        }

        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator ==(MipMapDescription left, MipMapDescription right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        public static bool operator !=(MipMapDescription left, MipMapDescription right)
        {
            return !Equals(left, right);
        }
    }
}
