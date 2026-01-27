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

namespace Stride.Graphics;

/// <summary>
///   Defines what sub-resources (mip-levels, array elements) from a <see cref="GraphicsResource"/> are
///   selected by a View.
/// </summary>
/// <remarks>
///   This selection model is taken from Nuaj by Patapom (<see href="https://wiki.nuaj.net/index.php?title=Nuaj"/>).
/// </remarks>
public enum ViewType
{
    /// <summary>
    ///   Gets a Texture View for <strong>the whole Texture</strong> and for all mips/arrays dimensions.
    /// </summary>
    /// <remarks>
    ///   Here is an example of what the resulting View will cover with a Texture Array of 3, each with 3 mip levels:
    ///   <code>
    ///          Array slice
    ///           0   1   2
    ///         ┌───┬───┬───┐
    ///       0 │ ▓ │ ▓ │ ▓ │   ■ = Selected
    ///     M   ├───┼───┼───┤   □ = Not selected
    ///     i 1 │ ▓ │ ▓ │ ▓ │
    ///     p   ├───┼───┼───┤
    ///       2 │ ▓ │ ▓ │ ▓ │
    ///         └───┴───┴───┘
    ///   </code>
    /// </remarks>
    Full = 0,

    /// <summary>
    ///   The Texture View contains <strong>a single Texture element</strong> at the specified <em>mip level</em> and <em>array index</em>.
    /// </summary>
    /// <remarks>
    ///   Here is an example of what the resulting View will cover with a Texture Array of 3, each with 3 mip levels
    ///   when specifying a mipmap level index of 1, and a array index of 1:
    ///   <code>
    ///          Array slice
    ///           0   1   2
    ///         ┌───┬───┬───┐
    ///       0 │   │   │   │   ■ = Selected
    ///     M   ├───┼───┼───┤   □ = Not selected
    ///     i 1 │   │ ▓ │   │
    ///     p   ├───┼───┼───┤
    ///       2 │   │   │   │
    ///         └───┴───┴───┘
    ///   </code>
    /// </remarks>
    Single = 1,

    /// <summary>
    ///   A band Texture View containing <strong>all the mip level Texture elements</strong> from the specified <em>mip level</em> and <em>array index</em>.
    /// </summary>
    /// <remarks>
    ///   Here is an example of what the resulting View will cover with a Texture Array of 3, each with 3 mip levels
    ///   when specifying a mipmap level index of 1, and a array index of 1:
    ///   <code>
    ///          Array slice
    ///           0   1   2
    ///         ┌───┬───┬───┐
    ///       0 │   │   │   │   ■ = Selected
    ///     M   ├───┼───┼───┤   □ = Not selected
    ///     i 1 │   │ ▓ │   │
    ///     p   ├───┼───┼───┤
    ///       2 │   │ ▓ │   │
    ///         └───┴───┴───┘
    ///   </code>
    /// </remarks>
    ArrayBand = 2,

    /// <summary>
    ///   A band Texture View containing <strong>all the array Texture elements</strong> from the specified <em>mip level</em> and <em>array index</em>.
    /// </summary>
    /// <remarks>
    ///   Here is an example of what the resulting View will cover with a Texture Array of 3, each with 3 mip levels
    ///   when specifying a mipmap level index of 1, and a array index of 1:
    ///   <code>
    ///          Array slice
    ///           0   1   2
    ///         ┌───┬───┬───┐
    ///       0 │   │   │   │   ■ = Selected
    ///     M   ├───┼───┼───┤   □ = Not selected
    ///     i 1 │   │ ▓ │ ▓ │
    ///     p   ├───┼───┼───┤
    ///       2 │   │   │   │
    ///         └───┴───┴───┘
    ///   </code>
    /// </remarks>
    MipBand = 3
}
