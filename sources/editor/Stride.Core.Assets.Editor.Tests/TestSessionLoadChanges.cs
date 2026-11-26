// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Presentation.Windows;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The changes that a load makes to the packages are saved at once only when the user accepted a package upgrade during the load;
/// the others (here an asset moved off a URL that another asset has) wait for the user's save, and closing asks for it.
/// </summary>
public sealed class TestSessionLoadChanges
{
    private static readonly string[] CollidingFiles = ["Echo.sdtesteditor", "Echo.sdtestother"];

    [Fact]
    public async Task AnAssetMovedOffATakenUrlIsNotSavedWhenTheSessionOpens()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Echo", "editor"), ("Echo" + EditorSessionFixture.OtherSuffix, "other"));

        // The load gave one of the two assets another URL, in the session only
        Assert.Contains(fixture.Session.AllAssets, x => x.Url == EditorSessionFixture.Url("Echo (2)"));
        Assert.Equal(CollidingFiles, fixture.AssetFiles());
        Assert.True(fixture.Session.HasUnsavedChanges);
        Assert.Empty(fixture.Dialogs.Messages);
        AssertNoBackup(fixture);
    }

    [Fact]
    public async Task ClosingASessionThatTheLoadChangedAsksAndDontSaveLeavesTheFiles()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Echo", "editor"), ("Echo" + EditorSessionFixture.OtherSuffix, "other"));
        fixture.Dialogs.PickButton = buttons => Button(buttons, "Don't save");

        Assert.True(await fixture.Session.CloseAsync());

        Assert.Equal("The project has unsaved changes. Do you want to save it?", Assert.Single(fixture.Dialogs.Messages));
        Assert.Equal(CollidingFiles, fixture.AssetFiles());
        AssertNoBackup(fixture);
    }

    [Fact]
    public async Task ClosingASessionThatTheLoadChangedAsksAndSaveWritesTheChanges()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Echo", "editor"), ("Echo" + EditorSessionFixture.OtherSuffix, "other"));
        fixture.Dialogs.PickButton = buttons => Button(buttons, "Save");

        Assert.True(await fixture.Session.CloseAsync());

        Assert.Single(fixture.Dialogs.Messages);
        Assert.Contains("Echo (2)", string.Join(" ", fixture.AssetFiles()));
    }

    [Fact]
    public async Task ClosingASessionWhoseUnsavedChangesAreDroppedDoesntAsk()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Echo", "editor"), ("Echo" + EditorSessionFixture.OtherSuffix, "other"));

        fixture.Session.DropUnsavedChanges();

        Assert.True(await fixture.Session.CloseAsync());
        Assert.Empty(fixture.Dialogs.Messages);
        Assert.Equal(CollidingFiles, fixture.AssetFiles());
    }

    [Fact]
    public async Task AnAcceptedPackageUpgradeIsSavedWhenTheSessionOpens()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(UpgradePackage, ("Wood", "wood"));

        // The upgrade question was asked and accepted (the first button), and the upgraded package saved
        Assert.Single(fixture.Dialogs.Messages);
        Assert.False(fixture.Session.HasUnsavedChanges);
        Assert.Null(fixture.Result.Session.UpgradeBackup);
    }

    // What a package upgrade does in the load: the editor asks the user, then the upgrade changes the package
    private static void UpgradePackage(PackageSessionResult result, PackageLoadParameters loadParameters)
    {
        var package = result.Session.LocalPackages.Single();
        if (PackageLoadParameters.ShouldUpgrade(loadParameters.PackageUpgradeRequested!(package, [])))
            package.IsDirty = true;
    }

    private static int Button(IReadOnlyCollection<DialogButtonInfo> buttons, string content) => buttons.Single(x => Equals(x.Content, content)).Result;

    private static void AssertNoBackup(EditorSessionFixture fixture)
    {
        Assert.Empty(Directory.EnumerateDirectories(fixture.Directory, ".stride-backup*", SearchOption.AllDirectories));
    }
}
