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

using System;
using Stride.Core;
using Stride.Core.ReferenceCounting;

namespace Stride.Graphics
{
    /// <summary>
    /// This class is a frontend to <see cref="SwapChain" /> and <see cref="SwapChain1" />.
    /// </summary>
    /// <remarks>
    /// In order to create a new <see cref="GraphicsPresenter"/>, a <see cref="GraphicsDevice"/> should have been initialized first.
    /// </remarks>
    public abstract class GraphicsPresenter : ComponentBase
    {
        private Texture depthStencilBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsPresenter" /> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="presentationParameters"> </param>
        protected GraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        {
            GraphicsDevice = device;
            var description = presentationParameters.Clone();

            // If we are creating a GraphicsPresenter with 
            if (device.Features.HasSRgb && device.ColorSpace == ColorSpace.Linear)
            {
                // If the device support SRgb and ColorSpace is linear, we use automatically a SRgb backbuffer
                if (description.BackBufferFormat == PixelFormat.R8G8B8A8_UNorm)
                {
                    description.BackBufferFormat = PixelFormat.R8G8B8A8_UNorm_SRgb;
                }
                else if (description.BackBufferFormat == PixelFormat.B8G8R8A8_UNorm)
                {
                    description.BackBufferFormat = PixelFormat.B8G8R8A8_UNorm_SRgb;
                }
            }
            else if (!device.Features.HasSRgb)
            {
                // If the device does not support SRgb, but the backbuffer format asked is SRgb, convert it to non SRgb
                if (description.BackBufferFormat == PixelFormat.R8G8B8A8_UNorm_SRgb)
                {
                    description.BackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
                }
                else if (description.BackBufferFormat == PixelFormat.B8G8R8A8_UNorm_SRgb)
                {
                    description.BackBufferFormat = PixelFormat.B8G8R8A8_UNorm;
                }
            }

            Description = description;

            ProcessPresentationParameters();

            // Creates a default DepthStencilBuffer.
            CreateDepthStencilBuffer();
        }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets the description of this presenter.
        /// </summary>
        public PresentationParameters Description { get; private set; }

        /// <summary>
        /// Gets the default back buffer for this presenter.
        /// </summary>
        public abstract Texture BackBuffer { get; }

        // Temporarily here until we can move WindowsMixedRealityGraphicsPresenter to Stride.VirtualReality (currently not possible because Stride.Games creates it)
        // This allows to keep Stride.Engine platform-independent
        internal Texture LeftEyeBuffer { get; set; }

        internal Texture RightEyeBuffer { get; set; }

        /// <summary>
        /// Gets the default depth stencil buffer for this presenter.
        /// </summary>
        public Texture DepthStencilBuffer
        {
            get
            {
                return depthStencilBuffer;
            }

            protected set
            {
                depthStencilBuffer = value;
            }
        }

        /// <summary>
        /// Gets the underlying native presenter (can be a <see cref="SharpDX.DXGI.SwapChain"/> or <see cref="SharpDX.DXGI.SwapChain1"/> or null, depending on the platform).
        /// </summary>
        /// <value>The native presenter.</value>
        public abstract object NativePresenter { get; }

        /// <summary>
        /// Gets or sets fullscreen mode for this presenter.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        /// <remarks>This method is only valid on Windows Desktop and has no effect on Windows Metro.</remarks>
        public abstract bool IsFullScreen { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PresentInterval"/>. Default is to wait for one vertical blanking.
        /// </summary>
        /// <value>The present interval.</value>
        public PresentInterval PresentInterval
        {
            get { return Description.PresentationInterval; }
            set { Description.PresentationInterval = value; }
        }

        public virtual void BeginDraw(CommandList commandList)
        {
        }

        public virtual void EndDraw(CommandList commandList, bool present)
        {
        }

        /// <summary>
        /// Presents the Backbuffer to the screen.
        /// </summary>
        public abstract void Present();

        /// <summary>
        /// Resizes the current presenter, by resizing the back buffer and the depth stencil buffer.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
        public void Resize(int width, int height, PixelFormat format)
        {
            GraphicsDevice.Begin();

            Description.BackBufferWidth = width;
            Description.BackBufferHeight = height;
            Description.BackBufferFormat = format;

            ResizeBackBuffer(width, height, format);
            ResizeDepthStencilBuffer(width, height, format);

            GraphicsDevice.End();
        }

        protected abstract void ResizeBackBuffer(int width, int height, PixelFormat format);

        protected abstract void ResizeDepthStencilBuffer(int width, int height, PixelFormat format);

        protected void ReleaseCurrentDepthStencilBuffer()
        {
            if (DepthStencilBuffer != null)
            {
                depthStencilBuffer.RemoveDisposeBy(this);
            }
        }

        protected override void Destroy()
        {
            OnDestroyed();
            base.Destroy();
        }
        
        /// <summary>
        /// Called when [destroyed].
        /// </summary>
        protected internal virtual void OnDestroyed()
        {
        }

        /// <summary>
        /// Called when [recreated].
        /// </summary>
        public virtual void OnRecreated()
        {
        }

        protected virtual void ProcessPresentationParameters()
        {
        }

        /// <summary>
        /// Creates the depth stencil buffer.
        /// </summary>
        protected virtual void CreateDepthStencilBuffer()
        {
            // If no depth stencil buffer, just return
            if (Description.DepthStencilFormat == PixelFormat.None)
                return;

            // Creates the depth stencil buffer.
            var flags = TextureFlags.DepthStencil;
            if (GraphicsDevice.Features.CurrentProfile >= GraphicsProfile.Level_10_0 && Description.MultisampleCount == MultisampleCount.None)
            {
                flags |= TextureFlags.ShaderResource;
            }

            // Create texture description
            var depthTextureDescription = TextureDescription.New2D(Description.BackBufferWidth, Description.BackBufferHeight, Description.DepthStencilFormat, flags);
            depthTextureDescription.MultisampleCount = Description.MultisampleCount;

            var depthTexture = Texture.New(GraphicsDevice, depthTextureDescription);
            DepthStencilBuffer = depthTexture.DisposeBy(this);
        }
    }
}
