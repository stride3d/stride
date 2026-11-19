// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Xaml.Interactivity;

namespace Stride.Core.Presentation.Avalonia.Behaviors;

/// <summary>
/// Runs a command with the text of a <see cref="TextBox"/> that asks for a value, such as the key of a new dictionary entry.
/// </summary>
/// <remarks>
/// When the box is shown, it shows <see cref="InitialText"/>, selected, and takes the focus. Enter runs <see cref="Command"/>
/// with the text and sets <see cref="IsOpen"/> to <see langword="false"/>, which closes the popup or drop-down the box is in;
/// Escape only closes it.
/// </remarks>
public sealed class ValidateTextBehavior : Behavior<TextBox>
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<ValidateTextBehavior, ICommand?>(nameof(Command));

    public static readonly StyledProperty<string?> InitialTextProperty =
        AvaloniaProperty.Register<ValidateTextBehavior, string?>(nameof(InitialText));

    public static readonly StyledProperty<bool> IsOpenProperty =
        AvaloniaProperty.Register<ValidateTextBehavior, bool>(nameof(IsOpen), defaultBindingMode: BindingMode.TwoWay);

    /// <summary>
    /// The command that Enter runs, with the text of the box as its parameter.
    /// </summary>
    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    /// <summary>
    /// The text the box shows each time it is shown.
    /// </summary>
    public string? InitialText
    {
        get => GetValue(InitialTextProperty);
        set => SetValue(InitialTextProperty, value);
    }

    /// <summary>
    /// Whether the popup or drop-down that holds the box is open; Enter and Escape set it to <see langword="false"/>.
    /// </summary>
    public bool IsOpen
    {
        get => GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    /// <inheritdoc/>
    protected override void OnAttached()
    {
        base.OnAttached();
        if (AssociatedObject is { } textBox)
        {
            textBox.KeyDown += OnKeyDown;
            textBox.AttachedToVisualTree += OnAttachedToVisualTree;
            if (textBox.IsAttachedToVisualTree())
                Begin();
        }
    }

    /// <inheritdoc/>
    protected override void OnDetaching()
    {
        if (AssociatedObject is { } textBox)
        {
            textBox.KeyDown -= OnKeyDown;
            textBox.AttachedToVisualTree -= OnAttachedToVisualTree;
        }
        base.OnDetaching();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        Begin();
    }

    /// <summary>
    /// Shows the initial text, selected, and gives the box the focus, once the box is laid out (a popup shows it as it opens).
    /// </summary>
    private void Begin()
    {
        if (AssociatedObject is { } shown)
            shown.Text = InitialText;
        Dispatcher.UIThread.Post(() =>
        {
            if (AssociatedObject is not { IsEffectivelyVisible: true } textBox)
                return;
            textBox.Focus();
            textBox.SelectAll();
        }, DispatcherPriority.Loaded);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
                Validate();
                e.Handled = true;
                break;
            case Key.Escape:
                IsOpen = false;
                e.Handled = true;
                break;
        }
    }

    private void Validate()
    {
        var text = AssociatedObject?.Text ?? string.Empty;
        if (Command is { } command && command.CanExecute(text))
            command.Execute(text);
        IsOpen = false;
    }
}
