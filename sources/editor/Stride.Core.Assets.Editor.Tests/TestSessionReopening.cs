// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// Opening a session, and opening it again, without any edit leaves it without unsaved changes, even when the opening changes
/// assets that cannot be saved: the assets of a read-only package (a NuGet package of the dependencies) that the session
/// reconciles with their archetype, as the voxel graphics compositor of Stride.Voxels is with the default one of the engine.
/// </summary>
public sealed class TestSessionReopening
{
    [Fact]
    public async Task AReadOnlyAssetReconciledWithItsArchetypeLeavesTheSessionClean()
    {
        var (archetype, derived) = ArchetypeAndOutdatedDerivedAsset();
        using var first = await EditorSessionFixture.OpenAsync(readOnly: true, ("Base", archetype), ("Derived", derived));
        AssertReconciled(first);

        await AssertCleanAsync(first);

        using var second = await first.ReopenAsync();
        AssertReconciled(second);
        await AssertCleanAsync(second);
    }

    [Fact]
    public async Task AnEditableAssetReconciledWithItsArchetypeStillHasToBeSaved()
    {
        var (archetype, derived) = ArchetypeAndOutdatedDerivedAsset();
        using var fixture = await EditorSessionFixture.OpenAsync(readOnly: false, ("Base", archetype), ("Derived", derived));
        var asset = AssertReconciled(fixture);

        // As in WPF: the change comes from the archetype, so it is not in the edit history, but it is not on disk yet
        Assert.True(asset.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
        Assert.True(fixture.Session.HasUnsavedChanges);
        Assert.Empty(fixture.Session.ActionHistory.Transactions);

        Assert.True(await fixture.Session.SaveSessionAsync());
        Assert.Equal("new", fixture.ReadAsset("Derived").Value);

        using var reopened = await fixture.ReopenAsync();
        await AssertCleanAsync(reopened);
    }

    /// <summary>
    /// An archetype, and an asset derived from it that was saved before the archetype changed: its value is not overridden,
    /// so the session gives it the value of the archetype when it opens.
    /// </summary>
    private static (TestEditorAsset Archetype, TestEditorAsset Derived) ArchetypeAndOutdatedDerivedAsset()
    {
        var archetype = new TestEditorAsset { Value = "new" };
        var derived = new TestEditorAsset { Value = "old", Archetype = new AssetReference(archetype.Id, "Base") };
        return (archetype, derived);
    }

    private static AssetViewModel AssertReconciled(EditorSessionFixture fixture)
    {
        var asset = fixture.Session.AllAssets.Single(x => x.Name == "Derived");
        Assert.Equal("new", ((TestEditorAsset)asset.Asset).Value);
        return asset;
    }

    private static async Task AssertCleanAsync(EditorSessionFixture fixture)
    {
        Assert.False(fixture.Session.IsDirty);
        Assert.False(fixture.Session.HasUnsavedAssets());
        Assert.False(fixture.Session.HasUnsavedChanges);

        // Closing asks nothing
        Assert.True(await fixture.Session.CloseAsync());
        Assert.Empty(fixture.Dialogs.Messages);
    }
}
