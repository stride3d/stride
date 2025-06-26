// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

public class PresentationParameters : IEquatable<PresentationParameters>
{
    #region Default values

    private const int DefaultBackBufferWidth = 800;
    private const int DefaultBackBufferHeight = 480;
    private const PixelFormat DefaultBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;
    private const PixelFormat DefaultDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
    private const MultisampleCount DefaultMultisampleCount = MultisampleCount.None;
    private const PresentInterval DefaultPresentationInterval = PresentInterval.Immediate;
    private const bool DefaultIsFullScreen = false;
    private const int DefaultRefreshRate = 60; // Hz
    private const ColorSpace DefaultColorSpace = ColorSpace.Linear;

    #endregion

    /// <summary>
    ///   Describess how data will be displayed to the screen.
    /// </summary>
    public PixelFormat BackBufferFormat;

    public int BackBufferHeight;

    public int BackBufferWidth;

    public PixelFormat DepthStencilFormat;

    public WindowHandle DeviceWindowHandle;

    public bool IsFullScreen;

    public MultisampleCount MultisampleCount;

    public PresentInterval PresentationInterval;

    public Rational RefreshRate;

    public int PreferredFullScreenOutputIndex;

    public ColorSpace ColorSpace;


    public PresentationParameters()
    {
        BackBufferWidth = DefaultBackBufferWidth;
        BackBufferHeight = DefaultBackBufferHeight;
        BackBufferFormat = DefaultBackBufferFormat;
        PresentationInterval = DefaultPresentationInterval;
        DepthStencilFormat = DefaultDepthStencilFormat;
        MultisampleCount = DefaultMultisampleCount;
        IsFullScreen = DefaultIsFullScreen;
        RefreshRate = DefaultRefreshRate;
        ColorSpace = DefaultColorSpace;
    }

    public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle windowHandle)
        : this(backBufferWidth, backBufferHeight, windowHandle, PixelFormat.R8G8B8A8_UNorm)
    {
    }

    public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle windowHandle, PixelFormat backBufferFormat)
        : this()
    {
        BackBufferWidth = backBufferWidth;
        BackBufferHeight = backBufferHeight;
        DeviceWindowHandle = windowHandle;
        BackBufferFormat = backBufferFormat;
    }


    public PresentationParameters Clone()
    {
        return (PresentationParameters) MemberwiseClone();
    }

    public bool Equals(PresentationParameters other)
    {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return BackBufferFormat == other.BackBufferFormat
            && BackBufferHeight == other.BackBufferHeight
            && BackBufferWidth == other.BackBufferWidth
            && DepthStencilFormat == other.DepthStencilFormat
            && Equals(DeviceWindowHandle, other.DeviceWindowHandle)
            && IsFullScreen == other.IsFullScreen
            && MultisampleCount == other.MultisampleCount
            && PresentationInterval == other.PresentationInterval
            && RefreshRate.Equals(other.RefreshRate)
            && PreferredFullScreenOutputIndex == other.PreferredFullScreenOutputIndex
            && ColorSpace == other.ColorSpace;
    }

    public override bool Equals(object obj)
    {
        /// <summary>
        ///   A <strong><see cref="SharpDX.DXGI.Format" /></strong> structure describing the display format.
        /// </summary>
        /// <summary>
        ///   A value that describes the resolution height.
        /// </summary>
        /// <summary>
        ///   A value that describes the resolution width.
        /// </summary>
        /// <summary>
        /// Gets or sets the depth stencil format
        /// </summary>
        /// <summary>
        ///   A Window object. See remarks.
        /// </summary>
        /// <remarks>
        ///   A window object is platform dependent:
        ///   <ul>
        ///     <li>On Windows Desktop: This could a low level window/control handle (IntPtr), or directly a Winform Control object.</li>
        ///     <li>On Windows Metro: This could be SwapChainBackgroundPanel or SwapChainPanel object.</li>
        ///   </ul>
        /// </remarks>
        /// <summary>
        ///   Gets or sets a value indicating whether the application is in full screen mode.
        /// </summary>
        /// <summary>
        ///   Gets or sets a value indicating the number of sample locations during multisampling.
        /// </summary>
        /// <summary>
        ///   Gets or sets the maximum rate at which the swap chain's back buffers can be presented to the front buffer.
        /// </summary>
        /// <summary>
        ///   A structure describing the refresh rate in hertz
        /// </summary>
        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        /// <summary>
        /// The colorspace used.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationParameters" /> class with default values.
        /// </summary>
        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationParameters" /> class with <see cref="PixelFormat.R8G8B8A8_UNorm"/>.
        /// </summary>
        /// <param name="backBufferWidth">Width of the back buffer.</param>
        /// <param name="backBufferHeight">Height of the back buffer.</param>
        /// <param name="deviceWindowHandle">The device window handle.</param>
        /// <summary>
        /// Initializes a new instance of the <see cref="PresentationParameters" /> class.
        /// </summary>
        /// <param name="backBufferWidth">Width of the back buffer.</param>
        /// <param name="backBufferHeight">Height of the back buffer.</param>
        /// <param name="deviceWindowHandle">The device window handle.</param>
        /// <param name="backBufferFormat">The back buffer format.</param>
        return obj is PresentationParameters parameters && Equals(parameters);
    }

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(BackBufferFormat);
        hash.Add(BackBufferHeight);
        hash.Add(BackBufferWidth);
        hash.Add(DepthStencilFormat);
        hash.Add(DeviceWindowHandle);
        hash.Add(IsFullScreen);
        hash.Add(MultisampleCount);
        hash.Add(PresentationInterval);
        hash.Add(RefreshRate);
        hash.Add(PreferredFullScreenOutputIndex);
        hash.Add(ColorSpace);
        return hash.ToHashCode();
    }

    public static bool operator ==(PresentationParameters left, PresentationParameters right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(PresentationParameters left, PresentationParameters right)
    {
        return !Equals(left, right);
    }
}
