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

using Stride.Core;

namespace Stride.Graphics;

public abstract class GraphicsPresenter : ComponentBase
{
    /// <summary>
    /// This class is a frontend to <see cref="SharpDX.DXGI.SwapChain" /> and <see cref="SharpDX.DXGI.SwapChain1" />.
    /// </summary>
    /// <remarks>
    /// In order to create a new <see cref="GraphicsPresenter"/>, a <see cref="GraphicsDevice"/> should have been initialized first.
    /// </remarks>
        /// <summary>
        /// If not null the given interval will be used during a <see cref="Present"/> operation. 
        /// </summary>
        /// <remarks>
        /// This is currently only supported by the Direct3D graphics implementation.
        /// </remarks>
        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsPresenter" /> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="presentationParameters"> </param>
    internal static readonly PropertyKey<PresentInterval?> ForcedPresentInterval = new(name: nameof(ForcedPresentInterval), ownerType: typeof(GraphicsDevice));


    protected GraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
    {
        GraphicsDevice = device;
        var description = presentationParameters.Clone();

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        description.BackBufferFormat = NormalizeBackBufferFormat(description.BackBufferFormat);

        /// <summary>
        /// Gets the description of this presenter.
        /// </summary>
        Description = description;

        /// <summary>
        /// Gets the default back buffer for this presenter.
        /// </summary>
        ProcessPresentationParameters();

        // Creates a default Depth-Stencil Buffer
        CreateDepthStencilBuffer();
    }


        /// <summary>
        /// Gets the default depth stencil buffer for this presenter.
        /// </summary>

        /// <summary>
        /// Gets the underlying native presenter (can be a <see cref="SharpDX.DXGI.SwapChain"/> or <see cref="SharpDX.DXGI.SwapChain1"/> or null, depending on the platform).
        /// </summary>
        /// <value>The native presenter.</value>
        /// <summary>
        /// Gets or sets fullscreen mode for this presenter.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        /// <remarks>This method is only valid on Windows Desktop and has no effect on Windows Metro.</remarks>
        /// <summary>
        /// Gets or sets the <see cref="PresentInterval"/>. Default is to wait for one vertical blanking.
        /// </summary>
        /// <value>The present interval.</value>

        /// <summary>
        /// Presents the Backbuffer to the screen.
        /// </summary>
        /// <summary>
        /// Resizes the current presenter, by resizing the back buffer and the depth stencil buffer.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="format"></param>
    public GraphicsDevice GraphicsDevice { get; private set; }

    public PresentationParameters Description { get; private set; }

    public abstract Texture BackBuffer { get; }

    internal Texture LeftEyeBuffer { get; set; }
    internal Texture RightEyeBuffer { get; set; }

    public Texture DepthStencilBuffer { get; protected set; }

    public abstract object NativePresenter { get; }

    public abstract bool IsFullScreen { get; set; }

    public PresentInterval PresentInterval
    {
        get => Description.PresentationInterval;
        set => Description.PresentationInterval = value;
    }

    public virtual void BeginDraw(CommandList commandList)
    {
    }

    public virtual void EndDraw(CommandList commandList, bool present)
    {
    }

    public abstract void Present();

    public void Resize(int width, int height, PixelFormat format)
    {
        GraphicsDevice.Begin();

        Description.BackBufferWidth = width;
        Description.BackBufferHeight = height;
        Description.BackBufferFormat = NormalizeBackBufferFormat(format);

        ResizeBackBuffer(width, height, format);
        ResizeDepthStencilBuffer(width, height, format);

        GraphicsDevice.End();
    }

    private PixelFormat NormalizeBackBufferFormat(PixelFormat backBufferFormat)
    {
        if (GraphicsDevice.Features.HasSRgb && GraphicsDevice.ColorSpace == ColorSpace.Linear)
        {
            // If the device support sRGB and ColorSpace is linear, we use automatically a sRGB backbuffer
            return backBufferFormat.ToSRgb();
        }
        /// <summary>
        /// Called when [destroyed].
        /// </summary>
        else
        {
            // If the device does not support sRGB or the ColorSpace is Gamma, but the backbuffer format asked is sRGB, convert it to non sRGB
            return backBufferFormat.ToNonSRgb();
        }
    }

        /// <summary>
        /// Called when [recreated].
        /// </summary>
    protected abstract void ResizeBackBuffer(int width, int height, PixelFormat format);

    protected abstract void ResizeDepthStencilBuffer(int width, int height, PixelFormat format);

    protected void ReleaseCurrentDepthStencilBuffer()
    {
        DepthStencilBuffer?.RemoveDisposeBy(this);
    }

    protected override void Destroy()
    {
        OnDestroyed();
        base.Destroy();
    }

    protected internal virtual void OnDestroyed()
    {
    }

        /// <summary>
        /// Creates the depth stencil buffer.
        /// </summary>
    public virtual void OnRecreated()
    {
    }

    protected virtual void ProcessPresentationParameters()
    {
    }

    protected virtual void CreateDepthStencilBuffer()
    {
        // If no Depth-Stencil Buffer, just return
        if (Description.DepthStencilFormat == PixelFormat.None)
            return;

        // Creates the Depth-Stencil Buffer
        var flags = TextureFlags.DepthStencil;
        if (GraphicsDevice.Features.CurrentProfile >= GraphicsProfile.Level_10_0 &&
            Description.MultisampleCount == MultisampleCount.None)
        {
            flags |= TextureFlags.ShaderResource;
        }

        var depthTextureDescription = TextureDescription.New2D(Description.BackBufferWidth, Description.BackBufferHeight, Description.DepthStencilFormat, flags);
        depthTextureDescription.MultisampleCount = Description.MultisampleCount;

        var depthTexture = Texture.New(GraphicsDevice, depthTextureDescription);
        DepthStencilBuffer = depthTexture.DisposeBy(this);
    }
}
