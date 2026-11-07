// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
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
}
