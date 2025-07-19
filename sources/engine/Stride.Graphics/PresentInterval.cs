// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines the relationship between the <see cref="GraphicsOutput"/>'s refresh rate and the rate
///   at which <em>Present</em> operations are completed.
/// </summary>
public enum PresentInterval
{
    // NOTE: Values should not be changed, as they are directly used by Present method (0: no frame to wait, 1: one frame...etc.)

    /// <summary>
    ///   The runtime updates the window client area <strong>immediately</strong>,
    ///   and <strong>might do so more than once during the adapter refresh period</strong>.
    ///   Present operations might be affected immediately.
    /// </summary>
    /// <remarks>
    ///   This option is always available for both windowed and full-screen swap chains.
    /// </remarks>
    Immediate = 0,

    /// <summary>
    ///   The default value. Equivalent to setting <see cref="One"/>.
    /// </summary>
    Default = One,

    /// <summary>
    ///   The driver <strong>waits for the vertical retrace period</strong>
    ///   (the runtime will beam trace to prevent tearing). This is commonly known as <strong>V-Sync</strong>.
    ///   Present operations are not affected more frequently than the screen refresh rate;
    ///   the runtime completes one Present operation per adapter refresh period, at most.
    /// </summary>
    /// <remarks>
    ///   This option is always available for both windowed and full-screen swap chains.
    /// </remarks>
    One = 1,

    /// <summary>
    ///   The driver <strong>waits for the vertical retrace period</strong>.
    ///   Present operations are not affected more frequently than <strong>every second screen refresh</strong>.
    /// </summary>
    Two = 2
}
