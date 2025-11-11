// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestInt2
{
    [Fact]
    public void TestInt2Constants()
    {
        Assert.Equal(0, Int2.Zero.X);
        Assert.Equal(0, Int2.Zero.Y);

        Assert.Equal(1, Int2.One.X);
        Assert.Equal(1, Int2.One.Y);

        Assert.Equal(1, Int2.UnitX.X);
        Assert.Equal(0, Int2.UnitX.Y);

        Assert.Equal(0, Int2.UnitY.X);
        Assert.Equal(1, Int2.UnitY.Y);
    }

    [Fact]
    public void TestInt2Constructors()
    {
        var v1 = new Int2(5);
        Assert.Equal(5, v1.X);
        Assert.Equal(5, v1.Y);

        var v2 = new Int2(3, 4);
        Assert.Equal(3, v2.X);
        Assert.Equal(4, v2.Y);

        var v3 = new Int2(new int[] { 1, 2 });
        Assert.Equal(1, v3.X);
        Assert.Equal(2, v3.Y);
    }

    [Fact]
    public void TestInt2Length()
    {
        var v = new Int2(3, 4);
        Assert.Equal(5.0f, v.Length());
    }

    [Fact]
    public void TestInt2LengthSquared()
    {
        var v = new Int2(3, 4);
        Assert.Equal(25, v.LengthSquared());
    }

    [Fact]
    public void TestInt2Dot()
    {
        var v1 = new Int2(2, 3);
        var v2 = new Int2(4, 5);
        var dot = Int2.Dot(v1, v2);
        Assert.Equal(23, dot); // 2*4 + 3*5 = 23

        Int2.Dot(ref v1, ref v2, out var dot2);
        Assert.Equal(dot, dot2);
    }

    [Fact]
    public void TestInt2Add()
    {
        var v1 = new Int2(3, 4);
        var v2 = new Int2(1, 2);
        var result = v1 + v2;
        Assert.Equal(4, result.X);
        Assert.Equal(6, result.Y);

        Int2.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Subtract()
    {
        var v1 = new Int2(5, 8);
        var v2 = new Int2(2, 3);
        var result = v1 - v2;
        Assert.Equal(3, result.X);
        Assert.Equal(5, result.Y);

        Int2.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Multiply()
    {
        var v = new Int2(3, 4);
        var result = v * 2;
        Assert.Equal(6, result.X);
        Assert.Equal(8, result.Y);

        var result2 = 2 * v;
        Assert.Equal(result, result2);

        Int2.Multiply(ref v, 2, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestInt2Divide()
    {
        var v = new Int2(10, 20);
        var result = v / 2;
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);

        Int2.Divide(ref v, 2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Negate()
    {
        var v = new Int2(3, -5);
        var result = -v;
        Assert.Equal(-3, result.X);
        Assert.Equal(5, result.Y);

        Int2.Negate(ref v, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Clamp()
    {
        var value = new Int2(-5, 15);
        var min = new Int2(0, 0);
        var max = new Int2(10, 10);
        var result = Int2.Clamp(value, min, max);

        Assert.Equal(0, result.X);
        Assert.Equal(10, result.Y);

        Int2.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Min()
    {
        var v1 = new Int2(1, 5);
        var v2 = new Int2(2, 3);
        var result = Int2.Min(v1, v2);
        Assert.Equal(1, result.X);
        Assert.Equal(3, result.Y);

        Int2.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Max()
    {
        var v1 = new Int2(1, 5);
        var v2 = new Int2(2, 3);
        var result = Int2.Max(v1, v2);
        Assert.Equal(2, result.X);
        Assert.Equal(5, result.Y);

        Int2.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt2Equality()
    {
        var v1 = new Int2(3, 4);
        var v2 = new Int2(3, 4);
        var v3 = new Int2(5, 6);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestInt2GetHashCode()
    {
        var v1 = new Int2(1, 2);
        var v2 = new Int2(1, 2);
        var v3 = new Int2(3, 4);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestInt2ToString()
    {
        var v = new Int2(1, 2);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
    }

    [Fact]
    public void TestInt2Indexer()
    {
        var v = new Int2(5, 10);
        Assert.Equal(5, v[0]);
        Assert.Equal(10, v[1]);

        v[0] = 15;
        v[1] = 20;
        Assert.Equal(15, v.X);
        Assert.Equal(20, v.Y);
    }

    [Fact]
    public void TestInt2ToVector2()
    {
        var i2 = new Int2(3, 4);
        var vec2 = (Vector2)i2;
        Assert.Equal(3.0f, vec2.X);
        Assert.Equal(4.0f, vec2.Y);
    }

    [Fact]
    public void TestInt2Modulate()
    {
        var v1 = new Int2(12, 20);
        var v2 = new Int2(3, 4);
        var result = Int2.Modulate(v1, v2);
        Assert.Equal(36, result.X);
        Assert.Equal(80, result.Y);
    }

    [Fact]
    public void TestInt2Pow()
    {
        var v = new Int2(2, 3);
        v.Pow(2);
        Assert.Equal(4, v.X);
        Assert.Equal(9, v.Y);
    }

    [Fact]
    public void TestInt2Lerp()
    {
        var start = new Int2(0, 0);
        var end = new Int2(10, 20);
        var result = Int2.Lerp(start, end, 0.5f);
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);
    }

    [Fact]
    public void TestInt2SmoothStep()
    {
        var start = new Int2(0, 0);
        var end = new Int2(10, 20);
        var result = Int2.SmoothStep(start, end, 0.5f);
        Assert.Equal(5, result.X);
        Assert.Equal(10, result.Y);
    }

    [Fact]
    public void TestInt2Deconstruct()
    {
        var v = new Int2(7, 8);
        var (x, y) = v;
        Assert.Equal(7, x);
        Assert.Equal(8, y);
    }
}

