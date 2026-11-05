// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Settings;
using Stride.Core.IO;
using Stride.Core.Settings;

namespace Stride.Core.Assets.Editor.Common.Tests;

public sealed class TestMRUAdditionalData
{
    private readonly SettingsContainer container = new();
    private readonly SettingsKey<List<MRUAdditionalData>> key;
    private readonly SettingsProfile profile;

    public TestMRUAdditionalData()
    {
        key = new SettingsKey<List<MRUAdditionalData>>("Internal/MostRecentlyUsedSessionsData2", container, () => new List<MRUAdditionalData>());
        profile = container.CreateSettingsProfile(true);
    }

    [Fact]
    public void ASessionUpdatedWhenTheListIsFullIsKept()
    {
        var sessions = Enumerable.Range(0, MRUAdditionalDataCollection.MaxMRUCount).Select(x => new UFile($"/Projects/Game{x}/Game{x}.sln")).ToList();
        foreach (var session in sessions)
            CreateCollection().UpdateOpenedAssets(session, [AssetId.New()]);

        var opened = AssetId.New();
        var latest = new UFile("/Projects/Latest/Latest.sln");
        CreateCollection().UpdateOpenedAssets(latest, [opened]);

        var collection = CreateCollection();
        Assert.Equal([opened], collection.GetData(latest)?.OpenedAssets);
        // The least recently used one makes room
        Assert.Null(collection.GetData(sessions[0]));
        Assert.NotNull(collection.GetData(sessions[1]));
    }

    [Fact]
    public void UpdatingASessionMakesItTheMostRecent()
    {
        var sessions = Enumerable.Range(0, MRUAdditionalDataCollection.MaxMRUCount).Select(x => new UFile($"/Projects/Game{x}/Game{x}.sln")).ToList();
        foreach (var session in sessions)
            CreateCollection().UpdateOpenedAssets(session, []);

        // The oldest one is used again, through the update WPF uses for its layouts
        CreateCollection().Update(sessions[0], x => x.DockingLayout = "<Layout />");
        CreateCollection().UpdateOpenedAssets(new UFile("/Projects/Latest/Latest.sln"), []);

        var collection = CreateCollection();
        Assert.Equal("<Layout />", collection.GetData(sessions[0])?.DockingLayout);
        Assert.Null(collection.GetData(sessions[1]));
    }

    private MRUAdditionalDataCollection CreateCollection()
    {
        // The profile stands for the file: what was saved is what the next collection loads
        return new MRUAdditionalDataCollection(() => profile, key, () => { });
    }
}
