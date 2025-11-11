// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using Stride.Core.Mathematics;
using Stride.Core.TypeConverters;
using Xunit;

namespace Stride.Core.Design.Tests.TypeConverters;

public class TestColorConverter
{
    private readonly ColorConverter converter = new();

    [Fact]
    public void CanConvertTo_Color3_ReturnsTrue()
    {
        Assert.True(converter.CanConvertTo(typeof(Color3)));
    }

    [Fact]
    public void CanConvertTo_Color4_ReturnsTrue()
    {
        Assert.True(converter.CanConvertTo(typeof(Color4)));
    }

    [Fact]
    public void CanConvertFrom_Color3_ReturnsTrue()
    {
        Assert.True(converter.CanConvertFrom(typeof(Color3)));
    }

    [Fact]
    public void CanConvertFrom_Color4_ReturnsTrue()
    {
        Assert.True(converter.CanConvertFrom(typeof(Color4)));
    }

    [Fact]
    public void ConvertTo_ToString_ReturnsString()
    {
        var color = new Color(255, 128, 64, 32);
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, color, typeof(string));

        Assert.NotNull(result);
        Assert.IsType<string>(result);
    }

    [Fact]
    public void ConvertTo_ToColor3_ConvertsCorrectly()
    {
        var color = new Color(255, 128, 64, 32);
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, color, typeof(Color3));

        Assert.NotNull(result);
        var color3 = Assert.IsType<Color3>(result);
        Assert.Equal(color.ToColor3(), color3);
    }

    [Fact]
    public void ConvertTo_ToColor4_ConvertsCorrectly()
    {
        var color = new Color(255, 128, 64, 32);
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, color, typeof(Color4));

        Assert.NotNull(result);
        var color4 = Assert.IsType<Color4>(result);
        Assert.Equal(color.ToColor4(), color4);
    }

    [Fact]
    public void ConvertTo_ToInstanceDescriptor_ReturnsDescriptor()
    {
        var color = new Color(255, 128, 64, 32);
        var result = converter.ConvertTo(null, CultureInfo.InvariantCulture, color, typeof(InstanceDescriptor));

        Assert.NotNull(result);
        Assert.IsType<InstanceDescriptor>(result);
    }

    [Fact]
    public void ConvertFrom_Color3_ConvertsCorrectly()
    {
        var color3 = new Color3(1.0f, 0.5f, 0.25f);
        var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, color3);

        Assert.NotNull(result);
        var color = Assert.IsType<Color>(result);
        Assert.Equal((Color)color3, color);
    }

    [Fact]
    public void ConvertFrom_Color4_ConvertsCorrectly()
    {
        var color4 = new Color4(1.0f, 0.5f, 0.25f, 1.0f);
        var result = converter.ConvertFrom(null, CultureInfo.InvariantCulture, color4);

        Assert.NotNull(result);
        var color = Assert.IsType<Color>(result);
        Assert.Equal((Color)color4, color);
    }

    [Fact]
    public void CreateInstance_WithPropertyValues_CreatesColor()
    {
        var properties = new Hashtable
        {
            ["R"] = (byte)255,
            ["G"] = (byte)128,
            ["B"] = (byte)64,
            ["A"] = (byte)32
        };

        var result = converter.CreateInstance(null, properties);

        Assert.NotNull(result);
        var color = Assert.IsType<Color>(result);
        Assert.Equal(255, color.R);
        Assert.Equal(128, color.G);
        Assert.Equal(64, color.B);
        Assert.Equal(32, color.A);
    }

    [Fact]
    public void GetProperties_ReturnsRGBAProperties()
    {
        var color = new Color(255, 128, 64, 32);
        var properties = converter.GetProperties(null, color, null);

        Assert.NotNull(properties);
        Assert.Equal(4, properties.Count);
        Assert.NotNull(properties["R"]);
        Assert.NotNull(properties["G"]);
        Assert.NotNull(properties["B"]);
        Assert.NotNull(properties["A"]);
    }
}
