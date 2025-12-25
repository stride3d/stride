// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Design.Tests.IO;

/// <summary>
/// Tests for <see cref="UDirectory"/> class.
/// </summary>
public class TestUDirectory
{
    [Fact]
    public void Constructor_WithValidPath_CreatesUDirectory()
    {
        var dir = new UDirectory("/path/to/directory");
        Assert.NotNull(dir);
        Assert.Equal("/path/to/directory", dir.FullPath);
    }

    [Fact]
    public void Constructor_WithNull_CreatesEmptyUDirectory()
    {
        var dir = new UDirectory(null);
        Assert.NotNull(dir);
    }

    [Fact]
    public void Empty_ReturnsEmptyDirectory()
    {
        Assert.NotNull(UDirectory.Empty);
        Assert.Equal(string.Empty, UDirectory.Empty.FullPath);
    }

    [Fact]
    public void This_ReturnsCurrentDirectory()
    {
        Assert.NotNull(UDirectory.This);
        Assert.Equal(".", UDirectory.This.FullPath);
    }

    [Theory]
    [InlineData("/path/to/directory", "directory")]
    [InlineData("/path/to/subdir", "subdir")]
    [InlineData("/root", "root")]
    [InlineData("folder", "")] // Relative path without separator - no directory name
    [InlineData("/", "")]
    public void GetDirectoryName_ReturnsCorrectName(string path, string expected)
    {
        var dir = new UDirectory(path);
        Assert.Equal(expected, dir.GetDirectoryName());
    }

    [Fact]
    public void Combine_WithTwoDirectories_ReturnsCorrectPath()
    {
        var left = new UDirectory("/path/to");
        var right = new UDirectory("subdirectory");
        var result = UDirectory.Combine(left, right);

        Assert.Equal("/path/to/subdirectory", result.FullPath);
    }

    [Fact]
    public void MakeRelative_WithAnchorDirectory_ReturnsRelativePath()
    {
        var dir = new UDirectory("/path/to/subdirectory");
        var anchor = new UDirectory("/path");
        var result = dir.MakeRelative(anchor);

        Assert.Equal("to/subdirectory", result.FullPath);
    }

    [Theory]
    [InlineData("/path/to/directory", "/path/to/directory/file.txt", true)]
    [InlineData("/path/to", "/path/to/directory/file.txt", true)]
    [InlineData("/path/to", "/path/to/file.txt", true)]
    [InlineData("/path/to", "/other/path/file.txt", false)]
    [InlineData("/path/to", "/path/tofile.txt", false)] // Must have separator after
    [InlineData("/path", "/path", false)] // Same path, not contained
    public void Contains_ChecksIfPathIsContained(string dirPath, string testPath, bool expected)
    {
        var dir = new UDirectory(dirPath);
        var path = new UFile(testPath);

        Assert.Equal(expected, dir.Contains(path));
    }

