// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;
using System;

namespace Stride.Core.Mathematics.Tests;

public class TestVector4
{


    [Fact]
    public void TestVector4Construction()
    {
        var v1 = new Vector4(5.5f, 10.3f, 15.7f, 20.1f);
        Assert.Equal(5.5f, v1.X);
        Assert.Equal(10.3f, v1.Y);
        Assert.Equal(15.7f, v1.Z);
        Assert.Equal(20.1f, v1.W);

        var v2 = new Vector4(7.7f);
        Assert.Equal(7.7f, v2.X);
        Assert.Equal(7.7f, v2.Y);
        Assert.Equal(7.7f, v2.Z);
        Assert.Equal(7.7f, v2.W);

        var v3 = new Vector4(new Vector2(2.0f, 3.0f), 4.0f, 5.0f);
        Assert.Equal(2.0f, v3.X);
        Assert.Equal(3.0f, v3.Y);
        Assert.Equal(4.0f, v3.Z);
        Assert.Equal(5.0f, v3.W);

        var v4 = new Vector4(new Vector3(1.0f, 2.0f, 3.0f), 4.0f);
        Assert.Equal(1.0f, v4.X);
        Assert.Equal(2.0f, v4.Y);
        Assert.Equal(3.0f, v4.Z);
        Assert.Equal(4.0f, v4.W);
    }

    [Fact]
    public void TestVector4StaticFields()
    {
        Assert.Equal(0.0f, Vector4.Zero.X);
        Assert.Equal(1.0f, Vector4.One.X);
        Assert.Equal(1.0f, Vector4.UnitX.X);
        Assert.Equal(0.0f, Vector4.UnitX.Y);
        Assert.Equal(1.0f, Vector4.UnitY.Y);
        Assert.Equal(1.0f, Vector4.UnitZ.Z);
        Assert.Equal(1.0f, Vector4.UnitW.W);
    }

    [Fact]
    public void TestVector4Operations()
    {
        var v1 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var v2 = new Vector4(5.0f, 6.0f, 7.0f, 8.0f);

        // Test addition
        var sum = v1 + v2;
        Assert.Equal(6.0f, sum.X);
        Assert.Equal(8.0f, sum.Y);
        Assert.Equal(10.0f, sum.Z);
        Assert.Equal(12.0f, sum.W);

        // Test subtraction
        var diff = v2 - v1;
        Assert.Equal(4.0f, diff.X);
        Assert.Equal(4.0f, diff.Y);
        Assert.Equal(4.0f, diff.Z);
        Assert.Equal(4.0f, diff.W);

        // Test dot product
        var dot = Vector4.Dot(v1, v2);
        Assert.Equal(70.0f, dot); // (1*5) + (2*6) + (3*7) + (4*8)

        // Test normalization
        var v = new Vector4(2.0f, 2.0f, 2.0f, 1.0f);
        var normalized = Vector4.Normalize(v);
        var length = (float)Math.Sqrt(13.0f); // sqrt(4 + 4 + 4 + 1)
        Assert.Equal(2.0f/length, normalized.X, 3);
        Assert.Equal(2.0f/length, normalized.Y, 3);
        Assert.Equal(2.0f/length, normalized.Z, 3);
        Assert.Equal(1.0f/length, normalized.W, 3);
        Assert.Equal(1.0f, normalized.Length(), 3);
    }

