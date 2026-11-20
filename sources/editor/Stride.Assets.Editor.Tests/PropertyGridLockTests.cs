// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Editor.ViewModels;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Mathematics;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// A locked property grid keeps showing its objects when the selection changes, in the editor or in the asset view, edits them
/// as usual, shows the latest selection once unlocked, and unlocks by itself when its objects go.
/// </summary>
public sealed class PropertyGridLockTests
{
    [Fact]
    public async Task ALockedGridKeepsItsObjectsAndShowsTheLatestSelectionOnceUnlocked()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var properties = fixture.Session.AssetViewProperties;
            var (box, parent) = (EntityHierarchyEditorTests.FindEntity(editor, "Box"), EntityHierarchyEditorTests.FindEntity(editor, "Parent"));
            await SelectAsync(editor, box);
            var viewModel = properties.ViewModel;
            Assert.Equal([box], properties.Selection);

            properties.IsLocked = true;
            Assert.True(properties.IsLocked);
            // The asset controls of the header act on the selected assets: a locked grid has none
            Assert.False(properties.IsAssetSelection);

            // Another entity of the editor
            await SelectAsync(editor, parent);
            Assert.Equal([box], properties.Selection);
            Assert.Same(viewModel, properties.ViewModel);
            Assert.Equal("Box", properties.Name);

            // An asset of the asset view
            var scene = fixture.Asset<SceneViewModel>("MainScene");
            fixture.Session.AssetCollection.SelectAssets([scene]);
            await Task.Delay(100);
            Assert.Equal([box], properties.Selection);
            Assert.Equal("Box", properties.Name);
            // The active properties are the same: the editors and the asset view share them
            Assert.Same(properties, fixture.Session.ActiveProperties);

            // A refresh shows the same objects again
            await properties.RefreshSelectedPropertiesAsync();
            Assert.Equal([box], properties.Selection);
            Assert.NotNull(properties.ViewModel);

            // Unlocking shows the latest selection at once
            properties.IsLocked = false;
            Assert.Equal(scene.Url, properties.Name);
            await ExclusiveDispatcherService.WaitUntilAsync(() => properties.Selection?.SequenceEqual([scene]) == true && properties.CanDisplayProperties, "the scene");
            Assert.True(properties.IsAssetSelection);
        }
        finally
        {
            editor.Destroy();
        }
    }

    [Fact]
    public async Task AnEmptyGridDoesNotLockAndUnlockingWithNoNewSelectionKeepsTheObjects()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var properties = fixture.Session.AssetViewProperties;
            properties.IsLocked = true;
            Assert.False(properties.IsLocked);

            var box = EntityHierarchyEditorTests.FindEntity(editor, "Box");
            await SelectAsync(editor, box);
            var viewModel = properties.ViewModel;
            properties.IsLocked = true;
            properties.IsLocked = false;
            Assert.Same(viewModel, properties.ViewModel);
            Assert.Equal([box], properties.Selection);
        }
        finally
        {
            editor.Destroy();
        }
    }

    [Fact]
    public async Task EditsInALockedGridWorkAndUndo()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var properties = fixture.Session.AssetViewProperties;
            var (box, parent) = (EntityHierarchyEditorTests.FindEntity(editor, "Box"), EntityHierarchyEditorTests.FindEntity(editor, "Parent"));
            await SelectAsync(editor, box);
            properties.IsLocked = true;
            await SelectAsync(editor, parent);

            var position = ResetToDefaultTests.Find(properties.ViewModel!.RootNode, "Position");
            position.NodeValue = new Vector3(1, 2, 3);
            Assert.Equal(new Vector3(1, 2, 3), box.AssetSideEntity.Transform.Position);
            Assert.Equal(Vector3.Zero, parent.AssetSideEntity.Transform.Position);

            fixture.Session.UndoRedoService.Undo();
            Assert.Equal(Vector3.Zero, box.AssetSideEntity.Transform.Position);
            // An undo of an edit keeps the lock
            await Task.Delay(50);
            Assert.True(properties.IsLocked);
        }
        finally
        {
            editor.Destroy();
        }
    }

    [Fact]
    public async Task DeletingALockedEntityUnlocksTheGridAndUndoingTheDeleteDoesNotLockItAgain()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        try
        {
            var properties = fixture.Session.AssetViewProperties;
            var (box, parent) = (EntityHierarchyEditorTests.FindEntity(editor, "Box"), EntityHierarchyEditorTests.FindEntity(editor, "Parent"));
            await SelectAsync(editor, box);
            properties.IsLocked = true;
            await SelectAsync(editor, parent);

            // The box is not selected: its delete changes no selection
            EntityHierarchyEditorTests.DeleteEntity(fixture, editor, box.AssetSideEntity);
            await ExclusiveDispatcherService.WaitUntilAsync(() => !properties.IsLocked, "the grid to unlock");
            // It shows the selection of the editor
            await ExclusiveDispatcherService.WaitUntilAsync(() => properties.Selection?.SequenceEqual([parent]) == true, "the parent");
            Assert.Equal("Parent", properties.Name);

            fixture.Session.UndoRedoService.Undo();
            Assert.NotNull(editor.HierarchyRoot.Children.FirstOrDefault(x => x.Name == "Box"));
            await Task.Delay(50);
            Assert.False(properties.IsLocked);
        }
        finally
        {
            editor.Destroy();
        }
    }

    [Fact]
    public async Task ClosingTheEditorOfTheLockedObjectsUnlocksTheGrid()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var editor = await EntityHierarchyEditorTests.OpenEditorAsync(fixture);
        var properties = fixture.Session.AssetViewProperties;
        await SelectAsync(editor, EntityHierarchyEditorTests.FindEntity(editor, "Box"));
        properties.IsLocked = true;

        editor.Destroy();

        await ExclusiveDispatcherService.WaitUntilAsync(() => !properties.IsLocked, "the grid to unlock");
        // The editor asked for an empty selection as it closed
        await ExclusiveDispatcherService.WaitUntilAsync(() => properties.Selection?.Count == 0, "the empty selection");
    }

    [Fact]
    public async Task DeletingALockedAssetUnlocksTheGrid()
    {
        await using var fixture = await EntityHierarchyEditorTests.OpenSceneAsync();
        var properties = fixture.Session.AssetViewProperties;
        var scene = fixture.Asset<SceneViewModel>("MainScene");
        fixture.Session.AssetCollection.SelectAssets([scene]);
        await ExclusiveDispatcherService.WaitUntilAsync(() => properties.Selection?.SequenceEqual([scene]) == true && properties.CanDisplayProperties, "the scene");
        properties.IsLocked = true;

        using (var transaction = fixture.Session.UndoRedoService.CreateTransaction())
        {
            scene.IsDeleted = true;
            fixture.Session.UndoRedoService.SetName(transaction, "Delete scene");
        }

        await ExclusiveDispatcherService.WaitUntilAsync(() => !properties.IsLocked, "the grid to unlock");
    }

    private static async Task SelectAsync(EntityHierarchyEditorViewModel editor, EntityViewModel entity)
    {
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(entity);
        await editor.PropertiesRefreshCompletion;
    }
}
