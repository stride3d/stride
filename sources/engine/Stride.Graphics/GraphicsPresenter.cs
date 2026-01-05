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
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Graphics;

/// <summary>
///   Represents an abstraction over a <strong>Swap-Chain</strong>.
/// </summary>
/// <remarks>
///   <para>
///     A <em>Swap-Chain</em> is a collection of same-sized buffers (one <strong>front-buffer</strong>,
///     one or more -usually one- <strong>Back-Buffer</strong>, and an optional <strong>Depth-Stencil Buffer</strong>)
///     that are used to present the final rendered image to the screen.
///   </para>
///   <para>
///     In order to create a new <see cref="GraphicsPresenter"/>, a <see cref="Graphics.GraphicsDevice"/>
///     should have been initialized first.
///   </para>
/// </remarks>
/// <seealso cref="SwapChainGraphicsPresenter"/>
/// <seealso cref="RenderTargetGraphicsPresenter"/>
public abstract class GraphicsPresenter : ComponentBase
{
    /// <summary>
    ///   A tag property that allows to override the <see cref="PresentInterval"/> property's value
    ///   with a forced one.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     If the value of this tag property is not <see langword="null"/> the given interval will be
    ///     used during a <see cref="Present"/> operation.
    ///   </para>
    ///   <para>
    ///     This is currently only supported by the Direct3D graphics implementation.
    ///   </para>
    /// </remarks>
    internal static readonly PropertyKey<PresentInterval?> ForcedPresentInterval = new(name: nameof(ForcedPresentInterval), ownerType: typeof(GraphicsDevice));


    /// <summary>
    ///   Initializes a new instance of the <see cref="GraphicsPresenter"/> class.
    /// </summary>
    /// <param name="device">The Graphics Device.</param>
    /// <param name="presentationParameters">
    ///   The parameters describing the buffers the <paramref name="device"/> will present to.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="device"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="presentationParameters"/> is <see langword="null"/>.</exception>
    /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
    protected GraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
    {
        ArgumentNullException.ThrowIfNull(device);
        ArgumentNullException.ThrowIfNull(presentationParameters);

        GraphicsDevice = device;
        var description = presentationParameters.Clone();

        description.BackBufferFormat = NormalizeBackBufferFormat(description.BackBufferFormat);

        Description = description;

        ProcessPresentationParameters();

        // Creates a default Depth-Stencil Buffer
        CreateDepthStencilBuffer();
    }


    /// <summary>
    ///   Gets the Graphics Device the Graphics Presenter is associated to.
    /// </summary>
    /// <value>
    ///   The Graphics Device that will be used for managing the Buffers and Textures, and starting/ending a frame.
    /// </value>
    public GraphicsDevice GraphicsDevice { get; }

    /// <summary>
    ///   Gets the parameters describing the Resources and behavior of the Graphics Presenter.
    /// </summary>
    public PresentationParameters Description { get; }

    /// <summary>
    ///   Gets the default Back-Buffer for the Graphics Presenter.
    /// </summary>
    /// <value>
    ///   The <see cref="Texture"/> where rendering will happen, which will then be presented to the front-buffer.
    /// </value>
    public abstract Texture BackBuffer { get; }

    // TODO: Temporarily here until we can move WindowsMixedRealityGraphicsPresenter to Stride.VirtualReality (currently not possible because Stride.Games creates it)
    // This allows to keep Stride.Engine platform-independent
    internal Texture LeftEyeBuffer { get; set; }
    internal Texture RightEyeBuffer { get; set; }

    /// <summary>
    ///   Gets the default Depth-Stencil Buffer for the Graphics Presenter.
    /// </summary>
    /// <value>
    ///   The <see cref="Texture"/> where depth (Z) values will be written, and optionally also
    ///   stencil masking values.
    /// </value>
    /// <remarks>
    ///   If the Depth-Stencil Buffer is the one created by this Graphics Presenter, it is attached to it its lifetime
    ///   is managed by this instance.
    ///   If a derived class needs to set a custom Depth-Stencil Buffer, it should first call
    ///   <see cref="DetachDepthStencilBuffer"/> to release the current one.
    /// </remarks>
    public Texture DepthStencilBuffer { get; protected set; }

