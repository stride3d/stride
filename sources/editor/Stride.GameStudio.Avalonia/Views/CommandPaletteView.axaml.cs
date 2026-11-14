// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Commands;

namespace Stride.GameStudio.Avalonia.Views;

/// <summary>
/// The command palette's search box and results (<see cref="CommandPaletteViewModel"/>): Up and Down move in the results,
/// Enter or a click runs the selected command, Escape closes.
/// </summary>
public sealed partial class CommandPaletteView : UserControl
{
    public CommandPaletteView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Raised when the palette should close: Escape, or before the chosen command runs.
    /// </summary>
    public event EventHandler? CloseRequested;

    /// <summary>
    /// Raised to run the chosen command, after the palette closed.
    /// </summary>
    public event EventHandler<CommandPaletteItem>? RunRequested;

    private CommandPaletteViewModel? ViewModel => DataContext as CommandPaletteViewModel;

    /// <summary>
    /// Gives the focus to the search box, with its text selected.
    /// </summary>
    public void FocusSearch()
    {
        SearchBox.Focus();
        SearchBox.SelectAll();
    }

    private void OnSearchKeyDown(object? sender, KeyEventArgs e)
    {
        if (ViewModel is not { } viewModel)
            return;
        switch (e.Key)
        {
            case Key.Down:
                viewModel.MoveSelection(1);
                break;
            case Key.Up:
                viewModel.MoveSelection(-1);
                break;
            case Key.PageDown:
                viewModel.MoveSelection(10);
                break;
            case Key.PageUp:
                viewModel.MoveSelection(-10);
                break;
            case Key.Enter:
                Run(viewModel.SelectedItem);
                break;
            case Key.Escape:
                CloseRequested?.Invoke(this, EventArgs.Empty);
                break;
            default:
                return;
        }
        if (viewModel.SelectedItem is { } selected)
            ResultList.ScrollIntoView(selected);
        e.Handled = true;
    }

    private void OnResultTapped(object? sender, TappedEventArgs e)
    {
        // A click on a result, not on the list's scroll bar
        if (e.Source is Control source && source.FindAncestorOfType<ListBoxItem>(includeSelf: true) is { DataContext: CommandPaletteItem item })
            Run(item);
    }

    private void Run(CommandPaletteItem? item)
    {
        if (item is not { IsEnabled: true })
            return;
        CloseRequested?.Invoke(this, EventArgs.Empty);
        RunRequested?.Invoke(this, item);
    }
}
