// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Stride.Core.Assets.Editor.Commands;

namespace Stride.GameStudio.Avalonia.Views;

/// <summary>
/// The Keyboard shortcuts page of the settings (<see cref="KeyboardShortcutsViewModel"/>). Its recorder gives the selected
/// command the keys pressed while it has the focus: modifiers and one key, refused (with the reason) when the editor would
/// not bind it; Escape and Tab leave it.
/// </summary>
public sealed partial class KeyboardShortcutsView : UserControl
{
    public KeyboardShortcutsView()
    {
        InitializeComponent();
        // Before the text box: it would handle some keys itself
        GestureRecorder.AddHandler(KeyDownEvent, OnRecorderKeyDown, RoutingStrategies.Tunnel);
        ShortcutList.SelectionChanged += (_, _) => ShowMessage(null);
    }

    private KeyboardShortcutsViewModel? ViewModel => DataContext as KeyboardShortcutsViewModel;

    /// <summary>
    /// The shortcut a key makes with its modifiers, as the editor writes it ("Ctrl+Shift+K"), or null for a modifier alone.
    /// </summary>
    public static string? ToGesture(Key key, KeyModifiers modifiers)
    {
        switch (key)
        {
            case Key.None:
            case Key.LeftCtrl:
            case Key.RightCtrl:
            case Key.LeftShift:
            case Key.RightShift:
            case Key.LeftAlt:
            case Key.RightAlt:
            case Key.LWin:
            case Key.RWin:
            case Key.System:
                return null;
        }
        return CommandGesture.TryParse(new KeyGesture(key, modifiers).ToString(), out var gesture) ? gesture.ToString() : null;
    }

    /// <summary>
    /// Gives the selected command the shortcut made of a key and its modifiers, as the recorder does.
    /// </summary>
    /// <returns>Null when it was given, else why not.</returns>
    public string? Record(Key key, KeyModifiers modifiers)
    {
        if (ViewModel is not { SelectedShortcut: { } line } viewModel || ToGesture(key, modifiers) is not { } gesture)
            return null;
        var error = viewModel.SetGesture(line, gesture);
        ShowMessage(error);
        return error;
    }

    private void OnRecorderKeyDown(object? sender, KeyEventArgs e)
    {
        // Leave the recorder with Escape or Tab alone
        if ((e.Key is Key.Escape or Key.Tab) && e.KeyModifiers is KeyModifiers.None or KeyModifiers.Shift)
        {
            if (e.Key == Key.Escape)
            {
                ShortcutList.Focus();
                e.Handled = true;
            }
            return;
        }
        e.Handled = true;
        Record(e.Key, e.KeyModifiers);
    }

    private void OnNoneClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { SelectedShortcut: { } line } viewModel)
            viewModel.SetNone(line);
        ShowMessage(null);
    }

    private void OnResetClick(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is { SelectedShortcut: { } line } viewModel)
            viewModel.Reset(line);
        ShowMessage(null);
    }

    private void OnResetAllClick(object? sender, RoutedEventArgs e)
    {
        ViewModel?.ResetAll();
        ShowMessage(null);
    }

    private void ShowMessage(string? message)
    {
        RecorderMessage.Text = message;
        RecorderMessage.IsVisible = message is not null;
    }
}
