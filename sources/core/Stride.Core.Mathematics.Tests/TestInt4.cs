// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestInt4
{
    [Fact]
    public void TestInt4Constants()
    {
        Assert.Equal(0, Int4.Zero.X);
        Assert.Equal(0, Int4.Zero.Y);
        Assert.Equal(0, Int4.Zero.Z);
        Assert.Equal(0, Int4.Zero.W);

        Assert.Equal(1, Int4.One.X);
        Assert.Equal(1, Int4.One.Y);
        Assert.Equal(1, Int4.One.Z);
        Assert.Equal(1, Int4.One.W);

        Assert.Equal(1, Int4.UnitX.X);
        Assert.Equal(0, Int4.UnitX.Y);

        Assert.Equal(0, Int4.UnitY.X);
        Assert.Equal(1, Int4.UnitY.Y);

        Assert.Equal(0, Int4.UnitZ.X);
        Assert.Equal(1, Int4.UnitZ.Z);

        Assert.Equal(0, Int4.UnitW.X);
        Assert.Equal(1, Int4.UnitW.W);
    }

    [Fact]
    public void TestInt4Constructors()
    {
        var v1 = new Int4(5);
        Assert.Equal(5, v1.X);
        Assert.Equal(5, v1.Y);
        Assert.Equal(5, v1.Z);
        Assert.Equal(5, v1.W);

        var v2 = new Int4(3, 4, 5, 6);
        Assert.Equal(3, v2.X);
        Assert.Equal(4, v2.Y);
        Assert.Equal(5, v2.Z);
        Assert.Equal(6, v2.W);

        var v3 = new Int4(new int[] { 9, 10, 11, 12 });
        Assert.Equal(9, v3.X);
        Assert.Equal(10, v3.Y);
        Assert.Equal(11, v3.Z);
        Assert.Equal(12, v3.W);
    }

    [Fact]
    public void TestInt4Length()
    {
        var v = new Int4(2, 3, 6, 0);
        Assert.Equal(7.0f, v.Length());
    }

    [Fact]
    public void TestInt4LengthSquared()
    {
        var v = new Int4(2, 3, 6, 0);
        Assert.Equal(49, v.LengthSquared());
    }

    [Fact]
    public void TestInt4Add()
    {
        var v1 = new Int4(3, 4, 5, 6);
        var v2 = new Int4(1, 2, 3, 4);
        var result = v1 + v2;
        Assert.Equal(4, result.X);
        Assert.Equal(6, result.Y);
        Assert.Equal(8, result.Z);
        Assert.Equal(10, result.W);

        Int4.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Subtract()
    {
        var v1 = new Int4(5, 8, 10, 12);
        var v2 = new Int4(2, 3, 4, 5);
        var result = v1 - v2;
        Assert.Equal(3, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(6, result.Z);
        Assert.Equal(7, result.W);

        Int4.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Multiply()
    {
        var v = new Int4(3, 4, 5, 6);
        var result = v * 2;
        Assert.Equal(6, result.X);
        Assert.Equal(8, result.Y);
        Assert.Equal(10, result.Z);
        Assert.Equal(12, result.W);

        var result2 = 2 * v;
        Assert.Equal(result, result2);

        Int4.Multiply(ref v, 2, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestInt4Divide()
    {
        var v = new Int4(10, 20, 30, 40);
        var result = v / 2;
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);
        Assert.Equal(15, result.Z);
        Assert.Equal(20, result.W);

        Int4.Divide(ref v, 2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Negate()
    {
        var v = new Int4(3, -5, 7, -9);
        var result = -v;
        Assert.Equal(-3, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(-7, result.Z);
        Assert.Equal(9, result.W);

        Int4.Negate(ref v, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Clamp()
    {
        var value = new Int4(-5, 5, 15, 25);
        var min = new Int4(0, 0, 0, 0);
        var max = new Int4(10, 10, 10, 10);
        var result = Int4.Clamp(value, min, max);

        Assert.Equal(0, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(10, result.Z);
        Assert.Equal(10, result.W);

        Int4.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Min()
    {
        var v1 = new Int4(1, 5, 3, 8);
        var v2 = new Int4(2, 3, 4, 7);
        var result = Int4.Min(v1, v2);
        Assert.Equal(1, result.X);
        Assert.Equal(3, result.Y);
        Assert.Equal(3, result.Z);
        Assert.Equal(7, result.W);

        Int4.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Max()
    {
        var v1 = new Int4(1, 5, 3, 8);
        var v2 = new Int4(2, 3, 4, 7);
        var result = Int4.Max(v1, v2);
        Assert.Equal(2, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(4, result.Z);
        Assert.Equal(8, result.W);

        Int4.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt4Equality()
    {
        var v1 = new Int4(3, 4, 5, 6);
        var v2 = new Int4(3, 4, 5, 6);
        var v3 = new Int4(5, 6, 7, 8);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestInt4GetHashCode()
    {
        var v1 = new Int4(1, 2, 3, 4);
        var v2 = new Int4(1, 2, 3, 4);
        var v3 = new Int4(5, 6, 7, 8);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestInt4ToString()
    {
        var v = new Int4(1, 2, 3, 4);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("3", str);
        Assert.Contains("4", str);
    }

    [Fact]
    public void TestInt4Indexer()
    {
        var v = new Int4(5, 10, 15, 20);
        Assert.Equal(5, v[0]);
        Assert.Equal(10, v[1]);
        Assert.Equal(15, v[2]);
        Assert.Equal(20, v[3]);

        v[0] = 25;
        v[1] = 30;
        v[2] = 35;
        v[3] = 40;
        Assert.Equal(25, v.X);
        Assert.Equal(30, v.Y);
        Assert.Equal(35, v.Z);
        Assert.Equal(40, v.W);
    }

    [Fact]
    public void TestInt4ToVector4()
    {
        var i4 = new Int4(3, 4, 5, 6);
        var vec4 = (Vector4)i4;
        Assert.Equal(3.0f, vec4.X);
        Assert.Equal(4.0f, vec4.Y);
        Assert.Equal(5.0f, vec4.Z);
        Assert.Equal(6.0f, vec4.W);
    }
}
