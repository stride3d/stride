// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSize2
{
    [Fact]
    public void TestSize2Construction()
    {
        var size = new Size2(800, 600);
        Assert.Equal(800, size.Width);
        Assert.Equal(600, size.Height);
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
    }

    [Fact]
    public void TestSize2Equality()
    {
        var size1 = new Size2(800, 600);
        var size2 = new Size2(800, 600);
        var size3 = new Size2(1024, 768);

        Assert.Equal(size1, size2);
        Assert.NotEqual(size1, size3);
        Assert.True(size1 == size2);
        Assert.True(size1 != size3);
        Assert.True(size1.Equals(size2));
        Assert.False(size1.Equals(size3));
    }

    [Fact]
    public void TestSize2GetHashCode()
    {
        var size1 = new Size2(800, 600);
        var size2 = new Size2(800, 600);
        Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
    }

    [Fact]
    public void TestSize2ToString()
    {
        var size = new Size2(800, 600);
        var str = size.ToString();
        Assert.NotNull(str);
        Assert.Contains("800", str);
        Assert.Contains("600", str);
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
