// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_DESKTOP

using Stride.Core.IO;

namespace Stride.Core.Tests.IO;

public class TemporaryDirectoryTests
{
    [Fact]
    public void Constructor_CreatesDirectory()
    {
        using var tempDir = new TemporaryDirectory();

        Assert.True(Directory.Exists(tempDir.DirectoryPath));
        Assert.NotNull(tempDir.DirectoryPath);
        Assert.NotEmpty(tempDir.DirectoryPath);
    }

    [Fact]
    public void Constructor_WithPath_CreatesDirectoryAtSpecifiedPath()
    {
        var uniquePath = $"temp_test_{Guid.NewGuid():N}";
        
        using var tempDir = new TemporaryDirectory(uniquePath);

        Assert.True(Directory.Exists(tempDir.DirectoryPath));
        Assert.Contains(uniquePath, tempDir.DirectoryPath);
    }

    [Fact]
    public void Constructor_ThrowsIfDirectoryAlreadyExists()
    {
        var uniquePath = $"temp_test_{Guid.NewGuid():N}";
        Directory.CreateDirectory(uniquePath);

        try
        {
            Assert.Throws<InvalidOperationException>(() => new TemporaryDirectory(uniquePath));
        }
        finally
        {
            Directory.Delete(uniquePath);
        }
    }

    [Fact]
    public void Dispose_DeletesDirectory()
    {
        string? directoryPath;

        using (var tempDir = new TemporaryDirectory())
        {
            directoryPath = tempDir.DirectoryPath;
            Assert.True(Directory.Exists(directoryPath));
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    public void Dispose_DeletesDirectoryWithFiles()
    {
        string? directoryPath;

        using (var tempDir = new TemporaryDirectory())
        {
            directoryPath = tempDir.DirectoryPath;
            var testFile = Path.Combine(directoryPath, "test.txt");
            File.WriteAllText(testFile, "test content");

            Assert.True(File.Exists(testFile));
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    public void Dispose_DeletesDirectoryWithSubdirectories()
    {
        string? directoryPath;

        using (var tempDir = new TemporaryDirectory())
        {
            directoryPath = tempDir.DirectoryPath;
            var subDir = Path.Combine(directoryPath, "subdir");
            Directory.CreateDirectory(subDir);
            var testFile = Path.Combine(subDir, "test.txt");
            File.WriteAllText(testFile, "test content");

            Assert.True(Directory.Exists(subDir));
            Assert.True(File.Exists(testFile));
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    public void DeleteDirectory_HandlesReadOnlyFiles()
    {
        string? directoryPath;

        using (var tempDir = new TemporaryDirectory())
        {
            directoryPath = tempDir.DirectoryPath;
            var testFile = Path.Combine(directoryPath, "readonly.txt");
            File.WriteAllText(testFile, "readonly content");
            File.SetAttributes(testFile, FileAttributes.ReadOnly);

            Assert.True(File.Exists(testFile));
        }

        Assert.False(Directory.Exists(directoryPath));
    }

    [Fact]
    public void DeleteDirectory_DoesNotThrowIfDirectoryMissing()
    {
        var nonExistentPath = $"temp_test_{Guid.NewGuid():N}";

        // Should not throw
        TemporaryDirectory.DeleteDirectory(nonExistentPath);
    }

    [Fact]
    public void DirectoryPath_ReturnsAbsolutePath()
    {
        using var tempDir = new TemporaryDirectory("relative_path");

        Assert.True(Path.IsPathRooted(tempDir.DirectoryPath));
    }

    [Fact]
    public void MultipleTemporaryDirectories_CanExistSimultaneously()
    {
        using var tempDir1 = new TemporaryDirectory();
        using var tempDir2 = new TemporaryDirectory();

        Assert.True(Directory.Exists(tempDir1.DirectoryPath));
        Assert.True(Directory.Exists(tempDir2.DirectoryPath));
        Assert.NotEqual(tempDir1.DirectoryPath, tempDir2.DirectoryPath);
    }
}

#endif
