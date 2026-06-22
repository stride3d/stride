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
    public void TestGetStreamThrowsWhenFileDoesNotExist()
    {
        // Arrange - a path that is guaranteed not to exist on any platform
        var packagePath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "NonExistent_" + Guid.NewGuid());
        var filePath = "nonexistent.txt";
        var packageFile = new PackageFile(packagePath, filePath);

        // Act & Assert - the concrete subtype (FileNotFoundException vs DirectoryNotFoundException)
        // differs across platforms, so assert the shared IOException base.
        Assert.ThrowsAny<IOException>(() => packageFile.GetStream());
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
}
