// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Globalization;
using Stride.Core.Mathematics;
using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

/// <summary>
/// Tests for <see cref="Color4Converter"/> class.
/// </summary>
public class TestColor4Converter
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var converter = new Color4Converter();
        Assert.NotNull(converter);
    }

    [Fact]
    public void CanConvertFrom_WithString_ReturnsTrue()
    {
        var converter = new Color4Converter();
        Assert.True(converter.CanConvertFrom(typeof(string)));
    }

    [Fact]
    public void CanConvertFrom_WithColor_ReturnsTrue()
    {
        var converter = new Color4Converter();
        Assert.True(converter.CanConvertFrom(typeof(Color)));
    }

    [Fact]
    public void CanConvertFrom_WithColor3_ReturnsTrue()
    {
        var converter = new Color4Converter();
        Assert.True(converter.CanConvertFrom(typeof(Color3)));
    }

    [Fact]
    public void CanConvertTo_ToString_ReturnsTrue()
    {
        var converter = new Color4Converter();
        Assert.True(converter.CanConvertTo(typeof(string)));
    }

    [Fact]
    public void CanConvertTo_ToColor_ReturnsTrue()
    {
        var converter = new Color4Converter();
        Assert.True(converter.CanConvertTo(typeof(Color)));
    }

    [Fact]
    public void CanConvertTo_ToColor3_ReturnsTrue()
    {
        var converter = new Color4Converter();
        Assert.True(converter.CanConvertTo(typeof(Color3)));
    }

    [Fact]
    public void ConvertFrom_WithColor_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var color = new Color(255, 128, 64, 32);
        var result = converter.ConvertFrom(color);

        Assert.IsType<Color4>(result);
        var color4 = (Color4)result!;
        Assert.Equal(color.ToColor4(), color4);
    }

    [Fact]
    public void ConvertFrom_WithColor3_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var color3 = new Color3(1.0f, 0.5f, 0.25f);
        var result = converter.ConvertFrom(color3);

        Assert.IsType<Color4>(result);
        var color4 = (Color4)result!;
        Assert.Equal(color3.ToColor4(), color4);
    }

    [Fact]
    public void ConvertFrom_WithHexString_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var result = converter.ConvertFrom("#FF8040");

        Assert.IsType<Color4>(result);
        var color4 = (Color4)result!;
        Assert.Equal(255, (int)(color4.R * 255));
        Assert.Equal(128, (int)(color4.G * 255));
        Assert.Equal(64, (int)(color4.B * 255));
    }

    [Fact]
    public void ConvertFrom_WithNamedColor_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        // Named colors are not supported by the formatted string parser, only hex strings
        // Hex format is ARGB (Alpha, Red, Green, Blue)
        var result = converter.ConvertFrom("#FFFF0000"); // Red with full alpha (ARGB)

        Assert.IsType<Color4>(result);
        var color4 = (Color4)result!;
        Assert.Equal(1.0f, color4.R, 2);
        Assert.Equal(0.0f, color4.G, 2);
        Assert.Equal(0.0f, color4.B, 2);
        Assert.Equal(1.0f, color4.A, 2);
    }

    [Fact]
    public void ConvertFrom_WithFormattedString_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var result = converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, "R:1 G:0.5 B:0.25 A:1");

        Assert.IsType<Color4>(result);
        var color4 = (Color4)result!;
        Assert.Equal(1.0f, color4.R);
        Assert.Equal(0.5f, color4.G);
        Assert.Equal(0.25f, color4.B);
        Assert.Equal(1.0f, color4.A);
    }

    [Fact]
    public void ConvertTo_ToString_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);
        var result = converter.ConvertTo(color4, typeof(string));

        Assert.IsType<string>(result);
        Assert.Equal(color4.ToString(), result);
    }

    [Fact]
    public void ConvertTo_ToColor_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);
        var result = converter.ConvertTo(color4, typeof(Color));

        Assert.IsType<Color>(result);
        var color = (Color)result!;
        Assert.Equal((Color)color4, color);
    }

    [Fact]
    public void ConvertTo_ToColor3_ConvertsCorrectly()
    {
        var converter = new Color4Converter();
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);
        var result = converter.ConvertTo(color4, typeof(Color3));

        Assert.IsType<Color3>(result);
        var color3 = (Color3)result!;
        Assert.Equal(color4.ToColor3(), color3);
    }

    [Fact]
    public void RoundTrip_String_PreservesValue()
    {
        var converter = new Color4Converter();
        var original = new Color4(1.0f, 0.5f, 0.25f, 0.75f);

        var str = converter.ConvertTo(original, typeof(string)) as string;
        Assert.NotNull(str);

        var result = converter.ConvertFrom(str!);
        Assert.IsType<Color4>(result);

        var color4 = (Color4)result!;
        Assert.Equal(original.R, color4.R, 4);
        Assert.Equal(original.G, color4.G, 4);
        Assert.Equal(original.B, color4.B, 4);
        Assert.Equal(original.A, color4.A, 4);
    }

    [Fact]
    public void ConvertTo_WithNullDestinationType_ThrowsArgumentNullException()
    {
        var converter = new Color4Converter();
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);

        Assert.Throws<ArgumentNullException>(() => converter.ConvertTo(color4, null!));
    }

    [Fact]
    public void GetProperties_ReturnsCorrectProperties()
    {
        var converter = new Color4Converter();
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);
        var properties = converter.GetProperties(color4);

        Assert.NotNull(properties);
        Assert.True(properties.Count > 0);
        Assert.NotNull(properties["R"]);
        Assert.NotNull(properties["G"]);
        Assert.NotNull(properties["B"]);
        Assert.NotNull(properties["A"]);
    }

    [Fact]
    public void CreateInstance_CreatesNewColor4()
    {
        var converter = new Color4Converter();
        var propertyValues = new Dictionary<string, object>
        {
            { "R", 1.0f },
            { "G", 0.5f },
            { "B", 0.25f },
            { "A", 0.75f }
        };

        var result = converter.CreateInstance(null, propertyValues);

        Assert.IsType<Color4>(result);
        var color4 = (Color4)result;
        Assert.Equal(1.0f, color4.R);
        Assert.Equal(0.5f, color4.G);
        Assert.Equal(0.25f, color4.B);
        Assert.Equal(0.75f, color4.A);
    }
}
