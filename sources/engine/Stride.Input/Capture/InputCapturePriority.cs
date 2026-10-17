// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input;

/// <summary>
///   Common priorities for <see cref="InputManager.TryCapture"/>. A higher priority takes a device from a lower one.
///   Any value is valid.
/// </summary>
public static class InputCapturePriority
{
    /// <summary>
    ///   For capture driven by hovering, such as a pointer over a UI panel.
    /// </summary>
    public const int Hover = 0;

    /// <summary>
    ///   For capture driven by focus, such as a text box that receives the keyboard.
    /// </summary>
    public const int Focus = 100;

    /// <summary>
    ///   For modal layers, such as a console or a pause menu.
    /// </summary>
    public const int Modal = 1000;
}
