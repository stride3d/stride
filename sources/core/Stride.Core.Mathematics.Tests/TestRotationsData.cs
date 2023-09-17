// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using System.Collections.Generic;

namespace Stride.Core.Mathematics.Tests
{
    public static class TestRotationsData
    {
        private static readonly float[] PrimaryAnglesToTest = new[]
        {
            // +/-90 are the singularities, but test other angles for coverage
            -180, -90, -30, 0f, 30, 90, 180
        };

        public class YRPTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var result = new List<object[]>();
                foreach (var pitchDegrees in PrimaryAnglesToTest)
                {
                    // For yaw/pitch/roll tests, the second rotation axis contains the singularity issue (ie. pitch/X-axis)
                    // Yaw & Roll are arbitrary non-zero values to ensure the rotation are working correctly
                    float yawDegrees = 45;
                    float rollDegrees = -90;
                    result.Add(new object[] { yawDegrees, pitchDegrees, rollDegrees });
                }
                // For completeness, also test the pitch rotation at singularities by itself
                result.Add(new object[] { 0, -90, 0 });
                result.Add(new object[] { 0, 90, 0 });

                return result.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }

        public class XYZTestData : IEnumerable<object[]>
        {
            public IEnumerator<object[]> GetEnumerator()
            {
                var result = new List<object[]>();
                foreach (var yawDegrees in PrimaryAnglesToTest)
                {
                    // For XYZ tests, the second rotation axis contains the singularity issue (ie. yaw/Y-axis)
                    // Pitch & Roll are arbitrary non-zero values to ensure the rotation are working correctly
                    float pitchDegrees = 45;
                    float rollDegrees = -90;
                    result.Add(new object[] { yawDegrees, pitchDegrees, rollDegrees });
                }
                // For completeness, also test the yaw rotation at singularities by itself
                result.Add(new object[] { -90, 0, 0 });
                result.Add(new object[] { 90, 0, 0 });

                return result.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
