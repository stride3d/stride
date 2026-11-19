// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Makes an <see cref="AutoCompleteBox"/> a filtering list for a text property: the list opens when the box gets the
/// focus, and the text goes to the property (one undo entry) when an item is picked in the list, on Enter (the first item
/// shown when the list is open) or when the focus leaves, not on every key. Escape shows the value of the property again.
/// </summary>
/// <remarks>
/// The <see cref="AutoCompleteBox.Text"/> binding must use <c>UpdateSourceTrigger=Explicit</c>, and the box filter its
/// items with <see cref="AutoCompleteFilterMode.Contains"/>.
/// </remarks>
public abstract class AutoCompletePickerBehavior : Behavior<AutoCompleteBox>
{
    private bool hasChanges;

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } box)
        {
            box.GotFocus += OnGotFocus;
            box.LostFocus += OnLostFocus;
            box.TextChanged += OnTextChanged;
            box.DropDownClosed += OnDropDownClosed;
            box.AddHandler(InputElement.KeyDownEvent, OnKeyDown, RoutingStrategies.Tunnel);
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } box)
        {
            box.GotFocus -= OnGotFocus;
            box.LostFocus -= OnLostFocus;
            box.TextChanged -= OnTextChanged;
            box.DropDownClosed -= OnDropDownClosed;
            box.RemoveHandler(InputElement.KeyDownEvent, OnKeyDown);
        }
        base.OnDetaching();
    }

    /// <summary>
    /// Gets the text that picking the given item of the list puts in the box, or <c>null</c> if it is not an item of this list.
    /// </summary>
    protected abstract string? GetItemText(object? item);

    private void OnGotFocus(object? sender, FocusChangedEventArgs e)
    {
        if (AssociatedObject is { IsDropDownOpen: false, ItemsSource: not null } box)
            box.IsDropDownOpen = true;
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        // The list of the box is part of it: the focus only leaves when it goes to another control
        if (AssociatedObject is { IsKeyboardFocusWithin: false })
            Commit();
    }

    private void OnTextChanged(object? sender, TextChangedEventArgs e)
    {
        // The text also changes when the binding shows the value of the property: only an edit counts
        if (AssociatedObject is { } box && (box.IsKeyboardFocusWithin || box.IsDropDownOpen))
            hasChanges = true;
    }

    private void OnDropDownClosed(object? sender, EventArgs e)
    {
        // An item picked in the list: the box shows its text. The list also closes when the text is cleared, with an item
        // still selected, which is no pick.
        if (AssociatedObject is { SelectedItem: { } item } box && GetItemText(item) is { } text && text == box.Text)
            Commit();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                // Enter in the open list takes the item it highlights, the first one shown
                if (AssociatedObject is { IsDropDownOpen: true, SelectedItem: null } list && FindShownItemText(list) is { } text)
                    list.Text = text;
                Commit();
                break;
            case Key.Escape when hasChanges && AssociatedObject is { } box:
                // The box would put back the typed text when its list closes: close it first, then show the property
                hasChanges = false;
                box.IsDropDownOpen = false;
                BindingOperations.GetBindingExpressionBase(box, AutoCompleteBox.TextProperty)?.UpdateTarget();
                hasChanges = false;
                e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Finds the text of the item the open list shows first for the text of the box: the item with this text, otherwise the
    /// first whose text contains it (the <see cref="AutoCompleteFilterMode.Contains"/> filter of the box).
    /// </summary>
    private string? FindShownItemText(AutoCompleteBox box)
    {
        if (box.ItemsSource is null || string.IsNullOrEmpty(box.Text))
            return null;

        var text = box.Text;
        var texts = box.ItemsSource.Cast<object?>().Select(GetItemText).OfType<string>().ToList();
        return texts.FirstOrDefault(x => string.Equals(x, text, StringComparison.OrdinalIgnoreCase))
            ?? texts.FirstOrDefault(x => x.Contains(text, StringComparison.OrdinalIgnoreCase));
    }

    private void Commit()
    {
        if (!hasChanges || AssociatedObject is not { } box)
            return;

        hasChanges = false;
        BindingOperations.GetBindingExpressionBase(box, AutoCompleteBox.TextProperty)?.UpdateSource();
    }
}
