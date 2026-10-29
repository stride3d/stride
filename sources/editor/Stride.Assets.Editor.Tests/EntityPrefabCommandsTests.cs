// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using static Stride.Assets.Editor.Tests.EntityHierarchyEditorTests;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// The prefab commands of the scene editor, as in the WPF scene editor: a prefab from the selection, whose entities become
/// instances, and the link to the prefab broken.
/// </summary>
public sealed class EntityPrefabCommandsTests
{
    [Fact]
    public Task APrefabFromTheSelectionHasItsEntitiesWhichBecomeInstancesOfIt() => RunOnUIThread(async ui =>
    {
        using var fixture = await OpenSceneAsync(ui);
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        Assert.False(editor.CreatePrefabFromSelectionCommand.IsEnabled);
        editor.SelectedContent.Add(parent);
        Assert.False(editor.OpenPrefabEditorCommand.IsEnabled);
        var undoRedo = fixture.Session.UndoRedoService;
        var names = new List<string?>();
        undoRedo.Done += (_, e) => names.Add(undoRedo.GetName(e.Transaction));

        editor.CreatePrefabFromSelectionCommand.Execute();

        var prefab = Assert.Single(fixture.Session.AllAssets.OfType<PrefabViewModel>());
        Assert.Equal("Parent", prefab.Name);
        Assert.Equal(["Child", "Parent"], prefab.Asset.Hierarchy.Parts.Values.Select(x => x.Entity.Name).Order());
        Assert.Equal("Parent", Assert.Single(prefab.Asset.Hierarchy.RootParts).Name);
        Assert.Same(prefab, parent.SourcePrefab);
        Assert.Same(prefab, FindEntity(editor, "Child").SourcePrefab);
        Assert.Contains(prefab, fixture.Session.AssetCollection.SelectedAssets);
        Assert.Equal(["Create Parent from selection"], names);

        undoRedo.Undo();

        Assert.Null(parent.SourcePrefab);
        Assert.DoesNotContain(fixture.Session.AllAssets, x => x is PrefabViewModel { IsDeleted: false });
        editor.Destroy();
    });

    [Fact]
    public Task BreakingTheLinkMakesTheEntitiesPlainEntities() => RunOnUIThread(async ui =>
    {
        using var fixture = await OpenSceneAsync(ui);
        var editor = await OpenEditorAsync(fixture);
        var parent = FindEntity(editor, "Parent");
        editor.SelectedContent.Add(parent);
        editor.CreatePrefabFromSelectionCommand.Execute();
        // The commands follow the selection
        editor.SelectedContent.Clear();
        editor.SelectedContent.Add(parent);
        Assert.True(editor.BreakLinkToPrefabCommand.IsEnabled);
        Assert.True(editor.OpenPrefabEditorCommand.IsEnabled);
        Assert.True(editor.SelectPrefabCommand.IsEnabled);

        editor.BreakLinkToPrefabCommand.Execute();

        Assert.Null(parent.SourcePrefab);
        Assert.Null(((EntityHierarchyAssetBase)editor.Asset.Asset).Hierarchy.Parts[parent.AssetSideEntity.Id].Base);
        // The child is not selected: it stays an instance
        Assert.NotNull(FindEntity(editor, "Child").SourcePrefab);
        editor.Destroy();
    });

    /// <summary>
    /// Runs the test on a UI thread, as the editor: the asset view reacts to the new asset from other threads through the
    /// dispatcher.
    /// </summary>
    private static async Task RunOnUIThread(Func<ThreadDispatcherService, Task> test)
    {
        using var ui = new ThreadDispatcherService("Fake UI thread");
        await ui.Run(() => test(ui));
    }
}
