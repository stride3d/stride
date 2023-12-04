// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xunit;

namespace Stride.Core.Mathematics.Tests
{
    public class TestQuaternion
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
    }
}
