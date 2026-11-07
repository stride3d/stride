// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Diagnostics;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.ViewModels;
using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The messages of the Asset errors panel: the asset each one is about (Asset column) and its selection on a double-click.
/// </summary>
public sealed class TestAssetLogMessages
{
    [Fact]
    public void TheAssetOfAMessageIsTheOneItNames()
    {
        var id = AssetId.New();
        Assert.Equal(new UFile("Textures/Wood"), AssetLogViewModel.GetAssetUrl(new AssetSerializableLogMessage(id, new UFile("Textures/Wood"), LogMessageType.Error, "error")));
        Assert.Equal(new UFile("Textures/Stone"), AssetLogViewModel.GetAssetUrl(new AssetLogMessage(null, new AssetReference(id, new UFile("Textures/Stone")), LogMessageType.Warning, "warning")));

        // About no asset
        Assert.Null(AssetLogViewModel.GetAssetUrl(new LogMessage("Session", LogMessageType.Error, "error")));
        Assert.Null(AssetLogViewModel.GetAssetUrl(new AssetLogMessage(null, null, LogMessageType.Error, "error")));
        Assert.Null(AssetLogViewModel.GetAssetUrl(new AssetSerializableLogMessage { Type = LogMessageType.Error, Text = "error" }));
        Assert.Null(AssetLogViewModel.GetAssetUrl(null));
    }

    [Fact]
    public async Task AMessageOfTheBuildLogOfAnAssetIsAboutThisAsset()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var log = fixture.Session.AssetLog;
        var wood = fixture.Session.AllAssets.Single();
        var build = new LoggerResult();
        // A build message does not name its asset
        build.Error("The texture could not be compiled.");

        log.SetAssetLog(wood.Id, AssetLogViewModel.BuildLogName, build);
        // The log also has the verbose messages of the session load, which the panel hides
        await WaitForAsync(() => log.FilteredMessages.Any(x => x.Type == LogMessageType.Error));

        var message = log.FilteredMessages.Single(x => x.Type == LogMessageType.Error);
        Assert.Equal("The texture could not be compiled.", message.Text);
        Assert.Equal(wood.AssetItem.Location, AssetLogViewModel.GetAssetUrl(message));
        Assert.Equal(1, log.ErrorCount);
    }

    [Fact]
    public async Task ADoubleClickOnAMessageSelectsItsAsset()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Materials/Stone", "stone"));
        var log = fixture.Session.AssetLog;
        var wood = fixture.Session.AllAssets.Single(x => x.Name == "Wood");
        var stone = fixture.Session.AllAssets.Single(x => x.Name == "Stone");
        fixture.Assets.SelectAssets([stone]);

        log.SelectMessageAssetCommand.Execute(new AssetSerializableLogMessage(wood.Id, wood.AssetItem.Location, LogMessageType.Error, "error"));

        // The folder of the asset is shown, as WPF does
        Assert.Equal([wood], fixture.Assets.SelectedAssets);
        Assert.Contains(wood.Directory, fixture.Assets.SelectedLocations);

        log.SelectMessageAssetCommand.Execute(new AssetLogMessage(null, new AssetReference(stone.Id, stone.AssetItem.Location), LogMessageType.Error, "error"));

        Assert.Equal([stone], fixture.Assets.SelectedAssets);
    }

    [Fact]
    public async Task ADoubleClickOnAMessageAboutNoAssetChangesNothing()
    {
        using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var log = fixture.Session.AssetLog;
        var wood = fixture.Session.AllAssets.Single();
        fixture.Assets.SelectAssets([wood]);

        log.SelectMessageAssetCommand.Execute(new LogMessage("Session", LogMessageType.Error, "error"));
        // An asset that is not in the session (deleted since the message)
        log.SelectMessageAssetCommand.Execute(new AssetSerializableLogMessage(AssetId.New(), new UFile("Textures/Gone"), LogMessageType.Error, "error"));
        log.SelectMessageAssetCommand.Execute(null);

        Assert.Equal([wood], fixture.Assets.SelectedAssets);
    }

    private static async Task WaitForAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow + TimeSpan.FromSeconds(30);
        while (!condition())
        {
            if (DateTime.UtcNow > timeout)
                throw new TimeoutException();
            await Task.Delay(10);
        }
    }
}
