// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

public sealed class ButtonCloseWindowBehavior : CloseWindowBehavior<Button>
{
    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        AssociatedObject!.Click += ButtonClicked;
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        AssociatedObject!.Click -= ButtonClicked;
        base.OnDetaching();
    }

    /// <summary>
    /// Raised when the associated button is clicked. Close the containing window
    /// </summary>
    private void ButtonClicked(object? sender, RoutedEventArgs e)
    {
        CloseWindow();
    }
}
