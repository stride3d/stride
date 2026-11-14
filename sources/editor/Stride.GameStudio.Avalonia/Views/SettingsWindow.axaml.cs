// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia.Controls;
using Stride.Core.Assets.Editor.Commands;

namespace Stride.GameStudio.Avalonia.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// The Keyboard shortcuts page, or null to hide it (no command to list).
    /// </summary>
    public KeyboardShortcutsViewModel? Shortcuts
    {
        get => ShortcutsView.DataContext as KeyboardShortcutsViewModel;
        set
        {
            ShortcutsView.DataContext = value;
            ShortcutsPage.IsVisible = value is not null;
        }
    }

    /// <summary>
    /// Shows the Keyboard shortcuts page rather than the settings.
    /// </summary>
    public void ShowShortcutsPage()
    {
        if (ShortcutsPage.IsVisible)
            Pages.SelectedItem = ShortcutsPage;
    }
}
