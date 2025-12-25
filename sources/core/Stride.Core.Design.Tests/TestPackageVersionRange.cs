// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Design.Tests;

/// <summary>
/// Tests for the <see cref="PackageVersionRange"/> class.
/// </summary>
public class TestPackageVersionRange
{
    [Fact]
    public void Constructor_Default_ShouldCreateEmptyRange()
    {
        var range = new PackageVersionRange();

        Assert.Null(range.MinVersion);
        Assert.Null(range.MaxVersion);
        Assert.False(range.IsMinInclusive);
        Assert.False(range.IsMaxInclusive);
    }

    [Fact]
    public void Constructor_WithSingleVersion_ShouldCreateExactVersionRange()
    {
        var version = new PackageVersion("1.2.3");
        var range = new PackageVersionRange(version);

        Assert.Equal(version, range.MinVersion);
        Assert.Equal(version, range.MaxVersion);
        Assert.True(range.IsMinInclusive);
        Assert.True(range.IsMaxInclusive);
    }

    [Fact]
    public void Constructor_WithMinVersionOnly_ShouldSetMinVersionCorrectly()
    {
        var version = new PackageVersion("1.2.3");
        var range = new PackageVersionRange(version, true);

        Assert.Equal(version, range.MinVersion);
        Assert.Null(range.MaxVersion);
        Assert.True(range.IsMinInclusive);
        Assert.False(range.IsMaxInclusive);
    }

    [Fact]
    public void Constructor_WithMinVersionExclusive_ShouldSetCorrectly()
    {
        var version = new PackageVersion("1.2.3");
        var range = new PackageVersionRange(version, false);

        Assert.Equal(version, range.MinVersion);
        Assert.Null(range.MaxVersion);
        Assert.False(range.IsMinInclusive);
        Assert.False(range.IsMaxInclusive);
    }

    [Fact]
    public void Constructor_WithBothVersions_ShouldSetAllPropertiesCorrectly()
    {
        var minVersion = new PackageVersion("1.0.0");
        var maxVersion = new PackageVersion("2.0.0");
        var range = new PackageVersionRange(minVersion, true, maxVersion, false);

        Assert.Equal(minVersion, range.MinVersion);
        Assert.Equal(maxVersion, range.MaxVersion);
        Assert.True(range.IsMinInclusive);
        Assert.False(range.IsMaxInclusive);
    }

    [Theory]
    [InlineData("[1.0.0]", "1.0.0", "1.0.0", true, true)]
    [InlineData("(1.0.0,2.0.0)", "1.0.0", "2.0.0", false, false)]
    [InlineData("[1.0.0,2.0.0]", "1.0.0", "2.0.0", true, true)]
    [InlineData("(1.0.0,2.0.0]", "1.0.0", "2.0.0", false, true)]
    [InlineData("[1.0.0,2.0.0)", "1.0.0", "2.0.0", true, false)]
    [InlineData("(,2.0.0)", null, "2.0.0", false, false)]
    [InlineData("(,2.0.0]", null, "2.0.0", false, true)]
    [InlineData("(1.0.0,)", "1.0.0", null, false, false)]
    [InlineData("[1.0.0,)", "1.0.0", null, true, false)]
    public void TryParse_WithValidRangeStrings_ShouldParseCorrectly(
        string rangeString,
        string? expectedMinVersion,
        string? expectedMaxVersion,
        bool expectedMinInclusive,
        bool expectedMaxInclusive)
    {
        Assert.True(PackageVersionRange.TryParse(rangeString, out var range));
        Assert.NotNull(range);

        if (expectedMinVersion != null)
        {
            Assert.NotNull(range.MinVersion);
            Assert.Equal(expectedMinVersion, range.MinVersion.ToString());
        }
        else
        {
            Assert.Null(range.MinVersion);
        }

        if (expectedMaxVersion != null)
        {
            Assert.NotNull(range.MaxVersion);
            Assert.Equal(expectedMaxVersion, range.MaxVersion.ToString());
        }
        else
        {
            Assert.Null(range.MaxVersion);
        }

        Assert.Equal(expectedMinInclusive, range.IsMinInclusive);
        Assert.Equal(expectedMaxInclusive, range.IsMaxInclusive);
    }

    [Theory]
    [InlineData("")]
    [InlineData("invalid")]
    [InlineData("(,)")]
    [InlineData("[,]")]
    [InlineData("[1.0.0")]
    [InlineData("1.0.0]")]
    [InlineData("[1.0.0,2.0.0,3.0.0]")]
    public void TryParse_WithInvalidRangeStrings_ShouldReturnFalse(string? rangeString)
    {
        Assert.False(PackageVersionRange.TryParse(rangeString!, out var range));
        Assert.Null(range);
    }

