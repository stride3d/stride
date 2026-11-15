// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
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

    [Flags]
    private enum SignedFlags : int
    {
        None = 0,
        Flag1 = 1,
        Flag2 = 2,
        All = -1
    }

    [Fact]
    public void GetIndividualFlags_Type_WithNegativeMember_ShouldReturnOnlyIndividualFlags()
    {
        var result = EnumExtensions.GetIndividualFlags(typeof(SignedFlags))
            .Cast<SignedFlags>()
            .ToList();

        Assert.Equal(new[] { SignedFlags.Flag1, SignedFlags.Flag2 }, result);
    }

    [Fact]
    public void GetIndividualFlags_Value_WithNegativeValue_ShouldReturnOnlyIndividualFlags()
    {
        var result = SignedFlags.All.GetIndividualFlags()
            .Cast<SignedFlags>()
            .ToList();

        Assert.Equal(new[] { SignedFlags.Flag1, SignedFlags.Flag2 }, result);
    }

    [Fact]
    public void GetAllFlags_WithNegativeValue_ShouldReturnAllMatchingNamedFlags()
    {
        var result = SignedFlags.All.GetAllFlags()
            .Cast<SignedFlags>()
            .ToList();

        Assert.Equal(
            new[] { SignedFlags.Flag1, SignedFlags.Flag2, SignedFlags.All },
            result);
    }

    [Fact]
    public void GetEnum_WithNegativeFlag_ShouldPreserveValue()
    {
        var flags = new Enum[] { SignedFlags.Flag1, SignedFlags.All };

        var result = (SignedFlags)EnumExtensions.GetEnum(
            typeof(SignedFlags), flags);

        Assert.Equal(SignedFlags.All, result);
    }

    [Flags]
    private enum SByteBoundaryFlags : sbyte
    {
        None = 0,
        Low = 1,
        High = sbyte.MinValue,
        Combined = Low | High,
        All = -1
    }

    [Flags]
    private enum ShortBoundaryFlags : short
    {
        None = 0,
        Low = 1,
        High = short.MinValue,
        Combined = Low | High,
        All = -1
    }

    [Flags]
    private enum IntBoundaryFlags : int
    {
        None = 0,
        Low = 1,
        High = int.MinValue,
        Combined = Low | High,
        All = -1
    }

    [Flags]
    private enum LongBoundaryFlags : long
    {
        None = 0,
        Low = 1,
        High = long.MinValue,
        Combined = Low | High,
        All = -1
    }

    [Theory]
    [InlineData(typeof(SByteBoundaryFlags))]
    [InlineData(typeof(ShortBoundaryFlags))]
    [InlineData(typeof(IntBoundaryFlags))]
    [InlineData(typeof(LongBoundaryFlags))]
    public void GetIndividualFlags_Type_WithSignedHighBit_ShouldReturnOnlyIndividualFlags(Type enumType)
    {
        var low = (Enum)Enum.Parse(enumType, "Low");
        var high = (Enum)Enum.Parse(enumType, "High");

        var result = EnumExtensions.GetIndividualFlags(enumType).ToList();

        Assert.Equal(new[] { low, high }, result);
    }

    [Theory]
    [InlineData(typeof(SByteBoundaryFlags))]
    [InlineData(typeof(ShortBoundaryFlags))]
    [InlineData(typeof(IntBoundaryFlags))]
    [InlineData(typeof(LongBoundaryFlags))]
    public void GetIndividualFlags_Value_WithSignedHighBit_ShouldPreserveSelection(Type enumType)
    {
        var none = (Enum)Enum.Parse(enumType, "None");
        var low = (Enum)Enum.Parse(enumType, "Low");
        var high = (Enum)Enum.Parse(enumType, "High");
        var combined = (Enum)Enum.Parse(enumType, "Combined");
        var all = (Enum)Enum.Parse(enumType, "All");

        Assert.Empty(none.GetIndividualFlags());
        Assert.Equal(new[] { low }, low.GetIndividualFlags().ToList());
        Assert.Equal(new[] { high }, high.GetIndividualFlags().ToList());
        Assert.Equal(new[] { low, high }, combined.GetIndividualFlags().ToList());
        Assert.Equal(new[] { low, high }, all.GetIndividualFlags().ToList());
    }

    [Theory]
    [InlineData(typeof(SByteBoundaryFlags))]
    [InlineData(typeof(ShortBoundaryFlags))]
    [InlineData(typeof(IntBoundaryFlags))]
    [InlineData(typeof(LongBoundaryFlags))]
    public void GetAllFlags_WithSignedHighBit_ShouldReturnMatchingNamedFlags(Type enumType)
    {
        var none = (Enum)Enum.Parse(enumType, "None");
        var low = (Enum)Enum.Parse(enumType, "Low");
        var high = (Enum)Enum.Parse(enumType, "High");
        var combined = (Enum)Enum.Parse(enumType, "Combined");
        var all = (Enum)Enum.Parse(enumType, "All");

        Assert.Empty(none.GetAllFlags());
        Assert.Equal(new[] { low }, low.GetAllFlags().ToList());
        Assert.Equal(new[] { high }, high.GetAllFlags().ToList());
        Assert.Equal(new[] { low, high, combined }, combined.GetAllFlags().ToList());
        Assert.Equal(new[] { low, high, combined, all }, all.GetAllFlags().ToList());
    }

    [Theory]
    [InlineData(typeof(SByteBoundaryFlags))]
    [InlineData(typeof(ShortBoundaryFlags))]
    [InlineData(typeof(IntBoundaryFlags))]
    [InlineData(typeof(LongBoundaryFlags))]
    public void GetEnum_WithSignedHighBit_ShouldPreserveCombinedValueAndRoundTrip(Type enumType)
    {
        var low = (Enum)Enum.Parse(enumType, "Low");
        var high = (Enum)Enum.Parse(enumType, "High");
        var combined = (Enum)Enum.Parse(enumType, "Combined");
        var all = (Enum)Enum.Parse(enumType, "All");

        var result = EnumExtensions.GetEnum(enumType, new[] { low, high });

        Assert.Equal(combined, result);
        Assert.Equal(new[] { low, high }, result.GetIndividualFlags().ToList());
        Assert.Equal(combined, EnumExtensions.GetEnum(enumType, result.GetIndividualFlags()));
        Assert.Equal(all, EnumExtensions.GetEnum(enumType, new[] { low, all }));
    }

    [Flags]
    private enum ULongFlags : ulong
    {
        None = 0,
        Flag1 = 1UL << 0,
        Flag63 = 1UL << 63,
        Combined = Flag1 | Flag63,
        All = ulong.MaxValue
    }

    [Fact]
    public void GetIndividualFlags_Type_WithULongFlags_ShouldReturnOnlyPowerOfTwoValues()
    {
        var result = EnumExtensions.GetIndividualFlags(typeof(ULongFlags)).Cast<ULongFlags>().ToList();

        Assert.Equal(new[] { ULongFlags.Flag1, ULongFlags.Flag63 }, result);
    }

    [Flags]
    private enum ULongAllFlags : ulong
    {
        None = 0,
        Flag1 = 1UL << 0,
        Flag63 = 1UL << 63,
        All = ulong.MaxValue
    }

    [Fact]
    public void GetIndividualFlags_Type_WithULongMaxValue_ShouldReturnOnlyPowerOfTwoValues()
    {
        var result = EnumExtensions.GetIndividualFlags(typeof(ULongAllFlags)).Cast<ULongAllFlags>().ToList();

        Assert.Equal(new[] { ULongAllFlags.Flag1, ULongAllFlags.Flag63 }, result);
    }

    [Theory]
    [InlineData(0UL, new ulong[0])]
    [InlineData(1UL, new[] { 1UL })]
    [InlineData(1UL << 63, new[] { 1UL << 63 })]
    [InlineData((1UL << 63) | 1UL, new[] { 1UL, 1UL << 63 })]
    [InlineData(ulong.MaxValue, new[] { 1UL, 1UL << 63 })]
    public void GetIndividualFlags_Value_WithULongFlags_ShouldReturnOnlyIndividualFlagsSet(ulong bits, ulong[] expected)
    {
        var result = ((ULongFlags)bits).GetIndividualFlags().Select(Convert.ToUInt64).ToList();

        Assert.Equal(expected, result);
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
