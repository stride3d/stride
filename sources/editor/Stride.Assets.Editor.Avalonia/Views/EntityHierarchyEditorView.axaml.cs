// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<EntityHierarchyEditorViewModel>]
public partial class EntityHierarchyEditorView : UserControl, IAssetEditorView
{
    private KeyModifiers pointerModifiers;

    public EntityHierarchyEditorView()
    {
        InitializeComponent();
        // The menu depends on what is under the mouse, as in the WPF scene editor: an item pastes into the selection, an
        // empty area of the tree pastes into the root.
        HierarchyTree.AddHandler(ContextRequestedEvent, OnTreeContextRequested, RoutingStrategies.Tunnel);
        // The keys held when a button of a row is clicked (Ctrl locks the children too)
        HierarchyTree.AddHandler(PointerReleasedEvent, (_, e) => pointerModifiers = e.KeyModifiers, RoutingStrategies.Tunnel);
        HierarchyTree.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);
    }

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        // The game renders while its editor is shown
        (DataContext as EntityHierarchyEditorViewModel)?.ShowGame();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        // F frames the last selected entity, from the hierarchy or the view, and Ctrl+D duplicates the selection, as in
        // the WPF scene editor. The keys reach the view model after the focused control, so typing in a text box doesn't
        // move the camera.
        if (e.Handled || e.Source is TextBox || DataContext is not EntityHierarchyEditorViewModel editor)
            return;

        var command = (e.Key, e.KeyModifiers) switch
        {
            (Key.F, KeyModifiers.None) => editor.FocusOnSelectionCommand,
            (Key.D, KeyModifiers.Control) => editor.DuplicateSelectionCommand,
            _ => null,
        };
        if (command is { IsEnabled: true })
        {
            command.Execute();
            e.Handled = true;
        }
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter and Escape end a rename: the keyboard goes back to the renamed item, as it would otherwise stay with the
        // hidden text box and the keys of the tree (Delete, F2) would do nothing
        if (e.Source is TextBox && e.Key is Key.Enter or Key.Escape)
            Dispatcher.UIThread.Post(() => (HierarchyTree.TreeContainerFromItem(HierarchyTree.SelectedItem!) ?? HierarchyTree).Focus());
    }

    private void OnTreeContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        var onItem = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(true) is not null;
        var menu = (ContextMenu)Resources[onItem ? "ItemContextMenu" : "RootContextMenu"]!;
        menu.DataContext = DataContext;
        if (DataContext is EntityHierarchyEditorViewModel editor)
            AddFactoryItems(menu.Items, onItem ? editor.CreateEntityInSelectionCommand : editor.CreateEntityInRootCommand);
        // Opened here rather than through the ContextMenu property: a menu set while the event is routed would only open
        // at the next request
        menu.Open(HierarchyTree);
        e.Handled = true;
    }

    private void OnLockClick(object? sender, RoutedEventArgs e)
    {
        // The button runs its command after this handler
        if (sender is Button button)
            button.CommandParameter = pointerModifiers.HasFlag(KeyModifiers.Control);
    }

    private void OnExpandAllClick(object? sender, RoutedEventArgs e) => SetExpanded(true);

    private void OnCollapseAllClick(object? sender, RoutedEventArgs e) => SetExpanded(false);

    /// <summary>
    /// Expands or collapses every item of the tree; the root stays expanded.
    /// </summary>
    private void SetExpanded(bool isExpanded)
    {
        if (DataContext is not EntityHierarchyEditorViewModel editor)
            return;

        foreach (var item in editor.HierarchyRoot.Children.BreadthFirst(x => x.Children))
        {
            item.IsExpanded = isExpanded;
        }
        editor.HierarchyRoot.IsExpanded = true;
    }

    private void OnCreateFlyoutOpening(object? sender, EventArgs e)
    {
        if (sender is MenuFlyout flyout && DataContext is EntityHierarchyEditorViewModel editor)
            AddFactoryItems(flyout.Items, editor.CreateEntityInRootCommand);
    }

    /// <summary>
    /// Puts the entity factories of the plugins, by category, after the item tagged "Factories" of a menu, as the
    /// "Create" menus of the WPF scene editor. The menu is built when it opens: the plugins can register factories at
    /// any time.
    /// </summary>
    private static void AddFactoryItems(ItemCollection items, ICommandBase createCommand)
    {
        var marker = items.OfType<Control>().First(x => Equals(x.Tag, "Factories"));
        var index = items.IndexOf(marker) + 1;
        while (index < items.Count && items[index] is Control { Tag: "Factory" })
            items.RemoveAt(index);

        foreach (var category in StrideEditorPlugin.EntityFactoryCategories.ToList())
        {
            var categoryItem = new MenuItem { Header = category.Name, Tag = "Factory" };
            foreach (var factory in category.Factories)
            {
                categoryItem.Items.Add(new MenuItem { Header = factory.Name, Command = createCommand, CommandParameter = factory.Factory });
            }
            items.Insert(index++, categoryItem);
        }
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        (DataContext as EntityHierarchyEditorViewModel)?.HideGame();
        base.OnDetachedFromVisualTree(e);
    }
}
