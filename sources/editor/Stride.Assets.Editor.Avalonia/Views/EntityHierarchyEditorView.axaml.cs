// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.AssetEditors.EntityHierarchyEditor.Widgets;
using Stride.Assets.Editor.Commands;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.Avalonia.Converters;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Commands;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;
using Stride.Core.Assets.Editor.Avalonia.Commands;
using Stride.Core.Assets.Editor.Commands;
using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Assets.Editor.Avalonia.Views;

[AssetEditorView<EntityHierarchyEditorViewModel>]
public partial class EntityHierarchyEditorView : UserControl, IAssetEditorView
{
    private static readonly Logger WidgetLog = GlobalLogger.GetLogger(nameof(EntityHierarchyEditorView));

    private KeyModifiers pointerModifiers;
    private EntityHierarchyEditorViewModel? editor;
    private EditorViewToolbarViewModel? toolbar;
    private SceneViewWidgetsViewModel? widgets;
    private readonly Dictionary<SceneViewWidgetViewModel, Control?> widgetViews = [];

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

    /// <summary>
    /// Gets the context menu of the scene view while it is open, or null.
    /// </summary>
    internal ContextMenu? SceneViewContextMenu { get; private set; }

    private const string ToolEntryTag = "ToolEntry";

    private bool sceneViewMenuInitialized;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (editor is not null)
            editor.PropertyChanged -= OnEditorPropertyChanged;
        editor = DataContext as EntityHierarchyEditorViewModel;
        if (editor is not null)
            editor.PropertyChanged += OnEditorPropertyChanged;
        FollowToolbar();
        // One set of widgets per editor, which this view shows
        FollowWidgets(editor?.GetOrCreateSceneViewWidgets());
    }

    private void OnEditorPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(EntityHierarchyEditorViewModel.ViewOptions))
            FollowToolbar();
        else if (e.PropertyName == nameof(EntityHierarchyEditorViewModel.SceneViewWidgets))
            FollowWidgets(editor?.SceneViewWidgets);
    }

    private void FollowWidgets(SceneViewWidgetsViewModel? newWidgets)
    {
        if (widgets == newWidgets)
            return;
        if (widgets is not null)
            widgets.WidgetsChanged -= OnWidgetsChanged;
        widgets = newWidgets;
        if (widgets is not null)
            widgets.WidgetsChanged += OnWidgetsChanged;
        UpdateWidgetViews();
    }

    private void OnWidgetsChanged(object? sender, EventArgs e) => UpdateWidgetViews();

    /// <summary>
    /// Docks the widgets that show on their edges, in order, with the views their plugins register for this editor.
    /// </summary>
    private void UpdateWidgetViews()
    {
        foreach (var panel in new[] { TopWidgets, BottomWidgets, LeftWidgets, RightWidgets })
            panel.Children.Clear();
        var shown = new HashSet<SceneViewWidgetViewModel>();
        if (widgets is not null)
        {
            foreach (var (edge, panel) in new[] { (SceneViewEdge.Top, TopWidgets), (SceneViewEdge.Bottom, BottomWidgets), (SceneViewEdge.Left, LeftWidgets), (SceneViewEdge.Right, RightWidgets) })
            {
                foreach (var widget in widgets.GetShownWidgets(edge))
                {
                    shown.Add(widget);
                    if (!widgetViews.TryGetValue(widget, out var view))
                        widgetViews[widget] = view = CreateWidgetView(widgets, widget);
                    if (view is not null)
                        panel.Children.Add(view);
                }
            }
        }
        // The views of the widgets that left; a hidden widget keeps its view for when it shows again
        foreach (var widget in widgetViews.Keys.Where(x => !shown.Contains(x) && (widgets is null || !widgets.Widgets.Contains(x))).ToList())
        {
            if (widgetViews.Remove(widget, out var view) && view is not null)
                view.DataContext = null;
        }
    }

    // The view of a widget: the one its plugin registers for this editor, created through the guard
    private Control? CreateWidgetView(SceneViewWidgetsViewModel set, SceneViewWidgetViewModel widget)
    {
        if (widget.Content is not { } content || editor?.ServiceProvider.TryGet<IAssetsPluginService>() is not EditorPluginHost host)
            return null;

        var viewType = host.GetViewType(content.GetType(), out var viewOwner);
        var owner = (object?)viewOwner ?? set.GetOwner(widget.Descriptor);
        if (viewType is null)
        {
            WidgetLog.Warning($"The scene view widget [{widget.Id}] of the plugin [{owner?.GetType().FullName}] has no view for this editor.");
            return null;
        }
        return host.TryRun(owner, PluginContributions.View(content.GetType()), () =>
        {
            var control = (Control)Activator.CreateInstance(viewType)!;
            control.DataContext = content;
            return control;
        }, out var created) ? created : null;
    }

    /// <summary>
    /// Gets the views of the widgets docked on the edges of the view, by edge.
    /// </summary>
    internal IReadOnlyDictionary<SceneViewEdge, Panel> WidgetPanels => new Dictionary<SceneViewEdge, Panel>
    {
        [SceneViewEdge.Top] = TopWidgets,
        [SceneViewEdge.Bottom] = BottomWidgets,
        [SceneViewEdge.Left] = LeftWidgets,
        [SceneViewEdge.Right] = RightWidgets,
    };

    // The toolbar of the view raises the context menu requests of the game
    private void FollowToolbar()
    {
        if (toolbar is not null)
            toolbar.ContextMenuRequested -= OnSceneViewContextMenuRequested;
        toolbar = editor?.ViewOptions as EditorViewToolbarViewModel;
        if (toolbar is not null)
            toolbar.ContextMenuRequested += OnSceneViewContextMenuRequested;
    }

    /// <summary>
    /// Opens the context menu of the scene view at the cursor, after a right click without camera drag: the entries of the
    /// active tool, then the entries of the editor (as the menu of the WPF scene view: create an entity at the cursor, and the
    /// actions on the selection), then the commands of the plugins at <see cref="SceneLocations.SceneViewContext"/>, with
    /// the click and the entity under it as their selection. A tool's menu that replaces the others shows alone, and an
    /// empty one keeps the menu closed.
    /// </summary>
    private void OnSceneViewContextMenuRequested(object? sender, EditorViewContextMenuViewModel request)
    {
        SceneViewContextMenu?.Close();
        SceneViewContextMenu = null;
        if (!IsAttachedToVisualTree() || DataContext is not EntityHierarchyEditorViewModel editor || !(request.ToolEntries.Count > 0 || request.ShowsCommands))
            return;

        // One menu, filled again at each request, as the menus of the tree
        var menu = (ContextMenu)Resources["SceneViewMenu"]!;
        if (!sceneViewMenuInitialized)
        {
            sceneViewMenuInitialized = true;
            menu.Closed += (_, _) =>
            {
                if (SceneViewContextMenu == menu)
                    SceneViewContextMenu = null;
            };
        }
        menu.DataContext = editor;
        foreach (var item in menu.Items.OfType<Control>().Where(x => Equals(x.Tag, ToolEntryTag) || CommandSlot.IsSlotItem(x)).ToList())
            menu.Items.Remove(item);
        AddFactoryItems(menu.Items, editor.CreateEntityCommand);
        // The entries of the editor show unless the tool's replace them
        foreach (var item in menu.Items.OfType<Control>().Where(x => !Equals(x.Tag, "Factories")))
            item.IsVisible = request.ShowsCommands;

        var index = 0;
        foreach (var entry in request.ToolEntries)
        {
            menu.Items.Insert(index++, new MenuItem
            {
                Header = entry.Title,
                Command = entry.Command,
                Tag = ToolEntryTag,
                Icon = entry.IconPath is { } path ? new global::Avalonia.Controls.Shapes.Path { Data = new PathDataToGeometry().Convert(path, typeof(Geometry), null, System.Globalization.CultureInfo.InvariantCulture) as Geometry, Width = 16, Height = 16, Stretch = Stretch.None, Fill = Foreground } : null,
            });
        }
        if (index > 0 && request.ShowsCommands)
            menu.Items.Insert(index, new Separator { Tag = ToolEntryTag });

        CommandSlot.SetSelection(menu, request.Selection);
        CommandSlot.SetLocation(menu, request.ShowsCommands ? SceneLocations.SceneViewContext : null);
        SceneViewContextMenu = menu;
        // At the cursor, which the right button was just released over
        menu.Open(GameViewHost);
    }

    private bool IsAttachedToVisualTree() => TopLevel.GetTopLevel(this) is not null;

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
        SceneViewContextMenu?.Close();
        (DataContext as EntityHierarchyEditorViewModel)?.HideGame();
        base.OnDetachedFromVisualTree(e);
    }
}
