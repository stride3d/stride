// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestCollisionHelper
{
    // ============================================
    // 1. ClosestPoint methods
    // ============================================

    // Point
    [Fact]
    public void TestClosestPointPointTriangle()
    {
        var point = new Vector3(0, 5, 0);
        var vertex1 = new Vector3(-1, 0, -1);
        var vertex2 = new Vector3(1, 0, -1);
        var vertex3 = new Vector3(0, 0, 1);

        CollisionHelper.ClosestPointPointTriangle(ref point, ref vertex1, ref vertex2, ref vertex3, out var result);

        // Closest point should be on the triangle (Y should be 0)
        Assert.Equal(0.0f, result.Y, 5);
    }

    // Plane
    [Fact]
    public void TestClosestPointPlanePoint()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var point = new Vector3(5, 10, 5);

        CollisionHelper.ClosestPointPlanePoint(ref plane, ref point, out var result);

        Assert.Equal(5, result.X);
        Assert.Equal(0, result.Y);
        Assert.Equal(5, result.Z);
    }

    // Box
    [Fact]
    public void TestClosestPointBoxPoint_Inside()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var point = new Vector3(0, 0, 0);

        CollisionHelper.ClosestPointBoxPoint(ref box, ref point, out var result);

        Assert.Equal(point, result);
    }

    [Fact]
    public void TestClosestPointBoxPoint_Outside()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var point = new Vector3(5, 5, 5);

        CollisionHelper.ClosestPointBoxPoint(ref box, ref point, out var result);

        Assert.Equal(1, result.X);
        Assert.Equal(1, result.Y);
        Assert.Equal(1, result.Z);
    }

    // Sphere
    [Fact]
    public void TestClosestPointSpherePoint_Outside()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);
        var point = new Vector3(10, 0, 0);

        CollisionHelper.ClosestPointSpherePoint(ref sphere, ref point, out var result);

        Assert.Equal(1.0f, result.X, 5);
        Assert.Equal(0.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void TestClosestPointSpherePoint_AtCenter()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);
        var point = Vector3.Zero;

        CollisionHelper.ClosestPointSpherePoint(ref sphere, ref point, out var result);

        Assert.Equal(Vector3.Zero, result);
    }

    [Fact]
    public void TestClosestPointSphereSphere_Separated()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 1.0f);
        var sphere2 = new BoundingSphere(new Vector3(10, 0, 0), 1.0f);

        CollisionHelper.ClosestPointSphereSphere(ref sphere1, ref sphere2, out var result);

        Assert.Equal(1.0f, result.X, 5);
        Assert.Equal(0.0f, result.Y, 5);
        Assert.Equal(0.0f, result.Z, 5);
    }

    [Fact]
    public void TestClosestPointSphereSphere_SameCenter()
    {
        var sphere1 = new BoundingSphere(Vector3.Zero, 1.0f);
        var sphere2 = new BoundingSphere(Vector3.Zero, 2.0f);

        CollisionHelper.ClosestPointSphereSphere(ref sphere1, ref sphere2, out var result);

        Assert.Equal(Vector3.Zero, result);
    }

    // ============================================
    // 2. Distance methods
    // ============================================

    [Fact]
    public void TestDistancePlanePoint_Above()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var point = new Vector3(0, 5, 0);

        var distance = CollisionHelper.DistancePlanePoint(ref plane, ref point);

        Assert.Equal(5.0f, distance, 5);
    }

    [Fact]
    public void TestDistancePlanePoint_OnPlane()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var point = new Vector3(5, 0, 5);

        var distance = CollisionHelper.DistancePlanePoint(ref plane, ref point);

        Assert.Equal(0.0f, distance, 5);
    }

    [Fact]
    public void TestDistanceBoxPoint_Inside()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var point = new Vector3(0, 0, 0);

        var distance = CollisionHelper.DistanceBoxPoint(ref box, ref point);

        Assert.Equal(0.0f, distance);
    }

    [Fact]
    public void TestDistanceBoxPoint_Outside()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var point = new Vector3(2, 2, 2);

        var distance = CollisionHelper.DistanceBoxPoint(ref box, ref point);

        Assert.True(distance > 0);
    }

    [Fact]
    public void TestDistanceBoxBox_Separated()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(5, 5, 5), new Vector3(10, 10, 10));

        var distance = CollisionHelper.DistanceBoxBox(ref box1, ref box2);

        Assert.True(distance > 0);
    }

    [Fact]
    public void TestDistanceBoxBox_Overlapping()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));

        var distance = CollisionHelper.DistanceBoxBox(ref box1, ref box2);

        Assert.Equal(0.0f, distance);
    }

    [Fact]
    public void TestDistanceSpherePoint_Outside()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);
        var point = new Vector3(5, 0, 0);

        var distance = CollisionHelper.DistanceSpherePoint(ref sphere, ref point);

        Assert.Equal(4.0f, distance, 5);
    }

    [Fact]
    public void TestDistanceSpherePoint_OnSurface()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);
        var point = new Vector3(1, 0, 0);

        var distance = CollisionHelper.DistanceSpherePoint(ref sphere, ref point);

        Assert.Equal(0.0f, distance, 5);
    }

    [Fact]
    public void TestDistanceSphereSphere_Separated()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 1.0f);
        var sphere2 = new BoundingSphere(new Vector3(10, 0, 0), 1.0f);

        var distance = CollisionHelper.DistanceSphereSphere(ref sphere1, ref sphere2);

        Assert.Equal(8.0f, distance, 5);
    }

    [Fact]
    public void TestDistanceSphereSphere_Touching()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 1.0f);
        var sphere2 = new BoundingSphere(new Vector3(2, 0, 0), 1.0f);

        var distance = CollisionHelper.DistanceSphereSphere(ref sphere1, ref sphere2);

        Assert.Equal(0.0f, distance, 5);
    }

    // ============================================
    // 3. LinePlaneIntersection
    // ============================================

    [Fact]
    public void TestLinePlaneIntersection_Intersecting()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var point1 = new Vector3(0, 5, 0);
        var point2 = new Vector3(0, -5, 0);

        var result = CollisionHelper.LinePlaneIntersection(plane, point1, point2, out var intersection);

        Assert.True(result);
        Assert.Equal(0.0f, intersection.X, 5);
        Assert.Equal(0.0f, intersection.Y, 5);
        Assert.Equal(0.0f, intersection.Z, 5);
    }

    [Fact]
    public void TestLinePlaneIntersection_Parallel()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var point1 = new Vector3(0, 5, 0);
        var point2 = new Vector3(10, 5, 0);

        var result = CollisionHelper.LinePlaneIntersection(plane, point1, point2, out var intersection);

        Assert.False(result);
        Assert.Equal(Vector3.Zero, intersection);
    }

    // ============================================
    // 4. Ray intersection methods
    // ============================================

    [Fact]
    public void TestRayIntersectsPoint_Intersects()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);
        var point = new Vector3(5, 0, 0);

        var result = CollisionHelper.RayIntersectsPoint(ref ray, ref point);

        Assert.True(result);
    }

    [Fact]
    public void TestRayIntersectsPoint_DoesNotIntersect()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);
        var point = new Vector3(5, 5, 0);

        var result = CollisionHelper.RayIntersectsPoint(ref ray, ref point);

        Assert.False(result);
    }

    [Fact]
    public void TestRayIntersectsPoint_Behind()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitX);
        var point = new Vector3(-5, 0, 0);

        var result = CollisionHelper.RayIntersectsPoint(ref ray, ref point);

        Assert.False(result);
    }

    [Fact]
    public void TestRayIntersectsRay()
    {
        // Intersecting rays in 2D
        var ray1 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var ray2 = new Ray(new Vector3(0.5f, -1, 0), new Vector3(0, 1, 0));

        var result = CollisionHelper.RayIntersectsRay(ref ray1, ref ray2, out var point);

        Assert.True(result);
        Assert.InRange(point.X, 0.4f, 0.6f);
    }

    [Fact]
    public void TestRayIntersectsRay_Intersecting()
    {
        var ray1 = new Ray(new Vector3(0, 0, 0), Vector3.UnitX);
        var ray2 = new Ray(new Vector3(0, 0, 0), Vector3.UnitY);

        var result = CollisionHelper.RayIntersectsPoint(ref ray1, ref ray2.Position);

        Assert.True(result);
    }

    [Fact]
    public void TestRayIntersectsPlaneWithDistance()
    {
        var ray = new Ray(new Vector3(0, 5, 0), -Vector3.UnitY);
        var plane = new Plane(Vector3.UnitY, 0);

        var result = CollisionHelper.RayIntersectsPlane(ref ray, ref plane, out float distance);

        Assert.True(result);
        Assert.Equal(5.0f, distance, 5);
    }

    [Fact]
    public void TestRayIntersectsPlaneWithPoint()
    {
        var ray = new Ray(new Vector3(0, 5, 0), -Vector3.UnitY);
        var plane = new Plane(Vector3.UnitY, 0);

        var result = CollisionHelper.RayIntersectsPlane(ref ray, ref plane, out Vector3 point);

        Assert.True(result);
        Assert.Equal(0.0f, point.Y, 5);
    }

    [Fact]
    public void TestRayIntersectsTriangleWithDistance()
    {
        var ray = new Ray(new Vector3(0, 5, 0), -Vector3.UnitY);
        var v1 = new Vector3(-1, 0, -1);
        var v2 = new Vector3(1, 0, -1);
        var v3 = new Vector3(0, 0, 1);

        var result = CollisionHelper.RayIntersectsTriangle(ref ray, ref v1, ref v2, ref v3, out float distance);

        Assert.True(result);
        Assert.Equal(5.0f, distance, 5);
    }

    [Fact]
    public void TestRayIntersectsTriangleWithPoint()
    {
        var ray = new Ray(new Vector3(0, 5, 0), -Vector3.UnitY);
        var v1 = new Vector3(-1, 0, -1);
        var v2 = new Vector3(1, 0, -1);
        var v3 = new Vector3(0, 0, 1);

        var result = CollisionHelper.RayIntersectsTriangle(ref ray, ref v1, ref v2, ref v3, out Vector3 point);

        Assert.True(result);
        Assert.Equal(0.0f, point.Y, 5);
    }

    [Fact]
    public void TestRayIntersectsRectangle()
    {
        var ray = new Ray(new Vector3(0, 0, 5), -Vector3.UnitZ);
        var matrix = Matrix.Identity;
        var size = new Vector3(2, 2, 0);

        var result = CollisionHelper.RayIntersectsRectangle(ref ray, ref matrix, ref size, 2, out var intersectionPoint);

        Assert.True(result);
        Assert.Equal(0.0f, intersectionPoint.Z, 5);
    }

    [Fact]
    public void TestRayIntersectsBoxWithDistance()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

        var result = CollisionHelper.RayIntersectsBox(ref ray, ref box, out float distance);

        Assert.True(result);
        Assert.InRange(distance, 3.9f, 4.1f);
    }

    [Fact]
    public void TestRayIntersectsBoxWithPoint()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

        var result = CollisionHelper.RayIntersectsBox(ref ray, ref box, out Vector3 point);

        Assert.True(result);
        Assert.Equal(-1.0f, point.Z, 5);
    }

    [Fact]
    public void TestRayIntersectsSphereWithDistance()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);

        var result = CollisionHelper.RayIntersectsSphere(ref ray, ref sphere, out float distance);

        Assert.True(result);
        Assert.InRange(distance, 3.9f, 4.1f);
    }

    [Fact]
    public void TestRayIntersectsSphereWithPoint()
    {
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);

        var result = CollisionHelper.RayIntersectsSphere(ref ray, ref sphere, out Vector3 point);

        Assert.True(result);
        Assert.InRange(point.Z, -1.1f, -0.9f);
    }

    // ============================================
    // 5. Plane intersection methods
    // ============================================

    [Fact]
    public void TestPlaneIntersectsPoint()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var pointOn = new Vector3(0, 0, 0);
        var pointFront = new Vector3(0, 1, 0);
        var pointBack = new Vector3(0, -1, 0);

        Assert.Equal(PlaneIntersectionType.Intersecting, CollisionHelper.PlaneIntersectsPoint(ref plane, ref pointOn));
        Assert.Equal(PlaneIntersectionType.Front, CollisionHelper.PlaneIntersectsPoint(ref plane, ref pointFront));
        Assert.Equal(PlaneIntersectionType.Back, CollisionHelper.PlaneIntersectsPoint(ref plane, ref pointBack));
    }

    [Fact]
    public void TestPlaneIntersectsPlaneBoolean()
    {
        var plane1 = new Plane(Vector3.UnitY, 0);
        var plane2 = new Plane(Vector3.UnitX, 0);
        var plane3 = new Plane(Vector3.UnitY, 1);

        Assert.True(CollisionHelper.PlaneIntersectsPlane(ref plane1, ref plane2));
        Assert.False(CollisionHelper.PlaneIntersectsPlane(ref plane1, ref plane3));
    }

    [Fact]
    public void TestPlaneIntersectsPlaneWithRay()
    {
        var plane1 = new Plane(Vector3.UnitY, 0);
        var plane2 = new Plane(Vector3.UnitX, 0);

        var result = CollisionHelper.PlaneIntersectsPlane(ref plane1, ref plane2, out var line);

        Assert.True(result);
        Assert.NotEqual(Vector3.Zero, line.Direction);
    }

    [Fact]
    public void TestPlaneIntersectsTriangle()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var v1 = new Vector3(0, -1, 0);
        var v2 = new Vector3(0, 1, 0);
        var v3 = new Vector3(1, 0, 0);

        var result = CollisionHelper.PlaneIntersectsTriangle(ref plane, ref v1, ref v2, ref v3);

        Assert.Equal(PlaneIntersectionType.Intersecting, result);
    }

    [Fact]
    public void TestPlaneIntersectsBox()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var boxIntersecting = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var boxFront = new BoundingBox(new Vector3(-1, 1, -1), new Vector3(1, 2, 1));

        Assert.Equal(PlaneIntersectionType.Intersecting, CollisionHelper.PlaneIntersectsBox(ref plane, ref boxIntersecting));
        Assert.Equal(PlaneIntersectionType.Front, CollisionHelper.PlaneIntersectsBox(ref plane, ref boxFront));
    }

    [Fact]
    public void TestPlaneIntersectsSphere()
    {
        var plane = new Plane(Vector3.UnitY, 0);
        var sphereIntersecting = new BoundingSphere(Vector3.Zero, 1.0f);
        var sphereFront = new BoundingSphere(new Vector3(0, 2, 0), 1.0f);

        Assert.Equal(PlaneIntersectionType.Intersecting, CollisionHelper.PlaneIntersectsSphere(ref plane, ref sphereIntersecting));
        Assert.Equal(PlaneIntersectionType.Front, CollisionHelper.PlaneIntersectsSphere(ref plane, ref sphereFront));
    }

    // ============================================
    // 6. Box/Sphere intersections
    // ============================================

    [Fact]
    public void TestBoxIntersectsBox()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
        var box3 = new BoundingBox(new Vector3(5, 5, 5), new Vector3(10, 10, 10));

        Assert.True(CollisionHelper.BoxIntersectsBox(ref box1, ref box2));
        Assert.False(CollisionHelper.BoxIntersectsBox(ref box1, ref box3));
    }

    [Fact]
    public void TestBoxIntersectsSphere()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var sphereIntersecting = new BoundingSphere(Vector3.Zero, 1.0f);
        var sphereSeparated = new BoundingSphere(new Vector3(10, 0, 0), 1.0f);

        Assert.True(CollisionHelper.BoxIntersectsSphere(ref box, ref sphereIntersecting));
        Assert.False(CollisionHelper.BoxIntersectsSphere(ref box, ref sphereSeparated));
    }

    [Fact]
    public void TestSphereIntersectsTriangle()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 2.0f);
        var v1 = new Vector3(1, 0, 0);
        var v2 = new Vector3(0, 1, 0);
        var v3 = new Vector3(0, 0, 1);

        var result = CollisionHelper.SphereIntersectsTriangle(ref sphere, ref v1, ref v2, ref v3);

        Assert.True(result);
    }

    [Fact]
    public void TestSphereIntersectsSphere()
    {
        var sphere1 = new BoundingSphere(Vector3.Zero, 1.0f);
        var sphere2 = new BoundingSphere(new Vector3(1, 0, 0), 1.0f);
        var sphere3 = new BoundingSphere(new Vector3(10, 0, 0), 1.0f);

        Assert.True(CollisionHelper.SphereIntersectsSphere(ref sphere1, ref sphere2));
        Assert.False(CollisionHelper.SphereIntersectsSphere(ref sphere1, ref sphere3));
    }

    // ============================================
    // 7. Contains methods
    // ============================================

    [Fact]
    public void TestBoxContainsPoint()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var pointInside = Vector3.Zero;
        var pointOutside = new Vector3(5, 0, 0);

        Assert.Equal(ContainmentType.Contains, CollisionHelper.BoxContainsPoint(ref box, ref pointInside));
        Assert.Equal(ContainmentType.Disjoint, CollisionHelper.BoxContainsPoint(ref box, ref pointOutside));
    }

    [Fact]
    public void TestBoxContainsBox()
    {
        var box1 = new BoundingBox(new Vector3(-2, -2, -2), new Vector3(2, 2, 2));
        var box2 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box3 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(3, 3, 3));

        Assert.Equal(ContainmentType.Contains, CollisionHelper.BoxContainsBox(ref box1, ref box2));
        Assert.Equal(ContainmentType.Intersects, CollisionHelper.BoxContainsBox(ref box1, ref box3));
    }

    [Fact]
    public void TestBoxContainsSphere()
    {
        var box = new BoundingBox(new Vector3(-2, -2, -2), new Vector3(2, 2, 2));
        var sphereInside = new BoundingSphere(Vector3.Zero, 0.5f);
        var sphereOutside = new BoundingSphere(new Vector3(10, 0, 0), 1.0f);

        Assert.Equal(ContainmentType.Contains, CollisionHelper.BoxContainsSphere(ref box, ref sphereInside));
        Assert.Equal(ContainmentType.Disjoint, CollisionHelper.BoxContainsSphere(ref box, ref sphereOutside));
    }

    [Fact]
    public void TestSphereContainsPoint()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 1.0f);
        var pointInside = new Vector3(0.5f, 0, 0);
        var pointOutside = new Vector3(5, 0, 0);

        Assert.Equal(ContainmentType.Contains, CollisionHelper.SphereContainsPoint(ref sphere, ref pointInside));
        Assert.Equal(ContainmentType.Disjoint, CollisionHelper.SphereContainsPoint(ref sphere, ref pointOutside));
    }

    [Fact]
    public void TestSphereContainsTriangle()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 2.0f);
        var v1 = new Vector3(0.1f, 0, 0);
        var v2 = new Vector3(0, 0.1f, 0);
        var v3 = new Vector3(0, 0, 0.1f);

        var result = CollisionHelper.SphereContainsTriangle(ref sphere, ref v1, ref v2, ref v3);

        Assert.Equal(ContainmentType.Contains, result);
    }

    [Fact]
    public void TestSphereContainsBox()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 2.0f);
        var boxInside = new BoundingBox(new Vector3(-0.5f, -0.5f, -0.5f), new Vector3(0.5f, 0.5f, 0.5f));
        var boxOutside = new BoundingBox(new Vector3(10, 10, 10), new Vector3(11, 11, 11));

        Assert.Equal(ContainmentType.Contains, CollisionHelper.SphereContainsBox(ref sphere, ref boxInside));
        Assert.Equal(ContainmentType.Disjoint, CollisionHelper.SphereContainsBox(ref sphere, ref boxOutside));
    }

    [Fact]
    public void TestSphereContainsSphere()
    {
        var sphere1 = new BoundingSphere(Vector3.Zero, 2.0f);
        var sphere2 = new BoundingSphere(Vector3.Zero, 1.0f);
        var sphere3 = new BoundingSphere(new Vector3(1, 0, 0), 0.5f);

        Assert.Equal(ContainmentType.Contains, CollisionHelper.SphereContainsSphere(ref sphere1, ref sphere2));
        Assert.Equal(ContainmentType.Contains, CollisionHelper.SphereContainsSphere(ref sphere1, ref sphere3));
    }

    // ============================================
    // 8. GetNearestHit
    // ============================================

    [Fact]
    public void TestGetNearestHit()
    {
        var ray = new Ray(Vector3.Zero, Vector3.UnitZ);
        var spheres = new[]
        {
            new BoundingSphere(new Vector3(0, 0, 5), 1.0f),
            new BoundingSphere(new Vector3(0, 0, 10), 1.0f),
            new BoundingSphere(new Vector3(0, 0, 3), 1.0f)
        };

        var result = CollisionHelper.GetNearestHit(spheres, ref ray, out var hitObject, out var distance, out var point);

        Assert.True(result);
        Assert.Equal(spheres[2], hitObject);
        Assert.InRange(distance, 1.9f, 2.1f);
    }
}
