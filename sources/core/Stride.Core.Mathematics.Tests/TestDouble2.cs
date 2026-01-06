// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestDouble2
{
    [Fact]
    public void TestDouble2Constants()
    {
        Assert.Equal(0.0, Double2.Zero.X);
        Assert.Equal(0.0, Double2.Zero.Y);

        Assert.Equal(1.0, Double2.One.X);
        Assert.Equal(1.0, Double2.One.Y);

        Assert.Equal(1.0, Double2.UnitX.X);
        Assert.Equal(0.0, Double2.UnitX.Y);

        Assert.Equal(0.0, Double2.UnitY.X);
        Assert.Equal(1.0, Double2.UnitY.Y);
    }

    [Fact]
    public void TestDouble2Constructors()
    {
        var v1 = new Double2(5.0);
        Assert.Equal(5.0, v1.X);
        Assert.Equal(5.0, v1.Y);

        var v2 = new Double2(3.0, 4.0);
        Assert.Equal(3.0, v2.X);
        Assert.Equal(4.0, v2.Y);

        var v3 = new Double2(new double[] { 1.0, 2.0 });
        Assert.Equal(1.0, v3.X);
        Assert.Equal(2.0, v3.Y);

        var v4 = new Double2(new Vector2(7.0f, 8.0f));
        Assert.Equal(7.0, v4.X);
        Assert.Equal(8.0, v4.Y);
    }

    [Fact]
    public void TestDouble2Length()
    {
        var v = new Double2(3.0, 4.0);
        Assert.Equal(5.0, v.Length());
    }

    [Fact]
    public void TestDouble2LengthSquared()
    {
        var v = new Double2(3.0, 4.0);
        Assert.Equal(25.0, v.LengthSquared());
    }

    [Fact]
    public void TestDouble2Distance()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(4.0, 6.0);
        Assert.Equal(5.0, Double2.Distance(v1, v2));
    }

    [Fact]
    public void TestDouble2DistanceSquared()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(4.0, 6.0);
        Assert.Equal(25.0, Double2.DistanceSquared(v1, v2));
    }

    [Fact]
    public void TestDouble2Dot()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(3.0, 4.0);
        Assert.Equal(11.0, Double2.Dot(v1, v2));
    }

    [Fact]
    public void TestDouble2Normalize()
    {
        var v = new Double2(3.0, 4.0);
        var normalized = Double2.Normalize(v);
        Assert.Equal(0.6, normalized.X, 5);
        Assert.Equal(0.8, normalized.Y, 5);
        Assert.Equal(1.0, normalized.Length(), 5);
    }

    [Fact]
    public void TestDouble2Add()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(3.0, 4.0);
        var result = v1 + v2;
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);
    }

    [Fact]
    public void TestDouble2Subtract()
    {
        var v1 = new Double2(5.0, 7.0);
        var v2 = new Double2(2.0, 3.0);
        var result = v1 - v2;
        Assert.Equal(3.0, result.X);
        Assert.Equal(4.0, result.Y);
    }

    [Fact]
    public void TestDouble2Multiply()
    {
        var v = new Double2(2.0, 3.0);
        var result = v * 2.0;
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);

        var result2 = 2.0 * v;
        Assert.Equal(4.0, result2.X);
        Assert.Equal(6.0, result2.Y);
    }

    [Fact]
    public void TestDouble2Divide()
    {
        var v = new Double2(4.0, 6.0);
        var result = v / 2.0;
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
    }

    [Fact]
    public void TestDouble2Negate()
    {
        var v = new Double2(1.0, -2.0);
        var result = -v;
        Assert.Equal(-1.0, result.X);
        Assert.Equal(2.0, result.Y);
    }

    [Fact]
    public void TestDouble2Modulate()
    {
        var v1 = new Double2(2.0, 3.0);
        var v2 = new Double2(4.0, 5.0);
        var result = Double2.Modulate(v1, v2);
        Assert.Equal(8.0, result.X);
        Assert.Equal(15.0, result.Y);
    }

    [Fact]
    public void TestDouble2Demodulate()
    {
        var v1 = new Double2(8.0, 15.0);
        var v2 = new Double2(4.0, 5.0);
        var result = Double2.Demodulate(v1, v2);
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
    }

    [Fact]
    public void TestDouble2Clamp()
    {
        var value = new Double2(5.0, -5.0);
        var min = new Double2(0.0, 0.0);
        var max = new Double2(10.0, 10.0);
        var result = Double2.Clamp(value, min, max);
        Assert.Equal(5.0, result.X);
        Assert.Equal(0.0, result.Y);
    }

    [Fact]
    public void TestDouble2Min()
    {
        var v1 = new Double2(1.0, 5.0);
        var v2 = new Double2(3.0, 2.0);
        var result = Double2.Min(v1, v2);
        Assert.Equal(1.0, result.X);
        Assert.Equal(2.0, result.Y);
    }

    [Fact]
    public void TestDouble2Max()
    {
        var v1 = new Double2(1.0, 5.0);
        var v2 = new Double2(3.0, 2.0);
        var result = Double2.Max(v1, v2);
        Assert.Equal(3.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2Lerp()
    {
        var start = new Double2(0.0, 0.0);
        var end = new Double2(10.0, 10.0);
        var result = Double2.Lerp(start, end, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2SmoothStep()
    {
        var start = new Double2(0.0, 0.0);
        var end = new Double2(10.0, 10.0);
        var result = Double2.SmoothStep(start, end, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2Barycentric()
    {
        var v1 = new Double2(0.0, 0.0);
        var v2 = new Double2(10.0, 0.0);
        var v3 = new Double2(0.0, 10.0);
        var result = Double2.Barycentric(v1, v2, v3, 0.5, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2CatmullRom()
    {
        var v1 = new Double2(0.0, 0.0);
        var v2 = new Double2(1.0, 1.0);
        var v3 = new Double2(2.0, 2.0);
        var v4 = new Double2(3.0, 3.0);
        var result = Double2.CatmullRom(v1, v2, v3, v4, 0.5);
        Assert.Equal(1.5, result.X, 5);
        Assert.Equal(1.5, result.Y, 5);
    }

    [Fact]
    public void TestDouble2Hermite()
    {
        var v1 = new Double2(0.0, 0.0);
        var t1 = new Double2(1.0, 1.0);
        var v2 = new Double2(2.0, 2.0);
        var t2 = new Double2(1.0, 1.0);
        var result = Double2.Hermite(v1, t1, v2, t2, 0.5);
        Assert.Equal(1.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2Reflect()
    {
        var vector = new Double2(1.0, -1.0);
        var normal = new Double2(0.0, 1.0);
        var result = Double2.Reflect(vector, normal);
        Assert.Equal(1.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2TransformByQuaternion()
    {
        var vector = new Double2(1.0, 0.0);
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        var result = Double2.Transform(vector, rotation);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2TransformCoordinate()
    {
        var vector = new Double2(1.0, 0.0);
        var matrix = Matrix.Translation(1.0f, 2.0f, 0.0f);
        var result = Double2.TransformCoordinate(vector, matrix);
        Assert.Equal(2.0, result.X, 5);
        Assert.Equal(2.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2TransformNormal()
    {
        var normal = new Double2(1.0, 0.0);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        var result = Double2.TransformNormal(normal, matrix);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2Equality()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(1.0, 2.0);
        var v3 = new Double2(3.0, 4.0);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);
        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestDouble2HashCode()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(1.0, 2.0);
        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }

    [Fact]
    public void TestDouble2ToString()
    {
        var v = new Double2(1.5, 2.5);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("5", str);
    }

    [Fact]
    public void TestDouble2ConversionFromVector2()
    {
        var v = new Vector2(1.0f, 2.0f);
        Double2 d = v;
        Assert.Equal(1.0, d.X);
        Assert.Equal(2.0, d.Y);
    }

    [Fact]
    public void TestDouble2ConversionToVector2()
    {
        var d = new Double2(1.0, 2.0);
        Vector2 v = (Vector2)d;
        Assert.Equal(1.0f, v.X);
        Assert.Equal(2.0f, v.Y);
    }

    [Fact]
    public void TestDouble2Indexer()
    {
        var v = new Double2(3.0, 4.0);
        Assert.Equal(3.0, v[0]);
        Assert.Equal(4.0, v[1]);

        v[0] = 5.0;
        v[1] = 6.0;
        Assert.Equal(5.0, v.X);
        Assert.Equal(6.0, v.Y);
    }

    [Fact]
    public void TestDouble2IndexerOutOfRange()
    {
        var v = new Double2(1.0, 2.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[2]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[-1] = 0.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[2] = 0.0);
    }

    [Fact]
    public void TestDouble2IsNormalized()
    {
        var normalized = new Double2(0.6, 0.8);
        Assert.True(normalized.IsNormalized);

        var notNormalized = new Double2(3.0, 4.0);
        Assert.False(notNormalized.IsNormalized);

        var zero = Double2.Zero;
        Assert.False(zero.IsNormalized);
    }

    [Fact]
    public void TestDouble2ToArray()
    {
        var v = new Double2(1.5, 2.5);
        var array = v.ToArray();
        Assert.Equal(2, array.Length);
        Assert.Equal(1.5, array[0]);
        Assert.Equal(2.5, array[1]);
    }

    [Fact]
    public void TestDouble2NormalizeInstance()
    {
        var v = new Double2(3.0, 4.0);
        v.Normalize();
        Assert.Equal(0.6, v.X, 5);
        Assert.Equal(0.8, v.Y, 5);
        Assert.Equal(1.0, v.Length(), 5);
    }

    [Fact]
    public void TestDouble2AddWithOutParameter()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(3.0, 4.0);
        Double2.Add(ref v1, ref v2, out var result);
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);
    }

    [Fact]
    public void TestDouble2SubtractWithOutParameter()
    {
        var v1 = new Double2(5.0, 7.0);
        var v2 = new Double2(2.0, 3.0);
        Double2.Subtract(ref v1, ref v2, out var result);
        Assert.Equal(3.0, result.X);
        Assert.Equal(4.0, result.Y);
    }

    [Fact]
    public void TestDouble2MultiplyWithOutParameter()
    {
        var v = new Double2(2.0, 3.0);
        Double2.Multiply(ref v, 2.0, out var result);
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);
    }

    [Fact]
    public void TestDouble2DivideWithOutParameter()
    {
        var v = new Double2(4.0, 6.0);
        Double2.Divide(ref v, 2.0, out var result);
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
    }

    [Fact]
    public void TestDouble2NegateWithOutParameter()
    {
        var v = new Double2(1.0, -2.0);
        Double2.Negate(ref v, out var result);
        Assert.Equal(-1.0, result.X);
        Assert.Equal(2.0, result.Y);
    }

    [Fact]
    public void TestDouble2ModulateWithOutParameter()
    {
        var v1 = new Double2(2.0, 3.0);
        var v2 = new Double2(4.0, 5.0);
        Double2.Modulate(ref v1, ref v2, out var result);
        Assert.Equal(8.0, result.X);
        Assert.Equal(15.0, result.Y);
    }

    [Fact]
    public void TestDouble2DemodulateWithOutParameter()
    {
        var v1 = new Double2(8.0, 15.0);
        var v2 = new Double2(4.0, 5.0);
        Double2.Demodulate(ref v1, ref v2, out var result);
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
    }

    [Fact]
    public void TestDouble2ClampWithOutParameter()
    {
        var value = new Double2(5.0, -5.0);
        var min = new Double2(0.0, 0.0);
        var max = new Double2(10.0, 10.0);
        Double2.Clamp(ref value, ref min, ref max, out var result);
        Assert.Equal(5.0, result.X);
        Assert.Equal(0.0, result.Y);
    }

    [Fact]
    public void TestDouble2MinWithOutParameter()
    {
        var v1 = new Double2(1.0, 5.0);
        var v2 = new Double2(3.0, 2.0);
        Double2.Min(ref v1, ref v2, out var result);
        Assert.Equal(1.0, result.X);
        Assert.Equal(2.0, result.Y);
    }

    [Fact]
    public void TestDouble2MaxWithOutParameter()
    {
        var v1 = new Double2(1.0, 5.0);
        var v2 = new Double2(3.0, 2.0);
        Double2.Max(ref v1, ref v2, out var result);
        Assert.Equal(3.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2LerpWithOutParameter()
    {
        var start = new Double2(0.0, 0.0);
        var end = new Double2(10.0, 10.0);
        Double2.Lerp(ref start, ref end, 0.5, out var result);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2SmoothStepWithOutParameter()
    {
        var start = new Double2(0.0, 0.0);
        var end = new Double2(10.0, 10.0);
        Double2.SmoothStep(ref start, ref end, 0.5, out var result);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2BarycentricWithOutParameter()
    {
        var v1 = new Double2(0.0, 0.0);
        var v2 = new Double2(10.0, 0.0);
        var v3 = new Double2(0.0, 10.0);
        Double2.Barycentric(ref v1, ref v2, ref v3, 0.5, 0.5, out var result);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
    }

    [Fact]
    public void TestDouble2CatmullRomWithOutParameter()
    {
        var v1 = new Double2(0.0, 0.0);
        var v2 = new Double2(1.0, 1.0);
        var v3 = new Double2(2.0, 2.0);
        var v4 = new Double2(3.0, 3.0);
        Double2.CatmullRom(ref v1, ref v2, ref v3, ref v4, 0.5, out var result);
        Assert.Equal(1.5, result.X, 5);
        Assert.Equal(1.5, result.Y, 5);
    }

    [Fact]
    public void TestDouble2HermiteWithOutParameter()
    {
        var v1 = new Double2(0.0, 0.0);
        var t1 = new Double2(1.0, 1.0);
        var v2 = new Double2(2.0, 2.0);
        var t2 = new Double2(1.0, 1.0);
        Double2.Hermite(ref v1, ref t1, ref v2, ref t2, 0.5, out var result);
        Assert.Equal(1.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2ReflectWithOutParameter()
    {
        var vector = new Double2(1.0, -1.0);
        var normal = new Double2(0.0, 1.0);
        Double2.Reflect(ref vector, ref normal, out var result);
        Assert.Equal(1.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2TransformByQuaternionWithOutParameter()
    {
        var vector = new Double2(1.0, 0.0);
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        Double2.Transform(ref vector, ref rotation, out var result);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2TransformCoordinateWithOutParameter()
    {
        var vector = new Double2(1.0, 0.0);
        var matrix = Matrix.Translation(1.0f, 2.0f, 0.0f);
        Double2.TransformCoordinate(ref vector, ref matrix, out var result);
        Assert.Equal(2.0, result.X, 5);
        Assert.Equal(2.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2TransformNormalWithOutParameter()
    {
        var normal = new Double2(1.0, 0.0);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        Double2.TransformNormal(ref normal, ref matrix, out var result);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
    }

    [Fact]
    public void TestDouble2NormalizeWithOutParameter()
    {
        var v = new Double2(3.0, 4.0);
        Double2.Normalize(ref v, out var result);
        Assert.Equal(0.6, result.X, 5);
        Assert.Equal(0.8, result.Y, 5);
    }

    [Fact]
    public void TestDouble2DistanceWithOutParameter()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(4.0, 6.0);
        Double2.Distance(ref v1, ref v2, out var result);
        Assert.Equal(5.0, result);
    }

    [Fact]
    public void TestDouble2DistanceSquaredWithOutParameter()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(4.0, 6.0);
        Double2.DistanceSquared(ref v1, ref v2, out var result);
        Assert.Equal(25.0, result);
    }

    [Fact]
    public void TestDouble2DotWithOutParameter()
    {
        var v1 = new Double2(1.0, 2.0);
        var v2 = new Double2(3.0, 4.0);
        Double2.Dot(ref v1, ref v2, out var result);
        Assert.Equal(11.0, result);
    }

    [Fact]
    public void TestDouble2TransformArrayByQuaternion()
    {
        var source = new[] { new Double2(1.0, 0.0), new Double2(0.0, 1.0) };
        var destination = new Double2[2];
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);

        Double2.Transform(source, ref rotation, destination);

        Assert.Equal(0.0, destination[0].X, 5);
        Assert.Equal(1.0, destination[0].Y, 5);
        Assert.Equal(-1.0, destination[1].X, 5);
        Assert.Equal(0.0, destination[1].Y, 5);
    }

    [Fact]
    public void TestDouble2TransformCoordinateArray()
    {
        var source = new[] { new Double2(1.0, 0.0), new Double2(0.0, 1.0) };
        var destination = new Double2[2];
        var matrix = Matrix.Translation(1.0f, 2.0f, 0.0f);

        Double2.TransformCoordinate(source, ref matrix, destination);

        Assert.Equal(2.0, destination[0].X, 5);
        Assert.Equal(2.0, destination[0].Y, 5);
        Assert.Equal(1.0, destination[1].X, 5);
        Assert.Equal(3.0, destination[1].Y, 5);
    }

    [Fact]
    public void TestDouble2TransformNormalArray()
    {
        var source = new[] { new Double2(1.0, 0.0), new Double2(0.0, 1.0) };
        var destination = new Double2[2];
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);

        Double2.TransformNormal(source, ref matrix, destination);

        Assert.Equal(0.0, destination[0].X, 5);
        Assert.Equal(1.0, destination[0].Y, 5);
        Assert.Equal(-1.0, destination[1].X, 5);
        Assert.Equal(0.0, destination[1].Y, 5);
    }

    [Fact]
    public void TestDouble2Deconstruct()
    {
        var v = new Double2(1.5, 2.5);
        var (x, y) = v;
        Assert.Equal(1.5, x);
        Assert.Equal(2.5, y);
    }
}

