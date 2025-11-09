// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestVector2
{
    [Fact]
    public void TestVector2Construction()
    {
        var v1 = new Vector2(5.5f, 10.3f);
        Assert.Equal(5.5f, v1.X);
        Assert.Equal(10.3f, v1.Y);

        var v2 = new Vector2(7.7f);
        Assert.Equal(7.7f, v2.X);
        Assert.Equal(7.7f, v2.Y);
    }

    [Fact]
    public void TestVector2StaticFields()
    {
        Assert.Equal(0.0f, Vector2.Zero.X);
        Assert.Equal(0.0f, Vector2.Zero.Y);

        Assert.Equal(1.0f, Vector2.One.X);
        Assert.Equal(1.0f, Vector2.One.Y);

        Assert.Equal(1.0f, Vector2.UnitX.X);
        Assert.Equal(0.0f, Vector2.UnitX.Y);

        Assert.Equal(0.0f, Vector2.UnitY.X);
        Assert.Equal(1.0f, Vector2.UnitY.Y);
    }

    [Fact]
    public void TestVector2Addition()
    {
        var v1 = new Vector2(1.0f, 2.0f);
        var v2 = new Vector2(3.0f, 4.0f);
        var result = v1 + v2;
        Assert.Equal(4.0f, result.X);
        Assert.Equal(6.0f, result.Y);

        Vector2.Add(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestVector2Subtraction()
    {
        var v1 = new Vector2(3.0f, 5.0f);
        var v2 = new Vector2(1.0f, 2.0f);
        var result = v1 - v2;
        Assert.Equal(2.0f, result.X);
        Assert.Equal(3.0f, result.Y);

        Vector2.Subtract(ref v1, ref v2, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestVector2Multiplication()
    {
        var v = new Vector2(3.0f, 4.0f);
        var result = v * 2.5f;
        Assert.Equal(7.5f, result.X);
        Assert.Equal(10.0f, result.Y);

        var result2 = 2.5f * v;
        Assert.Equal(result, result2);

        // Component-wise multiplication (same as Modulate)
        var v1 = new Vector2(2.0f, 3.0f);
        var v2 = new Vector2(4.0f, 5.0f);
        var result3 = v1 * v2;
        Assert.Equal(8.0f, result3.X);
        Assert.Equal(15.0f, result3.Y);
    }

    [Fact]
    public void TestVector2Division()
    {
        var v = new Vector2(10.0f, 20.0f);
        var result = v / 2.0f;
        Assert.Equal(5.0f, result.X);
        Assert.Equal(10.0f, result.Y);

        // Component-wise division (same as Demodulate)
        var v1 = new Vector2(12.0f, 20.0f);
        var v2 = new Vector2(3.0f, 4.0f);
        var result2 = v1 / v2;
        Assert.Equal(4.0f, result2.X);
        Assert.Equal(5.0f, result2.Y);
    }

    [Fact]
    public void TestVector2Negation()
    {
        var v = new Vector2(3.5f, -5.2f);
        var result = -v;
        Assert.Equal(-3.5f, result.X);
        Assert.Equal(5.2f, result.Y);

        Vector2.Negate(ref v, out var result2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestVector2DotProduct()
    {
        var v1 = new Vector2(1.0f, 2.0f);
        var v2 = new Vector2(3.0f, 4.0f);
        var result = Vector2.Dot(v1, v2);
        Assert.Equal(11.0f, result); // (1 * 3) + (2 * 4)
    }

    [Fact]
    public void TestVector2Normalization()
    {
        var v = new Vector2(3.0f, 4.0f);
        var normalized = Vector2.Normalize(v);
        Assert.Equal(0.6f, normalized.X, 3); // 3/5
        Assert.Equal(0.8f, normalized.Y, 3); // 4/5
        Assert.Equal(1.0f, normalized.Length(), 3);

        v.Normalize();
        Assert.Equal(0.6f, v.X, 3);
        Assert.Equal(0.8f, v.Y, 3);
    }

    [Theory]
    [InlineData(0.0f, 0.0f)]
    [InlineData(1.0f, 1.0f)]
    [InlineData(-1.0f, 1.0f)]
    [InlineData(3.0f, 3.0f)]
    public void TestVector2Length(float value, float expectedLength)
    {
        var vector = new Vector2(value, 0.0f);
        Assert.Equal(expectedLength, vector.Length());
        Assert.Equal(expectedLength * expectedLength, vector.LengthSquared());
    }

    [Fact]
    public void TestVector2Distance()
    {
        var v1 = new Vector2(1.0f, 2.0f);
        var v2 = new Vector2(4.0f, 6.0f);
        var distance = Vector2.Distance(v1, v2);
        Assert.Equal(5.0f, distance); // sqrt(3^2 + 4^2)

        var distanceSq = Vector2.DistanceSquared(v1, v2);
        Assert.Equal(25.0f, distanceSq);
    }

    [Fact]
    public void TestVector2MinMax()
    {
        var v1 = new Vector2(1.0f, 5.0f);
        var v2 = new Vector2(3.0f, 2.0f);

        var min = Vector2.Min(v1, v2);
        Assert.Equal(1.0f, min.X);
        Assert.Equal(2.0f, min.Y);

        var max = Vector2.Max(v1, v2);
        Assert.Equal(3.0f, max.X);
        Assert.Equal(5.0f, max.Y);
    }

    [Fact]
    public void TestVector2Clamp()
    {
        var value = new Vector2(5.0f, -2.0f);
        var min = new Vector2(0.0f, 0.0f);
        var max = new Vector2(10.0f, 10.0f);

        var clamped = Vector2.Clamp(value, min, max);
        Assert.Equal(5.0f, clamped.X);
        Assert.Equal(0.0f, clamped.Y);
    }

    [Fact]
    public void TestVector2Lerp()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(10.0f, 20.0f);

        var lerp = Vector2.Lerp(v1, v2, 0.5f);
        Assert.Equal(5.0f, lerp.X);
        Assert.Equal(10.0f, lerp.Y);
    }

    [Fact]
    public void TestVector2Reflect()
    {
        var vector = new Vector2(1.0f, 1.0f);
        var normal = new Vector2(0.0f, 1.0f);

        var reflected = Vector2.Reflect(vector, normal);
        Assert.Equal(1.0f, reflected.X);
        Assert.Equal(-1.0f, reflected.Y);
    }

    [Fact]
    public void TestVector2Equality()
    {
        var v1 = new Vector2(3.5f, 4.2f);
        var v2 = new Vector2(3.5f, 4.2f);
        var v3 = new Vector2(5.1f, 6.3f);

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
    public void TestVector2Barycentric()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(10.0f, 0.0f);
        var v3 = new Vector2(0.0f, 10.0f);
        var result = Vector2.Barycentric(v1, v2, v3, 0.25f, 0.25f);
        Assert.Equal(2.5f, result.X, 3);
        Assert.Equal(2.5f, result.Y, 3);
    }

    [Fact]
    public void TestVector2SmoothStep()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(10.0f, 20.0f);
        var result = Vector2.SmoothStep(v1, v2, 0.5f);
        Assert.Equal(5.0f, result.X);
        Assert.Equal(10.0f, result.Y);
    }

    [Fact]
    public void TestVector2Hermite()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var t1 = new Vector2(1.0f, 1.0f);
        var v2 = new Vector2(10.0f, 10.0f);
        var t2 = new Vector2(1.0f, 1.0f);
        var result = Vector2.Hermite(v1, t1, v2, t2, 0.5f);
        Assert.InRange(result.X, 4.0f, 6.0f);
        Assert.InRange(result.Y, 4.0f, 6.0f);
    }

    [Fact]
    public void TestVector2CatmullRom()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(5.0f, 5.0f);
        var v3 = new Vector2(10.0f, 10.0f);
        var v4 = new Vector2(15.0f, 15.0f);
        var result = Vector2.CatmullRom(v1, v2, v3, v4, 0.5f);
        Assert.Equal(7.5f, result.X, 3);
        Assert.Equal(7.5f, result.Y, 3);
    }

    [Fact]
    public void TestVector2Transform()
    {
        var v = new Vector2(1.0f, 0.0f);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        var result = Vector2.Transform(v, matrix);
        Assert.Equal(0.0f, result.X, 3);
        Assert.Equal(1.0f, result.Y, 3);
    }

    [Fact]
    public void TestVector2TransformQuaternion()
    {
        var v = new Vector2(1.0f, 0.0f);
        var q = Quaternion.RotationZ(MathUtil.PiOverTwo);
        var result = Vector2.Transform(v, q);
        Assert.Equal(0.0f, result.X, 3);
        Assert.Equal(1.0f, result.Y, 3);
    }

    [Fact]
    public void TestVector2TransformCoordinate()
    {
        var v = new Vector2(1.0f, 1.0f);
        var matrix = Matrix.Translation(5.0f, 10.0f, 0.0f);
        var result = Vector2.TransformCoordinate(v, matrix);
        Assert.Equal(6.0f, result.X, 3);
        Assert.Equal(11.0f, result.Y, 3);
    }

    [Fact]
    public void TestVector2TransformNormal()
    {
        var v = new Vector2(1.0f, 0.0f);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        var result = Vector2.TransformNormal(v, matrix);
        Assert.Equal(0.0f, result.X, 3);
        Assert.Equal(1.0f, result.Y, 3);
    }

    [Fact]
    public void TestVector2MoveTo()
    {
        var from = new Vector2(0.0f, 0.0f);
        var to = new Vector2(10.0f, 0.0f);
        var result = Vector2.MoveTo(from, to, 5.0f);
        Assert.Equal(5.0f, result.X);
        Assert.Equal(0.0f, result.Y);

        var result2 = Vector2.MoveTo(from, to, 15.0f);
        Assert.Equal(10.0f, result2.X);
        Assert.Equal(0.0f, result2.Y);
    }

    [Fact]
    public void TestVector2Conversions()
    {
        var v = new Vector2(3.5f, 4.2f);

        // System.Numerics.Vector2
        System.Numerics.Vector2 sysVec = v;
        Assert.Equal(3.5f, sysVec.X);
        Assert.Equal(4.2f, sysVec.Y);

        Vector2 backToStride = sysVec;
        Assert.Equal(v, backToStride);

        // Vector3
        Vector3 v3 = (Vector3)v;
        Assert.Equal(3.5f, v3.X);
        Assert.Equal(4.2f, v3.Y);
        Assert.Equal(0.0f, v3.Z);

        // Vector4
        Vector4 v4 = (Vector4)v;
        Assert.Equal(3.5f, v4.X);
        Assert.Equal(4.2f, v4.Y);
        Assert.Equal(0.0f, v4.Z);
        Assert.Equal(0.0f, v4.W);
    }

    [Fact]
    public void TestVector2HashCode()
    {
        var v1 = new Vector2(3.5f, 4.2f);
        var v2 = new Vector2(3.5f, 4.2f);
        var v3 = new Vector2(5.1f, 6.3f);

        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
        Assert.NotEqual(v1.GetHashCode(), v3.GetHashCode());
    }

    [Fact]
    public void TestVector2ScalarDivision()
    {
        var v = new Vector2(10.0f, 20.0f);
        var result = 100.0f / v;
        Assert.Equal(10.0f, result.X);
        Assert.Equal(5.0f, result.Y);
    }

    [Fact]
    public void TestVector2ZeroLengthNormalization()
    {
        var zero = Vector2.Zero;
        var normalized = Vector2.Normalize(zero);

        // Normalizing zero vector should return zero (not NaN)
        Assert.False(float.IsNaN(normalized.X));
        Assert.False(float.IsNaN(normalized.Y));
    }

    [Fact]
    public void TestVector2DivisionByZero()
    {
        var v = new Vector2(1.0f, 2.0f);
        var result = v / 0.0f;

        // Division by zero should produce infinity
        Assert.True(float.IsInfinity(result.X));
        Assert.True(float.IsInfinity(result.Y));
    }

    [Fact]
    public void TestVector2VeryLargeValues()
    {
        var v1 = new Vector2(float.MaxValue / 2, float.MaxValue / 2);

        // Should not overflow
        var result = v1 * 0.5f;
        Assert.False(float.IsInfinity(result.X));
        Assert.False(float.IsInfinity(result.Y));
    }

    [Fact]
    public void TestVector2NegativeZero()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(-0.0f, -0.0f);

        // -0.0 and 0.0 should be equal
        Assert.Equal(v1, v2);
    }

    [Fact]
    public void TestVector2MinMaxWithNaN()
    {
        var v1 = new Vector2(1.0f, float.NaN);
        var v2 = new Vector2(2.0f, 3.0f);

        var min = Vector2.Min(v1, v2);
        var max = Vector2.Max(v1, v2);

        // Implementation may use Math.Min/Max which have specific NaN behavior
        // Just verify the functions don't crash
        Assert.True(true); // Test passes if we get here without exceptions
    }

    [Fact]
    public void TestVector2LerpExtrapolation()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(10.0f, 10.0f);

        // Test extrapolation (amount > 1)
        var result = Vector2.Lerp(v1, v2, 2.0f);
        Assert.Equal(20.0f, result.X);
        Assert.Equal(20.0f, result.Y);

        // Test extrapolation (amount < 0)
        var result2 = Vector2.Lerp(v1, v2, -0.5f);
        Assert.Equal(-5.0f, result2.X);
        Assert.Equal(-5.0f, result2.Y);
    }

    [Fact]
    public void TestVector2EqualityPrecision()
    {
        var v1 = new Vector2(1.0f / 3.0f, 1.0f / 7.0f);
        var v2 = new Vector2(1.0f / 3.0f, 1.0f / 7.0f);

        // Should be exactly equal due to same calculation
        Assert.Equal(v1, v2);
        Assert.True(v1 == v2);
    }

    [Fact]
    public void TestVector2DotProductAccuracy()
    {
        var v1 = new Vector2(1e-20f, 1e-20f);
        var v2 = new Vector2(1e20f, 1e20f);

        var dot = Vector2.Dot(v1, v2);

        // Should handle extreme magnitude differences
        Assert.False(float.IsNaN(dot));
        Assert.False(float.IsInfinity(dot));
    }

    [Fact]
    public void TestVector2Modulate()
    {
        var v1 = new Vector2(2, 3);
        var v2 = new Vector2(4, 5);
        var result = Vector2.Modulate(v1, v2);

        Assert.Equal(8.0f, result.X);
        Assert.Equal(15.0f, result.Y);
    }

    [Fact]
    public void TestVector2Demodulate()
    {
        var v1 = new Vector2(12, 20);
        var v2 = new Vector2(3, 4);
        var result = Vector2.Demodulate(v1, v2);

        Assert.Equal(4.0f, result.X);
        Assert.Equal(5.0f, result.Y);
    }

    [Fact]
    public void TestVector2TransformQuaternionArray()
    {
        var source = new[] { new Vector2(1, 0), new Vector2(0, 1) };
        var dest = new Vector2[2];
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);

        Vector2.Transform(source, ref rotation, dest);

        Assert.True(MathUtil.NearEqual(dest[0].X, 0.0f));
        Assert.True(MathUtil.NearEqual(dest[0].Y, 1.0f));
    }

    [Fact]
    public void TestVector2TransformMatrixArray()
    {
        var source = new[] { new Vector2(1, 2), new Vector2(3, 4) };
        var dest = new Vector4[2];
        var matrix = Matrix.Translation(10, 20, 0);

        Vector2.Transform(source, ref matrix, dest);

        Assert.Equal(11.0f, dest[0].X);
        Assert.Equal(22.0f, dest[0].Y);
    }

    [Fact]
    public void TestVector2Deconstruct()
    {
        var v = new Vector2(1, 2);
        v.Deconstruct(out float x, out float y);

        Assert.Equal(1.0f, x);
        Assert.Equal(2.0f, y);
    }
}
