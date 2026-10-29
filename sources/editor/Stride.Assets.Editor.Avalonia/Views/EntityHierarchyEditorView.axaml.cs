// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Commands;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<EntityHierarchyEditorViewModel>]
public partial class EntityHierarchyEditorView : UserControl, IAssetEditorView
{
    public EntityHierarchyEditorView()
    {
        InitializeComponent();
        // The menu depends on what is under the mouse, as in the WPF scene editor: an item pastes into the selection, an
        // empty area of the tree pastes into the root. Tunnel, so that the menu is set before the tree opens it.
        HierarchyTree.AddHandler(ContextRequestedEvent, OnTreeContextRequested, RoutingStrategies.Tunnel);
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

    private void OnTreeContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        var onItem = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(true) is not null;
        var menu = (ContextMenu)Resources[onItem ? "ItemContextMenu" : "RootContextMenu"]!;
        menu.DataContext = DataContext;
        if (DataContext is EntityHierarchyEditorViewModel editor)
            AddFactoryItems(menu.Items, onItem ? editor.CreateEntityInSelectionCommand : editor.CreateEntityInRootCommand);
        HierarchyTree.ContextMenu = menu;
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
