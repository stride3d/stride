// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Controls;

/// <summary>
/// The spin buttons of a <see cref="NumericTextBox"/>.
/// </summary>
public enum NumericTextBoxRepeatButton
{
    IncreaseButton,
    DecreaseButton,
}

/// <summary>
/// The arguments of <see cref="NumericTextBox.RepeatButtonPressedEvent"/> and <see cref="NumericTextBox.RepeatButtonReleasedEvent"/>.
/// </summary>
public sealed class RepeatButtonPressedEventArgs : RoutedEventArgs
{
    public RepeatButtonPressedEventArgs(RoutedEvent routedEvent, NumericTextBoxRepeatButton button)
        : base(routedEvent)
    {
        Button = button;
    }

    /// <summary>
    /// Gets the spin button that was pressed or released.
    /// </summary>
    public NumericTextBoxRepeatButton Button { get; }
}
