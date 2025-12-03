// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestSize2F
{
    [Fact]
    public void TestSize2FConstruction()
    {
        var size = new Size2F(800.5f, 600.5f);
        Assert.Equal(800.5f, size.Width);
        Assert.Equal(600.5f, size.Height);
    }

    [Fact]
    public void TestSize2FZero()
    {
        var zero = Size2F.Zero;
        Assert.Equal(0f, zero.Width);
        Assert.Equal(0f, zero.Height);
    }

    [Fact]
    public void TestSize2FEquality()
    {
        var size1 = new Size2F(800.5f, 600.5f);
        var size2 = new Size2F(800.5f, 600.5f);
        var size3 = new Size2F(1024.5f, 768.5f);

        Assert.Equal(size1, size2);
        Assert.NotEqual(size1, size3);
        Assert.True(size1 == size2);
        Assert.True(size1 != size3);
        Assert.True(size1.Equals(size2));
        Assert.False(size1.Equals(size3));
    }

    [Fact]
    public void TestSize2FGetHashCode()
    {
        var size1 = new Size2F(800.5f, 600.5f);
        var size2 = new Size2F(800.5f, 600.5f);
        Assert.Equal(size1.GetHashCode(), size2.GetHashCode());
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
    public void TestSize2FDeconstruct()
    {
        var size = new Size2F(800.5f, 600.5f);
        var (width, height) = size;
        Assert.Equal(800.5f, width);
        Assert.Equal(600.5f, height);
    }
}
