// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestBoundingSphere
{
    [Fact]
    public void TestBoundingSphereFromPoints()
    {
        var points = new[]
        {
            new Vector3(1, 0, 0),
            new Vector3(-1, 0, 0),
            new Vector3(0, 1, 0),
            new Vector3(0, -1, 0),
            new Vector3(0, 0, 1),
            new Vector3(0, 0, -1)
        };

        var sphere = BoundingSphere.FromPoints(points);
        
        // Sphere should contain all points
        foreach (var point in points)
        {
            var distance = Vector3.Distance(sphere.Center, point);
            Assert.True(distance <= sphere.Radius);
        }

        // Test ref version
        BoundingSphere.FromPoints(points, out var sphere2);
        Assert.Equal(sphere.Center, sphere2.Center);
        Assert.Equal(sphere.Radius, sphere2.Radius);
    }

    [Fact]
    public void TestBoundingSphereFromBox()
    {
        var box = new BoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3));
        var sphere = BoundingSphere.FromBox(box);
        
        // Center should be at box center
        Assert.Equal(new Vector3(0, 0, 0), sphere.Center);
        
        // Radius should be distance from center to corner
        var expectedRadius = new Vector3(1, 2, 3).Length();
        Assert.Equal(expectedRadius, sphere.Radius, 3);

        // Test ref version
        BoundingSphere.FromBox(ref box, out var sphere2);
        Assert.Equal(sphere, sphere2);
    }

    [Fact]
    public void TestBoundingSphereTransform()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var transform = Matrix.Translation(10, 20, 30);
        
        BoundingSphere.Transform(ref sphere, ref transform, out var transformed);
        
        Assert.Equal(new Vector3(10, 20, 30), transformed.Center);
        Assert.Equal(5, transformed.Radius); // Radius unchanged by translation
    }

    [Fact]
    public void TestBoundingSphereTransformScale()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var transform = Matrix.Scaling(2, 2, 2);
        
        BoundingSphere.Transform(ref sphere, ref transform, out var transformed);
        
        Assert.Equal(Vector3.Zero, transformed.Center);
        Assert.Equal(10, transformed.Radius, 3); // Radius scaled by 2
    }

    [Fact]
    public void TestBoundingSphereMerge()
    {
        var sphere1 = new BoundingSphere(new Vector3(-5, 0, 0), 2);
        var sphere2 = new BoundingSphere(new Vector3(5, 0, 0), 2);
        
        BoundingSphere.Merge(ref sphere1, ref sphere2, out var merged);
        
        // Merged sphere should contain both spheres
        Assert.True(merged.Contains(ref sphere1) != ContainmentType.Disjoint);
        Assert.True(merged.Contains(ref sphere2) != ContainmentType.Disjoint);

        // Test non-ref version
        var merged2 = BoundingSphere.Merge(sphere1, sphere2);
        Assert.Equal(merged.Center, merged2.Center);
        Assert.Equal(merged.Radius, merged2.Radius);
    }

    [Fact]
    public void TestBoundingSphereContainsSphere()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 10);
        var sphere2 = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var sphere3 = new BoundingSphere(new Vector3(20, 0, 0), 5);
        var sphere4 = new BoundingSphere(new Vector3(8, 0, 0), 3);

        Assert.Equal(ContainmentType.Contains, sphere1.Contains(ref sphere2));
        Assert.Equal(ContainmentType.Disjoint, sphere1.Contains(ref sphere3));
        Assert.Equal(ContainmentType.Intersects, sphere1.Contains(ref sphere4));
    }

    [Fact]
    public void TestBoundingSphereContainsBox()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 10);
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(20, 20, 20), new Vector3(21, 21, 21));
        var box3 = new BoundingBox(new Vector3(5, 5, 5), new Vector3(15, 15, 15));

        Assert.Equal(ContainmentType.Contains, sphere.Contains(ref box1));
        Assert.Equal(ContainmentType.Disjoint, sphere.Contains(ref box2));
        Assert.Equal(ContainmentType.Intersects, sphere.Contains(ref box3));
    }

    [Fact]
    public void TestBoundingSphereIntersectsSphere()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var sphere2 = new BoundingSphere(new Vector3(8, 0, 0), 5);
        var sphere3 = new BoundingSphere(new Vector3(20, 0, 0), 5);

        Assert.True(sphere1.Intersects(ref sphere2));
        Assert.False(sphere1.Intersects(ref sphere3));
    }

    [Fact]
    public void TestBoundingSphereIntersectsBox()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(20, 20, 20), new Vector3(21, 21, 21));

        Assert.True(sphere.Intersects(ref box1));
        Assert.False(sphere.Intersects(ref box2));
    }

    [Fact]
    public void TestBoundingSphereIntersectsRay()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 5);
        
        // Ray pointing at sphere
        var ray1 = new Ray(new Vector3(-10, 0, 0), new Vector3(1, 0, 0));
        Assert.True(sphere.Intersects(ref ray1));
        
        // Ray pointing away
        var ray2 = new Ray(new Vector3(-10, 0, 0), new Vector3(-1, 0, 0));
        Assert.False(sphere.Intersects(ref ray2));
        
        // Ray from inside
        var ray3 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Assert.True(sphere.Intersects(ref ray3));
    }

    [Fact]
    public void TestBoundingSphereIntersectsRayWithDistance()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var ray = new Ray(new Vector3(-10, 0, 0), new Vector3(1, 0, 0));
        
        var result = sphere.Intersects(ref ray, out float distance);
        
        Assert.True(result);
        Assert.Equal(5, distance, 3); // Distance to sphere surface
    }

    [Fact]
    public void TestBoundingSphereIntersectsPlane()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 5);
        
        // Plane through sphere
        var plane1 = new Plane(0, 1, 0, 0);
        Assert.Equal(PlaneIntersectionType.Intersecting, sphere.Intersects(ref plane1));
        
        // Plane above sphere (normal points up, positive D means plane is shifted down)
        var plane2 = new Plane(0, 1, 0, 10);
        Assert.Equal(PlaneIntersectionType.Front, sphere.Intersects(ref plane2));
        
        // Plane below sphere (negative D means plane is shifted up)
        var plane3 = new Plane(0, 1, 0, -10);
        Assert.Equal(PlaneIntersectionType.Back, sphere.Intersects(ref plane3));
    }

    [Fact]
    public void TestBoundingSphereEquality()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var sphere2 = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var sphere3 = new BoundingSphere(new Vector3(1, 1, 1), 5);

        Assert.True(sphere1 == sphere2);
        Assert.False(sphere1 == sphere3);
        Assert.False(sphere1 != sphere2);
        Assert.True(sphere1 != sphere3);

        Assert.True(sphere1.Equals(sphere2));
        Assert.False(sphere1.Equals(sphere3));
    }

    [Fact]
    public void TestBoundingSphereHashCode()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var sphere2 = new BoundingSphere(new Vector3(0, 0, 0), 5);
        var sphere3 = new BoundingSphere(new Vector3(1, 1, 1), 5);

        Assert.Equal(sphere1.GetHashCode(), sphere2.GetHashCode());
        Assert.NotEqual(sphere1.GetHashCode(), sphere3.GetHashCode());
    }

    [Fact]
    public void TestBoundingSphereToString()
    {
        var sphere = new BoundingSphere(new Vector3(1, 2, 3), 5);
        var str = sphere.ToString();
        Assert.NotEmpty(str);
        Assert.Contains("Center", str);
        Assert.Contains("Radius", str);
    }

    [Fact]
    public void TestBoundingSphereEmpty()
    {
        var empty = BoundingSphere.Empty;
        Assert.Equal(Vector3.Zero, empty.Center);
        Assert.Equal(0, empty.Radius);
    }
}
