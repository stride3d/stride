// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class DirectoryWatcherTests : IDisposable
{
    private readonly string testDirectory;
    private readonly DirectoryWatcher watcher;

    public DirectoryWatcherTests()
    {
        testDirectory = Path.Combine(Path.GetTempPath(), $"DirectoryWatcherTest_{Guid.NewGuid():N}");
        Directory.CreateDirectory(testDirectory);
        watcher = new DirectoryWatcher();
    }

    public void Dispose()
    {
        watcher.Dispose();
        if (Directory.Exists(testDirectory))
        {
            try
            {
                Directory.Delete(testDirectory, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public void Constructor_DefaultFileFilter_IsSetToAllFiles()
    {
        Assert.Equal("*.*", watcher.FileFilter);
    }

    [Fact]
    public void Constructor_CustomFileFilter_IsSetCorrectly()
    {
        using var customWatcher = new DirectoryWatcher("*.txt");

        Assert.Equal("*.txt", customWatcher.FileFilter);
    }

    [Fact]
    public void Constructor_NullFileFilter_UsesDefaultFilter()
    {
        using var nullFilterWatcher = new DirectoryWatcher(null);

        Assert.Equal("*.*", nullFilterWatcher.FileFilter);
    }

    [Fact]
    public void Track_AddsDirectoryToTrackedList()
    {
        watcher.Track(testDirectory);

        var trackedDirs = watcher.GetTrackedDirectories();
        Assert.Contains(testDirectory, trackedDirs, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void UnTrack_RemovesDirectoryFromTrackedList()
    {
        watcher.Track(testDirectory);
        Assert.Contains(testDirectory, watcher.GetTrackedDirectories(), StringComparer.OrdinalIgnoreCase);

        watcher.UnTrack(testDirectory);

        var trackedDirs = watcher.GetTrackedDirectories();
        Assert.DoesNotContain(testDirectory, trackedDirs, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void GetTrackedDirectories_ReturnsEmptyListInitially()
    {
        using var newWatcher = new DirectoryWatcher();

        var trackedDirs = newWatcher.GetTrackedDirectories();

        Assert.NotNull(trackedDirs);
        Assert.Empty(trackedDirs);
    }

    [Fact]
    public void Track_WithFilePath_TracksParentDirectory()
    {
        var testFile = Path.Combine(testDirectory, "test.txt");
        File.WriteAllText(testFile, "test");

        watcher.Track(testFile);

        var trackedDirs = watcher.GetTrackedDirectories();
        Assert.Contains(testDirectory, trackedDirs, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void Modified_EventIsRaised_OnFileCreation()
    {
        FileEvent? capturedEvent = null;
        watcher.Modified += (sender, e) => capturedEvent = e;
        watcher.Track(testDirectory);

        // Give watcher time to initialize
        Thread.Sleep(500);

        var testFile = Path.Combine(testDirectory, "newfile.txt");
        File.WriteAllText(testFile, "content");

        // Wait for event to be raised
        Thread.Sleep(1000);

        Assert.NotNull(capturedEvent);
    }

    [Fact]
    public void Dispose_CanBeCalledMultipleTimes()
    {
        using var disposableWatcher = new DirectoryWatcher();

        disposableWatcher.Dispose();
        disposableWatcher.Dispose(); // Should not throw
    }

    [Fact]
    public void Track_InvalidPath_DoesNotThrow()
    {
        // Should not throw according to documentation
        watcher.Track("C:\\NonExistentDirectory\\InvalidPath");
    }

    [Fact]
    public void UnTrack_InvalidPath_DoesNotThrow()
    {
        // Should not throw according to documentation
        watcher.UnTrack("C:\\NonExistentDirectory\\InvalidPath");
    }
}

#endif
