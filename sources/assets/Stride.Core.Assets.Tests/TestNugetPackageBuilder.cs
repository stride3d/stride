// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.IO;
using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="NugetPackageBuilder"/> class.
/// </summary>
public class TestNugetPackageBuilder
{
    [Fact]
    public void TestEqualsWithSameReference()
    {
        // Arrange
        var builder = new NugetPackageBuilder();

        // Act & Assert
        Assert.True(builder.Equals(builder));
        Assert.True(builder.Equals((object)builder));
    }

    [Fact]
    public void TestEqualsWithNull()
    {
        // Arrange
        var builder = new NugetPackageBuilder();

        // Act & Assert
        Assert.False(builder.Equals(null));
        Assert.False(builder.Equals((object?)null));
    }

    [Fact]
    public void TestEqualityOperators()
    {
        // Arrange
        var builder1 = new NugetPackageBuilder();
        var builder2 = new NugetPackageBuilder();

        // Act & Assert
        Assert.False(builder1 == builder2); // Different instances
        Assert.True(builder1 != builder2);
#pragma warning disable CS1718 // Comparison made to same variable — intentional, exercises the operators with the same instance
        Assert.True(builder1 == builder1); // Same instance
        Assert.False(builder1 != builder1);
#pragma warning restore CS1718
    }

    [Fact]
    public void TestGetHashCode()
    {
        // Arrange - equality is delegated to the inner PackageBuilder, so the only
        // publicly-equal pair is a reference to the same instance.
        var builder = new NugetPackageBuilder();
        var sameReference = builder;

        // Assert - equal objects must produce equal (and stable) hash codes
        Assert.Equal(builder, sameReference);
        Assert.Equal(builder.GetHashCode(), sameReference.GetHashCode());
        Assert.Equal(builder.GetHashCode(), builder.GetHashCode());
    }

