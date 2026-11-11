// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Core.Assets.Editor.Editors;
using Stride.UI.Editor.ViewModels;
using Stride.UI.Panels;
using UICanvas = Stride.UI.Panels.Canvas;
using UIStackPanel = Stride.UI.Panels.StackPanel;
namespace Stride.UI.Editor.Avalonia.Views;

// KeyEventArgs alone would be Stride.UI's (a member of an enclosing namespace wins over the usings of the file)
using KeyEventArgs = global::Avalonia.Input.KeyEventArgs;

/// <summary>
/// The view of the UI editors (pages and libraries): the properties button, the toolbox and the
/// visual tree on the left, the zoom and snap toolbar and the game on the right. The editors register their own derived
/// view (<see cref="UIPageEditorView"/>).
/// </summary>
public partial class UIEditorView : UserControl, IAssetEditorView
{
    private INotifyCollectionChanged? observedFactories;

    public UIEditorView()
    {
        InitializeComponent();
        // The editor takes the focus when clicked, so that its shortcuts work
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        HierarchyTree.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);
        HierarchyTree.AddHandler(ContextRequestedEvent, OnTreeContextRequested, RoutingStrategies.Tunnel);
        ViewArea.AddHandler(ContextRequestedEvent, OnViewContextRequested, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Gets the groups of the toolbox, as shown.
    /// </summary>
    public IReadOnlyList<UIElementFactoryGroup> ToolboxGroups => Toolbox.ItemsSource as IReadOnlyList<UIElementFactoryGroup> ?? [];

    /// <summary>
    /// Gets the context menu of the elements, of the tree's empty area and of the view, once opened.
    /// </summary>
    public ContextMenu ElementContextMenu => (ContextMenu)Resources["ElementContextMenu"]!;

    private UIEditorBaseViewModel? Editor => DataContext as UIEditorBaseViewModel;

    /// <inheritdoc />
    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        ObserveFactories();
        // The game renders while its editor is shown
        Editor?.GameHost.ShowGame();
    }

