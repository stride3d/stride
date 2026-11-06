// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Recycling;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Media;
using Stride.Core.Assets.Editor.Panels;
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
        {
            viewModel.RecentFiles.CollectionChanged -= RecentFilesChanged;
            viewModel.ViewMenu.Items.CollectionChanged -= ViewMenuChanged;
        }
        viewModel = DataContext as MainViewModel;
        if (viewModel is not null)
        {
            viewModel.RecentFiles.CollectionChanged += RecentFilesChanged;
            viewModel.ViewMenu.Items.CollectionChanged += ViewMenuChanged;
        }
        UpdateRecentSessionsMenu();
        UpdateViewMenu();
        UpdateDockWindow();
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        UpdateDockWindow();
    }

    // The floating windows keep the shortcuts of the main window, and show the views of the panels it keeps
    private void UpdateDockWindow()
    {
        if (viewModel is null)
            return;
        viewModel.Panels.Factory.MainWindow = TopLevel.GetTopLevel(this) as MainWindow;
        ControlRecyclingDataTemplate.SetControlRecycling(DockArea, viewModel.Panels.Views);
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

    private void ViewMenuChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateViewMenu();

    /// <summary>
    /// Fills the View menu from the panels: a check box per panel, which shows or hides it, "New ..." and the open instances
    /// of the panels that can be opened several times, separators between the sections, then "Reset layout".
    /// </summary>
    private void UpdateViewMenu()
    {
        var items = ViewMenu.Items;
        items.Clear();
        if (viewModel is null)
            return;

        foreach (var entry in viewModel.ViewMenu.Items)
        {
            if (entry.IsSeparator)
            {
                items.Add(new Separator());
                continue;
            }
            var item = new MenuItem { Header = entry.Header, Tag = entry.PanelId };
            if (entry.IsCheckable)
            {
                item.ToggleType = MenuItemToggleType.CheckBox;
                item[!MenuItem.IsCheckedProperty] = new Binding(nameof(ViewMenuItemViewModel.IsChecked)) { Source = entry, Mode = BindingMode.TwoWay };
            }
            else
            {
                item.Command = entry.Command;
            }
            items.Add(item);
        }
    }

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
