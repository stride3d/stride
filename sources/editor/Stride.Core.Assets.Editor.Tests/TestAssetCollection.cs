// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Assets.Presentation.Services;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

public sealed class TestAssetCollection
{
    [Fact]
    public async Task SelectingAssetsSelectsTheirLocation()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"), ("Models/Tree", "tree"));
        var assets = fixture.Assets;
        var wood = fixture.Session.AllAssets.Single(x => x.Name == "Wood");

        fixture.OnUiThread(() => assets.SelectAssets([wood]));

        Assert.Equal([wood], assets.SelectedAssets);
        Assert.Same(wood, assets.SingleSelectedAsset);
        Assert.Same(wood, assets.SingleSelectedContent);
        Assert.Equal([wood.Directory], assets.SelectedLocations);
        Assert.Same(fixture.Package, assets.SelectedAssetsPackage);

        fixture.OnUiThread(() => assets.ClearSelection());

        Assert.Empty(assets.SelectedAssets);
        Assert.Null(assets.SingleSelectedAsset);
    }

    [Fact]
    public async Task SelectingAPackageListsItsAssets()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Models/Tree", "tree"));
        var assets = fixture.Assets;

        // The main view remembers the display mode across sessions: start from the default one
        fixture.OnUiThread(() => assets.DisplayAssetMode = DisplayAssetMode.AssetAndFolderInSelectedFolder);
        fixture.OnUiThread(() => assets.SelectedLocations.Add(fixture.Package));

        // This mode lists the sub-folders and the assets of the selected location only
        Assert.Empty(assets.FilteredAssets);
        Assert.Equal(["Models", "Textures"], assets.FilteredContent.OfType<DirectoryBaseViewModel>().Select(x => x.Name).Order());

        fixture.OnUiThread(() => assets.DisplayAssetMode = DisplayAssetMode.AssetInSelectedFolderAndSubFolder);

        Assert.Equal(["Tree", "Wood"], assets.FilteredAssets.Select(x => x.Name).Order());
        Assert.Empty(assets.FilteredContent.OfType<DirectoryBaseViewModel>());
    }

    [Fact]
    public async Task NameFiltersNarrowTheAssets()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"), ("Models/Tree", "tree"));
        var assets = fixture.Assets;
        fixture.SelectPackageRecursively();

        assets.AddAssetFilter(new AssetFilterViewModel(assets, FilterCategory.AssetName, "Wo", "Wo"));

        var filter = Assert.Single(assets.CurrentAssetFilters);
        Assert.True(filter.IsActive);
        Assert.Equal(["Wood"], assets.FilteredAssets.Select(x => x.Name));

        filter.IsActive = false;
        Assert.Equal(3, assets.FilteredAssets.Count);

        assets.ClearAssetFilters();
        Assert.Empty(assets.CurrentAssetFilters);
        Assert.Equal(3, assets.FilteredAssets.Count);
    }

    [Fact]
    public async Task TypeFiltersListTheAssetTypesOfTheSession()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var assets = fixture.Assets;
        fixture.SelectPackageRecursively();

        // Typing in the filter box proposes a name filter and the matching type filters
        assets.AssetFilterPattern = "TestEditor";

        Assert.Contains(assets.AvailableAssetFilters, x => x.Category == FilterCategory.AssetName && x.Filter == "TestEditor");
        var typeFilter = Assert.Single(assets.AvailableAssetFilters.Where(x => x.Category == FilterCategory.AssetType));
        Assert.Equal(typeof(TestEditorAsset).FullName, typeFilter.Filter);
        assets.AddAssetFilter(typeFilter);

        Assert.Equal(["Wood"], assets.FilteredAssets.Select(x => x.Name));
    }

    [Fact]
    public async Task RenamingAnAssetIsUndoable()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var asset = fixture.Session.AllAssets.Single();
        var undoRedo = fixture.Session.UndoRedoService!;

        using (var transaction = undoRedo.CreateTransaction())
        {
            asset.Name = "Oak";
            undoRedo.SetName(transaction, "Rename");
        }

        Assert.Equal(EditorSessionFixture.Url("Textures/Oak"), asset.Url);
        Assert.True(fixture.Session.IsDirty);
        // The action history records the transaction after the asset properties changed notification, asynchronously
        Assert.True(SpinWait.SpinUntil(() => fixture.Session.ActionHistory.CanUndo, TimeSpan.FromSeconds(10)));

        undoRedo.Undo();
        Assert.Equal(EditorSessionFixture.Url("Textures/Wood"), asset.Url);
        Assert.False(fixture.Session.IsDirty);

        undoRedo.Redo();
        Assert.Equal(EditorSessionFixture.Url("Textures/Oak"), asset.Url);
    }

    [Fact]
    public async Task RenamingTheSingleSelectedAssetRefreshesThePreview()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        var preview = new PreviewRecorder();
        fixture.ServiceProvider.RegisterService(preview);
        var wood = fixture.Session.AllAssets.Single(x => x.Name == "Wood");
        var stone = fixture.Session.AllAssets.Single(x => x.Name == "Stone");

        // The test plays the UI thread: it selects and renames on the dispatcher, one at a time with the refresh of the references
        // that the session dispatches from the thread pool after each rename
        var dispatcher = fixture.Session.Dispatcher;
        dispatcher.Invoke(() =>
        {
            fixture.Assets.SelectAssets([wood]);
            wood.Name = "Oak";
        });

        Assert.Equal([wood], preview.Assets);

        // With several assets selected, the preview shows none of them: nothing to refresh
        dispatcher.Invoke(() =>
        {
            fixture.Assets.SelectAssets([wood, stone]);
            wood.Name = "Pine";
        });

        Assert.Equal([wood], preview.Assets);
    }

    [Fact]
    public async Task DeletingAnAssetIsUndoable()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        var session = fixture.Session;
        var wood = session.AllAssets.Single(x => x.Name == "Wood");
        var directory = wood.Directory;
        fixture.OnUiThread(() => fixture.Assets.SelectedLocations.Add(directory));
        Assert.Equal(2, fixture.Assets.AssetCount);

        Assert.True(await fixture.Assets.DeleteContent([wood], skipConfirmation: true));

        Assert.True(wood.IsDeleted);
        Assert.Equal(["Stone"], session.AllAssets.Select(x => x.Name));
        Assert.DoesNotContain(wood, directory.Assets);
        Assert.Equal(1, fixture.Assets.AssetCount);
        Assert.Empty(fixture.Dialogs.Messages);

        session.UndoRedoService!.Undo();

        Assert.False(wood.IsDeleted);
        Assert.Equal(["Stone", "Wood"], session.AllAssets.Select(x => x.Name).Order());
        Assert.Contains(wood, directory.Assets);
        Assert.Equal(2, fixture.Assets.AssetCount);
    }

    [Fact]
    public async Task CopyAndPasteDuplicateTheAssetInTheSelectedFolder()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var session = fixture.Session;
        var assets = fixture.Assets;
        var wood = session.AllAssets.Single();

        fixture.OnUiThread(() => assets.SelectAssets([wood]));
        Assert.True(assets.CopyContentCommand.IsEnabled);
        assets.CopyContentCommand.Execute();
        await session.UndoRedoService!.TransactionCompletion;

        Assert.False(string.IsNullOrEmpty(fixture.Clipboard.Text));
        Assert.Contains("TestEditorAsset", fixture.Clipboard.Text);

        var target = fixture.Package.GetOrCreateAssetDirectory("Copies", false);
        fixture.OnUiThread(() => assets.ClearSelection());
        fixture.OnUiThread(() => assets.SelectedLocations.Clear());
        fixture.OnUiThread(() => assets.SelectedLocations.Add(target));
        assets.PasteCommand.Execute();
        await session.UndoRedoService.TransactionCompletion;

        var copy = Assert.Single(target.Assets);
        Assert.Equal(EditorSessionFixture.Url("Copies/Wood"), copy.Url);
        Assert.NotEqual(wood.Id, copy.Id);
        Assert.Equal("wood", ((TestEditorAsset)copy.Asset).Value);
        Assert.Equal(2, session.AllAssets.Count());
        Assert.Empty(fixture.Dialogs.Messages);
    }

    [Fact]
    public async Task PastingNeedsASingleTargetFolder()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var assets = fixture.Assets;
        fixture.Clipboard.Text = "irrelevant";

        assets.PasteCommand.Execute();
        await fixture.Session.UndoRedoService!.TransactionCompletion;

        Assert.Single(fixture.Dialogs.Messages);
        Assert.Single(fixture.Session.AllAssets);
    }

    private sealed class PreviewRecorder : IAssetPreviewUpdater
    {
        public List<AssetViewModel?> Assets { get; } = [];

        public void SetAssetToPreview(AssetViewModel? asset) => Assets.Add(asset);
    }
}