    [Fact]
    public void TryParse_WithNull_ShouldThrowArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => PackageVersionRange.TryParse(null!, out _));
    }

    [Fact]
    public void TryParse_ShouldParseValidRangeString()
    {
        var success = PackageVersionRange.TryParse("[1.0.0,2.0.0)", out var range);

        Assert.True(success);
        Assert.NotNull(range);
        Assert.NotNull(range.MinVersion);
        Assert.NotNull(range.MaxVersion);
        Assert.Equal("1.0.0", range.MinVersion.ToString());
        Assert.Equal("2.0.0", range.MaxVersion.ToString());
        Assert.True(range.IsMinInclusive);
        Assert.False(range.IsMaxInclusive);
    }

    [Fact]
    public void TryParse_WithInvalidRangeString_ShouldThrowArgumentException()
    {
        Assert.Throws<ArgumentNullException>(() => PackageVersionRange.TryParse(null!, out _));
    }

    [Fact]
    public void Contains_WithVersionInRange_ShouldReturnTrue()
    {
        PackageVersionRange.TryParse("[1.0.0,2.0.0]", out var range);
        var version = new PackageVersion("1.5.0");

        Assert.True(range!.Contains(version));
    }

    [Fact]
    public void Contains_WithVersionOutOfRange_ShouldReturnFalse()
    {
        PackageVersionRange.TryParse("[1.0.0,2.0.0]", out var range);
        var versionTooLow = new PackageVersion("0.9.0");
        var versionTooHigh = new PackageVersion("2.1.0");

        Assert.False(range!.Contains(versionTooLow));
        Assert.False(range.Contains(versionTooHigh));
    }

    [Fact]
    public void Contains_WithExactVersionAndInclusiveBounds_ShouldReturnTrue()
    {
        PackageVersionRange.TryParse("[1.0.0,2.0.0]", out var range);
        var minVersion = new PackageVersion("1.0.0");
        var maxVersion = new PackageVersion("2.0.0");

        Assert.True(range!.Contains(minVersion));
        Assert.True(range.Contains(maxVersion));
    }

    [Fact]
    public void Contains_WithExactVersionAndExclusiveBounds_ShouldReturnFalse()
    {
        PackageVersionRange.TryParse("(1.0.0,2.0.0)", out var range);
        var minVersion = new PackageVersion("1.0.0");
        var maxVersion = new PackageVersion("2.0.0");

        Assert.False(range!.Contains(minVersion));
        Assert.False(range.Contains(maxVersion));
    }

    [Fact]
    public void Equals_WithSameRange_ShouldReturnTrue()
    {
        var range1 = new PackageVersionRange(new PackageVersion("1.0.0"), true, new PackageVersion("2.0.0"), false);
        var range2 = new PackageVersionRange(new PackageVersion("1.0.0"), true, new PackageVersion("2.0.0"), false);

        Assert.True(range1.Equals(range2));
    }

    [Fact]
    public void Equals_WithDifferentRange_ShouldReturnFalse()
    {
        var range1 = new PackageVersionRange(new PackageVersion("1.0.0"), true, new PackageVersion("2.0.0"), false);
        var range2 = new PackageVersionRange(new PackageVersion("1.0.0"), false, new PackageVersion("2.0.0"), false);

        Assert.False(range1.Equals(range2));
    }

    [Fact]
    public void GetHashCode_WithSameRange_ShouldBeEqual()
    {
        var range1 = new PackageVersionRange(new PackageVersion("1.0.0"), true, new PackageVersion("2.0.0"), false);
        var range2 = new PackageVersionRange(new PackageVersion("1.0.0"), true, new PackageVersion("2.0.0"), false);

        Assert.Equal(range1.GetHashCode(), range2.GetHashCode());
    }

    [Fact]
    public void ToString_ShouldReturnFormattedString()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true, new PackageVersion("2.0.0"), false);
        var result = range.ToString();

        Assert.NotNull(result);
        Assert.Contains("1.0.0", result);
        Assert.Contains("2.0.0", result);
    }

    [Fact]
    public void ToString_WithMinVersionOnly_ShouldReturnMinVersion()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);
        var result = range.ToString();

        Assert.Equal("1.0.0", result);
    }

    [Fact]
    public void ToString_WithExactVersion_ShouldReturnBracketedVersion()
    {
        var version = new PackageVersion("1.2.3");
        var range = new PackageVersionRange(version);
        var result = range.ToString();

        Assert.Equal("[1.2.3]", result);
    }

    [Theory]
    [InlineData("[1.0.0,2.0.0]", "[1.0.0, 2.0.0]")]
    [InlineData("(1.0.0,2.0.0)", "(1.0.0, 2.0.0)")]
    [InlineData("[1.0.0,2.0.0)", "[1.0.0, 2.0.0)")]
    [InlineData("(1.0.0,2.0.0]", "(1.0.0, 2.0.0]")]
    public void ToString_WithVariousBrackets_ShouldFormatCorrectly(string input, string expected)
    {
        PackageVersionRange.TryParse(input, out var range);
        var result = range!.ToString();

        Assert.Equal(expected, result);
    }

    [Fact]
    public void ToPrettyPrint_WithMinVersionOnly_ShouldReturnGreaterThanOrEqual()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);
        var result = range.ToPrettyPrint();

        Assert.Equal("(>= 1.0.0)", result);
    }

    [Fact]
    public void ToPrettyPrint_WithExactVersion_ShouldReturnEquals()
    {
        var version = new PackageVersion("1.2.3");
        var range = new PackageVersionRange(version);
        var result = range.ToPrettyPrint();

        Assert.Equal("(= 1.2.3)", result);
    }

    [Fact]
    public void ToPrettyPrint_WithInclusiveRange_ShouldUseLessThanOrEqualAndGreaterThanOrEqual()
    {
        var range = new PackageVersionRange(
            new PackageVersion("1.0.0"), true,
            new PackageVersion("2.0.0"), true);
        var result = range.ToPrettyPrint();

        Assert.Contains(">=", result);
        Assert.Contains("<=", result);
        Assert.Contains("1.0.0", result);
        Assert.Contains("2.0.0", result);
    }

    [Fact]
    public void ToPrettyPrint_WithExclusiveRange_ShouldUseLessThanAndGreaterThan()
    {
        var range = new PackageVersionRange(
            new PackageVersion("1.0.0"), false,
            new PackageVersion("2.0.0"), false);
        var result = range.ToPrettyPrint();

        Assert.Contains(">", result);
        Assert.Contains("<", result);
        Assert.Contains("1.0.0", result);
        Assert.Contains("2.0.0", result);
    }

    [Fact]
    public void ToPrettyPrint_WithOnlyMaxVersion_ShouldShowOnlyMaxConstraint()
    {
        var range = new PackageVersionRange(null, false, new PackageVersion("2.0.0"), true);
        var result = range.ToPrettyPrint();

        Assert.Contains("<=", result);
        Assert.Contains("2.0.0", result);
        Assert.DoesNotContain("1.0.0", result);
    }

    [Fact]
    public void ToPrettyPrint_WithMinExclusiveOnly_ShouldUseGreaterThan()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), false);
        var result = range.ToPrettyPrint();

        Assert.Contains(">", result);
        Assert.DoesNotContain("=", result);
        Assert.Contains("1.0.0", result);
    }

    [Fact]
    public void GetSafeRange_ShouldCreateRangeWithIncrementedMinorVersion()
    {
        var version = new PackageVersion("1.5.3");
        var safeRange = PackageVersionRange.GetSafeRange(version);

        Assert.NotNull(safeRange);
        Assert.NotNull(safeRange.MinVersion);
        Assert.NotNull(safeRange.MaxVersion);
        Assert.Equal("1.5.3", safeRange.MinVersion.ToString());
        Assert.Equal(1, safeRange.MaxVersion.Version.Major);
        Assert.Equal(6, safeRange.MaxVersion.Version.Minor);
        Assert.True(safeRange.IsMinInclusive);
        Assert.False(safeRange.IsMaxInclusive);
    }

    [Fact]
    public void ToFilter_ShouldCreateFilterFunction()
    {
        var range = new PackageVersionRange(
            new PackageVersion("1.0.0"), true,
            new PackageVersion("2.0.0"), false);

        var filter = range.ToFilter<TestPackage>(p => p.Version);

        Assert.True(filter(new TestPackage { Version = new PackageVersion("1.5.0") }));
        Assert.True(filter(new TestPackage { Version = new PackageVersion("1.0.0") }));
        Assert.False(filter(new TestPackage { Version = new PackageVersion("2.0.0") }));
        Assert.False(filter(new TestPackage { Version = new PackageVersion("0.9.0") }));
    }

    [Fact]
    public void ToFilter_WithNullExtractor_ShouldThrowArgumentNullException()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);

        Assert.Throws<ArgumentNullException>(() => range.ToFilter<TestPackage>(null!));
    }

    [Fact]
    public void ToFilter_WithOnlyMinVersion_ShouldFilterCorrectly()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);
        var filter = range.ToFilter<TestPackage>(p => p.Version);

        Assert.True(filter(new TestPackage { Version = new PackageVersion("1.0.0") }));
        Assert.True(filter(new TestPackage { Version = new PackageVersion("2.0.0") }));
        Assert.False(filter(new TestPackage { Version = new PackageVersion("0.9.0") }));
    }

    [Fact]
    public void ToFilter_WithOnlyMaxVersion_ShouldFilterCorrectly()
    {
        var range = new PackageVersionRange(null, false, new PackageVersion("2.0.0"), true);
        var filter = range.ToFilter<TestPackage>(p => p.Version);

        Assert.True(filter(new TestPackage { Version = new PackageVersion("1.0.0") }));
        Assert.True(filter(new TestPackage { Version = new PackageVersion("2.0.0") }));
        Assert.False(filter(new TestPackage { Version = new PackageVersion("2.1.0") }));
    }

    [Fact]
    public void TryParse_WithPlainVersion_ShouldCreateMinInclusiveRange()
    {
        Assert.True(PackageVersionRange.TryParse("1.2.3", out var range));

        Assert.NotNull(range);
        Assert.NotNull(range.MinVersion);
        Assert.Null(range.MaxVersion);
        Assert.True(range.IsMinInclusive);
        Assert.False(range.IsMaxInclusive);
        Assert.Equal("1.2.3", range.MinVersion.ToString());
    }

    [Fact]
    public void TryParse_WithWhitespace_ShouldTrimAndParse()
    {
        Assert.True(PackageVersionRange.TryParse("  [1.0.0,2.0.0]  ", out var range));

        Assert.NotNull(range);
        Assert.NotNull(range.MinVersion);
        Assert.NotNull(range.MaxVersion);
    }

    [Theory]
    [InlineData("(1.0.0,)")]
    [InlineData("[1.0.0,)")]
    public void TryParse_WithOpenEndedMax_ShouldParseCorrectly(string rangeString)
    {
        Assert.True(PackageVersionRange.TryParse(rangeString, out var range));

        Assert.NotNull(range);
        Assert.NotNull(range.MinVersion);
        Assert.Null(range.MaxVersion);
    }

    [Fact]
    public void Equals_WithSameReference_ShouldReturnTrue()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);

        Assert.True(range.Equals(range));
        Assert.True(range.Equals((object)range));
    }

    [Fact]
    public void Equals_WithNull_ShouldReturnFalse()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);

        Assert.False(range.Equals((PackageVersionRange?)null));
        Assert.False(range.Equals((object?)null));
    }

    [Fact]
    public void Equals_WithDifferentType_ShouldReturnFalse()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);

        Assert.False(range.Equals("not a range"));
    }

    [Fact]
    public void Contains_WithNoMinOrMax_ShouldAlwaysReturnTrue()
    {
        var range = new PackageVersionRange();

        Assert.True(range.Contains(new PackageVersion("0.0.1")));
        Assert.True(range.Contains(new PackageVersion("999.999.999")));
    }

    [Fact]
    public void Contains_WithMinInclusiveAndVersionEqual_ShouldReturnTrue()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), true);

        Assert.True(range.Contains(new PackageVersion("1.0.0")));
    }

    [Fact]
    public void Contains_WithMinExclusiveAndVersionEqual_ShouldReturnFalse()
    {
        var range = new PackageVersionRange(new PackageVersion("1.0.0"), false);

        Assert.False(range.Contains(new PackageVersion("1.0.0")));
    }

    [Fact]
    public void Contains_WithMaxInclusiveAndVersionEqual_ShouldReturnTrue()
    {
        var range = new PackageVersionRange(null, false, new PackageVersion("2.0.0"), true);

        Assert.True(range.Contains(new PackageVersion("2.0.0")));
    }

    [Fact]
    public void Contains_WithMaxExclusiveAndVersionEqual_ShouldReturnFalse()
    {
        var range = new PackageVersionRange(null, false, new PackageVersion("2.0.0"), false);

        Assert.False(range.Contains(new PackageVersion("2.0.0")));
    }

    private class TestPackage
    {
        public PackageVersion Version { get; set; } = null!;
    }
}