    /// <summary>
    ///   Gets the underlying native presenter.
    /// </summary>
    /// <value>
    ///   The native presenter. Depending on platform, for exmaple, it can be a <see cref="Silk.NET.DXGI.IDXGISwapChain"/>
    ///   or <see cref="Silk.NET.DXGI.IDXGISwapChain1"/> or <see langword="null"/>.
    /// </value>
    public abstract object NativePresenter { get; }

    /// <summary>
    ///   Gets or sets a value indicating if the Graphics Presenter is in full-screen mode.
    /// </summary>
    /// <value>
    ///   A value indicating whether the presentation will be in full screen.
    ///   <list type="bullet">
    ///     <item><see langword="true"/> if the presentation will be in full screen.</item>
    ///     <item><see langword="false"/> if the presentation will be in a window.</item>
    ///   </list>
    /// </value>
    /// <remarks>This property is only valid on desktop Windows. It has no effect on UWP.</remarks>
    public abstract bool IsFullScreen { get; set; }

    /// <summary>
    ///   Gets or sets the presentation interval of the Graphics Presenter.
    /// </summary>
    /// <value>
    ///   A value of <see cref="Graphics.PresentInterval"/> indicating how often the display should be updated.
    ///   The default value is <see cref="PresentInterval.Default"/>, which is to wait for one vertical blanking.
    /// </value>
    public PresentInterval PresentInterval
    {
        get => Description.PresentationInterval;
        set => Description.PresentationInterval = value;
    }

    /// <summary>
    ///   Marks the beginning of a frame that will be presented later by the Graphics Presenter.
    /// </summary>
    /// <param name="commandList">The Command List where rendering commands will be registered.</param>
    /// <remarks>
    ///   When overriden in a derived class, this method should prepare the Graphics Presenter to receive
    ///   graphics commands to be executed at the beginning of the current frame.
    /// </remarks>
    public virtual void BeginDraw(CommandList commandList)
    {
    }

    /// <summary>
    ///   Marks the end of a frame that will be presented later by the Graphics Presenter.
    /// </summary>
    /// <param name="commandList">The Command List where rendering commands will be registered.</param>
    /// <param name="present">
    ///   A value indicating whether the frame will be presented, i.e. if the Back-Buffer will be shown to the screen.
    /// </param>
    /// <remarks>
    ///   When overriden in a derived class, this method should prepare the Graphics Presenter to receive
    ///   graphics commands to be executed at the end of the current frame.
    /// </remarks>
    public virtual void EndDraw(CommandList commandList, bool present)
    {
    }

    /// <summary>
    ///   Presents the Back-Buffer to the screen.
    /// </summary>
    /// <exception cref="GraphicsDeviceException">
    ///   An unexpected error occurred while presenting. Check the status of the Graphics Device
    ///   for more information (<see cref="GraphicsDeviceException.Status"/>).
    /// </exception>
    public abstract void Present();

    /// <summary>
    ///   Resizes the Back-Buffer and the Depth-Stencil Buffer.
    /// </summary>
    /// <param name="width">The new width of the buffers of the Graphics Presenter, in pixels.</param>
    /// <param name="height">The new height of the buffers of the Graphics Presenter, in pixels.</param>
    /// <param name="format">
    ///   The new preferred pixel format for the Back-Buffer. The specified format may be overriden
    ///   depending on Graphics Device features and configuration (for example, to use sRGB when appropriate).
    /// </param>
    /// <exception cref="System.NotSupportedException">
    ///   The specified pixel <paramref name="format"/> or size is not supported by the Graphics Device.
    /// </exception>
    public void Resize(int width, int height, PixelFormat format)
    {
        format = NormalizeBackBufferFormat(format);
        if (width == Description.BackBufferWidth
            && height == Description.BackBufferHeight
            && format == Description.BackBufferFormat)
            return;

        GraphicsDevice.Begin();

        Description.BackBufferWidth = width;
        Description.BackBufferHeight = height;
        Description.BackBufferFormat = format;

        ResizeBackBuffer(width, height, format);
        ResizeDepthStencilBuffer(width, height, DepthStencilBuffer.ViewFormat);

        GraphicsDevice.End();
    }

