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

namespace Stride.Graphics;

/// <summary>
///   A simple <see cref="GraphicsPresenter"/> wrapping a Render-Target Texture
///   where drawing will occur but no actual presentation will happen.
/// </summary>
/// <remarks>
///   This class is useful when configuring an application that will be rendering to a
///   Texture instead of the screen.
/// </remarks>
public class RenderTargetGraphicsPresenter : GraphicsPresenter
{
    private Texture backBuffer;


    /// <summary>
    ///   Initializes a new instance of the <see cref="RenderTargetGraphicsPresenter"/> class.
    /// </summary>
    /// <param name="device">The Graphics Device.</param>
    /// <param name="renderTarget">
    ///   A two-dimensional Texture that serves as a Render Target to draw into.
    /// </param>
    /// <param name="depthFormat">The format of the Depth-Stencil buffer</param>
    public RenderTargetGraphicsPresenter(GraphicsDevice device, Texture renderTarget, PixelFormat depthFormat = PixelFormat.None)
        : base(device, CreatePresentationParameters(renderTarget, depthFormat))
    {
        PresentInterval = Description.PresentationInterval;

        // Initialize the swap-chain
        SetBackBuffer(renderTarget);
    }

    private static PresentationParameters CreatePresentationParameters(Texture renderTarget2D, PixelFormat depthFormat)
    {
        return new PresentationParameters
        {
            BackBufferWidth = renderTarget2D.Width,
            BackBufferHeight = renderTarget2D.Height,
            BackBufferFormat = renderTarget2D.ViewFormat,
            DepthStencilFormat = depthFormat,
            DeviceWindowHandle = null,
            IsFullScreen = true,
            MultisampleCount = renderTarget2D.MultisampleCount,
            PresentationInterval = PresentInterval.One,
            RefreshRate = 60
        };
    }


    /// <inheritdoc/>
    public override Texture BackBuffer => backBuffer;

    /// <summary>
    ///   Sets the Back-Buffer where the frame must be rendered.
    /// </summary>
    /// <param name="backBuffer">The Render Target to use as Back-Buffer.</param>
    public void SetBackBuffer(Texture backBuffer)
    {
        this.backBuffer = backBuffer.EnsureRenderTarget();
    }

    /// <inheritdoc/>
    public override object NativePresenter => backBuffer;

    /// <inheritdoc path="/summary"/>
    /// <value>
    ///   A <see cref="RenderTargetGraphicsPresenter"/> always returns <see langword="true"/>
    ///   for this property, and this value cannot be modified.
    /// </value>
    public override bool IsFullScreen
    {
        get => true;
        set { }
    }

    /// <inheritdoc path="/summary"/>
    /// <remarks>
    ///   A <see cref="RenderTargetGraphicsPresenter"/> does nothing for this method; it wraps
    ///   an internal Render-Target Texture to draw to, and not a swap-chain buffer.
    /// </remarks>
    public override void Present()
    {
    }

    /// <inheritdoc/>
    protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
    {
        throw new NotImplementedException();
    }
}
