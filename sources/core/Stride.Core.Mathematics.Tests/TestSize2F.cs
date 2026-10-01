// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSize2F
{
    [Theory]
    [InlineData(800.5f, 600.5f)]
    [InlineData(0f, 0f)]
    [InlineData(-4.25f, -9.75f)]
    [InlineData(1920.1f, 0.5f)]
    public void TestSize2FConstruction(float width, float height)
    {
        var size = new Size2F(width, height);
        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
    }

    [Fact]
    public void TestSize2FZero()
    {
        var zero = Size2F.Zero;
        Assert.Equal(0f, zero.Width);
        Assert.Equal(0f, zero.Height);
    }

    [Fact]
    public void TestSize2FEmpty()
    {
        var empty = Size2F.Empty;
        Assert.Equal(0f, empty.Width);
        Assert.Equal(0f, empty.Height);
        Assert.Equal(Size2F.Zero, empty);
    }

    [Fact]
    public void TestSize2FEquality()
    {
        var size1 = new Size2F(800.5f, 600.5f);
        var size2 = new Size2F(800.5f, 600.5f);
        var size3 = new Size2F(1024.5f, 768.5f);
        var size4 = new Size2F(-3.25f, -17.75f);
        var size5 = new Size2F(-3.25f, -17.75f);

        Assert.Equal(size1, size2);
        Assert.NotEqual(size1, size3);
        Assert.True(size1 == size2);
        Assert.True(size1 != size3);
        Assert.True(size1.Equals(size2));
        Assert.False(size1.Equals(size3));

        // Negative dimensions.
        Assert.Equal(size4, size5);
        Assert.True(size4 == size5);
        Assert.NotEqual(size1, size4);
    }

    [Fact]
    public void TestSize2FEqualsObject()
    {
        var size = new Size2F(37.5f, -11.25f);

        Assert.True(size.Equals((object)new Size2F(37.5f, -11.25f)));
        Assert.False(size.Equals((object)new Size2F(37.5f, 11.25f)));
        Assert.False(size.Equals(null));
        Assert.False(size.Equals("not a size"));
        Assert.False(size.Equals(37.5f));
    }

    [Fact]
    public void TestSize2FGetHashCode()
    {
        var size1 = new Size2F(800.5f, 600.5f);
        var size2 = new Size2F(800.5f, 600.5f);
        var size3 = new Size2F(1024.5f, 768.5f);
        var size4 = new Size2F(-3.25f, -17.75f);
        var size5 = new Size2F(-3.25f, -17.75f);

        Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
        Assert.NotEqual(size1.GetHashCode(), size3.GetHashCode());
        Assert.Equal(size4.GetHashCode(), size5.GetHashCode());
    }

    [Fact]
    public void TestSize2FToString()
    {
        var size = new Size2F(800.5f, 600.5f);
        var str = size.ToString();
        Assert.NotNull(str);
        Assert.Contains("800", str);
        Assert.Contains("600", str);
    }

    [Fact]
    public void TestSize2FToStringWithFormat()
    {
        var size = new Size2F(7.5f, -42.25f);
        var str = size.ToString("F1", CultureInfo.InvariantCulture);

        Assert.Equal("(7.5,-42.2)", str);
    }

    [Fact]
    public void TestSize2FTryFormat()
    {
        var size = new Size2F(12.5f, -34.5f);
        ISpanFormattable formattable = size;

        Span<char> buffer = stackalloc char[64];
        var success = formattable.TryFormat(buffer, out var charsWritten, "F1", CultureInfo.InvariantCulture);

        Assert.True(success);
        Assert.Equal("(12.5,-34.5)", buffer[..charsWritten].ToString());
    }

    [Fact]
    public void TestSize2FDeconstruct()
    {
        var size = new Size2F(800.5f, 600.5f);
        var (width, height) = size;
        Assert.Equal(800.5f, width);
        Assert.Equal(600.5f, height);
    }
}
