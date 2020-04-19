// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

using Stride.Graphics;

namespace Stride.Games
{
    /// <summary>
    ///   Describess how data will be displayed to the screen.
    /// </summary>
    public class GameGraphicsParameters
    {
        /// <summary>
        ///   A value that describes the resolution width.
        /// </summary>
        public int PreferredBackBufferWidth;

        /// <summary>
        ///   A value that describes the resolution height.
        /// </summary>
        public int PreferredBackBufferHeight;

        /// <summary>
        ///   A <strong><see cref="SharpDX.DXGI.Format" /></strong> structure describing the display format.
        /// </summary>
        public PixelFormat PreferredBackBufferFormat;

        /// <summary>
        /// Gets or sets the depth stencil format
        /// </summary>
        public PixelFormat PreferredDepthStencilFormat;

        /// <summary>
        ///   Gets or sets a value indicating whether the application is in full screen mode.
        /// </summary>
        public bool IsFullScreen;

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex;

        /// <summary>
        /// Gets or sets the minimum graphics profile.
        /// </summary>
        public GraphicsProfile[] PreferredGraphicsProfile;

        /// <summary>
        /// The preferred refresh rate
        /// </summary>
        public Rational PreferredRefreshRate;

        /// <summary>
        ///   Gets or sets a value indicating the number of sample locations during multisampling.
        /// </summary>
        public MultisampleCount PreferredMultisampleCount;

        /// <summary>
        /// Gets or sets a value indicating whether to synochrnize present with vertical blanking.
        /// </summary>
        public bool SynchronizeWithVerticalRetrace;

        /// <summary>
        /// Gets or sets the colorspace.
        /// </summary>
        public ColorSpace ColorSpace;

        /// <summary>
        /// If populated the engine will try to initialize the device with the same unique id
        /// </summary>
        public string RequiredAdapterUid;
    }
}
