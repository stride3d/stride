// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Services;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Presentation.Services;

namespace Stride.Core.Assets.Editor.Tests;

public sealed class TestSessionOpening
{
    [Fact]
    public async Task OpeningAPackageLoadsItsAssets()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"), ("Models/Tree", "tree"));
        var session = fixture.Session;

        Assert.False(fixture.Result.HasErrors, fixture.Log);
        Assert.Empty(fixture.Dialogs.Messages);
        Assert.Same(session, SessionViewModel.Instance);

        var package = Assert.Single(session.LocalPackages);
        Assert.Equal(EditorSessionFixture.PackageName, package.Name);
        Assert.True(package.IsEditable);
        Assert.Equal(["Stone", "Tree", "Wood"], session.AllAssets.Select(x => x.Name).Order());
        Assert.Equal([EditorSessionFixture.Url("Models/Tree"), EditorSessionFixture.Url("Textures/Stone"), EditorSessionFixture.Url("Textures/Wood")], session.AllAssets.Select(x => x.Url).Order());
        Assert.All(session.AllAssets, asset => Assert.IsType<TestEditorAsset>(asset.Asset));
        Assert.False(session.IsDirty);
        Assert.Single(fixture.Dialogs.ProgressWindows);
    }

    [Fact]
    public async Task TheAssetCollectionIsTheMainViewOfTheSession()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Models/Tree", "tree"));
        var session = fixture.Session;

        Assert.IsAssignableFrom<IAssetCollectionViewModel>(session.AssetCollection);
        Assert.True(session.AssetCollection.IsMainView);
        Assert.Same(session, session.AssetCollection.Session);
        // Nothing is listed until a location is selected
        Assert.Equal(0, session.AssetCollection.AssetCount);
        fixture.SelectPackageRecursively();
        Assert.Equal(2, session.AssetCollection.AssetCount);
        Assert.Equal(session.AllAssets.OrderBy(x => x.Name), session.AssetCollection.Assets.OrderBy(x => x.Name));
        Assert.Same(session.AssetCollection, ((ISessionViewModel)session).ActiveAssetView);
        Assert.Same(session.AssetCollection.AssetViewProperties, session.AssetViewProperties);
    }

    [Fact]
    public async Task TheEditorCollectionIsSeparateFromTheAssetCollection()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var session = fixture.Session;

        Assert.IsAssignableFrom<IAssetEditorsManager>(session.EditorCollection);
        Assert.Empty(session.EditorCollection.Editors);
        Assert.Null(session.EditorCollection.ActiveEditor);

        // No plugin provides an editor for the test asset: opening it does nothing
        session.EditorCollection.OpenAssetEditor(session.AllAssets.Single());

        Assert.Empty(session.EditorCollection.Editors);
        Assert.Null(session.EditorCollection.ActiveEditor);
    }

    [Fact]
    public async Task TheSessionServicesAreRegisteredInTheProvider()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));

        var undoRedo = fixture.ServiceProvider.Get<IUndoRedoService>();
        Assert.Same(undoRedo, fixture.Session.UndoRedoService);
        Assert.Equal(200, undoRedo.Capacity);
        Assert.False(undoRedo.CanUndo);
        Assert.NotNull(fixture.ServiceProvider.TryGet<ICopyPasteService>());
        Assert.NotNull(fixture.ServiceProvider.TryGet<SelectionService>());
        Assert.NotNull(fixture.ServiceProvider.TryGet<UserDocumentationService>());
        Assert.Empty(fixture.Session.ActionHistory.Transactions);
    }

    [Fact]
    public async Task SavingWritesTheModifiedAssets()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var asset = fixture.Session.AllAssets.Single();

        using (var transaction = fixture.Session.UndoRedoService!.CreateTransaction())
        {
            ((TestEditorAsset)asset.Asset).Value = "oak";
            asset.AssetItem.IsDirty = true;
            fixture.Session.UndoRedoService.SetName(transaction, "Edit");
        }

        Assert.True(await fixture.Session.SaveSessionAsync());
        Assert.Equal("oak", fixture.ReadAsset("Textures/Wood").Value);
        Assert.False(fixture.Session.IsDirty);
    }
}
