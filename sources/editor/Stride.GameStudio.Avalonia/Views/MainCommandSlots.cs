// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.Presentation.Avalonia.Commands;
using Stride.Core;
using Stride.Core.Settings;
using Stride.GameStudio.Avalonia.Commands;
using Stride.GameStudio.Avalonia.Settings;
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

    // A menu item whose header is the title of its command, so that the next update gives it the title again
    private static readonly AttachedProperty<bool> HeaderFromCommandProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("HeaderFromCommand", typeof(MainCommandSlots));

    private readonly MainView view;
    private readonly MainViewModel viewModel;
    private readonly EditorPluginHost host;
    private readonly (MenuItem Menu, string Location)[] menus;
    private readonly List<Control> toolbarItems = [];
    private readonly Dictionary<ToolbarWidgetDescriptor, (IDestroyable ViewModel, Control? View)> widgets = [];
    // The commands that work without a session, which the window binds itself while none is open
    private readonly IReadOnlyList<(CommandDescriptor Descriptor, ICommand Command)> withoutSession;
    private readonly IReadOnlyList<CommandDescriptor> sessionCommands;
    private IReadOnlyList<BoundShortcut> shortcuts = [];
    // The shortcuts the menu items show: without a session, also those of the commands that wait for one
    private IReadOnlyDictionary<string, KeyGesture> shownGestures = new Dictionary<string, KeyGesture>();
    // The control a key was pressed on, while its shortcut runs
    private object? keySource;
    private bool updatePending;
    private bool disposed;
    private bool followsShortcutOverrides;

    public MainCommandSlots(MainView view, MainViewModel viewModel, EditorPluginHost host)
    {
        this.view = view;
        this.viewModel = viewModel;
        this.host = host;
        withoutSession = GameStudioCommands.CreateWithoutSession(viewModel);
        // The plugin host registers the editor's commands with the session: until then, the menus show them from these
        sessionCommands = GameStudioCommands.Create();
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
        // The settings outlive the window: they are followed while the view is shown only, so that a closed window is collected
        view.AttachedToVisualTree += OnViewAttached;
        view.DetachedFromVisualTree += OnViewDetached;
        if (TopLevel.GetTopLevel(view) is not null)
            FollowShortcutOverrides(true);
        // The buttons of the toolbar ask again whether their command can run after each click and key
        view.AddHandler(InputElement.PointerReleasedEvent, OnInput, RoutingStrategies.Bubble, handledEventsToo: true);
        view.AddHandler(InputElement.KeyUpEvent, OnInput, RoutingStrategies.Bubble, handledEventsToo: true);
        Update();
    }

    /// <summary>
    /// The shortcuts bound to commands now: those of the registry while a session is open, else those of the commands that
    /// work without a session (<see cref="GameStudioCommands.CreateWithoutSession"/>).
    /// </summary>
    public IReadOnlyList<BoundShortcut> Shortcuts => shortcuts;

    /// <summary>
    /// The widgets shown on the toolbar now, with their view models.
    /// </summary>
    public IReadOnlyDictionary<ToolbarWidgetDescriptor, (IDestroyable ViewModel, Control? View)> Widgets => widgets;

    /// <summary>
    /// Runs the command whose shortcut the key is, if it can run. Called by the main window and the floating windows after
    /// the focused control, so that a text box keeps the keys it uses. A shortcut of the editor that has the focus
    /// (<see cref="CommandDescriptor.GestureScope"/>) goes before the main window's.
    /// </summary>
    /// <returns>True if the key is the shortcut of a command, which then ran.</returns>
    public bool TryHandleKey(KeyEventArgs e)
    {
        if (e.Handled)
            return false;
        var editor = FindEditor(e.Source as Visual);
        var match = shortcuts.FirstOrDefault(x => x.Descriptor.GestureScope is { } scope && editor is not null && scope.IsInstanceOfType(editor) && x.Gesture.Matches(e))
            ?? shortcuts.FirstOrDefault(x => x.Descriptor.GestureScope is null && x.Gesture.Matches(e));
        if (match?.Command is not { } command)
            return false;

        // The command acts where the key was pressed (the Edit commands, on that window's focused control)
        keySource = e.Source;
        try
        {
            if (!command.CanExecute(null))
                return false;
            command.Execute(null);
        }
        finally
        {
            keySource = null;
        }
        e.Handled = true;
        return true;
    }

    // The view model of the asset editor that contains a control, if any
    private static IAssetEditorViewModel? FindEditor(Visual? visual)
    {
        for (var current = visual; current is not null; current = current.GetVisualParent())
        {
            if (current is StyledElement { DataContext: IAssetEditorViewModel editor })
                return editor;
        }
        return null;
    }

    /// <summary>
    /// Fills the slot of the View menu again, after the view rebuilt the menu's own items.
    /// </summary>
    public void UpdateViewMenu()
    {
        FillMenu(view.ViewMenu, CommandLocations.MainMenuView);
        UpdateCommandItems(view.ViewMenu, CommandLocations.MainMenuView);
    }

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
        // The items of every menu and the buttons of the toolbar
        UpdateCommandItems();
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
        view.AttachedToVisualTree -= OnViewAttached;
        view.DetachedFromVisualTree -= OnViewDetached;
        FollowShortcutOverrides(false);
        foreach (var reference in FindCommandItems().Select(x => x.Reference))
            reference.Target = null;
        view.RemoveHandler(InputElement.PointerReleasedEvent, OnInput);
        view.RemoveHandler(InputElement.KeyUpEvent, OnInput);
        ClearToolbar();
        shortcuts = [];
        shownGestures = new Dictionary<string, KeyGesture>();
        if (CommandSlot.ShortcutOf == ShortcutOf)
            CommandSlot.ShortcutOf = null;
    }

    private KeyGesture? ShortcutOf(CommandDescriptor descriptor) => shortcuts.FirstOrDefault(x => x.Descriptor == descriptor)?.Gesture;

    /// <summary>
    /// The shortcut bound now to the command with this identifier, or null.
    /// </summary>
    public KeyGesture? ShortcutOf(string id) => shortcuts.FirstOrDefault(x => x.Descriptor.Id == id)?.Gesture;

    // Vertical, as the other separators of the toolbars
    private static Separator CreateToolbarSeparator() => new() { Width = 1, Height = 20, Margin = new Thickness(2, 0) };

    private CommandContext? CreateContext(string location)
    {
        return viewModel.Session is { } session ? new CommandContext { Session = session, Location = location, Source = location == CommandLocations.Shortcut ? keySource : null } : null;
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
        var overrides = GameStudioEditorSettings.ShortcutOverrides.GetValue();
        var list = new List<BoundShortcut>();
        if (viewModel.Session is null)
        {
            // The commands that work without a session, with the user's shortcuts
            var bindings = CommandGestureBindings.Resolve(withoutSession.Select(x => x.Descriptor), _ => true, overrides);
            foreach (var (gesture, descriptor) in bindings.Bindings)
            {
                if (ToKeyGesture(gesture, descriptor) is { } keyGesture)
                    list.Add(new BoundShortcut(keyGesture, descriptor, withoutSession.First(x => x.Descriptor == descriptor).Command));
            }
        }
        else
        {
            // The user's shortcuts first. The editor's own commands (the main menu, the window's shortcuts) are in the registry:
            // no shortcut is reserved
            var bindings = host.ResolveCommandGestures(overrides);
            foreach (var (gesture, descriptor) in bindings.Bindings)
            {
                if (ToKeyGesture(gesture, descriptor) is { } keyGesture)
                    list.Add(new BoundShortcut(keyGesture, descriptor, new DescriptorCommand(host, descriptor, () => CreateContext(CommandLocations.Shortcut))));
            }
        }
        var shown = new Dictionary<string, KeyGesture>();
        if (viewModel.Session is null)
        {
            // The menu items of the commands that wait for a session show the shortcut they will have
            foreach (var (gesture, descriptor) in CommandGestureBindings.Resolve(sessionCommands, _ => true, overrides).Bindings)
            {
                if (ToKeyGesture(gesture, descriptor) is { } keyGesture)
                    shown.TryAdd(descriptor.Id, keyGesture);
            }
        }
        foreach (var shortcut in list)
            shown[shortcut.Descriptor.Id] = shortcut.Gesture;
        shortcuts = list;
        shownGestures = shown;
        CommandSlot.ShortcutOf = ShortcutOf;
    }

    /// <summary>
    /// The commands the command palette lists now, each with its title, its shortcut and its state, and a way to run it through
    /// the guard in the palette's context (<see cref="CommandContext.ForPalette"/>, with <paramref name="source"/>); without a
    /// session, the commands that work without one.
    /// </summary>
    /// <param name="source">The element the palette was opened from: the control that had the focus, in the main window or a
    /// floating window, so that the Edit commands act there.</param>
    public IReadOnlyList<CommandPaletteItem> GetPaletteItems(object? source)
    {
        if (disposed)
            return [];
        if (viewModel.Session is not { } session)
        {
            return [.. withoutSession.Where(x => x.Descriptor.ShowInPalette).Select(x =>
                new CommandPaletteItem(x.Descriptor, x.Descriptor.Title(), ShortcutOf(x.Descriptor.Id)?.ToString("p", null), x.Command.CanExecute(null), null, () =>
                {
                    if (x.Command.CanExecute(null))
                        x.Command.Execute(null);
                    return Task.CompletedTask;
                }))];
        }
        var context = CommandContext.ForPalette(session, null, source);
        var items = new List<CommandPaletteItem>();
        foreach (var descriptor in host.Commands.Items.Where(x => x.ShowInPalette && host.IsCommandAvailable(x)).ToList())
        {
            var title = host.GetCommandTitle(descriptor, context);
            var isEnabled = host.CanExecuteCommand(descriptor, context);
            var isChecked = host.IsCommandChecked(descriptor, context);
            // A command turned off by its title or its state in this context is not listed
            if (!host.IsCommandAvailable(descriptor))
                continue;
            items.Add(new CommandPaletteItem(descriptor, title, ShortcutOf(descriptor.Id)?.ToString("p", null), isEnabled, isChecked, () => host.ExecuteCommandAsync(descriptor, context)));
        }
        return items;
    }

    private static KeyGesture? ToKeyGesture(CommandGesture gesture, CommandDescriptor descriptor)
    {
        try
        {
            return KeyGesture.Parse(gesture.ToString());
        }
        catch (Exception e) when (e is ArgumentException or FormatException)
        {
            Log.Warning($"The shortcut [{gesture}] of the command [{descriptor.Id}] is not bound: this editor does not know its key.");
            return null;
        }
    }

    // The items of the menus and the buttons of the toolbar that bind a command by its identifier ({cmd:Command Stride.File.Save})
    private IEnumerable<(Control Item, CommandReference Reference, string Location)> FindCommandItems()
    {
        foreach (var (menu, location) in menus)
        {
            foreach (var item in FindCommandItems(menu, location))
                yield return item;
        }
        foreach (var button in view.MainToolBar.Items.OfType<Button>())
        {
            if (button.Command is CommandReference reference)
                yield return (button, reference, CommandLocations.MainToolbar);
        }
    }

    private static IEnumerable<(Control Item, CommandReference Reference, string Location)> FindCommandItems(MenuItem menu, string location)
    {
        foreach (var item in menu.Items.OfType<MenuItem>())
        {
            if (item.Command is CommandReference reference)
                yield return (item, reference, location);
            foreach (var child in FindCommandItems(item, location))
                yield return child;
        }
    }

    /// <summary>
    /// Gives the items of a menu (or, with no menu, of every menu and the toolbar) that bind a command by its identifier their
    /// command, their title, their shortcut and their check.
    /// </summary>
    private void UpdateCommandItems(MenuItem? menu = null, string? location = null)
    {
        if (disposed)
            return;
        var items = menu is not null && location is not null ? FindCommandItems(menu, location) : FindCommandItems();
        foreach (var (item, reference, itemLocation) in items.ToList())
        {
            var context = CreateContext(itemLocation);
            CommandDescriptor? descriptor;
            ICommand? target;
            if (context is not null)
            {
                descriptor = host.Commands.Items.FirstOrDefault(x => x.Id == reference.Id && host.IsCommandAvailable(x));
                target = descriptor is null ? null
                    : reference.Target is FollowingCommand { Command: DescriptorCommand current } existing && current.Descriptor == descriptor ? existing
                    : new FollowingCommand(new DescriptorCommand(host, descriptor, () => CreateContext(itemLocation)), GameStudioCommands.StateSource(viewModel, reference.Id));
            }
            else
            {
                // Without a session: the commands that work without one, the others disabled with their title and shortcut
                var entry = withoutSession.FirstOrDefault(x => x.Descriptor.Id == reference.Id);
                descriptor = entry.Descriptor ?? sessionCommands.FirstOrDefault(x => x.Id == reference.Id);
                target = entry.Command;
            }
            reference.Target = target;
            reference.RaiseCanExecuteChanged();

            var title = descriptor is null ? null : context is not null ? host.GetCommandTitle(descriptor, context) : descriptor.Title();
            var gesture = descriptor is null ? null : shownGestures.GetValueOrDefault(descriptor.Id);
            switch (item)
            {
                case MenuItem menuItem:
                    // The command's title, unless the item has a header of its own (for its access key)
                    if (title is not null && (menuItem.Header is null || menuItem.GetValue(HeaderFromCommandProperty)))
                    {
                        menuItem.Header = title;
                        menuItem.SetValue(HeaderFromCommandProperty, true);
                    }
                    menuItem.InputGesture = gesture;
                    if (descriptor is not null && context is not null && host.IsCommandChecked(descriptor, context) is { } isChecked)
                    {
                        menuItem.ToggleType = MenuItemToggleType.CheckBox;
                        menuItem.IsChecked = isChecked;
                    }
                    break;
                case Button button:
                    var tip = reference.ToolTip ?? title;
                    if (tip is not null)
                        // The shortcut as the menus show it
                        ToolTip.SetTip(button, gesture is null ? tip : $"{tip} ({gesture.ToString("p", null)})");
                    break;
            }
        }
    }

    // A menu that opens shows the titles and states of its commands now
    private void OnSubmenuOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is MenuItem menu && ReferenceEquals(e.Source, menu))
        {
            foreach (var (slotMenu, location) in menus)
            {
                if (slotMenu == menu)
                {
                    FillMenu(menu, location);
                    UpdateCommandItems(menu, location);
                }
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
        foreach (var button in view.MainToolBar.Items.OfType<Button>())
            (button.Command as CommandReference)?.RaiseCanExecuteChanged();
    }

    private void OnInput(object? sender, RoutedEventArgs e) => RefreshToolbar();

    private void OnContributionsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ScheduleUpdate();

    private void OnPluginStatusChanged(object? sender, PluginStatusChangedEventArgs e) => ScheduleUpdate();

    // The user's shortcuts changed (the Keyboard shortcuts page): the shortcuts and the menu items follow
    private void OnShortcutOverridesValidated(object? sender, ChangesValidatedEventArgs e) => ScheduleUpdate();

    private void OnViewAttached(object? sender, VisualTreeAttachmentEventArgs e)
    {
        FollowShortcutOverrides(true);
        // A change made while the view was not shown
        ScheduleUpdate();
    }

    private void OnViewDetached(object? sender, VisualTreeAttachmentEventArgs e) => FollowShortcutOverrides(false);

    private void FollowShortcutOverrides(bool follow)
    {
        if (follow == followsShortcutOverrides)
            return;
        followsShortcutOverrides = follow;
        if (follow)
            GameStudioEditorSettings.ShortcutOverrides.ChangesValidated += OnShortcutOverridesValidated;
        else
            GameStudioEditorSettings.ShortcutOverrides.ChangesValidated -= OnShortcutOverridesValidated;
    }

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

    // The command of an item bound by identifier, which also tells its item when the command it runs changes state (the
    // build buttons enable while a build runs, with no input)
    private sealed class FollowingCommand : ICommand
    {
        public FollowingCommand(DescriptorCommand command, ICommand? stateSource)
        {
            Command = command;
            StateSource = stateSource;
        }

        public DescriptorCommand Command { get; }

        public ICommand? StateSource { get; }

        public event EventHandler? CanExecuteChanged
        {
            add
            {
                Command.CanExecuteChanged += value;
                if (StateSource is not null)
                    StateSource.CanExecuteChanged += value;
            }
            remove
            {
                Command.CanExecuteChanged -= value;
                if (StateSource is not null)
                    StateSource.CanExecuteChanged -= value;
            }
        }

        public bool CanExecute(object? parameter) => Command.CanExecute(parameter);

        public void Execute(object? parameter) => Command.Execute(parameter);
    }
}

/// <summary>
/// A shortcut bound to a command in the main window: its keys, the command, and what runs it.
/// </summary>
internal sealed record BoundShortcut(KeyGesture Gesture, CommandDescriptor Descriptor, ICommand Command);
