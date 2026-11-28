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
/// removed by a save or a clean close, and restored as unsaved changes when the session is opened after a crash.
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
        Edit(fixture, wood, "oak");
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
        Edit(fixture, Asset(fixture, "Wood"), "oak");
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
        Edit(fixture, Asset(fixture, "Wood"), "oak");

        // The save is under way: IsSaving is set until it has written the assets
        var save = fixture.Session.SaveSessionAsync();
        Assert.True(fixture.Session.IsSaving);
        Assert.False(await recovery.WriteCopiesIfDueAsync());
        Assert.True(await save);

        Edit(fixture, Asset(fixture, "Wood"), "pine");
        var busy = true;
        recovery.AddSkipCondition(() => busy);
        Assert.False(await recovery.WriteCopiesIfDueAsync());
        Assert.Null(recovery.LastCopyTime);

        busy = false;
        Assert.True(await recovery.WriteCopiesIfDueAsync());

        // Off: no copies, and those written are removed
        recovery.IsEnabled = false;
        Assert.False(File.Exists(Path.Combine(recovery.ProcessFolder!, RecoveryStore.ManifestFileName)));
        Edit(fixture, Asset(fixture, "Wood"), "oak");
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

    [Fact]
    public async Task ARestoreBringsBackEachChangeAsUnsaved()
    {
        await using var crashed = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"), ("Textures/Stone", "stone"), ("Textures/Sand", "sand"));
        var before = Start(crashed);
        Edit(crashed, Asset(crashed, "Wood"), "oak");
        CreateAsset(crashed, "Trees/Pine", "pine");
        DeleteAsset(crashed, Asset(crashed, "Stone"));
        crashed.OnUiThread(() => Asset(crashed, "Sand").Name = "Dune");
        Assert.True(await before.WriteCopiesAsync());

        // The editor is gone without closing the session: its copies stay
        await using var fixture = await crashed.ReopenAsync();
        using var recovery = Start(fixture);
        recovery.IsProcessAlive = _ => false;
        var found = recovery.FindRecovery();
        Assert.NotNull(found);
        Assert.Equal(4, found.AssetCount);

        Assert.True(await recovery.RestoreAsync(found));

        var session = fixture.Session;
        var wood = Asset(fixture, "Wood");
        Assert.Equal("oak", ((TestEditorAsset)wood.Asset).Value);
        Assert.True(wood.IsDirty);
        var pine = Asset(fixture, "Pine");
        Assert.Equal(EditorSessionFixture.Url("Trees/Pine"), pine.AssetItem.Location.FullPath);
        Assert.True(pine.IsDirty);
        Assert.DoesNotContain(session.AllAssets, x => x.Name == "Stone");
        var dune = Asset(fixture, "Dune");
        Assert.Equal(EditorSessionFixture.Url("Textures/Dune"), dune.AssetItem.Location.FullPath);
        Assert.True(session.HasUnsavedAssets());
        // Nothing written to the project
        Assert.Equal(["Textures/Sand.sdtesteditor", "Textures/Stone.sdtesteditor", "Textures/Wood.sdtesteditor"], fixture.AssetFiles());
        // The copies are this editor's now
        Assert.Null(recovery.FindRecovery());
        Assert.Equal(4, ReadManifest(recovery).Packages.Single().Assets.Count + ReadManifest(recovery).Packages.Single().DeletedAssets.Count);

        // One step of the history, which undoes the restore
        var transaction = Assert.Single(session.ActionHistory.Transactions);
        Assert.Equal("Restore the recovered changes", transaction.Name);
        session.UndoRedoService.Undo();
        Assert.Equal("wood", ((TestEditorAsset)wood.Asset).Value);
        Assert.Contains(session.AllAssets, x => x.Name == "Stone");
        Assert.DoesNotContain(session.AllAssets, x => x.Name == "Pine");
        Assert.Contains(session.AllAssets, x => x.Name == "Sand");
        session.UndoRedoService.Redo();

        // A save writes them and removes the copies
        Assert.True(await session.SaveSessionAsync());
        Assert.Equal(["Textures/Dune.sdtesteditor", "Textures/Wood.sdtesteditor", "Trees/Pine.sdtesteditor"], fixture.AssetFiles());
        Assert.Equal("oak", fixture.ReadAsset("Textures/Wood").Value);
        Assert.False(File.Exists(Path.Combine(recovery.ProcessFolder!, RecoveryStore.ManifestFileName)));
    }

    [Fact]
    public async Task DiscardingRemovesTheCopiesAndChangesNothing()
    {
        await using var crashed = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var before = Start(crashed);
        Edit(crashed, Asset(crashed, "Wood"), "oak");
        Assert.True(await before.WriteCopiesAsync());

        await using var fixture = await crashed.ReopenAsync();
        using var recovery = Start(fixture);
        recovery.IsProcessAlive = _ => false;
        var found = recovery.FindRecovery();
        Assert.NotNull(found);

        recovery.Discard(found);

        Assert.Null(recovery.FindRecovery());
        Assert.False(Directory.Exists(before.ProcessFolder));
        Assert.Equal("wood", ((TestEditorAsset)Asset(fixture, "Wood").Asset).Value);
        Assert.False(fixture.Session.HasUnsavedAssets());
    }

    [Fact]
    public async Task AnotherEditorDoesNotTakeTheCopiesOfAnEditorThatRuns()
    {
        await using var first = await EditorSessionFixture.OpenAsync(("Textures/Wood", "wood"));
        var running = Start(first);
        Edit(first, Asset(first, "Wood"), "oak");
        Assert.True(await running.WriteCopiesAsync());

        // The same session in another editor: the process of the first one runs (it is this one)
        await using var second = await first.ReopenAsync();
        using var other = Start(second);

        Assert.Null(other.FindRecovery());
        Assert.NotEqual(running.ProcessFolder, other.ProcessFolder);
        Assert.True(File.Exists(Path.Combine(running.ProcessFolder!, RecoveryStore.ManifestFileName)));
    }

    private SessionRecoveryService Start(EditorSessionFixture fixture)
    {
        // No timer: the tests write the copies themselves, and a test leaves no timer behind
        var recovery = new SessionRecoveryService(fixture.Session, fixture.PackagePath, root) { TimeProvider = new ManualTimeProvider() };
        recovery.Start();
        return recovery;
    }

    private static AssetViewModel Asset(EditorSessionFixture fixture, string name) => fixture.Session.AllAssets.Single(x => x.Name == name);

    private static void Edit(EditorSessionFixture fixture, AssetViewModel asset, string value)
    {
        var undoRedo = fixture.Session.UndoRedoService;
        using var transaction = undoRedo.CreateTransaction();
        asset.PropertyGraph!.RootNode[nameof(TestEditorAsset.Value)].Update(value);
        undoRedo.SetName(transaction, "Edit " + value);
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
