// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using Stride.Core.Mathematics;
using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

/// <summary>
/// Tests for <see cref="Color3Converter"/> class.
/// </summary>
public class TestColor3Converter
{
    [Fact]
    public void Constructor_SetsProperties()
    {
        var converter = new Color3Converter();
        Assert.NotNull(converter);
    }

    [Fact]
    public void CanConvertFrom_WithString_ReturnsTrue()
    {
        var converter = new Color3Converter();
        Assert.True(converter.CanConvertFrom(typeof(string)));
    }

    [Fact]
    public void CanConvertFrom_WithColor_ReturnsTrue()
    {
        var converter = new Color3Converter();
        Assert.True(converter.CanConvertFrom(typeof(Color)));
    }

    [Fact]
    public void CanConvertFrom_WithColor4_ReturnsTrue()
    {
        var converter = new Color3Converter();
        Assert.True(converter.CanConvertFrom(typeof(Color4)));
    }

    [Fact]
    public void CanConvertTo_ToString_ReturnsTrue()
    {
        var converter = new Color3Converter();
        Assert.True(converter.CanConvertTo(typeof(string)));
    }

    [Fact]
    public void ConvertFrom_WithColor_ConvertsCorrectly()
    {
        var converter = new Color3Converter();
        var color = new Color(255, 128, 64);
        var result = converter.ConvertFrom(color);

        Assert.IsType<Color3>(result);
        var color3 = (Color3)result!;
        Assert.Equal(color.ToColor3(), color3);
    }

    [Fact]
    public void ConvertFrom_WithColor4_ConvertsCorrectly()
    {
        var converter = new Color3Converter();
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);
        var result = converter.ConvertFrom(color4);

        Assert.IsType<Color3>(result);
        var color3 = (Color3)result!;
        Assert.Equal(color4.ToColor3(), color3);
    }

    [Fact]
    public void ConvertFrom_WithHexString_ConvertsCorrectly()
    {
        var converter = new Color3Converter();
        var result = converter.ConvertFrom("#FF8040");

        Assert.IsType<Color3>(result);
        var color3 = (Color3)result!;
        Assert.Equal(255, (int)(color3.R * 255));
        Assert.Equal(128, (int)(color3.G * 255));
        Assert.Equal(64, (int)(color3.B * 255));
    }

    [Fact]
    public void ConvertFrom_WithFormattedString_ConvertsCorrectly()
    {
        var converter = new Color3Converter();
        var result = converter.ConvertFrom(null, System.Globalization.CultureInfo.InvariantCulture, "R:1 G:0.5 B:0.25");

        Assert.IsType<Color3>(result);
        var color3 = (Color3)result!;
        Assert.Equal(1.0f, color3.R);
        Assert.Equal(0.5f, color3.G);
        Assert.Equal(0.25f, color3.B);
    }

    [Fact]
    public void ConvertTo_ToString_ConvertsCorrectly()
    {
        var converter = new Color3Converter();
        var color3 = new Color3(1.0f, 0.5f, 0.25f);
        var result = converter.ConvertTo(color3, typeof(string));

        Assert.IsType<string>(result);
        Assert.Equal(color3.ToString(), result);
    }

    [Fact]
    public void RoundTrip_String_PreservesValue()
    {
        var converter = new Color3Converter();
        var original = new Color3(1.0f, 0.5f, 0.25f);

        var str = converter.ConvertTo(original, typeof(string)) as string;
        Assert.NotNull(str);

        var result = converter.ConvertFrom(str!);
        Assert.IsType<Color3>(result);

        var color3 = (Color3)result!;
        Assert.Equal(original.R, color3.R, 4);
        Assert.Equal(original.G, color3.G, 4);
        Assert.Equal(original.B, color3.B, 4);
    }

    [Fact]
    public void GetProperties_ReturnsCorrectProperties()
    {
        var converter = new Color3Converter();
        var color3 = new Color3(1.0f, 0.5f, 0.25f);
        var properties = converter.GetProperties(color3);

        Assert.NotNull(properties);
        Assert.True(properties.Count > 0);
        Assert.NotNull(properties["R"]);
        Assert.NotNull(properties["G"]);
        Assert.NotNull(properties["B"]);
    }

    [Fact]
    public void CreateInstance_CreatesNewColor3()
    {
        var converter = new Color3Converter();
        var propertyValues = new Dictionary<string, object>
        {
            { "R", 1.0f },
            { "G", 0.5f },
            { "B", 0.25f }
        };

        var result = converter.CreateInstance(null, propertyValues);

        Assert.IsType<Color3>(result);
        var color3 = (Color3)result;
        Assert.Equal(1.0f, color3.R);
        Assert.Equal(0.5f, color3.G);
        Assert.Equal(0.25f, color3.B);
    }
}
