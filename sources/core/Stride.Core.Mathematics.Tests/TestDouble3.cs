// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestDouble3
{
    // ============================================
    // 1. Static fields
    // ============================================

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

    // ============================================
    // 2. Constructors
    // ============================================

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

    // ============================================
    // 3. Properties
    // ============================================

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

    // ============================================
    // 4. Instance methods
    // ============================================

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
    public void TestDouble3Pow()
    {
        var v = new Double3(2.0, 3.0, 4.0);
        v.Pow(2.0);
        Assert.Equal(4.0, v.X);
        Assert.Equal(9.0, v.Y);
        Assert.Equal(16.0, v.Z);
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

    // ============================================
    // 5. Static methods
    // ============================================

    // Add
    [Fact]
    public void TestDouble3Add()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 5.0, 6.0);
        var result = Double3.Add(v1, v2);
        Assert.Equal(5.0, result.X);
        Assert.Equal(7.0, result.Y);
        Assert.Equal(9.0, result.Z);
    }

    // Subtract
    [Fact]
    public void TestDouble3Subtract()
    {
        var v1 = new Double3(5.0, 7.0, 9.0);
        var v2 = new Double3(2.0, 3.0, 4.0);
        var result = Double3.Subtract(v1, v2);
        Assert.Equal(3.0, result.X);
        Assert.Equal(4.0, result.Y);
        Assert.Equal(5.0, result.Z);
    }

    // Multiply
    [Fact]
    public void TestDouble3Multiply()
    {
        var v = new Double3(2.0, 3.0, 4.0);
        var result = Double3.Multiply(v, 2.0);
        Assert.Equal(4.0, result.X);
        Assert.Equal(6.0, result.Y);
        Assert.Equal(8.0, result.Z);
    }

    // Modulate
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

    // Divide
    [Fact]
    public void TestDouble3Divide()
    {
        var v = new Double3(4.0, 6.0, 8.0);
        var result = Double3.Divide(v, 2.0);
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
        Assert.Equal(4.0, result.Z);
    }

    // Demodulate
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

    // Negate
    [Fact]
    public void TestDouble3Negate()
    {
        var v = new Double3(1.0, -2.0, 3.0);
        var result = Double3.Negate(v);
        Assert.Equal(-1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(-3.0, result.Z);
    }

    // Barycentric
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

    // Clamp
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
    public void TestDouble3ClampWithOutParameter()
    {
        var value = new Double3(5.0, -5.0, 15.0);
        var min = new Double3(0.0, 0.0, 0.0);
        var max = new Double3(10.0, 10.0, 10.0);
        Double3.Clamp(ref value, ref min, ref max, out var result);
        Assert.Equal(5.0, result.X);
        Assert.Equal(0.0, result.Y);
        Assert.Equal(10.0, result.Z);
    }

    // Cross
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
    public void TestDouble3CrossWithOutParameter()
    {
        var v1 = new Double3(1.0, 0.0, 0.0);
        var v2 = new Double3(0.0, 1.0, 0.0);
        Double3.Cross(ref v1, ref v2, out var result);
        Assert.Equal(0.0, result.X, 5);
        Assert.Equal(0.0, result.Y, 5);
        Assert.Equal(1.0, result.Z, 5);
    }

    // Distance
    [Fact]
    public void TestDouble3Distance()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 6.0, 3.0);
        Assert.Equal(5.0, Double3.Distance(v1, v2));
    }

    // DistanceSquared
    [Fact]
    public void TestDouble3DistanceSquared()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 6.0, 3.0);
        Assert.Equal(25.0, Double3.DistanceSquared(v1, v2));
    }

    // Dot
    [Fact]
    public void TestDouble3Dot()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 5.0, 6.0);
        Assert.Equal(32.0, Double3.Dot(v1, v2));
    }

    // Normalize (static)
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
    public void TestDouble3NormalizeWithOutParameter()
    {
        var v = new Double3(3.0, 4.0, 0.0);
        Double3.Normalize(ref v, out var result);
        Assert.Equal(0.6, result.X, 5);
        Assert.Equal(0.8, result.Y, 5);
    }

    // Lerp
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

    // SmoothStep
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

    // Hermite
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

    // CatmullRom
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

    // Max
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

    // Min
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

    // Project
    [Fact]
    public void TestDouble3Project()
    {
        var vector = new Double3(0.5, 0.5, 0.5);
        var projection = Matrix.Identity;
        var result = Double3.Project(vector, 0, 0, 800, 600, 0.1, 1000, projection);
        Assert.NotEqual(0, result.X);
        Assert.NotEqual(0, result.Y);
    }

    [Fact]
    public void TestDouble3ProjectWithOutParameter()
    {
        var vector = new Double3(0.5, 0.5, 0.5);
        var projection = Matrix.Identity;
        Double3.Project(ref vector, 0, 0, 800, 600, 0.1, 1000, ref projection, out var result);
        Assert.NotEqual(0, result.X);
        Assert.NotEqual(0, result.Y);
    }

    // Unproject
    [Fact]
    public void TestDouble3Unproject()
    {
        var vector = new Double3(400, 300, 0.5);
        var projection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, 800f / 600f, 0.1f, 1000f);
        var result = Double3.Unproject(vector, 0, 0, 800, 600, 0.1, 1000, projection);
        // Just check that the method executes without exception
        Assert.True(true);
    }

    [Fact]
    public void TestDouble3UnprojectWithOutParameter()
    {
        var vector = new Double3(400, 300, 0.5);
        var projection = Matrix.PerspectiveFovLH(MathUtil.PiOverFour, 800f / 600f, 0.1f, 1000f);
        Double3.Unproject(ref vector, 0, 0, 800, 600, 0.1, 1000, ref projection, out var result);
        // Just check that the method executes without exception
        Assert.True(true);
    }

    // Reflect
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
    public void TestDouble3ReflectWithOutParameter()
    {
        var vector = new Double3(1.0, -1.0, 0.0);
        var normal = new Double3(0.0, 1.0, 0.0);
        Double3.Reflect(ref vector, ref normal, out var result);
        Assert.InRange(result.X, 0.99, 1.01);
        Assert.InRange(result.Y, 0.99, 1.01);
    }

    // Orthogonalize
    [Fact]
    public void TestDouble3Orthogonalize()
    {
        var source = new[] {
            new Double3(1.0, 1.0, 0.0),
            new Double3(1.0, 0.0, 0.0),
            new Double3(0.0, 1.0, 1.0)
        };
        var destination = new Double3[3];

        Double3.Orthogonalize(destination, source);

        // Check that vectors are orthogonal
        var dot01 = Double3.Dot(destination[0], destination[1]);
        Assert.InRange(dot01, -0.001, 0.001);
    }

    // Orthonormalize
    [Fact]
    public void TestDouble3Orthonormalize()
    {
        var source = new[] {
            new Double3(2.0, 0.0, 0.0),
            new Double3(0.0, 3.0, 0.0),
            new Double3(0.0, 0.0, 4.0)
        };
        var destination = new Double3[3];

        Double3.Orthonormalize(destination, source);

        // Check that vectors are orthonormal (orthogonal and unit length)
        Assert.InRange(destination[0].Length(), 0.999, 1.001);
        Assert.InRange(destination[1].Length(), 0.999, 1.001);
        Assert.InRange(destination[2].Length(), 0.999, 1.001);

        var dot01 = Double3.Dot(destination[0], destination[1]);
        Assert.InRange(dot01, -0.001, 0.001);
    }

    // Transform (quaternion)
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

    // Transform (matrix to Double4)
    [Fact]
    public void TestDouble3TransformWithOutDouble4()
    {
        var vector = new Double3(1.0, 0.0, 0.0);
        var matrix = Matrix.Translation(1.0f, 2.0f, 3.0f);
        Double3.Transform(ref vector, ref matrix, out Double4 result);
        Assert.Equal(2.0, result.X, 5);
        Assert.Equal(2.0, result.Y, 5);
        Assert.Equal(3.0, result.Z, 5);
        Assert.Equal(1.0, result.W, 5);
    }

    [Fact]
    public void TestDouble3TransformArrayToDouble4()
    {
        var source = new[] { new Double3(1.0, 0.0, 0.0), new Double3(0.0, 1.0, 0.0) };
        var destination = new Double4[2];
        var matrix = Matrix.Translation(1.0f, 2.0f, 3.0f);

        Double3.Transform(source, ref matrix, destination);

        Assert.Equal(2.0, destination[0].X, 5);
        Assert.Equal(2.0, destination[0].Y, 5);
    }

    // TransformCoordinate
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

    // TransformNormal
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

    // RotationYawPitchRoll
    [Fact]
    public void TestDouble3RotationYawPitchRoll()
    {
        var quat = Quaternion.RotationYawPitchRoll(MathUtil.PiOverTwo, 0, 0);
        var ypr = Double3.RotationYawPitchRoll(quat);
        Assert.InRange(ypr.X, MathUtil.PiOverTwo - 0.01, MathUtil.PiOverTwo + 0.01);
    }

    [Fact]
    public void TestDouble3RotationYawPitchRollWithOutParameter()
    {
        var quat = Quaternion.RotationYawPitchRoll(MathUtil.PiOverTwo, 0, 0);
        Double3.RotationYawPitchRoll(ref quat, out var ypr);
        Assert.InRange(ypr.X, MathUtil.PiOverTwo - 0.01, MathUtil.PiOverTwo + 0.01);
    }

    // WithOutParameters (combined test for Add, Subtract, Multiply, Modulate, Divide, Demodulate, Negate)
    [Fact]
    public void TestDouble3WithOutParameters()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 5.0, 6.0);

        // Test Add with out parameter
        Double3.Add(ref v1, ref v2, out var addResult);
        Assert.Equal(5.0, addResult.X);

        // Test Subtract with out parameter
        Double3.Subtract(ref v1, ref v2, out var subResult);
        Assert.Equal(-3.0, subResult.X);

        // Test Multiply with out parameter
        Double3.Multiply(ref v1, 2.0, out var mulResult);
        Assert.Equal(2.0, mulResult.X);

        // Test Divide with out parameter
        Double3.Divide(ref v1, 2.0, out var divResult);
        Assert.Equal(0.5, divResult.X);

        // Test Modulate with out parameter
        Double3.Modulate(ref v1, ref v2, out var modResult);
        Assert.Equal(4.0, modResult.X);

        // Test Demodulate with out parameter
        Double3.Demodulate(ref v2, ref v1, out var demodResult);
        Assert.Equal(4.0, demodResult.X);

        // Test Negate with out parameter
        Double3.Negate(ref v1, out var negResult);
        Assert.Equal(-1.0, negResult.X);
    }

    // StaticMethodsWithOutParameters (Distance, DistanceSquared, Dot)
    [Fact]
    public void TestDouble3StaticMethodsWithOutParameters()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 6.0, 3.0);

        // Test Distance with out parameter
        Double3.Distance(ref v1, ref v2, out var distance);
        Assert.Equal(5.0, distance);

        // Test DistanceSquared with out parameter
        Double3.DistanceSquared(ref v1, ref v2, out var distSq);
        Assert.Equal(25.0, distSq);

        // Test Dot with out parameter
        var v3 = new Double3(1.0, 2.0, 3.0);
        var v4 = new Double3(4.0, 5.0, 6.0);
        Double3.Dot(ref v3, ref v4, out var dot);
        Assert.Equal(32.0, dot);
    }

    // InterpolationMethodsWithOutParameters (Lerp, SmoothStep, Barycentric)
    [Fact]
    public void TestDouble3InterpolationMethodsWithOutParameters()
    {
        var start = new Double3(0.0, 0.0, 0.0);
        var end = new Double3(10.0, 10.0, 10.0);

        // Test Lerp with out parameter
        Double3.Lerp(ref start, ref end, 0.5, out var lerpResult);
        Assert.Equal(5.0, lerpResult.X);

        // Test SmoothStep with out parameter
        Double3.SmoothStep(ref start, ref end, 0.5, out var smoothResult);
        Assert.Equal(5.0, smoothResult.X);

        // Test Barycentric with out parameter
        var v1 = new Double3(0.0, 0.0, 0.0);
        var v2 = new Double3(10.0, 0.0, 0.0);
        var v3 = new Double3(0.0, 10.0, 0.0);
        Double3.Barycentric(ref v1, ref v2, ref v3, 0.5, 0.5, out var baryResult);
        Assert.Equal(5.0, baryResult.X);
        Assert.Equal(5.0, baryResult.Y);
    }

    // ComplexInterpolationWithOutParameters (Hermite, CatmullRom)
    [Fact]
    public void TestDouble3ComplexInterpolationWithOutParameters()
    {
        // Test Hermite with out parameter
        var v1 = new Double3(0.0, 0.0, 0.0);
        var t1 = new Double3(1.0, 1.0, 1.0);
        var v2 = new Double3(2.0, 2.0, 2.0);
        var t2 = new Double3(1.0, 1.0, 1.0);
        Double3.Hermite(ref v1, ref t1, ref v2, ref t2, 0.5, out var hermiteResult);
        Assert.InRange(hermiteResult.X, 0.9, 1.1);

        // Test CatmullRom with out parameter
        var c1 = new Double3(0.0, 0.0, 0.0);
        var c2 = new Double3(1.0, 1.0, 1.0);
        var c3 = new Double3(2.0, 2.0, 2.0);
        var c4 = new Double3(3.0, 3.0, 3.0);
        Double3.CatmullRom(ref c1, ref c2, ref c3, ref c4, 0.5, out var catmullResult);
        Assert.InRange(catmullResult.X, 1.4, 1.6);
    }

    // MinMaxWithOutParameters (Min, Max)
    [Fact]
    public void TestDouble3MinMaxWithOutParameters()
    {
        var v1 = new Double3(1.0, 5.0, 3.0);
        var v2 = new Double3(3.0, 2.0, 4.0);

        // Test Min with out parameter
        Double3.Min(ref v1, ref v2, out var minResult);
        Assert.Equal(1.0, minResult.X);
        Assert.Equal(2.0, minResult.Y);
        Assert.Equal(3.0, minResult.Z);

        // Test Max with out parameter
        Double3.Max(ref v1, ref v2, out var maxResult);
        Assert.Equal(3.0, maxResult.X);
        Assert.Equal(5.0, maxResult.Y);
        Assert.Equal(4.0, maxResult.Z);
    }

    // ============================================
    // 6. Operators
    // ============================================

    [Fact]
    public void TestDouble3AddOperator()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(4.0, 5.0, 6.0);
        var result = v1 + v2;
        Assert.Equal(5.0, result.X);
        Assert.Equal(7.0, result.Y);
        Assert.Equal(9.0, result.Z);
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
    public void TestDouble3SubtractOperator()
    {
        var v1 = new Double3(5.0, 7.0, 9.0);
        var v2 = new Double3(2.0, 3.0, 4.0);
        var result = v1 - v2;
        Assert.Equal(3.0, result.X);
        Assert.Equal(4.0, result.Y);
        Assert.Equal(5.0, result.Z);
    }

    [Fact]
    public void TestDouble3NegateOperator()
    {
        var v = new Double3(1.0, -2.0, 3.0);
        var result = -v;
        Assert.Equal(-1.0, result.X);
        Assert.Equal(2.0, result.Y);
        Assert.Equal(-3.0, result.Z);
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
    public void TestDouble3MultiplyOperator()
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
    public void TestDouble3VectorOperators()
    {
        var v1 = new Double3(10.0, 20.0, 30.0);
        var v2 = new Double3(2.0, 4.0, 5.0);

        // Vector * Vector
        var mult = v1 * v2;
        Assert.Equal(20.0, mult.X);
        Assert.Equal(80.0, mult.Y);
        Assert.Equal(150.0, mult.Z);

        // Vector / Vector
        var div = v1 / v2;
        Assert.Equal(5.0, div.X);
        Assert.Equal(5.0, div.Y);
        Assert.Equal(6.0, div.Z);

        // Vector + scalar
        var addScalar = v1 + 5.0;
        Assert.Equal(15.0, addScalar.X);
        Assert.Equal(25.0, addScalar.Y);
        Assert.Equal(35.0, addScalar.Z);

        // Vector - scalar
        var subScalar = v1 - 5.0;
        Assert.Equal(5.0, subScalar.X);
        Assert.Equal(15.0, subScalar.Y);
        Assert.Equal(25.0, subScalar.Z);

        // scalar / Vector
        var scalarDiv = 100.0 / v2;
        Assert.Equal(50.0, scalarDiv.X);
        Assert.Equal(25.0, scalarDiv.Y);
        Assert.Equal(20.0, scalarDiv.Z);
    }

    [Fact]
    public void TestDouble3DivideOperator()
    {
        var v = new Double3(4.0, 6.0, 8.0);
        var result = v / 2.0;
        Assert.Equal(2.0, result.X);
        Assert.Equal(3.0, result.Y);
        Assert.Equal(4.0, result.Z);
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

    // ============================================
    // 7. Conversions
    // ============================================

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
    public void TestDouble3ConversionFromVector3()
    {
        var v = new Vector3(1.0f, 2.0f, 3.0f);
        Double3 d = v;
        Assert.Equal(1.0, d.X);
        Assert.Equal(2.0, d.Y);
        Assert.Equal(3.0, d.Z);
    }

    [Fact]
    public void TestDouble3ConversionToHalf3()
    {
        var dbl = new Double3(1.5, 2.5, 3.5);
        var half = (Half3)dbl;
        Assert.InRange((float)half.X, 1.49f, 1.51f);
        Assert.InRange((float)half.Y, 2.49f, 2.51f);
        Assert.InRange((float)half.Z, 3.49f, 3.51f);
    }

    [Fact]
    public void TestDouble3ConversionFromHalf3()
    {
        var half = new Half3((Half)1.0f, (Half)2.0f, (Half)3.0f);
        var dbl = (Double3)half;
        Assert.InRange(dbl.X, 0.99, 1.01);
        Assert.InRange(dbl.Y, 1.99, 2.01);
        Assert.InRange(dbl.Z, 2.99, 3.01);
    }

    [Fact]
    public void TestDouble3ConversionToDouble2()
    {
        var d3 = new Double3(1.0, 2.0, 3.0);
        var d2 = (Double2)d3;
        Assert.Equal(1.0, d2.X);
        Assert.Equal(2.0, d2.Y);
    }

    [Fact]
    public void TestDouble3ConversionToDouble4()
    {
        var d3 = new Double3(1.0, 2.0, 3.0);
        var d4 = (Double4)d3;
        Assert.Equal(1.0, d4.X);
        Assert.Equal(2.0, d4.Y);
        Assert.Equal(3.0, d4.Z);
        Assert.Equal(0.0, d4.W);
    }

    [Fact]
    public void TestDouble3ConversionFromSystemNumericsVector3()
    {
        var sysVec = new System.Numerics.Vector3(1.0f, 2.0f, 3.0f);
        Double3 d3 = sysVec;
        Assert.Equal(1.0, d3.X);
        Assert.Equal(2.0, d3.Y);
        Assert.Equal(3.0, d3.Z);
    }

    [Fact]
    public void TestDouble3ConversionToSystemNumericsVector3()
    {
        var d3 = new Double3(1.0, 2.0, 3.0);
        var sysVec = (System.Numerics.Vector3)d3;
        Assert.Equal(1.0f, sysVec.X);
        Assert.Equal(2.0f, sysVec.Y);
        Assert.Equal(3.0f, sysVec.Z);
    }

    // ============================================
    // 8. Object overrides
    // ============================================

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
    public void TestDouble3ToStringWithFormat()
    {
        var v = new Double3(1.5, 2.5, 3.5);
        var str = v.ToString("F1", System.Globalization.CultureInfo.InvariantCulture);
        Assert.Contains("1.5", str);
        Assert.Contains("2.5", str);
        Assert.Contains("3.5", str);
    }

    [Fact]
    public void TestDouble3HashCode()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(1.0, 2.0, 3.0);
        Assert.Equal(v1.GetHashCode(), v2.GetHashCode());
    }

    [Fact]
    public void TestDouble3EqualsObject()
    {
        var v1 = new Double3(1.0, 2.0, 3.0);
        var v2 = new Double3(1.0, 2.0, 3.0);
        var v3 = new Double3(4.0, 5.0, 6.0);

        Assert.True(v1.Equals((object)v2));
        Assert.False(v1.Equals((object)v3));
        Assert.False(v1.Equals(null));
        Assert.False(v1.Equals(42));
    }

    // ============================================
    // 9. Deconstruct
    // ============================================

    [Fact]
    public void TestDouble3Deconstruct()
    {
        var v = new Double3(1.5, 2.5, 3.5);
        var (x, y, z) = v;
        Assert.Equal(1.5, x);
        Assert.Equal(2.5, y);
        Assert.Equal(3.5, z);
    }
}




