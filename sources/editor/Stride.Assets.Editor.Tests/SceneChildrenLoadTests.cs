// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Assets.Editor.Tests.Helpers;
using Stride.Assets.Entities;
using Stride.Assets.Presentation.ViewModels;
using Stride.Core.Assets;
using Stride.Core.Assets.Diagnostics;
using Stride.Core.Diagnostics;
using Stride.Core.Serialization;
using Stride.Engine;

namespace Stride.Assets.Editor.Tests;

/// <summary>
/// A scene keeps a child scene only when both name each other: the parent in <see cref="SceneAsset.ChildrenIds"/>, the child in
/// <see cref="SceneAsset.Parent"/>. The load removes the other children from the list, as before, and logs a warning to the log of
/// the load (the one the log pane shows) for each, about the parent scene.
/// </summary>
public sealed class SceneChildrenLoadTests
{
    private const string Rule = "A scene keeps a child scene only when both name each other: the parent in ChildrenIds, the child in Parent.";

    [Fact]
    public async Task AChildThatDoesNotNameItsParentIsRemovedWithAWarning()
    {
        var root = new SceneAsset();
        var child = new SceneAsset();
        root.ChildrenIds.Add(child.Id);
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Root", root), ("Child", child));

        var rootViewModel = fixture.Asset<SceneViewModel>("Root");
        Assert.Empty(rootViewModel.Asset.ChildrenIds);
        Assert.Empty(rootViewModel.Children);
        Assert.Null(fixture.Asset<SceneViewModel>("Child").Parent);
        Assert.True(rootViewModel.IsDirty);

        var warning = SingleWarning(fixture);
        Assert.Equal(root.Id, warning.AssetReference!.Id);
        Assert.Contains($"The scene lists the scene '{AssetsSessionFixture.Url("Child")}' in ChildrenIds, but the Parent of that scene is empty: it is removed from the list.", warning.Text);
        Assert.EndsWith(Rule, warning.Text);
    }

    [Fact]
    public async Task AChildThatNamesAnotherParentIsRemovedWithAWarning()
    {
        var root = new SceneAsset();
        var other = new SceneAsset();
        var child = new SceneAsset();
        root.ChildrenIds.Add(child.Id);
        other.ChildrenIds.Add(child.Id);
        child.Parent = AttachedReferenceManager.CreateProxyObject<Scene>(other.Id, "Other");
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Root", root), ("Other", other), ("Child", child));

        Assert.Empty(fixture.Asset<SceneViewModel>("Root").Asset.ChildrenIds);
        Assert.Same(fixture.Asset<SceneViewModel>("Other"), fixture.Asset<SceneViewModel>("Child").Parent);

        var warning = SingleWarning(fixture);
        Assert.Equal(root.Id, warning.AssetReference!.Id);
        Assert.Contains($"but the Parent of that scene is '{AssetsSessionFixture.Url("Other")}'", warning.Text);
    }

    [Fact]
    public async Task AnIdOfNoSceneIsRemovedWithAWarning()
    {
        var root = new SceneAsset();
        var missing = AssetId.New();
        root.ChildrenIds.Add(missing);
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Root", root));

        Assert.Empty(fixture.Asset<SceneViewModel>("Root").Asset.ChildrenIds);

        var warning = SingleWarning(fixture);
        Assert.Equal(root.Id, warning.AssetReference!.Id);
        Assert.Contains($"The scene lists the id {missing} in ChildrenIds, but no scene of the session has this id: it is removed from the list.", warning.Text);
    }

    [Fact]
    public async Task ScenesThatNameEachOtherLoadWithoutAWarning()
    {
        var root = new SceneAsset();
        var child = new SceneAsset();
        root.ChildrenIds.Add(child.Id);
        child.Parent = AttachedReferenceManager.CreateProxyObject<Scene>(root.Id, "Root");
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Root", root), ("Child", child));

        Assert.Equal([child.Id], fixture.Asset<SceneViewModel>("Root").Asset.ChildrenIds);
        Assert.Empty(Warnings(fixture));
    }

    [Fact]
    public async Task AChildThatNamesAParentNotListingItIsAddedToTheParent()
    {
        var root = new SceneAsset();
        var child = new SceneAsset();
        child.Parent = AttachedReferenceManager.CreateProxyObject<Scene>(root.Id, "Root");
        await using var fixture = await AssetsSessionFixture.OpenAsync(initializeEditor: false, ("Root", root), ("Child", child));

        // As before: the child's Parent wins, and the parent gets the child; nothing is lost
        var rootViewModel = fixture.Asset<SceneViewModel>("Root");
        Assert.Equal([child.Id], rootViewModel.Asset.ChildrenIds);
        Assert.Same(rootViewModel, fixture.Asset<SceneViewModel>("Child").Parent);
        Assert.Empty(Warnings(fixture));
    }

    private static List<AssetLogMessage> Warnings(AssetsSessionFixture fixture)
        => [.. fixture.Result.Messages.OfType<AssetLogMessage>().Where(x => x.Type == LogMessageType.Warning && x.Text.Contains("ChildrenIds", StringComparison.Ordinal))];

    private static AssetLogMessage SingleWarning(AssetsSessionFixture fixture) => Assert.Single(Warnings(fixture));
}
