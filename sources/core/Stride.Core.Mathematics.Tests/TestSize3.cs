// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSize3
{
    [Fact]
    public void TestSize3Construction()
    {
        var size = new Size3(800, 600, 32);
        Assert.Equal(800, size.Width);
        Assert.Equal(600, size.Height);
        Assert.Equal(32, size.Depth);
    }

    [Fact]
    public void TestSize3Zero()
    {
        var zero = Size3.Zero;
        Assert.Equal(0, zero.Width);
        Assert.Equal(0, zero.Height);
        Assert.Equal(0, zero.Depth);
    }

    [Fact]
    public void TestSize3One()
    {
        var one = Size3.One;
        Assert.Equal(1, one.Width);
        Assert.Equal(1, one.Height);
        Assert.Equal(1, one.Depth);
    }

    [Fact]
    public void TestSize3Empty()
    {
        var empty = Size3.Empty;
        Assert.Equal(0, empty.Width);
        Assert.Equal(0, empty.Height);
        Assert.Equal(0, empty.Depth);
    }

    [Fact]
    public void TestSize3Equality()
    {
        var size1 = new Size3(800, 600, 32);
        var size2 = new Size3(800, 600, 32);
        var size3 = new Size3(1024, 768, 64);

        Assert.Equal(size1, size2);
        Assert.NotEqual(size1, size3);
        Assert.True(size1 == size2);
        Assert.True(size1 != size3);
        Assert.True(size1.Equals(size2));
        Assert.False(size1.Equals(size3));
    }

    [Fact]
    public void TestSize3GetHashCode()
    {
        var size1 = new Size3(800, 600, 32);
        var size2 = new Size3(800, 600, 32);
        Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
    }

    [Fact]
    public void TestSize3CompareTo()
    {
        var size1 = new Size3(10, 10, 10);  // Volume = 1000
        var size2 = new Size3(5, 5, 5);     // Volume = 125
        var size3 = new Size3(10, 10, 10);  // Volume = 1000

        Assert.True(size1.CompareTo(size2) > 0);
        Assert.True(size2.CompareTo(size1) < 0);
        Assert.Equal(0, size1.CompareTo(size3));
    }

    [Fact]
    public void TestSize3ComparisonOperators()
    {
        var size1 = new Size3(10, 10, 10);  // Volume = 1000
        var size2 = new Size3(5, 5, 5);     // Volume = 125
        var size3 = new Size3(10, 10, 10);  // Volume = 1000

        Assert.True(size1 > size2);
        Assert.True(size1 >= size2);
        Assert.True(size1 >= size3);
        Assert.True(size2 < size1);
        Assert.True(size2 <= size1);
        Assert.True(size1 <= size3);
    }

    [Fact]
    public void TestSize3Up2()
    {
        var size = new Size3(10, 20, 30);
        var result = size.Up2();
        Assert.Equal(20, result.Width);
        Assert.Equal(40, result.Height);
        Assert.Equal(60, result.Depth);
    }

    [Fact]
    public void TestSize3Up2MultipleSteps()
    {
        var size = new Size3(10, 20, 30);
        var result = size.Up2(2);
        Assert.Equal(40, result.Width);
        Assert.Equal(80, result.Height);
        Assert.Equal(120, result.Depth);
    }

    [Fact]
    public void TestSize3Up2WithZeroDimensions()
    {
        var size = new Size3(0, 0, 0);
        var result = size.Up2();
        Assert.Equal(1, result.Width);
        Assert.Equal(1, result.Height);
        Assert.Equal(1, result.Depth);
    }

    [Fact]
    public void TestSize3Down2()
    {
        var size = new Size3(40, 80, 120);
        var result = size.Down2();
        Assert.Equal(20, result.Width);
        Assert.Equal(40, result.Height);
        Assert.Equal(60, result.Depth);
    }

    [Fact]
    public void TestSize3Down2MultipleSteps()
    {
        var size = new Size3(40, 80, 120);
        var result = size.Down2(2);
        Assert.Equal(10, result.Width);
        Assert.Equal(20, result.Height);
        Assert.Equal(30, result.Depth);
    }

    [Fact]
    public void TestSize3Down2MinimumOne()
    {
        var size = new Size3(1, 2, 4);
        var result = size.Down2(3);
        Assert.Equal(1, result.Width);  // Can't go below 1
        Assert.Equal(1, result.Height); // Can't go below 1
        Assert.Equal(1, result.Depth);  // 4 >> 3 = 0, but clamped to 1
    }

    [Fact]
    public void TestSize3Up2ThrowsOnNegativeCount()
    {
        var size = new Size3(10, 20, 30);
        Assert.Throws<ArgumentOutOfRangeException>(() => size.Up2(-1));
    }

    [Fact]
    public void TestSize3Down2ThrowsOnNegativeCount()
    {
        var size = new Size3(10, 20, 30);
        Assert.Throws<ArgumentOutOfRangeException>(() => size.Down2(-1));
    }

    [Fact]
    public void TestSize3Mip()
    {
        var size = new Size3(10, 20, 30);

        // Mip with direction = 0 returns unchanged
        var unchanged = size.Mip(0);
        Assert.Equal(size, unchanged);

        // Mip with direction < 0 goes down
        var down = size.Mip(-1);
        Assert.Equal(size.Down2(), down);

        // Mip with direction > 0 goes up
        var up = size.Mip(1);
        Assert.Equal(size.Up2(), up);
    }

    [Fact]
    public void TestSize3ToString()
    {
        var size = new Size3(800, 600, 32);
        var str = size.ToString();
        Assert.NotNull(str);
        Assert.Contains("800", str);
        Assert.Contains("600", str);
        Assert.Contains("32", str);
    }

    [Fact]
    public void TestSize3Deconstruct()
    {
        var size = new Size3(800, 600, 32);
        var (width, height, depth) = size;
        Assert.Equal(800, width);
        Assert.Equal(600, height);
        Assert.Equal(32, depth);
    }
}
