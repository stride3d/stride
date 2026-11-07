// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Engine;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The folders of the hierarchy of the scene editor, as in the WPF scene editor: a folder is the path in
/// <see cref="EntityDesign.Folder"/> of its entities, and the folders come before the entities, sorted by name.
/// </summary>
public sealed class EntityHierarchyFolderTests
{
    [Fact]
    public async Task TheEntitiesOfAFolderGoUnderItAndTheFoldersComeFirstSortedByName()
    {
        using var fixture = await OpenSceneAsync();
        AddEntity(fixture, "Tree", "Props/Nature", parent: null, index: 2);
        AddEntity(fixture, "Barrel", "Props", parent: null, index: 3);
        AddEntity(fixture, "Sun", "Lights", parent: null, index: 4);
        var editor = await OpenEditorAsync(fixture);

        var root = editor.HierarchyRoot;
        Assert.Equal(["Lights", "Props", "Box", "Parent"], root.Children.Select(x => x.Name));
        var props = Assert.IsType<EntityFolderViewModel>(root.Children[1]);
        Assert.Equal(["Nature", "Barrel"], props.Children.Select(x => x.Name));
        var nature = Assert.IsType<EntityFolderViewModel>(props.Children[0]);
        Assert.Equal("Props/Nature", nature.Path);
        Assert.Same(root, nature.Owner);
        Assert.Equal(["Tree"], nature.Children.Select(x => x.Name));
        // A folder is not an element of the transform hierarchy: its entities are children of the root
        Assert.Equal(["Sun", "Tree", "Barrel", "Box", "Parent"], root.TransformChildren.Select(x => x.Name));
        Assert.True(root.IsExpanded);
        editor.Destroy();
    }

    [Fact]
    public async Task AnEntityAddedInAFolderCreatesItAndTheUndoTakesBothAway()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        var added = Record(editor.HierarchyRoot, out _);

        AddEntity(fixture, "Lamp", "Lights", parent.AssetSideEntity, index: 1);

        var folder = Assert.IsType<EntityFolderViewModel>(parent.Children[0]);
        Assert.Equal("Lights", folder.Name);
        Assert.True(parent.IsExpanded);
        Assert.Equal(["Lamp"], folder.Children.Select(x => x.Name));
        // The game adds the entity to the transform of its owner, whatever the folder
        var e = Assert.Single(added);
        Assert.Same(parent, e.Parent);
        Assert.Equal(1, e.Index);

        fixture.Session.UndoRedoService.Undo();

        Assert.Equal(["Child"], parent.Children.Select(x => x.Name));

        fixture.Session.UndoRedoService.Redo();

        folder = Assert.IsType<EntityFolderViewModel>(parent.Children[0]);
        Assert.Equal(["Lamp"], folder.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task RenamingAFolderMovesItsEntitiesAndCanBeUndone()
    {
        using var fixture = await OpenSceneAsync();
        AddEntity(fixture, "Tree", "Props/Nature", parent: null, index: 2);
        AddEntity(fixture, "Sun", "Lights", parent: null, index: 3);
        var editor = await OpenEditorAsync(fixture);
        var props = (EntityFolderViewModel)editor.HierarchyRoot.Children.Single(x => x.Name == "Props");
        var tree = FindEntity(editor, "Tree");
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));

        props.Name = "Decor";

        Assert.Equal(["Rename folder 'Props' to 'Decor'"], names);
        Assert.Equal("Decor/Nature", tree.EntityDesign.Folder);
        // The folders stay sorted
        Assert.Equal(["Decor", "Lights", "Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));

        undoRedo.Undo();

        Assert.Equal("Props/Nature", tree.EntityDesign.Folder);
        Assert.Equal(["Lights", "Props", "Box", "Parent"], editor.HierarchyRoot.Children.Select(x => x.Name));
        editor.Destroy();
    }

    [Fact]
    public async Task AFolderCannotTakeTheNameOfASiblingOrAnInvalidName()
    {
        using var fixture = await OpenSceneAsync();
        AddEntity(fixture, "Tree", "Props", parent: null, index: 2);
        AddEntity(fixture, "Sun", "Lights", parent: null, index: 3);
        var editor = await OpenEditorAsync(fixture);
        var props = (EntityFolderViewModel)editor.HierarchyRoot.Children.Single(x => x.Name == "Props");

        props.Name = "lights";
        props.Name = "A/B";
        props.Name = " ";

        Assert.Equal("Props", props.Name);
        Assert.Equal("Props", FindEntity(editor, "Tree").EntityDesign.Folder);
        editor.Destroy();
    }

    [Fact]
    public async Task AnEmptyFolderIsKeptWhenItsLastEntityLeaves()
    {
        using var fixture = await OpenSceneAsync();
        var tree = AddEntity(fixture, "Tree", "Props", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);

        DeleteEntity(fixture, editor, tree);

        var folder = Assert.IsType<EntityFolderViewModel>(editor.HierarchyRoot.Children[0]);
        Assert.Empty(folder.Children);
        editor.Destroy();
    }

    /// <summary>
    /// Adds a new entity in a folder of the scene, as one undoable operation.
    /// </summary>
    internal static Entity AddEntity(AssetsSessionFixture fixture, string name, string folder, Entity? parent, int index)
    {
        var entity = new Entity(name);
        var design = new EntityDesign(entity, folder);
        var undoRedo = fixture.Session.UndoRedoService;
        var graph = (Core.Assets.Quantum.AssetCompositeHierarchyPropertyGraph<EntityDesign, Entity>)fixture.Asset<SceneViewModel>("MainScene").PropertyGraph!;
        using var transaction = undoRedo.CreateTransaction();
        graph.AddPartToAsset(new AssetPartCollection<EntityDesign, Entity> { design }, design, parent, index);
        undoRedo.SetName(transaction, "Add entity");
        return entity;
    }
}
