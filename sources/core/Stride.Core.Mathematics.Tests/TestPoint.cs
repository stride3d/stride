// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestPoint
{
    [Fact]
    public void TestEquality()
    {
        var point1 = new Point(5, 5);
        var point2 = new Point(5, 5);
        var point3 = new Point(10, 10);
        var point4 = new Point(-5, -10);
        var point5 = new Point(-5, -10);

        Assert.Equal(point1, point2);
        Assert.NotEqual(point1, point3);
        Assert.True(point1 == point2);
        Assert.True(point1 != point3);
        Assert.True(point1.Equals(point2));
        Assert.False(point1.Equals(point3));

        // Negative coordinates.
        Assert.Equal(point4, point5);
        Assert.True(point4 == point5);
        Assert.NotEqual(point1, point4);
    }

    [Fact]
    public void TestEqualsObject()
    {
        var point = new Point(3, -7);

        Assert.True(point.Equals((object)new Point(3, -7)));
        Assert.False(point.Equals((object)new Point(3, 7)));
        Assert.False(point.Equals(null));
        Assert.False(point.Equals("not a point"));
        Assert.False(point.Equals(3));
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(10, 20)]
    [InlineData(-5, -10)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void TestConstruction(int x, int y)
    {
        var point = new Point(x, y);
        Assert.Equal(x, point.X);
        Assert.Equal(y, point.Y);
    }

    [Fact]
    public void TestZero()
    {
        Assert.Equal(0, Point.Zero.X);
        Assert.Equal(0, Point.Zero.Y);
    }

    [Fact]
    public void TestGetHashCode()
    {
        var point1 = new Point(5, 10);
        var point2 = new Point(5, 10);
        var point3 = new Point(10, 5);
        var point4 = new Point(-3, -7);
        var point5 = new Point(-3, -7);

        Assert.Equal(point1.GetHashCode(), point2.GetHashCode());
        Assert.NotEqual(point1.GetHashCode(), point3.GetHashCode());
        Assert.Equal(point4.GetHashCode(), point5.GetHashCode());
    }

    [Fact]
    public void TestToString()
    {
        var point = new Point(-123, 456);
        var str = point.ToString();

        Assert.Equal("(-123,456)", str);
    }

    [Fact]
    public void TestToStringWithFormat()
    {
        var point = new Point(7, -42);
        var str = point.ToString("D4", CultureInfo.InvariantCulture);

        Assert.Equal("(0007,-0042)", str);
    }

    [Fact]
    public void TestTryFormat()
    {
        var point = new Point(12, -34);
        ISpanFormattable formattable = point;

        Span<char> buffer = stackalloc char[64];
        var success = formattable.TryFormat(buffer, out var charsWritten, default, CultureInfo.InvariantCulture);

        Assert.True(success);
        Assert.Equal("(12,-34)", buffer[..charsWritten].ToString());
    }

    [Fact]
    public void TestExplicitConversionFromVector2()
    {
        var vector = new Vector2(3.7f, -2.9f);
        var point = (Point)vector;

        // Explicit conversion truncates towards zero.
        Assert.Equal(3, point.X);
        Assert.Equal(-2, point.Y);
    }

    [Fact]
    public void TestImplicitConversionToVector2()
    {
        var point = new Point(-8, 15);
        Vector2 vector = point;

        Assert.Equal(-8f, vector.X);
        Assert.Equal(15f, vector.Y);
    }

    [Fact]
    public void TestDeconstruct()
    {
        var point = new Point(-9, 42);
        var (x, y) = point;

        Assert.Equal(-9, x);
        Assert.Equal(42, y);
    }
}
