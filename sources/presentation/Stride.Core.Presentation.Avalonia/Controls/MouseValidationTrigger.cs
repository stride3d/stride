// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// When a <see cref="NumericTextBox"/> writes a value changed with the mouse (a drag, or a held spin button) to its source.
/// </summary>
public enum MouseValidationTrigger
{
    /// <summary>
    /// The value is written at each step: every move of the mouse, every repeat of a spin button.
    /// </summary>
    OnMouseMove,
    /// <summary>
    /// The value is written once, when the mouse button is released: one change for the whole gesture.
    /// </summary>
    OnMouseUp,
}
