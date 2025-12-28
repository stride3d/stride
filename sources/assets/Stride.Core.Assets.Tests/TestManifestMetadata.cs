// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="ManifestMetadata"/> class.
/// </summary>
public class TestManifestMetadata
{
    [Fact]
    public void TestDefaultConstructor()
    {
        // Act
        var metadata = new ManifestMetadata();

        // Assert
        Assert.NotNull(metadata.Dependencies);
        Assert.Empty(metadata.Dependencies);
    }

    [Fact]
    public void TestPropertiesSetAndGet()
    {
        // Arrange
        var metadata = new ManifestMetadata();

        // Act
        metadata.Id = "Stride.Engine";
        metadata.Version = "4.0.0";
        metadata.Title = "Stride Engine";
        metadata.Description = "A powerful game engine";
        metadata.Authors = new[] { "Author1", "Author2" };
        metadata.Owners = new[] { "Owner1" };
        metadata.LicenseUrl = "https://stride3d.net/license";
        metadata.ProjectUrl = "https://stride3d.net";
        metadata.IconUrl = "https://stride3d.net/icon.png";
        metadata.RequireLicenseAcceptance = true;
        metadata.DevelopmentDependency = true;
        metadata.Summary = "Summary text";
        metadata.ReleaseNotes = "Release notes";
        metadata.Copyright = "Copyright 2025";
        metadata.Language = "en-US";
        metadata.Tags = "game engine 3d";
        metadata.MinClientVersionString = "3.0.0";

        // Assert
        Assert.Equal("Stride.Engine", metadata.Id);
        Assert.Equal("4.0.0", metadata.Version);
        Assert.Equal("Stride Engine", metadata.Title);
        Assert.Equal("A powerful game engine", metadata.Description);
        Assert.Equal(new[] { "Author1", "Author2" }, metadata.Authors);
        Assert.Equal(new[] { "Owner1" }, metadata.Owners);
        Assert.Equal("https://stride3d.net/license", metadata.LicenseUrl);
        Assert.Equal("https://stride3d.net", metadata.ProjectUrl);
        Assert.Equal("https://stride3d.net/icon.png", metadata.IconUrl);
        Assert.True(metadata.RequireLicenseAcceptance);
        Assert.True(metadata.DevelopmentDependency);
        Assert.Equal("Summary text", metadata.Summary);
        Assert.Equal("Release notes", metadata.ReleaseNotes);
        Assert.Equal("Copyright 2025", metadata.Copyright);
        Assert.Equal("en-US", metadata.Language);
        Assert.Equal("game engine 3d", metadata.Tags);
        Assert.Equal("3.0.0", metadata.MinClientVersionString);
    }

    [Fact]
    public void TestAddDependency()
    {
        // Arrange
        var metadata = new ManifestMetadata();
        var packageVersion = new PackageVersionRange(new PackageVersion("1.0.0"), true);

        // Act
        metadata.AddDependency("Stride.Core", packageVersion);

        // Assert
        Assert.Single(metadata.Dependencies);
        Assert.Equal("Stride.Core", metadata.Dependencies[0].Id);
        Assert.Equal(packageVersion, metadata.Dependencies[0].Version);
    }

    [Fact]
    public void TestAddMultipleDependencies()
    {
        // Arrange
        var metadata = new ManifestMetadata();
        var version1 = new PackageVersionRange(new PackageVersion("1.0.0"), true);
        var version2 = new PackageVersionRange(new PackageVersion("2.0.0"), true);
        var version3 = new PackageVersionRange(new PackageVersion("3.0.0"), true);

        // Act
        metadata.AddDependency("Stride.Core", version1);
        metadata.AddDependency("Stride.Graphics", version2);
        metadata.AddDependency("Stride.Physics", version3);

        // Assert
        Assert.Equal(3, metadata.Dependencies.Count);
        Assert.Equal("Stride.Core", metadata.Dependencies[0].Id);
        Assert.Equal("Stride.Graphics", metadata.Dependencies[1].Id);
        Assert.Equal("Stride.Physics", metadata.Dependencies[2].Id);
    }

    [Fact]
    public void TestDependenciesCanBeManuallyModified()
    {
        // Arrange
        var metadata = new ManifestMetadata();
        var dependency = new ManifestDependency
        {
            Id = "TestPackage",
            Version = new PackageVersionRange(new PackageVersion("1.0.0"), true)
        };

        // Act
        metadata.Dependencies.Add(dependency);

        // Assert
        Assert.Single(metadata.Dependencies);
        Assert.Equal("TestPackage", metadata.Dependencies[0].Id);
    }
}
