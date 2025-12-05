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
}
