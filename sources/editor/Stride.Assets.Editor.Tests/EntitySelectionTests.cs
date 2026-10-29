// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.EditorGame;
using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The selection of a scene editor in its game: only the entities in the game can be selected, and the selected entities
/// with no selected parent are the roots the transformation gizmos move.
/// </summary>
public sealed class EntitySelectionTests
{
    [Fact]
    public async Task AnEntityNotInTheGameIsNotSelected()
    {
        using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var box = EntityHierarchyEditorTests.FindEntity(editor, "Box");
        var selection = new EntitySelection();

        Assert.False(selection.Add(box));
        Assert.Equal(0, selection.Count);

        selection.AddSelectable(box.Id);
        Assert.True(selection.Add(box));
        Assert.False(selection.Add(box));
        Assert.Equal([box.Id], selection.GetSelectedRootIds());
        editor.Destroy();
    }

    [Fact]
    public async Task ASelectedParentIsTheRootOfItsSelectedChildren()
    {
        using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var (selection, entities) = SelectableScene(editor);

        selection.Add(entities["Child"]);
        Assert.Equal([entities["Child"].Id], selection.GetSelectedRootIds());

        selection.Add(entities["Parent"]);
        Assert.Equal([entities["Parent"].Id], selection.GetSelectedRootIds());
        Assert.Equal(2, selection.Count);

        selection.Add(entities["Box"]);
        Assert.Equal([entities["Box"].Id, entities["Parent"].Id], selection.GetSelectedRootIds().ToHashSet());
        editor.Destroy();
    }

    [Fact]
    public async Task RemovingARootMakesItsTopmostSelectedDescendantsRoots()
    {
        using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        // Parent > Child > Grandchild, and Parent > Second child
        var parent = EntityHierarchyEditorTests.FindEntity(editor, "Parent");
        var child = EntityHierarchyEditorTests.FindEntity(editor, "Child");
        EntityHierarchyEditorTests.AddEntity(fixture, editor, "Grandchild", child.AssetSideEntity, 0);
        EntityHierarchyEditorTests.AddEntity(fixture, editor, "Second child", parent.AssetSideEntity, 1);
        var (selection, entities) = SelectableScene(editor);
        foreach (var name in new[] { "Parent", "Child", "Grandchild", "Second child" })
            selection.Add(entities[name]);
        Assert.Equal([parent.Id], selection.GetSelectedRootIds());

        Assert.True(selection.Remove(parent));

        // The grandchild stays under its selected parent
        Assert.Equal([entities["Child"].Id, entities["Second child"].Id], selection.GetSelectedRootIds().ToHashSet());
        Assert.Equal(3, selection.Count);
        Assert.False(selection.Remove(parent));
        editor.Destroy();
    }

    [Fact]
    public async Task ClearingEmptiesTheSelectionButKeepsTheSelectableEntities()
    {
        using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var (selection, entities) = SelectableScene(editor);
        selection.Add(entities["Box"]);
        selection.Add(entities["Child"]);

        selection.Clear();

        Assert.Equal(0, selection.Count);
        Assert.Equal(0, selection.RootCount);
        Assert.True(selection.IsSelectable(entities["Box"].Id));
        editor.Destroy();
    }

    /// <summary>
    /// A selection where every entity of the editor can be selected.
    /// </summary>
    private static (EntitySelection Selection, Dictionary<string, EntityViewModel> Entities) SelectableScene(EntityHierarchyEditorViewModel editor)
    {
        var selection = new EntitySelection();
        var entities = editor.HierarchyRoot.InnerSubEntities.BreadthFirstEntities().ToDictionary(x => x.Name!);
        foreach (var entity in entities.Values)
            selection.AddSelectable(entity.Id);
        return (selection, entities);
    }
}
