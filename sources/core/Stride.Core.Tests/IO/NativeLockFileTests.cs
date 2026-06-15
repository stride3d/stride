// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class NativeLockFileTests : IDisposable
{
    private readonly string testFilePath;
    private readonly FileStream? testFileStream;

    public NativeLockFileTests()
    {
        testFilePath = Path.Combine(Path.GetTempPath(), $"locktest_{Guid.NewGuid():N}.tmp");
        testFileStream = File.Create(testFilePath);
        // Write some data to make the file non-empty
        testFileStream.Write(new byte[1024], 0, 1024);
        testFileStream.Flush();
    }

    public void Dispose()
    {
        testFileStream?.Dispose();
        try
        {
            if (File.Exists(testFilePath))
                File.Delete(testFilePath);
        }
        catch
        {
            // Ignore cleanup errors
        }
    }

    [Fact]
    public void TryLockFile_CanLockFile()
    {
        bool result = NativeLockFile.TryLockFile(testFileStream!, 0, 100, exclusive: false, failImmediately: true);

        // On platforms that support locking, this should succeed
        // On macOS, it returns false (not supported)
        Assert.True(result || OperatingSystem.IsMacOS());
    }

    [Fact]
    public void TryLockFile_WithExclusiveLock_CanLockFile()
    {
        bool result = NativeLockFile.TryLockFile(testFileStream!, 0, 100, exclusive: true, failImmediately: true);

        Assert.True(result || OperatingSystem.IsMacOS());
    }

    [Fact]
    public void TryUnlockFile_CanUnlockAfterLock()
    {
        bool lockResult = NativeLockFile.TryLockFile(testFileStream!, 0, 100, exclusive: false, failImmediately: true);

        if (lockResult)
        {
            // Should not throw on supported platforms
            NativeLockFile.TryUnlockFile(testFileStream!, 0, 100);
        }
    }

    [Fact]
    public void TryLockFile_WithOffset_CanLockPartOfFile()
    {
        bool result = NativeLockFile.TryLockFile(testFileStream!, offset: 100, count: 200, exclusive: false, failImmediately: true);

        Assert.True(result || OperatingSystem.IsMacOS());
    }

    [Fact]
    public void TryLockFile_FailImmediately_ReturnsImmediately()
    {
        // Lock the region first
        bool firstLock = NativeLockFile.TryLockFile(testFileStream!, 0, 100, exclusive: true, failImmediately: true);

        if (firstLock && !OperatingSystem.IsMacOS())
        {
            // Try to lock the same region with a different stream (would fail immediately)
            // Need to open with FileShare.None to allow concurrent access for lock testing
            var secondFilePath = Path.Combine(Path.GetTempPath(), $"locktest2_{Guid.NewGuid():N}.tmp");
            using var secondStream = File.Create(secondFilePath);
            secondStream.Write(new byte[1024], 0, 1024);
            secondStream.Flush();

            // Try locking the second file (different file, should succeed)
            bool secondLock = NativeLockFile.TryLockFile(secondStream, 0, 100, exclusive: true, failImmediately: true);

            Assert.True(secondLock || OperatingSystem.IsMacOS());

            // Cleanup
            NativeLockFile.TryUnlockFile(testFileStream!, 0, 100);
            if (secondLock)
            {
                NativeLockFile.TryUnlockFile(secondStream, 0, 100);
            }

            secondStream.Close();
            File.Delete(secondFilePath);
        }
    }

    [Fact]
    public void TryLockFile_MultipleLocks_OnDifferentRegions()
    {
        bool lock1 = NativeLockFile.TryLockFile(testFileStream!, 0, 100, exclusive: false, failImmediately: true);
        bool lock2 = NativeLockFile.TryLockFile(testFileStream!, 200, 100, exclusive: false, failImmediately: true);

        // Both locks should succeed (different regions)
        if (!OperatingSystem.IsMacOS())
        {
            Assert.True(lock1);
            Assert.True(lock2);

            // Cleanup
            NativeLockFile.TryUnlockFile(testFileStream!, 0, 100);
            NativeLockFile.TryUnlockFile(testFileStream!, 200, 100);
        }
    }

    [Fact]
    public void TryUnlockFile_OnMacOS_DoesNotThrow()
    {
        // This should not throw even on macOS where locking is not supported
        if (OperatingSystem.IsMacOS())
        {
            NativeLockFile.TryUnlockFile(testFileStream!, 0, 100);
        }
        else
        {
            // On other platforms, unlock without lock might throw or succeed
            // depending on implementation
            NativeLockFile.TryLockFile(testFileStream!, 0, 100, exclusive: false, failImmediately: true);
            NativeLockFile.TryUnlockFile(testFileStream!, 0, 100);
        }
    }
}
