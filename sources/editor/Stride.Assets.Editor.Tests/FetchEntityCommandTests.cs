// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Assets.Presentation.ViewModels;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;
using static Stride.Assets.Editor.Tests.EntityHierarchyLoadingTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The fetch command of the entity and component properties selects the entity in the editor that shows it.
/// </summary>
public sealed class FetchEntityCommandTests
{
    [Fact]
    public async Task FetchingAComponentSelectsItsEntityInTheEditor()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box");
        // The test plays the UI thread: the selection changes (of the editor, and of the asset view that follows it) run on the
        // dispatcher, one at a time with the refresh of the references that the session dispatches after the edits of the fixture
        fixture.Session.Dispatcher.Invoke(() =>
        {
            editor.SelectedItems.Add(FindEntity(editor, "Parent"));
            FetchEntityCommand.Fetch(fixture.Asset<SceneViewModel>("MainScene"), box.AssetSideEntity.Transform);
        });

        Assert.Equal([box], editor.SelectedItems);
        editor.Destroy();
    }

    [Fact]
    public async Task FetchingAnEntityOfAChildSceneSelectsItInTheEditorOfTheRootScene()
    {
        using var fixture = await OpenScenesAsync();
        var editor = await OpenEditorAsync(fixture);
        var child = ((SceneRootViewModel)editor.HierarchyRoot).ChildScenes.Single();
        var lamp = child.InnerSubEntities.Single();

        fixture.Session.Dispatcher.Invoke(() => FetchEntityCommand.Fetch(fixture.Asset<SceneViewModel>("ChildScene"), lamp.AssetSideEntity));

        Assert.Equal([lamp], editor.SelectedItems);
        editor.Destroy();
    }

    [Fact]
    public async Task FetchingAnEntityOfASceneThatNoEditorShowsDoesNothing()
    {
        using var fixture = await OpenSceneAsync();
        var editor = await OpenEditorAsync(fixture);
        var box = FindEntity(editor, "Box").AssetSideEntity;
        editor.Destroy();

        fixture.Session.Dispatcher.Invoke(() => FetchEntityCommand.Fetch(fixture.Asset<SceneViewModel>("MainScene"), box));

        Assert.Null(fixture.Asset<SceneViewModel>("MainScene").HierarchyRoot);
    }
}
