// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestUInt3
{
    [Fact]
    public void TestUInt3Constants()
    {
        Assert.Equal(0u, UInt3.Zero.X);
        Assert.Equal(0u, UInt3.Zero.Y);
        Assert.Equal(0u, UInt3.Zero.Z);

        Assert.Equal(1u, UInt3.One.X);
        Assert.Equal(1u, UInt3.One.Y);
        Assert.Equal(1u, UInt3.One.Z);

        Assert.Equal(1u, UInt3.UnitX.X);
        Assert.Equal(0u, UInt3.UnitX.Y);
        Assert.Equal(0u, UInt3.UnitX.Z);

        Assert.Equal(0u, UInt3.UnitY.X);
        Assert.Equal(1u, UInt3.UnitY.Y);
        Assert.Equal(0u, UInt3.UnitY.Z);

        Assert.Equal(0u, UInt3.UnitZ.X);
        Assert.Equal(0u, UInt3.UnitZ.Y);
        Assert.Equal(1u, UInt3.UnitZ.Z);

        Assert.Equal(12, UInt3.SizeInBytes);
    }

    [Fact]
    public void TestUInt3Constructors()
    {
        var v1 = new UInt3(5u);
        Assert.Equal(5u, v1.X);
        Assert.Equal(5u, v1.Y);
        Assert.Equal(5u, v1.Z);

        var v2 = new UInt3(3u, 4u, 5u);
        Assert.Equal(3u, v2.X);
        Assert.Equal(4u, v2.Y);
        Assert.Equal(5u, v2.Z);

        var v3 = new UInt3(new uint[] { 9u, 10u, 11u });
        Assert.Equal(9u, v3.X);
        Assert.Equal(10u, v3.Y);
        Assert.Equal(11u, v3.Z);
    }

    [Fact]
    public void TestUInt3Add()
    {
        var v1 = new UInt3(3u, 4u, 5u);
        var v2 = new UInt3(1u, 2u, 3u);
        var result = v1 + v2;
        Assert.Equal(4u, result.X);
        Assert.Equal(6u, result.Y);
        Assert.Equal(8u, result.Z);

        UInt3.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt3Subtract()
    {
        var v1 = new UInt3(5u, 8u, 10u);
        var v2 = new UInt3(2u, 3u, 4u);
        var result = v1 - v2;
        Assert.Equal(3u, result.X);
        Assert.Equal(5u, result.Y);
        Assert.Equal(6u, result.Z);

        UInt3.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt3Multiply()
    {
        var v = new UInt3(3u, 4u, 5u);
        var result = v * 2u;
        Assert.Equal(6u, result.X);
        Assert.Equal(8u, result.Y);
        Assert.Equal(10u, result.Z);

        var result2 = 2u * v;
        Assert.Equal(result, result2);

        UInt3.Multiply(ref v, 2u, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestUInt3Divide()
    {
        var v = new UInt3(10u, 20u, 30u);
        var result = v / 2u;
        Assert.Equal(5u, result.X);
        Assert.Equal(10u, result.Y);
        Assert.Equal(15u, result.Z);

        UInt3.Divide(ref v, 2u, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt3Clamp()
    {
        var value = new UInt3(5u, 15u, 25u);
        var min = new UInt3(10u, 10u, 10u);
        var max = new UInt3(20u, 20u, 20u);
        var result = UInt3.Clamp(value, min, max);

        Assert.Equal(10u, result.X);
        Assert.Equal(15u, result.Y);
        Assert.Equal(20u, result.Z);

        UInt3.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt3Min()
    {
        var v1 = new UInt3(1u, 5u, 3u);
        var v2 = new UInt3(2u, 3u, 4u);
        var result = UInt3.Min(v1, v2);
        Assert.Equal(1u, result.X);
        Assert.Equal(3u, result.Y);
        Assert.Equal(3u, result.Z);

        UInt3.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt3Max()
    {
        var v1 = new UInt3(1u, 5u, 3u);
        var v2 = new UInt3(2u, 3u, 4u);
        var result = UInt3.Max(v1, v2);
        Assert.Equal(2u, result.X);
        Assert.Equal(5u, result.Y);
        Assert.Equal(4u, result.Z);

        UInt3.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestUInt3Equality()
    {
        var v1 = new UInt3(3u, 4u, 5u);
        var v2 = new UInt3(3u, 4u, 5u);
        var v3 = new UInt3(5u, 6u, 7u);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestUInt3GetHashCode()
    {
        var v1 = new UInt3(1u, 2u, 3u);
        var v2 = new UInt3(1u, 2u, 3u);
        var v3 = new UInt3(5u, 6u, 7u);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestUInt3Indexer()
    {
        var v = new UInt3(5u, 10u, 15u);
        Assert.Equal(5u, v[0]);
        Assert.Equal(10u, v[1]);
        Assert.Equal(15u, v[2]);

        v[0] = 25u;
        v[1] = 30u;
        v[2] = 35u;
        Assert.Equal(25u, v.X);
        Assert.Equal(30u, v.Y);
        Assert.Equal(35u, v.Z);
    }

    [Fact]
    public void TestUInt3ToVector3()
    {
        var u3 = new UInt3(3u, 4u, 5u);
        var vec3 = (Vector3)u3;
        Assert.Equal(3.0f, vec3.X);
        Assert.Equal(4.0f, vec3.Y);
        Assert.Equal(5.0f, vec3.Z);
    }

    [Fact]
    public void TestUInt3Deconstruct()
    {
        var (x, y, z) = new UInt3(3u, 4u, 5u);
        Assert.Equal(3u, x);
        Assert.Equal(4u, y);
        Assert.Equal(5u, z);
    }
}