    [Fact]
    public void Contains_WithNull_ThrowsArgumentNullException()
    {
        var dir = new UDirectory("/path/to");
        Assert.Throws<ArgumentNullException>(() => dir.Contains(null!));
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesUDirectory()
    {
        UDirectory dir = "/path/to/directory";
        Assert.NotNull(dir);
        Assert.Equal("/path/to/directory", dir.FullPath);
    }

    [Fact]
    public void ImplicitConversion_FromNull_ReturnsNull()
    {
        string? nullString = null;
        UDirectory? dir = nullString;
        Assert.Null(dir);
    }

    [Theory]
    [InlineData("/path/to/directory/", "/path/to/directory")]
    [InlineData("/path/to/directory//", "/path/to/directory")]
    public void Constructor_WithTrailingSlash_NormalizesPath(string input, string expected)
    {
        var dir = new UDirectory(input);
        Assert.Equal(expected, dir.FullPath);
    }

    [Fact]
    public void UDirectory_WithWindowsPath_NormalizesCorrectly()
    {
        var dir = new UDirectory("C:\\path\\to\\directory");
        Assert.Contains("/", dir.FullPath);
        Assert.DoesNotContain("\\", dir.FullPath);
    }

    [Theory]
    [InlineData("/path/to", "/path/to/subdir", true)]
    [InlineData("/path/to", "/path/another", false)]
    [InlineData("/", "/path", false)] // Root doesn't contain direct child without separator check
    public void Contains_WithDirectory_ChecksCorrectly(string dirPath, string containedPath, bool expected)
    {
        var dir = new UDirectory(dirPath);
        var contained = new UDirectory(containedPath);

        Assert.Equal(expected, dir.Contains(contained));
    }

    [Fact]
    public void GetFullDirectory_OnUDirectory_ReturnsParent()
    {
        var dir = new UDirectory("/path/to/directory");
        var parent = dir.GetFullDirectory();

        Assert.Equal("/path/to/directory", parent.FullPath); // GetFullDirectory returns the directory itself, not parent
    }

    [Theory]
    [InlineData("C:/path/to/directory", "directory")]
    [InlineData("/mnt/data/folder", "folder")]
    public void GetDirectoryName_WithDrive_ReturnsCorrectName(string path, string expected)
    {
        var dir = new UDirectory(path);
        Assert.Equal(expected, dir.GetDirectoryName());
    }

    [Fact]
    public void Combine_WithAbsoluteRight_ReturnsRight()
    {
        var left = new UDirectory("/path/to");
        var right = new UDirectory("/absolute/path");
        var result = UDirectory.Combine(left, right);

        Assert.Equal("/absolute/path", result.FullPath);
    }

    [Fact]
    public void Combine_WithRelativeRight_CombinesPaths()
    {
        var left = new UDirectory("/path/to");
        var right = new UDirectory("relative/path");
        var result = UDirectory.Combine(left, right);

        Assert.Equal("/path/to/relative/path", result.FullPath);
    }

    [Fact]
    public void MakeRelative_WithSameDirectory_ReturnsDot()
    {
        var dir1 = new UDirectory("/a/b/c");
        var dir2 = new UDirectory("/a/b/c");
        var relative = dir1.MakeRelative(dir2);

        Assert.Equal(".", relative.FullPath);
    }

    [Fact]
    public void MakeRelative_WithParentDirectory_ReturnsDirectoryName()
    {
        var dir = new UDirectory("/a/b/c");
        var parent = new UDirectory("/a/b");
        var relative = dir.MakeRelative(parent);

        Assert.Equal("c", relative.FullPath);
    }

    [Fact]
    public void MakeRelative_WithChildDirectory_ReturnsParentReference()
    {
        var dir = new UDirectory("/a/b/c");
        var child = new UDirectory("/a/b/c/d");
        var relative = dir.MakeRelative(child);

        Assert.Equal("..", relative.FullPath);
    }

    [Fact]
    public void Contains_WithDrives_ChecksCorrectly()
    {
        var dir = new UDirectory("C:/a/b/c");
        Assert.True(dir.Contains(new UFile("C:/a/b/c/d")));
        Assert.True(dir.Contains(new UFile("C:/a/b/c/d/e")));
        Assert.True(dir.Contains(new UDirectory("C:/a/b/c/d")));
        Assert.True(dir.Contains(new UDirectory("C:/a/b/c/d/e")));
        Assert.False(dir.Contains(new UFile("C:/a/b/x")));
        Assert.False(dir.Contains(new UFile("C:/a/b/cx"))); // Must have separator after
    }

    [Theory]
    [InlineData("C:/a/b/c", "c")]
    [InlineData("C:/a/b/", "b")]
    [InlineData("C:/", "")]
    [InlineData("/a", "a")]
    [InlineData("/", "")]
    [InlineData("//a//", "a")]
    public void GetDirectoryName_WithVariousPaths_ReturnsCorrectName(string path, string expected)
    {
        Assert.Equal(expected, new UDirectory(path).GetDirectoryName());
    }

    [Fact]
    public void GetParent_ReturnsParentDirectory()
    {
        var dir = new UDirectory("c:/a/b");
        Assert.Equal("c:/a", dir.GetParent().FullPath);

        dir = new UDirectory("/a/b");
        Assert.Equal("/a", dir.GetParent().FullPath);

        dir = new UDirectory("a/b");
        Assert.Equal("a", dir.GetParent().FullPath);

        dir = new UDirectory("c:/");
        Assert.Equal("c:/", dir.GetParent().FullPath); // Root returns itself

        dir = new UDirectory("/");
        Assert.Equal("/", dir.GetParent().FullPath); // Root returns itself

        dir = new UDirectory("a");
        Assert.Equal("", dir.GetParent().FullPath); // No parent
    }

    [Fact]
    public void IsAbsolute_ChecksPathCorrectly()
    {
        Assert.True(new UDirectory("/a/b/c").IsAbsolute);
        Assert.True(new UDirectory("C:/a/b").IsAbsolute);
        Assert.False(new UDirectory("a/b/c").IsAbsolute);
        Assert.False(new UDirectory("../a").IsAbsolute);
    }

    [Fact]
    public void Equals_ComparesPathsCorrectly()
    {
        var dir1 = new UDirectory("/a/b/c");
        var dir2 = new UDirectory("/a/b/d/../c");
        Assert.Equal(dir1, dir2); // Normalized paths should be equal

        dir1 = new UDirectory(null);
        dir2 = new UDirectory(null);
        Assert.Equal(dir1, dir2);
    }
}
