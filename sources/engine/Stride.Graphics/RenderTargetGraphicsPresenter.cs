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

namespace Stride.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class RenderTargetGraphicsPresenter : GraphicsPresenter
    {
        private Texture backBuffer;

        public RenderTargetGraphicsPresenter(GraphicsDevice device, Texture renderTarget, PixelFormat depthFormat = PixelFormat.None)
            : base(device, CreatePresentationParameters(renderTarget, depthFormat))
        {
            PresentInterval = Description.PresentationInterval;
            // Initialize the swap chain
            SetBackBuffer(renderTarget);
        }

        private static PresentationParameters CreatePresentationParameters(Texture renderTarget2D, PixelFormat depthFormat)
        {
            return new PresentationParameters()
                {
                    BackBufferWidth = renderTarget2D.Width,
                    BackBufferHeight = renderTarget2D.Height,
                    BackBufferFormat = renderTarget2D.ViewFormat,
                    DepthStencilFormat = depthFormat,
                    DeviceWindowHandle = null,
                    IsFullScreen = true,
                    MultisampleCount = renderTarget2D.MultisampleCount,
                    PresentationInterval = PresentInterval.One,
                    RefreshRate = new Rational(60, 1),
                };
        }

        public override Texture BackBuffer
        {
            get
            {
                return backBuffer;
            }
        }

        /// <summary>
        /// Sets the back buffer.
        /// </summary>
        /// <param name="backBuffer">The back buffer.</param>
        public void SetBackBuffer(Texture backBuffer)
        {
            this.backBuffer = backBuffer.EnsureRenderTarget();
        }

        public override object NativePresenter
        {
            get
            {
                return backBuffer;
            }
        }

        public override bool IsFullScreen
        {
            get
            {
                return true;
            }

            set
            {
            }
        }

        public override void Present()
        {
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            throw new System.NotImplementedException();
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            throw new System.NotImplementedException();
        }
    }
}
