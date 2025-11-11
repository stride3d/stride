// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Design.Tests;

/// <summary>
/// Tests for the <see cref="PackageVersion"/> class.
/// </summary>
public class TestPackageVersion
{
    [Theory]
    [InlineData("1.0.0")]
    [InlineData("1.10.100")]
    [InlineData("1.0.0-alpha", "1.0.0", "alpha")]
    [InlineData("1.0.0-beta", "1.0.0", "beta")]
    [InlineData("1.0.0-RC", "1.0.0", "RC")]
    [InlineData("1.0.0-0", "1.0.0", "0")]
    [InlineData("1.0.0-0.1.2", "1.0.0", "0.1.2")]
    [InlineData("1.0.0-alpha.1", "1.0.0", "alpha.1")]
    [InlineData("1.0.0-alpha.1.0.1", "1.0.0", "alpha.1.0.1")]
    [InlineData("1.0.0-alpha+001", "1.0.0", "alpha")]
    [InlineData("1.0.0-rc.1+001", "1.0.0", "rc.1")]
    [InlineData("1.0.0+build001", "1.0.0", "")]
    [InlineData("1.0.0.1", "1.0.0.1", "", true)]
    [InlineData("1.0.0.1-alpha", "1.0.0.1", "alpha", true)]
    [InlineData("1.0.0.1-alpha+build", "1.0.0.1", "alpha", true)]
    [InlineData("1.0.0.1+build", "1.0.0.1", "", true)]
    public void TryParse_WithValidVersionStrings_ShouldParseCorrectly(string versionString, string? expectedVersionNumber = null, string expectedSpecialVersion = "", bool isFourDigitVersion = false)
    {
        expectedVersionNumber ??= versionString;

        Assert.True(PackageVersion.TryParse(versionString, out var pkgVersion), $"Failed to parse '{versionString}'");

        var actualVerStr = pkgVersion.Version.ToString(fieldCount: isFourDigitVersion ? 4 : 3);
        Assert.Equal(expectedVersionNumber, actualVerStr);
        Assert.Equal(expectedSpecialVersion, pkgVersion.SpecialVersion);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("invalid")]
    [InlineData("1.0.0.0.0")]
    [InlineData("a.b.c")]
    public void TryParse_WithInvalidVersionStrings_ShouldReturnFalse(string? versionString)
    {
        Assert.False(PackageVersion.TryParse(versionString!, out var result));
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithValidVersionString_ShouldSucceed()
    {
        var version = PackageVersion.Parse("1.2.3");

        Assert.NotNull(version);
        Assert.Equal(1, version.Version.Major);
        Assert.Equal(2, version.Version.Minor);
        Assert.Equal(3, version.Version.Build);
    }

    [Fact]
    public void Parse_WithInvalidVersionString_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentException>(() => PackageVersion.Parse("invalid"));
    }

    [Fact]
    public void Parse_WithNullOrEmpty_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PackageVersion.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => PackageVersion.Parse(""));
    }

    [Fact]
    public void Constructor_WithVersionComponents_ShouldCreateCorrectVersion()
    {
        var version = new PackageVersion(1, 2, 3, 4);

        Assert.Equal(1, version.Version.Major);
        Assert.Equal(2, version.Version.Minor);
        Assert.Equal(3, version.Version.Build);
        Assert.Equal(4, version.Version.Revision);
        Assert.Empty(version.SpecialVersion);
    }

    [Fact]
    public void Constructor_WithVersionAndSpecialVersion_ShouldCreateCorrectVersion()
    {
        var version = new PackageVersion(1, 2, 3, "alpha");

        Assert.Equal(1, version.Version.Major);
        Assert.Equal(2, version.Version.Minor);
        Assert.Equal(3, version.Version.Build);
        Assert.Equal("alpha", version.SpecialVersion);
    }

    [Fact]
    public void CompareTo_WithEqualVersions_ShouldReturnZero()
    {
        var version1 = new PackageVersion("1.2.3");
        var version2 = new PackageVersion("1.2.3");

        Assert.Equal(0, version1.CompareTo(version2));
    }

    [Fact]
    public void CompareTo_WithDifferentVersions_ShouldReturnCorrectResult()
    {
        var version1 = new PackageVersion("1.2.3");
        var version2 = new PackageVersion("1.2.4");

        Assert.True(version1.CompareTo(version2) < 0);
        Assert.True(version2.CompareTo(version1) > 0);
    }

    [Fact]
    public void Equals_WithSameVersion_ShouldReturnTrue()
    {
        var version1 = new PackageVersion("1.2.3-alpha");
        var version2 = new PackageVersion("1.2.3-alpha");

        Assert.True(version1.Equals(version2));
        Assert.True(version1 == version2);
        Assert.False(version1 != version2);
    }

    [Fact]
    public void Equals_WithDifferentVersion_ShouldReturnFalse()
    {
        var version1 = new PackageVersion("1.2.3");
        var version2 = new PackageVersion("1.2.4");

        Assert.False(version1.Equals(version2));
        Assert.False(version1 == version2);
        Assert.True(version1 != version2);
    }

    [Fact]
    public void GetHashCode_WithSameVersion_ShouldBeEqual()
    {
        var version1 = new PackageVersion("1.2.3-alpha");
        var version2 = new PackageVersion("1.2.3-alpha");

        Assert.Equal(version1.GetHashCode(), version2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnOriginalString()
    {
        var versionString = "1.2.3-alpha";
        var version = new PackageVersion(versionString);

        Assert.Equal(versionString, version.ToString());
    }

    [Fact]
    public void Zero_ShouldBeVersion0()
    {
        Assert.Equal(0, PackageVersion.Zero.Version.Major);
        Assert.Equal(0, PackageVersion.Zero.Version.Minor);
        Assert.Equal(0, PackageVersion.Zero.Version.Build);
    }
}
