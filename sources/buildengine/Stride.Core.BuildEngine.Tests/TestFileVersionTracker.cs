// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Stride.Core.Storage;
using Xunit;

namespace Stride.Core.BuildEngine.Tests;

public class TestFileVersionTracker : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "Stride", nameof(TestFileVersionTracker), Guid.NewGuid().ToString("N"));

    public TestFileVersionTracker()
    {
        Directory.CreateDirectory(directory);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(directory, true);
        }
        catch (IOException)
        {
        }
    }

    [Fact]
    public void MissingFileHashesToEmpty()
    {
        using var tracker = new FileVersionTracker(new MemoryStream());

        Assert.Equal(ObjectId.Empty, tracker.ComputeFileHash(Path.Combine(directory, "missing.txt")));
    }

    [Fact]
    public void FileDeletedWhileItsKeyIsReadDoesNotThrow()
    {
        var path = Path.Combine(directory, "flicker.txt");
        using var stop = new CancellationTokenSource();

        // Creates and deletes the file without pause, so that some keys see it exist, then lose it before its size is read
        var flicker = new Thread(() =>
        {
            while (!stop.IsCancellationRequested)
            {
                try
                {
                    File.WriteAllText(path, "content");
                    File.Delete(path);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        });
        flicker.Start();

        try
        {
            var watch = Stopwatch.StartNew();
            while (watch.Elapsed < TimeSpan.FromSeconds(2))
            {
                var key = new FileVersionKey(path);
                // The date and the size come from the same read: both are set or neither is
                Assert.Equal(key.FileSize == -1, key.LastModifiedDate == DateTime.MinValue);
            }
        }
        finally
        {
            stop.Cancel();
            flicker.Join();
        }
    }

    [Fact]
    public void FailedReadIsNotKept()
    {
        var path = Path.Combine(directory, "locked.txt");
        File.WriteAllText(path, "content");
        using var tracker = new FileVersionTracker(new MemoryStream());

        using (new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
        {
            Assert.Equal(ObjectId.Empty, tracker.ComputeFileHash(path));
        }

        // Same date and size as the failed read: the hash is computed again rather than taken from the failed read
        Assert.NotEqual(ObjectId.Empty, tracker.ComputeFileHash(path));
    }

    [Fact]
    public void CompactDropsMissingFoldersAndMissingReads()
    {
        var kept = Path.Combine(directory, "kept.txt");
        File.WriteAllText(kept, "content");
        var inMissingFolder = Path.Combine(directory, "deleted", "project", "asset.txt");
        var missingInFolder = Path.Combine(directory, "missing.txt");
        var cache = Path.Combine(directory, "cache");
        File.WriteAllText(cache,
            Entry(kept, 1, 7) + Entry(kept, 2, 7) + Entry(inMissingFolder, 1, 7) + Entry(missingInFolder, 1, 7) + Entry(Path.Combine(directory, "failed.txt"), 0, -1));

        Assert.True(FileVersionStorage.Compact(cache));

        // A file missing from a folder that exists stays: only folders are checked
        Assert.Equal([Entry(kept, 2, 7), Entry(missingInFolder, 1, 7)], ReadEntries(cache));
    }

    [Fact]
    public void CompactChecksFoldersOncePerDay()
    {
        var cache = Path.Combine(directory, "cache");
        var inMissingFolder = Path.Combine(directory, "deleted", "asset.txt");
        File.WriteAllText(cache, Entry(inMissingFolder, 1, 7));
        Assert.True(FileVersionStorage.Compact(cache));
        Assert.Empty(ReadEntries(cache));

        File.WriteAllText(cache, Entry(inMissingFolder, 1, 7));
        Assert.True(FileVersionStorage.Compact(cache));
        Assert.Equal([Entry(inMissingFolder, 1, 7)], ReadEntries(cache));

        File.SetLastWriteTimeUtc(cache + ".pruned", DateTime.UtcNow.AddDays(-2));
        Assert.True(FileVersionStorage.Compact(cache));
        Assert.Empty(ReadEntries(cache));
    }

    [Fact]
    public void CompactWithNothingToDropDoesNotRewrite()
    {
        var kept = Path.Combine(directory, "kept.txt");
        File.WriteAllText(kept, "content");
        var cache = Path.Combine(directory, "cache");
        File.WriteAllText(cache, Entry(kept, 1, 7));
        var writeTime = DateTime.UtcNow.AddHours(-1);
        File.SetLastWriteTimeUtc(cache, writeTime);

        Assert.True(FileVersionStorage.Compact(cache));

        Assert.Equal(writeTime, File.GetLastWriteTimeUtc(cache));
    }

    private static string Entry(string path, long ticks, long size) => $"{path}\t{ticks}\t{size}\t0123456789abcdef0123456789abcdef\n";

    private static string[] ReadEntries(string cache) => [.. File.ReadAllLines(cache).Order(StringComparer.Ordinal).Select(x => x + "\n")];
}
