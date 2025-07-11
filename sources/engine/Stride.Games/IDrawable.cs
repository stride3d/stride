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

namespace Stride.Games
{
    /// <summary>
    ///   An interface for a drawable Game Component that is called by the <see cref="GameBase"/> class.
    /// </summary>
    public interface IDrawable
    {
        /// <summary>
        ///   Occurs when the <see cref="DrawOrder"/> property changes.
        /// </summary>
        event EventHandler<EventArgs> DrawOrderChanged;
        /// <summary>
        ///   Occurs when the <see cref="Visible"/> property changes.
        /// </summary>
        event EventHandler<EventArgs> VisibleChanged;


        /// <summary>
        ///   Starts the drawing of the Game Component.
        ///   It prepares the drawable component for rendering and determines whether a following <see cref="Draw"/>
        ///   and <see cref="EndDraw"/> call should occur.
        /// </summary>
        /// <returns>
        ///   <see langword="true"/> if <see cref="Draw"/> should be called;
        ///   <see langword="false"/> otherwise.
        /// </returns>
        bool BeginDraw();

        /// <summary>
        ///   Draws the Game Component.
        /// </summary>
        /// <param name="gameTime">The current timing information.</param>
        void Draw(GameTime gameTime);

        /// <summary>
        ///   Ends the drawing of the Game Component.
        /// </summary>
        /// <remarks>
        ///   This method must be preceeded by calls to <see cref="Draw"/> and <see cref="BeginDraw"/>.
        /// </remarks>
        /// <exception cref="Graphics.GraphicsDeviceException">
        ///   The Game Device this Game Component is using to draw itself is not in a valid state to end drawing, or it is not available.
        /// </exception>
        void EndDraw();


        /// <summary>
        ///   Gets a value indicating whether the Game Component's <see cref="Draw"/> method
        ///   should be called by <see cref="GameBase.Draw"/>.
        /// </summary>
        /// <value>
        ///   <see langword="true"/> if the Game Component is visible and should be drawn;
        ///   otherwise, <see langword="false"/>.
        /// </value>
        bool Visible { get; }

        /// <summary>
        ///   Gets the draw order relative to other Game Components.
        ///   <see cref="IDrawable"/> components with a lower value are drawn first.
        /// </summary>
        int DrawOrder { get; }
    }
}
