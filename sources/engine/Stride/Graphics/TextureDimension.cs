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

using Stride.Core;

namespace Stride.Graphics;

// TODO: Should not this be in Stride.Graphics?

/// <summary>
///   Identifies the type of Texture resource being used.
/// </summary>
[DataContract]
public enum TextureDimension
{
    /// <summary>
    ///   The Texture is a one-dimensional (1D) texture.
    /// </summary>
    Texture1D,

    /// <summary>
    ///   The Texture is a two-dimensional (2D) texture.
    /// </summary>
    Texture2D,

    /// <summary>
    ///   The Texture is a three-dimensional (3D) texture (also known as a Volume Texture).
    /// </summary>
    Texture3D,

    /// <summary>
    ///   The Texture is a Cube Map, six two-dimensional images forming a cube, each with their own mip-chain.
    /// </summary>
    TextureCube
}
