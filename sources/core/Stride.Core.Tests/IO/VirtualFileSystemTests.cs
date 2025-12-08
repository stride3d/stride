using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Tests.IO;

public class VirtualFileSystemTests
{
    [Fact]
    public void DirectorySeparatorChar_IsForwardSlash()
    {
        Assert.Equal('/', VirtualFileSystem.DirectorySeparatorChar);
    }

    [Fact]
    public void AltDirectorySeparatorChar_IsBackslash()
    {
        Assert.Equal('\\', VirtualFileSystem.AltDirectorySeparatorChar);
    }

    [Fact]
    public void GetTempFileName_ReturnsUniquePaths()
    {
        var temp1 = VirtualFileSystem.GetTempFileName();
        var temp2 = VirtualFileSystem.GetTempFileName();

        Assert.NotEqual(temp1, temp2);
        Assert.NotEmpty(temp1);
        Assert.NotEmpty(temp2);
    }

    [Fact]
    public void GetTempFileName_ContainsTmpAndExtension()
    {
        var temp = VirtualFileSystem.GetTempFileName();

        var fileName = Path.GetFileName(temp);
        Assert.Contains("tmp", fileName);
        Assert.EndsWith(".tmp", fileName);
    }

    [Fact]
    public void ApplicationBinary_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.ApplicationBinary);
    }

    [Fact]
    public void ApplicationData_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.ApplicationData);
    }

    [Fact]
    public void ApplicationCache_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.ApplicationCache);
    }

    [Fact]
    public void Drive_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.Drive);
    }

    [Fact]
    public void Combine_CombinesTwoPaths()
    {
        var result = VirtualFileSystem.Combine("/path1", "path2");

        Assert.Equal("/path1/path2", result);
    }

    [Fact]
    public void Combine_HandlesMultipleCalls()
    {
        var result = VirtualFileSystem.Combine(VirtualFileSystem.Combine("/path1", "path2"), "path3");

        Assert.Equal("/path1/path2/path3", result);
    }

    [Fact]
    public void Combine_HandlesTrailingSeparators()
    {
        var result = VirtualFileSystem.Combine("/path1/", "path2");

        Assert.Equal("/path1/path2", result);
    }

    [Fact]
    public void GetParentFolder_ReturnsParentPath()
    {
        var result = VirtualFileSystem.GetParentFolder("/path1/path2/file.txt");

        Assert.Equal("/path1/path2", result);
    }

    [Fact]
    public void GetParentFolder_WithRootPath_ReturnsEmptyOrNull()
    {
        var result = VirtualFileSystem.GetParentFolder("/");

        // Implementation may return either null or empty string
        Assert.True(string.IsNullOrEmpty(result));
    }

    [Fact]
    public void GetFileName_ReturnsFileName()
    {
        var result = VirtualFileSystem.GetFileName("/path1/path2/file.txt");

        Assert.Equal("file.txt", result);
    }

    [Fact]
    public void GetFileName_WithoutExtension_ReturnsFileName()
    {
        var result = VirtualFileSystem.GetFileName("/path1/path2/file");

        Assert.Equal("file", result);
    }

    [Fact]
    public void ApplicationRoaming_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.ApplicationRoaming);
    }

    [Fact]
    public void ApplicationLocal_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.ApplicationLocal);
    }

    [Fact]
    public void ApplicationTemporary_IsNotNull()
    {
        Assert.NotNull(VirtualFileSystem.ApplicationTemporary);
    }

    [Fact]
    public void AllDirectorySeparatorChars_ContainsBothSeparators()
    {
        Assert.Contains('/', VirtualFileSystem.AllDirectorySeparatorChars);
        Assert.Contains('\\', VirtualFileSystem.AllDirectorySeparatorChars);
        Assert.Equal(2, VirtualFileSystem.AllDirectorySeparatorChars.Length);
    }

    [Fact]
    public void ApplicationDatabasePath_HasCorrectValue()
    {
        Assert.Equal("/data/db", VirtualFileSystem.ApplicationDatabasePath);
    }

    [Fact]
    public void LocalDatabasePath_HasCorrectValue()
    {
        Assert.Equal("/local/db", VirtualFileSystem.LocalDatabasePath);
    }

    [Fact]
    public void ApplicationDatabaseIndexName_HasCorrectValue()
    {
        Assert.Equal("index", VirtualFileSystem.ApplicationDatabaseIndexName);
    }

    [Fact]
    public void ApplicationDatabaseIndexPath_HasCorrectValue()
    {
        Assert.Equal("/data/db/index", VirtualFileSystem.ApplicationDatabaseIndexPath);
    }

    [Fact]
    public void Combine_HandlesBackslashSeparator()
    {
        var result = VirtualFileSystem.Combine("/path1", "path2\\subpath");

        Assert.Contains("path1", result);
        Assert.Contains("path2", result);
    }

    [Fact]
    public void Combine_HandlesEmptySecondPath()
    {
        var result = VirtualFileSystem.Combine("/path1", "");

        Assert.Equal("/path1", result);
    }

    [Fact]
    public void GetParentFolder_WithDeepPath_ReturnsCorrectParent()
    {
        var result = VirtualFileSystem.GetParentFolder("/a/b/c/d/e/file.txt");

        Assert.Equal("/a/b/c/d/e", result);
    }

    [Fact]
    public void GetFileName_WithOnlyFileName_ReturnsFileName()
    {
        var result = VirtualFileSystem.GetFileName("file.txt");

        Assert.Equal("file.txt", result);
    }

    [Fact]
    public void FileExists_AndFileDelete_Work()
    {
        var tempPath = VirtualFileSystem.GetTempFileName();
        var testData = new byte[] { 1, 2, 3, 4, 5 };

        // Write data using OpenStream
        using (var stream = VirtualFileSystem.OpenStream(tempPath, VirtualFileMode.Create, VirtualFileAccess.Write))
        {
            stream.Write(testData, 0, testData.Length);
        }

        try
        {
            Assert.True(VirtualFileSystem.FileExists(tempPath));
        }
        finally
        {
            // Cleanup
            VirtualFileSystem.FileDelete(tempPath);
        }
    }

    [Fact]
    public void FileDelete_RemovesFile()
    {
        var tempPath = VirtualFileSystem.GetTempFileName();
        var testData = new byte[] { 1, 2, 3 };

        // Write data using OpenStream
        using (var stream = VirtualFileSystem.OpenStream(tempPath, VirtualFileMode.Create, VirtualFileAccess.Write))
        {
            stream.Write(testData, 0, testData.Length);
        }

        Assert.True(VirtualFileSystem.FileExists(tempPath));

        VirtualFileSystem.FileDelete(tempPath);

        Assert.False(VirtualFileSystem.FileExists(tempPath));
    }

    [Fact]
    public void OpenStream_CanReadWrittenData()
    {
        var tempPath = VirtualFileSystem.GetTempFileName();
        var testData = new byte[] { 10, 20, 30, 40 };

        try
        {
            // Write data
            using (var writeStream = VirtualFileSystem.OpenStream(tempPath, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                writeStream.Write(testData, 0, testData.Length);
            }

            // Read data back
            using (var readStream = VirtualFileSystem.OpenStream(tempPath, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                var readData = new byte[testData.Length];
                readStream.Read(readData, 0, readData.Length);

                Assert.Equal(testData, readData);
            }
        }
        finally
        {
            VirtualFileSystem.FileDelete(tempPath);
        }
    }
}