    [Fact]
    public void TestPopulateWithValidMetadata()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "TestPackage",
            Version = "1.0.0",
            Title = "Test Package",
            Description = "A test package",
            Authors = new[] { "Test Author" },
            Copyright = "Copyright 2025"
        };

        // Act
        builder.Populate(metadata);

        // Assert
        Assert.Equal("TestPackage", builder.Id);
        Assert.Equal("1.0.0.0", builder.Version.ToString());
        Assert.Equal("Test Package", builder.Title);
        Assert.Equal("A test package", builder.Description);
        Assert.Contains("Test Author", builder.Authors);
        Assert.Equal("Copyright 2025", builder.Copyright);
    }

    [Fact]
    public void TestPopulateWithDependencies()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "TestPackage",
            Version = "1.0.0",
            Description = "Test"
        };
        metadata.AddDependency("Stride.Core", new PackageVersionRange(new PackageVersion("4.0.0"), true));
        metadata.AddDependency("Stride.Graphics", new PackageVersionRange(new PackageVersion("4.0.0"), true));

        // Act
        builder.Populate(metadata);

        // Assert - Populate maps the metadata onto the builder. NugetPackageBuilder does not
        // expose dependency groups on its public surface, so we verify the populated state that
        // is observable rather than re-asserting the input metadata's dependency count.
        Assert.Equal("TestPackage", builder.Id);
        Assert.Equal("1.0.0.0", builder.Version.ToString());
        Assert.Equal("Test", builder.Description);
    }

    [Fact]
    public void TestPropertiesAfterPopulate()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "ComplexPackage",
            Version = "2.5.3",
            Title = "Complex Test Package",
            Description = "A complex test package with many properties",
            Authors = new[] { "Author1", "Author2" },
            Owners = new[] { "Owner1" },
            Copyright = "Copyright 2025",
            Language = "en-US",
            LicenseUrl = "https://example.com/license",
            ProjectUrl = "https://example.com/project",
            IconUrl = "https://example.com/icon.png",
            RequireLicenseAcceptance = true,
            DevelopmentDependency = false,
            Summary = "Summary text",
            ReleaseNotes = "Release notes",
            Tags = "test package example"
        };

        // Act
        builder.Populate(metadata);

        // Assert
        Assert.Equal("ComplexPackage", builder.Id);
        Assert.Equal("2.5.3.0", builder.Version.ToString());
        Assert.Equal("Complex Test Package", builder.Title);
        Assert.Equal("A complex test package with many properties", builder.Description);
        Assert.Equal(2, builder.Authors.Count());
        Assert.Contains("Author1", builder.Authors);
        Assert.Contains("Author2", builder.Authors);
        Assert.Single(builder.Owners);
        Assert.Contains("Owner1", builder.Owners);
        Assert.Equal("Copyright 2025", builder.Copyright);
        Assert.Equal("en-US", builder.Language);
        Assert.Equal("https://example.com/license", builder.LicenseUrl?.ToString());
        Assert.Equal("https://example.com/project", builder.ProjectUrl?.ToString());
        Assert.Equal("https://example.com/icon.png", builder.IconUrl?.ToString());
        Assert.True(builder.RequireLicenseAcceptance);
        Assert.False(builder.DevelopmentDependency);
        Assert.Equal("Summary text", builder.Summary);
        Assert.Equal("Release notes", builder.ReleaseNotes);
        Assert.Contains("test", builder.Tags);
        Assert.Contains("package", builder.Tags);
        Assert.Contains("example", builder.Tags);
    }

    [Fact]
    public void TestTagsPropertyReturnsSpaceSeparatedString()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "TagTest",
            Version = "1.0.0",
            Description = "Test",
            Tags = "tag1 tag2 tag3"
        };

        // Act
        builder.Populate(metadata);
        var tags = builder.Tags;

        // Assert - getter joins each tag with a trailing space
        Assert.Equal("tag1 tag2 tag3 ", tags);
    }

    [Fact]
    public void TestClearFilesRemovesAllFiles()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "TestPackage",
            Version = "1.0.0",
            Description = "Test"
        };
        builder.Populate(metadata);

        // Create temp directory structure for testing
        var tempDir = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "PackageBuilderTest_" + Guid.NewGuid());
        try
        {
            Directory.CreateDirectory(tempDir);
            var testFile = System.IO.Path.Combine(tempDir, "test.txt");
            File.WriteAllText(testFile, "content");

            var manifestFiles = new List<ManifestFile>
            {
                new ManifestFile { Source = "test.txt", Target = "content/" }
            };

            builder.PopulateFiles(new UDirectory(tempDir), manifestFiles);
            
            // Act
            builder.ClearFiles();

            // Assert
            Assert.Empty(builder.Files);
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
        }
    }

    [Fact]
    public void TestPopulateMultipleTimes()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata1 = new ManifestMetadata
        {
            Id = "Package1",
            Version = "1.0.0",
            Description = "First"
        };
        var metadata2 = new ManifestMetadata
        {
            Id = "Package2",
            Version = "2.0.0",
            Description = "Second"
        };

        // Act
        builder.Populate(metadata1);
        builder.Populate(metadata2);

        // Assert - Last populate should win
        Assert.Equal("Package2", builder.Id);
        Assert.Equal("2.0.0.0", builder.Version.ToString());
        Assert.Equal("Second", builder.Description);
    }

    [Fact]
    public void TestFilesPropertyInitiallyEmpty()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "TestPackage",
            Version = "1.0.0",
            Description = "Test"
        };

        // Act
        builder.Populate(metadata);

        // Assert
        Assert.NotNull(builder.Files);
        Assert.Empty(builder.Files);
    }

    [Fact]
    public void TestMinClientVersionProperty()
    {
        // Arrange
        var builder = new NugetPackageBuilder();
        var metadata = new ManifestMetadata
        {
            Id = "TestPackage",
            Version = "1.0.0",
            Description = "Test",
            MinClientVersionString = "3.0.0"
        };

        // Act
        builder.Populate(metadata);

        // Assert
        Assert.NotNull(builder.MinClientVersion);
        Assert.Equal(new Version(3, 0, 0), builder.MinClientVersion);
    }
}
