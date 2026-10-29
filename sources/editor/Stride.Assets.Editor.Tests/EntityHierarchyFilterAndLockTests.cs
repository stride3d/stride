// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Presentation.ViewModels;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The filter, the renaming and the locks of the hierarchy of the scene editor, as in the WPF scene editor.
/// </summary>
public sealed class EntityHierarchyFilterAndLockTests
{
    [Fact]
    public async Task TheFilterShowsTheMatchingItemsAndTheirParents()
    {
        using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Sun light", "Lights", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        var lights = editor.HierarchyRoot.Children.Single(x => x.Name == "Lights");

        editor.FilterPattern = "CHILD";

        Assert.True(FindEntity(editor, "Parent").IsVisible);
        Assert.True(FindEntity(editor, "Child").IsVisible);
        Assert.False(FindEntity(editor, "Box").IsVisible);
        Assert.False(lights.IsVisible);

        // Every word must match
        editor.FilterPattern = "light sun";

        Assert.True(lights.IsVisible);
        Assert.True(FindEntity(editor, "Sun light").IsVisible);
        Assert.False(FindEntity(editor, "Parent").IsVisible);

        editor.FilterPattern = "";

        Assert.All(editor.HierarchyRoot.Children, x => Assert.True(x.IsVisible));
        Assert.True(FindEntity(editor, "Child").IsVisible);
        editor.Destroy();
    }

    [Fact]
    public async Task RenameEditsTheLastSelectedEntityOrFolder()
    {
        using var fixture = await OpenSceneAsync();
        EntityHierarchyFolderTests.AddEntity(fixture, "Sun", "Lights", parent: null, index: 2);
        var editor = await OpenEditorAsync(fixture);
        Assert.False(editor.RenameCommand.IsEnabled);

        editor.SelectedContent.Add(editor.HierarchyRoot);
        Assert.False(editor.RenameCommand.IsEnabled);

        var box = FindEntity(editor, "Box");
        editor.SelectedContent.Add(box);
        editor.RenameCommand.Execute();
        Assert.True(box.IsEditing);

        var lights = editor.HierarchyRoot.Children.Single(x => x.Name == "Lights");
        editor.SelectedContent.Add(lights);
        editor.RenameCommand.Execute();
        Assert.True(lights.IsEditing);
        editor.Destroy();
    }

    [Fact]
    public async Task LockingAnEntityLocksItsChildrenOnlyWithCtrlButTheRootLocksEverything()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        var child = FindEntity(editor, "Child");
        var locked = new List<string?>();
        editor.HierarchyRoot.LockChanged += (_, e) => locked.Add(e.Element.Name);

        parent.LockCommand.Execute(false);

        Assert.True(parent.IsLocked);
        Assert.False(child.IsLocked);

        parent.LockCommand.Execute(true);

        Assert.False(parent.IsLocked);
        Assert.False(child.IsLocked);

        editor.HierarchyRoot.LockCommand.Execute(false);

        Assert.True(editor.HierarchyRoot.IsLocked);
        Assert.True(FindEntity(editor, "Box").IsLocked);
        Assert.True(parent.IsLocked);
        Assert.True(child.IsLocked);
        Assert.Equal(["Parent", "Parent", "SceneRoot", "Box", "Parent", "Child"], locked);
        editor.Destroy();
    }
}