    [Fact]
    public void TestVector4Multiplication()
    {
        var v = new Vector4(3.0f, 4.0f, 5.0f, 6.0f);
        var result = v * 2.5f;
        Assert.Equal(7.5f, result.X);
        Assert.Equal(10.0f, result.Y);
        Assert.Equal(12.5f, result.Z);
        Assert.Equal(15.0f, result.W);

        var result2 = 2.5f * v;
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestVector4Division()
    {
        var v = new Vector4(10.0f, 20.0f, 30.0f, 40.0f);
        var result = v / 2.0f;
        Assert.Equal(5.0f, result.X);
        Assert.Equal(10.0f, result.Y);
        Assert.Equal(15.0f, result.Z);
        Assert.Equal(20.0f, result.W);
    }

    [Fact]
    public void TestVector4Negation()
    {
        var v = new Vector4(3.5f, -5.2f, 7.8f, -9.1f);
        var result = -v;
        Assert.Equal(-3.5f, result.X);
        Assert.Equal(5.2f, result.Y);
        Assert.Equal(-7.8f, result.Z);
        Assert.Equal(9.1f, result.W);

        Vector4.Negate(ref v, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestVector4Distance()
    {
        var v1 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var v2 = new Vector4(4.0f, 6.0f, 3.0f, 4.0f);
        var distance = Vector4.Distance(v1, v2);
        Assert.Equal(5.0f, distance); // sqrt(3^2 + 4^2)

        var distanceSq = Vector4.DistanceSquared(v1, v2);
        Assert.Equal(25.0f, distanceSq);
    }

    [Fact]
    public void TestVector4MinMax()
    {
        var v1 = new Vector4(1.0f, 5.0f, 3.0f, 7.0f);
        var v2 = new Vector4(3.0f, 2.0f, 4.0f, 6.0f);

        var min = Vector4.Min(v1, v2);
        Assert.Equal(1.0f, min.X);
        Assert.Equal(2.0f, min.Y);
        Assert.Equal(3.0f, min.Z);
        Assert.Equal(6.0f, min.W);

        var max = Vector4.Max(v1, v2);
        Assert.Equal(3.0f, max.X);
        Assert.Equal(5.0f, max.Y);
        Assert.Equal(4.0f, max.Z);
        Assert.Equal(7.0f, max.W);
    }

    [Fact]
    public void TestVector4Clamp()
    {
        var value = new Vector4(5.0f, -2.0f, 15.0f, 8.0f);
        var min = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        var max = new Vector4(10.0f, 10.0f, 10.0f, 10.0f);

        var clamped = Vector4.Clamp(value, min, max);
        Assert.Equal(5.0f, clamped.X);
        Assert.Equal(0.0f, clamped.Y);
        Assert.Equal(10.0f, clamped.Z);
        Assert.Equal(8.0f, clamped.W);
    }

    [Fact]
    public void TestVector4Lerp()
    {
        var v1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        var v2 = new Vector4(10.0f, 20.0f, 30.0f, 40.0f);

        var lerp = Vector4.Lerp(v1, v2, 0.5f);
        Assert.Equal(5.0f, lerp.X);
        Assert.Equal(10.0f, lerp.Y);
        Assert.Equal(15.0f, lerp.Z);
        Assert.Equal(20.0f, lerp.W);
    }

    [Fact]
    public void TestVector4Equality()
    {
        var v1 = new Vector4(3.5f, 4.2f, 5.1f, 6.3f);
        var v2 = new Vector4(3.5f, 4.2f, 5.1f, 6.3f);
        var v3 = new Vector4(5.1f, 6.3f, 7.2f, 8.4f);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);

        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
        Assert.False(v1.Equals(null));
        Assert.False(v1.Equals(new object()));
    }

    [Fact]
    public void TestVector4Barycentric()
    {
        var v1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        var v2 = new Vector4(10.0f, 0.0f, 0.0f, 0.0f);
        var v3 = new Vector4(0.0f, 10.0f, 0.0f, 0.0f);
        var result = Vector4.Barycentric(v1, v2, v3, 0.25f, 0.25f);
        Assert.Equal(2.5f, result.X, 3);
        Assert.Equal(2.5f, result.Y, 3);
        Assert.Equal(0.0f, result.Z, 3);
        Assert.Equal(0.0f, result.W, 3);
    }

    [Fact]
    public void TestVector4SmoothStep()
    {
        var v1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        var v2 = new Vector4(10.0f, 20.0f, 30.0f, 40.0f);
        var result = Vector4.SmoothStep(v1, v2, 0.5f);
        Assert.Equal(5.0f, result.X);
        Assert.Equal(10.0f, result.Y);
        Assert.Equal(15.0f, result.Z);
        Assert.Equal(20.0f, result.W);
    }

    [Fact]
    public void TestVector4Hermite()
    {
        var v1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        var t1 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        var v2 = new Vector4(10.0f, 10.0f, 10.0f, 10.0f);
        var t2 = new Vector4(1.0f, 1.0f, 1.0f, 1.0f);
        var result = Vector4.Hermite(v1, t1, v2, t2, 0.5f);
        Assert.InRange(result.X, 4.0f, 6.0f);
        Assert.InRange(result.Y, 4.0f, 6.0f);
        Assert.InRange(result.Z, 4.0f, 6.0f);
        Assert.InRange(result.W, 4.0f, 6.0f);
    }

    [Fact]
    public void TestVector4CatmullRom()
    {
        var v1 = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        var v2 = new Vector4(5.0f, 5.0f, 5.0f, 5.0f);
        var v3 = new Vector4(10.0f, 10.0f, 10.0f, 10.0f);
        var v4 = new Vector4(15.0f, 15.0f, 15.0f, 15.0f);
        var result = Vector4.CatmullRom(v1, v2, v3, v4, 0.5f);
        Assert.Equal(7.5f, result.X, 3);
        Assert.Equal(7.5f, result.Y, 3);
        Assert.Equal(7.5f, result.Z, 3);
        Assert.Equal(7.5f, result.W, 3);
    }

    [Fact]
    public void TestVector4Transform()
    {
        var v = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        var result = Vector4.Transform(v, matrix);
        Assert.Equal(0.0f, result.X, 3);
        Assert.Equal(1.0f, result.Y, 3);
        Assert.Equal(0.0f, result.Z, 3);
        Assert.Equal(1.0f, result.W, 3);
    }

    [Fact]
    public void TestVector4TransformQuaternion()
    {
        var v = new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        var q = Quaternion.RotationZ(MathUtil.PiOverTwo);
        var result = Vector4.Transform(v, q);
        Assert.Equal(0.0f, result.X, 3);
        Assert.Equal(1.0f, result.Y, 3);
        Assert.Equal(0.0f, result.Z, 3);
        Assert.Equal(1.0f, result.W, 3);
    }

    [Fact]
    public void TestVector4Conversions()
    {
        var v = new Vector4(3.5f, 4.2f, 5.1f, 6.3f);

        // System.Numerics.Vector4
        System.Numerics.Vector4 sysVec = v;
        Assert.Equal(3.5f, sysVec.X);
        Assert.Equal(4.2f, sysVec.Y);
        Assert.Equal(5.1f, sysVec.Z);
        Assert.Equal(6.3f, sysVec.W);

        Vector4 backToStride = sysVec;
        Assert.Equal(v, backToStride);

        // Vector2
        Vector2 v2 = (Vector2)v;
        Assert.Equal(3.5f, v2.X);
        Assert.Equal(4.2f, v2.Y);

        // Vector3
        Vector3 v3 = (Vector3)v;
        Assert.Equal(3.5f, v3.X);
        Assert.Equal(4.2f, v3.Y);
        Assert.Equal(5.1f, v3.Z);
    }

    [Fact]
    public void TestVector4HashCode()
    {
        var v1 = new Vector4(3.5f, 4.2f, 5.1f, 6.3f);
        var v2 = new Vector4(3.5f, 4.2f, 5.1f, 6.3f);
        var v3 = new Vector4(7.2f, 8.3f, 9.4f, 10.5f);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestVector4ScalarDivision()
    {
        var v = new Vector4(10.0f, 20.0f, 40.0f, 100.0f);
        var result = 100.0f / v;
        Assert.Equal(10.0f, result.X);
        Assert.Equal(5.0f, result.Y);
        Assert.Equal(2.5f, result.Z);
        Assert.Equal(1.0f, result.W);
    }

    [Fact]
    public void TestVector4ZeroLengthNormalization()
    {
        var zero = Vector4.Zero;
        var normalized = Vector4.Normalize(zero);

        // Normalizing zero vector should return zero (not NaN)
        Assert.False(float.IsNaN(normalized.X));
        Assert.False(float.IsNaN(normalized.Y));
        Assert.False(float.IsNaN(normalized.Z));
        Assert.False(float.IsNaN(normalized.W));
    }

    [Fact]
    public void TestVector4DivisionByZero()
    {
        var v = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var result = v / 0.0f;

        // Division by zero should produce infinity
        Assert.True(float.IsInfinity(result.X));
        Assert.True(float.IsInfinity(result.Y));
        Assert.True(float.IsInfinity(result.Z));
        Assert.True(float.IsInfinity(result.W));
    }

    [Fact]
    public void TestVector4ClampWithInvertedMinMax()
    {
        var value = new Vector4(5.0f, 5.0f, 5.0f, 5.0f);
        var min = new Vector4(10.0f, 10.0f, 10.0f, 10.0f);
        var max = new Vector4(0.0f, 0.0f, 0.0f, 0.0f); // max < min (invalid)

        // Behavior with inverted min/max - implementation clamps to min first
        var result = Vector4.Clamp(value, min, max);

        // Implementation clamps to min first, so result is min
        Assert.Equal(10.0f, result.X);
        Assert.Equal(10.0f, result.Y);
        Assert.Equal(10.0f, result.Z);
        Assert.Equal(10.0f, result.W);
    }

    [Fact]
    public void TestVector4FromVector2FillsZW()
    {
        var v2 = new Vector2(1.0f, 2.0f);
        var v4 = (Vector4)v2;

        Assert.Equal(1.0f, v4.X);
        Assert.Equal(2.0f, v4.Y);
        Assert.Equal(0.0f, v4.Z);
        Assert.Equal(0.0f, v4.W);
    }

    [Fact]
    public void TestVector4FromVector3FillsW()
    {
        var v3 = new Vector3(1.0f, 2.0f, 3.0f);
        var v4 = (Vector4)v3;

        Assert.Equal(1.0f, v4.X);
        Assert.Equal(2.0f, v4.Y);
        Assert.Equal(3.0f, v4.Z);
        Assert.Equal(0.0f, v4.W);
    }

    [Fact]
    public void TestVector4Modulate()
    {
        var v1 = new Vector4(2, 3, 4, 5);
        var v2 = new Vector4(3, 4, 5, 6);
        var result = Vector4.Modulate(v1, v2);

        Assert.Equal(6.0f, result.X);
        Assert.Equal(12.0f, result.Y);
        Assert.Equal(20.0f, result.Z);
        Assert.Equal(30.0f, result.W);
    }

    [Fact]
    public void TestVector4Demodulate()
    {
        var v1 = new Vector4(12, 20, 30, 40);
        var v2 = new Vector4(3, 4, 5, 8);
        var result = Vector4.Demodulate(v1, v2);

        Assert.Equal(4.0f, result.X);
        Assert.Equal(5.0f, result.Y);
        Assert.Equal(6.0f, result.Z);
        Assert.Equal(5.0f, result.W);
    }

    [Fact]
    public void TestVector4Moveto()
    {
        var from = new Vector4(0, 0, 0, 0);
        var to = new Vector4(10, 10, 10, 10);
        var result = Vector4.Moveto(from, to, 5.0f);

        // Distance from origin to (10,10,10,10) is 20, moving 5 units
        Assert.True(MathUtil.NearEqual(result.X, 2.5f) || Math.Abs(result.X - 2.5f) < 0.01f);
        Assert.True(MathUtil.NearEqual(result.Y, 2.5f) || Math.Abs(result.Y - 2.5f) < 0.01f);
        Assert.True(MathUtil.NearEqual(result.Z, 2.5f) || Math.Abs(result.Z - 2.5f) < 0.01f);
        Assert.True(MathUtil.NearEqual(result.W, 2.5f) || Math.Abs(result.W - 2.5f) < 0.01f);
    }

    [Fact]
    public void TestVector4MovetoExceedsDistance()
    {
        var from = new Vector4(0, 0, 0, 0);
        var to = new Vector4(1, 0, 0, 0);
        var result = Vector4.Moveto(from, to, 10.0f);

        // Moving further than target should arrive at target
        Assert.Equal(1.0f, result.X);
        Assert.Equal(0.0f, result.Y);
        Assert.Equal(0.0f, result.Z);
        Assert.Equal(0.0f, result.W);
    }

    [Fact]
    public void TestVector4TransformArray()
    {
        var source = new[] { new Vector4(1, 2, 3, 1), new Vector4(5, 6, 7, 1) };
        var dest = new Vector4[2];
        var matrix = Matrix.Translation(10, 20, 30);

        Vector4.Transform(source, ref matrix, dest);

        Assert.Equal(11.0f, dest[0].X);
        Assert.Equal(22.0f, dest[0].Y);
        Assert.Equal(33.0f, dest[0].Z);
        Assert.Equal(1.0f, dest[0].W);
    }

    [Fact]
    public void TestVector4TransformQuaternionArray()
    {
        var source = new[] { new Vector4(1, 0, 0, 1), new Vector4(0, 1, 0, 1) };
        var dest = new Vector4[2];
        var rotation = Quaternion.RotationY(MathUtil.PiOverTwo);

        Vector4.Transform(source, ref rotation, dest);

        Assert.True(MathUtil.NearEqual(dest[0].Z, -1.0f));
        Assert.True(MathUtil.NearEqual(dest[0].X, 0.0f));
        Assert.Equal(1.0f, dest[0].W);
    }

    [Fact]
    public void TestVector4Deconstruct()
    {
        var v = new Vector4(1, 2, 3, 4);
        v.Deconstruct(out float x, out float y, out float z, out float w);

        Assert.Equal(1.0f, x);
        Assert.Equal(2.0f, y);
        Assert.Equal(3.0f, z);
        Assert.Equal(4.0f, w);
    }


}
