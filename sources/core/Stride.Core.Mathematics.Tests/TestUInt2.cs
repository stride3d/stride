// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestUInt2
{
    [Fact]
    public void TestUInt2Constants()
    {
        Assert.Equal(0u, UInt2.Zero.X);
        Assert.Equal(0u, UInt2.Zero.Y);

        Assert.Equal(1u, UInt2.One.X);
        Assert.Equal(1u, UInt2.One.Y);

        Assert.Equal(1u, UInt2.UnitX.X);
        Assert.Equal(0u, UInt2.UnitX.Y);

        Assert.Equal(0u, UInt2.UnitY.X);
        Assert.Equal(1u, UInt2.UnitY.Y);

        Assert.Equal(8, UInt2.SizeInBytes);
    }

    [Fact]
    public void TestUInt2Constructors()
    {
        var v1 = new UInt2(5u);
        Assert.Equal(5u, v1.X);
        Assert.Equal(5u, v1.Y);

        var v2 = new UInt2(3u, 4u);
        Assert.Equal(3u, v2.X);
        Assert.Equal(4u, v2.Y);

        var v3 = new UInt2(new uint[] { 9u, 10u });
        Assert.Equal(9u, v3.X);
        Assert.Equal(10u, v3.Y);
    }

    [Fact]
    public void TestUInt2Add()
    {
        var v1 = new UInt2(3u, 4u);
        var v2 = new UInt2(1u, 2u);
        var result = v1 + v2;
        Assert.Equal(4u, result.X);
        Assert.Equal(6u, result.Y);

        UInt2.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt2Subtract()
    {
        var v1 = new UInt2(5u, 8u);
        var v2 = new UInt2(2u, 3u);
        var result = v1 - v2;
        Assert.Equal(3u, result.X);
        Assert.Equal(5u, result.Y);

        UInt2.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt2Multiply()
    {
        var v = new UInt2(3u, 4u);
        var result = v * 2u;
        Assert.Equal(6u, result.X);
        Assert.Equal(8u, result.Y);

        var result2 = 2u * v;
        Assert.Equal(result, result2);

        UInt2.Multiply(ref v, 2u, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestUInt2Divide()
    {
        var v = new UInt2(10u, 20u);
        var result = v / 2u;
        Assert.Equal(5u, result.X);
        Assert.Equal(10u, result.Y);

        UInt2.Divide(ref v, 2u, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt2Clamp()
    {
        var value = new UInt2(5u, 25u);
        var min = new UInt2(10u, 10u);
        var max = new UInt2(20u, 20u);
        var result = UInt2.Clamp(value, min, max);

        Assert.Equal(10u, result.X);
        Assert.Equal(20u, result.Y);

        UInt2.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt2Min()
    {
        var v1 = new UInt2(1u, 5u);
        var v2 = new UInt2(2u, 3u);
        var result = UInt2.Min(v1, v2);
        Assert.Equal(1u, result.X);
        Assert.Equal(3u, result.Y);

        UInt2.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt2Max()
    {
        var v1 = new UInt2(1u, 5u);
        var v2 = new UInt2(2u, 3u);
        var result = UInt2.Max(v1, v2);
        Assert.Equal(2u, result.X);
        Assert.Equal(5u, result.Y);

        UInt2.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt2Equality()
    {
        var v1 = new UInt2(3u, 4u);
        var v2 = new UInt2(3u, 4u);
        var v3 = new UInt2(5u, 6u);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestUInt2GetHashCode()
    {
        var v1 = new UInt2(1u, 2u);
        var v2 = new UInt2(1u, 2u);
        var v3 = new UInt2(5u, 6u);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestUInt2Indexer()
    {
        var v = new UInt2(5u, 10u);
        Assert.Equal(5u, v[0]);
        Assert.Equal(10u, v[1]);

        v[0] = 25u;
        v[1] = 30u;
        Assert.Equal(25u, v.X);
        Assert.Equal(30u, v.Y);
    }

    [Fact]
    public void TestUInt2ToVector2()
    {
        var u2 = new UInt2(3u, 4u);
        var vec2 = (Vector2)u2;
        Assert.Equal(3.0f, vec2.X);
        Assert.Equal(4.0f, vec2.Y);
    }

    [Fact]
    public void TestUInt2Deconstruct()
    {
        var (x, y) = new UInt2(3u, 4u);
        Assert.Equal(3u, x);
        Assert.Equal(4u, y);
    }
}
