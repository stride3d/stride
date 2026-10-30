// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Xaml.Interactivity;
using Stride.Assets.Models;

namespace Stride.Assets.Editor.Avalonia.Behaviors;

/// <summary>
/// Makes an <see cref="AutoCompleteBox"/> behave as the WPF <c>FilteringComboBox</c> of the node name of a node link: the
/// list opens when the box gets the focus, and the text goes to the property (one undo entry) when a node is picked in the
/// list, on Enter (the first node shown when the list is open) or when the focus leaves, not on every key. Escape shows
/// the value of the property again.
/// </summary>
/// <remarks>
/// The <see cref="AutoCompleteBox.Text"/> binding must use <c>UpdateSourceTrigger=Explicit</c>.
/// </remarks>
public sealed class NodeNamePickerBehavior : Behavior<AutoCompleteBox>
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
        // A node picked in the list: the box shows its name. The list also closes when the text is cleared, with a node
        // still selected, which is no pick.
        if (AssociatedObject is { SelectedItem: NodeInformation node } box && node.Name == box.Text)
            Commit();
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                // As the WPF FilteringComboBox, Enter in the open list takes the node it highlights, the first one shown
                if (AssociatedObject is { IsDropDownOpen: true, SelectedItem: null } list && FindShownNode(list) is { } node)
                    list.Text = node.Name;
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
    /// Finds the node the open list shows first for the text of the box: the one with this name, otherwise the first
    /// whose name contains it (the <see cref="AutoCompleteFilterMode.Contains"/> filter of the box).
    /// </summary>
    private static NodeInformation? FindShownNode(AutoCompleteBox box)
    {
        if (box.ItemsSource is not IEnumerable<NodeInformation> nodes || string.IsNullOrEmpty(box.Text))
            return null;

        var text = box.Text;
        return nodes.FirstOrDefault(x => string.Equals(x.Name, text, StringComparison.OrdinalIgnoreCase))
            ?? nodes.FirstOrDefault(x => x.Name?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false);
    }

    private void Commit()
    {
        if (!hasChanges || AssociatedObject is not { } box)
            return;

        hasChanges = false;
        BindingOperations.GetBindingExpressionBase(box, AutoCompleteBox.TextProperty)?.UpdateSource();
    }
}
