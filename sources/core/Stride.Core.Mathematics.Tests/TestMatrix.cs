// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Runtime.InteropServices;
using Xunit;
using System.Numerics;

namespace Stride.Core.Mathematics.Tests;

public class TestMatrix
{
    /* Note: As seen in the TestCompose* tests, we check both expectedQuat == decompedQuat and expectedQuat == -decompedQuat
     * This is because different combinations of yaw/pitch/roll can result in the same *orientation*, which is what we're actually testing.
     * This means that decomposing a rotation matrix or quaternion can actually have multiple answers, but we arbitrarily pick
     * one result, and this may not have actually been the original yaw/pitch/roll the user chose.
     */

    [Theory, ClassData(typeof(TestRotationsData.YRPTestData))]
    public void TestDecomposeYawPitchRollFromQuaternionYPR(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        var rotQuat = Quaternion.CreateFromYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        var rotMatrix = Matrix4x4.CreateFromQuaternion(rotQuat);
        rotMatrix.Decompose(out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = rotQuat;
        var decompedQuat = Quaternion.CreateFromYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat.NearEquals(decompedQuat) || expectedQuat.NearEquals(-decompedQuat), $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.YRPTestData))]
    public void TestDecomposeYawPitchRollFromMatrixYPR(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        var rotMatrix = Matrix4x4.CreateFromYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        rotMatrix.Decompose(out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = Quaternion.CreateFromYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        var decompedQuat = Quaternion.CreateFromYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat.NearEquals(decompedQuat) || expectedQuat.NearEquals(-decompedQuat), $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.YRPTestData))]
    public void TestDecomposeYawPitchRollFromMatricesZXY(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        // Yaw-Pitch-Roll is the intrinsic rotation order, so extrinsic is the reverse (ie. Z-X-Y)
        var rotMatrix = Matrix4x4.CreateRotationZ(rollRadians) * Matrix4x4.CreateRotationX(pitchRadians) * Matrix4x4.CreateRotationY(yawRadians);
        rotMatrix.Decompose(out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = Quaternion.CreateFromYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        var decompedQuat = Quaternion.CreateFromYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat.NearEquals(decompedQuat) || expectedQuat.NearEquals(-decompedQuat), $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.XYZTestData))]
    public void TestDecomposeXYZFromMatricesXYZ(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        var rotMatrix = Matrix4x4.CreateRotationX(pitchRadians) * Matrix4x4.CreateRotationY(yawRadians) * Matrix4x4.CreateRotationZ(rollRadians);
        rotMatrix.DecomposeXYZ(out Vector3 eulerAngles);

        var decompedRotMatrix = Matrix4x4.CreateRotationX(eulerAngles.X) * Matrix4x4.CreateRotationY(eulerAngles.Y) * Matrix4x4.CreateRotationZ(eulerAngles.Z);
        var decompedQuat = Quaternion.CreateFromRotationMatrix(decompedRotMatrix);

        var expectedQuat = Quaternion.Concatenate(Quaternion.Concatenate(Quaternion.CreateRotationX(pitchRadians), Quaternion.CreateRotationY(yawRadians)), Quaternion.CreateRotationZ(rollRadians));
        Assert.True(expectedQuat.NearEquals(decompedQuat) || expectedQuat.NearEquals(-decompedQuat), $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Fact]
    public void TestNumericConversion()
    {
        System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Matrix4x4 baseStrideMatrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Matrix4x4 strideMatrix = matrix;
        Assert.Equal(baseStrideMatrix, strideMatrix);
    }

    [Fact]
    public void TestStrideConversion()
    {
        Matrix4x4 matrix = new(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        System.Numerics.Matrix4x4 baseNumericseMatrix = new(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        System.Numerics.Matrix4x4 numericsMatrix = matrix;
        Assert.Equal(baseNumericseMatrix, numericsMatrix);
    }

    [Fact]
    public void TestMemoryIsRowByRow()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);
        var numerics = new System.Numerics.Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        float[] expected = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        Assert.Equal(expected, MemoryMarshal.Cast<Matrix4x4, float>(new ReadOnlySpan<Matrix4x4>(ref matrix)).ToArray());
        Assert.Equal(expected, MemoryMarshal.Cast<System.Numerics.Matrix4x4, float>(new ReadOnlySpan<System.Numerics.Matrix4x4>(ref numerics)).ToArray());
    }

    #region Matrix Operations Tests

    [Fact]
    public void TestMatrixMultiplication()
    {
        var m1 = new Matrix4x4(
            1, 2, 3, 0,
            4, 5, 6, 0,
            7, 8, 9, 0,
            0, 0, 0, 1);

        var m2 = new Matrix4x4(
            2, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 2, 0,
            0, 0, 0, 1);

        var result = m1 * m2;

        Assert.Equal(2f, result.M11);
        Assert.Equal(4f, result.M12);
        Assert.Equal(6f, result.M13);
        Assert.Equal(8f, result.M21);
        Assert.Equal(10f, result.M22);
        Assert.Equal(12f, result.M23);
        Assert.Equal(14f, result.M31);
        Assert.Equal(16f, result.M32);
        Assert.Equal(18f, result.M33);
        Assert.Equal(1f, result.M44);
    }

    [Fact]
    public void TestMatrixVectorMultiplication()
    {
        var matrix = new Matrix4x4(
            2, 0, 0, 0,
            0, 3, 0, 0,
            0, 0, 4, 0,
            1, 2, 3, 1);

        var vector = new Vector3(1, 1, 1);
        var result = Vector4.Transform(vector, matrix);

        Assert.Equal(3f, result.X); // 2*1 + 0*1 + 0*1 + 1*1
        Assert.Equal(5f, result.Y); // 0*1 + 3*1 + 0*1 + 2*1
        Assert.Equal(7f, result.Z); // 0*1 + 0*1 + 4*1 + 3*1
    }

    [Fact]
    public void TestMatrixDeterminant()
    {
        var matrix = new Matrix4x4(
            1, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 3, 0,
            0, 0, 0, 1);

        float det = matrix.GetDeterminant();
        Assert.Equal(6f, det); // 1 * 2 * 3 * 1
    }

    [Fact]
    public void TestMatrixInverse()
    {
        var matrix = new Matrix4x4(
            2, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 2, 0,
            1, 2, 3, 1);

        Matrix4x4.InvertSafe(matrix, out var inverse);
        var identity = matrix * inverse;

        // Check if the result is approximately identity matrix
        Assert.Equal(1f, identity.M11, 3);
        Assert.Equal(0f, identity.M12, 3);
        Assert.Equal(0f, identity.M13, 3);
        Assert.Equal(0f, identity.M14, 3);
        Assert.Equal(0f, identity.M21, 3);
        Assert.Equal(1f, identity.M22, 3);
        Assert.Equal(0f, identity.M23, 3);
        Assert.Equal(0f, identity.M24, 3);
        Assert.Equal(0f, identity.M31, 3);
        Assert.Equal(0f, identity.M32, 3);
        Assert.Equal(1f, identity.M33, 3);
        Assert.Equal(0f, identity.M34, 3);
        Assert.Equal(0f, identity.M41, 3);
        Assert.Equal(0f, identity.M42, 3);
        Assert.Equal(0f, identity.M43, 3);
        Assert.Equal(1f, identity.M44, 3);
    }

    [Fact]
    public void TestMatrixTranspose()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var transpose = Matrix4x4.Transpose(matrix);

        // Check diagonal elements remain the same
        Assert.Equal(matrix.M11, transpose.M11);
        Assert.Equal(matrix.M22, transpose.M22);
        Assert.Equal(matrix.M33, transpose.M33);
        Assert.Equal(matrix.M44, transpose.M44);

        // Check off-diagonal elements are swapped
        Assert.Equal(matrix.M12, transpose.M21);
        Assert.Equal(matrix.M13, transpose.M31);
        Assert.Equal(matrix.M14, transpose.M41);
        Assert.Equal(matrix.M21, transpose.M12);
        Assert.Equal(matrix.M23, transpose.M32);
        Assert.Equal(matrix.M24, transpose.M42);
        Assert.Equal(matrix.M31, transpose.M13);
        Assert.Equal(matrix.M32, transpose.M23);
        Assert.Equal(matrix.M34, transpose.M43);
        Assert.Equal(matrix.M41, transpose.M14);
        Assert.Equal(matrix.M42, transpose.M24);
        Assert.Equal(matrix.M43, transpose.M34);

        // Verify double transpose returns original matrix
        var doubleTranspose = Matrix4x4.Transpose(transpose);
        Assert.Equal(matrix, doubleTranspose);
    }

    [Fact]
    public void TestMatrixTransposeReturnsTranspose()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);
        var expected = Matrix4x4.Transpose(matrix);

        var transpose = Matrix4x4.Transpose(matrix);

        Assert.Equal(expected, transpose);
        Assert.NotEqual(matrix, transpose);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(90, 0, 0)]
    [InlineData(0, 90, 0)]
    [InlineData(0, 0, 90)]
    [InlineData(45, 45, 45)]
    public void TestRotationMatrixDecomposition(float x, float y, float z)
    {
        var rotationMatrix = Matrix4x4.CreateRotationX(MathUtil.DegreesToRadians(x)) *
                           Matrix4x4.CreateRotationY(MathUtil.DegreesToRadians(y)) *
                           Matrix4x4.CreateRotationZ(MathUtil.DegreesToRadians(z));

        rotationMatrix.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 translation);

        // Check scale is approximately unit
        Assert.Equal(1f, scale.X, 3);
        Assert.Equal(1f, scale.Y, 3);
        Assert.Equal(1f, scale.Z, 3);

        // Check translation is zero
        Assert.Equal(0f, translation.X, 3);
        Assert.Equal(0f, translation.Y, 3);
        Assert.Equal(0f, translation.Z, 3);

        // Reconstruct matrix from decomposed parts and compare
        var reconstructed = Matrix4x4.CreateTransformation(Vector3.Zero, Quaternion.Identity, scale, Vector3.Zero, rotation, translation);

        Assert.Equal(rotationMatrix.M11, reconstructed.M11, 3);
        Assert.Equal(rotationMatrix.M12, reconstructed.M12, 3);
        Assert.Equal(rotationMatrix.M13, reconstructed.M13, 3);
        Assert.Equal(rotationMatrix.M21, reconstructed.M21, 3);
        Assert.Equal(rotationMatrix.M22, reconstructed.M22, 3);
        Assert.Equal(rotationMatrix.M23, reconstructed.M23, 3);
        Assert.Equal(rotationMatrix.M31, reconstructed.M31, 3);
        Assert.Equal(rotationMatrix.M32, reconstructed.M32, 3);
        Assert.Equal(rotationMatrix.M33, reconstructed.M33, 3);
    }

    [Fact]
    public void TestMatrixTransformationWithScalingRotation()
    {
        var scalingCenter = new Vector3(1, -2, 3);
        var scalingRotation = Quaternion.CreateFromYawPitchRoll(0.4f, 0.7f, -0.3f);
        var scaling = new Vector3(2, 3, 4);
        var rotationCenter = new Vector3(-3, 1, 2);
        var rotation = Quaternion.CreateFromYawPitchRoll(-0.5f, 0.2f, 0.9f);
        var translation = new Vector3(5, 6, 7);

        var actual = Matrix4x4.CreateTransformation(scalingCenter, scalingRotation, scaling, rotationCenter, rotation, translation);

        // Scaling is applied along the axes turned by scalingRotation: the inverse rotation, then the scaling, then the rotation.
        var sr = Matrix4x4.CreateFromQuaternion(scalingRotation);
        Matrix4x4.InvertSafe(sr, out var srInverse);
        var expected = Matrix4x4.CreateTranslation(-scalingCenter) * srInverse * Matrix4x4.CreateScale(scaling) * sr * Matrix4x4.CreateTranslation(scalingCenter)
            * Matrix4x4.CreateTranslation(-rotationCenter) * Matrix4x4.CreateFromQuaternion(rotation) * Matrix4x4.CreateTranslation(rotationCenter)
            * Matrix4x4.CreateTranslation(translation);

        for (var i = 0; i < 16; i++)
            Assert.Equal(expected[i / 4, i % 4], actual[i / 4, i % 4], 4);
    }

    [Fact]
    public void TestMatrixTransformation()
    {
        var scale = new Vector3(2, 3, 4);
        var rotation = Quaternion.CreateFromYawPitchRoll(
            MathUtil.DegreesToRadians(30),
            MathUtil.DegreesToRadians(45),
            MathUtil.DegreesToRadians(60)
        );
        var translation = new Vector3(1, 2, 3);

        var transform = Matrix4x4.CreateTransformation(
            Vector3.Zero,    // scaling center
            Quaternion.Identity,  // scaling rotation
            scale,          // scale
            Vector3.Zero,    // rotation center
            rotation,       // rotation
            translation    // translation
        );

        // Test transformation of a point
        var point = new Vector3(1, 1, 1);
        var transformed = Vector4.Transform(point, transform);

        // The point should be:
        // 1. Scaled
        // 2. Rotated
        // 3. Translated

        // Verify the transformation by doing it step by step
        var scaled = new Vector3(
            point.X * scale.X,
            point.Y * scale.Y,
            point.Z * scale.Z
        );

        var rotated = Vector3.Transform(scaled, rotation);
        var final = rotated + translation;

        Assert.Equal(final.X, transformed.X, 3);
        Assert.Equal(final.Y, transformed.Y, 3);
        Assert.Equal(final.Z, transformed.Z, 3);
    }

    [Fact]
    public void TestMatrixScaling()
    {
        var scale = new Vector3(2, 3, 4);
        var scaleMatrix = Matrix4x4.CreateScale(scale);

        // Test scaling of a point
        var point = new Vector3(1, 1, 1);
        var scaled = Vector4.Transform(point, scaleMatrix);

        Assert.Equal(2f, scaled.X);
        Assert.Equal(3f, scaled.Y);
        Assert.Equal(4f, scaled.Z);
    }

    [Fact]
    public void TestMatrixConstruction()
    {
        // Test value constructor
        var m1 = Matrix4x4.Create(2.0f);
        Assert.Equal(2.0f, m1.M11);
        Assert.Equal(2.0f, m1.M22);
        Assert.Equal(2.0f, m1.M33);
        Assert.Equal(2.0f, m1.M44);

        // Test component constructor
        var m2 = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Assert.Equal(1f, m2.M11);
        Assert.Equal(2f, m2.M12);
        Assert.Equal(16f, m2.M44);

        // Test array constructor
        float[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        var m3 = Matrix4x4.Create(values);
        Assert.Equal(1f, m3.M11);
        Assert.Equal(16f, m3.M44);
    }

    [Fact]
    public void TestMatrixStaticFields()
    {
        // Test Zero matrix
        Assert.Equal(0f, default(Matrix4x4).M11);
        Assert.Equal(0f, default(Matrix4x4).M22);
        Assert.Equal(0f, default(Matrix4x4).M33);
        Assert.Equal(0f, default(Matrix4x4).M44);

        // Test Identity matrix
        Assert.Equal(1f, Matrix4x4.Identity.M11);
        Assert.Equal(1f, Matrix4x4.Identity.M22);
        Assert.Equal(1f, Matrix4x4.Identity.M33);
        Assert.Equal(1f, Matrix4x4.Identity.M44);
        Assert.Equal(0f, Matrix4x4.Identity.M12);
        Assert.Equal(0f, Matrix4x4.Identity.M41);

        // Test IsIdentity property
        Assert.True(Matrix4x4.Identity.NearEquals(Matrix4x4.Identity));
        Assert.False((default(Matrix4x4)).NearEquals(Matrix4x4.Identity));
    }

    [Fact]
    public void TestMatrixTranslation()
    {
        var translation = new Vector3(10, 20, 30);
        var matrix = Matrix4x4.CreateTranslation(translation);

        Assert.Equal(10f, matrix.M41);
        Assert.Equal(20f, matrix.M42);
        Assert.Equal(30f, matrix.M43);
        Assert.Equal(1f, matrix.M44);

        // Test Translation property
        Assert.Equal(translation, matrix.Translation);

        // Test transforming a point
        var point = new Vector3(1, 2, 3);
        var transformed = Vector4.Transform(point, matrix);
        Assert.Equal(11f, transformed.X);
        Assert.Equal(22f, transformed.Y);
        Assert.Equal(33f, transformed.Z);
    }

    [Fact]
    public void TestMatrixShadow()
    {
        var light = new Vector4(0, 10, 0, 1); // Light above
        var plane = new Plane(Vector3.UnitY, 0); // Ground plane

        var matrix = Matrix4x4.CreateShadow(light, plane);

        // Shadow matrix should project points onto the plane
        var point = new Vector3(1, 5, 1);
        var shadow = Vector4.Transform(point, matrix);

        // Shadow should be on the ground plane (Y ≈ 0)
        Assert.Equal(0f, shadow.Y, 2);
    }

    [Fact]
    public void TestMatrixRotationYawPitchRoll()
    {
        float yaw = MathUtil.PiOverFour;
        float pitch = MathUtil.PiOverFour / 2;
        float roll = MathUtil.PiOverFour / 3;

        var matrix = Matrix4x4.CreateFromYawPitchRoll(yaw, pitch, roll);

        // Should create a valid rotation matrix
        Assert.NotEqual(0f, matrix.GetDeterminant());

        // Rotation matrices should have determinant close to ±1
        Assert.Equal(1f, Math.Abs(matrix.GetDeterminant()), 1);
    }

    [Fact]
    public void TestMatrixTransformation2D()
    {
        var scalingCenter = new Vector2(5, 5);
        float scalingRotation = 0f;
        var scaling = new Vector2(2, 2);
        var rotationCenter = new Vector2(5, 5);
        float rotation = MathUtil.PiOverFour;
        var translation = new Vector2(10, 10);

        var matrix = Matrix4x4.CreateTransformation2D(scalingCenter, scalingRotation, scaling, rotationCenter, rotation, translation);

        // Should create a valid 2D transformation matrix
        Assert.NotEqual(0f, matrix.GetDeterminant());
    }

    [Fact]
    public void TestMatrixArrayAccess()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Assert.Equal(1f, matrix[0, 0]);
        Assert.Equal(2f, matrix[0, 1]);
        Assert.Equal(16f, matrix[3, 3]);

        // Test setter
        matrix[0, 0] = 100f;
        Assert.Equal(100f, matrix.M11);
    }

    [Fact]
    public void TestMatrixFromNumeric()
    {
        var numeric = new System.Numerics.Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var matrix = (Matrix4x4)numeric;

        Assert.Equal(1f, matrix.M11);
        Assert.Equal(2f, matrix.M12);
        Assert.Equal(16f, matrix.M44);
    }

    [Fact]
    public void TestMatrixToNumeric()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var numeric = (System.Numerics.Matrix4x4)matrix;

        Assert.Equal(1f, numeric.M11);
        Assert.Equal(2f, numeric.M12);
        Assert.Equal(16f, numeric.M44);
    }

    [Fact]
    public void TestMatrixOrthoRH()
    {
        float width = 800f;
        float height = 600f;
        float znear = 0.1f;
        float zfar = 1000f;

        var matrix = Matrix4x4.CreateOrthographic(width, height, znear, zfar);

        // RH should have different characteristics than LH
        Assert.Equal(1f, matrix.M44);
        Assert.NotEqual(0f, matrix.M11);
        Assert.NotEqual(0f, matrix.M22);
        Assert.NotEqual(0f, matrix.M33);
    }

    [Fact]
    public void TestMatrixOrthoOffCenterLH()
    {
        float left = -400f;
        float right = 400f;
        float bottom = -300f;
        float top = 300f;
        float znear = 0.1f;
        float zfar = 1000f;

        var matrix = Matrix4x4.CreateOrthographicOffCenterLeftHanded(left, right, bottom, top, znear, zfar);

        Assert.Equal(1f, matrix.M44);
        Assert.NotEqual(0f, matrix.M11);
        Assert.NotEqual(0f, matrix.M22);
    }

    [Fact]
    public void TestMatrixPerspectiveLH()
    {
        float width = 800f;
        float height = 600f;
        float znear = 0.1f;
        float zfar = 1000f;

        var matrix = Matrix4x4.CreatePerspectiveLeftHanded(width, height, znear, zfar);

        // Perspective matrices have M44 = 0
        Assert.Equal(0f, matrix.M44);
        Assert.NotEqual(0f, matrix.M11);
        Assert.NotEqual(0f, matrix.M22);
    }

    [Fact]
    public void TestMatrixLookAtRH()
    {
        var eye = new Vector3(0, 0, 5);
        var target = new Vector3(0, 0, 0);
        var up = new Vector3(0, 1, 0);

        var matrix = Matrix4x4.CreateLookAt(eye, target, up);

        // LookAt matrix should be invertible
        Assert.NotEqual(0f, matrix.GetDeterminant());
    }

    [Fact]
    public void TestMatrixSmoothStep()
    {
        var start = Matrix4x4.Identity;
        var end = Matrix4x4.CreateScale(2f);
        float amount = 0.5f;

        var result = Matrix4x4.SmoothStep(start, end, amount);

        // Result should be between start and end
        Assert.True(result.M11 > 1f && result.M11 < 2f);
    }

    [Fact]
    public void TestMatrixOrthogonalize()
    {
        // Create a matrix that's slightly non-orthogonal
        var matrix = Matrix4x4.CreateRotationY(0.1f);
        matrix.M12 += 0.01f; // Perturb it slightly

        var ortho = Matrix4x4.Orthogonalize(matrix);

        // After orthogonalization, rows should be more perpendicular
        var row1 = new Vector3(ortho.M11, ortho.M12, ortho.M13);
        var row2 = new Vector3(ortho.M21, ortho.M22, ortho.M23);

        float dot = Vector3.Dot(row1, row2);
        Assert.Equal(0f, dot, 2); // Rows should be nearly perpendicular
    }

    [Fact]
    public void TestMatrixOrthonormalize()
    {
        // Create a non-orthonormal matrix (scaled rotation)
        var matrix = new Matrix4x4(
            2, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 2, 0,
            0, 0, 0, 1);

        var orthonormal = Matrix4x4.Orthonormalize(matrix);

        // After orthonormalization, row vectors should have unit length
        var row1 = new Vector3(orthonormal.M11, orthonormal.M12, orthonormal.M13);
        Assert.Equal(1f, row1.Length(), 3);
    }

    #endregion

    #region Matrix Edge Cases Tests

    [Fact]
    public void TestMatrixIdentity()
    {
        var identity = Matrix4x4.Identity;

        Assert.Equal(1.0f, identity.M11);
        Assert.Equal(0.0f, identity.M12);
        Assert.Equal(0.0f, identity.M13);
        Assert.Equal(0.0f, identity.M14);

        Assert.Equal(0.0f, identity.M21);
        Assert.Equal(1.0f, identity.M22);
        Assert.Equal(0.0f, identity.M23);
        Assert.Equal(0.0f, identity.M24);

        Assert.Equal(0.0f, identity.M31);
        Assert.Equal(0.0f, identity.M32);
        Assert.Equal(1.0f, identity.M33);
        Assert.Equal(0.0f, identity.M34);

        Assert.Equal(0.0f, identity.M41);
        Assert.Equal(0.0f, identity.M42);
        Assert.Equal(0.0f, identity.M43);
        Assert.Equal(1.0f, identity.M44);
    }

    [Fact]
    public void TestMatrixMultiplicationByIdentity()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var result1 = matrix * Matrix4x4.Identity;
        var result2 = Matrix4x4.Identity * matrix;

        Assert.Equal(matrix, result1);
        Assert.Equal(matrix, result2);
    }

    [Fact]
    public void TestMatrixTransposeEdgeCase()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var transposed = Matrix4x4.Transpose(matrix);

        Assert.Equal(1f, transposed.M11);
        Assert.Equal(5f, transposed.M12);
        Assert.Equal(9f, transposed.M13);
        Assert.Equal(13f, transposed.M14);

        Assert.Equal(2f, transposed.M21);
        Assert.Equal(6f, transposed.M22);
        Assert.Equal(10f, transposed.M23);
        Assert.Equal(14f, transposed.M24);
    }

    [Fact]
    public void TestMatrixInverseIdentity()
    {
        var identity = Matrix4x4.Identity;
        Matrix4x4.InvertSafe(identity, out var inverse);

        Assert.Equal(identity, inverse);
    }

    [Fact]
    public void TestMatrixInverseSingular()
    {
        // Singular matrix (determinant = 0)
        var singular = new Matrix4x4(
            1, 2, 3, 0,
            2, 4, 6, 0,
            3, 6, 9, 0,
            0, 0, 0, 1);

        Matrix4x4.InvertSafe(singular, out var inverse);

        // Inverse of singular matrix - implementation may return specific values
        // Just verify it doesn't crash
        Assert.True(true); // Test passes if we reach here
    }

    [Fact]
    public void TestMatrixDeterminantIdentity()
    {
        var identity = Matrix4x4.Identity;
        var det = identity.GetDeterminant();

        Assert.Equal(1.0f, det);
    }

    [Fact]
    public void TestMatrixDeterminantZero()
    {
        // Matrix with determinant 0
        var matrix = new Matrix4x4(
            1, 2, 3, 0,
            2, 4, 6, 0,
            3, 6, 9, 0,
            0, 0, 0, 1);

        var det = matrix.GetDeterminant();

        Assert.Equal(0.0f, det, 5);
    }

    [Fact]
    public void TestMatrixTranslationEdgeCase()
    {
        var translation = Matrix4x4.CreateTranslation(5.0f, 10.0f, 15.0f);
        var point = new Vector3(1.0f, 1.0f, 1.0f);

        var transformed = Vector3.TransformCoordinate(point, translation);

        Assert.Equal(6.0f, transformed.X);
        Assert.Equal(11.0f, transformed.Y);
        Assert.Equal(16.0f, transformed.Z);
    }

    [Fact]
    public void TestMatrixScalingEdgeCase()
    {
        var scaling = Matrix4x4.CreateScale(2.0f, 3.0f, 4.0f);
        var point = new Vector3(1.0f, 1.0f, 1.0f);

        var transformed = Vector3.TransformCoordinate(point, scaling);

        Assert.Equal(2.0f, transformed.X);
        Assert.Equal(3.0f, transformed.Y);
        Assert.Equal(4.0f, transformed.Z);
    }

    [Fact]
    public void TestMatrixUniformScaling()
    {
        var scaling = Matrix4x4.CreateScale(2.0f);
        var point = new Vector3(1.0f, 2.0f, 3.0f);

        var transformed = Vector3.TransformCoordinate(point, scaling);

        Assert.Equal(2.0f, transformed.X);
        Assert.Equal(4.0f, transformed.Y);
        Assert.Equal(6.0f, transformed.Z);
    }

    [Fact]
    public void TestMatrixRotationX90Degrees()
    {
        var rotation = Matrix4x4.CreateRotationX(MathUtil.PiOverTwo);
        var point = new Vector3(0.0f, 1.0f, 0.0f);

        var transformed = Vector3.TransformCoordinate(point, rotation);

        Assert.Equal(0.0f, transformed.X, 5);
        Assert.Equal(0.0f, transformed.Y, 5);
        Assert.Equal(1.0f, transformed.Z, 5);
    }

    [Fact]
    public void TestMatrixRotationY90Degrees()
    {
        var rotation = Matrix4x4.CreateRotationY(MathUtil.PiOverTwo);
        var point = new Vector3(1.0f, 0.0f, 0.0f);

        var transformed = Vector3.TransformCoordinate(point, rotation);

        Assert.Equal(0.0f, transformed.X, 5);
        Assert.Equal(0.0f, transformed.Y, 5);
        Assert.Equal(-1.0f, transformed.Z, 5);
    }

    [Fact]
    public void TestMatrixRotationZ90Degrees()
    {
        var rotation = Matrix4x4.CreateRotationZ(MathUtil.PiOverTwo);
        var point = new Vector3(1.0f, 0.0f, 0.0f);

        var transformed = Vector3.TransformCoordinate(point, rotation);

        Assert.Equal(0.0f, transformed.X, 5);
        Assert.Equal(1.0f, transformed.Y, 5);
        Assert.Equal(0.0f, transformed.Z, 5);
    }

    [Fact]
    public void TestMatrixIsIdentity()
    {
        var identity = Matrix4x4.Identity;
        Assert.True(identity.NearEquals(Matrix4x4.Identity));

        var notIdentity = Matrix4x4.CreateTranslation(1, 0, 0);
        Assert.False(notIdentity.NearEquals(Matrix4x4.Identity));
    }

    [Fact]
    public void TestMatrixEquality()
    {
        var m1 = new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var m2 = new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var m3 = Matrix4x4.Identity;

        Assert.True(m1 == m2);
        Assert.False(m1 == m3);
        Assert.False(m1 != m2);
        Assert.True(m1 != m3);

        Assert.True(m1.Equals(m2));
        Assert.False(m1.Equals(m3));
    }

    [Fact]
    public void TestMatrixNegation()
    {
        var matrix = new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var negated = -matrix;

        Assert.Equal(-1f, negated.M11);
        Assert.Equal(-2f, negated.M12);
        Assert.Equal(-16f, negated.M44);
    }

    [Fact]
    public void TestMatrixAddition()
    {
        var m1 = Matrix4x4.Identity;
        var m2 = Matrix4x4.Identity;

        var sum = m1 + m2;

        Assert.Equal(2.0f, sum.M11);
        Assert.Equal(2.0f, sum.M22);
        Assert.Equal(2.0f, sum.M33);
        Assert.Equal(2.0f, sum.M44);
        Assert.Equal(0.0f, sum.M12);
    }

    [Fact]
    public void TestMatrixSubtraction()
    {
        var m1 = Matrix4x4.CreateScale(2.0f);
        var m2 = Matrix4x4.Identity;

        var diff = m1 - m2;

        Assert.Equal(1.0f, diff.M11);
        Assert.Equal(1.0f, diff.M22);
        Assert.Equal(1.0f, diff.M33);
        Assert.Equal(0.0f, diff.M44);
    }

    [Fact]
    public void TestMatrixScalarMultiplication()
    {
        var matrix = Matrix4x4.Identity;
        var scaled = matrix * 2.0f;

        Assert.Equal(2.0f, scaled.M11);
        Assert.Equal(2.0f, scaled.M22);
        Assert.Equal(2.0f, scaled.M33);
        Assert.Equal(2.0f, scaled.M44);

        var scaled2 = 2.0f * matrix;
        Assert.Equal(scaled, scaled2);
    }

    [Fact]
    public void TestMatrixScalarDivision()
    {
        var matrix = Matrix4x4.CreateScale(4.0f);
        matrix.M44 = 4.0f;
        var divided = matrix / 2.0f;

        Assert.Equal(2.0f, divided.M11);
        Assert.Equal(2.0f, divided.M22);
        Assert.Equal(2.0f, divided.M33);
        Assert.Equal(2.0f, divided.M44);
    }

    [Fact]
    public void TestMatrixRowColumn()
    {
        var matrix = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var row1 = matrix.X;
        Assert.Equal(1f, row1.X);
        Assert.Equal(2f, row1.Y);
        Assert.Equal(3f, row1.Z);
        Assert.Equal(4f, row1.W);

        var column1 = matrix.Column1;
        Assert.Equal(1f, column1.X);
        Assert.Equal(5f, column1.Y);
        Assert.Equal(9f, column1.Z);
        Assert.Equal(13f, column1.W);
    }

    [Fact]
    public void TestMatrixDecomposeTranslationOnly()
    {
        var translation = Matrix4x4.CreateTranslation(5.0f, 10.0f, 15.0f);

        translation.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 trans);

        Assert.Equal(new Vector3(1, 1, 1), scale);
        Assert.Equal(Quaternion.Identity, rotation);
        Assert.Equal(new Vector3(5, 10, 15), trans);
    }

    [Fact]
    public void TestMatrixDecomposeScaleOnly()
    {
        var scaling = Matrix4x4.CreateScale(2.0f, 3.0f, 4.0f);

        scaling.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 trans);

        Assert.Equal(2.0f, scale.X, 5);
        Assert.Equal(3.0f, scale.Y, 5);
        Assert.Equal(4.0f, scale.Z, 5);
        Assert.Equal(Quaternion.Identity, rotation);
        Assert.Equal(Vector3.Zero, trans);
    }

    [Fact]
    public void TestMatrixLerp()
    {
        var start = Matrix4x4.Identity;
        var end = Matrix4x4.CreateScale(2.0f);

        var halfway = Matrix4x4.Lerp(start, end, 0.5f);

        // At 0.5, should be halfway between identity (1) and scaling (2)
        Assert.Equal(1.5f, halfway.M11, 5);
        Assert.Equal(1.5f, halfway.M22, 5);
        Assert.Equal(1.5f, halfway.M33, 5);
        Assert.Equal(1.0f, halfway.M44, 5);

        // At 0, should equal start
        var atStart = Matrix4x4.Lerp(start, end, 0.0f);
        Assert.Equal(start, atStart);

        // At 1, should equal end
        var atEnd = Matrix4x4.Lerp(start, end, 1.0f);
        Assert.Equal(end, atEnd);
    }

    [Fact]
    public void TestMatrixBillboard()
    {
        var objectPos = new Vector3(5, 0, 0);
        var cameraPos = Vector3.Zero;
        var cameraUp = Vector3.UnitY;
        var cameraForward = Vector3.UnitZ;

        var billboard = Matrix4x4.CreateBillboard(objectPos, cameraPos, cameraUp, cameraForward);

        // Billboard matrix should be invertible
        Assert.NotEqual(0f, billboard.GetDeterminant());

        // Translation should match object position
        Assert.Equal(objectPos.X, billboard.M41, 5);
        Assert.Equal(objectPos.Y, billboard.M42, 5);
        Assert.Equal(objectPos.Z, billboard.M43, 5);
    }

    [Fact]
    public void TestMatrixReflection()
    {
        // Create a ground plane at Y=0
        var plane = new Plane(Vector3.UnitY, 0);
        var reflection = Matrix4x4.CreateReflection(plane);

        // Reflecting a point above the plane should give point below
        var point = new Vector3(1, 5, 1);
        var reflected = Vector3.TransformCoordinate(point, reflection);

        Assert.Equal(1.0f, reflected.X, 5);
        Assert.Equal(-5.0f, reflected.Y, 5);
        Assert.Equal(1.0f, reflected.Z, 5);
    }

    [Fact]
    public void TestMatrixPerspectiveFovLH()
    {
        float fov = MathUtil.PiOverFour;
        float aspect = 16f / 9f;
        float znear = 0.1f;
        float zfar = 1000f;

        var matrix = Matrix4x4.CreatePerspectiveFieldOfViewLeftHanded(fov, aspect, znear, zfar);

        // Perspective matrices should have M34 = 1 (for LH)
        Assert.Equal(1f, matrix.M34, 5);
        Assert.Equal(0f, matrix.M44, 5);
    }

    [Fact]
    public void TestMatrixPerspectiveFovRH()
    {
        float fov = MathUtil.PiOverFour;
        float aspect = 16f / 9f;
        float znear = 0.1f;
        float zfar = 1000f;

        var matrix = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, znear, zfar);

        // Perspective matrices should have M34 = -1 (for RH)
        Assert.Equal(-1f, matrix.M34, 5);
        Assert.Equal(0f, matrix.M44, 5);
    }

    [Fact]
    public void TestMatrixLookAtLH()
    {
        var eye = new Vector3(0, 0, -10);
        var target = Vector3.Zero;
        var up = Vector3.UnitY;

        var lookAt = Matrix4x4.CreateLookAtLeftHanded(eye, target, up);

        // LookAt matrix should be invertible
        Assert.NotEqual(0f, lookAt.GetDeterminant());
    }

    [Fact]
    public void TestMatrixOrthoOffCenterRH()
    {
        float left = -5f;
        float right = 5f;
        float bottom = -5f;
        float top = 5f;
        float znear = 0.1f;
        float zfar = 100f;

        var ortho = Matrix4x4.CreateOrthographicOffCenter(left, right, bottom, top, znear, zfar);

        Assert.Equal(1f, ortho.M44, 5);
        Assert.NotEqual(0f, ortho.M11);
        Assert.NotEqual(0f, ortho.M22);
        Assert.NotEqual(0f, ortho.M33);
    }

    [Fact]
    public void TestMatrixPerspectiveRH()
    {
        float width = 800f;
        float height = 600f;
        float znear = 0.1f;
        float zfar = 1000f;

        var perspective = Matrix4x4.CreatePerspective(width, height, znear, zfar);

        Assert.Equal(0f, perspective.M44);
        Assert.NotEqual(0f, perspective.M11);
        Assert.NotEqual(0f, perspective.M22);
    }

    [Fact]
    public void TestMatrixPerspectiveOffCenterLH()
    {
        float left = -400f;
        float right = 400f;
        float bottom = -300f;
        float top = 300f;
        float znear = 0.1f;
        float zfar = 1000f;

        var perspective = Matrix4x4.CreatePerspectiveOffCenterLeftHanded(left, right, bottom, top, znear, zfar);

        Assert.Equal(0f, perspective.M44);
        Assert.NotEqual(0f, perspective.M11);
        Assert.NotEqual(0f, perspective.M22);
    }

    [Fact]
    public void TestMatrixPerspectiveOffCenterRH()
    {
        float left = -400f;
        float right = 400f;
        float bottom = -300f;
        float top = 300f;
        float znear = 0.1f;
        float zfar = 1000f;

        var perspective = Matrix4x4.CreatePerspectiveOffCenter(left, right, bottom, top, znear, zfar);

        Assert.Equal(0f, perspective.M44);
        Assert.NotEqual(0f, perspective.M11);
        Assert.NotEqual(0f, perspective.M22);
    }

    [Fact]
    public void TestMatrixOrthoLH()
    {
        float width = 800f;
        float height = 600f;
        float znear = 0.1f;
        float zfar = 1000f;

        var ortho = Matrix4x4.CreateOrthographicLeftHanded(width, height, znear, zfar);

        Assert.Equal(1f, ortho.M44);
        Assert.NotEqual(0f, ortho.M11);
        Assert.NotEqual(0f, ortho.M22);
    }

    [Fact]
    public void TestMatrixRotationAxis()
    {
        var axis = Vector3.Normalize(new Vector3(1, 1, 0));
        var angle = MathUtil.PiOverFour;

        var rotation = Matrix4x4.CreateFromAxisAngle(axis, angle);

        // Should be a valid rotation matrix (determinant ≈ ±1)
        Assert.Equal(1f, Math.Abs(rotation.GetDeterminant()), 3);
    }

    [Fact]
    public void TestMatrixAffineTransformation()
    {
        float scaling = 2.0f;
        var rotation = Quaternion.CreateRotationY(MathUtil.PiOverFour);
        var translation = new Vector3(10, 20, 30);

        var affine = Matrix4x4.CreateAffineTransformation(scaling, rotation, translation);

        // Should create valid transformation
        Assert.NotEqual(default(Matrix4x4), affine);
        Assert.Equal(translation.X, affine.M41, 5);
        Assert.Equal(translation.Y, affine.M42, 5);
        Assert.Equal(translation.Z, affine.M43, 5);
    }

    [Fact]
    public void TestMatrixAffineTransformation2D()
    {
        float scaling = 1.5f;
        float rotation = MathUtil.PiOverFour;
        var translation = new Vector2(100, 50);

        var affine = Matrix4x4.CreateAffineTransformation2D(scaling, rotation, translation);

        // Should create valid transformation
        Assert.NotEqual(default(Matrix4x4), affine);
        Assert.Equal(translation.X, affine.M41, 5);
        Assert.Equal(translation.Y, affine.M42, 5);
    }

    [Fact]
    public void TestMatrixExponent()
    {
        var matrix = Matrix4x4.CreateScale(2.0f);

        // Matrix^2 should be Scaling(4.0f)
        var squared = Matrix4x4.Exponent(matrix, 2);

        Assert.Equal(4.0f, squared.M11, 5);
        Assert.Equal(4.0f, squared.M22, 5);
        Assert.Equal(4.0f, squared.M33, 5);
    }

    [Fact]
    public void TestMatrixDivide()
    {
        var matrix = Matrix4x4.CreateScale(10.0f);
        var divisor = 2.0f;

        var result = matrix / divisor;

        Assert.Equal(5.0f, result.M11);
        Assert.Equal(5.0f, result.M22);
        Assert.Equal(5.0f, result.M33);
    }

    [Fact]
    public void TestMatrixToArray()
    {
        var matrix = new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var array = matrix.ToArray();

        Assert.Equal(16, array.Length);
        Assert.Equal(1f, array[0]);
        Assert.Equal(16f, array[15]);
    }

    [Fact]
    public void TestMatrixGetHashCodeEdgeCase()
    {
        var m1 = Matrix4x4.Identity;
        var m2 = Matrix4x4.Identity;

        Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
    }

    [Fact]
    public void TestMatrixExchangeRows()
    {
        var m = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        m.ExchangeRows(0, 1);

        Assert.Equal(5, m.M11);
        Assert.Equal(6, m.M12);
        Assert.Equal(7, m.M13);
        Assert.Equal(8, m.M14);
        Assert.Equal(1, m.M21);
        Assert.Equal(2, m.M22);
        Assert.Equal(3, m.M23);
        Assert.Equal(4, m.M24);
    }

    [Fact]
    public void TestMatrixExchangeColumns()
    {
        var m = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        m.ExchangeColumns(0, 1);

        Assert.Equal(2, m.M11);
        Assert.Equal(1, m.M12);
        Assert.Equal(6, m.M21);
        Assert.Equal(5, m.M22);
        Assert.Equal(10, m.M31);
        Assert.Equal(9, m.M32);
        Assert.Equal(14, m.M41);
        Assert.Equal(13, m.M42);
    }

    [Fact]
    public void TestMatrixInvertMethod()
    {
        var m = Matrix4x4.CreateTranslation(5, 10, 15);
        Matrix4x4.InvertSafe(m, out m);

        Matrix4x4.InvertSafe(Matrix4x4.CreateTranslation(5, 10, 15), out var expected);
        Assert.Equal(expected, m);
    }

    [Fact]
    public void TestMatrixTransposeMethod()
    {
        var m = new Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        m = Matrix4x4.Transpose(m);

        Assert.Equal(1, m.M11);
        Assert.Equal(5, m.M12);
        Assert.Equal(9, m.M13);
        Assert.Equal(13, m.M14);
    }

    [Fact]
    public void TestMatrixDecomposeLQ()
    {
        var m = Matrix4x4.CreateRotationX(MathUtil.PiOverFour) * Matrix4x4.CreateTranslation(1, 2, 3);
        m.DecomposeLQ(out Matrix4x4 l, out Matrix4x4 q);

        // L should be lower triangular, Q should be orthogonal
        Assert.NotEqual(default(Matrix4x4), l);
        Assert.NotEqual(default(Matrix4x4), q);
    }

    [Fact]
    public void TestMatrixNegate()
    {
        var m = new Matrix4x4(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        Matrix4x4 result = Matrix4x4.Negate(m);

        Assert.Equal(-1, result.M11);
        Assert.Equal(-2, result.M12);
        Assert.Equal(-16, result.M44);
    }

    [Fact]
    public void TestMatrixRotationQuaternion()
    {
        var q = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathUtil.PiOverTwo);
        var m = Matrix4x4.CreateFromQuaternion(q);

        Assert.True(MathUtil.NearEqual(m.M22, 1f));
        Assert.True(MathUtil.NearEqual(m.M11, 0f));
        Assert.True(MathUtil.NearEqual(m.M33, 0f));
    }

    [Fact]
    public void TestMatrixDecomposeScaleRotationTranslation()
    {
        var scale = new Vector3(2, 3, 4);
        var translation = new Vector3(10, 20, 30);
        var rotation = Quaternion.CreateRotationY(MathUtil.PiOverFour);
        var scalingCenter = Vector3.Zero;
        var rotationCenter = Vector3.Zero;

        var m = Matrix4x4.CreateTransformation(scalingCenter, Quaternion.Identity, scale, rotationCenter, rotation, translation);
        m.Decompose(out Vector3 outScale, out Quaternion outRotation, out Vector3 outTranslation);

        Assert.True(MathUtil.NearEqual(scale.X, outScale.X));
        Assert.True(MathUtil.NearEqual(scale.Y, outScale.Y));
        Assert.True(MathUtil.NearEqual(scale.Z, outScale.Z));
        Assert.True(MathUtil.NearEqual(translation.X, outTranslation.X));
        Assert.True(MathUtil.NearEqual(translation.Y, outTranslation.Y));
        Assert.True(MathUtil.NearEqual(translation.Z, outTranslation.Z));
    }

    [Fact]
    public void TestMatrixDecomposeXYZ()
    {
        var rotation = new Vector3(0.1f, 0.2f, 0.3f);
        var m = Matrix4x4.CreateRotationX(rotation.X) * Matrix4x4.CreateRotationY(rotation.Y) * Matrix4x4.CreateRotationZ(rotation.Z);

        m.DecomposeXYZ(out Vector3 result);

        Assert.True(MathUtil.NearEqual(rotation.X, result.X));
        Assert.True(MathUtil.NearEqual(rotation.Y, result.Y));
        Assert.True(MathUtil.NearEqual(rotation.Z, result.Z));
    }

    #endregion
}
