// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestInt3
{
    [Fact]
    public void TestInt3Constants()
    {
        Assert.Equal(0, Int3.Zero.X);
        Assert.Equal(0, Int3.Zero.Y);
        Assert.Equal(0, Int3.Zero.Z);

        Assert.Equal(1, Int3.One.X);
        Assert.Equal(1, Int3.One.Y);
        Assert.Equal(1, Int3.One.Z);

        Assert.Equal(1, Int3.UnitX.X);
        Assert.Equal(0, Int3.UnitX.Y);
        Assert.Equal(0, Int3.UnitX.Z);

        Assert.Equal(0, Int3.UnitY.X);
        Assert.Equal(1, Int3.UnitY.Y);
        Assert.Equal(0, Int3.UnitY.Z);

        Assert.Equal(0, Int3.UnitZ.X);
        Assert.Equal(0, Int3.UnitZ.Y);
        Assert.Equal(1, Int3.UnitZ.Z);
    }

    [Fact]
    public void TestInt3Constructors()
    {
        var v1 = new Int3(5);
        Assert.Equal(5, v1.X);
        Assert.Equal(5, v1.Y);
        Assert.Equal(5, v1.Z);

        var v2 = new Int3(3, 4, 5);
        Assert.Equal(3, v2.X);
        Assert.Equal(4, v2.Y);
        Assert.Equal(5, v2.Z);

        var v3 = new Int3(new Vector2(1, 2), 3);
        Assert.Equal(1, v3.X);
        Assert.Equal(2, v3.Y);
        Assert.Equal(3, v3.Z);

        var v4 = new Int3(new int[] { 7, 8, 9 });
        Assert.Equal(7, v4.X);
        Assert.Equal(8, v4.Y);
        Assert.Equal(9, v4.Z);
    }

    [Fact]
    public void TestInt3Length()
    {
        var v = new Int3(2, 3, 6);
        Assert.Equal(7.0f, v.Length());
    }

    [Fact]
    public void TestInt3LengthSquared()
    {
        var v = new Int3(2, 3, 6);
        Assert.Equal(49, v.LengthSquared());
    }

    [Fact]
    public void TestInt3Dot()
    {
        var v1 = new Int3(2, 3, 4);
        var v2 = new Int3(5, 6, 7);
        var dot = Int3.Dot(v1, v2);
        Assert.Equal(56, dot); // 2*5 + 3*6 + 4*7 = 56

        Int3.Dot(ref v1, ref v2, out var dot2);
        Assert.Equal(dot, dot2);
    }

    [Fact]
    public void TestInt3Add()
    {
        var v1 = new Int3(3, 4, 5);
        var v2 = new Int3(1, 2, 3);
        var result = v1 + v2;
        Assert.Equal(4, result.X);
        Assert.Equal(6, result.Y);
        Assert.Equal(8, result.Z);

        Int3.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Subtract()
    {
        var v1 = new Int3(5, 8, 10);
        var v2 = new Int3(2, 3, 4);
        var result = v1 - v2;
        Assert.Equal(3, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(6, result.Z);

        Int3.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Multiply()
    {
        var v = new Int3(3, 4, 5);
        var result = v * 2;
        Assert.Equal(6, result.X);
        Assert.Equal(8, result.Y);
        Assert.Equal(10, result.Z);

        var result2 = 2 * v;
        Assert.Equal(result, result2);

        Int3.Multiply(ref v, 2, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestInt3Divide()
    {
        var v = new Int3(10, 20, 30);
        var result = v / 2;
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);
        Assert.Equal(15, result.Z);

        Int3.Divide(ref v, 2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Negate()
    {
        var v = new Int3(3, -5, 7);
        var result = -v;
        Assert.Equal(-3, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(-7, result.Z);

        Int3.Negate(ref v, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Clamp()
    {
        var value = new Int3(-5, 5, 15);
        var min = new Int3(0, 0, 0);
        var max = new Int3(10, 10, 10);
        var result = Int3.Clamp(value, min, max);

        Assert.Equal(0, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(10, result.Z);

        Int3.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Min()
    {
        var v1 = new Int3(1, 5, 3);
        var v2 = new Int3(2, 3, 4);
        var result = Int3.Min(v1, v2);
        Assert.Equal(1, result.X);
        Assert.Equal(3, result.Y);
        Assert.Equal(3, result.Z);

        Int3.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Max()
    {
        var v1 = new Int3(1, 5, 3);
        var v2 = new Int3(2, 3, 4);
        var result = Int3.Max(v1, v2);
        Assert.Equal(2, result.X);
        Assert.Equal(5, result.Y);
        Assert.Equal(4, result.Z);

        Int3.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3Equality()
    {
        var v1 = new Int3(3, 4, 5);
        var v2 = new Int3(3, 4, 5);
        var v3 = new Int3(5, 6, 7);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestInt3GetHashCode()
    {
        var v1 = new Int3(1, 2, 3);
        var v2 = new Int3(1, 2, 3);
        var v3 = new Int3(4, 5, 6);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestInt3ToString()
    {
        var v = new Int3(1, 2, 3);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("3", str);
    }

    [Fact]
    public void TestInt3Indexer()
    {
        var v = new Int3(5, 10, 15);
        Assert.Equal(5, v[0]);
        Assert.Equal(10, v[1]);
        Assert.Equal(15, v[2]);

        v[0] = 20;
        v[1] = 25;
        v[2] = 30;
        Assert.Equal(20, v.X);
        Assert.Equal(25, v.Y);
        Assert.Equal(30, v.Z);
    }

    [Fact]
    public void TestInt3ToVector3()
    {
        var i3 = new Int3(3, 4, 5);
        var vec3 = (Vector3)i3;
        Assert.Equal(3.0f, vec3.X);
        Assert.Equal(4.0f, vec3.Y);
        Assert.Equal(5.0f, vec3.Z);
    }

    [Fact]
    public void TestInt3Modulate()
    {
        var v1 = new Int3(12, 20, 30);
        var v2 = new Int3(3, 4, 5);
        var result = Int3.Modulate(v1, v2);
        Assert.Equal(36, result.X);
        Assert.Equal(80, result.Y);
        Assert.Equal(150, result.Z);
    }

    [Fact]
    public void TestInt3Pow()
    {
        var v = new Int3(2, 3, 4);
        v.Pow(2);
        Assert.Equal(4, v.X);
        Assert.Equal(9, v.Y);
        Assert.Equal(16, v.Z);
    }

    [Fact]
    public void TestInt3Lerp()
    {
        var start = new Int3(0, 0, 0);
        var end = new Int3(10, 20, 30);
        var result = Int3.Lerp(start, end, 0.5f);
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);
        Assert.Equal(15, result.Z);
    }

    [Fact]
    public void TestInt3SmoothStep()
    {
        var start = new Int3(0, 0, 0);
        var end = new Int3(10, 20, 30);
        var result = Int3.SmoothStep(start, end, 0.5f);
        // SmoothStep at 0.5 should equal Lerp at 0.5 for integers
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);
        Assert.Equal(15, result.Z);
    }

    [Fact]
    public void TestInt3Deconstruct()
    {
        var v = new Int3(7, 8, 9);
        var (x, y, z) = v;
        Assert.Equal(7, x);
        Assert.Equal(8, y);
        Assert.Equal(9, z);
    }
}
