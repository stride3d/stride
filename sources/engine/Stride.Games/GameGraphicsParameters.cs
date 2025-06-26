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

using Stride.Graphics;

namespace Stride.Games;

/// <summary>
///   Describes the preferred graphics configuration for a Game.
/// </summary>
public class GameGraphicsParameters
{
    /// <summary>
    ///   The preferred width of the back-buffer, in pixels.
    /// </summary>
    /// <remarks>
    ///   Both <see cref="PreferredBackBufferWidth"/> and <see cref="PreferredBackBufferHeight"/>
    ///   determine both the screen resolution (if in full-screen mode) or the window size
    ///   (if in windowed-mode).
    /// </remarks>
    public int PreferredBackBufferWidth;

    /// <summary>
    ///   The preferred height of the back-buffer, in pixels.
    /// </summary>
    /// <remarks>
    ///   Both <see cref="PreferredBackBufferWidth"/> and <see cref="PreferredBackBufferHeight"/>
    ///   determine both the screen resolution (if in full-screen mode) or the window size
    ///   (if in windowed-mode).
    /// </remarks>
    public int PreferredBackBufferHeight;

    /// <summary>
    ///   A <strong><see cref="PixelFormat"/></strong> specifying the display format.
    /// </summary>
    public PixelFormat PreferredBackBufferFormat;

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
    public PixelFormat PreferredDepthStencilFormat;

    /// <summary>
    ///   A value indicating whether the application must render in full-screen mode (<see langword="true"/>)
    ///   or inside a window (<see langword="false"/>).
    /// </summary>
    public bool IsFullScreen;

    /// <summary>
    ///   The index of the output (monitor) to use when rendering in full-screen mode.
    /// </summary>
    /// <remarks>
    ///   This output index does not have any effect when windowed mode is used (i.e. when
    ///   <see cref="IsFullScreen"/> is set to <see langword="false"/>).
    /// </remarks>
    public int PreferredFullScreenOutputIndex;

    /// <summary>
    ///   An array of <see cref="GraphicsProfile"/>s ordered according to preference, where
    ///   they will be tested in order from the start and the first to succeed will be selected.
    /// </summary>
    /// <remarks>
    ///   The <strong>Graphics Profile</strong> determines which hardware and software characteristics
    ///   and features are available for rendering, so it directly impacts things as performance,
    ///   image quality, and graphics effects complexity. Also, higher profiles may enable advanced
    ///   graphical effects but require modern hardware.
    /// </remarks>
    public GraphicsProfile[] PreferredGraphicsProfile;

    /// <summary>
    ///   The preferred refresh rate, in hertz.
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
    public Rational PreferredRefreshRate;

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
    public MultisampleCount PreferredMultisampleCount;

    /// <summary>
    ///   A value indicating whether to synchronize the presentation of the rendered frame with
    ///   the vertical retrace (i.e. <see cref="PreferredRefreshRate"/>) of the monitor.
    /// </summary>
    /// <remarks>
    ///   <para>
    ///     The synchronization with the vertical refresh, also known as <strong>V-Sync</strong>,
    ///     limits the frequency of presenting frames to the screen to <see cref="PreferredRefreshRate"/>.
    ///   </para>
    ///   <para>
    ///     If enabled (<see langword="true"/>), the number of frames produced each second will be
    ///     limited by the number of times the monitor can refresh its image each second.
    ///   </para>
    ///   <para>
    ///     If disabled (<see langword="false"/>), the number of frames produced each second will be
    ///     unlimited. However, due to mismatch between frame generation and monitor refresh rate,
    ///     an artifact known as <strong>screen tearing</strong> can occur when multiple frames are
    ///     displayed simultanously. This is especially noticeable at high frame rates.
    ///   </para>
    /// </remarks>
    public bool SynchronizeWithVerticalRetrace;

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
    ///   A value indicating whether the application should use a specific adapter
    ///   (based on its <see cref="GraphicsAdapter.AdapterUid"/>) for rendering.
    /// </summary>
    /// <remarks>
    ///   If <see cref="RequiredAdapterUid"/> is not <see langword="null"/>, the engine will try to
    ///   initialize a device with the same <see cref="GraphicsAdapter.AdapterUid"/>.
    /// </remarks>
    public string? RequiredAdapterUid; // TODO: What happens if invalid or unavailable?
}
