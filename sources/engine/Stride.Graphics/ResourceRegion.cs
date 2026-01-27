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

using System.Runtime.InteropServices;

namespace Stride.Graphics;

/// <summary>
///   Defines a 3D box with integer coordinates, represented as the coordinates of its minimum (left, top, front)
///   and maximum (right, bottom, back) corners.
/// </summary>
/// <remarks>
///   The values for <see cref="Right"/>, <see cref="Bottom"/>, and <see cref="Back"/> are each one pixel
///   past the end of the pixels that are included in the box region.
///   <para/>
///   That is, the values for <see cref="Left"/>, <see cref="Top"/>, and <see cref="Front"/> are included
///   in the box region while the values for <see cref="Right"/>, <see cref="Bottom"/>, and <see cref="Back"/>
///   are excluded from the box region.
///   <para/>
///   For example, for a box that is one pixel wide, where <c>(Right - Left) == 1</c>, the box region includes
///   the left pixel but not the right pixel.
/// </remarks>
/// <remarks>
///   Initializes a new resource region structure from its coordinates.
/// </remarks>
/// <param name="left">The X position of the left hand side of the box.</param>
/// <param name="top">The Y position of the top of the box.</param>
/// <param name="front">The Z position of the front of the box.</param>
/// <param name="right">The X position of the right hand side of the box.</param>
/// <param name="bottom">The Y position of the bottom of the box.</param>
/// <param name="back">The Z position of the back of the box.</param>
[StructLayout(LayoutKind.Sequential, Pack = 0)]
public partial struct ResourceRegion(int left, int top, int front, int right, int bottom, int back)
{
    /// <summary>
    ///   The X position of the left hand side of the box.
    /// </summary>
    public int Left = left;

    /// <summary>
    ///   The Y position of the top of the box.
    /// </summary>
    public int Top = top;

    /// <summary>
    ///   The Z position of the front of the box.
    /// </summary>
    public int Front = front;

    /// <summary>
    ///   The X position of the right hand side of the box.
    /// </summary>
    public int Right = right;

    /// <summary>
    ///   The Y position of the bottom of the box.
    /// </summary>
    public int Bottom = bottom;

    /// <summary>
    ///   The Z position of the back of the box.
    /// </summary>
    public int Back = back;


    /// <summary>
    ///   Gets the width of the box (i.e. <c><see cref="Right"/> - <see cref="Left"/></c>).
    /// </summary>
    public readonly int Width => Right - Left;

    /// <summary>
    ///   Gets the height of the box (i.e. <c><see cref="Bottom"/> - <see cref="Top"/></c>).
    /// </summary>
    public readonly int Height => Bottom - Top;

    /// <summary>
    ///   Gets the depth of the box (i.e. <c><see cref="Back"/> - <see cref="Front"/></c>).
    /// </summary>
    public readonly int Depth => Back - Front;
}
