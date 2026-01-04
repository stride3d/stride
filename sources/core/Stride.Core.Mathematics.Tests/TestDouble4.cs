// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestDouble4
{
    [Fact]
    public void TestDouble4Constants()
    {
        Assert.Equal(0.0, Double4.Zero.X);
        Assert.Equal(0.0, Double4.Zero.Y);
        Assert.Equal(0.0, Double4.Zero.Z);
        Assert.Equal(0.0, Double4.Zero.W);

        Assert.Equal(1.0, Double4.One.X);
        Assert.Equal(1.0, Double4.One.Y);
        Assert.Equal(1.0, Double4.One.Z);
        Assert.Equal(1.0, Double4.One.W);

        Assert.Equal(1.0, Double4.UnitX.X);
        Assert.Equal(0.0, Double4.UnitX.Y);
        Assert.Equal(0.0, Double4.UnitX.Z);
        Assert.Equal(0.0, Double4.UnitX.W);

        Assert.Equal(0.0, Double4.UnitY.X);
        Assert.Equal(1.0, Double4.UnitY.Y);
        Assert.Equal(0.0, Double4.UnitY.Z);
        Assert.Equal(0.0, Double4.UnitY.W);

        Assert.Equal(0.0, Double4.UnitZ.X);
        Assert.Equal(0.0, Double4.UnitZ.Y);
        Assert.Equal(1.0, Double4.UnitZ.Z);
        Assert.Equal(0.0, Double4.UnitZ.W);

        Assert.Equal(0.0, Double4.UnitW.X);
        Assert.Equal(0.0, Double4.UnitW.Y);
        Assert.Equal(0.0, Double4.UnitW.Z);
        Assert.Equal(1.0, Double4.UnitW.W);
    }

    [Fact]
    public void TestDouble4Constructors()
    {
        var v1 = new Double4(5.0);
        Assert.Equal(5.0, v1.X);
        Assert.Equal(5.0, v1.Y);
        Assert.Equal(5.0, v1.Z);
        Assert.Equal(5.0, v1.W);

        var v2 = new Double4(3.0, 4.0, 5.0, 6.0);
        Assert.Equal(3.0, v2.X);
        Assert.Equal(4.0, v2.Y);
        Assert.Equal(5.0, v2.Z);
        Assert.Equal(6.0, v2.W);

        var v3 = new Double4(new Double2(1.0, 2.0), 3.0, 4.0);
        Assert.Equal(1.0, v3.X);
        Assert.Equal(2.0, v3.Y);
        Assert.Equal(3.0, v3.Z);
        Assert.Equal(4.0, v3.W);

        var v4 = new Double4(new Double3(5.0, 6.0, 7.0), 8.0);
        Assert.Equal(5.0, v4.X);
        Assert.Equal(6.0, v4.Y);
        Assert.Equal(7.0, v4.Z);
        Assert.Equal(8.0, v4.W);

        var v5 = new Double4(new double[] { 1.0, 2.0, 3.0, 4.0 });
        Assert.Equal(1.0, v5.X);
        Assert.Equal(2.0, v5.Y);
        Assert.Equal(3.0, v5.Z);
        Assert.Equal(4.0, v5.W);

        var v6 = new Double4(new Vector4(9.0f, 10.0f, 11.0f, 12.0f));
        Assert.Equal(9.0, v6.X);
        Assert.Equal(10.0, v6.Y);
        Assert.Equal(11.0, v6.Z);
        Assert.Equal(12.0, v6.W);
    }

    [Fact]
    public void TestDouble4Length()
    {
        var v = new Double4(2.0, 3.0, 6.0, 0.0);
        Assert.Equal(7.0, v.Length());
    }

    [Fact]
    public void TestDouble4LengthSquared()
    {
        var v = new Double4(2.0, 3.0, 6.0, 0.0);
        Assert.Equal(49.0, v.LengthSquared());
    }

    [Fact]
    public void TestDouble4Distance()
    {
        var v1 = new Double4(1.0, 2.0, 3.0, 4.0);
        var v2 = new Double4(4.0, 6.0, 3.0, 4.0);
        var distance = Double4.Distance(v1, v2);
        Assert.Equal(5.0, distance);
    }

    [Fact]
    public void TestDouble4DistanceSquared()
    {
        var v1 = new Double4(1.0, 2.0, 3.0, 4.0);
        var v2 = new Double4(4.0, 6.0, 3.0, 4.0);
        var distanceSquared = Double4.DistanceSquared(v1, v2);
        Assert.Equal(25.0, distanceSquared);
    }

    [Fact]
    public void TestDouble4Dot()
    {
        var v1 = new Double4(2.0, 3.0, 4.0, 5.0);
        var v2 = new Double4(6.0, 7.0, 8.0, 9.0);
        var dot = Double4.Dot(v1, v2);
        Assert.Equal(110.0, dot); // 2*6 + 3*7 + 4*8 + 5*9 = 110
    }

    [Fact]
    public void TestDouble4Normalize()
    {
        var v = new Double4(3.0, 4.0, 0.0, 0.0);
        v.Normalize();
        Assert.Equal(1.0, v.Length(), 5);

        var v2 = new Double4(2.0, 3.0, 6.0, 0.0);
        var normalized = Double4.Normalize(v2);
        Assert.Equal(1.0, normalized.Length(), 5);
    }

    [Fact]
    public void TestDouble4Add()
    {
        var v1 = new Double4(3.5, 4.2, 5.1, 6.3);
        var v2 = new Double4(1.1, 2.3, 3.4, 4.5);
        var result = v1 + v2;
        Assert.Equal(4.6, result.X, 10);
        Assert.Equal(6.5, result.Y, 10);
        Assert.Equal(8.5, result.Z, 10);
        Assert.Equal(10.8, result.W, 10);

        Double4.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Subtract()
    {
        var v1 = new Double4(5.5, 8.8, 10.1, 12.4);
        var v2 = new Double4(2.2, 3.3, 4.4, 5.5);
        var result = v1 - v2;
        Assert.Equal(3.3, result.X, 10);
        Assert.Equal(5.5, result.Y, 10);
        Assert.Equal(5.7, result.Z, 10);
        Assert.Equal(6.9, result.W, 10);

        Double4.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Multiply()
    {
        var v = new Double4(3.0, 4.0, 5.0, 6.0);
        var result = v * 2.5;
        Assert.Equal(7.5, result.X);
        Assert.Equal(10.0, result.Y);
        Assert.Equal(12.5, result.Z);
        Assert.Equal(15.0, result.W);

        var result2 = 2.5 * v;
        Assert.Equal(result, result2);

        Double4.Multiply(ref v, 2.5, out var result3);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestDouble4Divide()
    {
        var v = new Double4(10.0, 20.0, 30.0, 40.0);
        var result = v / 2.0;
        Assert.Equal(5.0, result.X);
        Assert.Equal(10.0, result.Y);
        Assert.Equal(15.0, result.Z);
        Assert.Equal(20.0, result.W);

        Double4.Divide(ref v, 2.0, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Negate()
    {
        var v = new Double4(3.5, -5.2, 7.8, -9.1);
        var result = -v;
        Assert.Equal(-3.5, result.X);
        Assert.Equal(5.2, result.Y);
        Assert.Equal(-7.8, result.Z);
        Assert.Equal(9.1, result.W);

        Double4.Negate(ref v, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Modulate()
    {
        var v1 = new Double4(2.0, 3.0, 4.0, 5.0);
        var v2 = new Double4(6.0, 7.0, 8.0, 9.0);
        var result = Double4.Modulate(v1, v2);
        Assert.Equal(12.0, result.X);
        Assert.Equal(21.0, result.Y);
        Assert.Equal(32.0, result.Z);
        Assert.Equal(45.0, result.W);

        Double4.Modulate(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Clamp()
    {
        var value = new Double4(-5.0, 5.0, 15.0, 25.0);
        var min = new Double4(0.0, 0.0, 0.0, 0.0);
        var max = new Double4(10.0, 10.0, 10.0, 10.0);
        var result = Double4.Clamp(value, min, max);

        Assert.Equal(0.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(10.0, result.Z);
        Assert.Equal(10.0, result.W);

        Double4.Clamp(ref value, ref min, ref max, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Min()
    {
        var v1 = new Double4(1.0, 5.0, 3.0, 8.0);
        var v2 = new Double4(2.0, 3.0, 4.0, 7.0);
        var result = Double4.Min(v1, v2);
        Assert.Equal(1.0, result.X);
        Assert.Equal(3.0, result.Y);
        Assert.Equal(3.0, result.Z);
        Assert.Equal(7.0, result.W);

        Double4.Min(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Max()
    {
        var v1 = new Double4(1.0, 5.0, 3.0, 8.0);
        var v2 = new Double4(2.0, 3.0, 4.0, 7.0);
        var result = Double4.Max(v1, v2);
        Assert.Equal(2.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(4.0, result.Z);
        Assert.Equal(8.0, result.W);

        Double4.Max(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Lerp()
    {
        var start = new Double4(0.0, 0.0, 0.0, 0.0);
        var end = new Double4(10.0, 20.0, 30.0, 40.0);
        var result = Double4.Lerp(start, end, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(10.0, result.Y);
        Assert.Equal(15.0, result.Z);
        Assert.Equal(20.0, result.W);

        Double4.Lerp(ref start, ref end, 0.5, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4SmoothStep()
    {
        var start = new Double4(0.0, 0.0, 0.0, 0.0);
        var end = new Double4(10.0, 10.0, 10.0, 10.0);
        var result = Double4.SmoothStep(start, end, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(5.0, result.Z);
        Assert.Equal(5.0, result.W);

        Double4.SmoothStep(ref start, ref end, 0.5, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Barycentric()
    {
        var v1 = new Double4(0.0, 0.0, 0.0, 0.0);
        var v2 = new Double4(10.0, 0.0, 0.0, 0.0);
        var v3 = new Double4(0.0, 10.0, 0.0, 0.0);
        var result = Double4.Barycentric(v1, v2, v3, 0.5, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(0.0, result.Z);
        Assert.Equal(0.0, result.W);

        Double4.Barycentric(ref v1, ref v2, ref v3, 0.5, 0.5, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4CatmullRom()
    {
        var v1 = new Double4(0.0, 0.0, 0.0, 0.0);
        var v2 = new Double4(1.0, 1.0, 1.0, 1.0);
        var v3 = new Double4(2.0, 2.0, 2.0, 2.0);
        var v4 = new Double4(3.0, 3.0, 3.0, 3.0);
        var result = Double4.CatmullRom(v1, v2, v3, v4, 0.5);
        Assert.Equal(1.5, result.X);
        Assert.Equal(1.5, result.Y);
        Assert.Equal(1.5, result.Z);
        Assert.Equal(1.5, result.W);

        Double4.CatmullRom(ref v1, ref v2, ref v3, ref v4, 0.5, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Hermite()
    {
        var v1 = new Double4(0.0, 0.0, 0.0, 0.0);
        var t1 = new Double4(1.0, 1.0, 1.0, 1.0);
        var v2 = new Double4(10.0, 10.0, 10.0, 10.0);
        var t2 = new Double4(1.0, 1.0, 1.0, 1.0);
        var result = Double4.Hermite(v1, t1, v2, t2, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(5.0, result.Z);
        Assert.Equal(5.0, result.W);

        Double4.Hermite(ref v1, ref t1, ref v2, ref t2, 0.5, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestDouble4Transform()
    {
        var v = new Double4(1.0, 0.0, 0.0, 1.0);
        var rotation = Quaternion.RotationAxis(Vector3.UnitZ, MathUtil.PiOverTwo);
        var result = Double4.Transform(v, rotation);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(0.0, result.Z, 5);
        Assert.Equal(1.0, result.W, 5);

        Double4.Transform(ref v, ref rotation, out var result2);
        Assert.Equal(result.X, result2.X, 5);
        Assert.Equal(result.Y, result2.Y, 5);
        Assert.Equal(result.Z, result2.Z, 5);
        Assert.Equal(result.W, result2.W, 5);
    }

    [Fact]
    public void TestDouble4Equality()
    {
        var v1 = new Double4(3.5, 4.2, 5.1, 6.3);
        var v2 = new Double4(3.5, 4.2, 5.1, 6.3);
        var v3 = new Double4(5.1, 6.3, 7.2, 8.4);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestDouble4GetHashCode()
    {
        var v1 = new Double4(1.0, 2.0, 3.0, 4.0);
        var v2 = new Double4(1.0, 2.0, 3.0, 4.0);
        var v3 = new Double4(5.0, 6.0, 7.0, 8.0);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestDouble4ToString()
    {
        var v = new Double4(1.0, 2.0, 3.0, 4.0);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("3", str);
        Assert.Contains("4", str);
    }

    [Fact]
    public void TestDouble4FromVector4()
    {
        var vec4 = new Vector4(1.5f, 2.5f, 3.5f, 4.5f);
        var d4 = new Double4(vec4);
        Assert.Equal(1.5, d4.X, 5);
        Assert.Equal(2.5, d4.Y, 5);
        Assert.Equal(3.5, d4.Z, 5);
        Assert.Equal(4.5, d4.W, 5);
    }

    [Fact]
    public void TestDouble4ToVector4()
    {
        var d4 = new Double4(1.5, 2.5, 3.5, 4.5);
        var vec4 = (Vector4)d4;
        Assert.Equal(1.5f, vec4.X, 5);
        Assert.Equal(2.5f, vec4.Y, 5);
        Assert.Equal(3.5f, vec4.Z, 5);
        Assert.Equal(4.5f, vec4.W, 5);
    }

    [Fact]
    public void TestDouble4Demodulate()
    {
        var v1 = new Double4(12, 20, 30, 40);
        var v2 = new Double4(3, 4, 5, 8);
        var result = Double4.Demodulate(v1, v2);

        Assert.Equal(4.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(6.0, result.Z);
        Assert.Equal(5.0, result.W);
    }

    [Fact]
    public void TestDouble4TransformMatrixArray()
    {
        var source = new[] { new Double4(1, 2, 3, 1), new Double4(5, 6, 7, 1) };
        var dest = new Double4[2];
        var matrix = Matrix.Translation(10, 20, 30);

        Double4.Transform(source, ref matrix, dest);

        Assert.Equal(11.0, dest[0].X);
        Assert.Equal(22.0, dest[0].Y);
        Assert.Equal(33.0, dest[0].Z);
        Assert.Equal(1.0, dest[0].W);
    }

    [Fact]
    public void TestDouble4TransformQuaternionArray()
    {
        var source = new[] { new Double4(1, 0, 0, 1), new Double4(0, 1, 0, 1) };
        var dest = new Double4[2];
        var rotation = Quaternion.RotationY(MathUtil.PiOverTwo);

        Double4.Transform(source, ref rotation, dest);

        Assert.True(MathUtil.NearEqual((float)dest[0].Z, -1.0f));
        Assert.True(MathUtil.NearEqual((float)dest[0].X, 0.0f));
    }

    [Fact]
    public void TestDouble4Deconstruct()
    {
        var v = new Double4(1, 2, 3, 4);
        v.Deconstruct(out double x, out double y, out double z, out double w);

        Assert.Equal(1.0, x);
        Assert.Equal(2.0, y);
        Assert.Equal(3.0, z);
        Assert.Equal(4.0, w);
    }
}
