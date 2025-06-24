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

public readonly struct MipMapDescription(int width, int height, int depth, int rowStride, int depthStride, int widthPacked, int heightPacked)
    : IEquatable<MipMapDescription>
{
    /// <summary>
    /// Describes a mipmap.
    public readonly int Width = width;

    public readonly int Height = height;

    public readonly int WidthPacked = widthPacked;

    public readonly int HeightPacked = heightPacked;

    public readonly int Depth = depth;

    public readonly int RowStride = rowStride;

    /// </summary>
    public readonly int DepthStride = depthStride;

    public readonly int MipmapSize = depthStride * depth;


    public bool Equals(MipMapDescription other)
    {
        return Width == other.Width
            && Height == other.Height
            && WidthPacked == other.WidthPacked
            && HeightPacked == other.HeightPacked
            && Depth == other.Depth
            && RowStride == other.RowStride
            && MipmapSize == other.MipmapSize
            && DepthStride == other.DepthStride;
    }

    public override bool Equals(object obj)
    {
        if (obj is null)
            return false;

        return obj is MipMapDescription mipMapDescription && Equals(mipMapDescription);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Width, Height, WidthPacked, HeightPacked, Depth, RowStride, MipmapSize, DepthStride);
    }

    public static bool operator ==(MipMapDescription left, MipMapDescription right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MipMapDescription left, MipMapDescription right)
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MipMapDescription" /> class.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <param name="rowStride">The row stride.</param>
        /// <param name="depthStride">The depth stride.</param>
        /// <summary>
        /// Width of this mipmap.
        /// </summary>
        /// <summary>
        /// Height of this mipmap.
        /// </summary>
        /// <summary>
        /// Width of this mipmap.
        /// </summary>
        /// <summary>
        /// Height of this mipmap.
        /// </summary>
        /// <summary>
        /// Depth of this mipmap.
        /// </summary>
        /// <summary>
        /// RowStride of this mipmap (number of bytes per row).
        /// </summary>
        /// <summary>
        /// DepthStride of this mipmap (number of bytes per depth slice).
        /// </summary>
        /// <summary>
        /// Size in bytes of this whole mipmap.
        /// </summary>
        /// <summary>
        /// Implements the ==.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        /// <summary>
        /// Implements the !=.
        /// </summary>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The result of the operator.</returns>
        return !Equals(left, right);
    }
}
