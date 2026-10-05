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

    [Fact]
    public void TestInt3SizeInBytes()
    {
        Assert.Equal(12, Int3.SizeInBytes);
    }

    [Fact]
    public void TestInt3LengthUntruncated()
    {
        var v = new Int3(2, 3, 4);
        Assert.Equal(MathF.Sqrt(4 + 9 + 16), v.LengthUntruncated());
    }

    [Fact]
    public void TestInt3ToArray()
    {
        var v = new Int3(-7, 13, -21);
        var arr = v.ToArray();
        Assert.Equal(3, arr.Length);
        Assert.Equal(-7, arr[0]);
        Assert.Equal(13, arr[1]);
        Assert.Equal(-21, arr[2]);
    }

    [Fact]
    public void TestInt3AddSubtractStaticValueOverloads()
    {
        var v1 = new Int3(-6, 9, 2);
        var v2 = new Int3(4, -11, 5);

        var sum = Int3.Add(v1, v2);
        Assert.Equal(-2, sum.X);
        Assert.Equal(-2, sum.Y);
        Assert.Equal(7, sum.Z);

        var diff = Int3.Subtract(v1, v2);
        Assert.Equal(-10, diff.X);
        Assert.Equal(20, diff.Y);
        Assert.Equal(-3, diff.Z);
    }

    [Fact]
    public void TestInt3UnaryPlus()
    {
        var v = new Int3(-4, 9, -1);
        var result = +v;
        Assert.Equal(v, result);
    }

    [Fact]
    public void TestInt3NegateStaticValueOverload()
    {
        var v = new Int3(-13, 21, 0);
        var result = Int3.Negate(v);
        Assert.Equal(13, result.X);
        Assert.Equal(-21, result.Y);
        Assert.Equal(0, result.Z);
    }

    [Fact]
    public void TestInt3ModulateRef()
    {
        var v1 = new Int3(-6, 7, 3);
        var v2 = new Int3(3, -4, -5);
        var result = Int3.Modulate(v1, v2);
        Assert.Equal(-18, result.X);
        Assert.Equal(-28, result.Y);
        Assert.Equal(-15, result.Z);

        Int3.Modulate(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3LerpRefAndNonHalfAmount()
    {
        var start = new Int3(-10, 4, 8);
        var end = new Int3(30, -16, -8);

        var result = Int3.Lerp(start, end, 0.25f);
        Assert.Equal(0, result.X);
        Assert.Equal(-1, result.Y);
        Assert.Equal(4, result.Z);

        Int3.Lerp(ref start, ref end, 0.25f, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3SmoothStepRefAndClampedAmount()
    {
        var start = new Int3(-10, 4, 8);
        var end = new Int3(30, -16, -8);

        // Amount outside [0, 1] should be clamped.
        var below = Int3.SmoothStep(start, end, -1f);
        Assert.Equal(start, below);

        var above = Int3.SmoothStep(start, end, 2f);
        Assert.Equal(end, above);

        Int3.SmoothStep(ref start, ref end, 0.25f, out var resultRef);
        Assert.Equal(Int3.SmoothStep(start, end, 0.25f), resultRef);
    }

    [Fact]
    public void TestInt3RoundFromVector3()
    {
        var v = new Vector3(2.5f, -2.5f, 1.4f);

        var result = Int3.Round(v, MidpointRounding.AwayFromZero);
        Assert.Equal(3, result.X);
        Assert.Equal(-3, result.Y);
        Assert.Equal(1, result.Z);

        Int3.Round(in v, out var result2, MidpointRounding.AwayFromZero);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestInt3EqualsObject()
    {
        var v1 = new Int3(-3, 8, 5);
        object v2 = new Int3(-3, 8, 5);
        object v3 = new Int3(1, 2, 3);
        object notInt3 = "not an Int3";

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
        Assert.False(v1.Equals(notInt3));
        Assert.False(v1.Equals(null));
    }

    [Fact]
    public void TestInt3ExplicitConversions()
    {
        var v = new Int3(-6, 11, 4);

        var vec2 = (Vector2)v;
        Assert.Equal(-6.0f, vec2.X);
        Assert.Equal(11.0f, vec2.Y);

        var vec4 = (Vector4)v;
        Assert.Equal(-6.0f, vec4.X);
        Assert.Equal(11.0f, vec4.Y);
        Assert.Equal(4.0f, vec4.Z);
        Assert.Equal(0.0f, vec4.W);
    }

    [Fact]
    public void TestInt3SystemNumericsVector3Conversion()
    {
        var sysVec = new System.Numerics.Vector3(-2.9f, 7.4f, 3.6f);
        var v = (Int3)sysVec;
        Assert.Equal(-2, v.X);
        Assert.Equal(7, v.Y);
        Assert.Equal(3, v.Z);

        var backToSys = (System.Numerics.Vector3)v;
        Assert.Equal(-2.0f, backToSys.X);
        Assert.Equal(7.0f, backToSys.Y);
        Assert.Equal(3.0f, backToSys.Z);
    }
}
