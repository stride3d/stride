// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestUInt4
{
    [Fact]
    public void TestUInt4Constants()
    {
        Assert.Equal(0u, UInt4.Zero.X);
        Assert.Equal(0u, UInt4.Zero.Y);
        Assert.Equal(0u, UInt4.Zero.Z);
        Assert.Equal(0u, UInt4.Zero.W);

        Assert.Equal(1u, UInt4.One.X);
        Assert.Equal(1u, UInt4.One.Y);
        Assert.Equal(1u, UInt4.One.Z);
        Assert.Equal(1u, UInt4.One.W);

        Assert.Equal(1u, UInt4.UnitX.X);
        Assert.Equal(0u, UInt4.UnitX.Y);
        Assert.Equal(0u, UInt4.UnitX.Z);
        Assert.Equal(0u, UInt4.UnitX.W);

        Assert.Equal(0u, UInt4.UnitY.X);
        Assert.Equal(1u, UInt4.UnitY.Y);
        Assert.Equal(0u, UInt4.UnitY.Z);
        Assert.Equal(0u, UInt4.UnitY.W);

        Assert.Equal(0u, UInt4.UnitZ.X);
        Assert.Equal(0u, UInt4.UnitZ.Y);
        Assert.Equal(1u, UInt4.UnitZ.Z);
        Assert.Equal(0u, UInt4.UnitZ.W);

        Assert.Equal(0u, UInt4.UnitW.X);
        Assert.Equal(0u, UInt4.UnitW.Y);
        Assert.Equal(0u, UInt4.UnitW.Z);
        Assert.Equal(1u, UInt4.UnitW.W);
    }

    [Fact]
    public void TestUInt4Constructors()
    {
        var v1 = new UInt4(5u);
        Assert.Equal(5u, v1.X);
        Assert.Equal(5u, v1.Y);
        Assert.Equal(5u, v1.Z);
        Assert.Equal(5u, v1.W);

        var v2 = new UInt4(3u, 4u, 5u, 6u);
        Assert.Equal(3u, v2.X);
        Assert.Equal(4u, v2.Y);
        Assert.Equal(5u, v2.Z);
        Assert.Equal(6u, v2.W);

        var v3 = new UInt4(new uint[] { 9u, 10u, 11u, 12u });
        Assert.Equal(9u, v3.X);
        Assert.Equal(10u, v3.Y);
        Assert.Equal(11u, v3.Z);
        Assert.Equal(12u, v3.W);
    }

    [Fact]
    public void TestUInt4Add()
    {
        var v1 = new UInt4(3u, 4u, 5u, 6u);
        var v2 = new UInt4(1u, 2u, 3u, 4u);
        var result = v1 + v2;
        Assert.Equal(4u, result.X);
        Assert.Equal(6u, result.Y);
        Assert.Equal(8u, result.Z);
        Assert.Equal(10u, result.W);

        UInt4.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt4Subtract()
    {
        var v1 = new UInt4(5u, 8u, 10u, 12u);
        var v2 = new UInt4(2u, 3u, 4u, 5u);
        var result = v1 - v2;
        Assert.Equal(3u, result.X);
        Assert.Equal(5u, result.Y);
        Assert.Equal(6u, result.Z);
        Assert.Equal(7u, result.W);

        UInt4.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt4Multiply()
    {
        var v = new UInt4(3u, 4u, 5u, 6u);
        var result = v * 2u;
        Assert.Equal(6u, result.X);
        Assert.Equal(8u, result.Y);
        Assert.Equal(10u, result.Z);
        Assert.Equal(12u, result.W);

        var result2 = 2u * v;
        Assert.Equal(result, result2);

        UInt4.Multiply(ref v, 2u, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestUInt4Divide()
    {
        var v = new UInt4(10u, 20u, 30u, 40u);
        var result = v / 2u;
        Assert.Equal(5u, result.X);
        Assert.Equal(10u, result.Y);
        Assert.Equal(15u, result.Z);
        Assert.Equal(20u, result.W);

        UInt4.Divide(ref v, 2u, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt4Clamp()
    {
        var value = new UInt4(5u, 15u, 25u, 35u);
        var min = new UInt4(10u, 10u, 10u, 10u);
        var max = new UInt4(20u, 20u, 20u, 20u);
        var result = UInt4.Clamp(value, min, max);

        Assert.Equal(10u, result.X);
        Assert.Equal(15u, result.Y);
        Assert.Equal(20u, result.Z);
        Assert.Equal(20u, result.W);

        UInt4.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt4Min()
    {
        var v1 = new UInt4(1u, 5u, 3u, 8u);
        var v2 = new UInt4(2u, 3u, 4u, 7u);
        var result = UInt4.Min(v1, v2);
        Assert.Equal(1u, result.X);
        Assert.Equal(3u, result.Y);
        Assert.Equal(3u, result.Z);
        Assert.Equal(7u, result.W);

        UInt4.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt4Max()
    {
        var v1 = new UInt4(1u, 5u, 3u, 8u);
        var v2 = new UInt4(2u, 3u, 4u, 7u);
        var result = UInt4.Max(v1, v2);
        Assert.Equal(2u, result.X);
        Assert.Equal(5u, result.Y);
        Assert.Equal(4u, result.Z);
        Assert.Equal(8u, result.W);

        UInt4.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt4Equality()
    {
        var v1 = new UInt4(3u, 4u, 5u, 6u);
        var v2 = new UInt4(3u, 4u, 5u, 6u);
        var v3 = new UInt4(5u, 6u, 7u, 8u);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestUInt4GetHashCode()
    {
        var v1 = new UInt4(1u, 2u, 3u, 4u);
        var v2 = new UInt4(1u, 2u, 3u, 4u);
        var v3 = new UInt4(5u, 6u, 7u, 8u);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestUInt4ToString()
    {
        var v = new UInt4(1u, 2u, 3u, 4u);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("3", str);
        Assert.Contains("4", str);
    }

    [Fact]
    public void TestUInt4Indexer()
    {
        var v = new UInt4(5u, 10u, 15u, 20u);
        Assert.Equal(5u, v[0]);
        Assert.Equal(10u, v[1]);
        Assert.Equal(15u, v[2]);
        Assert.Equal(20u, v[3]);

        v[0] = 25u;
        v[1] = 30u;
        v[2] = 35u;
        v[3] = 40u;
        Assert.Equal(25u, v.X);
        Assert.Equal(30u, v.Y);
        Assert.Equal(35u, v.Z);
        Assert.Equal(40u, v.W);
    }

    [Fact]
    public void TestUInt4ToVector4()
    {
        var u4 = new UInt4(3u, 4u, 5u, 6u);
        var vec4 = (Vector4)u4;
        Assert.Equal(3.0f, vec4.X);
        Assert.Equal(4.0f, vec4.Y);
        Assert.Equal(5.0f, vec4.Z);
        Assert.Equal(6.0f, vec4.W);
    }
}
