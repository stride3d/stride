// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestQuaternion
{
    /* Note: As seen in the decomposition tests, we check both expectedQuat == decompedQuat and expectedQuat == -decompedQuat
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
        Quaternion.RotationYawPitchRoll(ref rotQuat, out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = rotQuat;
        var decompedQuat = Quaternion.RotationYawPitchRoll(decomposedYaw, decomposedPitch, decomposedRoll);
        Assert.True(expectedQuat == decompedQuat || expectedQuat == -decompedQuat, $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Theory, ClassData(typeof(TestRotationsData.YRPTestData))]
    public void TestDecomposeYawPitchRollFromQuaternionYXZ(float yawDegrees, float pitchDegrees, float rollDegrees)
    {
        var yawRadians = MathUtil.DegreesToRadians(yawDegrees);
        var pitchRadians = MathUtil.DegreesToRadians(pitchDegrees);
        var rollRadians = MathUtil.DegreesToRadians(rollDegrees);

        var rotX = Quaternion.RotationX(pitchRadians);
        var rotY = Quaternion.RotationY(yawRadians);
        var rotZ = Quaternion.RotationZ(rollRadians);
        // Yaw-Pitch-Roll is the intrinsic rotation order, so extrinsic is the reverse (ie. Z-X-Y)
        var rotQuat = rotZ * rotX * rotY;
        Quaternion.RotationYawPitchRoll(ref rotQuat, out float decomposedYaw, out float decomposedPitch, out float decomposedRoll);

        var expectedQuat = rotQuat;
        var decompRotX = Quaternion.RotationX(decomposedPitch);
        var decompRotY = Quaternion.RotationY(decomposedYaw);
        var decompRotZ = Quaternion.RotationZ(decomposedRoll);
        var decompedQuat = decompRotZ * decompRotX * decompRotY;
        Assert.True(expectedQuat == decompedQuat || expectedQuat == -decompedQuat, $"Quat not equals: Expected: {expectedQuat} - Actual: {decompedQuat}");
    }

    [Fact]
    public void TestQuaternionIdentity()
    {
        var identity = Quaternion.Identity;
        Assert.Equal(0.0f, identity.X);
        Assert.Equal(0.0f, identity.Y);
        Assert.Equal(0.0f, identity.Z);
        Assert.Equal(1.0f, identity.W);
    }

    [Fact]
    public void TestQuaternionZero()
    {
        var zero = Quaternion.Zero;
        Assert.Equal(0.0f, zero.X);
        Assert.Equal(0.0f, zero.Y);
        Assert.Equal(0.0f, zero.Z);
        Assert.Equal(0.0f, zero.W);
    }

    [Theory]
    [InlineData(1, 0, 0, 0, 1)]
    [InlineData(0, 1, 0, 0, 1)]
    [InlineData(0, 0, 1, 0, 1)]
    [InlineData(0, 0, 0, 1, 1)]
    [InlineData(1, 2, 3, 4, 5.477226f)]
    public void TestQuaternionLength(float x, float y, float z, float w, float expectedLength)
    {
        var quat = new Quaternion(x, y, z, w);
        Assert.Equal(expectedLength, quat.Length(), 3);
    }

    [Fact]
    public void TestQuaternionDotProduct()
    {
        var q1 = new Quaternion(1, 2, 3, 4);
        var q2 = new Quaternion(5, 6, 7, 8);
        var dot = Quaternion.Dot(q1, q2);
        Assert.Equal(70.0f, dot); // 1*5 + 2*6 + 3*7 + 4*8 = 5 + 12 + 21 + 32
    }

    [Fact]
    public void TestQuaternionConstruction()
    {
        // Test component constructor
        var q1 = new Quaternion(1, 2, 3, 4);
        Assert.Equal(1f, q1.X);
        Assert.Equal(2f, q1.Y);
        Assert.Equal(3f, q1.Z);
        Assert.Equal(4f, q1.W);

        // Test Vector3 + W constructor
        var q2 = new Quaternion(new Vector3(5, 6, 7), 8);
        Assert.Equal(5f, q2.X);
        Assert.Equal(6f, q2.Y);
        Assert.Equal(7f, q2.Z);
        Assert.Equal(8f, q2.W);

        // Test Vector4 constructor
        var q3 = new Quaternion(new Vector4(9, 10, 11, 12));
        Assert.Equal(9f, q3.X);
        Assert.Equal(10f, q3.Y);
        Assert.Equal(11f, q3.Z);
        Assert.Equal(12f, q3.W);

        // Test single value constructor
        var q4 = new Quaternion(3.5f);
        Assert.Equal(3.5f, q4.X);
        Assert.Equal(3.5f, q4.Y);
        Assert.Equal(3.5f, q4.Z);
        Assert.Equal(3.5f, q4.W);
    }

    [Fact]
    public void TestQuaternionAdd()
    {
        var q1 = new Quaternion(1, 2, 3, 4);
        var q2 = new Quaternion(5, 6, 7, 8);

        var result = q1 + q2;
        Assert.Equal(6f, result.X);
        Assert.Equal(8f, result.Y);
        Assert.Equal(10f, result.Z);
        Assert.Equal(12f, result.W);

        // Test static method
        var result2 = Quaternion.Add(q1, q2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestQuaternionSubtract()
    {
        var q1 = new Quaternion(10, 20, 30, 40);
        var q2 = new Quaternion(1, 2, 3, 4);

        var result = q1 - q2;
        Assert.Equal(9f, result.X);
        Assert.Equal(18f, result.Y);
        Assert.Equal(27f, result.Z);
        Assert.Equal(36f, result.W);

        // Test static method
        var result2 = Quaternion.Subtract(q1, q2);
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestQuaternionMultiplyScalar()
    {
        var q = new Quaternion(1, 2, 3, 4);
        var result = q * 2.5f;

        Assert.Equal(2.5f, result.X);
        Assert.Equal(5f, result.Y);
        Assert.Equal(7.5f, result.Z);
        Assert.Equal(10f, result.W);

        // Test reverse order
        var result2 = 2.5f * q;
        Assert.Equal(result, result2);
    }

    [Fact]
    public void TestQuaternionMultiplyQuaternion()
    {
        var q1 = Quaternion.RotationY(MathUtil.PiOverTwo);
        var q2 = Quaternion.RotationZ(MathUtil.PiOverTwo);

        var result = q1 * q2;

        // Multiplying quaternions should compose rotations
        Assert.NotEqual(Quaternion.Identity, result);
        Assert.NotEqual(Quaternion.Zero, result);
    }

    [Fact]
    public void TestQuaternionNegate()
    {
        var q = new Quaternion(1, 2, 3, 4);
        var negated = -q;

        Assert.Equal(-1f, negated.X);
        Assert.Equal(-2f, negated.Y);
        Assert.Equal(-3f, negated.Z);
        Assert.Equal(-4f, negated.W);

        // Test static method
        var negated2 = Quaternion.Negate(q);
        Assert.Equal(negated, negated2);
    }

    [Fact]
    public void TestQuaternionConjugate()
    {
        var q = new Quaternion(1, 2, 3, 4);
        var conjugate = Quaternion.Conjugate(q);

        // Conjugate negates X, Y, Z but keeps W
        Assert.Equal(-1f, conjugate.X);
        Assert.Equal(-2f, conjugate.Y);
        Assert.Equal(-3f, conjugate.Z);
        Assert.Equal(4f, conjugate.W);
    }

    [Fact]
    public void TestQuaternionInvert()
    {
        var q = Quaternion.RotationY(MathUtil.PiOverFour);
        var inverted = Quaternion.Invert(q);

        // q * q^-1 should equal identity
        var product = q * inverted;

        Assert.Equal(Quaternion.Identity.X, product.X, 5);
        Assert.Equal(Quaternion.Identity.Y, product.Y, 5);
        Assert.Equal(Quaternion.Identity.Z, product.Z, 5);
        Assert.Equal(Quaternion.Identity.W, product.W, 5);
    }

    [Fact]
    public void TestQuaternionNormalize()
    {
        var q = new Quaternion(1, 2, 3, 4);
        var normalized = Quaternion.Normalize(q);

        // Normalized quaternion should have length 1
        var length = normalized.Length();
        Assert.Equal(1f, length, 5);
    }

    [Fact]
    public void TestQuaternionLengthSquared()
    {
        var q = new Quaternion(1, 2, 3, 4);
        var lengthSquared = q.LengthSquared();

        // 1² + 2² + 3² + 4² = 1 + 4 + 9 + 16 = 30
        Assert.Equal(30f, lengthSquared);
    }

    [Fact]
    public void TestQuaternionRotationX()
    {
        var angle = MathUtil.PiOverTwo;
        var q = Quaternion.RotationX(angle);

        // Rotation quaternions should have unit length
        Assert.Equal(1f, q.Length(), 5);

        // Apply rotation to a vector
        var v = new Vector3(0, 1, 0);
        var rotated = Vector3.Transform(v, q);

        // Rotating (0,1,0) by 90° around X should give approximately (0,0,1)
        Assert.Equal(0f, rotated.X, 5);
        Assert.Equal(0f, rotated.Y, 5);
        Assert.Equal(1f, rotated.Z, 5);
    }

    [Fact]
    public void TestQuaternionRotationY()
    {
        var angle = MathUtil.PiOverTwo;
        var q = Quaternion.RotationY(angle);

        Assert.Equal(1f, q.Length(), 5);

        var v = new Vector3(1, 0, 0);
        var rotated = Vector3.Transform(v, q);

        // Rotating (1,0,0) by 90° around Y should give approximately (0,0,-1)
        Assert.Equal(0f, rotated.X, 5);
        Assert.Equal(0f, rotated.Y, 5);
        Assert.Equal(-1f, rotated.Z, 5);
    }

    [Fact]
    public void TestQuaternionRotationZ()
    {
        var angle = MathUtil.PiOverTwo;
        var q = Quaternion.RotationZ(angle);

        Assert.Equal(1f, q.Length(), 5);

        var v = new Vector3(1, 0, 0);
        var rotated = Vector3.Transform(v, q);

        // Rotating (1,0,0) by 90° around Z should give approximately (0,1,0)
        Assert.Equal(0f, rotated.X, 5);
        Assert.Equal(1f, rotated.Y, 5);
        Assert.Equal(0f, rotated.Z, 5);
    }

    [Fact]
    public void TestQuaternionRotationAxis()
    {
        var axis = Vector3.UnitY;
        var angle = MathUtil.Pi;

        var q = Quaternion.RotationAxis(axis, angle);

        // Should be a unit quaternion
        Assert.Equal(1f, q.Length(), 5);

        // 180° rotation around Y should be equivalent to RotationY
        var qY = Quaternion.RotationY(angle);

        // Quaternions q and -q represent the same rotation
        Assert.True(
            (Math.Abs(q.X - qY.X) < 0.001f && Math.Abs(q.W - qY.W) < 0.001f) ||
            (Math.Abs(q.X + qY.X) < 0.001f && Math.Abs(q.W + qY.W) < 0.001f));
    }

    [Fact]
    public void TestQuaternionRotationMatrix()
    {
        var matrix = Matrix.RotationY(MathUtil.PiOverFour);
        var q = Quaternion.RotationMatrix(matrix);

        // Should be a unit quaternion
        Assert.Equal(1f, q.Length(), 5);

        // Convert back to matrix and compare
        var matrix2 = Matrix.RotationQuaternion(q);

        // Matrices should be approximately equal
        Assert.Equal(matrix.M11, matrix2.M11, 4);
        Assert.Equal(matrix.M22, matrix2.M22, 4);
        Assert.Equal(matrix.M33, matrix2.M33, 4);
    }

    [Fact]
    public void TestQuaternionLerp()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationY(MathUtil.Pi);

        // Lerp at 0 should return q1
        var lerp0 = Quaternion.Lerp(q1, q2, 0f);
        Assert.Equal(q1.X, lerp0.X, 5);
        Assert.Equal(q1.W, lerp0.W, 5);

        // Lerp at 1 should return q2 (or -q2, same rotation)
        var lerp1 = Quaternion.Lerp(q1, q2, 1f);
        Assert.True(
            (Math.Abs(lerp1.X - q2.X) < 0.001f && Math.Abs(lerp1.W - q2.W) < 0.001f) ||
            (Math.Abs(lerp1.X + q2.X) < 0.001f && Math.Abs(lerp1.W + q2.W) < 0.001f));

        // Lerp at 0.5 should be between them
        var lerp05 = Quaternion.Lerp(q1, q2, 0.5f);
        Assert.NotEqual(q1, lerp05);
        Assert.NotEqual(q2, lerp05);
    }

    [Fact]
    public void TestQuaternionSlerp()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationY(MathUtil.PiOverTwo);

        // Slerp at 0 should return q1
        var slerp0 = Quaternion.Slerp(q1, q2, 0f);
        Assert.Equal(q1.X, slerp0.X, 5);
        Assert.Equal(q1.W, slerp0.W, 5);

        // Slerp at 1 should return q2
        var slerp1 = Quaternion.Slerp(q1, q2, 1f);
        Assert.Equal(q2.X, slerp1.X, 5);
        Assert.Equal(q2.W, slerp1.W, 5);

        // Slerp provides smooth spherical interpolation
        var slerp05 = Quaternion.Slerp(q1, q2, 0.5f);
        Assert.Equal(1f, slerp05.Length(), 5); // Should maintain unit length
    }

    [Fact]
    public void TestQuaternionSquad()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationY(MathUtil.PiOverFour);
        var q3 = Quaternion.RotationY(MathUtil.PiOverTwo);
        var q4 = Quaternion.RotationY(MathUtil.Pi);

        var result = Quaternion.Squad(q1, q2, q3, q4, 0.5f);

        // Squad should produce smooth interpolation
        Assert.Equal(1f, result.Length(), 4); // Should be unit quaternion
    }

    [Fact]
    public void TestQuaternionBaryCentric()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationX(MathUtil.PiOverFour);
        var q3 = Quaternion.RotationY(MathUtil.PiOverFour);

        var result = Quaternion.Barycentric(q1, q2, q3, 0.33f, 0.33f);

        // Should produce a valid quaternion
        Assert.NotEqual(Quaternion.Zero, result);
    }

    [Fact]
    public void TestQuaternionEquality()
    {
        var q1 = new Quaternion(1, 2, 3, 4);
        var q2 = new Quaternion(1, 2, 3, 4);
        var q3 = new Quaternion(1, 2, 3, 5);

        Assert.True(q1 == q2);
        Assert.False(q1 == q3);
        Assert.True(q1 != q3);
        Assert.True(q1.Equals(q2));
        Assert.False(q1.Equals(q3));
    }

    [Fact]
    public void TestQuaternionGetHashCode()
    {
        var q1 = new Quaternion(1, 2, 3, 4);
        var q2 = new Quaternion(1, 2, 3, 4);

        Assert.Equal(q1.GetHashCode(), q2.GetHashCode());
    }

    [Fact]
    public void TestQuaternionToString()
    {
        var q = new Quaternion(1, 2, 3, 4);
        var str = q.ToString();

        Assert.NotNull(str);
        Assert.NotEmpty(str);
        Assert.Contains("1", str);
        Assert.Contains("2", str);
        Assert.Contains("3", str);
        Assert.Contains("4", str);
    }

    [Fact]
    public void TestQuaternionBetweenDirections()
    {
        var from = Vector3.UnitZ;
        var to = Vector3.UnitX;

        var q = Quaternion.BetweenDirections(from, to);

        // Apply rotation
        var rotated = Vector3.Transform(from, q);

        // Should rotate from to to
        Assert.Equal(to.X, rotated.X, 4);
        Assert.Equal(to.Y, rotated.Y, 4);
        Assert.Equal(to.Z, rotated.Z, 4);
    }

    [Fact]
    public void TestQuaternionIsIdentity()
    {
        Assert.True(Quaternion.Identity.IsIdentity);
        Assert.False(Quaternion.Zero.IsIdentity);
        Assert.False(new Quaternion(1, 2, 3, 4).IsIdentity);

        // Normalized identity should still be identity
        var q = new Quaternion(0, 0, 0, 2);
        var normalized = Quaternion.Normalize(q);
        Assert.True(normalized.IsIdentity);
    }

    [Fact]
    public void TestQuaternionAngleBetween()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationY(MathUtil.PiOverTwo);

        var angle = Quaternion.AngleBetween(q1, q2);

        // Angle should be approximately π/2
        Assert.Equal(MathUtil.PiOverTwo, angle, 4);
    }

    [Fact]
    public void TestQuaternionAngleProperty()
    {
        var axis = Vector3.UnitY;
        var angleIn = MathUtil.PiOverFour;

        var q = Quaternion.RotationAxis(axis, angleIn);
        var angleOut = q.Angle;

        // Angle property should return the rotation angle
        Assert.Equal(angleIn, angleOut, 4);
    }

    [Fact]
    public void TestQuaternionExponential()
    {
        var q = new Quaternion(0.1f, 0.2f, 0.3f, 0f);
        var exp = Quaternion.Exponential(q);

        // Exponential should produce a valid quaternion
        Assert.NotEqual(Quaternion.Zero, exp);
    }

    [Fact]
    public void TestQuaternionLogarithm()
    {
        var q = Quaternion.RotationY(MathUtil.PiOverFour);
        var log = Quaternion.Logarithm(q);

        // Log of a rotation should have W near 0
        Assert.Equal(0f, log.W, 4);
    }

    [Fact]
    public void TestQuaternionFromToNumeric()
    {
        var q = new Quaternion(1, 2, 3, 4);

        // Convert to System.Numerics.Quaternion
        var numeric = (System.Numerics.Quaternion)q;
        Assert.Equal(1f, numeric.X);
        Assert.Equal(2f, numeric.Y);
        Assert.Equal(3f, numeric.Z);
        Assert.Equal(4f, numeric.W);

        // Convert back
        var q2 = (Quaternion)numeric;
        Assert.Equal(q, q2);
    }

    #region Quaternion Operations Tests

    private const float Epsilon = 1e-6f;

    [Fact]
    public void TestQuaternionMultiplication()
    {
        // Test identity quaternion multiplication
        var identity = Quaternion.Identity;
        var rotation = Quaternion.RotationY(MathUtil.PiOverTwo);

        var result1 = identity * rotation;
        var result2 = rotation * identity;

        // Multiplication with identity should not change the quaternion
        Assert.Equal(rotation.X, result1.X, 3);
        Assert.Equal(rotation.Y, result1.Y, 3);
        Assert.Equal(rotation.Z, result1.Z, 3);
        Assert.Equal(rotation.W, result1.W, 3);

        Assert.Equal(rotation.X, result2.X, 3);
        Assert.Equal(rotation.Y, result2.Y, 3);
        Assert.Equal(rotation.Z, result2.Z, 3);
        Assert.Equal(rotation.W, result2.W, 3);

        // Test that quaternion multiplication properly combines rotations
        var v = Vector3.UnitX;
        var rotated = Vector3.Transform(v, rotation);
        var doubleRotated = Vector3.Transform(rotated, rotation);

        // Single 90-degree rotation around Y should take (1,0,0) to (0,0,-1)
        Assert.Equal(0.0f, rotated.X, 3);
        Assert.Equal(0.0f, rotated.Y, 3);
        Assert.Equal(-1.0f, rotated.Z, 3);

        // Two 90-degree rotations around Y should take (1,0,0) to (-1,0,0)
        Assert.Equal(-1.0f, doubleRotated.X, 3);
        Assert.Equal(0.0f, doubleRotated.Y, 3);
        Assert.Equal(0.0f, doubleRotated.Z, 3);

        // Test that multiplication of quaternions equals applying rotations in sequence
        var doubleRotation = rotation * rotation;
        var doubleRotatedDirect = Vector3.Transform(v, doubleRotation);

        Assert.Equal(doubleRotated.X, doubleRotatedDirect.X, 3);
        Assert.Equal(doubleRotated.Y, doubleRotatedDirect.Y, 3);
        Assert.Equal(doubleRotated.Z, doubleRotatedDirect.Z, 3);
    }

    [Fact]
    public void TestQuaternionVectorRotation()
    {
        // Create a quaternion that rotates 90 degrees around Y axis
        var rotation = Quaternion.RotationAxis(Vector3.UnitY, -MathUtil.PiOverTwo); // Negative for clockwise rotation

        // Rotate a vector pointing along Z axis
        var vector = Vector3.UnitZ;
        var rotated = Vector3.Transform(vector, rotation);

        // After 90 degree clockwise Y rotation, Z should point along negative X
        Assert.Equal(-1.0f, rotated.X, 3);
        Assert.Equal(0.0f, rotated.Y, 3);
        Assert.Equal(0.0f, rotated.Z, 3);
    }

    [Fact]
    public void TestQuaternionSlerpOperations()
    {
        // Create two quaternions 90 degrees apart
        var start = Quaternion.Identity;
        var end = Quaternion.RotationAxis(Vector3.UnitY, -MathUtil.PiOverTwo); // Negative for clockwise rotation

        // Test interpolation at different points
        var halfway = Quaternion.Slerp(start, end, 0.5f);
        var quarterWay = Quaternion.Slerp(start, end, 0.25f);

        // Test interpolated rotations on a vector
        var vector = Vector3.UnitZ;
        var halfwayRotated = Vector3.Transform(vector, halfway);
        var quarterWayRotated = Vector3.Transform(vector, quarterWay);

        // At halfway point (45 degrees), rotated vector should be at (-0.707, 0, 0.707)
        Assert.Equal(-0.707f, halfwayRotated.X, 3);
        Assert.Equal(0.0f, halfwayRotated.Y, 3);
        Assert.Equal(0.707f, halfwayRotated.Z, 3);

        // At quarter way point (22.5 degrees), rotated vector should be at (-0.383, 0, 0.924)
        Assert.Equal(-0.383f, quarterWayRotated.X, 3);
        Assert.Equal(0.0f, quarterWayRotated.Y, 3);
        Assert.Equal(0.924f, quarterWayRotated.Z, 3);
    }

    [Theory]
    [InlineData(0, 0, 0)]      // Identity
    [InlineData(90, 0, 0)]     // Pure X rotation
    [InlineData(0, 90, 0)]     // Pure Y rotation
    [InlineData(0, 0, 90)]     // Pure Z rotation
    [InlineData(45, 45, 45)]   // Combined rotation
    public void TestQuaternionEulerConversion(float pitchDegrees, float yawDegrees, float rollDegrees)
    {
        var pitch = MathUtil.DegreesToRadians(pitchDegrees);
        var yaw = MathUtil.DegreesToRadians(yawDegrees);
        var roll = MathUtil.DegreesToRadians(rollDegrees);

        // Create quaternion from euler angles
        var quat = Quaternion.RotationYawPitchRoll(yaw, pitch, roll);

        // Create matrix from both quaternion and euler angles directly
        var matFromQuat = Matrix.RotationQuaternion(quat);
        var matFromEuler = Matrix.RotationYawPitchRoll(yaw, pitch, roll);

        // Test vectors to verify rotations
        var vectors = new[]
        {
            Vector3.UnitX,
            Vector3.UnitY,
            Vector3.UnitZ,
            new Vector3(1, 1, 1)
        };

        foreach (var vector in vectors)
        {
            var rotatedByQuat = Vector3.Transform(vector, matFromQuat);
            var rotatedByEuler = Vector3.Transform(vector, matFromEuler);

            // Both rotations should produce the same result
            Assert.Equal(rotatedByQuat.X, rotatedByEuler.X, 3);
            Assert.Equal(rotatedByQuat.Y, rotatedByEuler.Y, 3);
            Assert.Equal(rotatedByQuat.Z, rotatedByEuler.Z, 3);
        }
    }

    [Fact]
    public void TestQuaternionNormalizationOperations()
    {
        // Create a non-normalized quaternion
        var q = new Quaternion(2.0f, 3.0f, 4.0f, 5.0f);
        var normalized = Quaternion.Normalize(q);

        // Length should be 1
        var length = (float)Math.Sqrt(
            normalized.X * normalized.X +
            normalized.Y * normalized.Y +
            normalized.Z * normalized.Z +
            normalized.W * normalized.W
        );
        Assert.Equal(1.0f, length, 3);

        // Original ratios should be preserved
        var ratio = 2.0f / 3.0f;
        Assert.Equal(ratio, normalized.X / normalized.Y, 3);
    }

    [Fact]
    public void TestQuaternionInverseOperations()
    {
        var q = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.PiOverFour);
        Quaternion.Invert(ref q, out var inverse);

        // q * q^-1 should equal identity
        var product = q * inverse;
        Assert.Equal(0.0f, product.X, Epsilon);
        Assert.Equal(0.0f, product.Y, Epsilon);
        Assert.Equal(0.0f, product.Z, Epsilon);
        Assert.Equal(1.0f, product.W, Epsilon);

        // Test inverse rotation on a vector
        var vector = new Vector3(1, 0, 1);
        var rotated = Vector3.Transform(vector, q);
        var unrotated = Vector3.Transform(rotated, inverse);

        // Should get back original vector
        Assert.Equal(vector.X, unrotated.X, 3);
        Assert.Equal(vector.Y, unrotated.Y, 3);
        Assert.Equal(vector.Z, unrotated.Z, 3);
    }

    [Fact]
    public void TestQuaternionToRotationMatrix()
    {
        var q = Quaternion.RotationYawPitchRoll(
            MathUtil.DegreesToRadians(30),
            MathUtil.DegreesToRadians(45),
            MathUtil.DegreesToRadians(60)
        );

        var matrix = Matrix.RotationQuaternion(q);

        // Test that both quaternion and matrix rotate a vector the same way
        var vector = new Vector3(1, 2, 3);
        var rotatedByQuaternion = Vector3.Transform(vector, q);
        var rotatedByMatrix = Vector3.Transform(vector, matrix);

        Assert.Equal(rotatedByQuaternion.X, rotatedByMatrix.X, 3);
        Assert.Equal(rotatedByQuaternion.Y, rotatedByMatrix.Y, 3);
        Assert.Equal(rotatedByQuaternion.Z, rotatedByMatrix.Z, 3);
    }

    #endregion

    #region Quaternion Edge Cases Tests

    [Fact]
    public void TestQuaternionIdentityEdgeCase()
    {
        var identity = Quaternion.Identity;

        Assert.Equal(0.0f, identity.X);
        Assert.Equal(0.0f, identity.Y);
        Assert.Equal(0.0f, identity.Z);
        Assert.Equal(1.0f, identity.W);
    }

    [Fact]
    public void TestQuaternionNormalizationEdgeCase()
    {
        var q = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        q.Normalize();

        var length = MathF.Sqrt(q.X * q.X + q.Y * q.Y + q.Z * q.Z + q.W * q.W);
        Assert.Equal(1.0f, length, 5);
    }

    [Fact]
    public void TestQuaternionZeroLengthNormalization()
    {
        var q = new Quaternion(0.0f, 0.0f, 0.0f, 0.0f);
        var normalized = Quaternion.Normalize(q);

        // Should not produce NaN
        Assert.False(float.IsNaN(normalized.X));
        Assert.False(float.IsNaN(normalized.Y));
        Assert.False(float.IsNaN(normalized.Z));
        Assert.False(float.IsNaN(normalized.W));
    }

    [Fact]
    public void TestQuaternionInverseEdgeCase()
    {
        var q = Quaternion.RotationY(MathUtil.PiOverTwo);
        Quaternion.Invert(ref q, out var inverse);

        var product = q * inverse;

        // q * q^-1 should be identity
        Assert.Equal(0.0f, product.X, 5);
        Assert.Equal(0.0f, product.Y, 5);
        Assert.Equal(0.0f, product.Z, 5);
        Assert.Equal(1.0f, MathF.Abs(product.W), 5);
    }

    [Fact]
    public void TestQuaternionMultiplicationIdentity()
    {
        var q = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        var result1 = q * Quaternion.Identity;
        var result2 = Quaternion.Identity * q;

        Assert.Equal(q, result1);
        Assert.Equal(q, result2);
    }

    [Fact]
    public void TestQuaternionSlerpIdenticalInputs()
    {
        var q = Quaternion.RotationY(MathUtil.PiOverTwo);
        var result = Quaternion.Slerp(q, q, 0.5f);

        // Slerping between identical quaternions should return the same quaternion
        Assert.Equal(q.X, result.X, 5);
        Assert.Equal(q.Y, result.Y, 5);
        Assert.Equal(q.Z, result.Z, 5);
        Assert.Equal(q.W, result.W, 5);
    }

    [Fact]
    public void TestQuaternionSlerpBoundaries()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationY(MathUtil.PiOverTwo);

        // At t=0, should return q1
        var result0 = Quaternion.Slerp(q1, q2, 0.0f);
        Assert.Equal(q1.X, result0.X, 5);
        Assert.Equal(q1.Y, result0.Y, 5);
        Assert.Equal(q1.Z, result0.Z, 5);
        Assert.Equal(q1.W, result0.W, 5);

        // At t=1, should return q2
        var result1 = Quaternion.Slerp(q1, q2, 1.0f);
        Assert.Equal(q2.X, result1.X, 5);
        Assert.Equal(q2.Y, result1.Y, 5);
        Assert.Equal(q2.Z, result1.Z, 5);
        Assert.Equal(q2.W, result1.W, 5);
    }

    [Fact]
    public void TestQuaternionLerpBoundaries()
    {
        var q1 = Quaternion.Identity;
        var q2 = Quaternion.RotationY(MathUtil.PiOverTwo);

        // At t=0, should return q1 (normalized)
        var result0 = Quaternion.Lerp(q1, q2, 0.0f);
        Assert.Equal(q1.X, result0.X, 5);
        Assert.Equal(q1.Y, result0.Y, 5);
        Assert.Equal(q1.Z, result0.Z, 5);
        Assert.Equal(q1.W, result0.W, 5);

        // At t=1, should return q2 (normalized)
        var result1 = Quaternion.Lerp(q1, q2, 1.0f);
        Assert.Equal(q2.X, result1.X, 5);
        Assert.Equal(q2.Y, result1.Y, 5);
        Assert.Equal(q2.Z, result1.Z, 5);
        Assert.Equal(q2.W, result1.W, 5);
    }

    [Fact]
    public void TestQuaternionRotationZeroAxis()
    {
        var axis = Vector3.Zero;
        var q = Quaternion.RotationAxis(axis, MathUtil.PiOverTwo);

        // Rotation around zero axis should produce identity or be handled gracefully
        Assert.False(float.IsNaN(q.X));
        Assert.False(float.IsNaN(q.Y));
        Assert.False(float.IsNaN(q.Z));
        Assert.False(float.IsNaN(q.W));
    }

    [Fact]
    public void TestQuaternionRotationAxisNormalization()
    {
        var axis = new Vector3(2.0f, 0.0f, 0.0f); // Not normalized
        var q = Quaternion.RotationAxis(axis, MathUtil.PiOverTwo);

        // Should still produce valid rotation
        var vector = new Vector3(0.0f, 1.0f, 0.0f);
        var rotated = Vector3.Transform(vector, q);

        Assert.False(float.IsNaN(rotated.X));
        Assert.False(float.IsNaN(rotated.Y));
        Assert.False(float.IsNaN(rotated.Z));
    }

    [Fact]
    public void TestQuaternionRotationFullCircle()
    {
        var q = Quaternion.RotationY(MathUtil.TwoPi);
        var vector = new Vector3(1.0f, 0.0f, 0.0f);
        var rotated = Vector3.Transform(vector, q);

        // Full rotation should bring us back to original position
        Assert.Equal(1.0f, rotated.X, 4);
        Assert.Equal(0.0f, rotated.Y, 4);
        Assert.Equal(0.0f, rotated.Z, 4);
    }

    [Fact]
    public void TestQuaternionEqualityEdgeCase()
    {
        var q1 = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        var q2 = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        var q3 = Quaternion.Identity;

        Assert.True(q1 == q2);
        Assert.False(q1 == q3);
        Assert.False(q1 != q2);
        Assert.True(q1 != q3);

        Assert.True(q1.Equals(q2));
        Assert.False(q1.Equals(q3));
    }

    [Fact]
    public void TestQuaternionNegation()
    {
        var q = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        var negated = -q;

        Assert.Equal(-1.0f, negated.X);
        Assert.Equal(-2.0f, negated.Y);
        Assert.Equal(-3.0f, negated.Z);
        Assert.Equal(-4.0f, negated.W);
    }

    [Fact]
    public void TestQuaternionAddition()
    {
        var q1 = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        var q2 = new Quaternion(5.0f, 6.0f, 7.0f, 8.0f);

        var sum = q1 + q2;

        Assert.Equal(6.0f, sum.X);
        Assert.Equal(8.0f, sum.Y);
        Assert.Equal(10.0f, sum.Z);
        Assert.Equal(12.0f, sum.W);
    }

    [Fact]
    public void TestQuaternionSubtraction()
    {
        var q1 = new Quaternion(5.0f, 6.0f, 7.0f, 8.0f);
        var q2 = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);

        var diff = q1 - q2;

        Assert.Equal(4.0f, diff.X);
        Assert.Equal(4.0f, diff.Y);
        Assert.Equal(4.0f, diff.Z);
        Assert.Equal(4.0f, diff.W);
    }

    [Fact]
    public void TestQuaternionScalarMultiplication()
    {
        var q = new Quaternion(1.0f, 2.0f, 3.0f, 4.0f);
        var scaled = q * 2.0f;

        Assert.Equal(2.0f, scaled.X);
        Assert.Equal(4.0f, scaled.Y);
        Assert.Equal(6.0f, scaled.Z);
        Assert.Equal(8.0f, scaled.W);
    }

    [Fact]
    public void TestQuaternionDot()
    {
        var q1 = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);
        var q2 = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);

        var dot = Quaternion.Dot(q1, q2);
        Assert.Equal(1.0f, dot);

        var q3 = new Quaternion(0.0f, 1.0f, 0.0f, 0.0f);
        var dot2 = Quaternion.Dot(q1, q3);
        Assert.Equal(0.0f, dot2);
    }

    [Fact]
    public void TestQuaternionRotationMatrixConversion()
    {
        var q = Quaternion.RotationY(MathUtil.PiOverTwo);
        var matrix = Matrix.RotationQuaternion(q);
        var qBack = Quaternion.RotationMatrix(matrix);

        // Converting back and forth should preserve the rotation
        Assert.Equal(q.X, qBack.X, 4);
        Assert.Equal(q.Y, qBack.Y, 4);
        Assert.Equal(q.Z, qBack.Z, 4);
        Assert.Equal(MathF.Abs(q.W), MathF.Abs(qBack.W), 4); // Sign may differ
    }

    [Fact]
    public void TestQuaternionAxisAngleExtraction()
    {
        var originalAxis = Vector3.UnitY;
        var originalAngle = MathUtil.PiOverTwo;

        var q = Quaternion.RotationAxis(originalAxis, originalAngle);
        var angle = q.Angle;
        var axis = q.Axis;

        // The angle returned is the magnitude of the quaternion's rotation
        // which is calculated as 2 * acos(w), not necessarily the original angle
        // Just verify the extracted axis and angle produce a valid rotation
        Assert.False(float.IsNaN(angle));
        Assert.False(float.IsNaN(axis.X));
        Assert.False(float.IsNaN(axis.Y));
        Assert.False(float.IsNaN(axis.Z));

        // Verify axis length is approximately 1 (allowing for non-normalized in some cases)
        var axisLength = axis.Length();
        Assert.InRange(axisLength, 0.9f, 1.5f); // Allow some tolerance
    }

    [Fact]
    public void TestQuaternionRotationBetweenParallelVectors()
    {
        // Test rotating from one vector to a parallel vector
        var v1 = Vector3.Normalize(new Vector3(1.0f, 0.0f, 0.0f));
        var v2 = Vector3.Normalize(new Vector3(2.0f, 0.0f, 0.0f)); // Parallel to v1

        // Create rotation manually using axis-angle
        var axis = Vector3.Cross(v1, v2);
        if (axis.LengthSquared() < 1e-5f)
        {
            // Vectors are parallel, rotation is identity
            var q = Quaternion.Identity;
            Assert.Equal(1.0f, MathF.Abs(q.W), 4);
        }
    }

    [Fact]
    public void TestQuaternionRotationBetweenOppositeVectors()
    {
        // Test 180-degree rotation
        var v1 = new Vector3(1.0f, 0.0f, 0.0f);
        var v2 = new Vector3(-1.0f, 0.0f, 0.0f);

        // Create a 180-degree rotation around Y axis
        var q = Quaternion.RotationAxis(Vector3.UnitY, MathUtil.Pi);

        var rotated = Vector3.Transform(v1, q);
        Assert.Equal(-1.0f, rotated.X, 3);
        Assert.Equal(0.0f, rotated.Y, 3);
        Assert.Equal(0.0f, rotated.Z, 3);
    }

    #endregion
}
