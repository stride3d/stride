// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Yaml.Tests;

public class TestVersion
{
    [Fact]
    public void TestVersionConstruction()
    {
        var version = new Version(1, 2);

        Assert.Equal(1, version.Major);
        Assert.Equal(2, version.Minor);
    }

    [Fact]
    public void TestVersionZeroValues()
    {
        var version = new Version(0, 0);

        Assert.Equal(0, version.Major);
        Assert.Equal(0, version.Minor);
    }

    [Fact]
    public void TestVersionLargeValues()
    {
        var version = new Version(int.MaxValue, int.MaxValue);

        Assert.Equal(int.MaxValue, version.Major);
        Assert.Equal(int.MaxValue, version.Minor);
    }

    [Fact]
    public void TestVersionEqualsIdentical()
    {
        var version1 = new Version(1, 2);
        var version2 = new Version(1, 2);

        Assert.True(version1.Equals(version2));
    }

    [Fact]
    public void TestVersionEqualsSameInstance()
    {
        var version = new Version(1, 2);

        Assert.True(version.Equals(version));
    }

    [Fact]
    public void TestVersionNotEqualsDifferentMajor()
    {
        var version1 = new Version(1, 2);
        var version2 = new Version(2, 2);

        Assert.False(version1.Equals(version2));
    }

    [Fact]
    public void TestVersionNotEqualsDifferentMinor()
    {
        var version1 = new Version(1, 2);
        var version2 = new Version(1, 3);

        Assert.False(version1.Equals(version2));
    }

    [Fact]
    public void TestVersionNotEqualsNull()
    {
        var version = new Version(1, 2);

        Assert.False(version.Equals(null));
    }

    [Fact]
    public void TestVersionNotEqualsDifferentType()
    {
        var version = new Version(1, 2);
        var other = "1.2";

        Assert.False(version.Equals(other));
    }

    [Fact]
    public void TestVersionGetHashCodeConsistency()
    {
        var version = new Version(1, 2);

        var hash1 = version.GetHashCode();
        var hash2 = version.GetHashCode();

        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void TestVersionGetHashCodeEqualInstances()
    {
        var version1 = new Version(1, 2);
        var version2 = new Version(1, 2);

        Assert.Equal(version1.GetHashCode(), version2.GetHashCode());
    }

    [Fact]
    public void TestVersionGetHashCodeDifferentInstances()
    {
        var version1 = new Version(1, 2);
        var version2 = new Version(2, 1);
        var version3 = new Version(10, 20);

        // Different versions can have different hash codes
        // We test that the GetHashCode method executes without error
        var hash1 = version1.GetHashCode();
        var hash2 = version2.GetHashCode();
        var hash3 = version3.GetHashCode();

        // At minimum, verify hash codes are computed consistently
        Assert.Equal(hash1, version1.GetHashCode());
        Assert.Equal(hash2, version2.GetHashCode());
        Assert.Equal(hash3, version3.GetHashCode());
    }

    [Fact]
    public void TestVersionCommonYamlVersions()
    {
        var version10 = new Version(1, 0);
        var version11 = new Version(1, 1);
        var version12 = new Version(1, 2);

        Assert.Equal(1, version10.Major);
        Assert.Equal(0, version10.Minor);

        Assert.Equal(1, version11.Major);
        Assert.Equal(1, version11.Minor);

        Assert.Equal(1, version12.Major);
        Assert.Equal(2, version12.Minor);

        Assert.False(version10.Equals(version11));
        Assert.False(version11.Equals(version12));
        Assert.False(version10.Equals(version12));
    }

    [Fact]
    public void TestVersionNegativeValues()
    {
        // Version class doesn't validate negative values in constructor
        // This tests the actual behavior
        var version = new Version(-1, -1);

        Assert.Equal(-1, version.Major);
        Assert.Equal(-1, version.Minor);
    }

    [Fact]
    public void TestVersionEqualsWithNegativeValues()
    {
        var version1 = new Version(-1, -2);
        var version2 = new Version(-1, -2);

        Assert.True(version1.Equals(version2));
    }
}
