// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

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

        var rotQuat = Quaternion.RotationYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        var rotMatrix = Matrix.RotationQuaternion(rotQuat);
        rotMatrix.Decompose(out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = rotQuat;
        var decompedQuat = Quaternion.RotationYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat == decompedQuat || expectedQuat == -decompedQuat, $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.YRPTestData))]
    public void TestDecomposeYawPitchRollFromMatrixYPR(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        var rotMatrix = Matrix.RotationYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        rotMatrix.Decompose(out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = Quaternion.RotationYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        var decompedQuat = Quaternion.RotationYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat == decompedQuat || expectedQuat == -decompedQuat, $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.YRPTestData))]
    public void TestDecomposeYawPitchRollFromMatricesZXY(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        // Yaw-Pitch-Roll is the intrinsic rotation order, so extrinsic is the reverse (ie. Z-X-Y)
        var rotMatrix = Matrix.RotationZ(rollRadians) * Matrix.RotationX(pitchRadians) * Matrix.RotationY(yawRadians);
        rotMatrix.Decompose(out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = Quaternion.RotationYawPitchRoll(yawRadians, pitchRadians, rollRadians);
        var decompedQuat = Quaternion.RotationYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat == decompedQuat || expectedQuat == -decompedQuat, $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.XYZTestData))]
    public void TestDecomposeXYZFromMatricesXYZ(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        var rotMatrix = Matrix.RotationX(pitchRadians) * Matrix.RotationY(yawRadians) * Matrix.RotationZ(rollRadians);
        rotMatrix.DecomposeXYZ(out Vector3 eulerAngles);

        var decompedRotMatrix = Matrix.RotationX(eulerAngles.X) * Matrix.RotationY(eulerAngles.Y) * Matrix.RotationZ(eulerAngles.Z);
        var decompedQuat = Quaternion.RotationMatrix(decompedRotMatrix);

        var expectedQuat = Quaternion.RotationX(pitchRadians) * Quaternion.RotationY(yawRadians) * Quaternion.RotationZ(rollRadians);
        Assert.True(expectedQuat == decompedQuat || expectedQuat == -decompedQuat, $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Fact]
    public void TestNumericConversion()
    {
        System.Numerics.Matrix4x4 matrix = new System.Numerics.Matrix4x4(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Matrix baseStrideMatrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Matrix strideMatrix = matrix;
        Assert.Equal(baseStrideMatrix, strideMatrix);
    }

    [Fact]
    public void TestStrideConversion()
    {
        Matrix matrix = new(
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

    #region Matrix Operations Tests

    [Fact]
    public void TestMatrixMultiplication()
    {
        var m1 = new Matrix(
            1, 2, 3, 0,
            4, 5, 6, 0,
            7, 8, 9, 0,
            0, 0, 0, 1);

        var m2 = new Matrix(
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
        var matrix = new Matrix(
            2, 0, 0, 0,
            0, 3, 0, 0,
            0, 0, 4, 0,
            1, 2, 3, 1);

        var vector = new Vector3(1, 1, 1);
        var result = Vector3.Transform(vector, matrix);

        Assert.Equal(3f, result.X); // 2*1 + 0*1 + 0*1 + 1*1
        Assert.Equal(5f, result.Y); // 0*1 + 3*1 + 0*1 + 2*1
        Assert.Equal(7f, result.Z); // 0*1 + 0*1 + 4*1 + 3*1
    }

    [Fact]
    public void TestMatrixDeterminant()
    {
        var matrix = new Matrix(
            1, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 3, 0,
            0, 0, 0, 1);

        float det = matrix.Determinant();
        Assert.Equal(6f, det); // 1 * 2 * 3 * 1
    }

    [Fact]
    public void TestMatrixInverse()
    {
        var matrix = new Matrix(
            2, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 2, 0,
            1, 2, 3, 1);

        Matrix.Invert(ref matrix, out var inverse);
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
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Matrix.Transpose(ref matrix, out var transpose);

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
        Matrix.Transpose(ref transpose, out var doubleTranspose);
        Assert.Equal(matrix, doubleTranspose);
    }

    [Theory]
    [InlineData(0, 0, 0)]
    [InlineData(90, 0, 0)]
    [InlineData(0, 90, 0)]
    [InlineData(0, 0, 90)]
    [InlineData(45, 45, 45)]
    public void TestRotationMatrixDecomposition(float x, float y, float z)
    {
        var rotationMatrix = Matrix.RotationX(MathUtil.DegreesToRadians(x)) *
                           Matrix.RotationY(MathUtil.DegreesToRadians(y)) *
                           Matrix.RotationZ(MathUtil.DegreesToRadians(z));

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
        var reconstructed = Matrix.Transformation(Vector3.Zero, Quaternion.Identity, scale, Vector3.Zero, rotation, translation);

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
    public void TestMatrixTransformation()
    {
        var scale = new Vector3(2, 3, 4);
        var rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(30),
            MathUtil.DegreesToRadians(45),
            MathUtil.DegreesToRadians(60)
        );
        var translation = new Vector3(1, 2, 3);

        var transform = Matrix.Transformation(
            Vector3.Zero,    // scaling center
            Quaternion.Identity,  // scaling rotation
            scale,          // scale
            Vector3.Zero,    // rotation center
            rotation,       // rotation
            translation    // translation
        );

        // Test transformation of a point
        var point = new Vector3(1, 1, 1);
        var transformed = Vector3.Transform(point, transform);

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
        var scaleMatrix = Matrix.Scaling(scale);

        // Test scaling of a point
        var point = new Vector3(1, 1, 1);
        var scaled = Vector3.Transform(point, scaleMatrix);

        Assert.Equal(2f, scaled.X);
        Assert.Equal(3f, scaled.Y);
        Assert.Equal(4f, scaled.Z);
    }

    [Fact]
    public void TestMatrixConstruction()
    {
        // Test value constructor
        var m1 = new Matrix(2.0f);
        Assert.Equal(2.0f, m1.M11);
        Assert.Equal(2.0f, m1.M22);
        Assert.Equal(2.0f, m1.M33);
        Assert.Equal(2.0f, m1.M44);

        // Test component constructor
        var m2 = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Assert.Equal(1f, m2.M11);
        Assert.Equal(2f, m2.M12);
        Assert.Equal(16f, m2.M44);

        // Test array constructor
        float[] values = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16];
        var m3 = new Matrix(values);
        Assert.Equal(1f, m3.M11);
        Assert.Equal(16f, m3.M44);
    }

    [Fact]
    public void TestMatrixStaticFields()
    {
        // Test Zero matrix
        Assert.Equal(0f, Matrix.Zero.M11);
        Assert.Equal(0f, Matrix.Zero.M22);
        Assert.Equal(0f, Matrix.Zero.M33);
        Assert.Equal(0f, Matrix.Zero.M44);

        // Test Identity matrix
        Assert.Equal(1f, Matrix.Identity.M11);
        Assert.Equal(1f, Matrix.Identity.M22);
        Assert.Equal(1f, Matrix.Identity.M33);
        Assert.Equal(1f, Matrix.Identity.M44);
        Assert.Equal(0f, Matrix.Identity.M12);
        Assert.Equal(0f, Matrix.Identity.M41);

        // Test IsIdentity property
        Assert.True(Matrix.Identity.IsIdentity);
        Assert.False(Matrix.Zero.IsIdentity);
    }

    [Fact]
    public void TestMatrixTranslation()
    {
        var translation = new Vector3(10, 20, 30);
        var matrix = Matrix.Translation(translation);

        Assert.Equal(10f, matrix.M41);
        Assert.Equal(20f, matrix.M42);
        Assert.Equal(30f, matrix.M43);
        Assert.Equal(1f, matrix.M44);

        // Test TranslationVector property
        Assert.Equal(translation, matrix.TranslationVector);

        // Test transforming a point
        var point = new Vector3(1, 2, 3);
        var transformed = Vector3.Transform(point, matrix);
        Assert.Equal(11f, transformed.X);
        Assert.Equal(22f, transformed.Y);
        Assert.Equal(33f, transformed.Z);
    }

    [Fact]
    public void TestMatrixShadow()
    {
        var light = new Vector4(0, 10, 0, 1); // Light above
        var plane = new Plane(Vector3.UnitY, 0); // Ground plane

        var matrix = Matrix.Shadow(light, plane);

        // Shadow matrix should project points onto the plane
        var point = new Vector3(1, 5, 1);
        var shadow = Vector3.Transform(point, matrix);

        // Shadow should be on the ground plane (Y ≈ 0)
        Assert.Equal(0f, shadow.Y, 2);
    }

    [Fact]
    public void TestMatrixRotationYawPitchRoll()
    {
        float yaw = MathUtil.PiOverFour;
        float pitch = MathUtil.PiOverFour / 2;
        float roll = MathUtil.PiOverFour / 3;

        var matrix = Matrix.RotationYawPitchRoll(yaw, pitch, roll);

        // Should create a valid rotation matrix
        Assert.NotEqual(0f, matrix.Determinant());

        // Rotation matrices should have determinant close to ±1
        Assert.Equal(1f, Math.Abs(matrix.Determinant()), 1);
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

        var matrix = Matrix.Transformation2D(scalingCenter, scalingRotation, scaling, rotationCenter, rotation, translation);

        // Should create a valid 2D transformation matrix
        Assert.NotEqual(0f, matrix.Determinant());
    }

    [Fact]
    public void TestMatrixArrayAccess()
    {
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Assert.Equal(1f, matrix[0]);
        Assert.Equal(2f, matrix[1]);
        Assert.Equal(16f, matrix[15]);

        // Test setter
        matrix[0] = 100f;
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

        var matrix = (Matrix)numeric;

        Assert.Equal(1f, matrix.M11);
        Assert.Equal(2f, matrix.M12);
        Assert.Equal(16f, matrix.M44);
    }

    [Fact]
    public void TestMatrixToNumeric()
    {
        var matrix = new Matrix(
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

        var matrix = Matrix.OrthoRH(width, height, znear, zfar);

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

        var matrix = Matrix.OrthoOffCenterLH(left, right, bottom, top, znear, zfar);

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

        var matrix = Matrix.PerspectiveLH(width, height, znear, zfar);

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

        var matrix = Matrix.LookAtRH(eye, target, up);

        // LookAt matrix should be invertible
        Assert.NotEqual(0f, matrix.Determinant());
    }

    [Fact]
    public void TestMatrixSmoothStep()
    {
        var start = Matrix.Identity;
        var end = Matrix.Scaling(2f);
        float amount = 0.5f;

        var result = Matrix.SmoothStep(start, end, amount);

        // Result should be between start and end
        Assert.True(result.M11 > 1f && result.M11 < 2f);
    }

    [Fact]
    public void TestMatrixOrthogonalize()
    {
        // Create a matrix that's slightly non-orthogonal
        var matrix = Matrix.RotationY(0.1f);
        matrix.M12 += 0.01f; // Perturb it slightly

        var ortho = Matrix.Orthogonalize(matrix);

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
        var matrix = new Matrix(
            2, 0, 0, 0,
            0, 2, 0, 0,
            0, 0, 2, 0,
            0, 0, 0, 1);

        var orthonormal = Matrix.Orthonormalize(matrix);

        // After orthonormalization, row vectors should have unit length
        var row1 = new Vector3(orthonormal.M11, orthonormal.M12, orthonormal.M13);
        Assert.Equal(1f, row1.Length(), 3);
    }

    #endregion

    #region Matrix Edge Cases Tests

    [Fact]
    public void TestMatrixIdentity()
    {
        var identity = Matrix.Identity;

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
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var result1 = matrix * Matrix.Identity;
        var result2 = Matrix.Identity * matrix;

        Assert.Equal(matrix, result1);
        Assert.Equal(matrix, result2);
    }

    [Fact]
    public void TestMatrixTransposeEdgeCase()
    {
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Matrix.Transpose(ref matrix, out var transposed);

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
        var identity = Matrix.Identity;
        Matrix.Invert(ref identity, out var inverse);

        Assert.Equal(identity, inverse);
    }

    [Fact]
    public void TestMatrixInverseSingular()
    {
        // Singular matrix (determinant = 0)
        var singular = new Matrix(
            1, 2, 3, 0,
            2, 4, 6, 0,
            3, 6, 9, 0,
            0, 0, 0, 1);

        Matrix.Invert(ref singular, out var inverse);

        // Inverse of singular matrix - implementation may return specific values
        // Just verify it doesn't crash
        Assert.True(true); // Test passes if we reach here
    }

    [Fact]
    public void TestMatrixDeterminantIdentity()
    {
        var identity = Matrix.Identity;
        var det = identity.Determinant();

        Assert.Equal(1.0f, det);
    }

    [Fact]
    public void TestMatrixDeterminantZero()
    {
        // Matrix with determinant 0
        var matrix = new Matrix(
            1, 2, 3, 0,
            2, 4, 6, 0,
            3, 6, 9, 0,
            0, 0, 0, 1);

        var det = matrix.Determinant();

        Assert.Equal(0.0f, det, 5);
    }

    [Fact]
    public void TestMatrixTranslationEdgeCase()
    {
        var translation = Matrix.Translation(5.0f, 10.0f, 15.0f);
        var point = new Vector3(1.0f, 1.0f, 1.0f);

        var transformed = Vector3.TransformCoordinate(point, translation);

        Assert.Equal(6.0f, transformed.X);
        Assert.Equal(11.0f, transformed.Y);
        Assert.Equal(16.0f, transformed.Z);
    }

    [Fact]
    public void TestMatrixScalingEdgeCase()
    {
        var scaling = Matrix.Scaling(2.0f, 3.0f, 4.0f);
        var point = new Vector3(1.0f, 1.0f, 1.0f);

        var transformed = Vector3.TransformCoordinate(point, scaling);

        Assert.Equal(2.0f, transformed.X);
        Assert.Equal(3.0f, transformed.Y);
        Assert.Equal(4.0f, transformed.Z);
    }

    [Fact]
    public void TestMatrixUniformScaling()
    {
        var scaling = Matrix.Scaling(2.0f);
        var point = new Vector3(1.0f, 2.0f, 3.0f);

        var transformed = Vector3.TransformCoordinate(point, scaling);

        Assert.Equal(2.0f, transformed.X);
        Assert.Equal(4.0f, transformed.Y);
        Assert.Equal(6.0f, transformed.Z);
    }

    [Fact]
    public void TestMatrixRotationX90Degrees()
    {
        var rotation = Matrix.RotationX(MathUtil.PiOverTwo);
        var point = new Vector3(0.0f, 1.0f, 0.0f);

        var transformed = Vector3.TransformCoordinate(point, rotation);

        Assert.Equal(0.0f, transformed.X, 5);
        Assert.Equal(0.0f, transformed.Y, 5);
        Assert.Equal(1.0f, transformed.Z, 5);
    }

    [Fact]
    public void TestMatrixRotationY90Degrees()
    {
        var rotation = Matrix.RotationY(MathUtil.PiOverTwo);
        var point = new Vector3(1.0f, 0.0f, 0.0f);

        var transformed = Vector3.TransformCoordinate(point, rotation);

        Assert.Equal(0.0f, transformed.X, 5);
        Assert.Equal(0.0f, transformed.Y, 5);
        Assert.Equal(-1.0f, transformed.Z, 5);
    }

    [Fact]
    public void TestMatrixRotationZ90Degrees()
    {
        var rotation = Matrix.RotationZ(MathUtil.PiOverTwo);
        var point = new Vector3(1.0f, 0.0f, 0.0f);

        var transformed = Vector3.TransformCoordinate(point, rotation);

        Assert.Equal(0.0f, transformed.X, 5);
        Assert.Equal(1.0f, transformed.Y, 5);
        Assert.Equal(0.0f, transformed.Z, 5);
    }

    [Fact]
    public void TestMatrixIsIdentity()
    {
        var identity = Matrix.Identity;
        Assert.True(identity.IsIdentity);

        var notIdentity = Matrix.Translation(1, 0, 0);
        Assert.False(notIdentity.IsIdentity);
    }

    [Fact]
    public void TestMatrixEquality()
    {
        var m1 = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var m2 = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var m3 = Matrix.Identity;

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
        var matrix = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var negated = -matrix;

        Assert.Equal(-1f, negated.M11);
        Assert.Equal(-2f, negated.M12);
        Assert.Equal(-16f, negated.M44);
    }

    [Fact]
    public void TestMatrixAddition()
    {
        var m1 = Matrix.Identity;
        var m2 = Matrix.Identity;

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
        var m1 = Matrix.Scaling(2.0f);
        var m2 = Matrix.Identity;

        var diff = m1 - m2;

        Assert.Equal(1.0f, diff.M11);
        Assert.Equal(1.0f, diff.M22);
        Assert.Equal(1.0f, diff.M33);
        Assert.Equal(0.0f, diff.M44);
    }

    [Fact]
    public void TestMatrixScalarMultiplication()
    {
        var matrix = Matrix.Identity;
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
        var matrix = Matrix.Scaling(4.0f);
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
        var matrix = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var row1 = matrix.Row1;
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
        var translation = Matrix.Translation(5.0f, 10.0f, 15.0f);

        translation.Decompose(out Vector3 scale, out Quaternion rotation, out Vector3 trans);

        Assert.Equal(new Vector3(1, 1, 1), scale);
        Assert.Equal(Quaternion.Identity, rotation);
        Assert.Equal(new Vector3(5, 10, 15), trans);
    }

    [Fact]
    public void TestMatrixDecomposeScaleOnly()
    {
        var scaling = Matrix.Scaling(2.0f, 3.0f, 4.0f);

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
        var start = Matrix.Identity;
        var end = Matrix.Scaling(2.0f);

        var halfway = Matrix.Lerp(start, end, 0.5f);

        // At 0.5, should be halfway between identity (1) and scaling (2)
        Assert.Equal(1.5f, halfway.M11, 5);
        Assert.Equal(1.5f, halfway.M22, 5);
        Assert.Equal(1.5f, halfway.M33, 5);
        Assert.Equal(1.0f, halfway.M44, 5);

        // At 0, should equal start
        var atStart = Matrix.Lerp(start, end, 0.0f);
        Assert.Equal(start, atStart);

        // At 1, should equal end
        var atEnd = Matrix.Lerp(start, end, 1.0f);
        Assert.Equal(end, atEnd);
    }

    [Fact]
    public void TestMatrixBillboard()
    {
        var objectPos = new Vector3(5, 0, 0);
        var cameraPos = Vector3.Zero;
        var cameraUp = Vector3.UnitY;
        var cameraForward = Vector3.UnitZ;

        var billboard = Matrix.Billboard(objectPos, cameraPos, cameraUp, cameraForward);

        // Billboard matrix should be invertible
        Assert.NotEqual(0f, billboard.Determinant());

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
        var reflection = Matrix.Reflection(plane);

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

        var matrix = Matrix.PerspectiveFovLH(fov, aspect, znear, zfar);

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

        var matrix = Matrix.PerspectiveFovRH(fov, aspect, znear, zfar);

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

        var lookAt = Matrix.LookAtLH(eye, target, up);

        // LookAt matrix should be invertible
        Assert.NotEqual(0f, lookAt.Determinant());
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

        var ortho = Matrix.OrthoOffCenterRH(left, right, bottom, top, znear, zfar);

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

        var perspective = Matrix.PerspectiveRH(width, height, znear, zfar);

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

        var perspective = Matrix.PerspectiveOffCenterLH(left, right, bottom, top, znear, zfar);

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

        var perspective = Matrix.PerspectiveOffCenterRH(left, right, bottom, top, znear, zfar);

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

        var ortho = Matrix.OrthoLH(width, height, znear, zfar);

        Assert.Equal(1f, ortho.M44);
        Assert.NotEqual(0f, ortho.M11);
        Assert.NotEqual(0f, ortho.M22);
    }

    [Fact]
    public void TestMatrixRotationAxis()
    {
        var axis = Vector3.Normalize(new Vector3(1, 1, 0));
        var angle = MathUtil.PiOverFour;

        var rotation = Matrix.RotationAxis(axis, angle);

        // Should be a valid rotation matrix (determinant ≈ ±1)
        Assert.Equal(1f, Math.Abs(rotation.Determinant()), 3);
    }

    [Fact]
    public void TestMatrixAffineTransformation()
    {
        float scaling = 2.0f;
        var rotation = Quaternion.RotationY(MathUtil.PiOverFour);
        var translation = new Vector3(10, 20, 30);

        var affine = Matrix.AffineTransformation(scaling, rotation, translation);

        // Should create valid transformation
        Assert.NotEqual(Matrix.Zero, affine);
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

        var affine = Matrix.AffineTransformation2D(scaling, rotation, translation);

        // Should create valid transformation
        Assert.NotEqual(Matrix.Zero, affine);
        Assert.Equal(translation.X, affine.M41, 5);
        Assert.Equal(translation.Y, affine.M42, 5);
    }

    [Fact]
    public void TestMatrixExponent()
    {
        var matrix = Matrix.Scaling(2.0f);

        // Matrix^2 should be Scaling(4.0f)
        var squared = Matrix.Exponent(matrix, 2);

        Assert.Equal(4.0f, squared.M11, 5);
        Assert.Equal(4.0f, squared.M22, 5);
        Assert.Equal(4.0f, squared.M33, 5);
    }

    [Fact]
    public void TestMatrixDivide()
    {
        var matrix = Matrix.Scaling(10.0f);
        var divisor = 2.0f;

        var result = matrix / divisor;

        Assert.Equal(5.0f, result.M11);
        Assert.Equal(5.0f, result.M22);
        Assert.Equal(5.0f, result.M33);
    }

    [Fact]
    public void TestMatrixToArray()
    {
        var matrix = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        var array = matrix.ToArray();

        Assert.Equal(16, array.Length);
        Assert.Equal(1f, array[0]);
        Assert.Equal(16f, array[15]);
    }

    [Fact]
    public void TestMatrixGetHashCodeEdgeCase()
    {
        var m1 = Matrix.Identity;
        var m2 = Matrix.Identity;

        Assert.Equal(m1.GetHashCode(), m2.GetHashCode());
    }

    [Fact]
    public void TestMatrixExchangeRows()
    {
        var m = new Matrix(
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
        var m = new Matrix(
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
        var m = Matrix.Translation(5, 10, 15);
        m.Invert();

        var expected = Matrix.Invert(Matrix.Translation(5, 10, 15));
        Assert.Equal(expected, m);
    }

    [Fact]
    public void TestMatrixTransposeMethod()
    {
        var m = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        m.Transpose();

        Assert.Equal(1, m.M11);
        Assert.Equal(5, m.M12);
        Assert.Equal(9, m.M13);
        Assert.Equal(13, m.M14);
    }

    [Fact]
    public void TestMatrixDecomposeLQ()
    {
        var m = Matrix.RotationX(MathUtil.PiOverFour) * Matrix.Translation(1, 2, 3);
        m.DecomposeLQ(out Matrix l, out Matrix q);

        // L should be lower triangular, Q should be orthogonal
        Assert.NotEqual(Matrix.Zero, l);
        Assert.NotEqual(Matrix.Zero, q);
    }

    [Fact]
    public void TestMatrixNegate()
    {
        var m = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16);
        Matrix.Negate(ref m, out Matrix result);

        Assert.Equal(-1, result.M11);
        Assert.Equal(-2, result.M12);
        Assert.Equal(-16, result.M44);
    }

    [Fact]
    public void TestMatrixRotationQuaternion()
    {
        var q = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.PiOverTwo);
        var m = Matrix.RotationQuaternion(q);

        Assert.True(MathUtil.NearEqual(m.M22, 1f));
        Assert.True(MathUtil.NearEqual(m.M11, 0f));
        Assert.True(MathUtil.NearEqual(m.M33, 0f));
    }

    [Fact]
    public void TestMatrixDecomposeScaleRotationTranslation()
    {
        var scale = new Vector3(2, 3, 4);
        var translation = new Vector3(10, 20, 30);
        var rotation = Quaternion.RotationY(MathUtil.PiOverFour);
        var scalingCenter = Vector3.Zero;
        var rotationCenter = Vector3.Zero;

        var m = Matrix.Transformation(scalingCenter, Quaternion.Identity, scale, rotationCenter, rotation, translation);
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
        var m = Matrix.RotationX(rotation.X) * Matrix.RotationY(rotation.Y) * Matrix.RotationZ(rotation.Z);

        m.DecomposeXYZ(out Vector3 result);

        Assert.True(MathUtil.NearEqual(rotation.X, result.X));
        Assert.True(MathUtil.NearEqual(rotation.Y, result.Y));
        Assert.True(MathUtil.NearEqual(rotation.Z, result.Z));
    }

    #endregion
}
