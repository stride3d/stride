// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="PackageFile"/> class.
/// </summary>
public class TestPackageFile
{
    [Fact]
    public void TestConstructorWithPathOnly()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "lib/net8.0/TestAssembly.dll";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert
        Assert.Equal(filePath, packageFile.Path);
        Assert.Null(packageFile.SourcePath);
    }

    [Fact]
    public void TestFullPathCombinesPackagePathAndFilePath()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "lib/net8.0/TestAssembly.dll";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert
        var expectedFullPath = System.IO.Path.Combine(packagePath, filePath);
        Assert.Equal(expectedFullPath, packageFile.FullPath);
    }

    [Fact]
    public void TestPathProperty()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "content/images/logo.png";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert
        Assert.Equal(filePath, packageFile.Path);
    }

    [Fact]
    public void TestSourcePathProperty()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "readme.txt";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert
        Assert.Null(packageFile.SourcePath);
    }

    [Fact]
    public void TestGetStreamThrowsWhenFileDoesNotExist()
    {
        // Arrange
        var packagePath = @"C:\NonExistent";
        var filePath = "nonexistent.txt";
        var packageFile = new PackageFile(packagePath, filePath);

        // Act & Assert
        Assert.Throws<DirectoryNotFoundException>(() => packageFile.GetStream());
    }

    [Fact]
    public void TestGetStreamReturnsStreamForExistingFile()
    {
        // Arrange
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PackageFileTest_" + Guid.NewGuid());
        var filePath = "test.txt";
        var fileContent = "Test content";

        try
        {
            // Create temporary directory and file
            Directory.CreateDirectory(tempDir);
            var fullPath = System.IO.Path.Combine(tempDir, filePath);
            File.WriteAllText(fullPath, fileContent);

            var packageFile = new PackageFile(tempDir, filePath);

            // Act
            using (var stream = packageFile.GetStream())
            using (var reader = new StreamReader(stream))
            {
                var content = reader.ReadToEnd();

                // Assert
                Assert.Equal(fileContent, content);
            }
        }
        finally
        {
            // Cleanup
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TestFullPathWithForwardSlashes()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "lib/netstandard2.0/Stride.Core.dll";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert - Path.Combine should handle forward slashes
        Assert.Contains("Stride.Core.dll", packageFile.FullPath);
        Assert.StartsWith(packagePath, packageFile.FullPath);
    }

    [Fact]
    public void TestMultiplePackageFilesWithSamePackagePath()
    {
        // Arrange
        var packagePath = @"C:\Packages\SharedPackage";
        var file1 = "lib/net8.0/Assembly1.dll";
        var file2 = "lib/net8.0/Assembly2.dll";
        var file3 = "content/data.xml";

        // Act
        var packageFile1 = new PackageFile(packagePath, file1);
        var packageFile2 = new PackageFile(packagePath, file2);
        var packageFile3 = new PackageFile(packagePath, file3);

        // Assert
        Assert.Equal(file1, packageFile1.Path);
        Assert.Equal(file2, packageFile2.Path);
        Assert.Equal(file3, packageFile3.Path);
        Assert.All(new[] { packageFile1, packageFile2, packageFile3 }, 
            pf => Assert.StartsWith(packagePath, pf.FullPath));
    }

    [Fact]
    public void TestWithRelativeFilePath()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "../outside/file.txt";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert
        Assert.Equal(filePath, packageFile.Path);
        var fullPath = packageFile.FullPath;
        Assert.Contains("outside", fullPath);
    }

    [Fact]
    public void TestWithRootedFilePath()
    {
        // Arrange
        var packagePath = @"C:\Packages\TestPackage";
        var filePath = "tools/tool.exe";

        // Act
        var packageFile = new PackageFile(packagePath, filePath);

        // Assert
        Assert.Equal(System.IO.Path.Combine(packagePath, filePath), packageFile.FullPath);
    }
}
