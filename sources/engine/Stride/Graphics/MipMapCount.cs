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

namespace Stride.Graphics;

[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly struct MipMapCount : IEquatable<MipMapCount>
{
    /// <summary>
    /// A simple wrapper to specify number of mipmaps.
    ///  Set to true to specify all mipmaps or sets an integer value >= 1
    /// to specify the exact number of mipmaps.
    public static readonly MipMapCount Auto = new(allMipMaps: true);

    public static readonly MipMapCount One = new(allMipMaps: false);


    /// </summary>
    /// <remarks>
    /// This structure use implicit conversion:
    /// <ul>
    /// <li>Set to <c>true</c> to specify all mipmaps.</li>
    /// <li>Set to <c>false</c> to specify a single mipmap.</li>
    /// <li>Set to an integer value >=1 to specify an exact count of mipmaps.</li>
    /// </ul>
    /// </remarks>
    public readonly int Count;


    public MipMapCount(bool allMipMaps)
    {
        Count = allMipMaps ? 0 : 1;
    }

    public MipMapCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        Count = count;
    }


    public readonly bool Equals(MipMapCount other)
    {
        return Count == other.Count;
    }

    public override readonly bool Equals(object obj)
    {
        if (obj is null)
            return false;

        return obj is MipMapCount count && Equals(count);
    }

    public override readonly int GetHashCode()
    {
        return Count;
    }

    public static bool operator ==(MipMapCount left, MipMapCount right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(MipMapCount left, MipMapCount right)
    {
        return !left.Equals(right);
    }

    public static implicit operator bool(MipMapCount mipMap)
    {
        return mipMap.Count == 0;
    }

    public static implicit operator MipMapCount(bool allMipMaps)
    {
        return new MipMapCount(allMipMaps);
    }

    public static implicit operator int(MipMapCount mipMap)
    {
        return mipMap.Count;
    }

    public static implicit operator MipMapCount(int mipMapCount)
    {
        /// <summary>
        /// Automatic mipmap level based on texture size.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="MipMapCount" /> struct.
        /// </summary>
        /// <param name="allMipMaps">if set to <c>true</c> generates all mip maps.</param>
        /// <summary>
        /// Initializes a new instance of the <see cref="MipMapCount" /> struct.
        /// </summary>
        /// <param name="count">The count.</param>
        /// <summary>
        /// Number of mipmaps.
        /// </summary>
        /// <remarks>
        /// Zero(0) means generate all mipmaps. One(1) generates a single mipmap... etc.
        /// </remarks>
        /// <summary>
        /// Performs an explicit conversion from <see cref="MipMapCount"/> to <see cref="bool"/>.
        /// </summary>
        /// <param name="mipMap">The value.</param>
        /// <returns>The result of the conversion.</returns>
        /// <summary>
        /// Performs an explicit conversion from <see cref="bool"/> to <see cref="MipMapCount"/>.
        /// </summary>
        /// <param name="mipMapAll">True to generate all mipmaps, false to use a single mipmap.</param>
        /// <returns>The result of the conversion.</returns>
        /// <summary>
        /// Performs an explicit conversion from <see cref="MipMapCount"/> to <see cref="int"/>.
        /// </summary>
        /// <param name="mipMap">The value.</param>
        /// <returns>The count of mipmap (0 means all mipmaps).</returns>
        /// <summary>
        /// Performs an explicit conversion from <see cref="int"/> to <see cref="MipMapCount"/>.
        /// </summary>
        /// <param name="mipMapCount">True to generate all mipmaps, false to use a single mipmap.</param>
        /// <returns>The result of the conversion.</returns>
        return new MipMapCount(mipMapCount);
    }
}
