// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Defines the rotation of a display, indicating how the Back-Buffers should be rotated to fit
///   the physical rotation of a monitor.
/// </summary>
public enum DisplayRotation
{
    /// <summary>
    ///   The default value for the rotation (no rotation, or unspecified).
    /// </summary>
    Default,

    /// <summary>
    ///   The display is rotated 90 degrees clockwise.
    /// </summary>
    Rotate90,
    /// <summary>
    ///   The display is rotated 180 degrees clockwise.
    /// </summary>
    Rotate180,
    /// <summary>
    ///   The display is rotated 270 degrees clockwise.
    /// </summary>
    Rotate270
}
