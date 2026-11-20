// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.Json;
using Stride.Core.Assets.Editor.Components.Recovery;
using Stride.Core.Assets.Editor.Tests.Helpers;
using Stride.Core.Assets.Presentation.ViewModels;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Editor.Tests;

/// <summary>
/// The recovery copies of a session: written outside of the project for its unsaved changes only, without changing the session,
/// removed by a save or a clean close.
/// </summary>
public sealed class SessionRecoveryServiceTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "Stride.Core.Assets.Editor.Tests", "recovery-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    [Fact]
    public async Task TheCopiesHoldOnlyTheChangedAssetsAndLeaveTheSessionAsItIs()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        using var recovery = Start(fixture);
        var wood = Asset(fixture, "Wood");
        await EditAsync(fixture, wood, "oak");
        var history = fixture.Session.ActionHistory.Transactions.Count;
        fixture.OnUiThread(() => fixture.Assets.SelectAssets([wood]));

        Assert.True(await recovery.WriteCopiesAsync());

        var manifest = ReadManifest(recovery);
        var package = Assert.Single(manifest.Packages);
        var copy = Assert.Single(package.Assets);
        Assert.Equal((Guid)wood.Id, copy.Id);
        Assert.Equal(EditorSessionFixture.Url("Textures/Wood"), copy.Location);
        Assert.Contains("oak", ReadCopy(recovery, manifest, copy));
        Assert.Empty(package.DeletedAssets);
        // The session is as it was: dirty, with the same history and selection, and its files untouched
        Assert.True(wood.IsDirty);
        Assert.True(wood.AssetItem.IsDirty);
        Assert.True(fixture.Session.HasUnsavedAssets());
        Assert.Equal(history, fixture.Session.ActionHistory.Transactions.Count);
        Assert.Equal([wood], fixture.Assets.SelectedAssets);
        Assert.Equal("wood", fixture.ReadAsset("Textures/Wood").Value);
        Assert.DoesNotContain(root, Path.GetFullPath(fixture.Directory));
        Assert.NotNull(recovery.LastCopyTime);

        // Nothing changed since: nothing written again
        Assert.False(await recovery.WriteCopiesAsync());
    }

    [Fact]
    public async Task TheCopiesHoldTheNewAndTheDeletedAssets()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"));
        using var recovery = Start(fixture);
        var stone = Asset(fixture, "Stone");
        var pine = CreateAsset(fixture, "Trees/Pine", "pine");
        DeleteAsset(fixture, stone);

        Assert.True(await recovery.WriteCopiesAsync());

        var package = Assert.Single(ReadManifest(recovery).Packages);
        var created = Assert.Single(package.Assets);
        Assert.Equal((Guid)pine.Id, created.Id);
        Assert.Equal(EditorSessionFixture.Url("Trees/Pine"), created.Location);
        var deleted = Assert.Single(package.DeletedAssets);
        Assert.Equal((Guid)stone.Id, deleted.Id);
        Assert.Equal(EditorSessionFixture.Url("Textures/Stone"), deleted.Location);
        Assert.Equal(["Textures/Stone.sdtesteditor", "Textures/Wood.sdtesteditor"], fixture.AssetFiles());
    }

    [Fact]
    public async Task ASaveRemovesTheCopiesAndACleanCloseRemovesTheFolder()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var recovery = Start(fixture);
        await EditAsync(fixture, Asset(fixture, "Wood"), "oak");
        Assert.True(await recovery.WriteCopiesAsync());
        var folder = recovery.ProcessFolder!;
        Assert.True(File.Exists(Path.Combine(folder, RecoveryStore.ManifestFileName)));

        Assert.True(await fixture.Session.SaveSessionAsync());

        Assert.False(File.Exists(Path.Combine(folder, RecoveryStore.ManifestFileName)));
        Assert.Empty(Directory.EnumerateDirectories(folder));
        // The marker stays while the session is open
        Assert.True(File.Exists(Path.Combine(folder, RecoveryStore.MarkerFileName)));
        Assert.False(await recovery.WriteCopiesAsync());

        recovery.Dispose();

        Assert.False(Directory.Exists(folder));
    }

    [Fact]
    public async Task TheTimerWaitsForTheSaveAndForTheEditorsConditions()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        using var recovery = Start(fixture);
        await EditAsync(fixture, Asset(fixture, "Wood"), "oak");

        // The save is under way: IsSaving is set until it has written the assets
        var save = fixture.Session.SaveSessionAsync();
        Assert.True(fixture.Session.IsSaving);
        Assert.False(await recovery.WriteCopiesIfDueAsync());
        Assert.True(await save);

        await EditAsync(fixture, Asset(fixture, "Wood"), "pine");
        var busy = true;
        recovery.AddSkipCondition(() => busy);
        Assert.False(await recovery.WriteCopiesIfDueAsync());
        Assert.Null(recovery.LastCopyTime);

        busy = false;
        Assert.True(await recovery.WriteCopiesIfDueAsync());

        // Off: no copies, and those written are removed
        recovery.IsEnabled = false;
        Assert.False(File.Exists(Path.Combine(recovery.ProcessFolder!, RecoveryStore.ManifestFileName)));
        await EditAsync(fixture, Asset(fixture, "Wood"), "oak");
        Assert.False(await recovery.WriteCopiesIfDueAsync());
    }

    [Fact]
    public async Task TheTimerFollowsTheIntervalAndTheSwitch()
    {
        await using var fixture = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var clock = new ManualTimeProvider();
        using var recovery = new SessionRecoveryService(fixture.Session, fixture.PackagePath, root) { TimeProvider = clock };
        Assert.Null(clock.Timer);

        recovery.Start();

        Assert.Equal(SessionRecoveryService.DefaultInterval, clock.Timer!.Period);
        recovery.Interval = TimeSpan.FromMinutes(1);
        Assert.Equal(TimeSpan.FromMinutes(1), clock.Timer.Period);
        recovery.IsEnabled = false;
        Assert.True(clock.Timer.IsDisposed);
        recovery.IsEnabled = true;
        Assert.False(clock.Timer.IsDisposed);
        Assert.Equal(TimeSpan.FromMinutes(1), clock.Timer.Period);
    }

    private SessionRecoveryService Start(EditorSessionFixture fixture)
    {
        // No timer: the tests write the copies themselves, and a test leaves no timer behind
        var recovery = new SessionRecoveryService(fixture.Session, fixture.PackagePath, root) { TimeProvider = new ManualTimeProvider() };
        recovery.Start();
        return recovery;
    }

    private static AssetViewModel Asset(EditorSessionFixture fixture, string name) => fixture.Session.AllAssets.Single(x => x.Name == name);

    private static async Task EditAsync(EditorSessionFixture fixture, AssetViewModel asset, string value)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        var count = fixture.Session.ActionHistory.Transactions.Count;
        using (var transaction = undoRedo.CreateTransaction())
        {
            asset.PropertyGraph!.RootNode[nameof(TestEditorAsset.Value)].Update(value);
            undoRedo.SetName(transaction, "Edit " + value);
        }
        // The action history records the transaction asynchronously
        await ExclusiveDispatcherService.WaitUntilAsync(() => fixture.Session.ActionHistory.Transactions.Count > count, "the edit in the history");
    }

    private static AssetViewModel CreateAsset(EditorSessionFixture fixture, string location, string value)
    {
        var package = fixture.Package;
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        var directory = package.GetOrCreateAssetDirectory(package.GetDisplayDirectory(location), true);
        var asset = package.CreateAsset(directory, new AssetItem(location, new TestEditorAsset { Value = value }), true, new LoggerResult());
        undoRedo.SetName(transaction, "Create " + location);
        return asset;
    }

    private static void DeleteAsset(EditorSessionFixture fixture, AssetViewModel asset)
    {
        Assert.Equal(1, fixture.Assets.DeleteAssets([asset], forceDelete: true));
    }

    private static RecoveryManifest ReadManifest(SessionRecoveryService recovery)
    {
        var path = Path.Combine(recovery.ProcessFolder!, RecoveryStore.ManifestFileName);
        return JsonSerializer.Deserialize<RecoveryManifest>(File.ReadAllBytes(path))!;
    }

    private static string ReadCopy(SessionRecoveryService recovery, RecoveryManifest manifest, RecoveredAsset asset)
        => File.ReadAllText(Path.Combine(recovery.ProcessFolder!, manifest.Folder!, asset.File.ToString()));

    /// <summary>
    /// A clock whose timer never fires by itself.
    /// </summary>
    private sealed class ManualTimeProvider : TimeProvider
    {
        public ManualTimer? Timer { get; private set; }

        public override ITimer CreateTimer(TimerCallback callback, object? state, TimeSpan dueTime, TimeSpan period)
        {
            Timer = new ManualTimer { Period = period };
            return Timer;
        }
    }

    private sealed class ManualTimer : ITimer
    {
        public TimeSpan Period { get; set; }

        public bool IsDisposed { get; private set; }

        public bool Change(TimeSpan dueTime, TimeSpan period)
        {
            Period = period;
            return true;
        }

        public void Dispose() => IsDisposed = true;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return ValueTask.CompletedTask;
        }
    }
}
