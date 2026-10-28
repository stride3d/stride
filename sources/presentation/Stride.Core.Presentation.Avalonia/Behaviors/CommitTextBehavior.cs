// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

/// <summary>
/// Writes the text of a <see cref="TextBox"/> back to the source of its <see cref="TextBox.TextProperty"/> binding only when
/// the user changed it and then pressed Enter or moved the focus away, the way the WPF editor's text boxes validate their
/// value. Escape gives the text box the bound value back. The binding must use <see cref="UpdateSourceTrigger.Explicit"/>:
/// Avalonia's default trigger writes the text back whenever it changes, including when the binding itself displays a
/// value, and a converter that cannot round-trip (a content reference becomes a new proxy object) then loops.
/// </summary>
public sealed class CommitTextBehavior : Behavior<TextBox>
{
    private bool hasChanges;

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } textBox)
        {
            textBox.TextChanging += OnTextChanging;
            textBox.KeyDown += OnKeyDown;
            textBox.LostFocus += OnLostFocus;
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } textBox)
        {
            textBox.TextChanging -= OnTextChanging;
            textBox.KeyDown -= OnKeyDown;
            textBox.LostFocus -= OnLostFocus;
        }
        base.OnDetaching();
    }

    private void OnTextChanging(object? sender, TextChangingEventArgs e)
    {
        // The text also changes when the binding displays a new source value: only an edit made while the box has the
        // focus is something to commit. TextChanging is raised as the text changes; TextChanged comes later from the
        // dispatcher, when the focus may have moved (and the edit, been cancelled).
        if (AssociatedObject is { IsFocused: true, IsReadOnly: false })
            hasChanges = true;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (AssociatedObject is not { } textBox)
            return;

        switch (e.Key)
        {
            case Key.Enter when !textBox.AcceptsReturn:
                Commit();
                e.Handled = true;
                break;
            case Key.Escape when hasChanges:
                Cancel();
                e.Handled = true;
                break;
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Commit();
    }

    private void Commit()
    {
        if (!hasChanges || AssociatedObject is not { } textBox)
            return;

        hasChanges = false;
        BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty)?.UpdateSource();
    }

    private void Cancel()
    {
        if (AssociatedObject is not { } textBox)
            return;

        // Showing the bound value again changes the text while the box has the focus, which counts as an edit: forget the
        // edit only afterwards, or moving the focus away would write the bound value back
        BindingOperations.GetBindingExpressionBase(textBox, TextBox.TextProperty)?.UpdateTarget();
        hasChanges = false;
    }
}
