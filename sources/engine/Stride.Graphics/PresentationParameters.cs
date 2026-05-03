// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Graphics;

/// <summary>
///   Describes how a <see cref="GraphicsDevice"/> will display to the screen.
/// </summary>
public sealed class PresentationParameters : IEquatable<PresentationParameters>
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
    private const ColorSpaceType DefaultOutputColorSpace = ColorSpaceType.Rgb_Full_G22_None_P709; // Default RGB output for monitors with a standard gamma of 2.2

    #endregion

    /// <summary>
    ///   A <strong><see cref="PixelFormat"/></strong> specifying the display format.
    /// </summary>
    public PixelFormat BackBufferFormat;

    /// <summary>
    ///   The height of the back-buffer, in pixels.
    /// </summary>
    /// <remarks>
    ///   Both <see cref="BackBufferWidth"/> and <see cref="BackBufferHeight"/>
    ///   determine both the screen resolution (if in full-screen mode) or the window size
    ///   (if in windowed-mode).
    /// </remarks>
    public int BackBufferHeight;

    /// <summary>
    ///   The width of the back-buffer, in pixels.
    /// </summary>
    /// <remarks>
    ///   Both <see cref="BackBufferWidth"/> and <see cref="BackBufferHeight"/>
    ///   determine both the screen resolution (if in full-screen mode) or the window size
    ///   (if in windowed-mode).
    /// </remarks>
    public int BackBufferWidth;

    /// <summary>
    ///   A <strong><see cref="PixelFormat"/></strong> specifying the depth-stencil format.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <strong>Depth Buffer</strong> (also known as Z-buffer) is used to determine the depth of each pixel
    ///     so close geometry correctly occludes farther geometry. The format determines the precission
    ///     of the depth buffer.
    ///   </para>
    ///   <para>
    ///     The <strong>Stencil Buffer</strong> is used to store additional information for each pixel, such as
    ///     marking or discarding specific pixels for different effects.
    ///   </para>
    ///   <para>
    ///     This format determines both because usually the stencil buffer is a part of the depth buffer
    ///     reserved for other uses.
    ///   </para>
    ///   <para>
    ///     Some examples are <see cref="PixelFormat.D24_UNorm_S8_UInt"/>, where the depth buffer uses 24 bits
    ///     and the stencil buffer uses 8 bits, for a total of 32 bits per pixel, or
    ///     <see cref="PixelFormat.D32_Float"/>, which uses 32 bits for the depth buffer and no bits for the stencil buffer.
    ///   </para>
    /// </remarks>
    public PixelFormat DepthStencilFormat;

    /// <summary>
    ///   The window object or handle where the presentation will occur.
    /// </summary>
    /// <remarks>
    ///   A window object is platform-dependent:
    ///   <list type="bullet">
    ///     <item>
    ///       <term>Windows Desktop</term>
    ///       <description>
    ///         This could be a low-level window/control handle (<see cref="IntPtr"/>), or
    ///         directly a Windows Forms' <c>Form</c> or <c>Control</c> object.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term>Windows Metro</term>
    ///       <description>
    ///         This could be a <c>SwapChainBackgroundPanel</c> or <c>SwapChainPanel</c> object.
    ///       </description>
    ///     </item>
    ///   </list>
    /// </remarks>
    public WindowHandle DeviceWindowHandle;

    /// <summary>
    ///   A value indicating whether the application must render in full-screen mode (<see langword="true"/>)
    ///   or inside a window (<see langword="false"/>).
    /// </summary>
    public bool IsFullScreen;

    /// <summary>
    ///   A <see cref="MultisampleCount"/> indicating the number of sample locations during multi-sampling.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The multi-sampling is applied to the back-buffer to reduce the aliasing artifacts. This is
    ///     known as <strong>Multi-Sampling Anti-Aliasing</strong> (MSAA).
    ///   </para>
    ///   <para>
    ///     The higher the number of samples, the aliasing patterns will be less visible, but it will result
    ///     in more memory being consumed, and costlier rasterization.
    ///   </para>
    ///   <para>
    ///     If <see cref="MultisampleCount.None"/> is selected, no multi-sampling will be applied.
    ///     Common values include <see cref="MultisampleCount.X2"/> (minimal anti-aliasing) and
    ///     <see cref="MultisampleCount.X8"/> (high-quality anti-aliasing).
    ///     Higher values increase GPU workload.
    ///   </para>
    /// </remarks>
    public MultisampleCount MultisampleCount;

    /// <summary>
    ///   A value of <see cref="PresentationInterval"/> determining the maximum rate
    ///   at which the Swap Chain's back buffers can be presented to the front buffer.
    /// </summary>
    public PresentInterval PresentationInterval;

    /// <summary>
    ///   The refresh rate of the screen, in hertz.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <strong>Refresh Rate</strong> is the number of times per second the screen is refreshed,
    ///     i.e. the number of frames per second the monitor can display.
    ///   </para>
    ///   <para>
    ///     The value is represented as a <see cref="Rational"/>, so it can represent both usual integer
    ///     refresh rates (e.g. 60Hz) and fractional refresh rates (e.g. 59.94Hz).
    ///   </para>
    ///   <para>
    ///     Usually, the refresh rate is only respected when rendering in full-screen mode (i.e. when
    ///     <see cref="IsFullScreen"/> is set to <see langword="true"/>).
    ///   </para>
    ///   <para>
    ///     Common refresh rates include 60Hz, 120Hz, and 144Hz, depending on monitor capabilities.
    ///   </para>
    /// </remarks>
    public Rational RefreshRate;

    /// <summary>
    ///   The index of the preferred output (monitor) to use when switching to full-screen mode.
    /// </summary>
    /// <remarks>
    ///   This parameter does not have any effect when windowed mode is used
    ///   (<see cref="IsFullScreen"/> is <see langword="false"/>).
    /// </remarks>
    public int PreferredFullScreenOutputIndex;

    /// <summary>
    ///   The color space to use for presenting the frame to the screen.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The <strong>Color Space</strong> defines how colors are represented and displayed on the screen.
    ///     Common values include:
    ///   </para>
    ///   <list type="bullet">
    ///     <item>
    ///       <term><see cref="ColorSpace.Gamma"/></term>
    ///       <description>
    ///         It usually represents sRGB, the standard RGB color space used in most monitors and applications.
    ///         It offers a limited range of colors suitable for general purposes.
    ///       </description>
    ///     </item>
    ///     <item>
    ///       <term><see cref="ColorSpace.Linear"/></term>
    ///       <description>
    ///         A linear color space suitable for high-dynamic-range color values like HDR10, supporting a wider
    ///         range of brightness and colors. This is commonly used in modern HDR displays for enhanced image quality.
    ///       </description>
    ///     </item>
    ///   </list>
    ///   <para>
    ///     Choosing the appropriate color space affects the visual quality of your application.
    ///     For example, sRGB is recommended for compatibility, while HDR10 may enhance visuals in
    ///     games or applications designed for HDR content.
    ///   </para>
    /// </remarks>
    public ColorSpace ColorSpace;

    /// <summary>
    ///   The color space type used for the Graphics Presenter output.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The output color space can be used to render to HDR monitors.
    ///     Consult the documentation of the <see cref="ColorSpaceType"/> enum for more details.
    ///   </para>
    ///   <para>
    ///     Note that this is currently only supported in Stride when using the Direct3D Graphics API.
    ///     For more information about High Dynamic Range (HDR) rendering, see
    ///     <see href="https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range"/>.
    ///   </para>
    /// </remarks>
    public ColorSpaceType OutputColorSpace;


    /// <summary>
    ///   Initializes a new instance of the <see cref="PresentationParameters"/> class with default values.
    /// </summary>
    /// <remarks>
    ///   The returned instance will be configured with the following default values:
    ///   <list type="bullet">
    ///     <item>
    ///       A back buffer resolution of 800x480 pixels, with a 32-bits-per-pixel integer format
    ///       (<see cref="PixelFormat.R8G8B8A8_UNorm"/>).
    ///     </item>
    ///     <item>
    ///       A 24-bit integer depth buffer with an additional 8-bit stencil buffer
    ///       (<see cref="PixelFormat.D24_UNorm_S8_UInt"/>).
    ///     </item>
    ///     <item>No multi-sampling.</item>
    ///     <item>
    ///       Assuming a linear color space (<see cref="ColorSpace.Linear"/>) and an output color space
    ///       <see cref="ColorSpaceType.RgbFullG22NoneP709"/>, which is the default RGB output for monitors
    ///       with a standard gamma of 2.2.
    ///     </item>
    ///     <item>
    ///       A windowed presentation at 60 Hz with no V-Sync (<see cref="PresentInterval.Immediate"/>).
    ///     </item>
    ///   </list>
    /// </remarks>
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
        OutputColorSpace = DefaultOutputColorSpace;
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="PresentationParameters"/> class with default values, but
    ///   with the specified back buffer size, using <see cref="PixelFormat.R8G8B8A8_UNorm"/>, and window handle.
    /// </summary>
    /// <param name="backBufferWidth">The width of the back buffer, in pixels.</param>
    /// <param name="backBufferHeight">The height of the back buffer, in pixels.</param>
    /// <param name="windowHandle">The window handle.</param>
    /// <seealso cref="PresentationParameters()"/>
    public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle windowHandle)
        : this(backBufferWidth, backBufferHeight, windowHandle, PixelFormat.R8G8B8A8_UNorm)
    {
    }

    /// <summary>
    ///   Initializes a new instance of the <see cref="PresentationParameters"/> class with default values,
    ///   but with the specified back buffer size, pixel format, and window handle.
    /// </summary>
    /// <param name="backBufferWidth">The width of the back buffer, in pixels.</param>
    /// <param name="backBufferHeight">The height of the back buffer, in pixels.</param>
    /// <param name="backBufferFormat">The back buffer format.</param>
    /// <param name="windowHandle">The window handle.</param>
    /// <seealso cref="PresentationParameters()"/>
    public PresentationParameters(int backBufferWidth, int backBufferHeight, WindowHandle windowHandle, PixelFormat backBufferFormat)
        : this()
    {
        BackBufferWidth = backBufferWidth;
        BackBufferHeight = backBufferHeight;
        DeviceWindowHandle = windowHandle;
        BackBufferFormat = backBufferFormat;
    }


    /// <summary>
    ///   Creates a new <see cref="PresentationParameters"/> object that is a copy of the current instance.
    /// </summary>
    /// <returns>A new <see cref="PresentationParameters"/> object that is a copy of this instance.</returns>
    public PresentationParameters Clone()
    {
        return (PresentationParameters) MemberwiseClone();
    }

    /// <inheritdoc />
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

    /// <inheritdoc />
    public override bool Equals(object obj)
    {
        return obj is PresentationParameters parameters && Equals(parameters);
    }

    /// <inheritdoc />
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
