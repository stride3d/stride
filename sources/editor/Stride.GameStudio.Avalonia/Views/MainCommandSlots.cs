// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core;
using Stride.GameStudio.Avalonia.ViewModels;

namespace Stride.GameStudio.Avalonia.Views;

/// <summary>
/// The commands and widgets of the plugins (and the editor's own) in the main window: at the end of each main menu, in the
/// Tools menu (shown only when it has commands), on the main toolbar after the build buttons, and their shortcuts, which the
/// main window and the floating windows run after the focused control. It follows the commands and widgets the plugin host
/// registers, the plugins' states, and the session.
/// </summary>
internal sealed class MainCommandSlots : IDisposable
{
    private static readonly Logger Log = GlobalLogger.GetLogger("Commands");

    private readonly MainView view;
    private readonly MainViewModel viewModel;
    private readonly EditorPluginHost host;
    private readonly (MenuItem Menu, string Location)[] menus;
    private readonly List<Control> toolbarItems = [];
    private readonly Dictionary<ToolbarWidgetDescriptor, (IDestroyable ViewModel, Control? View)> widgets = [];
    private IReadOnlyList<(KeyGesture Gesture, DescriptorCommand Command)> shortcuts = [];
    private bool updatePending;
    private bool disposed;

    public MainCommandSlots(MainView view, MainViewModel viewModel, EditorPluginHost host)
    {
        this.view = view;
        this.viewModel = viewModel;
        this.host = host;
        menus =
        [
            (view.FileMenu, CommandLocations.MainMenuFile),
            (view.EditMenu, CommandLocations.MainMenuEdit),
            (view.ViewMenu, CommandLocations.MainMenuView),
            (view.ProjectMenu, CommandLocations.MainMenuProject),
            (view.ToolsMenu, CommandLocations.MainMenuTools),
            (view.HelpMenu, CommandLocations.MainMenuHelp),
        ];
        foreach (var (menu, _) in menus)
            menu.SubmenuOpened += OnSubmenuOpened;
        host.Commands.CollectionChanged += OnContributionsChanged;
        host.ToolbarWidgets.CollectionChanged += OnContributionsChanged;
        host.PluginStatusChanged += OnPluginStatusChanged;
        viewModel.PropertyChanged += OnViewModelPropertyChanged;
        // The buttons of the toolbar ask again whether their command can run after each click and key
        view.AddHandler(InputElement.PointerReleasedEvent, OnInput, RoutingStrategies.Bubble, handledEventsToo: true);
        view.AddHandler(InputElement.KeyUpEvent, OnInput, RoutingStrategies.Bubble, handledEventsToo: true);
        Update();
    }

    /// <summary>
    /// The shortcuts bound to commands now.
    /// </summary>
    public IReadOnlyList<(KeyGesture Gesture, DescriptorCommand Command)> Shortcuts => shortcuts;

    /// <summary>
    /// The widgets shown on the toolbar now, with their view models.
    /// </summary>
    public IReadOnlyDictionary<ToolbarWidgetDescriptor, (IDestroyable ViewModel, Control? View)> Widgets => widgets;

