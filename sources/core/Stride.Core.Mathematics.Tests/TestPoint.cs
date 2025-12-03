// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

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

        Assert.Equal(point1, point2);
        Assert.NotEqual(point1, point3);
        Assert.True(point1 == point2);
        Assert.True(point1 != point3);
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

        Assert.Equal(point1.GetHashCode(), point2.GetHashCode());
        Assert.NotEqual(point1.GetHashCode(), point3.GetHashCode());
    }
}