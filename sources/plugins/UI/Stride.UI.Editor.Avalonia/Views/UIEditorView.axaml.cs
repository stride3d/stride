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
namespace Stride.UI.Editor.Views;

// KeyEventArgs alone would be Stride.UI's (a member of an enclosing namespace wins over the usings of the file)
using KeyEventArgs = global::Avalonia.Input.KeyEventArgs;

/// <summary>
/// The view of the UI editors (pages and libraries), as the WPF UIEditorView: the properties button, the toolbox and the
/// visual tree on the left, the zoom and snap toolbar and the game on the right. The editors register their own derived
/// view (<see cref="UIPageEditorView"/>).
/// </summary>
public partial class UIEditorView : UserControl, IAssetEditorView
{
    private INotifyCollectionChanged? observedFactories;

    public UIEditorView()
    {
        InitializeComponent();
        // The editor takes the focus when clicked, so that its shortcuts work, as the WPF UI editor
        AddHandler(PointerPressedEvent, OnPreviewPointerPressed, RoutingStrategies.Tunnel, handledEventsToo: true);
        HierarchyTree.AddHandler(KeyDownEvent, OnTreeKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// Gets the groups of the toolbox, as shown.
    /// </summary>
    public IReadOnlyList<UIElementFactoryGroup> ToolboxGroups => Toolbox.ItemsSource as IReadOnlyList<UIElementFactoryGroup> ?? [];

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

        // Ctrl+D duplicates the selection, as in the WPF UI editor; the keys reach the view model after the focused control
        if (e is { Key: Key.D, KeyModifiers: KeyModifiers.Control } && Editor?.DuplicateSelectionCommand is { IsEnabled: true } duplicate)
        {
            duplicate.Execute();
            e.Handled = true;
            return;
        }

        // The arrow keys of the view don't move the focus to the other controls of the editor, as in the WPF UI editor: the
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
