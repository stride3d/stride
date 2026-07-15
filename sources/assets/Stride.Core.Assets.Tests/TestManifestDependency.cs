// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="ManifestDependency"/> class.
/// </summary>
public class TestManifestDependency
{
    [Fact]
    public void TestIdProperty()
    {
        // Arrange
        var dependency = new ManifestDependency();

        // Act
        dependency.Id = "Stride.Core";

        // Assert
        Assert.Equal("Stride.Core", dependency.Id);
    }

    [Fact]
    public void TestVersionProperty()
    {
        // Arrange
        var dependency = new ManifestDependency();
        var version = new PackageVersionRange(new PackageVersion("4.0.0"), true);

        // Act
        dependency.Version = version;

        // Assert
        Assert.Equal(version, dependency.Version);
    }

    [Fact]
    public void TestPropertiesDefaultToNull()
    {
        // Act
        var dependency = new ManifestDependency();

        // Assert
        Assert.Null(dependency.Id);
        Assert.Null(dependency.Version);
    }
}
