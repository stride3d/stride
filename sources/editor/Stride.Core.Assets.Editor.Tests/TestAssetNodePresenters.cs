// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Keys;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The behaviors of the WPF node presenters and asset node view models that the agnostic copies took over: reorderable collections,
/// and a multi-selection that is inherited or overridden only when every selected asset is.
/// </summary>
public sealed class TestAssetNodePresenters
{
    [Fact]
    public async Task ReorderableCollectionsGetTheReorderItemViewModel()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var asset = CreateAsset(fixture, "Lists", new TestListEditorAsset());

        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync([asset]);

        var reorderable = graph!.RootNode.GetChild(nameof(TestListEditorAsset.Reorderable))!;
        var fixedOrder = graph.RootNode.GetChild(nameof(TestListEditorAsset.FixedOrder))!;
        Assert.IsType<ReorderCollectionItemViewModel>(reorderable.AssociatedData[CollectionData.ReorderCollectionItem]);
        Assert.False(fixedOrder.AssociatedData.ContainsKey(CollectionData.ReorderCollectionItem));
    }

    [Fact]
    public async Task ADerivedAssetAloneHasABase()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var derived = CreateDerivedAsset(fixture, wood);

        var value = await ValueNodeOf(fixture, derived);

        Assert.True(value.HasBase);
        Assert.True(value.IsInherited);
        Assert.False(value.IsOverridden);
    }

    [Fact]
    public async Task AMultiSelectionHasABaseOnlyWhenEveryAssetHasOne()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var derived = CreateDerivedAsset(fixture, wood);

        // The derived asset comes first: the node must not answer for the first asset only
        var value = await ValueNodeOf(fixture, derived, wood);

        Assert.False(value.HasBase);
        Assert.False(value.IsInherited);
        Assert.False(value.IsOverridden);
    }

    [Fact]
    public async Task AMultiSelectionIsOverriddenOnlyWhenEveryAssetOverridesTheProperty()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var wood = fixture.Session.AllAssets.Single();
        var first = CreateDerivedAsset(fixture, wood);
        var second = CreateDerivedAsset(fixture, wood);
        Edit(fixture, first, "oak");

        var partly = await ValueNodeOf(fixture, first, second);
        Assert.True(partly.HasBase);
        Assert.False(partly.IsOverridden);

        Edit(fixture, second, "pine");

        var both = await ValueNodeOf(fixture, first, second);
        Assert.True(both.IsOverridden);
        Assert.False(both.IsInherited);
    }

    private static async Task<AssetNodeViewModel> ValueNodeOf(EditorSessionFixture fixture, params AssetViewModel[] assets)
    {
        var graph = await fixture.Session.AssetViewProperties.GenerateSelectionPropertiesAsync(assets);
        return Assert.IsType<AssetNodeViewModel>(graph!.RootNode.GetChild(nameof(TestEditorAsset.Value)));
    }

    private static void Edit(EditorSessionFixture fixture, AssetViewModel asset, string value)
    {
        var undoRedo = fixture.Session.UndoRedoService!;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph.RootNode[nameof(TestEditorAsset.Value)].Update(value);
        undoRedo.SetName(transaction, "Edit " + value);
    }

    private static AssetViewModel CreateDerivedAsset(EditorSessionFixture fixture, AssetViewModel baseAsset)
    {
        var name = baseAsset.Name + "-Derived" + fixture.Session.AllAssets.Count();
        return CreateAsset(fixture, name, baseAsset.AssetItem.CreateDerivedAsset());
    }

    private static AssetViewModel CreateAsset(EditorSessionFixture fixture, string location, Asset asset)
    {
        var directory = fixture.Package.AssetMountPoint;
        return fixture.Package.CreateAsset(directory, new AssetItem(location, asset), false, null);
    }
}

/// <summary>
/// An asset with a collection that can be reordered and one that cannot.
/// </summary>
[DataContract("TestListEditorAsset")]
[AssetDescription(".sdtestlist")]
public sealed class TestListEditorAsset : Asset
{
    [MemberCollection(CanReorderItems = true)]
    public List<string> Reorderable { get; } = [];

    public List<string> FixedOrder { get; } = [];
}
