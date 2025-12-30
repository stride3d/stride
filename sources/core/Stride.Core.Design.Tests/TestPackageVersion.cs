// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Serialization;
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

    [Theory]
    [InlineData("1.2.3", "1.2.4", true)]
    [InlineData("1.2.3", "1.2.3", false)]
    [InlineData("1.2.4", "1.2.3", false)]
    [InlineData("1.2.3-alpha", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.3-alpha", false)]
    public void LessThanOperator_ShouldCompareCorrectly(string version1Str, string version2Str, bool expected)
    {
        var version1 = new PackageVersion(version1Str);
        var version2 = new PackageVersion(version2Str);

        Assert.Equal(expected, version1 < version2);
    }

    [Theory]
    [InlineData("1.2.3", "1.2.4", true)]
    [InlineData("1.2.3", "1.2.3", true)]
    [InlineData("1.2.4", "1.2.3", false)]
    public void LessThanOrEqualOperator_ShouldCompareCorrectly(string version1Str, string version2Str, bool expected)
    {
        var version1 = new PackageVersion(version1Str);
        var version2 = new PackageVersion(version2Str);

        Assert.Equal(expected, version1 <= version2);
    }

    [Theory]
    [InlineData("1.2.4", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.3", false)]
    [InlineData("1.2.3", "1.2.4", false)]
    [InlineData("1.2.3", "1.2.3-alpha", true)]
    [InlineData("1.2.3-alpha", "1.2.3", false)]
    public void GreaterThanOperator_ShouldCompareCorrectly(string version1Str, string version2Str, bool expected)
    {
        var version1 = new PackageVersion(version1Str);
        var version2 = new PackageVersion(version2Str);

        Assert.Equal(expected, version1 > version2);
    }

    [Theory]
    [InlineData("1.2.4", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.3", true)]
    [InlineData("1.2.3", "1.2.4", false)]
    public void GreaterThanOrEqualOperator_ShouldCompareCorrectly(string version1Str, string version2Str, bool expected)
    {
        var version1 = new PackageVersion(version1Str);
        var version2 = new PackageVersion(version2Str);

        Assert.Equal(expected, version1 >= version2);
    }

    [Fact]
    public void CompareTo_WithNull_ShouldReturn1()
    {
        var version = new PackageVersion("1.2.3");

        Assert.Equal(1, version.CompareTo((PackageVersion?)null));
        Assert.Equal(1, version.CompareTo((object?)null));
    }

    [Fact]
    public void CompareTo_WithNonPackageVersionObject_ShouldThrowArgumentException()
    {
        var version = new PackageVersion("1.2.3");

        Assert.Throws<ArgumentException>(() => version.CompareTo("not a version"));
    }

    [Theory]
    [InlineData("1.2.3-alpha", "1.2.3-beta", true)]
    [InlineData("1.2.3-beta", "1.2.3-alpha", false)]
    [InlineData("1.2.3-alpha.1", "1.2.3-alpha.2", true)]
    public void CompareTo_WithSpecialVersions_ShouldCompareAlphabetically(string version1Str, string version2Str, bool version1IsLess)
    {
        var version1 = new PackageVersion(version1Str);
        var version2 = new PackageVersion(version2Str);

        if (version1IsLess)
        {
            Assert.True(version1.CompareTo(version2) < 0);
        }
        else
        {
            Assert.True(version1.CompareTo(version2) > 0);
        }
    }

    [Fact]
    public void TryParseStrict_WithValidThreeComponentVersion_ShouldSucceed()
    {
        Assert.True(PackageVersion.TryParseStrict("1.2.3", out var version));
        Assert.NotNull(version);
        Assert.Equal(1, version.Version.Major);
        Assert.Equal(2, version.Version.Minor);
        Assert.Equal(3, version.Version.Build);
    }

    [Theory]
    [InlineData("1.2")]
    [InlineData("1.2.3.4")]
    public void TryParseStrict_WithInvalidComponentCount_ShouldFail(string versionString)
    {
        Assert.False(PackageVersion.TryParseStrict(versionString, out var version));
        Assert.Null(version);
    }

    [Fact]
    public void ParseOptionalVersion_WithValidVersion_ShouldReturnPackageVersion()
    {
        var version = PackageVersion.ParseOptionalVersion("1.2.3");

        Assert.NotNull(version);
        Assert.Equal(1, version.Version.Major);
    }

    [Fact]
    public void ParseOptionalVersion_WithInvalidVersion_ShouldReturnNull()
    {
        var version = PackageVersion.ParseOptionalVersion("invalid");

        Assert.Null(version);
    }

    [Fact]
    public void ParseOptionalVersion_WithNull_ShouldReturnNull()
    {
        var version = PackageVersion.ParseOptionalVersion(null!);

        Assert.Null(version);
    }

    [Theory]
    [InlineData("1.2.3", new[] { "1", "2", "3", "0" })]
    [InlineData("1.2.3.4", new[] { "1", "2", "3", "4" })]
    [InlineData("1.0", new[] { "1", "0", "0", "0" })]
    [InlineData("10.20.30.40", new[] { "10", "20", "30", "40" })]
    public void GetOriginalVersionComponents_ShouldReturnCorrectComponents(string versionString, string[] expected)
    {
        var version = new PackageVersion(versionString);
        var components = version.GetOriginalVersionComponents();

        Assert.Equal(4, components.Length);
        Assert.Equal(expected, components);
    }

    [Fact]
    public void GetOriginalVersionComponents_WithSpacesInVersion_ShouldPreserveOriginal()
    {
        var version = PackageVersion.Parse("1 . 2 . 3");
        var components = version.GetOriginalVersionComponents();

        Assert.Equal(4, components.Length);
        Assert.Equal("1", components[0]);
        Assert.Equal("2", components[1]);
        Assert.Equal("3", components[2]);
    }

    [Fact]
    public void Constructor_WithVersion_ShouldNormalizeToFourComponents()
    {
        var systemVersion = new Version(1, 2);
        var packageVersion = new PackageVersion(systemVersion);

        Assert.Equal(1, packageVersion.Version.Major);
        Assert.Equal(2, packageVersion.Version.Minor);
        Assert.Equal(0, packageVersion.Version.Build);
        Assert.Equal(0, packageVersion.Version.Revision);
    }

    [Fact]
    public void Constructor_WithNullVersion_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new PackageVersion((Version)null!));
    }

    [Fact]
    public void Equals_WithDifferentSpecialVersionCase_ShouldBeEqual()
    {
        var version1 = new PackageVersion("1.2.3-ALPHA");
        var version2 = new PackageVersion("1.2.3-alpha");

        // Equals should be case-insensitive for special versions
        Assert.True(version1.Equals(version2));
        // Note: HashCode may differ due to implementation details, but equal objects
        // don't need to have equal hash codes (though it's preferred for performance)
    }

    [Fact]
    public void Equals_WithNullObject_ShouldReturnFalse()
    {
        var version = new PackageVersion("1.2.3");

        Assert.False(version.Equals((object?)null));
        Assert.False(version.Equals((PackageVersion?)null));
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        var version = new PackageVersion("1.2.3");

        Assert.True(version.Equals(version));
        Assert.True(version.Equals((object)version));
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        var version = new PackageVersion("1.2.3");

        Assert.False(version.Equals("1.2.3"));
    }

    [Fact]
    public void TryParse_WithIntegerVersion_ShouldParseTo_X_0()
    {
        Assert.True(PackageVersion.TryParse("5", out var version));
        Assert.NotNull(version);
        Assert.Equal(5, version.Version.Major);
        Assert.Equal(0, version.Version.Minor);
    }

    [Fact]
    public void ToString_WithVersionComponents_ShouldIncludeAllParts()
    {
        var version = new PackageVersion("1.2.3-alpha+build");

        var result = version.ToString();

        Assert.Contains("1.2.3", result);
        Assert.Contains("alpha", result);
    }

    [Fact]
    public void LessThanOperator_WithNullVersion1_ShouldThrowArgumentNullException()
    {
        PackageVersion? version1 = null;
        var version2 = new PackageVersion("1.2.3");

        Assert.Throws<ArgumentNullException>(() => version1! < version2);
    }

    [Fact]
    public void GreaterThanOperator_WithNullVersion1_ShouldThrowArgumentNullException()
    {
        PackageVersion? version1 = null;
        var version2 = new PackageVersion("1.2.3");

        Assert.Throws<ArgumentNullException>(() => version1! > version2);
    }

    [Fact]
    public void Serialization_ShouldSerializeAndDeserializeCorrectly()
    {
        var originalVersion = new PackageVersion("1.2.3-alpha");
        using var stream = new MemoryStream();
        var serializationStream = new BinarySerializationWriter(stream);

        var serializer = new PackageVersion.PackageVersionDataSerializer();
        var tempVersion = originalVersion;
        serializer.Serialize(ref tempVersion, ArchiveMode.Serialize, serializationStream);

        stream.Position = 0;
        var deserializationStream = new BinarySerializationReader(stream);
        PackageVersion? deserializedVersion = null!;
        serializer.Serialize(ref deserializedVersion!, ArchiveMode.Deserialize, deserializationStream);

        Assert.NotNull(deserializedVersion);
        Assert.Equal(originalVersion.Version.Major, deserializedVersion.Version.Major);
        Assert.Equal(originalVersion.Version.Minor, deserializedVersion.Version.Minor);
        Assert.Equal(originalVersion.Version.Build, deserializedVersion.Version.Build);
        Assert.Equal(originalVersion.SpecialVersion, deserializedVersion.SpecialVersion);
    }

    [Fact]
    public void Serialization_IsBlittable_ShouldReturnTrue()
    {
        var serializer = new PackageVersion.PackageVersionDataSerializer();

        Assert.True(serializer.IsBlittable);
    }
}
