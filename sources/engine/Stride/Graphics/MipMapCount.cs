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

/// <summary>
///   Describes the number of mipmap levels of a Texture.
/// </summary>
/// <remarks>
///   <see cref="MipMapCount"/> allows implicit conversion from several types of values:
///   <list type="bullet">
///     <item>
///       Set to <see langword="true"/> to specify <strong>all mipmaps</strong> (i.e. the whole mipchain).
///       This is equivalent to <see cref="Auto"/>.
///     </item>
///     <item>
///       Set to <see langword="false"/> to specify <strong>a single mipmap</strong>.
///       This is equivalent to <see cref="One"/>.
///     </item>
///     <item>Set to any positive non-zero integer to indicate a specific number of mipmaps.</item>
///   </list>
/// </remarks>
[StructLayout(LayoutKind.Sequential, Size = 4)]
public readonly struct MipMapCount : IEquatable<MipMapCount>
{
    /// <summary>
    ///   Automatic mipmap count based on the size of the Texture (i.e. the <strong>whole mipchain</strong>).
    /// </summary>
    public static readonly MipMapCount Auto = new(allMipMaps: true);

    /// <summary>
    ///   Just a single mipmap.
    /// </summary>
    public static readonly MipMapCount One = new(allMipMaps: false);


    /// <summary>
    ///   The number of mipmaps.
    /// </summary>
    /// <remarks>
    ///   A value of zero (0) means that <strong>all mipmaps</strong> (the whole mipchain) will be generated.
    ///   A value of one (1) means only <strong>a single mipmap</strong> is generated.
    ///   Any other number indicates the number of mipmaps to generate.
    /// </remarks>
    public readonly int Count;


    /// <summary>
    ///   Initializes a new instance of the <see cref="MipMapCount"/> struct.
    /// </summary>
    /// <param name="allMipMaps">
    ///   <see langword="true"/> to indicate that all mipmap levels should be generated;
    ///   <see langword="false"/> to indicate only a single level.
    /// </param>
    public MipMapCount(bool allMipMaps)
    {
        Count = allMipMaps ? 0 : 1;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="MipMapCount"/> struct.
    /// </summary>
    /// <param name="count">The mipmap count.</param>
    public MipMapCount(int count)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);

        Count = count;
    }


    /// <inheritdoc/>
    public readonly bool Equals(MipMapCount other)
    {
        return Count == other.Count;
    }

    /// <inheritdoc/>
    public override readonly bool Equals(object obj)
    {
        if (obj is null)
            return false;

        return obj is MipMapCount count && Equals(count);
    }

    /// <inheritdoc/>
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

    /// <summary>
    ///   Performs an implicit conversion from <see cref="MipMapCount"/> to <see cref="bool"/>.
    /// </summary>
    /// <param name="mipMap">The value to convert.</param>
    /// <returns>
    ///   <see langword="true"/> if <strong>all mipmap levels</strong> should be generated;
    ///   <see langword="false"/> <strong>a single level or more</strong> should be generated.
    /// </returns>
    public static implicit operator bool(MipMapCount mipMap)
    {
        return mipMap.Count == 0;
    }

    /// <summary>
    ///   Performs an implicit conversion from <see cref="bool"/> to <see cref="MipMapCount"/>.
    /// </summary>
    /// <param name="allMipMaps">
    ///   <see langword="true"/> to indicate that all mipmap levels should be generated;
    ///   <see langword="false"/> to indicate only a single level.
    /// </param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator MipMapCount(bool allMipMaps)
    {
        return new MipMapCount(allMipMaps);
    }

    /// <summary>
    ///   Performs an implicit conversion from <see cref="MipMapCount"/> to <see cref="int"/>.
    /// </summary>
    /// <param name="mipMap">The value to convert.</param>
    /// <returns>
    ///   The number of mipmaps. A value of zero (0) means <strong>all mipmaps</strong>.
    /// </returns>
    public static implicit operator int(MipMapCount mipMap)
    {
        return mipMap.Count;
    }

    /// <summary>
    ///   Performs an implicit conversion from <see cref="int"/> to <see cref="MipMapCount"/>.
    /// </summary>
    /// <param name="mipMapCount">
    ///   The number of mipmaps. A value of zero (0) means <strong>all mipmaps</strong>.
    /// </param>
    /// <returns>The result of the conversion.</returns>
    public static implicit operator MipMapCount(int mipMapCount)
    {
        return new MipMapCount(mipMapCount);
    }
}