    /// <summary>
    /// Runs the command whose shortcut the key is, if it can run. Called by the main window and the floating windows after
    /// the focused control, so that a text box keeps the keys it uses.
    /// </summary>
    /// <returns>True if the key is the shortcut of a command, which then ran.</returns>
    public bool TryHandleKey(KeyEventArgs e)
    {
        if (e.Handled)
            return false;
        foreach (var (gesture, command) in shortcuts)
        {
            if (!gesture.Matches(e))
                continue;
            if (!command.CanExecute(null))
                return false;
            command.Execute(null);
            e.Handled = true;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Fills the slot of the View menu again, after the view rebuilt the menu's own items.
    /// </summary>
    public void UpdateViewMenu() => FillMenu(view.ViewMenu, CommandLocations.MainMenuView);

    /// <summary>
    /// Fills every slot again from the host now.
    /// </summary>
    public void Update()
    {
        updatePending = false;
        if (disposed)
            return;

        // The shortcuts first: the menu items show them
        UpdateShortcuts();
        foreach (var (menu, location) in menus)
            FillMenu(menu, location);
        UpdateToolbar();
    }

    public void Dispose()
    {
        if (disposed)
            return;
        disposed = true;
        foreach (var (menu, _) in menus)
        {
            menu.SubmenuOpened -= OnSubmenuOpened;
            CommandSlot.Fill(menu.Items, null, string.Empty, () => null);
        }
        view.ToolsMenu.IsVisible = false;
        host.Commands.CollectionChanged -= OnContributionsChanged;
        host.ToolbarWidgets.CollectionChanged -= OnContributionsChanged;
        host.PluginStatusChanged -= OnPluginStatusChanged;
        viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        view.RemoveHandler(InputElement.PointerReleasedEvent, OnInput);
        view.RemoveHandler(InputElement.KeyUpEvent, OnInput);
        ClearToolbar();
        shortcuts = [];
        if (CommandSlot.ShortcutOf == ShortcutOf)
            CommandSlot.ShortcutOf = null;
    }

    private KeyGesture? ShortcutOf(CommandDescriptor descriptor)
    {
        foreach (var (gesture, command) in shortcuts)
        {
            if (command.Descriptor == descriptor)
                return gesture;
        }
        return null;
    }

    // Vertical, as the other separators of the toolbars
    private static Separator CreateToolbarSeparator() => new() { Width = 1, Height = 20, Margin = new Thickness(2, 0) };

    private CommandContext? CreateContext(string location)
    {
        return viewModel.Session is { } session ? new CommandContext { Session = session, Location = location } : null;
    }

    private void FillMenu(MenuItem menu, string location)
    {
        if (disposed)
            return;
        var count = CommandSlot.Fill(menu.Items, host, location, () => CreateContext(location), menu == view.FileMenu ? view.ExitSeparator : null);
        if (menu == view.ToolsMenu)
            menu.IsVisible = count > 0;
    }

    private void UpdateToolbar()
    {
        ClearToolbar(keepWidgets: true);
        var toolbar = view.MainToolBar;
        var context = CreateContext(CommandLocations.MainToolbar);
        if (context is null)
        {
            DestroyWidgets(widgets.Keys.ToList());
            return;
        }

        // The command buttons, in their groups
        foreach (var group in CommandArrangement.Arrange(host.Commands.Items.Where(host.IsCommandAvailable), CommandLocations.MainToolbar))
        {
            toolbarItems.Add(CreateToolbarSeparator());
            foreach (var descriptor in group)
            {
                var button = new Button
                {
                    Command = new DescriptorCommand(host, descriptor, () => CreateContext(CommandLocations.MainToolbar)),
                    Content = CommandSlot.CreateIcon(descriptor.Icon) ?? (object)host.GetCommandTitle(descriptor, context),
                    Tag = descriptor.Id,
                };
                ToolTip.SetTip(button, host.GetCommandTitle(descriptor, context));
                ToolTip.SetShowOnDisabled(button, true);
                toolbarItems.Add(button);
            }
        }

        // The widgets, by order, with the views their plugins register
        var available = host.ToolbarWidgets.Items.Where(host.IsToolbarWidgetAvailable).ToList();
        DestroyWidgets(widgets.Keys.Except(available).ToList());
        var shown = new List<Control>();
        foreach (var descriptor in available.OrderBy(x => x.Order))
        {
            if (!widgets.TryGetValue(descriptor, out var widget))
            {
                if (host.CreateToolbarWidget(descriptor, new ToolbarWidgetContext { Descriptor = descriptor, Session = context.Session, ServiceProvider = context.Session.ServiceProvider }) is not { } widgetViewModel)
                    continue;
                widget = (widgetViewModel, CreateWidgetView(descriptor, widgetViewModel));
                widgets[descriptor] = widget;
            }
            if (widget.View is { } widgetView)
                shown.Add(widgetView);
        }
        if (shown.Count > 0)
        {
            toolbarItems.Add(CreateToolbarSeparator());
            toolbarItems.AddRange(shown);
        }

        foreach (var item in toolbarItems)
            toolbar.Items.Add(item);
    }

    // The view of a widget: the one its plugin registers for this editor, created through the guard
    private Control? CreateWidgetView(ToolbarWidgetDescriptor descriptor, IDestroyable widgetViewModel)
    {
        var viewType = host.GetViewType(widgetViewModel.GetType(), out var viewOwner);
        if (viewType is null)
        {
            Log.Warning($"The toolbar widget [{descriptor.Id}] of the plugin [{host.ToolbarWidgets.GetOwner(descriptor)?.GetType().FullName}] has no view for this editor.");
            return null;
        }
        var owner = (object?)viewOwner ?? host.ToolbarWidgets.GetOwner(descriptor);
        return host.TryRun(owner, PluginContributions.View(widgetViewModel.GetType()), () =>
        {
            var control = (Control)Activator.CreateInstance(viewType)!;
            control.DataContext = widgetViewModel;
            return control;
        }, out var created) ? created : null;
    }

    private void ClearToolbar(bool keepWidgets = false)
    {
        var toolbar = view.MainToolBar;
        foreach (var item in toolbarItems)
            toolbar.Items.Remove(item);
        toolbarItems.Clear();
        if (!keepWidgets)
            DestroyWidgets(widgets.Keys.ToList());
    }

    private void DestroyWidgets(IReadOnlyList<ToolbarWidgetDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
        {
            if (!widgets.Remove(descriptor, out var widget))
                continue;
            if (widget.View is { } widgetView)
                widgetView.DataContext = null;
            host.DestroyToolbarWidget(descriptor, widget.ViewModel);
        }
    }

    private void UpdateShortcuts()
    {
        if (viewModel.Session is null)
        {
            shortcuts = [];
            return;
        }
        var bindings = host.ResolveCommandGestures(view.GetReservedGestures());
        var list = new List<(KeyGesture, DescriptorCommand)>();
        foreach (var (gesture, descriptor) in bindings.Bindings)
        {
            KeyGesture keyGesture;
            try
            {
                keyGesture = KeyGesture.Parse(gesture.ToString());
            }
            catch (Exception e) when (e is ArgumentException or FormatException)
            {
                Log.Warning($"The shortcut [{gesture}] of the command [{descriptor.Id}] is not bound: this editor does not know its key.");
                continue;
            }
            list.Add((keyGesture, new DescriptorCommand(host, descriptor, () => CreateContext(CommandLocations.Shortcut))));
        }
        shortcuts = list;
        CommandSlot.ShortcutOf = ShortcutOf;
    }

    // A menu that opens shows the titles and states of its commands now
    private void OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menu && ReferenceEquals(e.Source, menu))
        {
            foreach (var (slotMenu, location) in menus)
            {
                if (slotMenu == menu)
                    FillMenu(menu, location);
            }
        }
    }

    /// <summary>
    /// Asks the buttons of the toolbar whether their command can run now.
    /// </summary>
    public void RefreshToolbar()
    {
        foreach (var button in toolbarItems.OfType<Button>())
            (button.Command as DescriptorCommand)?.RaiseCanExecuteChanged();
    }

    private void OnInput(object? sender, RoutedEventArgs e) => RefreshToolbar();

    private void OnContributionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScheduleUpdate();

    private void OnPluginStatusChanged(object? sender, PluginStatusChangedEventArgs e) => ScheduleUpdate();

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.Session))
            ScheduleUpdate();
    }

    // The plugins register many commands at once, possibly from another thread: one update after them
    private void ScheduleUpdate()
    {
        if (updatePending || disposed)
            return;
        updatePending = true;
        Dispatcher.UIThread.Post(Update);
    }
}
