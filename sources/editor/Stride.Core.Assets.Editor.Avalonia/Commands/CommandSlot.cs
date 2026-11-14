// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Commands;

/// <summary>
/// The place of a menu where the commands of a location (<see cref="CommandLocations"/>) go, after the menu's own items: a
/// context menu with <see cref="LocationProperty"/> fills it each time it opens, with the selection of its view
/// (<see cref="SelectionProperty"/>); the main window fills the slots of its menus itself (<see cref="Fill"/>). Each time it
/// opens, such a context menu also gives its own items that bind a command by its identifier (<see cref="CommandExtension"/>)
/// their command in its context, their title and their shortcut (<see cref="BindItems"/>).
/// </summary>
[Experimental("STREXP003")]
public static class CommandSlot
{
    /// <summary>
    /// The location whose commands a context menu shows after its own items, each time it has opened.
    /// </summary>
    public static readonly AttachedProperty<string?> LocationProperty =
        AvaloniaProperty.RegisterAttached<ContextMenu, string?>("Location", typeof(CommandSlot));

    /// <summary>
    /// The selection of the view a context menu belongs to: an item, a list of items, or null for none. The commands get it as
    /// <see cref="CommandContext.Selection"/>.
    /// </summary>
    public static readonly AttachedProperty<object?> SelectionProperty =
        AvaloniaProperty.RegisterAttached<ContextMenu, object?>("Selection", typeof(CommandSlot));

    // The items that a slot added to a menu, so that the next fill removes them
    private static readonly AttachedProperty<bool> IsSlotItemProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsSlotItem", typeof(CommandSlot));

    // A menu item whose header is the title of its command, so that the next time it opens it gets the title again
    private static readonly AttachedProperty<bool> HeaderFromCommandProperty =
        AvaloniaProperty.RegisterAttached<MenuItem, bool>("HeaderFromCommand", typeof(CommandSlot));

    static CommandSlot()
    {
        LocationProperty.Changed.AddClassHandler<ContextMenu>((menu, e) =>
        {
            menu.Opened -= OnOpened;
            if (e.NewValue is string)
                menu.Opened += OnOpened;
        });
    }

    public static string? GetLocation(ContextMenu menu) => menu.GetValue(LocationProperty);

    public static void SetLocation(ContextMenu menu, string? value) => menu.SetValue(LocationProperty, value);

    public static object? GetSelection(ContextMenu menu) => menu.GetValue(SelectionProperty);

    public static void SetSelection(ContextMenu menu, object? value) => menu.SetValue(SelectionProperty, value);

    /// <summary>
    /// The shortcut bound to a command, which its menu items show; set by the shell that binds the shortcuts.
    /// </summary>
    public static Func<CommandDescriptor, KeyGesture?>? ShortcutOf { get; set; }

    /// <summary>
    /// Whether an item of a menu was added by a slot.
    /// </summary>
    public static bool IsSlotItem(Control item) => item.GetValue(IsSlotItemProperty);

    /// <summary>
    /// The plugin host of the open session, or null.
    /// </summary>
    public static EditorPluginHost? FindHost(SessionViewModel? session)
    {
        return session?.ServiceProvider.TryGet<IAssetsPluginService>() as EditorPluginHost;
    }

    /// <summary>
    /// Replaces the slot items of <paramref name="items"/> with the commands of <paramref name="location"/> that the host
    /// shows, in their groups: a separator before each group (before the first one only when the menu shows items of its own
    /// above it), then a menu item per command, with its title in context and its icon, enabled when it can run, and checked
    /// when it is a toggle that is on.
    /// </summary>
    /// <param name="items">The items of the menu.</param>
    /// <param name="host">The plugin host, or null to only remove the slot items.</param>
    /// <param name="location">The location.</param>
    /// <param name="getContext">The context of the commands when they run, or null when there is none.</param>
    /// <param name="before">An item of the menu that the commands go before (File > Exit, for instance), or null for the end.</param>
    /// <returns>The number of commands shown.</returns>
    public static int Fill(ItemCollection items, EditorPluginHost? host, string location, Func<CommandContext?> getContext, Control? before = null)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(getContext);

        foreach (var item in items.OfType<Control>().Where(IsSlotItem).ToList())
            items.Remove(item);
        var context = getContext();
        if (host is null || context is null)
            return 0;