    /// <inheritdoc />
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Editor?.GameHost.HideGame();
        ObserveFactories(stop: true);
        base.OnDetachedFromVisualTree(e);
    }

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (this.IsAttachedToVisualTree())
            ObserveFactories();
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (e.Handled || e.Source is TextBox)
            return;

        // Ctrl+D duplicates the selection; the keys reach the view model after the focused control
        if (e is { Key: Key.D, KeyModifiers: KeyModifiers.Control } && Editor?.DuplicateSelectionCommand is { IsEnabled: true } duplicate)
        {
            duplicate.Execute();
            e.Handled = true;
            return;
        }

        // The arrow keys of the view don't move the focus to the other controls of the editor: the
        // game has seen them already
        if (e.Key is Key.Up or Key.Down or Key.Left or Key.Right && e.Source is Visual source && (ReferenceEquals(source, this) || ViewArea.IsVisualAncestorOf(source)))
            e.Handled = true;
    }

    private void OnPreviewPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!IsKeyboardFocusWithin)
            Focus();
    }

    private void OnTreeKeyDown(object? sender, KeyEventArgs e)
    {
        // Enter and Escape end a rename: the keyboard goes back to the renamed item, as it would otherwise stay with the
        // hidden text box and the keys of the tree (Delete, F2) would do nothing
        if (e.Source is TextBox && e.Key is Key.Enter or Key.Escape)
            Dispatcher.UIThread.Post(() => (HierarchyTree.SelectedItem is { } item ? HierarchyTree.TreeContainerFromItem(item) : null ?? HierarchyTree)?.Focus());
    }

    private void OnTreeContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        // On a row, the menu of its element (the press selected it); on the empty area, the menu of the root of the asset
        var row = (e.Source as Visual)?.FindAncestorOfType<TreeViewItem>(true);
        if ((row?.DataContext as UIHierarchyItemViewModel ?? Editor?.RootPart as UIHierarchyItemViewModel) is { } target)
            OpenElementMenu(HierarchyTree, target, fromView: false);
        e.Handled = true;
    }

    private void OnViewContextRequested(object? sender, ContextRequestedEventArgs e)
    {
        if (Editor is not { } editor || editor.RootPart is not UIHierarchyItemViewModel root)
            return;

        // The elements under the pointer, for "Set current selection": the game finds them
        if (editor.HasGame)
            editor.RefreshSelectableElementsCommand.Execute();
        OpenElementMenu(ViewArea, editor.LastSelectedElement ?? (UIHierarchyItemViewModel)root, fromView: true);
        e.Handled = true;
    }

    /// <summary>
    /// Opens the context menu of an element (or of the root of the asset): the
    /// panel tools of the element, the tools of its parent's layout, the actions on the selection; from the view, also the
    /// elements under the pointer and the view options.
    /// </summary>
    private void OpenElementMenu(Control placementTarget, UIHierarchyItemViewModel target, bool fromView)
    {
        var editor = target.Editor;
        var menu = ElementContextMenu;
        var element = target as UIElementViewModel;
        var panel = element as PanelViewModel;
        var parent = (element?.Parent as UIElementViewModel)?.AssetSideUIElement;
        // From the view, the element part is the last selected element's, when there is one
        var showElementPart = !fromView || element is not null;

        foreach (var item in menu.Items.OfType<Control>())
        {
            var tag = item.Tag as string;
            item.IsVisible = tag switch
            {
                "selection" => fromView && editor.SelectableElements.Count > 0,
                "view" => fromView,
                "panel" or "ungroup" or "changeLayout" => showElementPart && panel is not null,
                "canvas" or "canvasHeader" => showElementPart && parent is UICanvas,
                "grid" or "gridHeader" => showElementPart && parent is GridBase,
                "stack" or "stackHeader" => showElementPart && parent is UIStackPanel,
                "element" => showElementPart && element is not null,
                _ => showElementPart,
            };
            if (item is not MenuItem menuItem)
                continue;

            switch (tag)
            {
                case "selection":
                    // The elements under the pointer; the checked one is the last selected element
                    menuItem.Items.Clear();
                    foreach (var selectable in editor.SelectableElements)
                    {
                        menuItem.Items.Add(new MenuItem
                        {
                            Header = UIEditorBaseViewModel.GetDisplayName(selectable.AssetSideUIElement),
                            ToggleType = MenuItemToggleType.CheckBox,
                            IsChecked = ReferenceEquals(selectable, editor.LastSelectedElement),
                            Command = editor.SetCurrentSelectionCommand,
                            CommandParameter = selectable.Id,
                        });
                    }
                    break;
                case "groupInto":
                    FillFactories(menuItem, editor.PanelFactories, editor.GroupIntoCommand);
                    menuItem.IsEnabled = editor.GroupIntoCommand.IsEnabled && menuItem.Items.Count > 0;
                    break;
                case "changeLayout" when panel is not null:
                    FillFactories(menuItem, editor.PanelFactories, panel.ChangeLayoutTypeCommand);
                    menuItem.IsEnabled = menuItem.Items.Count > 0;
                    break;
                case "ungroup":
                    // A root can't be ungrouped
                    menuItem.IsEnabled = element?.Parent is UIElementViewModel;
                    break;
                case "canvasHeader" or "gridHeader" or "stackHeader":
                    menuItem.Header = parent?.GetType().Name;
                    break;
                case "paste":
                    // On the empty area, the pasted elements become roots
                    menuItem.CommandParameter = target is UIRootViewModel;
                    break;
            }
        }

        menu.DataContext = target;
        // Opened here rather than through the ContextMenu property: a menu set while the event is routed would only open at
        // the next request
        menu.Open(placementTarget);
    }

    private static void FillFactories(MenuItem menuItem, IReadOnlyCollection<IUIElementFactory>? factories, Stride.Core.Presentation.Commands.ICommandBase command)
    {
        menuItem.Items.Clear();
        foreach (var factory in factories ?? [])
        {
            menuItem.Items.Add(new MenuItem { Header = factory.Name, Command = command, CommandParameter = factory });
        }
    }

    private void OnExpandAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in TopLevelItems())
            HierarchyTree.ExpandSubTree(item);
    }

    private void OnCollapseAllClick(object? sender, RoutedEventArgs e)
    {
        foreach (var item in TopLevelItems())
            HierarchyTree.CollapseSubTree(item);
    }

    private IEnumerable<TreeViewItem> TopLevelItems()
    {
        return Enumerable.Range(0, HierarchyTree.ItemCount).Select(HierarchyTree.ContainerFromIndex).OfType<TreeViewItem>().ToList();
    }

    /// <summary>
    /// Follows the factories of the editor (the UI libraries of the session come and go) to rebuild the groups of the toolbox.
    /// </summary>
    private void ObserveFactories(bool stop = false)
    {
        var factories = stop ? null : Editor?.Factories;
        if (!ReferenceEquals(observedFactories, factories))
        {
            if (observedFactories is not null)
                observedFactories.CollectionChanged -= OnFactoriesChanged;
            observedFactories = factories;
            if (observedFactories is not null)
                observedFactories.CollectionChanged += OnFactoriesChanged;
        }
        UpdateToolbox();
    }

    private void OnFactoriesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateToolbox();

    private void UpdateToolbox()
    {
        Toolbox.ItemsSource = observedFactories is IEnumerable<IUIElementFactory> factories ? UIElementFactoryGroup.Create(factories) : null;
    }
}