    /// <summary>
    ///   Sets the output color space of the Graphics Presenter and the pixel format to use for the Back-Buffer.
    /// </summary>
    /// <param name="colorSpace">The output color space the Graphics Presenter should use.</param>
    /// <param name="format">The pixel format to use for the Back-Buffer.</param>
    /// <remarks>
    ///   <para>
    ///     The output color space can be used to render to HDR monitors.
    ///   </para>
    ///   <para>
    ///     Use the following combinations:
    ///     <list type="table">
    ///       <item>
    ///         <term>For rendering to a SDR display with gamma 2.2</term>
    ///         <description>
    ///           Set a color space of <see cref="ColorSpaceType.RgbFullG22NoneP709"/> with a Back-Buffer format <see cref="PixelFormat.R8G8B8A8_UNorm"/>,
    ///           <see cref="PixelFormat.R8G8B8A8_UNorm_SRgb"/>, <see cref="PixelFormat.B8G8R8A8_UNorm"/>, or <see cref="PixelFormat.B8G8R8A8_UNorm"/>.
    ///         </description>
    ///       </item>
    ///       <item>
    ///         <term>For rendering to a HDR display in scRGB (standard linear), and letting the Windows DWM do the color conversion</term>
    ///         <description>
    ///           Set a color space of <see cref="ColorSpaceType.RgbFullG10NoneP709"/> with a Back-Buffer format <see cref="PixelFormat.R16G16B16A16_Float"/>.
    ///         </description>
    ///       </item>
    ///       <item>
    ///         <term>
    ///           For rendering to a HDR display in HDR10 / BT.2100, with no color conversion by the Windows DWM, rendering needs to happen in the
    ///           same color space as the display.
    ///         </term>
    ///         <description>
    ///           Set a color space of <see cref="ColorSpaceType.RgbFullG2084NoneP2020"/> with a Back-Buffer format <see cref="PixelFormat.R10G10B10A2_UNorm"/>.
    ///         </description>
    ///       </item>
    ///     </list>
    ///   </para>
    ///   <para>
    ///     Note that this is currently only supported in Stride when using the Direct3D Graphics API.
    ///     For more information about High Dynamic Range (HDR) rendering, see
    ///     <see href="https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range"/>.
    ///   </para>
    /// </remarks>
    /// <exception cref="System.NotSupportedException">
    ///   The specified pixel <paramref name="format"/> or size is not supported by the Graphics Device.
    /// </exception>
    public void SetOutputColorSpace(ColorSpaceType colorSpace, PixelFormat format)
    {
        format = NormalizeBackBufferFormat(format);

        GraphicsDevice.Begin();

        Description.BackBufferFormat = format;

        ResizeBackBuffer(Description.BackBufferWidth, Description.BackBufferHeight, format);
        ResizeDepthStencilBuffer(Description.BackBufferWidth, Description.BackBufferHeight, DepthStencilBuffer.ViewFormat);

        // We need to recreate the Swap Chain
        OnDestroyed();
        Description.OutputColorSpace = colorSpace;
        OnRecreated();

        GraphicsDevice.End();
    }

    /// <summary>
    ///   Normalizes the Back-Buffer format to take into account the color space and sRGB format.
    /// </summary>
    /// <param name="backBufferFormat">The current Back-Buffer format.</param>
    /// <returns>The normalized pixel format.</returns>
    private PixelFormat NormalizeBackBufferFormat(PixelFormat backBufferFormat)
    {
        if (GraphicsDevice.Features.HasSRgb && GraphicsDevice.ColorSpace == ColorSpace.Linear)
        {
            // If the device support sRGB and ColorSpace is linear, we use automatically a sRGB backbuffer
            return backBufferFormat.ToSRgb();
        }
        else
        {
            // If the device does not support sRGB or the ColorSpace is Gamma, but the backbuffer format asked is sRGB, convert it to non sRGB
            return backBufferFormat.ToNonSRgb();
        }
    }

    /// <summary>
    ///   Calls <see cref="Texture.OnDestroyed"/> for all children of the specified Texture.
    /// </summary>
    /// <param name="parentTexture">The parent Texture whose children are to be destroyed.</param>
    /// <returns>A list of the children Textures which were destroyed.</returns>
    protected List<Texture> DestroyChildrenTextures(Texture parentTexture)
    {
        var childrenTextures = new List<Texture>();
        var resources = GraphicsDevice.Resources;

        lock (resources)
        {
            foreach (var resource in resources)
            {
                if (resource is Texture texture && texture.ParentTexture == parentTexture)
                {
                    texture.OnDestroyed(immediately: true);
                    childrenTextures.Add(texture);
                }
            }
        }

        return childrenTextures;
    }

