// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

/// <summary>
/// Edits a name in place with a text box that the view shows while the view model is editing (<see cref="IsEditing"/>, see
/// <see cref="Commands.BeginEditCommand"/>). The box takes the focus with the name selected. Enter commits the text to
/// <see cref="Name"/>; Escape, or the focus leaving the box, cancels the edit.
/// </summary>
public sealed class InlineEditBehavior : Behavior<TextBox>
{
    public static readonly StyledProperty<string?> NameProperty =
        AvaloniaProperty.Register<InlineEditBehavior, string?>(nameof(Name), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<bool> IsEditingProperty =
        AvaloniaProperty.Register<InlineEditBehavior, bool>(nameof(IsEditing), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// The name being edited; set when the user commits the edit.
    /// </summary>
    public string? Name
    {
        get => GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>
    /// Whether the name is being edited; the box shows while it is, and the behavior ends the edit.
    /// </summary>
    public bool IsEditing
    {
        get => GetValue(IsEditingProperty);
        set => SetValue(IsEditingProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } textBox)
        {
            textBox.KeyDown += OnKeyDown;
            textBox.LostFocus += OnLostFocus;
            textBox.AttachedToVisualTree += OnAttachedToVisualTree;
        }
        if (IsEditing)
            Begin();
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } textBox)
        {
            textBox.KeyDown -= OnKeyDown;
            textBox.LostFocus -= OnLostFocus;
            textBox.AttachedToVisualTree -= OnAttachedToVisualTree;
        }
        base.OnDetaching();
    }

    /// <inheritdoc/>
    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == IsEditingProperty && change.GetNewValue<bool>() && AssociatedObject is not null)
            Begin();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        if (IsEditing)
            Begin();
    }

    /// <summary>
    /// Shows the name in the box and gives it the focus, once the box is laid out (it is shown by the same change).
    /// </summary>
    private void Begin()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (!IsEditing || AssociatedObject is not { IsEffectivelyVisible: true } textBox)
                return;
            textBox.Text = Name;
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                Commit();
                e.Handled = true;
                break;
            case Key.Escape:
                Cancel();
                e.Handled = true;
                break;
        }
    }

    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Cancel();
    }

    private void Commit()
    {
        if (!IsEditing || AssociatedObject is not { } textBox)
            return;

        var text = textBox.Text;
        IsEditing = false;
        if (!string.IsNullOrWhiteSpace(text) && text != Name)
            Name = text;
    }

    private void Cancel()
    {
        if (IsEditing)
            IsEditing = false;
    }
}
