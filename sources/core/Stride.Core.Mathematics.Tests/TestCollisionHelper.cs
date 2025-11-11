// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestCollisionHelper
{
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
    public void TestRayIntersectsRay_Intersecting()
    {
        var ray1 = new Ray(new Vector3(0, 0, 0), Vector3.UnitX);
        var ray2 = new Ray(new Vector3(0, 0, 0), Vector3.UnitY);
        
        var result = CollisionHelper.RayIntersectsPoint(ref ray1, ref ray2.Position);
        
        Assert.True(result);
    }

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
}