    /// <summary>
    ///   Resizes the Back-Buffer.
    /// </summary>
    /// <param name="width">The new width of the Back-Buffer, in pixels.</param>
    /// <param name="height">The new height of the Back-Buffer, in pixels.</param>
    /// <param name="format">The new pixel format for the Back-Buffer.</param>
    /// <exception cref="NotSupportedException">
    ///   The specified pixel <paramref name="format"/> or size is not supported by the Graphics Device.
    /// </exception>
    /// <remarks>
    ///   When implementing this method, the derived class should resize the Back-Buffer to the specified
    ///   size and format.
    /// </remarks>
    protected abstract void ResizeBackBuffer(int width, int height, PixelFormat format);

    /// <summary>
    ///   Resizes the Depth-Stencil Buffer.
    /// </summary>
    /// <param name="width">The new width of the Depth-Stencil Buffer, in pixels.</param>
    /// <param name="height">The new height of the Depth-Stencil Buffer, in pixels.</param>
    /// <param name="format">The new pixel format for the Depth-Stencil Buffer.</param>
    /// <exception cref="NotSupportedException">
    ///   The specified depth <paramref name="format"/> or size is not supported by the Graphics Device.
    /// </exception>
    /// <remarks>
    ///   When implementing this method, the derived class should resize the Depth-Stencil Buffer to the specified
    ///   size and format.
    /// </remarks>
    protected abstract void ResizeDepthStencilBuffer(int width, int height, PixelFormat format);

    /// <summary>
    ///   Detaches the current Depth-Stencil Buffer from the Graphics Presenter.
    /// </summary>
    /// <remarks>
    ///   If <see cref="DepthStencilBuffer"/> is the Depth-Stencil Buffer created by this Graphics Presenter,
    ///   it is attached to it its lifetime is managed by this instance.
    ///   If a derived class needs to set a custom Depth-Stencil Buffer, it should first call this method
    ///   to detach the current one.
    /// </remarks>
    protected void DetachDepthStencilBuffer()
    {
        DepthStencilBuffer?.RemoveDisposeBy(this);
    }

    /// <inheritdoc/>
    protected override void Destroy()
    {
        OnDestroyed();
        base.Destroy();
    }

    /// <summary>
    ///   Called when the Graphics Presenter has been destroyed.
    /// </summary>
    /// <param name="immediately">
    ///   A value indicating whether the resources used by the Graphics Presenter should be
    ///   destroyed immediately (<see langword="true"/>), or if it can be deferred until
    ///   it's safe to do so (<see langword="false"/>).
    /// </param>
    /// <remarks>
    ///   When overriden in a derived class, this method allows to perform additional cleanup
    ///   and release of associated resources.
    /// </remarks>
    protected internal virtual void OnDestroyed(bool immediately = false)
    {
    }

    /// <summary>
    ///   Called when the Graphics Presenter has been reinitialized.
    /// </summary>
    /// <remarks>
    ///   When overriden in a derived class, this method allows to perform additional resource
    ///   creation, configuration, and initialization.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    ///   <see cref="PresentationParameters.DeviceWindowHandle"/> is <see langword="null"/> or
    ///   the <see cref="WindowHandle.Handle"/> is invalid or zero.
    /// </exception>
    public virtual void OnRecreated()
    {
    }

    /// <summary>
    ///   Processes and adjusts the Presentation Parameters before initializing the Graphics Presenter.
    /// </summary>
    /// <remarks>
    ///   When overriden in a derived class, this method allows to modify the specified Presentation Parameters
    ///   before initializing the internal buffers and resources.
    /// </remarks>
    protected virtual void ProcessPresentationParameters()
    {
    }

    /// <summary>
    ///   Creates the Depth-Stencil Buffer.
    /// </summary>
    /// <exception cref="NotSupportedException">The Depth-Stencil format specified is not supported.</exception>
    /// <remarks>
    ///   <para>
    ///     When overriden in a derived class, this method allows to create a custom Depth-Stencil Buffer
    ///     when initializing the Graphics Presenter.
    ///   </para>
    ///   <para>
    ///     By default, if a depth format has been specified, a Depth-Stencil Buffer is created with the same
    ///     size as the Back-Buffer.
    ///   </para>
    /// </remarks>
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

        var depthTexture = GraphicsDevice.IsDebugMode
            ? new Texture(GraphicsDevice, name: "Depth-Stencil Buffer")
            : new Texture(GraphicsDevice);

        depthTexture.InitializeFrom(depthTextureDescription);
        DepthStencilBuffer = depthTexture.DisposeBy(this);
    }
}
