// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModel;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The tags of the selected assets, shown in the header of the property grid (<see cref="TagsViewModel"/>).
/// </summary>
public sealed class TagsViewModelTests
{
    [Fact]
    public async Task TheTagsAreTheUnionOfTheSelectedAssetsTags()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var (wood, stone) = (Asset(fixture, "Wood"), Asset(fixture, "Stone"));
        wood.Tags.Add("nature");
        wood.Tags.Add("brown");
        stone.Tags.Add("nature");

        fixture.Assets.SelectAssets([wood]);
        Assert.Equal(["brown", "nature"], Names(fixture));
        // A single asset shows no counter
        Assert.All(fixture.Session.AssetTags.Tags, x => Assert.Equal("", x.Counter));

        fixture.Assets.SelectAssets([wood, stone]);
        Assert.Equal(["brown", "nature"], Names(fixture));
        Assert.Equal(" (1)", Tag(fixture, "brown").Counter);
        Assert.True(Tag(fixture, "brown").CanAddToAll);
        Assert.Equal(TagsViewModel.AllCounter, Tag(fixture, "nature").Counter);
        Assert.False(Tag(fixture, "nature").CanAddToAll);

        fixture.Assets.ClearSelection();
        Assert.Empty(fixture.Session.AssetTags.Tags);
    }

    [Fact]
    public async Task AddingATagTagsEverySelectedAssetInOneUndoableStep()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var (wood, stone) = (Asset(fixture, "Wood"), Asset(fixture, "Stone"));
        var undoRedo = fixture.Session.UndoRedoService!;
        fixture.Assets.SelectAssets([wood, stone]);

        fixture.Session.AssetTags.AddTagCommand.Execute("rock");

        Assert.Equal(["rock"], wood.Tags);
        Assert.Equal(["rock"], stone.Tags);
        Assert.Equal(["rock"], Names(fixture));
        Assert.Equal(TagsViewModel.AllCounter, Tag(fixture, "rock").Counter);
        AssertHistoryEntry(fixture, "Added tag 'rock' to 2 assets");

        undoRedo.Undo();
        Assert.Empty(wood.Tags);
        Assert.Empty(stone.Tags);
        // The undo changes the assets' tags from outside: the list follows
        Assert.Empty(fixture.Session.AssetTags.Tags);

        undoRedo.Redo();
        Assert.Equal(["rock"], Names(fixture));
    }

    [Fact]
    public async Task ABlankTagAddsNothing()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"));
        var wood = Asset(fixture, "Wood");
        fixture.Assets.SelectAssets([wood]);

        fixture.Session.AssetTags.AddTagCommand.Execute("  ");

        Assert.Empty(wood.Tags);
        Assert.Empty(fixture.Session.AssetTags.Tags);
        Assert.False(fixture.Session.UndoRedoService!.CanUndo);
    }

    [Fact]
    public async Task AddingATagToAllCompletesTheAssetsThatMissIt()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var (wood, stone) = (Asset(fixture, "Wood"), Asset(fixture, "Stone"));
        wood.Tags.Add("nature");
        fixture.Assets.SelectAssets([wood, stone]);

        Tag(fixture, "nature").AddTagToAllCommand.Execute();

        Assert.Equal(["nature"], wood.Tags);
        Assert.Equal(["nature"], stone.Tags);
        Assert.Equal(TagsViewModel.AllCounter, Tag(fixture, "nature").Counter);
        AssertHistoryEntry(fixture, "Added tag 'nature' to " + stone.Url);
    }

    [Fact]
    public async Task RemovingATagUntagsEverySelectedAsset()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var (wood, stone) = (Asset(fixture, "Wood"), Asset(fixture, "Stone"));
        var undoRedo = fixture.Session.UndoRedoService!;
        wood.Tags.Add("nature");
        wood.Tags.Add("brown");
        stone.Tags.Add("nature");
        fixture.Assets.SelectAssets([wood, stone]);

        Tag(fixture, "nature").RemoveTagCommand.Execute();

        Assert.Equal(["brown"], wood.Tags);
        Assert.Empty(stone.Tags);
        AssertHistoryEntry(fixture, "Remove tag 'nature' from 2 assets");

        undoRedo.Undo();
        Assert.Contains("nature", wood.Tags);
        Assert.Contains("nature", stone.Tags);
        Assert.Equal(["brown", "nature"], Names(fixture));
    }

    [Fact]
    public async Task OnlyTheSelectedAssetsRefreshTheTags()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var (wood, stone) = (Asset(fixture, "Wood"), Asset(fixture, "Stone"));
        fixture.Assets.SelectAssets([wood]);
        fixture.Assets.SelectAssets([stone]);

        // The asset that left the selection no longer changes the tags shown
        wood.Tags.Add("nature");
        Assert.Empty(fixture.Session.AssetTags.Tags);

        stone.Tags.Add("rock");
        Assert.Equal(["rock"], Names(fixture));
    }

    private static AssetViewModel Asset(EditorSessionFixture fixture, string location)
    {
        return fixture.Session.GetAssetByUrl(EditorSessionFixture.Url(location)) ?? throw new InvalidOperationException($"No asset at {location}");
    }

    private static void AssertHistoryEntry(EditorSessionFixture fixture, string name)
    {
        // The action history records the transaction after the asset properties changed notification, asynchronously
        var history = fixture.Session.ActionHistory;
        Assert.True(SpinWait.SpinUntil(() => history.Transactions.Any(x => x.Name == name), TimeSpan.FromSeconds(10)), $"No history entry '{name}'");
    }

    private static TagViewModel Tag(EditorSessionFixture fixture, string name) => fixture.Session.AssetTags.Tags.Single(x => x.Name == name);

    private static List<string> Names(EditorSessionFixture fixture) => fixture.Session.AssetTags.Tags.Select(x => x.Name).ToList();
}
