// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestDouble3
{
    [Fact]
    public void TestDouble3Constants()
    {
        Assert.Equal(0.0, Double3.Zero.X);
        Assert.Equal(0.0, Double3.Zero.Y);
        Assert.Equal(0.0, Double3.Zero.Z);

        Assert.Equal(1.0, Double3.One.X);
        Assert.Equal(1.0, Double3.One.Y);
        Assert.Equal(1.0, Double3.One.Z);

        Assert.Equal(1.0, Double3.UnitX.X);
        Assert.Equal(0.0, Double3.UnitX.Y);
        Assert.Equal(0.0, Double3.UnitX.Z);

        Assert.Equal(0.0, Double3.UnitY.X);
        Assert.Equal(1.0, Double3.UnitY.Y);
        Assert.Equal(0.0, Double3.UnitY.Z);

        Assert.Equal(0.0, Double3.UnitZ.X);
        Assert.Equal(0.0, Double3.UnitZ.Y);
        Assert.Equal(1.0, Double3.UnitZ.Z);
    }

    [Fact]
    public void TestDouble3Constructors()
    {
        var v1 = new Double3(5.0);
        Assert.Equal(5.0, v1.X);
        Assert.Equal(5.0, v1.Y);
        Assert.Equal(5.0, v1.Z);

        var v2 = new Double3(3.0, 4.0, 5.0);
        Assert.Equal(3.0, v2.X);
        Assert.Equal(4.0, v2.Y);
        Assert.Equal(5.0, v2.Z);

        var v3 = new Double3(new Double2(1.0, 2.0), 3.0);
        Assert.Equal(1.0, v3.X);
        Assert.Equal(2.0, v3.Y);
        Assert.Equal(3.0, v3.Z);

        var v4 = new Double3(new double[] { 1.0, 2.0, 3.0 });
        Assert.Equal(1.0, v4.X);
        Assert.Equal(2.0, v4.Y);
        Assert.Equal(3.0, v4.Z);

        var v5 = new Double3(new Vector3(7.0f, 8.0f, 9.0f));
        Assert.Equal(7.0, v5.X);
        Assert.Equal(8.0, v5.Y);
        Assert.Equal(9.0, v5.Z);
    }

    [Fact]
    public void TestDouble3Length()
    {
        var v = new Double3(2.0, 3.0, 6.0);
        Assert.Equal(7.0, v.Length());
    }

    [Fact]
    public void TestDouble3LengthSquared()
    {
        var v = new Double3(2.0, 3.0, 6.0);
        Assert.Equal(49.0, v.LengthSquared());
    }

    [Fact]
    public void TestDouble3Distance()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 6.0, 3.0);
        Assert.Equal(5.0, Double3.Distance(v1, v2));
    }

    [Fact]
    public void TestDouble3DistanceSquared()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 6.0, 3.0);
        Assert.Equal(25.0, Double3.DistanceSquared(v1, v2));
    }

    [Fact]
    public void TestDouble3Dot()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 5.0, 6.0);
        Assert.Equal(32.0, Double3.Dot(v1, v2));
    }

    [Fact]
    public void TestDouble3Cross()
    {
        var v1 = new Double3(1.0, 0.0, 0.0);
        var v2 = new Double3(0.0, 1.0, 0.0);
        var result = Double3.Cross(v1, v2);
        Assert.Equal(0.0, result.X);
        Assert.Equal(0.0, result.Y);
        Assert.Equal(1.0, result.Z);
    }

    [Fact]
    public void TestDouble3Normalize()
    {
        var v = new Double3(3.0, 0.0, 4.0);
        var normalized = Double3.Normalize(v);
        Assert.Equal(0.6, normalized.X, 5);
        Assert.Equal(0.0, normalized.Y, 5);
        Assert.Equal(0.8, normalized.Z, 5);
        Assert.Equal(1.0, normalized.Length(), 5);
    }

    [Fact]
    public void TestDouble3Add()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 5.0, 6.0);
        var result = v1 + v2;
        Assert.Equal(5.0, result.X);
        Assert.Equal(7.0, result.Y);
        Assert.Equal(9.0, result.Z);
    }

    [Fact]
    public void TestDouble3Subtract()
    {
        var v1 = new Double3(5.0, 7.0, 9.0);
        var v2 = new Double3(2.0, 3.0, 4.0);
        var result = v1 - v2;
        Assert.Equal(3.0, result.X);
        Assert.Equal(4.0, result.Y);
        Assert.Equal(5.0, result.Z);
    }

    [Fact]
    public void TestDouble3Multiply()
    {
        var v = new Double3(2.0, 3.0, 4.0);
        var result = v * 2.0;
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);
        Assert.Equal(8.0, result.Z);

        var result2 = 2.0 * v;
        Assert.Equal(4.0, result2.X);
        Assert.Equal(6.0, result2.Y);
        Assert.Equal(8.0, result2.Z);
    }

    [Fact]
    public void TestDouble3Divide()
    {
        var v = new Double3(4.0, 6.0, 8.0);
        var result = v / 2.0;
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
        Assert.Equal(4.0, result.Z);
    }

    [Fact]
    public void TestDouble3Negate()
    {
        var v = new Double3(1.0, -2.0, 3.0);
        var result = -v;
        Assert.Equal(-1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(-3.0, result.Z);
    }

    [Fact]
    public void TestDouble3Modulate()
    {
        var v1 = new Double3(2.0, 3.0, 4.0);
        var v2 = new Double3(5.0, 6.0, 7.0);
        var result = Double3.Modulate(v1, v2);
        Assert.Equal(10.0, result.X);
        Assert.Equal(18.0, result.Y);
        Assert.Equal(28.0, result.Z);
    }

    [Fact]
    public void TestDouble3Clamp()
    {
        var value = new Double3(5.0, -5.0, 15.0);
        var min = new Double3(0.0, 0.0, 0.0);
        var max = new Double3(10.0, 10.0, 10.0);
        var result = Double3.Clamp(value, min, max);
        Assert.Equal(5.0, result.X);
        Assert.Equal(0.0, result.Y);
        Assert.Equal(10.0, result.Z);
    }

    [Fact]
    public void TestDouble3Min()
    {
        var v1 = new Double3(1.0, 5.0, 3.0);
        var v2 = new Double3(3.0, 2.0, 4.0);
        var result = Double3.Min(v1, v2);
        Assert.Equal(1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(3.0, result.Z);
    }

    [Fact]
    public void TestDouble3Max()
    {
        var v1 = new Double3(1.0, 5.0, 3.0);
        var v2 = new Double3(3.0, 2.0, 4.0);
        var result = Double3.Max(v1, v2);
        Assert.Equal(3.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(4.0, result.Z);
    }

    [Fact]
    public void TestDouble3Lerp()
    {
        var start = new Double3(0.0, 0.0, 0.0);
        var end = new Double3(10.0, 10.0, 10.0);
        var result = Double3.Lerp(start, end, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(5.0, result.Z);
    }

    [Fact]
    public void TestDouble3SmoothStep()
    {
        var start = new Double3(0.0, 0.0, 0.0);
        var end = new Double3(10.0, 10.0, 10.0);
        var result = Double3.SmoothStep(start, end, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(5.0, result.Z);
    }

    [Fact]
    public void TestDouble3Barycentric()
    {
        var v1 = new Double3(0.0, 0.0, 0.0);
        var v2 = new Double3(10.0, 0.0, 0.0);
        var v3 = new Double3(0.0, 10.0, 0.0);
        var result = Double3.Barycentric(v1, v2, v3, 0.5, 0.5);
        Assert.Equal(5.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(0.0, result.Z);
    }

    [Fact]
    public void TestDouble3CatmullRom()
    {
        var v1 = new Double3(0.0, 0.0, 0.0);
        var v2 = new Double3(1.0, 1.0, 1.0);
        var v3 = new Double3(2.0, 2.0, 2.0);
        var v4 = new Double3(3.0, 3.0, 3.0);
        var result = Double3.CatmullRom(v1, v2, v3, v4, 0.5);
        Assert.Equal(1.5, result.X, 5);
        Assert.Equal(1.5, result.Y, 5);
        Assert.Equal(1.5, result.Z, 5);
    }

    [Fact]
    public void TestDouble3Hermite()
    {
        var v1 = new Double3(0.0, 0.0, 0.0);
        var t1 = new Double3(1.0, 1.0, 1.0);
        var v2 = new Double3(2.0, 2.0, 2.0);
        var t2 = new Double3(1.0, 1.0, 1.0);
        var result = Double3.Hermite(v1, t1, v2, t2, 0.5);
        Assert.Equal(1.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(1.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3Reflect()
    {
        var vector = new Double3(1.0, -1.0, 0.0);
        var normal = new Double3(0.0, 1.0, 0.0);
        var result = Double3.Reflect(vector, normal);
        Assert.Equal(1.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(0.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3TransformByQuaternion()
    {
        var vector = new Double3(1.0, 0.0, 0.0);
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        var result = Double3.Transform(vector, rotation);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(0.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3TransformCoordinate()
    {
        var vector = new Double3(1.0, 0.0, 0.0);
        var matrix = Matrix.Translation(1.0f, 2.0f, 3.0f);
        var result = Double3.TransformCoordinate(vector, matrix);
        Assert.Equal(2.0, result.X, 5);
        Assert.Equal(2.0, result.Y, 5);
        Assert.Equal(3.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3TransformNormal()
    {
        var normal = new Double3(1.0, 0.0, 0.0);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        var result = Double3.TransformNormal(normal, matrix);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(0.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3Equality()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(1.0, 2.0, 3.0);
        var v3 = new Double3(4.0, 5.0, 6.0);

        Assert.True(v1 == v2);
        Assert.False(v1 == v3);
        Assert.False(v1 != v2);
        Assert.True(v1 != v3);
        Assert.True(v1.Equals(v2));
        Assert.False(v1.Equals(v3));
    }

    [Fact]
    public void TestDouble3HashCode()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(1.0, 2.0, 3.0);
        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }

    [Fact]
    public void TestDouble3ToString()
    {
        var v = new Double3(1.5, 2.5, 3.5);
        var str = v.ToString();
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("3", str);
        Assert.Contains("5", str);
    }

    [Fact]
    public void TestDouble3ConversionFromVector3()
    {
        var v = new Vector3(1.0f, 2.0f, 3.0f);
        Double3 d = v;
        Assert.Equal(1.0, d.X);
        Assert.Equal(2.0, d.Y);
        Assert.Equal(3.0, d.Z);
    }

    [Fact]
    public void TestDouble3ConversionToVector3()
    {
        var d = new Double3(1.0, 2.0, 3.0);
        Vector3 v = (Vector3)d;
        Assert.Equal(1.0f, v.X);
        Assert.Equal(2.0f, v.Y);
        Assert.Equal(3.0f, v.Z);
    }

    [Fact]
    public void TestDouble3Indexer()
    {
        var v = new Double3(3.0, 4.0, 5.0);
        Assert.Equal(3.0, v[0]);
        Assert.Equal(4.0, v[1]);
        Assert.Equal(5.0, v[2]);

        v[0] = 6.0;
        v[1] = 7.0;
        v[2] = 8.0;
        Assert.Equal(6.0, v.X);
        Assert.Equal(7.0, v.Y);
        Assert.Equal(8.0, v.Z);
    }

    [Fact]
    public void TestDouble3IndexerOutOfRange()
    {
        var v = new Double3(1.0, 2.0, 3.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[3]);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[-1] = 0.0);
        Assert.Throws<ArgumentOutOfRangeException>(() => v[3] = 0.0);
    }

    [Fact]
    public void TestDouble3IsNormalized()
    {
        var normalized = new Double3(1.0, 0.0, 0.0);
        Assert.True(normalized.IsNormalized);

        var notNormalized = new Double3(3.0, 4.0, 5.0);
        Assert.False(notNormalized.IsNormalized);

        var zero = Double3.Zero;
        Assert.False(zero.IsNormalized);
    }

    [Fact]
    public void TestDouble3ToArray()
    {
        var v = new Double3(1.5, 2.5, 3.5);
        var array = v.ToArray();
        Assert.Equal(3, array.Length);
        Assert.Equal(1.5, array[0]);
        Assert.Equal(2.5, array[1]);
        Assert.Equal(3.5, array[2]);
    }

    [Fact]
    public void TestDouble3NormalizeInstance()
    {
        var v = new Double3(3.0, 4.0, 0.0);
        v.Normalize();
        Assert.Equal(0.6, v.X, 5);
        Assert.Equal(0.8, v.Y, 5);
        Assert.Equal(0.0, v.Z, 5);
        Assert.Equal(1.0, v.Length(), 5);
    }

    [Fact]
    public void TestDouble3CrossWithOutParameter()
    {
        var v1 = new Double3(1.0, 0.0, 0.0);
        var v2 = new Double3(0.0, 1.0, 0.0);
        Double3.Cross(ref v1, ref v2, out var result);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(0.0, result.Y, 5);
        Assert.Equal(1.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3NormalizeWithOutParameter()
    {
        var v = new Double3(3.0, 4.0, 0.0);
        Double3.Normalize(ref v, out var result);
        Assert.Equal(0.6, result.X, 5);
        Assert.Equal(0.8, result.Y, 5);
    }

    [Fact]
    public void TestDouble3TransformByQuaternionWithOutParameter()
    {
        var vector = new Double3(1.0, 0.0, 0.0);
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        Double3.Transform(ref vector, ref rotation, out var result);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(0.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3TransformCoordinateWithOutParameter()
    {
        var vector = new Double3(1.0, 0.0, 0.0);
        var matrix = Matrix.Translation(1.0f, 2.0f, 3.0f);
        Double3.TransformCoordinate(ref vector, ref matrix, out var result);
        Assert.Equal(2.0, result.X, 5);
        Assert.Equal(2.0, result.Y, 5);
        Assert.Equal(3.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3TransformNormalWithOutParameter()
    {
        var normal = new Double3(1.0, 0.0, 0.0);
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        Double3.TransformNormal(ref normal, ref matrix, out var result);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(1.0, result.Y, 5);
        Assert.Equal(0.0, result.Z, 5);
    }

    [Fact]
    public void TestDouble3TransformArrayByQuaternion()
    {
        var source = new[] { new Double3(1.0, 0.0, 0.0), new Double3(0.0, 1.0, 0.0) };
        var destination = new Double3[2];
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);

        Double3.Transform(source, ref rotation, destination);

        Assert.Equal(0.0, destination[0].X, 5);
        Assert.Equal(1.0, destination[0].Y, 5);
        Assert.Equal(-1.0, destination[1].X, 5);
        Assert.Equal(0.0, destination[1].Y, 5);
    }

    [Fact]
    public void TestDouble3TransformCoordinateArray()
    {
        var source = new[] { new Double3(1.0, 0.0, 0.0), new Double3(0.0, 1.0, 0.0) };
        var destination = new Double3[2];
        var matrix = Matrix.Translation(1.0f, 2.0f, 3.0f);

        Double3.TransformCoordinate(source, ref matrix, destination);

        Assert.Equal(2.0, destination[0].X, 5);
        Assert.Equal(2.0, destination[0].Y, 5);
        Assert.Equal(1.0, destination[1].X, 5);
        Assert.Equal(3.0, destination[1].Y, 5);
    }

    [Fact]
    public void TestDouble3TransformNormalArray()
    {
        var source = new[] { new Double3(1.0, 0.0, 0.0), new Double3(0.0, 1.0, 0.0) };
        var destination = new Double3[2];
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);

        Double3.TransformNormal(source, ref matrix, destination);

        Assert.Equal(0.0, destination[0].X, 5);
        Assert.Equal(1.0, destination[0].Y, 5);
        Assert.Equal(-1.0, destination[1].X, 5);
        Assert.Equal(0.0, destination[1].Y, 5);
    }

    [Fact]
    public void TestDouble3Deconstruct()
    {
        var v = new Double3(1.5, 2.5, 3.5);
        var (x, y, z) = v;
        Assert.Equal(1.5, x);
        Assert.Equal(2.5, y);
        Assert.Equal(3.5, z);
    }

    [Fact]
    public void TestDouble3Demodulate()
    {
        var v1 = new Double3(12.0, 20.0, 30.0);
        var v2 = new Double3(3.0, 4.0, 5.0);
        var result = Double3.Demodulate(v1, v2);
        Assert.Equal(4.0, result.X);
        Assert.Equal(5.0, result.Y);
        Assert.Equal(6.0, result.Z);
    }

    [Fact]
    public void TestDouble3UnaryPlus()
    {
        var v = new Double3(1.0, 2.0, 3.0);
        var result = +v;
        Assert.Equal(1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(3.0, result.Z);
    }

    [Fact]
    public void TestDouble3ScalarMultiplyLeft()
    {
        var v = new Double3(2.0, 3.0, 4.0);
        var result = 3.0 * v;
        Assert.Equal(6.0, result.X);
        Assert.Equal(9.0, result.Y);
        Assert.Equal(12.0, result.Z);
    }

    [Fact]
    public void TestDouble3ScalarDivideRight()
    {
        var v = new Double3(12.0, 18.0, 24.0);
        var result = v / 3.0;
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);
        Assert.Equal(8.0, result.Z);
    }
}




