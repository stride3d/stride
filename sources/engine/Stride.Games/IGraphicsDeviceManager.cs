// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
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

using Stride.Graphics;

namespace Stride.Games;

/// <summary>
///   Defines the interface for an object that manages the Graphics Device lifecycle.
/// </summary>
public interface IGraphicsDeviceManager
{
    /// <summary>
    ///   Creates a valid Graphics Device ready to draw.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///   Thrown if the Graphics Device could not be created.
    /// </exception>
    void CreateDevice();

    /// <summary>
    ///   Called by the Game at the beginning of drawing.
    /// </summary>
    /// <returns>
    ///   <see langword="true"/> if the Graphics Device is ready to draw;
    ///   <see langword="false"/> if the Graphics Device is not ready or if the Game should skip drawing this frame.
    /// </returns>
    bool BeginDraw();

    /// <summary>
    ///   Called by the Game at the end of drawing.
    /// </summary>
    /// <param name="present">A value indicating whether the Game should present the Back-Buffer to the screen.</param>
    /// <exception cref="GraphicsException">
    ///   Could not present the Back-Buffer after drawing.
    /// </exception>
    /// <exception cref="GraphicsDeviceException">
    ///   The Graphics Device is not in a valid state to end drawing, or it is not available.
    /// </exception>
    void EndDraw(bool present);
}
