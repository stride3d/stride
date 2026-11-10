// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Stride.Assets.Editor.ViewModels;
using Stride.Core.Assets.Editor.Annotations;
using Stride.Core.Assets.Editor.Editors;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// The view of the graphics compositor editor, ported from the WPF <c>GraphicsCompositorEditorView</c>: the render stages, the
/// render features and the camera slots on the left, the graph of the entry points and the shared renderers on the right. The
/// property grid shows what is selected in the lists or the graph.
/// </summary>
[AssetEditorView<GraphicsCompositorEditorViewModel>]
public sealed partial class GraphicsCompositorEditorView : UserControl, IAssetEditorView
{
    private GraphicsCompositorGraph? graph;
    private GraphicsCompositorEditorViewModel? editor;

    public GraphicsCompositorEditorView()
    {
        InitializeComponent();
        // F2 renames the render stage or camera slot that has the keyboard, as the WPF editable lists
        RenderStageList.AddHandler(KeyDownEvent, OnRenameListKeyDown, RoutingStrategies.Tunnel);
        CameraSlotList.AddHandler(KeyDownEvent, OnRenameListKeyDown, RoutingStrategies.Tunnel);
    }

    /// <summary>
    /// The links and the commands the node graph shows, for the blocks of the editor.
    /// </summary>
    public GraphicsCompositorGraph? CompositorGraph => graph;

    private GraphicsCompositorEditorViewModel? Editor => DataContext as GraphicsCompositorEditorViewModel;

    /// <inheritdoc />
    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        graph?.Dispose();
        graph = null;
        if (editor is not null)
        {
            editor.RenderFeatureTypes.CollectionChanged -= OnMenuTypesChanged;
            editor.SharedRendererFactories.CollectionChanged -= OnMenuTypesChanged;
        }
        editor = Editor;
        if (editor is not null)
        {
            // The menus follow the types: loaded plugins add some
            editor.RenderFeatureTypes.CollectionChanged += OnMenuTypesChanged;
            editor.SharedRendererFactories.CollectionChanged += OnMenuTypesChanged;

            graph = new GraphicsCompositorGraph(editor.Blocks);
            Graph.Links = graph.Links;
            Graph.LinkResolver = graph;
            Graph.LinkCommand = graph.LinkCommand;
            Graph.UnlinkCommand = graph.UnlinkCommand;
            // The selection of the editor, which fills the property grid
            Graph.SelectedNodes = (IList)editor.SelectedSharedRenderers;
            Graph.SelectedLinks = (IList)editor.SelectedRendererLinks;
        }
        else
        {
            Graph.Links = null;
            Graph.LinkResolver = null;
            Graph.LinkCommand = null;
            Graph.UnlinkCommand = null;
            Graph.SelectedNodes = null;
            Graph.SelectedLinks = null;
        }
        UpdateMenus();
    }

    /// <inheritdoc />
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        // The editor takes the focus so that Delete works, as in WPF
        if (!IsKeyboardFocusWithin)
            Focus();
    }

    private void OnMenuTypesChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateMenus();

    private void UpdateMenus()
    {
        // The menus are filled ahead: a menu filled while it opens shows no item
        var flyout = (MenuFlyout)AddRenderFeatureButton.Flyout!;
        flyout.Items.Clear();
        CreateRendererMenuItem.Items.Clear();
        if (editor is not null)
        {
            // A render feature of each type, as the drop-down of the WPF list
            foreach (var type in editor.RenderFeatureTypes)
                flyout.Items.Add(new MenuItem { Header = type.DisplayValue, Command = editor.AddNewRenderFeatureCommand, CommandParameter = type });
            // A renderer of each shared renderer type, as the Create group of the WPF context menu
            foreach (var factory in editor.SharedRendererFactories)
                CreateRendererMenuItem.Items.Add(new MenuItem { Header = factory.Name, Command = factory.CreateCommand });
        }
        AddRenderFeatureButton.IsEnabled = flyout.Items.Count > 0;
        CreateRendererMenuItem.IsEnabled = CreateRendererMenuItem.Items.Count > 0;
    }

    private void OnRenameItemDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Control item && BeginRename(item))
            e.Handled = true;
    }

    private void OnRenameListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.F2 || e.KeyModifiers != KeyModifiers.None || e.Source is TextBox || sender is not ListBox list)
            return;

        if (list.SelectedItem is { } selected && list.ContainerFromItem(selected) is { } container && BeginRename(container))
            e.Handled = true;
    }

    private static bool BeginRename(Control item)
    {
        if (item.GetSelfAndVisualDescendants().OfType<TextBox>().FirstOrDefault(x => x.Name == "RenameBox") is not { } box)
            return false;

        // The behavior of the box shows the name, selects it and takes the focus
        box.IsVisible = true;
        return true;
    }
}