        var count = 0;
        foreach (var group in CommandArrangement.Arrange(host.Commands.Items.Where(host.IsCommandAvailable), location))
        {
            var groupItems = new List<MenuItem>();
            foreach (var descriptor in group)
            {
                var header = host.GetCommandTitle(descriptor, context);
                var isChecked = host.IsCommandChecked(descriptor, context);
                // A command turned off by its title or its state in context is not shown
                if (!host.IsCommandAvailable(descriptor))
                    continue;
                var item = new MenuItem
                {
                    Header = header,
                    Command = new DescriptorCommand(host, descriptor, getContext),
                    Tag = descriptor.Id,
                    Icon = CreateIcon(descriptor.Icon),
                    InputGesture = ShortcutOf?.Invoke(descriptor),
                };
                // A toggle shows whether it is on
                if (isChecked is { } on)
                {
                    item.ToggleType = MenuItemToggleType.CheckBox;
                    item.IsChecked = on;
                }
                groupItems.Add(Mark(item));
            }
            if (groupItems.Count == 0)
                continue;
            // A separator before each group, unless it would open the menu
            var index = before is not null && items.IndexOf(before) is >= 0 and var beforeIndex ? beforeIndex : items.Count;
            if (count > 0 || items.Take(index).OfType<Control>().Any(x => x.IsVisible))
                items.Insert(index++, Mark(new Separator()));
            foreach (var item in groupItems)
                items.Insert(index++, item);
            count += groupItems.Count;
        }
        return count;
    }

    /// <summary>
    /// Gives the items of a menu, at any depth, that bind a command by its identifier (<c>{cmd:Command Stride.Edit.Copy}</c>) the
    /// command of the host with that identifier in the menu's context, its title (unless the item has a header of its own), the
    /// shortcut bound to it (<see cref="ShortcutOf"/>) and, for a toggle, its check. An item whose command is not loaded, or
    /// with no host or context, is disabled.
    /// </summary>
    /// <param name="items">The items of the menu.</param>
    /// <param name="host">The plugin host, or null.</param>
    /// <param name="getContext">The context of the commands when they run, or null when there is none.</param>
    public static void BindItems(ItemCollection items, EditorPluginHost? host, Func<CommandContext?> getContext)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(getContext);

        var context = getContext();
        foreach (var item in FindCommandItems(items))
        {
            var reference = (CommandReference)item.Command!;
            var descriptor = host is null || context is null ? null : host.Commands.Items.FirstOrDefault(x => x.Id == reference.Id && host.IsCommandAvailable(x));
            reference.Target = descriptor is null ? null : new DescriptorCommand(host!, descriptor, getContext);
            if (descriptor is null)
            {
                item.InputGesture = null;
                continue;
            }
            // The command's title, unless the item has a header of its own
            if (item.Header is null || item.GetValue(HeaderFromCommandProperty))
            {
                item.Header = host!.GetCommandTitle(descriptor, context!);
                item.SetValue(HeaderFromCommandProperty, true);
            }
            item.InputGesture = ShortcutOf?.Invoke(descriptor);
            if (host!.IsCommandChecked(descriptor, context!) is { } isChecked)
            {
                item.ToggleType = MenuItemToggleType.CheckBox;
                item.IsChecked = isChecked;
            }
        }
    }

    /// <summary>
    /// The view model of the asset editor that contains an element of the view, if any: the editor a context menu or a shortcut
    /// acts in (<see cref="CommandContext.Editor"/>).
    /// </summary>
    public static IAssetEditorViewModel? FindEditor(object? element)
    {
        for (var current = element as Visual; current is not null; current = current.GetVisualParent())
        {
            if (current is StyledElement { DataContext: IAssetEditorViewModel editor })
                return editor;
        }
        return null;
    }

    // The items of a menu bound to a command by its identifier, with those of their submenus
    private static IEnumerable<MenuItem> FindCommandItems(ItemCollection items)
    {
        foreach (var item in items.OfType<MenuItem>())
        {
            if (item.Command is CommandReference)
                yield return item;
            foreach (var child in FindCommandItems(item.Items))
                yield return child;
        }
    }

    /// <summary>
    /// A control that shows the icon of a command: an image of the toolkit, or a control as it is.
    /// </summary>
    public static Control? CreateIcon(object? icon)
    {
        return icon switch
        {
            IImage image => new Image { Source = image, Width = 16, Height = 16 },
            Control control => control,
            _ => null,
        };
    }

    /// <summary>
    /// The selection as a list: the items of an enumerable (not a string), an item alone, or nothing.
    /// </summary>
    public static IReadOnlyList<object> ToSelection(object? selection)
    {
        return selection switch
        {
            null => [],
            string => [selection],
            IEnumerable enumerable => enumerable.Cast<object>().Where(x => x is not null).ToList(),
            _ => [selection],
        };
    }

    private static T Mark<T>(T control) where T : Control
    {
        control.SetValue(IsSlotItemProperty, true);
        return control;
    }

    // When the menu has opened, rather than when it opens: only then is it in the logical tree of its control, so that its
    // bindings (the selection) have their data context and the control it opened on is known
    private static void OnOpened(object? sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || GetLocation(menu) is not { } location)
            return;

        var session = SessionViewModel.CurrentInstance;
        var source = menu.Parent is Popup popup ? popup.PlacementTarget ?? popup.Parent : menu.PlacementTarget ?? menu.Parent;
        // The context of the commands is taken when the menu opens: the selection that the menu was opened on
        var context = session is null ? null : new CommandContext
        {
            Session = session,
            Location = location,
            Selection = ToSelection(GetSelection(menu)),
            Source = source,
            Editor = FindEditor(source),
        };
        var host = FindHost(session);
        BindItems(menu.Items, host, () => context);
        Fill(menu.Items, host, location, () => context);
    }
}
