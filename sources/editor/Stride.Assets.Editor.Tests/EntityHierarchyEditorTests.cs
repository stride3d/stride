// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.AssetEditors.GameEditor;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Quantum;
using Stride.Core.Mathematics;
using Stride.Core.Presentation.Services;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The hierarchy of the scene editor follows the entities the asset gets and loses (undo and redo included), and the
/// transformations made in the game come back to the asset as one undoable operation.
/// </summary>
public sealed class EntityHierarchyEditorTests
{
    [Fact]
    public async Task AnEntityAddedToTheSceneGetsAViewModelAndIsAnnounced()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var added = Record(editor.HierarchyRoot, out _);

        var entity = AddEntity(fixture, editor, "New", parent: null, index: 1);

        var viewModel = Assert.IsType<EntityViewModel>(editor.HierarchyRoot.Children[1]);
        Assert.Same(entity, viewModel.AssetSideEntity);
        Assert.Equal("New", viewModel.Name);
        Assert.Same(editor.HierarchyRoot, viewModel.Parent);
        var e = Assert.Single(added);
        Assert.Same(editor.HierarchyRoot, e.Parent);
        Assert.Same(viewModel, e.Entity);
        Assert.Equal(1, e.Index);
        editor.Destroy();
    }

    [Fact]
    public async Task AChildEntityGoesUnderTheViewModelOfItsParent()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        var added = Record(editor.HierarchyRoot, out _);

        AddEntity(fixture, editor, "Second child", parent.AssetSideEntity, index: 1);

        Assert.Equal(["Child", "Second child"], parent.Children.Select(x => x.Name));
        var e = Assert.Single(added);
        Assert.Same(parent, e.Parent);
        Assert.Equal("Second child", e.Entity.Name);
        editor.Destroy();
    }

    [Fact]
    public async Task ADeletedEntityLeavesTheTreeAndTheSelectionWithItsChildren()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        var child = FindEntity(editor, "Child");
        editor.SelectedContent.Add(child);
        Record(editor.HierarchyRoot, out var removed);

        DeleteEntity(fixture, editor, parent.AssetSideEntity);

        Assert.DoesNotContain(editor.HierarchyRoot.Children, x => x.Name == "Parent");
        Assert.Empty(editor.SelectedContent);
        Assert.Empty(editor.SelectedItems);
        // The children leave first, then their parent
        Assert.Equal([("Parent", "Child"), ("SceneRoot", "Parent")], removed.Select(x => (x.Parent.Name, x.Entity.Name)));
        editor.Destroy();
    }

    [Fact]
    public async Task UndoingADeletionBringsTheEntityBackAtItsPlace()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        DeleteEntity(fixture, editor, parent.AssetSideEntity);
        var added = Record(editor.HierarchyRoot, out _);

        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        var restored = FindEntity(editor, "Parent");
        Assert.NotSame(parent, restored);
        Assert.Equal(["Child"], restored.Children.Select(x => x.Name));
        Assert.Equal(["Parent", "Child"], added.Select(x => x.Entity.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task TheTransformationsOfAGizmoMoveAreOneUndoableOperation()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        var parent = FindEntity(editor, "Parent");
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
        var rotation = Quaternion.RotationY(MathUtil.PiOverTwo);

        editor.UpdateTransformations(new Dictionary<Core.AbsoluteId, TransformationTRS>
        {
            [box.Id] = new() { Position = new Vector3(1, 2, 3), Rotation = Quaternion.Identity, Scale = Vector3.One },
            [parent.Id] = new() { Position = Vector3.Zero, Rotation = rotation, Scale = new Vector3(2) },
        });

        Assert.Equal(["Update transformation"], names);
        Assert.Equal(new Vector3(1, 2, 3), box.AssetSideEntity.Transform.Position);
        Assert.Equal(rotation, parent.AssetSideEntity.Transform.Rotation);
        Assert.Equal(new Vector3(2), parent.AssetSideEntity.Transform.Scale);

        undoRedo.Undo();

        Assert.Equal(Vector3.Zero, box.AssetSideEntity.Transform.Position);
        Assert.Equal(Quaternion.Identity, parent.AssetSideEntity.Transform.Rotation);
        Assert.Equal(Vector3.One, parent.AssetSideEntity.Transform.Scale);
        editor.Destroy();
    }

    [Fact]
    public async Task WithoutAGameTheCameraCannotFrameTheSelection()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");

        editor.SelectedContent.Add(box);

        // The session of the tests has no game viewport: the editor works without a game
        Assert.False(editor.FocusOnSelectionCommand.IsEnabled);
        Assert.False(editor.FocusOnEntityCommand.IsEnabled);
        await editor.FocusOnEntityAsync(box);
        editor.Destroy();
    }

    /// <summary>
    /// A scene with a "Box" root entity, and a "Parent" root entity that has a "Child".
    /// </summary>
    internal static async Task<AssetsSessionFixture> OpenSceneAsync(IDispatcherService? dispatcher = null)
    {
        var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: true, dispatcher ?? new NullDispatcherService(), ("MainScene", new SceneAsset()));
        Assert.False(fixture.Result.HasErrors, fixture.Log);

        // The entities go through the property graph, as the editor adds them: a scene saved by the test would need the
        // metadata of its references (the transform of a child is a reference)
        var graph = (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph;
        using (fixture.Session.UndoRedoService.CreateTransaction())
        {
            var box = new EntityDesign(new Entity("Box"));
            var parent = new EntityDesign(new Entity("Parent"));
            var child = new EntityDesign(new Entity("Child"));
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { box }, box, null, 0);
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { parent }, parent, null, 1);
            graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { child }, child, parent.Entity, 0);
        }
        return fixture;
    }

    internal static async Task<SceneEditorViewModel> OpenEditorAsync(AssetsSessionFixture fixture)
    {
        Assert.False(fixture.Result.HasErrors, fixture.Log);
        var editor = new SceneEditorViewModel(fixture.Asset<SceneViewModel>("MainScene"));
        Assert.True(await editor.Initialize());
        return editor;
    }

    internal static EntityViewModel FindEntity(EntityHierarchyEditorViewModel editor, string name)
    {
        return editor.HierarchyRoot.InnerSubEntities.BreadthFirstEntities().Single(x => x.Name == name);
    }

    /// <summary>
    /// Adds a new entity to the scene, as one undoable operation.
    /// </summary>
    internal static Entity AddEntity(AssetsSessionFixture fixture, EntityHierarchyEditorViewModel editor, string name, Entity? parent, int index)
    {
        var entity = new Entity(name);
        var design = new EntityDesign(entity);
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        PropertyGraph(editor).AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, parent, index);
        undoRedo.SetName(transaction, "Add entity");
        return entity;
    }

    /// <summary>
    /// Deletes an entity of the scene with its children, as one undoable operation.
    /// </summary>
    internal static void DeleteEntity(AssetsSessionFixture fixture, EntityHierarchyEditorViewModel editor, Entity entity)
    {
        var graph = PropertyGraph(editor);
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        graph.DeleteParts([graph.Asset.Hierarchy.Parts[entity.Id]], out _);
        undoRedo.SetName(transaction, "Delete entity");
    }

    internal static AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity> PropertyGraph(EntityHierarchyEditorViewModel editor)
    {
        return (AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)editor.Asset.PropertyGraph;
    }

    internal static List<EntityHierarchyChangedEventArgs> Record(EntityHierarchyRootViewModel root, out List<EntityHierarchyChangedEventArgs> removed)
    {
        var added = new List<EntityHierarchyChangedEventArgs>();
        var removedEvents = new List<EntityHierarchyChangedEventArgs>();
        root.EntityAdded += (_, e) => added.Add(e);
        root.EntityRemoved += (_, e) => removedEvents.Add(e);
        removed = removedEvents;
        return added;
    }
}

internal static class EntityViewModelTestExtensions
{
    /// <summary>
    /// The entities and all their descendants.
    /// </summary>
    public static IEnumerable<EntityViewModel> BreadthFirstEntities(this IEnumerable<EntityViewModel> entities)
    {
        var queue = new Queue<EntityViewModel>(entities);
        while (queue.TryDequeue(out var entity))
        {
            yield return entity;
            foreach (var child in entity.TransformChildren)
            {
                queue.Enqueue(child);
            }
        }
    }
}
