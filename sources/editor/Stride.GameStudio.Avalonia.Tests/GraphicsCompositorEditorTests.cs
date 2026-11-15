// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Editor.ViewModels.GraphicsCompositorEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Assets.Rendering;
using Stride.Core.Presentation.Avalonia.Controls;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;
using Stride.Rendering;
using Stride.Rendering.Compositing;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The graphics compositor editor in the shell: the graph shows the entry points and the shared renderers with their slots, a drag
/// from an output slot to a renderer links them in one undo entry, a link dragged off its renderer is removed, and the selection
/// of a block fills the property grid and is deleted with the Delete key.
/// </summary>
public sealed class GraphicsCompositorEditorTests
{
    [AvaloniaFact]
    public async Task TheViewShowsTheListsAndTheBlocksWithTheirSlots()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view, graph) = await OpenEditorAsync(fixture, window);

                Assert.Equal(["Opaque", "Transparent"], Part<ListBox>(view, "RenderStageList").Items.OfType<RenderStageViewModel>().Select(x => x.Name));
                Assert.Equal(["MeshRenderFeature"], Part<ListBox>(view, "RenderFeatureList").Items.OfType<RenderFeatureViewModel>().Select(x => x.Title));
                Assert.Equal(["Main"], Part<ListBox>(view, "CameraSlotList").Items.OfType<GraphicsCompositorCameraSlotsViewModel>().Select(x => x.Name));

                // A node for each block, with a connector for each slot
                Assert.Equal(3, editor.Blocks.Count);
                foreach (var block in editor.Blocks.Cast<GraphicsCompositorBlockViewModel>())
                {
                    var container = graph.ContainerFromNode(block);
                    Assert.NotNull(container);
                    Assert.Contains(container.GetVisualDescendants().OfType<TextBlock>(), x => x.Text == block.Title);
                    Assert.All(block.InputSlots, x => Assert.NotNull(graph.ConnectorFromSlot(x, NodeGraphSlotDirection.Input)));
                    Assert.All(block.OutputSlots, x => Assert.NotNull(graph.ConnectorFromSlot(x, NodeGraphSlotDirection.Output)));
                }
                var entryPoints = Assert.IsType<EntryPointBlockViewModel>(editor.Blocks[0]);
                Assert.Equal(["Editor", "Game", "SingleView"], entryPoints.OutputSlots.Select(x => x.Name).Order());
                Assert.Empty(graph.Links!.Cast<object>());

                // The entry points come first, the renderers that nothing references under them
                var entryPosition = graph.GetNodePosition(entryPoints)!.Value;
                Assert.All(editor.Blocks.Skip(1), x => Assert.True(graph.GetNodePosition(x)!.Value.Y > entryPosition.Y));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task DraggingAnEntryPointToARendererLinksThemInOneUndoEntry()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view, graph) = await OpenEditorAsync(fixture, window);
                var asset = (GraphicsCompositorAsset)editor.Asset.Asset;
                var forward = Block<ForwardRenderer>(editor);
                var game = editor.Blocks[0].OutputSlots.Single(x => x.Name == nameof(GraphicsCompositorAsset.Game));
                var names = RecordUndoNames(fixture);

                await DragAsync(window, Center(window, graph.ConnectorFromSlot(game, NodeGraphSlotDirection.Output)!),
                    Center(window, graph.ConnectorFromSlot(forward.InputSlots[0], NodeGraphSlotDirection.Input)!));

                Assert.Same(forward.GetSharedRenderer(), asset.Game);
                Assert.Equal(["Connect Game to "], names);
                var link = Assert.Single(view.CompositorGraph!.Links);
                Assert.Same(game, link.SourceSlot);
                Assert.True(graph.ConnectorFromSlot(game, NodeGraphSlotDirection.Output)!.IsConnected);

                // Undo removes the link from the asset and from the graph; redo brings it back
                fixture.Session.UndoRedoService.Undo();
                await WindowTestHelper.PumpAsync();
                Assert.Null(asset.Game);
                Assert.Empty(view.CompositorGraph.Links);
                Assert.False(graph.ConnectorFromSlot(game, NodeGraphSlotDirection.Output)!.IsConnected);

                fixture.Session.UndoRedoService.Redo();
                await WindowTestHelper.PumpAsync();
                Assert.Same(forward.GetSharedRenderer(), asset.Game);
                Assert.Single(view.CompositorGraph.Links);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ALinkDraggedOffItsRendererIsRemovedAndARefusedLinkIsNotMade()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view, graph) = await OpenEditorAsync(fixture, window);
                var asset = (GraphicsCompositorAsset)editor.Asset.Asset;
                var forward = Block<ForwardRenderer>(editor);
                var game = editor.Blocks[0].OutputSlots.Single(x => x.Name == nameof(GraphicsCompositorAsset.Game));
                game.LinkTo(forward.InputSlots[0]);
                await WindowTestHelper.PumpAsync();
                Assert.Single(view.CompositorGraph!.Links);
                var names = RecordUndoNames(fixture);

                // The input of the forward renderer picks up the link; released on the background, it is removed
                var input = graph.ConnectorFromSlot(forward.InputSlots[0], NodeGraphSlotDirection.Input)!;
                await DragAsync(window, Center(window, input), BottomRight(window, graph));
                Assert.Null(asset.Game);
                Assert.Empty(view.CompositorGraph.Links);
                Assert.Equal(["Clear link"], names);

                // The post effects slot of a forward renderer takes no scene renderer: the drag is refused
                var postEffects = forward.OutputSlots.Single(x => x.Name == nameof(ForwardRenderer.PostEffects));
                var debug = Block<DebugRenderer>(editor);
                await DragAsync(window, Center(window, graph.ConnectorFromSlot(postEffects, NodeGraphSlotDirection.Output)!),
                    Center(window, graph.ConnectorFromSlot(debug.InputSlots[0], NodeGraphSlotDirection.Input)!));
                Assert.Empty(view.CompositorGraph.Links);
                Assert.Equal(["Clear link"], names);
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task ASelectedBlockFillsThePropertyGridAndDeleteRemovesIt()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, _, graph) = await OpenEditorAsync(fixture, window);
                var asset = (GraphicsCompositorAsset)editor.Asset.Asset;
                var debug = Block<DebugRenderer>(editor);
                var names = RecordUndoNames(fixture);

                var title = graph.ContainerFromNode(debug)!.GetVisualDescendants().OfType<TextBlock>().First(x => x.Text == debug.Title);
                var point = Center(window, title);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                await WindowTestHelper.WaitUntilAsync(() => fixture.Session.ActiveProperties.TypeDescription == "Shared renderer", "the properties of the renderer");
                Assert.Equal([debug], editor.SelectedSharedRenderers);
                Assert.True(graph.ContainerFromNode(debug)!.IsSelected);

                // Delete, while the editor has the keyboard
                Assert.True(graph.IsKeyboardFocusWithin, "The graph does not have the keyboard");
                window.KeyPressQwerty(PhysicalKey.Delete, RawInputModifiers.None);
                await WindowTestHelper.PumpAsync();
                Assert.DoesNotContain(asset.SharedRenderers, x => x is DebugRenderer);
                Assert.Null(graph.ContainerFromNode(debug));
                Assert.Equal(2, editor.Blocks.Count);
                Assert.Contains("Delete renderer(s)", names);

                fixture.Session.UndoRedoService.Undo();
                await WindowTestHelper.PumpAsync();
                Assert.Contains(asset.SharedRenderers, x => x is DebugRenderer);
                Assert.Equal(3, editor.Blocks.Count);
                Assert.NotNull(graph.ContainerFromNode(Block<DebugRenderer>(editor)));
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    [AvaloniaFact]
    public async Task TheAddMenusListTheRenderFeatureAndRendererTypes()
    {
        var (fixture, window) = await OpenAsync();
        using (fixture)
        {
            try
            {
                var (editor, view, graph) = await OpenEditorAsync(fixture, window);
                Assert.NotEmpty(editor.RenderFeatureTypes);
                Assert.NotEmpty(editor.SharedRendererFactories);

                // The drop-down of the render features shows a menu item for each type
                var button = Part<DropDownButton>(view, "AddRenderFeatureButton");
                var point = Center(window, button);
                window.MouseDown(point, MouseButton.Left);
                window.MouseUp(point, MouseButton.Left);
                await WindowTestHelper.PumpAsync();
                var flyout = Assert.IsType<MenuFlyout>(button.Flyout);
                Assert.True(flyout.IsOpen);
                var items = flyout.Items.OfType<MenuItem>().ToList();
                Assert.Equal(editor.RenderFeatureTypes.Select(x => x.DisplayValue), items.Select(x => x.Header));
                Assert.All(items, x => Assert.True(x.IsEffectivelyVisible && x.Bounds.Width > 0, $"{x.Header} is not shown"));

                // A click adds a render feature of the type, in one undo entry
                var count = editor.RenderFeatures.Count;
                var names = RecordUndoNames(fixture);
                items[0].Command!.Execute(items[0].CommandParameter);
                await WindowTestHelper.PumpAsync();
                flyout.Hide();
                Assert.Equal(count + 1, editor.RenderFeatures.Count);
                Assert.Single(names);

                // The Create group of the context menu of the graph lists the shared renderer types
                var graphPoint = BottomRight(window, graph);
                window.MouseDown(graphPoint, MouseButton.Right);
                window.MouseUp(graphPoint, MouseButton.Right);
                await WindowTestHelper.PumpAsync();
                var menu = graph.ContextMenu!;
                Assert.True(menu.IsOpen);
                var create = menu.Items.OfType<MenuItem>().Single(x => x.Name == "CreateRendererMenuItem");
                Assert.Equal(editor.SharedRendererFactories.Select(x => x.Name), create.Items.OfType<MenuItem>().Select(x => x.Header));
                menu.Close();
                await WindowTestHelper.PumpAsync();
            }
            finally
            {
                await window.CloseAsync();
            }
        }
    }

    private static async Task<(ShellSessionFixture Fixture, MainWindow Window)> OpenAsync()
    {
        var asset = new GraphicsCompositorAsset();
        asset.RenderStages.Add(new RenderStage("Opaque", "Main"));
        asset.RenderStages.Add(new RenderStage("Transparent", "Transparent"));
        asset.RenderFeatures.Add(new MeshRenderFeature());
        asset.Cameras.Add(new SceneCameraSlot { Name = "Main" });
        asset.SharedRenderers.Add(new ForwardRenderer());
        asset.SharedRenderers.Add(new DebugRenderer());
        var fixture = ShellSessionFixture.Create(("Compositor", asset));
        var window = new MainWindow { DataContext = fixture.Main, Width = 1600, Height = 1000 };
        await window.ShowAsync();
        Assert.True(await fixture.Main.OpenSession(fixture.PackagePath));
        await WindowTestHelper.PumpAsync();
        return (fixture, window);
    }

    /// <summary>
    /// Opens the editor of the compositor, the way a double-click in the asset view does.
    /// </summary>
    private static async Task<(GraphicsCompositorEditorViewModel Editor, GraphicsCompositorEditorView View, NodeGraphControl Graph)> OpenEditorAsync(ShellSessionFixture fixture, Window window)
    {
        var compositor = fixture.Session.AllAssets.OfType<GraphicsCompositorViewModel>().Single();
        fixture.Session.AssetCollection.SelectAssets([compositor]);
        await WindowTestHelper.PumpAsync();
        fixture.Session.EditSelectedContentCommand.Execute();
        await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<GraphicsCompositorEditorView>().Any(), "the compositor editor");

        var editor = Assert.IsType<GraphicsCompositorEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
        var view = window.GetVisualDescendants().OfType<GraphicsCompositorEditorView>().Single();
        var graph = Part<NodeGraphControl>(view, "Graph");
        await WindowTestHelper.WaitUntilAsync(() => editor.Blocks.Count > 0 && editor.Blocks.All(x => graph.GetNodePosition(x) is not null), "the blocks of the graph");
        await WindowTestHelper.PumpAsync();
        return (editor, view, graph);
    }

    private static SharedRendererBlockViewModel Block<T>(GraphicsCompositorEditorViewModel editor)
        => editor.Blocks.OfType<SharedRendererBlockViewModel>().Single(x => x.GetSharedRenderer() is T);

    private static List<string?> RecordUndoNames(ShellSessionFixture fixture)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) =>
        {
            if (e.Transaction.Operations.Count > 0)
                names.Add(undoRedo.GetName(e.Transaction));
        };
        return names;
    }

    private static async Task DragAsync(Window window, Point from, Point to)
    {
        window.MouseDown(from, MouseButton.Left);
        window.MouseMove(from + (to - from) / 2);
        window.MouseMove(to);
        window.MouseUp(to, MouseButton.Left);
        await WindowTestHelper.PumpAsync();
    }

    private static Point Center(Window window, Control control)
        => control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window)!.Value;

    private static Point BottomRight(Window window, Control control)
        => control.TranslatePoint(new Point(control.Bounds.Width - 10, control.Bounds.Height - 10), window)!.Value;

    private static T Part<T>(Control view, string name) where T : Control
        => view.GetVisualDescendants().OfType<T>().Single(x => x.Name == name);
}
