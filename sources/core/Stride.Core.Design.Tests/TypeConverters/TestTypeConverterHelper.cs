// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

/// <summary>
/// Tests for <see cref="TypeConverterHelper"/> class.
/// </summary>
public class TestTypeConverterHelper
{
    [Theory]
    [InlineData(typeof(double), typeof(float?))]
    [InlineData(typeof(double), typeof(int?))]
    [InlineData(typeof(double), typeof(long?))]
    [InlineData(typeof(double), typeof(ulong?))]
    [InlineData(typeof(double), typeof(short?))]
    [InlineData(typeof(double), typeof(ushort?))]
    [InlineData(typeof(double), typeof(sbyte?))]
    [InlineData(typeof(double), typeof(byte?))]
    [InlineData(typeof(double), typeof(decimal?))]
    [InlineData(typeof(float), typeof(float?))]
    [InlineData(typeof(int), typeof(int?))]
    [InlineData(typeof(string), typeof(float?))]
    [InlineData(typeof(string), typeof(int?))]
    [InlineData(typeof(string), typeof(double?))]
    public void TryConvert_ToNullable_Succeeds(Type sourceType, Type destinationType)
    {
        Assert.True(TypeConverterHelper.CanConvert(sourceType, destinationType));

        // char is excluded: Convert.ChangeType does not support numeric to char conversions (existing behavior)
        object source;
        double expectedValue;
        if (sourceType == typeof(string))
        {
            source = destinationType == typeof(int?) ? "4" : "4.5";
            expectedValue = source == "4" ? 4 : 4.5;
        }
        else
        {
            source = sourceType == typeof(float) ? 1.5f : 2.0;
            expectedValue = sourceType == typeof(float) ? 1.5 : 2.0;
        }

        Assert.True(TypeConverterHelper.TryConvert(source, destinationType, out var target));
        Assert.NotNull(target);

        // The result must be a boxed instance of the underlying (non-nullable) type
        var underlyingType = Nullable.GetUnderlyingType(destinationType);
        Assert.IsType(underlyingType, target);
        Assert.Equal(expectedValue, Convert.ToDouble(target), precision: 15);
    }

    [Fact]
    public void TryConvert_InvalidStringToNullable_Fails()
    {
        Assert.False(TypeConverterHelper.TryConvert("not a number", typeof(float?), out var target));
        Assert.Null(target);
    }

    [Fact]
    public void TryConvert_NullSourceToNullable_FailsWithNullTarget()
    {
        // A null source is not a conversion: the helper reports failure with a null target.
        // Callers (NodeViewModel, GraphNodeBase) handle null values before calling this method.
        Assert.False(TypeConverterHelper.TryConvert(null!, typeof(float?), out var target));
        Assert.Null(target);
    }

    [Fact]
    public void TryConvert_ToNonNullableStillWorks()
    {
        // Non-nullable destinations keep working as before
        Assert.True(TypeConverterHelper.TryConvert(1.5, typeof(float), out var floatTarget));
        Assert.Equal(1.5f, floatTarget);

        Assert.True(TypeConverterHelper.TryConvert(3, typeof(double), out var doubleTarget));
        Assert.Equal(3.0, doubleTarget);
    }

    [Fact]
    public void TryConvert_IncompatibleTypeToNullable_Fails()
    {
        // A type with no conversion path to the underlying type must still be rejected
        Assert.False(TypeConverterHelper.TryConvert(new object(), typeof(float?), out var target));
        Assert.Null(target);
    }

    [Fact]
    public void TryConvert_DirectUnderlyingValueToNullable_Succeeds()
    {
        // A boxed underlying value is re-boxed as the nullable value
        Assert.True(TypeConverterHelper.TryConvert(7, typeof(int?), out var target));
        Assert.IsType<int>(target);
        Assert.Equal(7, target);
    }
}
