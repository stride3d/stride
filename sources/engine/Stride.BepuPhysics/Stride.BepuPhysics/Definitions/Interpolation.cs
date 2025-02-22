// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.BepuPhysics.Definitions;

public enum InterpolationMode
{
    /// <summary>
    /// No interpolation, the body will be moved on every physics update and left alone during normal updates
    /// </summary>
    None,
    /// <summary>
    /// The body will move from the previous physics pose to the current physics pose,
    /// introducing one physics update of latency but should be very smooth
    /// </summary>
    Interpolated,
    /// <summary>
    /// The body will move from the current physics pose to a predicted one,
    /// reducing the latency but introducing imprecise or jerky motion when the pose changes significantly
    /// </summary>
    Extrapolated
}