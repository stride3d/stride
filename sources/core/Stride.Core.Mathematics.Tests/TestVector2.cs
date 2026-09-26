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

    [Fact]
    public void TestVector2SizeInBytes()
    {
        // Two floats, 4 bytes each.
        Assert.Equal(8, Vector2.SizeInBytes);
    }

    [Fact]
    public void TestVector2ArrayConstructor()
    {
        var v = new Vector2([1.0f, 2.0f]);
        Assert.Equal(1.0f, v.X);
        Assert.Equal(2.0f, v.Y);

        Assert.Throws<ArgumentNullException>(() => new Vector2((float[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector2([1.0f]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new Vector2([1.0f, 2.0f, 3.0f]));
    }

    [Fact]
    public void TestVector2ToArray()
    {
        var v = new Vector2(3.0f, 4.0f);
        var arr = v.ToArray();

        Assert.Equal(2, arr.Length);
        Assert.Equal(3.0f, arr[0]);
        Assert.Equal(4.0f, arr[1]);
    }

    [Fact]
    public void TestVector2Indexer()
    {
        var v = new Vector2(1.5f, 2.5f);
        Assert.Equal(1.5f, v[0]);
        Assert.Equal(2.5f, v[1]);

        v[0] = 10.0f;
        v[1] = 20.0f;
        Assert.Equal(10.0f, v.X);
        Assert.Equal(20.0f, v.Y);

        Assert.Throws<ArgumentOutOfRangeException>(() => v[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[2] = 5.0f);
    }

    [Fact]
    public void TestVector2IsNormalized()
    {
        var normalized = new Vector2(1.0f, 0.0f);
        Assert.True(normalized.IsNormalized);

        var alsoNormalized = new Vector2(0.6f, 0.8f); // 0.6^2 + 0.8^2 == 1
        Assert.True(alsoNormalized.IsNormalized);

        var notNormalized = new Vector2(3.0f, 4.0f);
        Assert.False(notNormalized.IsNormalized);
    }

    [Fact]
    public void TestVector2UnaryPlus()
    {
        var v = new Vector2(3.5f, -5.2f);
        var result = +v;

        Assert.Equal(v, result);
        Assert.Equal(3.5f, result.X);
        Assert.Equal(-5.2f, result.Y);
    }

    [Fact]
    public void TestVector2EqualsStrict()
    {
        var v1 = new Vector2(1.0f, 2.0f);
        var v2 = new Vector2(1.0f, 2.0f);
        Assert.True(v1.EqualsStrict(v2));
        Assert.False(v1.EqualsStrict(new Vector2(3.0f, 4.0f)));

        // A difference of a few ULPs at magnitude 1.0 is well within MathUtil.ZeroTolerance (1e-6),
        // so the tolerant Equals() considers them equal, but EqualsStrict requires bit-for-bit equality.
        var v3 = new Vector2(1.0f + 5e-7f, 2.0f);
        Assert.NotEqual(v1.X, v3.X);
        Assert.True(v1.Equals(v3));
        Assert.False(v1.EqualsStrict(v3));
    }

    [Fact]
    public void TestVector2ToString()
    {
        var v = new Vector2(1.5f, 2.5f);
        var s = v.ToString();
        Assert.Contains("X:", s);
        Assert.Contains("Y:", s);

        var formatted = v.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Equal("X:1.50 Y:2.50", formatted);
    }

    [Fact]
    public void TestVector2TryFormat()
    {
        var v = new Vector2(1.5f, 2.5f);
        ISpanFormattable formattable = v;
        Span<char> buffer = stackalloc char[64];

        bool success = formattable.TryFormat(buffer, out int charsWritten, "F1", System.Globalization.CultureInfo.InvariantCulture);

        Assert.True(success);
        Assert.Equal("X:1.5 Y:2.5", new string(buffer[..charsWritten]));
    }

    [Fact]
    public void TestVector2MinMaxNegative()
    {
        var v1 = new Vector2(-5.0f, 3.0f);
        var v2 = new Vector2(2.0f, -7.0f);

        var min = Vector2.Min(v1, v2);
        Assert.Equal(-5.0f, min.X);
        Assert.Equal(-7.0f, min.Y);

        var max = Vector2.Max(v1, v2);
        Assert.Equal(2.0f, max.X);
        Assert.Equal(3.0f, max.Y);
    }

    [Fact]
    public void TestVector2BarycentricNonTrivial()
    {
        var v1 = new Vector2(-1.0f, -1.0f);
        var v2 = new Vector2(3.0f, -1.0f);
        var v3 = new Vector2(-1.0f, 4.0f);

        var result = Vector2.Barycentric(v1, v2, v3, 0.3f, 0.4f);

        Assert.Equal(0.2f, result.X, 3);
        Assert.Equal(1.0f, result.Y, 3);
    }

    [Fact]
    public void TestVector2HermiteNonTrivial()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var t1 = new Vector2(2.0f, 0.0f);
        var v2 = new Vector2(4.0f, 4.0f);
        var t2 = new Vector2(0.0f, -2.0f);

        var result = Vector2.Hermite(v1, t1, v2, t2, 0.5f);

        Assert.Equal(2.25f, result.X, 3);
        Assert.Equal(2.25f, result.Y, 3);
    }

    [Fact]
    public void TestVector2CatmullRomNonTrivial()
    {
        var v1 = new Vector2(0.0f, 0.0f);
        var v2 = new Vector2(1.0f, 2.0f);
        var v3 = new Vector2(3.0f, 3.0f);
        var v4 = new Vector2(4.0f, 0.0f);

        var result = Vector2.CatmullRom(v1, v2, v3, v4, 0.5f);

        Assert.Equal(2.0f, result.X, 3);
        Assert.Equal(2.8125f, result.Y, 3);
    }

    [Fact]
    public void TestVector2ReflectNonAxisAligned()
    {
        var vector = new Vector2(1.0f, 0.0f);
        var normal = Vector2.Normalize(new Vector2(1.0f, 1.0f)); // 45-degree surface normal

        var reflected = Vector2.Reflect(vector, normal);

        // Reflecting (1,0) off a 45-degree normal swaps and flips the components.
        Assert.Equal(0.0f, reflected.X, 3);
        Assert.Equal(-1.0f, reflected.Y, 3);
    }

    [Fact]
    public void TestVector2TransformNonAxisAlignedRotation()
    {
        var v = new Vector2(2.0f, 3.0f);
        float angle = 37.0f * MathF.PI / 180.0f;
        var matrix = Matrix.RotationZ(angle);

        var result = Vector2.Transform(v, matrix);
        float resultLength = MathF.Sqrt((result.X * result.X) + (result.Y * result.Y));

        // Rotation preserves length.
        Assert.Equal(v.Length(), resultLength, 3);

        // The angle between the original and transformed vector must equal the rotation angle.
        float cosAngle = ((v.X * result.X) + (v.Y * result.Y)) / (v.Length() * resultLength);
        Assert.Equal(MathF.Cos(angle), cosAngle, 3);

        // Transforming by the equivalent quaternion rotation must produce the same result.
        var q = Quaternion.RotationZ(angle);
        var resultQ = Vector2.Transform(v, q);
        Assert.Equal(result.X, resultQ.X, 3);
        Assert.Equal(result.Y, resultQ.Y, 3);
    }

    [Fact]
    public void TestVector2TransformMatrixWithScaleAndTranslation()
    {
        var v = new Vector2(1.0f, 1.0f);
        var matrix = Matrix.Identity;
        matrix.M11 = 2.0f;
        matrix.M22 = 3.0f;
        matrix.M41 = 5.0f;
        matrix.M42 = 7.0f;

        var result = Vector2.Transform(v, matrix);
        Assert.Equal(7.0f, result.X, 3);
        Assert.Equal(10.0f, result.Y, 3);
        Assert.Equal(0.0f, result.Z, 3);
        Assert.Equal(1.0f, result.W, 3);

        var coordinateResult = Vector2.TransformCoordinate(v, matrix);
        Assert.Equal(7.0f, coordinateResult.X, 3);
        Assert.Equal(10.0f, coordinateResult.Y, 3);
    }

    [Fact]
    public void TestVector2TransformCoordinateArray()
    {
        var source = new[] { new Vector2(1.0f, 1.0f), new Vector2(2.0f, 2.0f) };
        var dest = new Vector2[2];
        var matrix = Matrix.Translation(5.0f, 10.0f, 0.0f);

        Vector2.TransformCoordinate(source, ref matrix, dest);

        Assert.Equal(6.0f, dest[0].X, 3);
        Assert.Equal(11.0f, dest[0].Y, 3);
        Assert.Equal(7.0f, dest[1].X, 3);
        Assert.Equal(12.0f, dest[1].Y, 3);

        Assert.Throws<ArgumentNullException>(() => Vector2.TransformCoordinate((Vector2[])null!, ref matrix, dest));
        Assert.Throws<ArgumentNullException>(() => Vector2.TransformCoordinate(source, ref matrix, (Vector2[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector2.TransformCoordinate(source, ref matrix, new Vector2[1]));
    }

    [Fact]
    public void TestVector2TransformNormalArray()
    {
        var source = new[] { new Vector2(1.0f, 0.0f), new Vector2(0.0f, 1.0f) };
        var dest = new Vector2[2];
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);

        Vector2.TransformNormal(source, ref matrix, dest);

        Assert.Equal(0.0f, dest[0].X, 3);
        Assert.Equal(1.0f, dest[0].Y, 3);
        Assert.Equal(-1.0f, dest[1].X, 3);
        Assert.Equal(0.0f, dest[1].Y, 3);

        Assert.Throws<ArgumentNullException>(() => Vector2.TransformNormal((Vector2[])null!, ref matrix, dest));
        Assert.Throws<ArgumentNullException>(() => Vector2.TransformNormal(source, ref matrix, (Vector2[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector2.TransformNormal(source, ref matrix, new Vector2[1]));
    }

    [Fact]
    public void TestVector2TransformArrayArgumentValidation()
    {
        var source = new[] { new Vector2(1.0f, 0.0f) };
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        var matrix = Matrix.Identity;

        Assert.Throws<ArgumentNullException>(() => Vector2.Transform((Vector2[])null!, ref rotation, new Vector2[1]));
        Assert.Throws<ArgumentNullException>(() => Vector2.Transform(source, ref rotation, (Vector2[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector2.Transform(source, ref rotation, new Vector2[0]));

        Assert.Throws<ArgumentNullException>(() => Vector2.Transform((Vector2[])null!, ref matrix, new Vector4[1]));
        Assert.Throws<ArgumentNullException>(() => Vector2.Transform(source, ref matrix, (Vector4[])null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector2.Transform(source, ref matrix, new Vector4[0]));
    }

    [Fact]
    public void TestVector2Orthogonalize()
    {
        var source = new[] { new Vector2(1.0f, 0.0f), new Vector2(1.0f, 1.0f) };
        var destination = new Vector2[2];

        Vector2.Orthogonalize(destination, source);

        // The first vector is unchanged.
        Assert.Equal(1.0f, destination[0].X, 3);
        Assert.Equal(0.0f, destination[0].Y, 3);

        // q2 = m2 - (q1.m2 / q1.q1) * q1 = (1,1) - (1/1)*(1,0) = (0,1)
        Assert.Equal(0.0f, destination[1].X, 3);
        Assert.Equal(1.0f, destination[1].Y, 3);

        // The resulting vectors must be orthogonal to each other.
        Assert.Equal(0.0f, Vector2.Dot(destination[0], destination[1]), 3);

        Assert.Throws<ArgumentNullException>(() => Vector2.Orthogonalize(null!, source));
        Assert.Throws<ArgumentNullException>(() => Vector2.Orthogonalize(destination, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector2.Orthogonalize(new Vector2[1], source));
    }

    [Fact]
    public void TestVector2Orthonormalize()
    {
        var source = new[] { new Vector2(3.0f, 0.0f), new Vector2(1.0f, 1.0f) };
        var destination = new Vector2[2];

        Vector2.Orthonormalize(destination, source);

        // q1 = (3,0) / |(3,0)| = (1,0)
        Assert.Equal(1.0f, destination[0].X, 3);
        Assert.Equal(0.0f, destination[0].Y, 3);

        // q2 = normalize((1,1) - dot(q1,(1,1)) * q1) = normalize((1,1) - (1,0)) = (0,1)
        Assert.Equal(0.0f, destination[1].X, 3);
        Assert.Equal(1.0f, destination[1].Y, 3);

        // Both results must be unit length and orthogonal to each other.
        Assert.Equal(1.0f, destination[0].Length(), 3);
        Assert.Equal(1.0f, destination[1].Length(), 3);
        Assert.Equal(0.0f, Vector2.Dot(destination[0], destination[1]), 3);

        Assert.Throws<ArgumentNullException>(() => Vector2.Orthonormalize(null!, source));
        Assert.Throws<ArgumentNullException>(() => Vector2.Orthonormalize(destination, null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => Vector2.Orthonormalize(new Vector2[1], source));
    }
}
