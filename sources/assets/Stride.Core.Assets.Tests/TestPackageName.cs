// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Packages;

namespace Stride.Core.Assets.Tests;

/// <summary>
/// Tests for the <see cref="PackageName"/> class.
/// </summary>
public class TestPackageName
{
    [Fact]
    public void TestConstructorWithValidParameters()
    {
        // Arrange
        var id = "Stride.Engine";
        var version = new PackageVersion("4.0.0");

        // Act
        var packageName = new PackageName(id, version);

        // Assert
        Assert.Equal(id, packageName.Id);
        Assert.Equal(version, packageName.Version);
    }

    [Fact]
    public void TestConstructorThrowsOnNullId()
    {
        // Arrange
        var version = new PackageVersion("4.0.0");

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PackageName(null!, version));
    }

    [Fact]
    public void TestConstructorThrowsOnNullVersion()
    {
        // Arrange
        var id = "Stride.Engine";

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new PackageName(id, null!));
    }

    [Fact]
    public void TestEqualsWithSameValues()
    {
        // Arrange
        var id = "Stride.Engine";
        var version = new PackageVersion("4.0.0");
        var packageName1 = new PackageName(id, version);
        var packageName2 = new PackageName(id, version);

        // Act & Assert
        Assert.True(packageName1.Equals(packageName2));
        Assert.True(packageName2.Equals(packageName1));
        Assert.True(packageName1.Equals((object)packageName2));
    }

    [Fact]
    public void TestEqualsWithDifferentId()
    {
        // Arrange
        var version = new PackageVersion("4.0.0");
        var packageName1 = new PackageName("Stride.Engine", version);
        var packageName2 = new PackageName("Stride.Graphics", version);

        // Act & Assert
        Assert.False(packageName1.Equals(packageName2));
        Assert.False(packageName2.Equals(packageName1));
    }

    [Fact]
    public void TestEqualsWithDifferentVersion()
    {
        // Arrange
        var id = "Stride.Engine";
        var packageName1 = new PackageName(id, new PackageVersion("4.0.0"));
        var packageName2 = new PackageName(id, new PackageVersion("4.1.0"));

        // Act & Assert
        Assert.False(packageName1.Equals(packageName2));
        Assert.False(packageName2.Equals(packageName1));
    }

    [Fact]
    public void TestEqualsWithNull()
    {
        // Arrange
        var packageName = new PackageName("Stride.Engine", new PackageVersion("4.0.0"));

        // Act & Assert
        Assert.False(packageName.Equals(null));
        Assert.False(packageName.Equals((object?)null));
    }

    [Fact]
    public void TestEqualsWithSameReference()
    {
        // Arrange
        var packageName = new PackageName("Stride.Engine", new PackageVersion("4.0.0"));

        // Act & Assert
        Assert.True(packageName.Equals(packageName));
        Assert.True(packageName.Equals((object)packageName));
    }

    [Fact]
    public void TestGetHashCode()
    {
        // Arrange
        var id = "Stride.Engine";
        var version = new PackageVersion("4.0.0");
        var packageName1 = new PackageName(id, version);
        var packageName2 = new PackageName(id, version);

        // Act
        var hash1 = packageName1.GetHashCode();
        var hash2 = packageName2.GetHashCode();

        // Assert - Equal objects should have equal hash codes
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TestDifferentObjectsAreNotEqual()
    {
        // Arrange
        var packageName1 = new PackageName("Stride.Engine", new PackageVersion("4.0.0"));
        var packageName2 = new PackageName("Stride.Graphics", new PackageVersion("4.0.0"));
        var packageName3 = new PackageName("Stride.Engine", new PackageVersion("4.1.0"));

        // Assert - inequality is a guaranteed contract (unlike hash-code distinctness,
        // where collisions are legal and would make a NotEqual assertion brittle).
        Assert.NotEqual(packageName1, packageName2);
        Assert.NotEqual(packageName1, packageName3);
    }

    [Fact]
    public void TestEqualsWithDifferentType()
    {
        // Arrange
        var packageName = new PackageName("Stride.Engine", new PackageVersion("4.0.0"));
        var otherObject = "not a PackageName";

        // Act & Assert
        Assert.False(packageName.Equals(otherObject));
    }
}
