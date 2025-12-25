// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

/// <summary>
/// Tests for <see cref="BaseConverter"/> class through a concrete implementation.
/// </summary>
public class TestBaseConverter
{
    // Test converter implementation
    private class TestConverter : BaseConverter
    {
        public TestConverter()
        {
            var type = typeof(TestStruct);
            Properties = new PropertyDescriptorCollection(
            [
                new FieldPropertyDescriptor(type.GetField(nameof(TestStruct.X))!),
                new FieldPropertyDescriptor(type.GetField(nameof(TestStruct.Y))!)
            ]);
        }

        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                return ConvertFromString<TestStruct, int>(context, culture, str);
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object? ConvertTo(ITypeDescriptorContext? context, CultureInfo? culture, object? value, Type destinationType)
        {
            if (value is TestStruct testStruct && destinationType == typeof(string))
            {
                return ConvertFromValues(context, culture, [testStruct.X, testStruct.Y]);
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    private struct TestStruct
    {
        public int X;
        public int Y;

        public TestStruct(int x, int y)
        {
            X = x;
            Y = y;
        }
    }

    [Fact]
    public void CanConvertFrom_String_ReturnsTrue()
    {
        var converter = new TestConverter();
        Assert.True(converter.CanConvertFrom(typeof(string)));
    }

    [Fact]
    public void CanConvertFrom_Int_ReturnsFalse()
    {
        var converter = new TestConverter();
        Assert.False(converter.CanConvertFrom(typeof(int)));
    }

    [Fact]
    public void CanConvertTo_String_ReturnsTrue()
    {
        var converter = new TestConverter();
        Assert.True(converter.CanConvertTo(typeof(string)));
    }

    [Fact]
    public void CanConvertTo_InstanceDescriptor_ReturnsTrue()
    {
        var converter = new TestConverter();
        Assert.True(converter.CanConvertTo(typeof(System.ComponentModel.Design.Serialization.InstanceDescriptor)));
    }

    [Fact]
    public void ConvertFrom_ValidString_ReturnsStruct()
    {
        var converter = new TestConverter();
        var result = converter.ConvertFrom("X:10 Y:20");

        Assert.NotNull(result);
        var testStruct = (TestStruct)result;
        Assert.Equal(10, testStruct.X);
        Assert.Equal(20, testStruct.Y);
    }

    [Fact]
    public void ConvertFrom_WithCulture_UsesSpecifiedCulture()
    {
        var converter = new TestConverter();
        var culture = new CultureInfo("fr-FR");
        var result = converter.ConvertFrom(null, culture, "X:10 Y:20");

        Assert.NotNull(result);
        var testStruct = (TestStruct)result;
        Assert.Equal(10, testStruct.X);
        Assert.Equal(20, testStruct.Y);
    }

    [Fact]
    public void ConvertFrom_NullString_ReturnsDefault()
    {
        var converter = new TestConverter();
        var result = converter.ConvertFrom(null, null, "");

        // Empty string returns default struct, not null
        Assert.NotNull(result);
        var testStruct = (TestStruct)result;
        Assert.Equal(0, testStruct.X);
        Assert.Equal(0, testStruct.Y);
    }

    [Fact]
    public void ConvertFrom_InvalidFormat_ThrowsFormatException()
    {
        var converter = new TestConverter();
        Assert.Throws<FormatException>(() => converter.ConvertFrom("InvalidFormat"));
    }

    [Fact]
    public void ConvertTo_ValidStruct_ReturnsString()
    {
        var converter = new TestConverter();
        var testStruct = new TestStruct(10, 20);
        var result = converter.ConvertTo(testStruct, typeof(string));

        Assert.NotNull(result);
        var str = result as string;
        Assert.NotNull(str);
        Assert.Contains("10", str);
        Assert.Contains("20", str);
    }

    [Fact]
    public void ConvertTo_WithCulture_UsesSpecifiedCulture()
    {
        var converter = new TestConverter();
        var testStruct = new TestStruct(10, 20);
        var culture = new CultureInfo("fr-FR");
        var result = converter.ConvertTo(null, culture, testStruct, typeof(string));

        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void GetCreateInstanceSupported_ReturnsTrue()
    {
        var converter = new TestConverter();
        Assert.True(converter.GetCreateInstanceSupported(null));
    }

    [Fact]
    public void GetPropertiesSupported_ReturnsTrue()
    {
        var converter = new TestConverter();
        Assert.True(converter.GetPropertiesSupported(null));
    }

    [Fact]
    public void GetProperties_ReturnsPropertyCollection()
    {
        var converter = new TestConverter();
        var testStruct = new TestStruct(10, 20);
        var properties = converter.GetProperties(testStruct);

        Assert.NotNull(properties);
        Assert.Equal(2, properties.Count);
        Assert.NotNull(properties["X"]);
        Assert.NotNull(properties["Y"]);
    }

    [Fact]
    public void GetProperties_WithAttributes_ReturnsPropertyCollection()
    {
        var converter = new TestConverter();
        var testStruct = new TestStruct(10, 20);
        var properties = converter.GetProperties(null, testStruct, null);

        Assert.NotNull(properties);
        Assert.Equal(2, properties.Count);
    }

    [Fact]
    public void Properties_InitializedCorrectly()
    {
        var converter = new TestConverter();
        Assert.NotNull(converter.GetProperties(new TestStruct()));
        Assert.Equal(2, converter.GetProperties(new TestStruct()).Count);
    }
}
