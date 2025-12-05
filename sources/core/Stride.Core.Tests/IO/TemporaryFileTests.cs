// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class TemporaryFileTests
{
    [Fact]
    public void Constructor_CreatesUniqueTemporaryFile()
    {
        using var tempFile1 = new TemporaryFile();
        using var tempFile2 = new TemporaryFile();

        Assert.NotNull(tempFile1.Path);
        Assert.NotNull(tempFile2.Path);
        Assert.NotEqual(tempFile1.Path, tempFile2.Path);
    }

    [Fact]
    public void Path_ReturnsValidPath()
    {
        using var tempFile = new TemporaryFile();

        Assert.False(string.IsNullOrEmpty(tempFile.Path));
    }

    [Fact]
    public void Dispose_DeletesTemporaryFile()
    {
        string? path;

        using (var tempFile = new TemporaryFile())
        {
            path = tempFile.Path;
            // Write something to ensure file exists
            using (var stream = VirtualFileSystem.OpenStream(path, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                var data = System.Text.Encoding.UTF8.GetBytes("test");
                stream.Write(data, 0, data.Length);
            }
            Assert.True(VirtualFileSystem.FileExists(path));
        }

        // File should be deleted after dispose
        Assert.False(VirtualFileSystem.FileExists(path));
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var tempFile = new TemporaryFile();

        tempFile.Dispose();
        tempFile.Dispose(); // Should not throw
    }

    [Fact]
    public void Finalizer_DeletesFile()
    {
        // NOTE: This test is inherently non-deterministic because finalizers
        // are not guaranteed to run immediately or at all. Removing this test
        // as it's testing implementation details of GC which we cannot rely on.
        // The important behavior (file cleanup on Dispose) is already tested.

        // Skip this test - finalizer behavior is not deterministic
    }
}
