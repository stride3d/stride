// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestCollisionHelper
{
    [Theory, ClassData(typeof(RayPlaneIntersectionTestData))]
    public void TestRayPlaneIntersections(Ray ray, Plane plane, bool expectedIsHit, Vector3? expectedHitPoint)
    {
        bool isHit = CollisionHelper.RayIntersectsPlane(in ray, in plane, out Vector3 hitPoint);
        Assert.Equal(expectedIsHit, isHit);
        if (isHit)
        {
            Assert.Equal(expectedHitPoint, hitPoint);
        }
    }

    private class RayPlaneIntersectionTestData : IEnumerable<object[]>
    {
        public IEnumerator<object[]> GetEnumerator()
        {
            var result = new List<object[]>();

            /* Ray hits */
            {   // Plane on XZ, Y = 0
                var rayPosition = new Vector3(1, 1, 1);
                var rayDirection = Vector3.Normalize(new Vector3(-1, -1, -1));
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitY, 0);
                bool expectedIsHit = true;
                var expectedHitPoint = new Vector3(0, 0, 0);
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on XZ, Y = 1, ray points down
                var rayPosition = new Vector3(0, 2, 0);
                var rayDirection = -Vector3.UnitY;
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitY, 1);
                bool expectedIsHit = true;
                var expectedHitPoint = new Vector3(0, 1, 0);
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on YZ, X = 1
                var rayPosition = new Vector3(2, 2, 2);
                var rayDirection = Vector3.Normalize(new Vector3(-1, -1, -1));
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitX, 1);
                bool expectedIsHit = true;
                var expectedHitPoint = new Vector3(1, 1, 1);
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on XZ, Y = 1
                var rayPosition = new Vector3(2, 2, 2);
                var rayDirection = Vector3.Normalize(new Vector3(-1, -1, -1));
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitY, 1);
                bool expectedIsHit = true;
                var expectedHitPoint = new Vector3(1, 1, 1);
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on XY, Z = 1
                var rayPosition = new Vector3(2, 2, 2);
                var rayDirection = Vector3.Normalize(new Vector3(-1, -1, -1));
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitZ, 1);
                bool expectedIsHit = true;
                var expectedHitPoint = new Vector3(1, 1, 1);
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane direction (1, 1, 0), Y = 1 - Ray down from (1, 1, 0) to (1, 0, 0)
                var rayPosition = new Vector3(1, 1, 0);
                var rayDirection = -Vector3.UnitY;
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(point: Vector3.UnitY, normal: Vector3.Normalize(new Vector3(1, 1, 0)));
                bool expectedIsHit = true;
                var expectedHitPoint = new Vector3(1, 0, 0);
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            /* Ray misses */
            {   // Plane on XZ, Y = 0 - Parallel ray
                var rayPosition = new Vector3(1, 1, 1);
                var rayDirection = Vector3.UnitY;
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitY, 0);
                bool expectedIsHit = false;
                var expectedHitPoint = null as Vector3?;
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on XZ, Y = 0 - Perpendicular ray
                var rayPosition = new Vector3(1, 1, 1);
                var rayDirection = Vector3.UnitX;
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitY, 0);
                bool expectedIsHit = false;
                var expectedHitPoint = null as Vector3?;
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on XZ, Y = 0 - Parallel ray
                var rayPosition = new Vector3(1, 1, 1);
                var rayDirection = Vector3.UnitY;
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitY, 0);
                bool expectedIsHit = false;
                var expectedHitPoint = null as Vector3?;
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }
            {   // Plane on XY, Z = 0 - Parallel ray
                var rayPosition = new Vector3(1, 1, 1);
                var rayDirection = Vector3.UnitZ;
                var ray = new Ray(rayPosition, rayDirection);
                var plane = new Plane(Vector3.UnitZ, 0);
                bool expectedIsHit = false;
                var expectedHitPoint = null as Vector3?;
                result.Add([ray, plane, expectedIsHit, expectedHitPoint]);
            }

            return result.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
