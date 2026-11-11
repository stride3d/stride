// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Stride.Assets.Editor.Avalonia.Views;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Editor.Avalonia.DragDrop;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Quantum;
using Stride.Engine;
using Stride.GameStudio.Avalonia.Tests.Helpers;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Tests;

/// <summary>
/// The drag and drop in the tree of the scene editor: a drop on a row adds as children, a drop
/// between two rows inserts there, a drop on the empty area adds assets to the scene, and a refused drop says why and
/// changes nothing. Each drop is one entry of the undo history.
/// </summary>
/// <remarks>
/// The tests have no game (the previews are disabled), therefore the view of the game is empty: its drop target is checked
/// through its behavior.
/// </remarks>
public sealed class SceneTreeDropTests
{
    [AvaloniaFact]
    public async Task AnEntityDroppedOnAnotherBecomesItsLastChildInOneTransaction()
    {
        await using var scene = await OpenSceneAsync();
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;

        Drop(scene, [scene.Entity("Child")], scene.RowCenter("Box"));

        var box = scene.Entity("Box");
        Assert.Equal(["Child"], box.TransformChildren.Select(x => x.Name));
        Assert.Empty(scene.Entity("Parent").TransformChildren);
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");

        history.UndoCommand.Execute();
        Assert.Empty(scene.Entity("Box").TransformChildren);
        Assert.Equal(["Child"], scene.Entity("Parent").TransformChildren.Select(x => x.Name));
        history.RedoCommand.Execute();
        Assert.Equal(["Child"], scene.Entity("Box").TransformChildren.Select(x => x.Name));
    }

    [AvaloniaFact]
    public async Task AnEntityDroppedOnTheTopBandOfARowIsInsertedBeforeIt()
    {
        await using var scene = await OpenSceneAsync();
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        var behavior = scene.Behavior;
        var position = scene.RowTopBand("Box");

        var target = behavior.GetDropTarget([scene.Entity("Child")], position, AddChildModifiers.None);
        Assert.Equal(TreeViewDropKind.Insert, target.Kind);
        Assert.Equal(InsertPosition.Before, target.InsertPosition);
        Assert.Equal(DropAcceptance.Accepted, target.Acceptance);
        Assert.StartsWith("Insert before Box", target.Message);
        Assert.NotNull(target.InsertLine);

        Drop(scene, [scene.Entity("Child")], position);

        Assert.Equal(["Child", "Box", "Parent"], scene.Editor.HierarchyRoot.Children.Select(x => x.Name));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.Equal(["Box", "Parent"], scene.Editor.HierarchyRoot.Children.Select(x => x.Name));
    }

    [AvaloniaFact]
    public async Task AnEntityDroppedOnTheBottomBandOfTheLastRowOfASubtreeGoesAfterTheItemThePointerPoints()
    {
        await using var scene = await OpenSceneAsync();
        var behavior = scene.Behavior;
        // Child is the last child of Parent, the last child of the root: below Child, the indentation of the pointer chooses
        var child = scene.Row("Child");
        var bottom = scene.RowBottomBand("Child");

        var deep = behavior.GetDropTarget([scene.Entity("Box")], bottom, AddChildModifiers.None);
        Assert.Equal(TreeViewDropKind.Insert, deep.Kind);
        Assert.Equal(InsertPosition.After, deep.InsertPosition);
        Assert.Same(child, deep.Item);

        // Left of Child's indentation, but right of Parent's: after Parent
        var shallowX = scene.Indent("Parent") + 1;
        Assert.True(shallowX < scene.Indent("Child"));
        var shallow = behavior.GetDropTarget([scene.Entity("Box")], bottom.WithX(shallowX), AddChildModifiers.None);
        Assert.Equal(TreeViewDropKind.Insert, shallow.Kind);
        Assert.Same(scene.Row("Parent"), shallow.Item);

        Drop(scene, [scene.Entity("Box")], bottom.WithX(shallowX));
        // After Parent, which is the last entity of the root
        Assert.Equal(["Parent", "Box"], scene.Editor.HierarchyRoot.Children.Select(x => x.Name));
    }

