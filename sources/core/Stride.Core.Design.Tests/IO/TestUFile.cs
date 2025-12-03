// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Xunit;

namespace Stride.Core.Design.Tests.IO;

/// <summary>
/// Tests for <see cref="UFile"/> class.
/// </summary>
public class TestUFile
{
    [Fact]
    public void Constructor_WithValidPath_CreatesUFile()
    {
        var file = new UFile("/path/to/file.txt");
        Assert.NotNull(file);
        Assert.Equal("/path/to/file.txt", file.FullPath);
    }

    [Fact]
    public void Constructor_WithNull_CreatesEmptyUFile()
    {
        var file = new UFile(null);
        Assert.NotNull(file);
    }

    [Theory]
    [InlineData("/path/to/file.txt", "file.txt")]
    [InlineData("/file.txt", "file.txt")]
    [InlineData("file.txt", "file.txt")]
    [InlineData("/path/to/file", "file")]
    [InlineData("/path/to/.config", ".config")]
    public void GetFileName_ReturnsCorrectFileName(string path, string expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetFileName());
    }

    [Theory]
    [InlineData("/path/to/file.txt", "file")]
    [InlineData("/file.txt", "file")]
    [InlineData("file.txt", "file")]
    [InlineData("/path/to/file", "file")]
    [InlineData("/path/to/.config", null)] // Dot files have no name without extension
    public void GetFileNameWithoutExtension_ReturnsCorrectName(string path, string? expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetFileNameWithoutExtension());
    }

    [Theory]
    [InlineData("/path/to/file.txt", ".txt")]
    [InlineData("/file.log", ".log")]
    [InlineData("document.pdf", ".pdf")]
    [InlineData("/path/to/file", null)]
    [InlineData("/path/to/.config", ".config")] // Dot files: whole name is the extension
    public void GetFileExtension_ReturnsCorrectExtension(string path, string? expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetFileExtension());
    }

    [Theory]
    [InlineData("/path/to/file.txt", "/path/to/file.txt")]
    [InlineData("/file.txt", "/file.txt")]
    [InlineData("file.txt", "file.txt")]
    public void GetDirectoryAndFileName_ReturnsCorrectPath(string path, string expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetDirectoryAndFileName());
    }

    [Theory]
    [InlineData("/path/to/file.txt", "/path/to/file")]
    [InlineData("/file.txt", "/file")]
    [InlineData("file.txt", "file")]
    [InlineData("/path/to/file", "/path/to/file")]
    public void GetDirectoryAndFileNameWithoutExtension_ReturnsCorrectPath(string path, string expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetDirectoryAndFileNameWithoutExtension());
    }

    [Theory]
    [InlineData("/path/to/file.txt", "/path/to/file")]
    [InlineData("/file.txt", "/file")]
    [InlineData("file.txt", "file")]
    [InlineData("/path/to/file", "/path/to/file")]
    public void GetFullPathWithoutExtension_ReturnsCorrectPath(string path, string expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetFullPathWithoutExtension());
    }

    [Fact]
    public void Combine_WithDirectoryAndFile_ReturnsCorrectPath()
    {
        var directory = new UDirectory("/path/to");
        var file = new UFile("file.txt");
        var result = UFile.Combine(directory, file);

        Assert.Equal("/path/to/file.txt", result.FullPath);
    }

    [Fact]
    public void MakeRelative_WithAnchorDirectory_ReturnsRelativePath()
    {
        var file = new UFile("/path/to/file.txt");
        var anchor = new UDirectory("/path");
        var result = file.MakeRelative(anchor);

        Assert.Equal("to/file.txt", result.FullPath);
    }

    [Theory]
    [InlineData("/path/to/file.txt", true)]
    [InlineData("/file.txt", true)]
    [InlineData("file.txt", true)]
    [InlineData("relative/path/file.txt", true)]
    [InlineData("/path/to/directory/", false)] // Ends with slash - not a file
    [InlineData("directory/", false)] // Ends with slash - not a file
    public void IsValid_ValidatesCorrectly(string path, bool expected)
    {
        Assert.Equal(expected, UFile.IsValid(path));
    }

    [Fact]
    public void IsValid_WithNull_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => UFile.IsValid(null!));
    }

    [Fact]
    public void ImplicitConversion_FromString_CreatesUFile()
    {
        UFile file = "/path/to/file.txt";
        Assert.NotNull(file);
        Assert.Equal("/path/to/file.txt", file.FullPath);
    }

    [Fact]
    public void ImplicitConversion_FromNull_ReturnsNull()
    {
        string? nullString = null;
        UFile? file = nullString;
        Assert.Null(file);
    }

    [Theory]
    [InlineData("/path/to/file.txt", "/path/to/other.txt", true)]
    [InlineData("/path/to/file.txt", "/path/to/file.txt", true)]
    [InlineData("/path/to/file.txt", "/other/file.txt", false)]
    public void Equality_ComparesCorrectly(string path1, string path2, bool expectedSameDirectory)
    {
        var file1 = new UFile(path1);
        var file2 = new UFile(path2);

        if (expectedSameDirectory)
        {
            Assert.Equal(file1.GetFullDirectory(), file2.GetFullDirectory());
        }
        else
        {
            Assert.NotEqual(file1.GetFullDirectory(), file2.GetFullDirectory());
        }
    }

    [Theory]
    [InlineData("C:/path/to/file.txt", "file.txt")]
    [InlineData("C:/file.txt", "file.txt")]
    [InlineData("/mnt/data/file.log", "file.log")]
    public void GetFileName_WithDrive_ReturnsCorrectFileName(string path, string expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetFileName());
    }

    [Theory]
    [InlineData("/path/to/file.tar.gz", ".gz")]
    [InlineData("/path/to/archive.tar.bz2", ".bz2")]
    public void GetFileExtension_WithMultipleDots_ReturnsLastExtension(string path, string expected)
    {
        var file = new UFile(path);
        Assert.Equal(expected, file.GetFileExtension());
    }

    [Fact]
    public void UFile_WithWindowsPath_NormalizesCorrectly()
    {
        var file = new UFile("C:\\path\\to\\file.txt");
        Assert.Contains("/", file.FullPath);
        Assert.DoesNotContain("\\", file.FullPath);
    }

    [Fact]
    public void GetDirectory_ReturnsDirectoryPortionOnly()
    {
        var file = new UFile("/a/b/c.txt");
        Assert.Equal("/a/b", file.GetFullDirectory().FullPath);

        file = new UFile("/test.txt");
        Assert.Equal("/", file.GetFullDirectory().FullPath);

        file = new UFile("a.txt");
        Assert.Equal(string.Empty, file.GetFullDirectory().FullPath);
    }

    [Fact]
    public void Normalization_WithComplexPath_NormalizesCorrectly()
    {
        var file = new UFile("/a/b/.././././//c.txt");
        Assert.Equal("/a", file.GetFullDirectory().FullPath);
        Assert.Equal("c", file.GetFileNameWithoutExtension());
        Assert.Equal(".txt", file.GetFileExtension());
        Assert.Equal("/a/c", file.GetDirectoryAndFileNameWithoutExtension());
        Assert.Equal("/a/c.txt", file.FullPath);

        file = new UFile("../.././././//c.txt");
        Assert.Equal("../..", file.GetFullDirectory().FullPath);
        Assert.Equal("c", file.GetFileNameWithoutExtension());
        Assert.Equal(".txt", file.GetFileExtension());
        Assert.Equal("../../c", file.GetDirectoryAndFileNameWithoutExtension());
        Assert.Equal("../../c.txt", file.FullPath);

        file = new UFile("a/../../../c.txt");
        Assert.Equal("../../c.txt", file.FullPath);
    }

    [Fact]
    public void Equals_ComparesPathsCorrectly()
    {
        var file1 = new UFile(null);
        var file2 = new UFile(null);
        Assert.Equal(file1, file2);

        file1 = new UFile("/a/b/c.txt");
        file2 = new UFile("/a/b/d/../c.txt");
        Assert.Equal(file1, file2);

        // Test is not done on Extensions
        file1 = new UFile("/a/b/c.txt");
        file2 = new UFile("/a/b/d/../c.png");
        Assert.NotEqual(file1, file2);
        Assert.Equal(file1.GetDirectoryAndFileNameWithoutExtension(), file2.GetDirectoryAndFileNameWithoutExtension());
    }

    [Fact]
    public void IsAbsolute_ChecksPathCorrectly()
    {
        Assert.False(new UFile("test.txt").IsAbsolute);
        Assert.True(new UFile("/test.txt").IsAbsolute);
        Assert.True(new UFile("C:/test.txt").IsAbsolute);
        Assert.False(new UFile("../test.txt").IsAbsolute);
    }

    [Fact]
    public void MakeRelative_WithComplexPaths_ReturnsCorrectRelativePath()
    {
        var anchor = new UDirectory("/a/b/c");

        // Test direct relative
        var file = new UFile("/a/b/c/test.txt");
        var relative = file.MakeRelative(anchor);
        Assert.Equal("test.txt", relative.FullPath);

        // Test direct relative + subdir
        file = new UFile("/a/b/c/test/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("test/test.txt", relative.FullPath);

        // Test relative 1
        file = new UFile("/a/b/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("../test.txt", relative.FullPath);

        // Test relative 2
        file = new UFile("/a/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("../../test.txt", relative.FullPath);

        // Test relative 3
        file = new UFile("/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("../../../test.txt", relative.FullPath);

        // Test already relative
        file = new UFile("../test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("../test.txt", relative.FullPath);

        // Test only root path in common
        file = new UFile("/e/f/g/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("../../../e/f/g/test.txt", relative.FullPath);
    }

    [Fact]
    public void MakeRelative_WithDrives_HandlesCorrectly()
    {
        var anchor = new UDirectory("C:/a/b/c");

        // Test direct relative
        var file = new UFile("C:/a/b/c/test.txt");
        var relative = file.MakeRelative(anchor);
        Assert.Equal("test.txt", relative.FullPath);

        // Test relative 1
        file = new UFile("C:/a/b/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("../test.txt", relative.FullPath);

        // Test different drive - should return absolute path
        file = new UFile("E:/e/f/g/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("E:/e/f/g/test.txt", relative.FullPath);

        // Test different drive single file
        file = new UFile("E:/test.txt");
        relative = file.MakeRelative(anchor);
        Assert.Equal("E:/test.txt", relative.FullPath);
    }

    [Fact]
    public void MixedSlashes_NormalizesToForwardSlash()
    {
        var file1 = new UFile("/a\\b/c\\d.txt");
        var file2 = new UFile("/a/b/c/d.txt");
        Assert.Equal(file1.ToString(), file2.ToString());
    }

    [Fact]
    public void Combine_WithRelativePath_CombinesCorrectly()
    {
        var path = UPath.Combine("/a/b/c", new UFile("../d/e.txt"));
        Assert.Equal("/a/b/d/e.txt", path.ToString());
    }
}
