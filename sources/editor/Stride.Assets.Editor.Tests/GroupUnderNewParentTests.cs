// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;
using static Stride.Assets.Editor.Tests.SceneEditingCommandsTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The command that groups the selection under a new parent: where the parent goes, the entities don't move in the world, and
/// the single entry of the undo history.
/// </summary>
public sealed class GroupUnderNewParentTests
{
    [Fact]
    public async Task TheGroupGoesAtTheCenterUnderTheCommonParentAndNothingMovesInTheWorld()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var (box, parent, child) = Place(fixture, editor);
        // The parent turns: the child keeps its place and its rotation in the world when it changes parent
        Update(fixture, parent, nameof(TransformComponent.Rotation), Quaternion.RotationY(MathUtil.PiOverTwo));
        var childWorld = World(child);
        editor.SelectedContent.Add(box);
        editor.SelectedContent.Add(child);
        await editor.PropertiesRefreshCompletion;
        var names = RecordUndoNames(fixture);

        Assert.True(editor.GroupUnderNewParentCommand.CanExecute(null));
        var group = editor.GroupSelectionUnderNewParent();

        Assert.NotNull(group);
        Assert.Equal(["Group under a new parent"], names);
        // Box is in the scene and Child under Parent: the group goes in the scene, where Box was
        Assert.Null(group.AssetSideEntity.Transform.Parent);
        Assert.Equal(["Group", "Parent"], editor.HierarchyRoot.InnerSubEntities.OfType<EntityViewModel>().Select(x => x.Name));
        // The moved entities have new view models
        box = FindEntity(editor, "Box");
        child = FindEntity(editor, "Child");
        // At the center of Box (1, 0, 0) and Child (5, 0, 0) in the world
        AssertNear(new Vector3(5, 0, 0), childWorld.Position);
        AssertNear(new Vector3(3, 0, 0), WorldPosition(group));
        Assert.Equal(["Box", "Child"], group.AssetSideEntity.Transform.Children.Select(x => x.Entity.Name));
        AssertNear(new Vector3(1, 0, 0), WorldPosition(box));
        var (position, rotation, _) = World(child);
        AssertNear(childWorld.Position, position);
        AssertNear(Vector3.Transform(Vector3.UnitX, childWorld.Rotation), Vector3.Transform(Vector3.UnitX, rotation));
        // The group is selected, and shows its children
        Assert.Equal([group], editor.SelectedItems);
        Assert.True(group.IsExpanded);

        fixture.Session.UndoRedoService.Undo();
        Assert.Equal(["Box", "Parent"], editor.HierarchyRoot.InnerSubEntities.OfType<EntityViewModel>().Select(x => x.Name));
        box = FindEntity(editor, "Box");
        child = FindEntity(editor, "Child");
        Assert.Same(parent.AssetSideEntity.Transform, child.AssetSideEntity.Transform.Parent);
        Assert.Equal(new Vector3(0, 0, 2), child.AssetSideEntity.Transform.Position);
        Assert.Equal(new Vector3(1, 0, 0), box.AssetSideEntity.Transform.Position);
        editor.Destroy();
    }

    [Fact]
    public async Task ChildrenOfOneEntityAreGroupedUnderIt()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var (_, parent, child) = Place(fixture, editor);
        AddEntity(fixture, editor, "Other", parent.AssetSideEntity, 1);
        var other = FindEntity(editor, "Other");
        Update(fixture, other, nameof(TransformComponent.Position), new Vector3(0, 4, 0));
        Update(fixture, parent, nameof(TransformComponent.Scale), new Vector3(2));
        editor.SelectedContent.Add(other);
        editor.SelectedContent.Add(child);
        await editor.PropertiesRefreshCompletion;

        var group = editor.GroupSelectionUnderNewParent();

        Assert.NotNull(group);
        Assert.Same(parent.AssetSideEntity.Transform, group.AssetSideEntity.Transform.Parent);
        // In the order of the tree, whatever the order of the selection
        Assert.Equal(["Child", "Other"], group.AssetSideEntity.Transform.Children.Select(x => x.Entity.Name));
        // Parent at (3, 0, 0) scaled by 2: Child at (3, 0, 4) and Other at (3, 8, 0) in the world
        AssertNear(new Vector3(3, 4, 2), WorldPosition(group));
        AssertNear(new Vector3(0, 2, 1), group.AssetSideEntity.Transform.Position);
        child = FindEntity(editor, "Child");
        other = FindEntity(editor, "Other");
        AssertNear(new Vector3(3, 0, 4), WorldPosition(child));
        AssertNear(new Vector3(3, 8, 0), WorldPosition(other));
        editor.Destroy();
    }

    [Fact]
    public async Task ThereIsNothingToGroupWithoutASelection()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var names = RecordUndoNames(fixture);

        Assert.False(editor.GroupUnderNewParentCommand.CanExecute(null));
        Assert.Null(editor.GroupSelectionUnderNewParent());
        Assert.Empty(names);
        Assert.False(EntityHierarchyEditorViewModel.TryGetGroupParent([], out _, out _));
        editor.Destroy();
    }

    private static (Vector3 Position, Quaternion Rotation, Vector3 Scale) World(EntityViewModel entity)
    {
        var transform = entity.AssetSideEntity.Transform;
        transform.UpdateWorldMatrix();
        transform.GetWorldTransformation(out var position, out var rotation, out var scale);
        return (position, rotation, scale);
    }
}
