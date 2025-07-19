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

/// <summary>
///   Describes a mipmap image.
/// </summary>
/// <param name="width">The width of the mipmap, in texels.</param>
/// <param name="height">The height of the mipmap, in texels.</param>
/// <param name="depth">The depth of the mipmap, in texels.</param>
/// <param name="rowStride">The row stride of the mipmap, in bytes.</param>
/// <param name="depthStride">The depth stride of the mipmap, in bytes.</param>
/// <param name="widthPacked">
///   The width of the mipmap in <em>"blocks"</em>.
///   <para>
///     If the pixel format is not a <strong>block-compressed format</strong> (like BC1, BC2, etc.), this will be
///     the same as <paramref name="width"/>.
///   </para>
///   <para>
///     Otherwise, if the format is block-compressed, this parameter represents the number of blocks
///     across the width (usually ~1/4 the width).
///   </para>
/// </param>
/// <param name="heightPacked">
///   The height of the mipmap in <em>"blocks"</em>.
///   <para>
///     If the pixel format is not a <strong>block-compressed format</strong> (like BC1, BC2, etc.), this will be
///     the same as <paramref name="height"/>.
///   </para>
///   <para>
///     Otherwise, if the format is block-compressed, this parameter represents the number of blocks
///     across the height (usually ~1/4 the height).
///   </para>
/// </param>
/// <remarks>
///   Mipmaps are a sequence of precomputed textures, each of which is a progressively smaller version of the original texture.
///   These are used in 3D graphics to improve rendering performance and reduce aliasing artifacts.
///   Each of the images in the sequence is called a <strong>mipmap level</strong>. This structure describes one of those.
/// </remarks>
public readonly struct MipMapDescription(int width, int height, int depth, int rowStride, int depthStride, int widthPacked, int heightPacked)
    : IEquatable<MipMapDescription>
{
    /// <summary>
    ///   Width of the mipmap, in texels.
    /// </summary>
    public readonly int Width = width;

    /// <summary>
    ///   Height of the mipmap, in texels.
    /// </summary>
    public readonly int Height = height;

    /// <summary>
    ///   Width of the mipmap, in blocks.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If the pixel format is not a <strong>block-compressed format</strong> (like BC1, BC2, etc.), this will be
    ///     the same as <see cref="Width"/>.
    ///   </para>
    ///   <para>
    ///     Otherwise, if the format is block-compressed, this parameter represents the number of blocks
    ///     across the width (usually ~1/4 the width).
    ///   </para>
    /// </remarks>
    public readonly int WidthPacked = widthPacked;

    /// <summary>
    ///   Height of the mipmap, in blocks.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If the pixel format is not a <strong>block-compressed format</strong> (like BC1, BC2, etc.), this will be
    ///     the same as <see cref="Height"/>.
    ///   </para>
    ///   <para>
    ///     Otherwise, if the format is block-compressed, this parameter represents the number of blocks
    ///     across the height (usually ~1/4 the height).
    ///   </para>
    /// </remarks>
    public readonly int HeightPacked = heightPacked;

    /// <summary>
    ///   Depth of the mipmap, in texels.
    /// </summary>
    public readonly int Depth = depth;

    /// <summary>
    ///   Row stride of the mipmap (i.e. number of bytes per row).
    /// </summary>
    public readonly int RowStride = rowStride;

    /// <summary>
    ///   Depth stride of the mipmap (i.e. number of bytes per depth slice).
    /// </summary>
    public readonly int DepthStride = depthStride;

    /// <summary>
    ///   Size of the whole mipmap, in bytes.
    /// </summary>
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
        return !Equals(left, right);
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return FormattableString.Invariant($"{nameof(MipMapDescription)}: {Width}x{Height}x{Depth}, Size: {MipmapSize} bytes");
    }
}
