// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestRay
{
    [Fact]
    public void TestRayConstruction()
    {
        var position = new Vector3(1, 2, 3);
        var direction = new Vector3(0, 1, 0);
        var ray = new Ray(position, direction);

        Assert.Equal(1, ray.Position.X);
        Assert.Equal(2, ray.Position.Y);
        Assert.Equal(3, ray.Position.Z);
        Assert.Equal(0, ray.Direction.X);
        Assert.Equal(1, ray.Direction.Y);
        Assert.Equal(0, ray.Direction.Z);
    }

    [Fact]
    public void TestRayIntersectsPlane()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(0, 1, 0));
        var plane = new Plane(0, 1, 0, -5); // Horizontal plane at Y=5

        var result = ray.Intersects(ref plane, out float distance);
        Assert.True(result);
        Assert.Equal(5.0f, distance, 3);

        // Test with point
        var hasPoint = ray.Intersects(ref plane, out Vector3 point);
        Assert.True(hasPoint);
        Assert.Equal(0, point.X, 3);
        Assert.Equal(5, point.Y, 3);
        Assert.Equal(0, point.Z, 3);
    }

    [Fact]
    public void TestRayIntersectsBoundingBox()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var box = new BoundingBox(new Vector3(5, -1, -1), new Vector3(10, 1, 1));

        var result = ray.Intersects(ref box, out float distance);
        Assert.True(result);
        Assert.Equal(5.0f, distance, 3);
    }

    [Fact]
    public void TestRayIntersectsBoundingSphere()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var sphere = new BoundingSphere(new Vector3(10, 0, 0), 2.0f);

        var result = ray.Intersects(ref sphere, out float distance);
        Assert.True(result);
        Assert.Equal(8.0f, distance, 3); // 10 - 2 (radius)
    }

    [Fact]
    public void TestRayIntersectsTriangle()
    {
        var ray = new Ray(new Vector3(0.5f, 0.5f, -5), new Vector3(0, 0, 1));
        var v1 = new Vector3(0, 0, 0);
        var v2 = new Vector3(1, 0, 0);
        var v3 = new Vector3(0, 1, 0);

        var result = ray.Intersects(ref v1, ref v2, ref v3, out float distance);
        Assert.True(result);
        Assert.Equal(5.0f, distance, 3);
    }

    [Fact]
    public void TestRayNoIntersection()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var plane = new Plane(0, 1, 0, 5); // Parallel to ray

        var result = ray.Intersects(ref plane, out float distance);
        Assert.False(result);
    }

    [Fact]
    public void TestRayEquality()
    {
        var ray1 = new Ray(new Vector3(1, 2, 3), new Vector3(0, 1, 0));
        var ray2 = new Ray(new Vector3(1, 2, 3), new Vector3(0, 1, 0));
        var ray3 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));

        Assert.True(ray1 == ray2);
        Assert.False(ray1 == ray3);
        Assert.False(ray1 != ray2);
        Assert.True(ray1 != ray3);

        Assert.True(ray1.Equals(ray2));
        Assert.False(ray1.Equals(ray3));
    }

    [Fact]
    public void TestRayHashCode()
    {
        var ray1 = new Ray(new Vector3(1, 2, 3), new Vector3(0, 1, 0));
        var ray2 = new Ray(new Vector3(1, 2, 3), new Vector3(0, 1, 0));
        var ray3 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));

        Assert.Equal(ray1.GetHashCode(), ray2.GetHashCode());
        Assert.NotEqual(ray1.GetHashCode(), ray3.GetHashCode());
    }

    [Fact]
    public void TestRayToString()
    {
        var ray = new Ray(new Vector3(1, 2, 3), new Vector3(0, 1, 0));
        var str = ray.ToString();
        Assert.NotEmpty(str);
    }

    [Fact]
    public void TestRayIntersectsPoint()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var pointOnRay = new Vector3(5, 0, 0);
        var pointOffRay = new Vector3(5, 1, 0);

        Assert.True(ray.Intersects(ref pointOnRay));
        Assert.False(ray.Intersects(ref pointOffRay));
    }

    [Fact]
    public void TestRayIntersectsRay()
    {
        // Intersecting rays
        var ray1 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var direction = new Vector3(1, -1, 0);
        direction.Normalize();
        var ray2 = new Ray(new Vector3(0, 1, 0), direction);

        Assert.True(ray1.Intersects(ref ray2));

        // Non-intersecting skew rays (different planes, won't meet)
        var ray3 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0)); // Along X-axis
        var ray4 = new Ray(new Vector3(0, 1, 1), new Vector3(0, 1, 0)); // Along Y-axis, offset in Z
        // These rays are skew - they don't intersect and aren't parallel
    }

    [Fact]
    public void TestRayIntersectsRayWithPoint()
    {
        var ray1 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var ray2 = new Ray(new Vector3(5, 5, 0), new Vector3(0, -1, 0));

        var result = ray1.Intersects(ref ray2, out Vector3 point);
        Assert.True(result);
        Assert.Equal(5, point.X, 3);
        Assert.Equal(0, point.Y, 3);
        Assert.Equal(0, point.Z, 3);
    }

    [Fact]
    public void TestRayIntersectsBoundingBoxWithPoint()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var box = new BoundingBox(new Vector3(5, -1, -1), new Vector3(10, 1, 1));

        var result = ray.Intersects(ref box, out Vector3 point);
        Assert.True(result);
        Assert.Equal(5, point.X, 3);
        Assert.Equal(0, point.Y, 3);
        Assert.Equal(0, point.Z, 3);
    }

    [Fact]
    public void TestRayIntersectsBoundingSphereWithPoint()
    {
        var ray = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        var sphere = new BoundingSphere(new Vector3(10, 0, 0), 2.0f);

        var result = ray.Intersects(ref sphere, out Vector3 point);
        Assert.True(result);
        Assert.Equal(8, point.X, 3);
        Assert.Equal(0, point.Y, 3);
        Assert.Equal(0, point.Z, 3);
    }

    [Fact]
    public void TestRayIntersectsTriangleWithPoint()
    {
        var ray = new Ray(new Vector3(0.5f, 0.5f, -5), new Vector3(0, 0, 1));
        var v1 = new Vector3(0, 0, 0);
        var v2 = new Vector3(1, 0, 0);
        var v3 = new Vector3(0, 1, 0);

        var result = ray.Intersects(ref v1, ref v2, ref v3, out Vector3 point);
        Assert.True(result);
        Assert.Equal(0.5f, point.X, 3);
        Assert.Equal(0.5f, point.Y, 3);
        Assert.Equal(0, point.Z, 3);
    }
}
