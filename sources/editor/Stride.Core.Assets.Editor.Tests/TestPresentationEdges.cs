// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Editor.Components.Properties;
using Stride.Core.Assets.Presentation.Services;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Settings;
using Stride.Core.Storage;
using Stride.Core.Assets.Presentation.Components.Properties;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Assets.Presentation.ViewModels.Progress;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The session objects only use the Presentation-level services: the part of the plugin service in
/// <see cref="IPresentationPluginService"/>, and the tracker of the session for the updates from the source files.
/// </summary>
public class TestPresentationEdges
{
    // The package settings live in the settings container of the packages
    private static readonly SettingsKey<int> PackageSetting = new("Test/PresentationEdges/PackageSetting", PackageUserSettings.SettingsContainer, 1);

    [Fact]
    public void SessionNodeContainerTakesThePrimitiveTypesOfThePresentationPluginService()
    {
        var plugins = new PresentationPluginService();
        plugins.PrimitiveTypes.Add(typeof(PrimitiveBase));
        var session = new FakeSession(plugins);

        Assert.True(session.AssetNodeContainer.IsPrimitiveType(typeof(PrimitiveChild)));
        Assert.False(session.AssetNodeContainer.IsPrimitiveType(typeof(TestPresentationEdges)));
    }

    [Fact]
    public void LoadPackageInformationReportsItsProgressAndRegistersTheProfileSettings()
    {
        var plugins = new PresentationPluginService();
        plugins.ProfileSettings.Add(new PackageSettingsEntry(PackageSetting, TargetPackage.NonExecutable));
        var session = new FakeSession(plugins);
        var package = session.CreateLocalPackage("Game");
        var progress = new ProgressRecorder { Maximum = 1 };

        package.LoadPackageInformation(new LoggerResult(), progress);

        Assert.Equal("Package processed", progress.Messages[^1]);
        var settings = Assert.IsType<PackageSettingsWrapper>(((IPropertyProviderViewModel)package).GetRootNode()!.Retrieve());
        var setting = Assert.IsType<PackageSettingsWrapper.SettingsKeyWrapper<int>>(settings.UserSettings[PackageSetting.DisplayName]);
        Assert.Equal(1, setting.TypedValue);
    }

    [Fact]
    public void UpdateFromSourceGoesThroughTheTrackerOfTheSession()
    {
        var session = new FakeSession();
        var tracker = new SourceTrackerRecorder();
        session.SourceTracker = tracker;
        var asset = CreateAsset(session);

        asset.Sources.UpdateFromSourceCommand.Execute();

        var updated = Assert.Single(tracker.Updates);
        Assert.Equal([asset], updated);
        Assert.Empty(session.ProgressWindows.Shown);
    }

    [Fact]
    public void UpdateFromSourceWithoutTrackerUpdatesTheAssetInOneTransaction()
    {
        var session = new FakeSession();
        var asset = CreateAsset(session);
        var transactions = session.UndoRedo.RetrieveAllTransactions().Count();

        asset.Sources.UpdateFromSourceCommand.Execute();

        var all = session.UndoRedo.RetrieveAllTransactions().ToList();
        Assert.Equal(transactions + 1, all.Count);
        Assert.Equal("Update [Data/MyAsset] from its source(s)", session.UndoRedo.GetName(all[^1]));
        Assert.False(asset.Sources.NeedUpdateFromSource);
        Assert.Empty(session.ProgressWindows.Shown);
    }

    private static AssetViewModel CreateAsset(FakeSession session)
    {
        var package = session.CreateLocalPackage("Game");
        var directory = package.GetOrCreateAssetDirectory("Data", false);
        return package.CreateAsset(directory, new AssetItem("Data/MyAsset", new TestSessionAsset()), true, new LoggerResult());
    }

    private class PrimitiveBase;

    private sealed class PrimitiveChild : PrimitiveBase;

    /// <summary>
    /// A plugin service with only the Presentation-level part, to check that the session objects need nothing more.
    /// </summary>
    private sealed class PresentationPluginService : IPresentationPluginService
    {
        public List<Type> PrimitiveTypes { get; } = [];

        public List<PackageSettingsEntry> ProfileSettings { get; } = [];

        public IEnumerable<Type> GetPrimitiveTypes(ISessionViewModel session) => PrimitiveTypes;

        public IEnumerable<PackageSettingsEntry> GetProfileSettings() => ProfileSettings;
    }

    private sealed class ProgressRecorder : IProgressViewModel
    {
        public List<string> Messages { get; } = [];

        public double Maximum { get; set; }

        public double Minimum { get; set; }

        public double ProgressValue { get; set; }

        public void UpdateProgress(string message, double value)
        {
            Messages.Add(message);
            ProgressValue = value;
        }
    }

    private sealed class SourceTrackerRecorder : IAssetSourceTrackerViewModel
    {
        public List<IReadOnlyCollection<AssetViewModel>> Updates { get; } = [];

        public ObjectId GetCurrentHash(UFile file) => ObjectId.Empty;

        public void UpdateAssetStatus(AssetViewModel asset)
        {
        }

        public Task UpdateAssetsFromSourceAsync(IReadOnlyCollection<AssetViewModel> assets)
        {
            Updates.Add(assets);
            return Task.CompletedTask;
        }
    }
}
