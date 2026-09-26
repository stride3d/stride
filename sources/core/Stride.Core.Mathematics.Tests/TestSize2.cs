// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSize2
{
    [Theory]
    [InlineData(800, 600)]
    [InlineData(0, 0)]
    [InlineData(-4, -9)]
    [InlineData(1920, 1)]
    public void TestSize2Construction(int width, int height)
    {
        var size = new Size2(width, height);
        Assert.Equal(width, size.Width);
        Assert.Equal(height, size.Height);
    }

    [Fact]
    public void TestSize2Zero()
    {
        var zero = Size2.Zero;
        Assert.Equal(0, zero.Width);
        Assert.Equal(0, zero.Height);
    }

    [Fact]
    public void TestSize2Empty()
    {
        var empty = Size2.Empty;
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
        Assert.Equal(Size2.Zero, empty);
    }

    [Fact]
    public void TestSize2Equality()
    {
        var size1 = new Size2(800, 600);
        var size2 = new Size2(800, 600);
        var size3 = new Size2(1024, 768);
        var size4 = new Size2(-3, -17);
        var size5 = new Size2(-3, -17);

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
    public void TestSize2EqualsObject()
    {
        var size = new Size2(37, -11);

        Assert.True(size.Equals((object)new Size2(37, -11)));
        Assert.False(size.Equals((object)new Size2(37, 11)));
        Assert.False(size.Equals(null));
        Assert.False(size.Equals("not a size"));
        Assert.False(size.Equals(37));
    }

    [Fact]
    public void TestSize2GetHashCode()
    {
        var size1 = new Size2(800, 600);
        var size2 = new Size2(800, 600);
        var size3 = new Size2(1024, 768);
        var size4 = new Size2(-3, -17);
        var size5 = new Size2(-3, -17);

        Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
        Assert.NotEqual(size1.GetHashCode(), size3.GetHashCode());
        Assert.Equal(size4.GetHashCode(), size5.GetHashCode());
    }

    [Fact]
    public void TestSize2ToString()
    {
        var size = new Size2(800, 600);
        var str = size.ToString();
        Assert.Equal("(800,600)", str);
    }

    [Fact]
    public void TestSize2ToStringWithFormat()
    {
        var size = new Size2(7, -42);
        var str = size.ToString("D4", CultureInfo.InvariantCulture);

        Assert.Equal("(0007,-0042)", str);
    }

    [Fact]
    public void TestSize2TryFormat()
    {
        var size = new Size2(12, -34);
        ISpanFormattable formattable = size;

        Span<char> buffer = stackalloc char[64];
        var success = formattable.TryFormat(buffer, out var charsWritten, default, CultureInfo.InvariantCulture);

        Assert.True(success);
        Assert.Equal("(12,-34)", buffer[..charsWritten].ToString());
    }

    [Fact]
    public void TestSize2Deconstruct()
    {
        var size = new Size2(800, 600);
        var (width, height) = size;
        Assert.Equal(800, width);
        Assert.Equal(600, height);
    }
}
