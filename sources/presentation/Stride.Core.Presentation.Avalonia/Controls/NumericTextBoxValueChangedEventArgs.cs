// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Controls;

public sealed class NumericTextBoxValueChangedEventArgs : RoutedEventArgs
{
    public NumericTextBoxValueChangedEventArgs(RoutedEvent routedEvent, double? oldValue, double? newValue)
        : base(routedEvent)
    {
        OldValue = oldValue;
        NewValue = newValue;
    }

    public double? OldValue { get; }
    public double? NewValue { get; }
}
