// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The tracker of the asset files changed outside of the editor, when the watch of their folder failed and started again.
/// </summary>
public sealed class ExternalAssetChangeTrackerTests
{
    [Fact]
    public async Task TheAssetFilesChangedWhileTheWatchHadFailedAreBroughtIntoTheSession()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Wood", "wood"), ("Stone", "stone"));
        var session = fixture.Session;
        var watcher = new LosingDirectoryWatcher();
        session.ExternalAssetChanges!.Dispose();
        session.ExternalAssetChanges = new ExternalAssetChangeTracker(session, watcher);
        var assetFolder = Path.Combine(fixture.Directory, EditorSessionFixture.PackageName, "Assets");
        var wood = session.GetAssetByUrl(EditorSessionFixture.Url("Wood"))!;

        // The watcher reports none of these changes
        var woodFile = fixture.ReadAsset("Wood");
        woodFile.Value = "oak";
        AssetFileSerializer.Save(Path.Combine(assetFolder, "Wood.sdtesteditor"), woodFile, null);
        AssetFileSerializer.Save(Path.Combine(assetFolder, "Pine.sdtesteditor"), new TestEditorAsset { Value = "pine" }, null);
        watcher.RaiseChangesMissed(assetFolder);

        await ExclusiveDispatcherService.WaitUntilAsync(() => ((TestEditorAsset)wood.Asset).Value == "oak", "the reload of the changed asset");
        await ExclusiveDispatcherService.WaitUntilAsync(() => session.GetAssetByUrl(EditorSessionFixture.Url("Pine")) is not null, "the new asset");
        Assert.False(wood.IsDirty);
        Assert.Equal("stone", ((TestEditorAsset)session.GetAssetByUrl(EditorSessionFixture.Url("Stone"))!.Asset).Value);
    }
}
