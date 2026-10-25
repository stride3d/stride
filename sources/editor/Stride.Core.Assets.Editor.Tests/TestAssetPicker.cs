// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The asset picker that the content-reference properties open (<see cref="AssetPickerViewModel"/>).
/// </summary>
public sealed class TestAssetPicker
{
    [Fact]
    public async Task ThePickerListsTheAcceptedTypesOnly()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone" + EditorSessionFixture.OtherSuffix, "stone"), ("Tree", "tree"));

        var picker = new AssetPickerViewModel(fixture.Session, [typeof(TestEditorAsset)]);

        Assert.Equal(["Tree", "Wood"], picker.FilteredAssets.Select(x => x.Name));
        Assert.Null(picker.SelectedAsset);
    }

    [Fact]
    public async Task NoAcceptedTypeListsEveryAsset()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone" + EditorSessionFixture.OtherSuffix, "stone"));

        var picker = new AssetPickerViewModel(fixture.Session, []);

        Assert.Equal(["Stone", "Wood"], picker.FilteredAssets.Select(x => x.Name));
    }

    [Fact]
    public async Task TheFilterAndTheSearchTextNarrowTheList()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Woodland", "woodland"), ("Stone", "stone"));

        var picker = new AssetPickerViewModel(fixture.Session, [typeof(TestEditorAsset)], x => x.Name != "Woodland");
        Assert.Equal(["Stone", "Wood"], picker.FilteredAssets.Select(x => x.Name));

        picker.SearchText = "WO";
        Assert.Equal(["Wood"], picker.FilteredAssets.Select(x => x.Name));

        picker.SearchText = "";
        Assert.Equal(["Stone", "Wood"], picker.FilteredAssets.Select(x => x.Name));
    }

    [Fact]
    public async Task ALocationListsItsAssetsAndTheOnesOfItsSubFolders()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Old/Stone", "stone"), ("Models/Tree", "tree"));
        var picker = new AssetPickerViewModel(fixture.Session, []);
        var textures = fixture.Package.AssetMountPoint.SubDirectories.Single(x => x.Name == "Textures");

        picker.SelectedLocation = textures;
        Assert.Equal(["Stone", "Wood"], picker.FilteredAssets.Select(x => x.Name));

        picker.SelectedLocation = fixture.Package;
        Assert.Equal(["Stone", "Tree", "Wood"], picker.FilteredAssets.Select(x => x.Name));

        picker.SelectedLocation = null;
        Assert.Equal(["Stone", "Tree", "Wood"], picker.FilteredAssets.Select(x => x.Name));
        Assert.Contains(fixture.Package, picker.Packages);
    }

    [Fact]
    public async Task ThePickerOpensOnTheInitialAssetAndItsFolder()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Models/Tree", "tree"));
        var wood = fixture.Session.AllAssets.Single(x => x.Name == "Wood");

        var picker = new AssetPickerViewModel(fixture.Session, [], initialAsset: wood);

        Assert.Same(wood.Directory, picker.SelectedLocation);
        Assert.Same(wood, picker.SelectedAsset);
        Assert.Equal(["Wood"], picker.FilteredAssets.Select(x => x.Name));
    }

    [Fact]
    public async Task AnInitialAssetThatIsNotAcceptedIsNotSelected()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone" + EditorSessionFixture.OtherSuffix, "stone"));
        var stone = fixture.Session.AllAssets.Single(x => x.Name == "Stone");

        var picker = new AssetPickerViewModel(fixture.Session, [typeof(TestEditorAsset)], initialAsset: stone);

        Assert.Null(picker.SelectedAsset);
        Assert.Equal(["Wood"], picker.FilteredAssets.Select(x => x.Name));
    }

    [Fact]
    public async Task NarrowingTheListClearsASelectionItNoLongerHolds()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var picker = new AssetPickerViewModel(fixture.Session, []);
        picker.SelectedAsset = picker.FilteredAssets.Single(x => x.Name == "Stone");

        picker.SearchText = "Wood";

        Assert.Null(picker.SelectedAsset);
    }
}
