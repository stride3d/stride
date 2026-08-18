// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Graphics;

/// <summary>
///   What happens when the device does not provide a declared capability.
/// </summary>
public enum GraphicsRequirementSeverity
{
    /// <summary>
    ///   The renderer applies its own fallback and works at reduced quality. The framework does nothing
    ///   with this value.
    /// </summary>
    Preferred,

    /// <summary>
    ///   The renderer cannot work without the capability.
    /// </summary>
    Required
}
