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

    #region Additional Coverage Tests

    private static void AssertUnitLength(Vector4 v) => Assert.Equal(1f, v.Length(), 3);

    private static void AssertVector4NearEqual(Vector4 expected, Vector4 actual)
    {
        Assert.Equal(expected.X, actual.X, 2);
        Assert.Equal(expected.Y, actual.Y, 2);
        Assert.Equal(expected.Z, actual.Z, 2);
        Assert.Equal(expected.W, actual.W, 2);
    }

    [Fact]
    public void TestMatrixRowsAndColumnsGettersSetters()
    {
        var m = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Assert.Equal(new Vector4(5, 6, 7, 8), m.Row2);
        Assert.Equal(new Vector4(9, 10, 11, 12), m.Row3);
        Assert.Equal(new Vector4(13, 14, 15, 16), m.Row4);

        Assert.Equal(new Vector4(2, 6, 10, 14), m.Column2);
        Assert.Equal(new Vector4(3, 7, 11, 15), m.Column3);
        Assert.Equal(new Vector4(4, 8, 12, 16), m.Column4);

        m.Row2 = new Vector4(50, 60, 70, 80);
        Assert.Equal(50f, m.M21);
        Assert.Equal(60f, m.M22);
        Assert.Equal(70f, m.M23);
        Assert.Equal(80f, m.M24);

        m.Column3 = new Vector4(101, 102, 103, 104);
        Assert.Equal(101f, m.M13);
        Assert.Equal(102f, m.M23);
        Assert.Equal(103f, m.M33);
        Assert.Equal(104f, m.M43);
    }

    [Fact]
    public void TestMatrixDirectionProperties()
    {
        var m = Matrix.Identity;
        m.Right = new Vector3(1, 0, 0);
        m.Up = new Vector3(0, 1, 0);
        m.Backward = new Vector3(0, 0, 1);

        Assert.Equal(new Vector3(1, 0, 0), m.Right);
        Assert.Equal(new Vector3(-1, 0, 0), m.Left);
        Assert.Equal(new Vector3(0, 1, 0), m.Up);
        Assert.Equal(new Vector3(0, -1, 0), m.Down);
        Assert.Equal(new Vector3(0, 0, 1), m.Backward);
        Assert.Equal(new Vector3(0, 0, -1), m.Forward);

        m.Left = new Vector3(-2, 0, 0);
        Assert.Equal(new Vector3(2, 0, 0), m.Right);

        m.Down = new Vector3(0, -3, 0);
        Assert.Equal(new Vector3(0, 3, 0), m.Up);

        m.Forward = new Vector3(0, 0, -4);
        Assert.Equal(new Vector3(0, 0, 4), m.Backward);
    }

    [Fact]
    public void TestMatrixScaleVectorAndTranslationVectorSetters()
    {
        var m = Matrix.Identity;
        m.ScaleVector = new Vector3(2, 3, 4);
        Assert.Equal(2f, m.M11);
        Assert.Equal(3f, m.M22);
        Assert.Equal(4f, m.M33);

        m.TranslationVector = new Vector3(10, 20, 30);
        Assert.Equal(10f, m.M41);
        Assert.Equal(20f, m.M42);
        Assert.Equal(30f, m.M43);
    }

    [Fact]
    public void TestMatrixIndexer2D()
    {
        var m = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        Assert.Equal(1f, m[0, 0]);
        Assert.Equal(6f, m[1, 1]);
        Assert.Equal(16f, m[3, 3]);

        m[2, 1] = 100f;
        Assert.Equal(100f, m.M32);
        Assert.Equal(100f, m[2, 1]);
    }

    [Fact]
    public void TestMatrixIndexerOutOfRange()
    {
        var m = Matrix.Identity;

        Assert.Throws<ArgumentOutOfRangeException>(() => m[16]);
        Assert.Throws<ArgumentOutOfRangeException>(() => m[-1]);
        Assert.Throws<ArgumentOutOfRangeException>(() => m[16] = 1f);

        Assert.Throws<ArgumentOutOfRangeException>(() => m[4, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => m[0, 4]);
        Assert.Throws<ArgumentOutOfRangeException>(() => m[-1, 0]);
        Assert.Throws<ArgumentOutOfRangeException>(() => m[0, 4] = 1f);
    }

    [Fact]
    public void TestMatrixExchangeRowsOutOfRange()
    {
        var m = Matrix.Identity;
        Assert.Throws<ArgumentOutOfRangeException>(() => m.ExchangeRows(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => m.ExchangeRows(0, 4));
    }

    [Fact]
    public void TestMatrixExchangeColumnsOutOfRange()
    {
        var m = Matrix.Identity;
        Assert.Throws<ArgumentOutOfRangeException>(() => m.ExchangeColumns(-1, 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => m.ExchangeColumns(0, 4));
    }

    [Fact]
    public void TestMatrixExchangeRowsSameIndexIsNoOp()
    {
        var m = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);
        var copy = m;

        m.ExchangeRows(2, 2);

        Assert.Equal(copy, m);
    }

    [Fact]
    public void TestMatrixDecomposeScaleTranslationOnly()
    {
        var m = Matrix.Scaling(2, 3, 4) * Matrix.Translation(5, 6, 7);

        bool success = m.Decompose(out Vector3 scale, out Vector3 translation);

        Assert.True(success);
        Assert.Equal(2f, scale.X, 3);
        Assert.Equal(3f, scale.Y, 3);
        Assert.Equal(4f, scale.Z, 3);
        Assert.Equal(5f, translation.X, 3);
        Assert.Equal(6f, translation.Y, 3);
        Assert.Equal(7f, translation.Z, 3);
    }

    [Fact]
    public void TestMatrixDecomposeScaleTranslationFailsWithZeroScale()
    {
        var m = Matrix.Scaling(0, 1, 1);

        bool success = m.Decompose(out Vector3 scale, out Vector3 translation);

        Assert.False(success);
    }

    [Fact]
    public void TestMatrixDecomposeComposedTransformRoundTrip()
    {
        var scale = new Vector3(2, 5, 3);
        var rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(25), MathUtil.DegreesToRadians(40), MathUtil.DegreesToRadians(-15));
        var translation = new Vector3(7, -3, 12);

        var composed = Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);
        composed.Decompose(out Vector3 outScale, out Quaternion outRotation, out Vector3 outTranslation);

        Assert.Equal(scale.X, outScale.X, 3);
        Assert.Equal(scale.Y, outScale.Y, 3);
        Assert.Equal(scale.Z, outScale.Z, 3);
        Assert.Equal(translation.X, outTranslation.X, 3);
        Assert.Equal(translation.Y, outTranslation.Y, 3);
        Assert.Equal(translation.Z, outTranslation.Z, 3);
        Assert.True(rotation == outRotation || rotation == -outRotation);

        // Rebuilding from the decomposed parts should reproduce the composed matrix.
        var rebuilt = Matrix.Scaling(outScale) * Matrix.RotationQuaternion(outRotation) * Matrix.Translation(outTranslation);
        for (int i = 0; i < 16; i++)
            Assert.Equal(composed[i], rebuilt[i], 2);
    }

    [Fact]
    public void TestMatrixDecomposeReflectionNegativeScale()
    {
        var scale = new Vector3(-2, 3, 4);
        var rotation = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(20), MathUtil.DegreesToRadians(35), MathUtil.DegreesToRadians(50));
        var translation = new Vector3(1, -2, 3);

        var m = Matrix.Scaling(scale) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

        bool success = m.Decompose(out Vector3 outScale, out Matrix outRotation, out Vector3 outTranslation);

        Assert.True(success);
        Assert.Equal(translation.X, outTranslation.X, 3);
        Assert.Equal(translation.Y, outTranslation.Y, 3);
        Assert.Equal(translation.Z, outTranslation.Z, 3);

        // The magnitude of each scale component must be preserved, even though the sign may be
        // redistributed across axes to keep the recovered rotation a proper (non-reflective) rotation.
        Assert.Equal(Math.Abs(scale.X), Math.Abs(outScale.X), 3);
        Assert.Equal(Math.Abs(scale.Y), Math.Abs(outScale.Y), 3);
        Assert.Equal(Math.Abs(scale.Z), Math.Abs(outScale.Z), 3);

        // The recovered rotation matrix must be a proper rotation (determinant ~= +1, no reflection).
        Assert.Equal(1f, outRotation.Determinant(), 3);

        // Reconstructing scale * rotation * translation from the decomposed parts reproduces the original matrix.
        var reconstructed = Matrix.Scaling(outScale) * outRotation * Matrix.Translation(outTranslation);
        for (int i = 0; i < 16; i++)
            Assert.Equal(m[i], reconstructed[i], 2);
    }

    [Fact]
    public void TestMatrixDecomposeQR()
    {
        var a = Matrix.RotationYawPitchRoll(0.3f, 0.5f, 0.7f) * Matrix.Scaling(2, 3, 4) * Matrix.Translation(5, -2, 9);

        a.DecomposeQR(out Matrix q, out Matrix r);

        // Q's columns must be orthonormal.
        AssertUnitLength(q.Column1);
        AssertUnitLength(q.Column2);
        AssertUnitLength(q.Column3);
        AssertUnitLength(q.Column4);
        Assert.Equal(0f, Vector4.Dot(q.Column1, q.Column2), 3);
        Assert.Equal(0f, Vector4.Dot(q.Column1, q.Column3), 3);
        Assert.Equal(0f, Vector4.Dot(q.Column1, q.Column4), 3);
        Assert.Equal(0f, Vector4.Dot(q.Column2, q.Column3), 3);
        Assert.Equal(0f, Vector4.Dot(q.Column2, q.Column4), 3);
        Assert.Equal(0f, Vector4.Dot(q.Column3, q.Column4), 3);

        // R must be (right) upper triangular.
        Assert.Equal(0f, r.M21, 3);
        Assert.Equal(0f, r.M31, 3);
        Assert.Equal(0f, r.M41, 3);
        Assert.Equal(0f, r.M32, 3);
        Assert.Equal(0f, r.M42, 3);
        Assert.Equal(0f, r.M43, 3);

        // Q * R (as a standard column-vector matrix product) must reconstruct the original columns of A.
        var col1 = q.Column1 * r.M11;
        var col2 = (q.Column1 * r.M12) + (q.Column2 * r.M22);
        var col3 = (q.Column1 * r.M13) + (q.Column2 * r.M23) + (q.Column3 * r.M33);
        var col4 = (q.Column1 * r.M14) + (q.Column2 * r.M24) + (q.Column3 * r.M34) + (q.Column4 * r.M44);

        AssertVector4NearEqual(a.Column1, col1);
        AssertVector4NearEqual(a.Column2, col2);
        AssertVector4NearEqual(a.Column3, col3);
        AssertVector4NearEqual(a.Column4, col4);
    }

    [Fact]
    public void TestMatrixDecomposeLQReconstruction()
    {
        var a = Matrix.RotationYawPitchRoll(0.2f, 0.6f, 0.9f) * Matrix.Scaling(3, 1, 5) * Matrix.Translation(-3, 4, 2);

        a.DecomposeLQ(out Matrix l, out Matrix q);

        // Q's rows must be orthonormal.
        AssertUnitLength(q.Row1);
        AssertUnitLength(q.Row2);
        AssertUnitLength(q.Row3);
        AssertUnitLength(q.Row4);
        Assert.Equal(0f, Vector4.Dot(q.Row1, q.Row2), 3);
        Assert.Equal(0f, Vector4.Dot(q.Row1, q.Row3), 3);
        Assert.Equal(0f, Vector4.Dot(q.Row1, q.Row4), 3);
        Assert.Equal(0f, Vector4.Dot(q.Row2, q.Row3), 3);
        Assert.Equal(0f, Vector4.Dot(q.Row2, q.Row4), 3);
        Assert.Equal(0f, Vector4.Dot(q.Row3, q.Row4), 3);

        // L must be lower triangular.
        Assert.Equal(0f, l.M12, 3);
        Assert.Equal(0f, l.M13, 3);
        Assert.Equal(0f, l.M14, 3);
        Assert.Equal(0f, l.M23, 3);
        Assert.Equal(0f, l.M24, 3);
        Assert.Equal(0f, l.M34, 3);

        // L * Q (as a standard row-vector matrix product) must reconstruct the original rows of A.
        var row1 = l.M11 * q.Row1;
        var row2 = (l.M21 * q.Row1) + (l.M22 * q.Row2);
        var row3 = (l.M31 * q.Row1) + (l.M32 * q.Row2) + (l.M33 * q.Row3);
        var row4 = (l.M41 * q.Row1) + (l.M42 * q.Row2) + (l.M43 * q.Row3) + (l.M44 * q.Row4);

        AssertVector4NearEqual(a.Row1, row1);
        AssertVector4NearEqual(a.Row2, row2);
        AssertVector4NearEqual(a.Row3, row3);
        AssertVector4NearEqual(a.Row4, row4);
    }

    [Fact]
    public void TestMatrixUpperTriangularForm()
    {
        var m = new Matrix(
            4, 1, 2, 1,
            1, 5, 1, 2,
            2, 1, 6, 1,
            1, 2, 1, 7);

        Matrix.UpperTriangularForm(ref m, out var result);

        // Entries strictly below the diagonal must be zero.
        Assert.Equal(0f, result[1, 0], 2);
        Assert.Equal(0f, result[2, 0], 2);
        Assert.Equal(0f, result[2, 1], 2);
        Assert.Equal(0f, result[3, 0], 2);
        Assert.Equal(0f, result[3, 1], 2);
        Assert.Equal(0f, result[3, 2], 2);
    }

    [Fact]
    public void TestMatrixLowerTriangularForm()
    {
        var m = new Matrix(
            4, 1, 2, 1,
            1, 5, 1, 2,
            2, 1, 6, 1,
            1, 2, 1, 7);

        Matrix.LowerTriangularForm(ref m, out var result);

        // Entries strictly above the diagonal must be zero.
        Assert.Equal(0f, result[0, 1], 2);
        Assert.Equal(0f, result[0, 2], 2);
        Assert.Equal(0f, result[0, 3], 2);
        Assert.Equal(0f, result[1, 2], 2);
        Assert.Equal(0f, result[1, 3], 2);
        Assert.Equal(0f, result[2, 3], 2);
    }

    [Fact]
    public void TestMatrixRowEchelonForm()
    {
        // Diagonally dominant, so Gaussian elimination proceeds without needing row swaps.
        var m = new Matrix(
            4, 1, 2, 1,
            1, 5, 1, 2,
            2, 1, 6, 1,
            1, 2, 1, 7);

        Matrix.RowEchelonForm(ref m, out var result);

        for (int r = 0; r < 4; r++)
        {
            Assert.Equal(1f, result[r, r], 2);
            for (int i = r + 1; i < 4; i++)
                Assert.Equal(0f, result[i, r], 2);
        }
    }

    [Fact]
    public void TestMatrixReducedRowEchelonForm()
    {
        var m = new Matrix(
            4, 1, 2, 1,
            1, 5, 1, 2,
            2, 1, 6, 1,
            1, 2, 1, 7);
        var augment = new Vector4(10, 12, 14, 16);

        Matrix.ReducedRowEchelonForm(in m, in augment, out Matrix result, out Vector4 solution);

        // For an invertible 4x4 matrix, the reduced row echelon form is the identity matrix.
        for (int r = 0; r < 4; r++)
            for (int c = 0; c < 4; c++)
                Assert.Equal(r == c ? 1f : 0f, result[r, c], 2);

        // The augmented column now holds the solution x to m * x = augment.
        Assert.Equal(augment.X, Vector4.Dot(m.Row1, solution), 2);
        Assert.Equal(augment.Y, Vector4.Dot(m.Row2, solution), 2);
        Assert.Equal(augment.Z, Vector4.Dot(m.Row3, solution), 2);
        Assert.Equal(augment.W, Vector4.Dot(m.Row4, solution), 2);
    }

    [Fact]
    public void TestMatrixExponentZero()
    {
        var m = Matrix.RotationYawPitchRoll(0.3f, 0.4f, 0.5f) * Matrix.Scaling(2, 3, 4);

        var result = Matrix.Exponent(m, 0);

        Assert.Equal(Matrix.Identity, result);
    }

    [Fact]
    public void TestMatrixExponentOne()
    {
        var m = Matrix.RotationYawPitchRoll(0.3f, 0.4f, 0.5f) * Matrix.Scaling(2, 3, 4);

        var result = Matrix.Exponent(m, 1);

        Assert.Equal(m, result);
    }

    [Fact]
    public void TestMatrixExponentThree()
    {
        var m = Matrix.RotationX(MathUtil.PiOverFour) * Matrix.Scaling(2, 1, 1);
        var expected = m * m * m;

        var result = Matrix.Exponent(m, 3);

        for (int i = 0; i < 16; i++)
            Assert.Equal(expected[i], result[i], 2);
    }

    [Fact]
    public void TestMatrixExponentNegativeThrows()
    {
        var m = Matrix.Identity;
        Assert.Throws<ArgumentOutOfRangeException>(() => Matrix.Exponent(m, -1));
    }

    [Fact]
    public void TestMatrixMultiplyStaticOverloadsAgreeWithOperator()
    {
        var a = Matrix.RotationY(MathUtil.PiOverFour);
        var b = Matrix.Translation(1, 2, 3);

        var viaOperator = a * b;
        var viaStaticValue = Matrix.Multiply(a, b);
        Matrix.Multiply(ref a, ref b, out var viaStaticRef);
        Matrix.MultiplyIn(in a, in b, out var viaMultiplyIn);

        Assert.Equal(viaOperator, viaStaticValue);
        Assert.Equal(viaOperator, viaStaticRef);
        Assert.Equal(viaOperator, viaMultiplyIn);
    }

    [Fact]
    public void TestMatrixDivideMatrixByMatrix()
    {
        var a = new Matrix(
            2, 4, 6, 8,
            10, 12, 14, 16,
            18, 20, 22, 24,
            26, 28, 30, 32);
        var b = new Matrix(2f);

        var result = Matrix.Divide(a, b);

        for (int i = 0; i < 16; i++)
            Assert.Equal(a[i] / 2f, result[i]);
    }

    [Fact]
    public void TestMatrixPerspectiveOffCenterLHDepthRange()
    {
        float znear = 2f, zfar = 50f;
        var m = Matrix.PerspectiveOffCenterLH(-3, 5, -2, 6, znear, zfar);

        var nearPoint = Vector3.TransformCoordinate(new Vector3(0, 0, znear), m);
        var farPoint = Vector3.TransformCoordinate(new Vector3(0, 0, zfar), m);

        Assert.Equal(0f, nearPoint.Z, 3);
        Assert.Equal(1f, farPoint.Z, 3);
    }

    [Fact]
    public void TestMatrixPerspectiveOffCenterRHDepthRange()
    {
        float znear = 2f, zfar = 50f;
        var m = Matrix.PerspectiveOffCenterRH(-3, 5, -2, 6, znear, zfar);

        var nearPoint = Vector3.TransformCoordinate(new Vector3(0, 0, -znear), m);
        var farPoint = Vector3.TransformCoordinate(new Vector3(0, 0, -zfar), m);

        Assert.Equal(0f, nearPoint.Z, 3);
        Assert.Equal(1f, farPoint.Z, 3);
    }

    [Fact]
    public void TestMatrixPerspectiveFovLHMatchesOffCenterForSymmetricFrustum()
    {
        float fov = MathUtil.PiOverFour;
        float aspect = 16f / 9f;
        float znear = 0.5f, zfar = 100f;

        var viaFov = Matrix.PerspectiveFovLH(fov, aspect, znear, zfar);

        float yScale = 1f / MathF.Tan(fov * 0.5f);
        float xScale = yScale / aspect;
        float halfHeight = znear / yScale;
        float halfWidth = znear / xScale;
        var viaOffCenter = Matrix.PerspectiveOffCenterLH(-halfWidth, halfWidth, -halfHeight, halfHeight, znear, zfar);

        Assert.Equal(viaOffCenter, viaFov);
    }

    [Fact]
    public void TestMatrixOrthoOffCenterLHDepthRange()
    {
        float znear = 1f, zfar = 20f;
        var m = Matrix.OrthoOffCenterLH(-4, 4, -3, 3, znear, zfar);

        var nearPoint = Vector3.TransformCoordinate(new Vector3(0, 0, znear), m);
        var farPoint = Vector3.TransformCoordinate(new Vector3(0, 0, zfar), m);

        Assert.Equal(0f, nearPoint.Z, 3);
        Assert.Equal(1f, farPoint.Z, 3);
    }

    [Fact]
    public void TestMatrixOrthoLHMatchesOffCenterForSymmetricVolume()
    {
        float width = 10f, height = 6f, znear = 0.5f, zfar = 100f;

        var viaWidthHeight = Matrix.OrthoLH(width, height, znear, zfar);
        var viaOffCenter = Matrix.OrthoOffCenterLH(-width / 2, width / 2, -height / 2, height / 2, znear, zfar);

        Assert.Equal(viaOffCenter, viaWidthHeight);
    }

    [Fact]
    public void TestMatrixLookAtLHTransformsTargetToPositiveZ()
    {
        var eye = new Vector3(0, 0, -10);
        var target = Vector3.Zero;
        var up = Vector3.UnitY;

        var view = Matrix.LookAtLH(eye, target, up);
        var targetInViewSpace = Vector3.TransformCoordinate(target, view);

        // In a LH view space, the look direction points toward +Z.
        Assert.Equal(0f, targetInViewSpace.X, 3);
        Assert.Equal(0f, targetInViewSpace.Y, 3);
        Assert.True(targetInViewSpace.Z > 0f);
    }

    [Fact]
    public void TestMatrixLookAtRHTransformsTargetToNegativeZ()
    {
        var eye = new Vector3(0, 0, 10);
        var target = Vector3.Zero;
        var up = Vector3.UnitY;

        var view = Matrix.LookAtRH(eye, target, up);
        var targetInViewSpace = Vector3.TransformCoordinate(target, view);

        // In a RH view space, the look direction points toward -Z.
        Assert.Equal(0f, targetInViewSpace.X, 3);
        Assert.Equal(0f, targetInViewSpace.Y, 3);
        Assert.True(targetInViewSpace.Z < 0f);
    }

    [Fact]
    public void TestMatrixAffineTransformationFull()
    {
        float scaling = 2f;
        var rotation = Quaternion.RotationY(MathUtil.PiOverTwo);
        var translation = new Vector3(1, 2, 3);

        var affine = Matrix.AffineTransformation(scaling, rotation, translation);
        var expected = Matrix.Scaling(scaling) * Matrix.RotationQuaternion(rotation) * Matrix.Translation(translation);

        Assert.Equal(expected, affine);
    }

    [Fact]
    public void TestMatrixToStringContainsAllComponents()
    {
        var m = new Matrix(
            1, 2, 3, 4,
            5, 6, 7, 8,
            9, 10, 11, 12,
            13, 14, 15, 16);

        var text = m.ToString();

        Assert.Contains("M11:1", text);
        Assert.Contains("M44:16", text);
    }

    [Fact]
    public void TestMatrixTryFormat()
    {
        var m = Matrix.Identity;
        Span<char> buffer = new char[200];

        bool success = ((ISpanFormattable)m).TryFormat(buffer, out int charsWritten, default, null);

        Assert.True(success);
        Assert.True(charsWritten > 0);
    }

    #endregion
}
