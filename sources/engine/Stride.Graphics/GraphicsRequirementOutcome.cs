// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   Why a renderer does or does not get the capability it declared.
/// </summary>
public enum GraphicsRequirementOutcome
{
    /// <summary>
    ///   The backend implements the capability and the device provides it.
    /// </summary>
    Available,

    /// <summary>
    ///   The hardware or the driver does not provide the capability. Another device may.
    /// </summary>
    NotProvidedByDevice,

    /// <summary>
    ///   The backend has not implemented the capability, whatever the device provides. No device
    ///   changes this answer.
    /// </summary>
    NotImplementedByBackend
}
