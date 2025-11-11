// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Extensions;
using Xunit;

namespace Stride.Core.Design.Tests.Extensions;

/// <summary>
/// Tests for the <see cref="EnumExtensions"/> class.
/// </summary>
public class TestEnumExtensions
{
    [Flags]
    private enum TestFlags
    {
        None = 0,
        Flag1 = 1,
        Flag2 = 2,
        Flag3 = 4,
        Flag4 = 8,
        Combined12 = Flag1 | Flag2,  // 3
        Combined34 = Flag3 | Flag4,  // 12
        All = Flag1 | Flag2 | Flag3 | Flag4  // 15
    }

    [Fact]
    public void GetIndividualFlags_Type_ShouldReturnOnlyPowerOfTwoValues()
    {
        var result = EnumExtensions.GetIndividualFlags(typeof(TestFlags)).Cast<TestFlags>().ToList();

        Assert.Equal(4, result.Count);
        Assert.Contains(TestFlags.Flag1, result);
        Assert.Contains(TestFlags.Flag2, result);
        Assert.Contains(TestFlags.Flag3, result);
        Assert.Contains(TestFlags.Flag4, result);
        Assert.DoesNotContain(TestFlags.None, result);
        Assert.DoesNotContain(TestFlags.Combined12, result);
        Assert.DoesNotContain(TestFlags.Combined34, result);
        Assert.DoesNotContain(TestFlags.All, result);
    }

    [Fact]
    public void GetAllFlags_WithSingleFlag_ShouldReturnThatFlag()
    {
        var value = TestFlags.Flag1;

        var result = value.GetAllFlags().Cast<TestFlags>().ToList();

        Assert.Single(result);
        Assert.Contains(TestFlags.Flag1, result);
    }

    [Fact]
    public void GetAllFlags_WithCombinedFlags_ShouldReturnAllMatchingFlags()
    {
        var value = TestFlags.Combined12;

        var result = value.GetAllFlags().Cast<TestFlags>().ToList();

        // Should contain individual flags AND the combined value
        Assert.Contains(TestFlags.Flag1, result);
        Assert.Contains(TestFlags.Flag2, result);
        Assert.Contains(TestFlags.Combined12, result);
    }

    [Fact]
    public void GetAllFlags_WithNone_ShouldReturnEmpty()
    {
        var value = TestFlags.None;

        var result = value.GetAllFlags().ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void GetAllFlags_WithAll_ShouldReturnAllDefinedFlags()
    {
        var value = TestFlags.All;

        var result = value.GetAllFlags().Cast<TestFlags>().ToList();

        Assert.Contains(TestFlags.Flag1, result);
        Assert.Contains(TestFlags.Flag2, result);
        Assert.Contains(TestFlags.Flag3, result);
        Assert.Contains(TestFlags.Flag4, result);
        Assert.Contains(TestFlags.Combined12, result);
        Assert.Contains(TestFlags.Combined34, result);
        Assert.Contains(TestFlags.All, result);
    }

    [Fact]
    public void GetIndividualFlags_Value_ShouldReturnOnlyIndividualFlagsSet()
    {
        var value = TestFlags.Combined12;

        var result = value.GetIndividualFlags().Cast<TestFlags>().ToList();

        Assert.Equal(2, result.Count);
        Assert.Contains(TestFlags.Flag1, result);
        Assert.Contains(TestFlags.Flag2, result);
        Assert.DoesNotContain(TestFlags.Combined12, result);
    }

    [Fact]
    public void GetIndividualFlags_Value_WithSingleFlag_ShouldReturnThatFlag()
    {
        var value = TestFlags.Flag3;

        var result = value.GetIndividualFlags().Cast<TestFlags>().ToList();

        Assert.Single(result);
        Assert.Contains(TestFlags.Flag3, result);
    }

    [Fact]
    public void GetIndividualFlags_Value_WithNone_ShouldReturnEmpty()
    {
        var value = TestFlags.None;

        var result = value.GetIndividualFlags().ToList();

        Assert.Empty(result);
    }

    [Fact]
    public void GetEnum_WithMultipleFlags_ShouldCombineThem()
    {
        var flags = new Enum[] { TestFlags.Flag1, TestFlags.Flag3 };

        var result = (TestFlags)EnumExtensions.GetEnum(typeof(TestFlags), flags);

        Assert.Equal(TestFlags.Flag1 | TestFlags.Flag3, result);
        Assert.True(result.HasFlag(TestFlags.Flag1));
        Assert.True(result.HasFlag(TestFlags.Flag3));
        Assert.False(result.HasFlag(TestFlags.Flag2));
        Assert.False(result.HasFlag(TestFlags.Flag4));
    }

    [Fact]
    public void GetEnum_WithSingleFlag_ShouldReturnThatFlag()
    {
        var flags = new Enum[] { TestFlags.Flag2 };

        var result = (TestFlags)EnumExtensions.GetEnum(typeof(TestFlags), flags);

        Assert.Equal(TestFlags.Flag2, result);
    }

    [Fact]
    public void GetEnum_WithEmptyList_ShouldReturnNone()
    {
        var flags = Enumerable.Empty<Enum>();

        var result = (TestFlags)EnumExtensions.GetEnum(typeof(TestFlags), flags);

        Assert.Equal(TestFlags.None, result);
    }

    [Fact]
    public void GetEnum_WithAllFlags_ShouldReturnAll()
    {
        var flags = new Enum[] { TestFlags.Flag1, TestFlags.Flag2, TestFlags.Flag3, TestFlags.Flag4 };

        var result = (TestFlags)EnumExtensions.GetEnum(typeof(TestFlags), flags);

        Assert.Equal(TestFlags.All, result);
    }
}
