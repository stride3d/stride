// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Media;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainView : UserControl
{
    private MainViewModel? viewModel;

    public MainView()
    {
        InitializeComponent();
        EditRouter = new EditCommandRouter(this);
        // The items of the Edit menu run the router's commands. They are set here rather than bound in XAML: a binding to an
        // ancestor of the item resolves only while its menu is open, and the item is disabled until then.
        UndoMenuItem.Command = EditRouter.Undo;
        RedoMenuItem.Command = EditRouter.Redo;
        CutMenuItem.Command = EditRouter.Cut;
        CopyMenuItem.Command = EditRouter.Copy;
        PasteMenuItem.Command = EditRouter.Paste;
        DeleteMenuItem.Command = EditRouter.Delete;
        RenameMenuItem.Command = EditRouter.Rename;
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (viewModel is not null)
            viewModel.RecentFiles.CollectionChanged -= RecentFilesChanged;
        viewModel = DataContext as MainViewModel;
        if (viewModel is not null)
            viewModel.RecentFiles.CollectionChanged += RecentFilesChanged;
        UpdateRecentSessionsMenu();
    }

    /// <summary>
    /// Routes the commands of the Edit menu and their shortcuts to the panel that has the focus.
    /// </summary>
    public EditCommandRouter EditRouter { get; }

    /// <summary>
    /// Gets a platform-specific <see cref="KeyGesture"/> for the Save action: Ctrl+S, or Cmd+S on macOS.
    /// </summary>
    public static KeyGesture SaveGesture => new(Key.S, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    /// <summary>
    /// The shortcut of File > New (Ctrl+N), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture NewGesture => new(Key.N, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    /// <summary>
    /// The shortcut of Project > Build project (F6, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture BuildProjectGesture => new(Key.F6);

    /// <summary>
    /// The shortcut of Project > Start project (F5, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture StartProjectGesture => new(Key.F5);

    /// <summary>
    /// The shortcut of Project > Cancel build (Ctrl+F6, as in WPF), bound on the main window like <see cref="SaveGesture"/>.
    /// </summary>
    public static KeyGesture CancelBuildGesture => new(Key.F6, Application.Current?.PlatformSettings?.HotkeyConfiguration.CommandModifiers ?? KeyModifiers.Control);

    private void RecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateRecentSessionsMenu();

    /// <summary>
    /// Fills File > Open recent as WPF does: one item per recent session (its version and its path, the full path in the tool
    /// tip), then "Clear list".
    /// </summary>
    private void UpdateRecentSessionsMenu()
    {
        var items = RecentSessionsMenu.Items;
        items.Clear();
        if (viewModel is null)
            return;

        var headerTemplate = (IDataTemplate)Resources["RecentSessionHeaderTemplate"]!;
        foreach (var recent in viewModel.RecentFiles)
        {
            var item = new MenuItem { Header = recent, HeaderTemplate = headerTemplate, Command = recent.OpenCommand };
            ToolTip.SetTip(item, recent.DisplayPath);
            items.Add(item);
        }
        items.Add(new Separator());
        items.Add(new MenuItem
        {
            Header = Tr._p("Menu", "Clear list"),
            Command = viewModel.ClearRecentFilesCommand,
            Icon = new Image { Source = (IImage?)this.FindResource("ImageDelete") },
        });
    }
}
