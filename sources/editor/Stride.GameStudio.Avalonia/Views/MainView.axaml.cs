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
using Avalonia.Controls.Primitives;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Panels;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core.Extensions;
using Stride.Core.Translation;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Settings;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

public partial class MainView : UserControl
{
    private MainViewModel? viewModel;
    private MainCommandSlots? commandSlots;
    private CommandPaletteViewModel? palette;
    // The control the palette was opened from, which gets the focus back when it closes
    private IInputElement? paletteSource;

    public MainView()
    {
        InitializeComponent();
        // The items of the Edit menu run the commands of the registry, which run the router's on the focused control
        EditRouter = new EditCommandRouter(this);
        CommandPalette.CloseRequested += (_, _) => CommandPalettePopup.IsOpen = false;
        CommandPalette.RunRequested += (_, item) => palette?.RunAsync(item).Forget();
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
        commandSlots?.Dispose();
        commandSlots = null;
        if (viewModel is not null && viewModel.EditRouter == EditRouter)
            viewModel.EditRouter = null;
        if (viewModel is not null && viewModel.ShowCommandPalette == ShowCommandPalette)
            viewModel.ShowCommandPalette = null;
        CommandPalettePopup.IsOpen = false;
        viewModel = DataContext as MainViewModel;
        if (viewModel is not null)
        {
            viewModel.EditRouter = EditRouter;
            viewModel.ShowCommandPalette = ShowCommandPalette;
            viewModel.RecentFiles.CollectionChanged += RecentFilesChanged;
            viewModel.ViewMenu.Items.CollectionChanged += ViewMenuChanged;
            if (viewModel.ServiceProvider.TryGet<IAssetsPluginService>() is EditorPluginHost host)
                commandSlots = new MainCommandSlots(this, viewModel, host);
        }
        UpdateRecentSessionsMenu();
        if (OpenWithIDEButton.Flyout is MenuFlyout openWith)
            FillOpenWithIDEMenu(openWith.Items);
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
    /// The commands and widgets that the editor and the plugins place in the main menus and on the toolbar, and their
    /// shortcuts; null without a plugin host.
    /// </summary>
    internal MainCommandSlots? CommandSlots => commandSlots;

    /// <summary>
    /// The command palette, open or not.
    /// </summary>
    internal Popup CommandPaletteHost => CommandPalettePopup;

    /// <summary>
    /// Opens the command palette over the main window, with the commands that can be listed now.
    /// </summary>
    /// <param name="source">The element it was opened from: the control a key was pressed on (in this window or a floating
    /// one), or null for the control that last had the focus in this window. The commands it runs act there.</param>
    internal void ShowCommandPalette(object? source)
    {
        if (commandSlots is null || viewModel is null)
            return;
        source ??= EditRouter.Target;
        paletteSource = source as IInputElement;
        palette = new CommandPaletteViewModel(viewModel.ServiceProvider, commandSlots.GetPaletteItems(source), GameStudioInternalSettings.RecentCommands.GetValue());
        palette.RecentCommandsChanged += (_, ids) => GameStudioInternalSettings.RecentCommands.SetValue([.. ids]);
        CommandPalette.DataContext = palette;
        CommandPalettePopup.IsOpen = true;
        CommandPalette.FocusSearch();
    }

    private void OnCommandPaletteClosed(object? sender, EventArgs e)
    {
        // The focus goes back where it was, for the command that runs next and for the user
        if (paletteSource is { } source && (source as Visual)?.IsAttachedToVisualTree() == true)
            source.Focus();
        paletteSource = null;
    }

    private void RecentFilesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateRecentSessionsMenu();

    private void ViewMenuChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateViewMenu();

    /// <summary>
    /// Fills the View menu from the panels: a check box per panel, which shows or hides it (the panel's command of the registry), "New ..." and the open instances
    /// of the panels that can be opened several times, separators between the sections, then "Reset layout" and the command palette.
    /// </summary>
    private void UpdateViewMenu()
    {
        var items = ViewMenu.Items;
        items.Clear();
        if (viewModel is null)
            return;
        // The commands placed in the View menu come back after the panels
        try
        {
            FillViewMenu(items, viewModel);
        }
        finally
        {
            commandSlots?.UpdateViewMenu();
        }
    }

    private void FillViewMenu(ItemCollection items, MainViewModel viewModel)
    {
        foreach (var entry in viewModel.ViewMenu.Items)
        {
            if (entry.IsSeparator)
            {
                items.Add(new Separator());
                continue;
            }
            var item = new MenuItem { Header = entry.Header, Tag = entry.PanelId };
            if (entry is { IsCheckable: true, PanelId: { } panelId })
            {
                // The panel's command, checked while the panel is shown (the main window's slots give it its state)
                item.ToggleType = MenuItemToggleType.CheckBox;
                item.IsChecked = entry.IsChecked;
                item.Command = new CommandReference(PanelCommands.IdOf(panelId));
            }
            else
            {
                item.Command = entry.Command;
            }
            items.Add(item);
        }
        // The command palette, after Reset layout
        items.Add(new Separator());
        items.Add(new MenuItem { Name = "CommandPaletteMenuItem", Command = new CommandReference(GameStudioCommands.CommandPaletteId) });
    }

    /// <summary>
    /// Fills the toolbar's Open with drop-down: one item per IDE (<see cref="MainViewModel.AvailableIDEs"/>), each
    /// opening the solution with it (<see cref="Core.Assets.Editor.ViewModels.SessionViewModel.OpenInIDECommand"/> of the session
    /// open then). The items are made with the view model, as those of Open recent, so that the flyout has them when it opens.
    /// </summary>
    internal void FillOpenWithIDEMenu(ItemCollection items)
    {
        items.Clear();
        if (viewModel is null)
            return;
        foreach (var ide in viewModel.AvailableIDEs)
        {
            var item = new MenuItem { Header = ide.DisplayName, CommandParameter = ide };
            item.Bind(MenuItem.CommandProperty, new Binding($"{nameof(MainViewModel.Session)}.{nameof(Core.Assets.Editor.ViewModels.SessionViewModel.OpenInIDECommand)}") { Source = viewModel });
            items.Add(item);
        }
    }

    /// <summary>
    /// Fills File > Open recent: one item per recent session (its version and its path, the full path in the tool
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
