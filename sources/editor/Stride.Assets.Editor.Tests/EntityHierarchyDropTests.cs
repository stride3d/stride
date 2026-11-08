// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;
using Stride.Core.Assets.Presentation.ViewModels;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;
using static Stride.Assets.Editor.Tests.EntityHierarchyLoadingTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The drag and drop of entities and folders in the hierarchy of the scene editor, as in the WPF scene editor: a drop on
/// an item adds the dropped items as its last children, a drop before or after an item inserts them next to it, and each
/// drop is one undoable operation.
/// </summary>
public sealed class EntityHierarchyDropTests
{
    [Fact]
    public async Task AnEntityDroppedOnAnotherBecomesItsLastChildAsOneUndoEntry()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        var child = FindEntity(editor, "Child");
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));
        editor.SelectedContent.Add(child);

        Assert.Equal(DropAcceptance.Accepted, ((IAddChildViewModel)box).GetAddChildrenAcceptance([child], AddChildModifiers.None, out _));
        ((IAddChildViewModel)box).AddChildren([child], AddChildModifiers.None);

        Assert.Equal(["Move entities to the scene"], names);
        var moved = Assert.Single(box.TransformChildren);
        Assert.Equal("Child", moved.Name);
        Assert.Empty(FindEntity(editor, "Parent").TransformChildren);
        // The selection follows the moved entity
        Assert.Same(moved, Assert.Single(editor.SelectedContent));

        undoRedo.Undo();

        Assert.Empty(box.TransformChildren);
        Assert.Equal(["Child"], FindEntity(editor, "Parent").TransformChildren.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task AnEntityCannotBeDroppedOnItselfOrOnItsChildren()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        var child = FindEntity(editor, "Child");

        Assert.Equal(DropAcceptance.NoOp, ((IAddChildViewModel)parent).GetAddChildrenAcceptance([parent], AddChildModifiers.None, out var message));
        Assert.Equal("Nothing to move", message);
        Assert.Equal(DropAcceptance.Rejected, ((IAddChildViewModel)child).GetAddChildrenAcceptance([parent], AddChildModifiers.None, out message));
        Assert.Equal("Can't drop an entity on itself or its children", message);
        // The last child, dropped on its parent, changes nothing
        Assert.Equal(DropAcceptance.NoOp, ((IAddChildViewModel)parent).GetAddChildrenAcceptance([child], AddChildModifiers.None, out message));
        editor.Destroy();
    }

    [Fact]
    public async Task AnEntityDroppedBeforeAnotherIsInsertedNextToIt()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        var child = FindEntity(editor, "Child");

        Assert.Equal(DropAcceptance.Accepted, ((IInsertChildViewModel)box).GetInsertChildrenAcceptance([child], InsertPosition.Before, AddChildModifiers.None, out var message));
        Assert.StartsWith("Insert before Box", message);
        ((IInsertChildViewModel)box).InsertChildren([child], InsertPosition.Before, AddChildModifiers.None);

        Assert.Equal(["Child", "Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task AFolderDroppedOnAnEntityMovesItsEntitiesWithTheirPaths()
    {
        await using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        AddEntity(fixture, editor, "Tree", null, 2);
        var root = editor.HierarchyRoot;
        var tree = FindEntity(editor, "Tree");
        var props = editor.CreateFolder(root, false);
        props.Name = "Props";
        // The tree goes in the folder
        ((IAddChildViewModel)props).AddChildren([tree], AddChildModifiers.None);
        Assert.Equal("Props", FindEntity(editor, "Tree").EntityDesign.Folder);
        var box = FindEntity(editor, "Box");

        Assert.Equal(DropAcceptance.Accepted, ((IAddChildViewModel)box).GetAddChildrenAcceptance([props], AddChildModifiers.None, out _));
        ((IAddChildViewModel)box).AddChildren([props], AddChildModifiers.None);

        var folder = Assert.IsType<EntityFolderViewModel>(Assert.Single(box.Children));
        Assert.Equal("Props", folder.Name);
        Assert.Equal(["Tree"], folder.Children.Select(x => x.Name));
        Assert.Same(box, FindEntity(editor, "Tree").TransformParent);
        Assert.DoesNotContain(root.Children, x => x.Name == "Props");
        editor.Destroy();
    }

    [Fact]
    public async Task WithAltAnEntityKeepsItsWorldPositionAcrossTheOffsetOfAChildScene()
    {
        await using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var root = (SceneRootViewModel)editor.HierarchyRoot;
        var child = root.ChildScenes.Single();
        await child.RequestLoading(true);
        var lamp = child.InnerSubEntities.Single();
        var box = FindEntity(editor, "Box");

        ((IAddChildViewModel)root).AddChildren([lamp], AddChildModifiers.Alt);

        Assert.Empty(child.InnerSubEntities);
        var moved = root.InnerSubEntities.Single(x => x.Name == "Lamp");
        // The child scene is at (5, 0, 0): the lamp stays where it was shown
        Assert.Equal(new Vector3(6, 0, 0), moved.AssetSideEntity.Transform.Position);
        // An entity from another asset gets a new id
        Assert.NotEqual(lamp.Id.ObjectId, moved.Id.ObjectId);
        Assert.True(moved.IsLoaded);
        Assert.Same(box, FindEntity(editor, "Box"));
        editor.Destroy();
    }
}