    [AvaloniaFact]
    public async Task ARefusedDropSaysWhyAndChangesNothing()
    {
        await using var scene = await OpenSceneAsync();
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        var parent = scene.Entity("Parent");
        var position = scene.RowCenter("Child");

        var target = scene.Behavior.GetDropTarget([parent], position, AddChildModifiers.None);
        Assert.Equal(DropAcceptance.Rejected, target.Acceptance);
        Assert.Equal("Can't drop an entity on itself or its children", target.Message);

        // While the entity is over the row, the tree shows the message next to the pointer
        var data = AssetDragDropHelper.CreateItemData([parent]);
        var point = scene.ToWindow(position);
        scene.Window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Move, RawInputModifiers.None);
        scene.Window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Move, RawInputModifiers.None);
        var feedback = DragFeedbackAdorner.Get(scene.Tree);
        Assert.NotNull(feedback);
        Assert.Equal(DropAcceptance.Rejected, feedback.Acceptance);
        Assert.Equal("Can't drop an entity on itself or its children", feedback.Message);

        scene.Window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Move, RawInputModifiers.None);
        Assert.Null(DragFeedbackAdorner.Get(scene.Tree));
        Assert.False(scene.Behavior.IsAutoScrolling);
        await Task.Delay(200);
        await WindowTestHelper.PumpAsync();
        Assert.Equal(["Child"], scene.Entity("Parent").TransformChildren.Select(x => x.Name));
        Assert.Equal(count, history.Transactions.Count);
    }

    [AvaloniaFact]
    public async Task TheEmptyAreaTakesAssetsIntoTheSceneAndRefusesEntities()
    {
        await using var scene = await OpenSceneAsync();
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        var empty = scene.EmptyArea();

        // The empty area belongs to the editor, which takes only assets
        var entityTarget = scene.Behavior.GetDropTarget([scene.Entity("Box")], empty, AddChildModifiers.None);
        Assert.Equal(DropAcceptance.Rejected, entityTarget.Acceptance);
        Assert.Equal("Only assets can be dropped here", entityTarget.Message);

        Drop(scene, [scene.Fixture.Asset("Prefab")], empty);

        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        Assert.Equal(["Box", "Parent", "Prefab"], scene.Editor.HierarchyRoot.Children.Select(x => x.Name));
        Assert.Equal(["PrefabEntity"], scene.Entity("Prefab").TransformChildren.Select(x => x.Name));
        history.UndoCommand.Execute();
        Assert.Equal(["Box", "Parent"], scene.Editor.HierarchyRoot.Children.Select(x => x.Name));
    }

    [AvaloniaFact]
    public async Task ASceneDroppedOnTheSceneRootBecomesAChildScene()
    {
        await using var scene = await OpenSceneAsync();
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        var child = scene.Fixture.Asset("ChildScene");

        // A scene doesn't go into an entity
        var onEntity = scene.Behavior.GetDropTarget([child], scene.RowCenter("Box"), AddChildModifiers.None);
        Assert.Equal(DropAcceptance.Rejected, onEntity.Acceptance);

        Drop(scene, [child], scene.RowCenter("MainScene"));

        var root = Assert.IsType<SceneRootViewModel>(scene.Editor.HierarchyRoot);
        Assert.Equal(["ChildScene"], root.ChildScenes.Select(x => x.Name));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        history.UndoCommand.Execute();
        Assert.Empty(root.ChildScenes);
    }

    [AvaloniaFact]
    public async Task ADragMovesTheSelectionWhenItStartsOnIt()
    {
        await using var scene = await OpenSceneAsync();
        var box = scene.Entity("Box");
        var parent = scene.Entity("Parent");
        var child = scene.Entity("Child");

        scene.Editor.ClearSelection();
        scene.Editor.SelectedContent.AddRange([box, child]);
        await WindowTestHelper.PumpAsync();
        Assert.Equal([box, child], scene.Behavior.GetItemsToDrag(scene.Row("Child")));
        // An item out of the selection goes alone
        Assert.Equal([parent], scene.Behavior.GetItemsToDrag(scene.Row("Parent")));

        // An item and one of its parents can't be dragged together
        scene.Editor.SelectedContent.Add(parent);
        await WindowTestHelper.PumpAsync();
        Assert.Empty(scene.Behavior.GetItemsToDrag(scene.Row("Child")));

        // Both entities move in one transaction
        scene.Editor.ClearSelection();
        scene.Editor.SelectedContent.AddRange([box, child]);
        await WindowTestHelper.PumpAsync();
        var history = scene.Fixture.Session.ActionHistory;
        var count = history.Transactions.Count;
        Drop(scene, scene.Behavior.GetItemsToDrag(scene.Row("Box")), scene.RowCenter("MainScene"));
        await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry");
        Assert.Equal(["Parent", "Box", "Child"], scene.Editor.HierarchyRoot.Children.Select(x => x.Name));
    }

    [AvaloniaFact]
    public async Task APressOnAMultipleSelectionKeepsItUntilTheRelease()
    {
        await using var scene = await OpenSceneAsync();
        var box = scene.Entity("Box");
        var parent = scene.Entity("Parent");
        scene.Editor.ClearSelection();
        scene.Editor.SelectedContent.AddRange([box, parent]);
        await WindowTestHelper.PumpAsync();
        var point = scene.ToWindow(scene.NameCenter("Box"));

        scene.Window.MouseMove(point);
        scene.Window.MouseDown(point, MouseButton.Left);
        await WindowTestHelper.PumpAsync();
        // The press keeps the selection, so that a drag would move both entities
        Assert.Equal([box, parent], scene.Editor.SelectedContent);

        scene.Window.MouseUp(point, MouseButton.Left);
        await WindowTestHelper.PumpAsync();
        // A release without drag selects the item alone, as a click does
        Assert.Equal([box], scene.Editor.SelectedContent);
    }

    [AvaloniaFact]
    public async Task TheViewOfTheGameTakesAssetsAndRefusesEntities()
    {
        await using var scene = await OpenSceneAsync();
        var host = scene.View.GetVisualDescendants().OfType<ContentControl>().Single(x => x.Name == "GameViewHost");
        var behavior = global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(host).OfType<AddChildDropBehavior>().Single();

        Assert.Null(behavior.GetDropTarget([scene.Entity("Box")], AddChildModifiers.None, out var message));
        Assert.Equal("Only assets can be dropped here", message);
        Assert.Same(scene.Editor, behavior.GetDropTarget([scene.Fixture.Asset("Prefab")], AddChildModifiers.None, out _));
    }

    private static void Drop(SceneFixture scene, IReadOnlyCollection<object> items, Point positionInTree)
    {
        var point = scene.ToWindow(positionInTree);
        var data = AssetDragDropHelper.CreateItemData(items);
        scene.Window.DragDrop(point, RawDragEventType.DragEnter, data, DragDropEffects.Move, RawInputModifiers.None);
        scene.Window.DragDrop(point, RawDragEventType.DragOver, data, DragDropEffects.Move, RawInputModifiers.None);
        scene.Window.DragDrop(point, RawDragEventType.Drop, data, DragDropEffects.Move, RawInputModifiers.None);
    }

    private const string PrefabYaml = """
        !PrefabAsset
        Id: 6a1c3c0e-0d4b-4c43-9a55-1f3f2b0c8e01
        SerializedVersion: {Stride: 3.1.0.1}
        Tags: []
        Hierarchy:
            RootParts:
                - ref!! 2b7b9b43-5a83-4f0e-9a8e-8f6e3c7d1a02
            Parts:
                -   Entity:
                        Id: 2b7b9b43-5a83-4f0e-9a8e-8f6e3c7d1a02
                        Name: PrefabEntity
                        Components:
                            60c67fbd4a5dbcdf4c6d6286a96f4143: !TransformComponent
                                Id: 46333eab-2f75-4e90-a5fa-3240df14695c
                                Position: {X: 0.0, Y: 0.0, Z: 0.0}
                                Rotation: {X: 0.0, Y: 0.0, Z: 0.0, W: 1.0}
                                Scale: {X: 1.0, Y: 1.0, Z: 1.0}
                                Children: {}
        """;

    /// <summary>
    /// Opens a session with a scene, a second scene and a prefab, then the editor of the scene with three entities: Box,
    /// and Parent with its child Child.
    /// </summary>
    internal static async Task<SceneFixture> OpenSceneAsync()
    {
        var fixture = ShellSessionFixture.Create(("MainScene", new SceneAsset()), ("ChildScene", new SceneAsset()), ("Prefab", new PrefabAsset()));
        // The root parts of a prefab are references to its parts, which only the asset serialization of the session writes:
        // the prefab with its entity is written by hand
        File.WriteAllText(Path.Combine(fixture.PackageDirectory, "Assets", "Prefab.sdprefab"), PrefabYaml);
        // A dialog of the session would wait forever for an answer in a headless test
        fixture.ReplaceDialogService();
        await fixture.OpenAsync();
        var window = new MainWindow { DataContext = fixture.Main, Width = 1600, Height = 1000 };
        try
        {
            await window.ShowAsync();
            var sceneAsset = (SceneViewModel)fixture.Asset("MainScene");
            fixture.Session.AssetCollection.SelectAssets([sceneAsset]);
            await WindowTestHelper.PumpAsync();
            fixture.Session.EditSelectedContentCommand.Execute();
            await WindowTestHelper.WaitUntilAsync(() => window.GetVisualDescendants().OfType<EntityHierarchyEditorView>().Any(), "the scene editor");
            var editor = Assert.IsType<SceneEditorViewModel>(fixture.Session.EditorCollection.ActiveEditor);
            var view = window.GetVisualDescendants().OfType<EntityHierarchyEditorView>().Single();
            await WindowTestHelper.WaitUntilAsync(() => editor.HierarchyRoot is not null, "the hierarchy of the scene");

            // The entities go through the property graph, as the editor adds them
            var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)sceneAsset.PropertyGraph!;
            var history = fixture.Session.ActionHistory;
            var count = history.Transactions.Count;
            using (fixture.Session.UndoRedoService.CreateTransaction())
            {
                var box = new EntityDesign(new Entity("Box"));
                var parent = new EntityDesign(new Entity("Parent"));
                var child = new EntityDesign(new Entity("Child"));
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { box }, box, null, 0);
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { parent }, parent, null, 1);
                graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { child }, child, parent.Entity, 0);
            }
            // The history shows the transaction later: a test that counts the entries of its drop starts after it
            await WindowTestHelper.WaitUntilAsync(() => history.Transactions.Count == count + 1, "the history entry of the entities");

            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            var tree = view.FindControl<TreeView>("HierarchyTree") ?? throw new InvalidOperationException("No tree in the scene editor.");
            var behavior = global::Avalonia.Xaml.Interactivity.Interaction.GetBehaviors(tree).OfType<TreeViewDragDropBehavior>().Single();
            var scene = new SceneFixture(fixture, window, editor, view, tree, behavior);
            await WindowTestHelper.WaitUntilAsync(() =>
            {
                foreach (var item in tree.GetVisualDescendants().OfType<TreeViewItem>())
                    item.IsExpanded = true;
                return scene.TryRow("Child") is not null;
            }, "the rows of the entities");
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            return scene;
        }
        catch
        {
            await window.CloseAsync();
            fixture.Dispose();
            throw;
        }
    }

    internal sealed class SceneFixture(ShellSessionFixture fixture, MainWindow window, SceneEditorViewModel editor, EntityHierarchyEditorView view, TreeView tree, TreeViewDragDropBehavior behavior) : IAsyncDisposable
    {
        public ShellSessionFixture Fixture { get; } = fixture;

        public MainWindow Window { get; } = window;

        public SceneEditorViewModel Editor { get; } = editor;

        public EntityHierarchyEditorView View { get; } = view;

        public TreeView Tree { get; } = tree;

        public TreeViewDragDropBehavior Behavior { get; } = behavior;

        public EntityViewModel Entity(string name)
        {
            var entities = new List<EntityViewModel>();
            var queue = new Queue<EntityViewModel>(Editor.HierarchyRoot.InnerSubEntities);
            while (queue.TryDequeue(out var entity))
            {
                entities.Add(entity);
                foreach (var child in entity.TransformChildren)
                    queue.Enqueue(child);
            }
            return entities.Single(x => x.Name == name);
        }

        public TreeViewItem? TryRow(string name)
        {
            return Tree.GetVisualDescendants().OfType<TreeViewItem>().FirstOrDefault(x => x.DataContext is EntityHierarchyItemViewModel item && item.Name == name);
        }

        public TreeViewItem Row(string name) => TryRow(name) ?? throw new InvalidOperationException($"No row {name} in the tree.");

        /// <summary>
        /// Gets the row of an item without its children, relative to the tree.
        /// </summary>
        public Rect Header(string name)
        {
            var item = Row(name);
            var header = item.GetVisualDescendants().OfType<Control>().FirstOrDefault(x => x.Name == "PART_Header" && ReferenceEquals(x.TemplatedParent, item)) ?? item;
            var origin = header.TranslatePoint(default, Tree) ?? throw new InvalidOperationException("The row is not in the tree.");
            var text = item.GetVisualDescendants().OfType<TextBlock>().First(x => x.Text == name);
            var textX = text.TranslatePoint(default, Tree)!.Value.X;
            return new Rect(textX, origin.Y, Math.Max(1, Tree.Bounds.Width - textX - 60), header.Bounds.Height);
        }

        public Point RowCenter(string name) => Header(name).Center;

        /// <summary>
        /// Gets the center of the name of a row, relative to the tree: where a click selects it (the buttons of the row
        /// take the clicks on their side).
        /// </summary>
        public Point NameCenter(string name)
        {
            var text = Row(name).GetVisualDescendants().OfType<TextBlock>().First(x => x.Text == name);
            return text.TranslatePoint(new Point(text.Bounds.Width / 2, text.Bounds.Height / 2), Tree) ?? throw new InvalidOperationException("The row is not in the tree.");
        }

        public Point RowTopBand(string name) => Header(name).TopLeft + new Point(10, 1);

        public Point RowBottomBand(string name) => Header(name).BottomLeft + new Point(10, -1);

        public Point EmptyArea()
        {
            var last = Header("Child");
            return new Point(last.X, last.Bottom + 40);
        }

        /// <summary>
        /// Gets where the content of a row starts, relative to the tree: the indentation of its level.
        /// </summary>
        public double Indent(string name)
        {
            var item = Row(name);
            var presenter = item.GetVisualDescendants().OfType<Control>().First(x => x.Name == "PART_HeaderPresenter" && ReferenceEquals(x.TemplatedParent, item));
            return presenter.TranslatePoint(default, Tree)!.Value.X;
        }

        public Point ToWindow(Point positionInTree) => Tree.TranslatePoint(positionInTree, Window) ?? throw new InvalidOperationException("The tree is not in the window.");

        public async ValueTask DisposeAsync()
        {
            await Window.CloseAsync();
            Fixture.Dispose();
        }
    }
}
