// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Threading.Tasks.Dataflow;
using Stride.Core.Assets.Tracking;
using Stride.Core.IO;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// The tracker of the source files of the assets, when the watch of their folder failed and started again.
/// </summary>
public sealed class TestAssetSourceTracker : IDisposable
{
    private readonly string directory = Directory.CreateTempSubdirectory("AssetSourceTrackerTest_").FullName;

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
            // Best effort
        }
    }

    [Fact]
    public async Task ASourceFileChangedWhileTheWatchHadFailedIsHashedAgain()
    {
        var source = new UFile(Path.Combine(directory, "source.txt"));
        File.WriteAllText(source.ToOSPath(), "first");
        var asset = new AssetImportObjectTest { Source = source };
        var package = new Package();
        package.Assets.Add(new AssetItem("Imported", asset));
        using var session = new PackageSession(package);
        var tracker = session.SourceTracker;
        var watcher = new LosingDirectoryWatcher();
        tracker.CreateDirectoryWatcher = () => watcher;
        var changes = new BufferBlock<IReadOnlyList<SourceFileChangedData>>();
        using var link = tracker.SourceFileChanged.LinkTo(changes);

        try
        {
            // The tracking computes the hash of the source file when it starts
            tracker.EnableTracking = true;
            await ReceiveSourceFileChangeAsync(changes, asset.Id, () => tracker.GetCurrentHash(source) != default);
            var firstHash = tracker.GetCurrentHash(source);

            // The watcher reports none of the changes
            File.WriteAllText(source.ToOSPath(), "second, longer");
            watcher.RaiseChangesMissed(directory);

            await ReceiveSourceFileChangeAsync(changes, asset.Id, () => tracker.GetCurrentHash(source) != firstHash);
        }
        finally
        {
            tracker.EnableTracking = false;
        }
    }

    private static async Task ReceiveSourceFileChangeAsync(BufferBlock<IReadOnlyList<SourceFileChangedData>> changes, AssetId assetId, Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (true)
        {
            var batch = await changes.ReceiveAsync(timeout.Token);
            if (batch.Any(x => x.Type == SourceFileChangeType.SourceFile && x.AssetId == assetId) && condition())
                return;
        }
    }

    /// <summary>
    /// A directory watcher that loses all its events, as a watch whose events overflow, and reports that it missed changes when told.
    /// </summary>
    private sealed class LosingDirectoryWatcher : DirectoryWatcher
    {
        protected override void OnModified(object sender, FileEvent e)
        {
        }

        public void RaiseChangesMissed(string path) => OnChangesMissed(this, new ChangesMissedEventArgs(path));
    }
}
