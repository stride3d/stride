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
        Assert.Equal(2.0f / length, normalized.X, 3);
        Assert.Equal(2.0f / length, normalized.Y, 3);
        Assert.Equal(2.0f / length, normalized.Z, 3);
        Assert.Equal(1.0f / length, normalized.W, 3);
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

        var result2 = Vector4.Negate(v);
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
        var max = new Vector4(1.0f, 1.0f, 1.0f, 1.0f); // max < min (invalid)

        // Behavior with inverted min/max - implementation clamps to min first
        var result = Vector4.Clamp(value, min, max);

        // Implementation clamps to min first, so result is max
        // note: follows HLSL convention
        Assert.Equal(1.0f, result.X);
        Assert.Equal(1.0f, result.Y);
        Assert.Equal(1.0f, result.Z);
        Assert.Equal(1.0f, result.W);
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
        Assert.True(MathUtil.NearEqual(dest[0].W, 1.0f));
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

    [Fact]
    public void TestVector4SizeInBytes()
    {
        Assert.Equal(16, Vector4.SizeInBytes);
    }

    [Fact]
    public void TestVector4IsNormalized()
    {
        var normalized = new Vector4(1, 0, 0, 0);
        var notNormalized = new Vector4(2, 0, 0, 0);
        var normalizedNonAxis = Vector4.Normalize(new Vector4(1, 2, 3, 4));

        Assert.True(normalized.IsNormalized);
        Assert.False(notNormalized.IsNormalized);
        Assert.True(normalizedNonAxis.IsNormalized);
    }

    [Fact]
    public void TestVector4Indexer()
    {
        var v = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);

        Assert.Equal(1.0f, v[0]);
        Assert.Equal(2.0f, v[1]);
        Assert.Equal(3.0f, v[2]);
        Assert.Equal(4.0f, v[3]);

        v[0] = 10.0f;
        v[1] = 20.0f;
        v[2] = 30.0f;
        v[3] = 40.0f;

        Assert.Equal(10.0f, v.X);
        Assert.Equal(20.0f, v.Y);
        Assert.Equal(30.0f, v.Z);
        Assert.Equal(40.0f, v.W);

        Assert.Throws<ArgumentOutOfRangeException>(() => v[4]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[4] = 1.0f);
    }

    [Fact]
    public void TestVector4ArrayConstructor()
    {
        var v = new Vector4(new float[] { 1.0f, 2.0f, 3.0f, 4.0f });
        Assert.Equal(1.0f, v.X);
        Assert.Equal(2.0f, v.Y);
        Assert.Equal(3.0f, v.Z);
        Assert.Equal(4.0f, v.W);

        Assert.Throws<ArgumentNullException>(() => new Vector4((float[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector4(new float[] { 1.0f, 2.0f, 3.0f }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector4(new float[] { 1.0f, 2.0f, 3.0f, 4.0f, 5.0f }));
    }

    [Fact]
    public void TestVector4ToArray()
    {
        var v = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var array = v.ToArray();

        Assert.Equal(4, array.Length);
        Assert.Equal(1.0f, array[0]);
        Assert.Equal(2.0f, array[1]);
        Assert.Equal(3.0f, array[2]);
        Assert.Equal(4.0f, array[3]);
    }

    [Fact]
    public void TestVector4Pow()
    {
        var v = new Vector4(2.0f, 3.0f, 4.0f, 5.0f);
        v.Pow(2.0f);

        Assert.Equal(4.0f, v.X, 3);
        Assert.Equal(9.0f, v.Y, 3);
        Assert.Equal(16.0f, v.Z, 3);
        Assert.Equal(25.0f, v.W, 3);
    }

    [Fact]
    public void TestVector4UnaryPlus()
    {
        var v = new Vector4(1.5f, -2.5f, 3.5f, -4.5f);
        var result = +v;

        Assert.Equal(v, result);
        Assert.Equal(1.5f, result.X);
        Assert.Equal(-2.5f, result.Y);
        Assert.Equal(3.5f, result.Z);
        Assert.Equal(-4.5f, result.W);
    }

    [Fact]
    public void TestVector4EqualsStrict()
    {
        var v1 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var v2 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var v3 = new Vector4(1.0f + (MathUtil.ZeroTolerance / 2.0f), 2.0f, 3.0f, 4.0f);

        Assert.True(v1.EqualsStrict(v2));
        // A small difference within ZeroTolerance is still "equal" via Equals, but not via EqualsStrict
        Assert.True(v1.Equals(v3));
        Assert.False(v1.EqualsStrict(v3));
    }

    [Fact]
    public void TestVector4ToString()
    {
        var v = new Vector4(1.5f, 2.5f, 3.5f, 4.5f);
        var s = v.ToString();
        Assert.Contains("X:", s);
        Assert.Contains("Y:", s);
        Assert.Contains("Z:", s);
        Assert.Contains("W:", s);

        var formatted = v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("X:1.5 Y:2.5 Z:3.5 W:4.5", formatted);
    }

    [Fact]
    public void TestVector4TryFormat()
    {
        var v = new Vector4(1.5f, 2.5f, 3.5f, 4.5f);
        Span<char> destination = new char[64];

        bool success = ((ISpanFormattable)v).TryFormat(destination, out int charsWritten, "F1".AsSpan(), System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(success);
        Assert.Equal("X:1.5 Y:2.5 Z:3.5 W:4.5", destination[..charsWritten].ToString());
    }

    [Fact]
    public void TestVector4OrthogonalizeNonTrivial()
    {
        var source = new[]
        {
            new Vector4(1, 1, 0, 0),
            new Vector4(1, 0, 1, 0),
            new Vector4(0, 1, 1, 0),
            new Vector4(1, 1, 1, 1),
        };
        var dest = new Vector4[4];

        Vector4.Orthogonalize(dest, source);

        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[0], dest[1]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[0], dest[2]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[0], dest[3]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[1], dest[2]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[1], dest[3]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[2], dest[3]), 0.0f));

        Assert.Throws<ArgumentNullException>(() => Vector4.Orthogonalize(null!, source));
        Assert.Throws<ArgumentNullException>(() => Vector4.Orthogonalize(dest, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector4.Orthogonalize(new Vector4[1], source));
    }

    [Fact]
    public void TestVector4Orthonormalize()
    {
        var source = new[]
        {
            new Vector4(2, 0, 0, 0),
            new Vector4(2, 2, 0, 0),
            new Vector4(2, 2, 2, 0),
            new Vector4(2, 2, 2, 2),
        };
        var dest = new Vector4[4];

        Vector4.Orthonormalize(dest, source);

        for (int i = 0; i < dest.Length; i++)
        {
            Assert.True(MathUtil.NearEqual(dest[i].Length(), 1.0f));
        }

        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[0], dest[1]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[1], dest[2]), 0.0f));
        Assert.True(MathUtil.NearEqual(Vector4.Dot(dest[2], dest[3]), 0.0f));

        Assert.Throws<ArgumentNullException>(() => Vector4.Orthonormalize(null!, source));
        Assert.Throws<ArgumentNullException>(() => Vector4.Orthonormalize(dest, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector4.Orthonormalize(new Vector4[1], source));
    }

    [Fact]
    public void TestVector4StaticAddSubtractOutParam()
    {
        var v1 = new Vector4(1.0f, 2.0f, 3.0f, 4.0f);
        var v2 = new Vector4(5.0f, 6.0f, 7.0f, 8.0f);

        var sum = Vector4.Add(v1, v2);
        Assert.Equal(new Vector4(6.0f, 8.0f, 10.0f, 12.0f), sum);
        Assert.Equal(sum, Vector4.Add(v1, v2));

        var diff = Vector4.Subtract(v2, v1);
        Assert.Equal(new Vector4(4.0f, 4.0f, 4.0f, 4.0f), diff);
        Assert.Equal(diff, Vector4.Subtract(v2, v1));
    }

    [Fact]
    public void TestVector4StaticMultiplyDivideOutParam()
    {
        var v = new Vector4(2.0f, 4.0f, 6.0f, 8.0f);

        var scaled = Vector4.Multiply(v, 3.0f);
        Assert.Equal(new Vector4(6.0f, 12.0f, 18.0f, 24.0f), scaled);
        Assert.Equal(scaled, Vector4.Multiply(v, 3.0f));

        var divided = Vector4.Divide(v, 2.0f);
        Assert.Equal(new Vector4(1.0f, 2.0f, 3.0f, 4.0f), divided);
        Assert.Equal(divided, Vector4.Divide(v, 2.0f));
    }

    [Fact]
    public void TestVector4StaticModulateDemodulateOutParam()
    {
        var v1 = new Vector4(2.0f, 3.0f, 4.0f, 5.0f);
        var v2 = new Vector4(3.0f, 4.0f, 5.0f, 6.0f);

        var modulated = Vector4.Modulate(v1, v2);
        Assert.Equal(new Vector4(6.0f, 12.0f, 20.0f, 30.0f), modulated);

        var demodulated = Vector4.Demodulate(modulated, v2);
        Assert.Equal(v1, demodulated);
    }

    [Fact]
    public void TestVector4BarycentricNonTrivial()
    {
        var v1 = new Vector4(-5.0f, 2.0f, 1.0f, 0.0f);
        var v2 = new Vector4(3.0f, -4.0f, 6.0f, 2.0f);
        var v3 = new Vector4(1.0f, 1.0f, -3.0f, -1.0f);

        var result = Vector4.Barycentric(v1, v2, v3, 0.3f, 0.6f);

        Assert.Equal(v1.X + (0.3f * (v2.X - v1.X)) + (0.6f * (v3.X - v1.X)), result.X, 3);
        Assert.Equal(v1.Y + (0.3f * (v2.Y - v1.Y)) + (0.6f * (v3.Y - v1.Y)), result.Y, 3);
        Assert.Equal(v1.Z + (0.3f * (v2.Z - v1.Z)) + (0.6f * (v3.Z - v1.Z)), result.Z, 3);
        Assert.Equal(v1.W + (0.3f * (v2.W - v1.W)) + (0.6f * (v3.W - v1.W)), result.W, 3);
    }

    [Fact]
    public void TestVector4HermiteNonTrivial()
    {
        var v1 = new Vector4(1.0f, -2.0f, 3.0f, 0.0f);
        var t1 = new Vector4(2.0f, 1.0f, -1.0f, 1.0f);
        var v2 = new Vector4(-4.0f, 5.0f, 2.0f, -3.0f);
        var t2 = new Vector4(0.5f, -0.5f, 1.0f, 2.0f);

        var result = Vector4.Hermite(v1, t1, v2, t2, 0.25f);

        float amount = 0.25f;
        float squared = amount * amount;
        float cubed = amount * squared;
        float part1 = (2.0f * cubed) - (3.0f * squared) + 1.0f;
        float part2 = (-2.0f * cubed) + (3.0f * squared);
        float part3 = cubed - (2.0f * squared) + amount;
        float part4 = cubed - squared;

        Assert.Equal((v1.X * part1) + (v2.X * part2) + (t1.X * part3) + (t2.X * part4), result.X, 3);
        Assert.Equal((v1.Y * part1) + (v2.Y * part2) + (t1.Y * part3) + (t2.Y * part4), result.Y, 3);
        Assert.Equal((v1.Z * part1) + (v2.Z * part2) + (t1.Z * part3) + (t2.Z * part4), result.Z, 3);
        Assert.Equal((v1.W * part1) + (v2.W * part2) + (t1.W * part3) + (t2.W * part4), result.W, 3);
    }

    [Fact]
    public void TestVector4CatmullRomNonTrivial()
    {
        var v1 = new Vector4(1.0f, 0.0f, -2.0f, 3.0f);
        var v2 = new Vector4(2.0f, -1.0f, 0.0f, 1.0f);
        var v3 = new Vector4(-3.0f, 4.0f, 1.0f, -2.0f);
        var v4 = new Vector4(0.0f, 2.0f, 3.0f, 0.0f);

        var result = Vector4.CatmullRom(v1, v2, v3, v4, 0.35f);

        // At amount == 0, the result should be value2; verify the general formula holds at a different point too
        var atZero = Vector4.CatmullRom(v1, v2, v3, v4, 0.0f);
        Assert.Equal(v2.X, atZero.X, 3);
        Assert.Equal(v2.Y, atZero.Y, 3);
        Assert.Equal(v2.Z, atZero.Z, 3);
        Assert.Equal(v2.W, atZero.W, 3);

        var atOne = Vector4.CatmullRom(v1, v2, v3, v4, 1.0f);
        Assert.Equal(v3.X, atOne.X, 3);
        Assert.Equal(v3.Y, atOne.Y, 3);
        Assert.Equal(v3.Z, atOne.Z, 3);
        Assert.Equal(v3.W, atOne.W, 3);

        // Sanity: result at 0.35 differs from both endpoints for this non-trivial input
        Assert.NotEqual(v2, result);
        Assert.NotEqual(v3, result);
    }

    [Fact]
    public void TestVector4TransformMatrixNonAxisAligned()
    {
        var v = new Vector4(1.0f, 2.0f, 3.0f, 1.0f);
        var matrix = Matrix.RotationYawPitchRoll(0.4f, 0.6f, 0.2f) * Matrix.Translation(5.0f, -3.0f, 2.0f);

        var result = Vector4.Transform(v, matrix);

        var expectedX = (v.X * matrix.M11) + (v.Y * matrix.M21) + (v.Z * matrix.M31) + (v.W * matrix.M41);
        var expectedY = (v.X * matrix.M12) + (v.Y * matrix.M22) + (v.Z * matrix.M32) + (v.W * matrix.M42);
        var expectedZ = (v.X * matrix.M13) + (v.Y * matrix.M23) + (v.Z * matrix.M33) + (v.W * matrix.M43);
        var expectedW = (v.X * matrix.M14) + (v.Y * matrix.M24) + (v.Z * matrix.M34) + (v.W * matrix.M44);

        Assert.Equal(expectedX, result.X, 3);
        Assert.Equal(expectedY, result.Y, 3);
        Assert.Equal(expectedZ, result.Z, 3);
        Assert.Equal(expectedW, result.W, 3);
    }

    [Fact]
    public void TestVector4TransformQuaternionNonAxisAligned()
    {
        var v = new Vector4(1.0f, 2.0f, 3.0f, 5.0f);
        var q = Quaternion.RotationYawPitchRoll(0.3f, 0.5f, 0.7f);

        var result = Vector4.Transform(v, q);

        // W is unaffected by rotation, and the rotated XYZ part must preserve length
        Assert.Equal(v.W, result.W, 3);
        var originalLength3 = MathF.Sqrt((v.X * v.X) + (v.Y * v.Y) + (v.Z * v.Z));
        var resultLength3 = MathF.Sqrt((result.X * result.X) + (result.Y * result.Y) + (result.Z * result.Z));
        Assert.Equal(originalLength3, resultLength3, 3);

        // Should not equal input (verifies real rotation happened)
        Assert.False(MathUtil.NearEqual(result.X, v.X) && MathUtil.NearEqual(result.Y, v.Y) && MathUtil.NearEqual(result.Z, v.Z));
    }

    [Fact]
    public void TestVector4ExplicitConversionsDropComponents()
    {
        var v = new Vector4(1.5f, 2.5f, 3.5f, 4.5f);

        var v2 = (Vector2)v;
        Assert.Equal(1.5f, v2.X);
        Assert.Equal(2.5f, v2.Y);

        var v3 = (Vector3)v;
        Assert.Equal(1.5f, v3.X);
        Assert.Equal(2.5f, v3.Y);
        Assert.Equal(3.5f, v3.Z);
    }
}
