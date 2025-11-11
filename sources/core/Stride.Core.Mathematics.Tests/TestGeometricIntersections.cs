// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestGeometricIntersections
{
    [Fact]
    public void TestRaySphereIntersection()
    {
        var sphere = new BoundingSphere(new Vector3(0, 0, 0), 1.0f);

        // Test ray that hits sphere
        var ray1 = new Ray(new Vector3(0, 0, -2), Vector3.UnitZ);
        float distance1;
        Assert.True(ray1.Intersects(ref sphere, out distance1));
        Assert.True(distance1 >= 0); // Distance should be positive

        // Test ray that misses sphere
        var ray2 = new Ray(new Vector3(2, 0, -2), Vector3.UnitZ);
        float distance2;
        Assert.False(ray2.Intersects(ref sphere, out distance2));

        // Test ray from inside sphere
        var ray3 = new Ray(Vector3.Zero, Vector3.UnitZ);
        float distance3;
        Assert.True(ray3.Intersects(ref sphere, out distance3));
        Assert.True(distance3 >= 0); // Distance should be positive

        // Test ray tangent to sphere
        var ray4 = new Ray(new Vector3(1, 0, -2), Vector3.UnitZ);
        float distance4;
        Assert.True(ray4.Intersects(ref sphere, out distance4));
        Assert.True(distance4 >= 0); // Distance should be positive
    }

    [Fact]
    public void TestRayPlaneIntersection()
    {
        // Create a plane aligned with XZ plane (normal pointing up)
        var plane = new Plane(Vector3.UnitY, -1.0f); // Plane at y = 1

        // Test ray that hits plane from above
        var ray1 = new Ray(new Vector3(0, 2, 0), -Vector3.UnitY);
        float distance1;
        Assert.True(ray1.Intersects(ref plane, out distance1));
        Assert.True(distance1 >= 0); // Distance should be positive

        // Test ray that hits plane from below
        var ray2 = new Ray(new Vector3(0, 0, 0), Vector3.UnitY);
        float distance2;
        Assert.True(ray2.Intersects(ref plane, out distance2));
        Assert.True(distance2 >= 0); // Distance should be positive

        // Test ray parallel to plane
        var ray3 = new Ray(new Vector3(0, 0, 0), Vector3.UnitX);
        float distance3;
        Assert.False(ray3.Intersects(ref plane, out distance3));

        // Test ray in plane
        var ray4 = new Ray(new Vector3(0, 1, 0), Vector3.UnitX);
        float distance4;
        Assert.False(ray4.Intersects(ref plane, out distance4));
    }

    [Fact]
    public void TestBoundingBoxBoundingBoxIntersection()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

        // Test intersecting boxes
        var box2 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
        Assert.True(box1.Intersects(ref box2));

        // Test non-intersecting boxes
        var box3 = new BoundingBox(new Vector3(2, 2, 2), new Vector3(3, 3, 3));
        Assert.False(box1.Intersects(ref box3));

        // Test touching boxes (on edge)
        var box4 = new BoundingBox(new Vector3(1, -1, -1), new Vector3(2, 1, 1));
        Assert.True(box1.Intersects(ref box4));

        // Test containing boxes
        var box5 = new BoundingBox(new Vector3(-2, -2, -2), new Vector3(2, 2, 2));
        Assert.True(box1.Intersects(ref box5));
    }

    [Fact]
    public void TestBoundingBoxBoundingSphereIntersection()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));

        // Test intersecting sphere
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 1.0f);
        Assert.True(box.Intersects(ref sphere1));

        // Test non-intersecting sphere
        var sphere2 = new BoundingSphere(new Vector3(3, 3, 3), 1.0f);
        Assert.False(box.Intersects(ref sphere2));

        // Test touching sphere (at corner)
        var sphere3 = new BoundingSphere(new Vector3(1, 1, 1), 0.0f);
        Assert.True(box.Intersects(ref sphere3));

        // Test sphere containing box
        var sphere4 = new BoundingSphere(new Vector3(0, 0, 0), 2.0f);
        Assert.True(box.Intersects(ref sphere4));

        // Test sphere intersecting at corner
        var sphere5 = new BoundingSphere(new Vector3(2, 2, 2), 1.732051f); // sqrt(3)
        Assert.True(box.Intersects(ref sphere5));
    }

    [Fact]
    public void TestRayTriangleIntersection()
    {
        var v1 = new Vector3(0, 0, 0);
        var v2 = new Vector3(1, 0, 0);
        var v3 = new Vector3(0, 1, 0);

        // Test ray that hits triangle
        var ray1 = new Ray(new Vector3(0.2f, 0.2f, 1), -Vector3.UnitZ);
        float distance1;
        Assert.True(ray1.Intersects(ref v1, ref v2, ref v3, out distance1));
        Assert.Equal(1.0f, distance1, 3);

        // Test ray that misses triangle
        var ray2 = new Ray(new Vector3(-1, -1, 1), -Vector3.UnitZ);
        float distance2;
        Assert.False(ray2.Intersects(ref v1, ref v2, ref v3, out distance2));

        // Test ray parallel to triangle
        var ray3 = new Ray(new Vector3(0, 0, 1), Vector3.UnitX);
        float distance3;
        Assert.False(ray3.Intersects(ref v1, ref v2, ref v3, out distance3));

        // Test ray hitting triangle edge
        var ray4 = new Ray(new Vector3(0.5f, 0, 1), -Vector3.UnitZ);
        float distance4;
        Assert.True(ray4.Intersects(ref v1, ref v2, ref v3, out distance4));
        Assert.Equal(1.0f, distance4, 3);
    }

    [Fact]
    public void TestBoundingFrustumIntersection()
    {
        // Create a view frustum looking down Z axis
        var projection = Matrix.PerspectiveFovRH(
            MathUtil.PiOverFour, // 45 degrees FOV
            1.0f,                // aspect ratio
            0.1f,                // near plane
            100.0f               // far plane
        );
        var view = Matrix.LookAtRH(
            new Vector3(0, 0, -10),  // camera position
            Vector3.Zero,            // looking at origin
            Vector3.UnitY            // up vector
        );
        var viewProj = view * projection;
        var frustum = new BoundingFrustum(ref viewProj);

        // Test intersection with a box
        var box = new BoundingBox(new Vector3(-1), new Vector3(1));
        var boxExt = new BoundingBoxExt(box.Minimum, box.Maximum);
        Assert.True(frustum.Contains(ref boxExt));
    }

    [Theory]
    [InlineData(1, 1, 1, 2, 2, 2, 1, 1, 1)] // Box fully inside
    [InlineData(0, 0, 0, 2, 2, 2, 1, 1, 1)] // Box intersecting
    [InlineData(3, 3, 3, 4, 4, 4, 1, 1, 1)] // Box fully outside
    public void TestBoxContainmentWithSphere(
        float boxMinX, float boxMinY, float boxMinZ,
        float boxMaxX, float boxMaxY, float boxMaxZ,
        float sphereX, float sphereY, float sphereZ)
    {
        var box = new BoundingBox(
            new Vector3(boxMinX, boxMinY, boxMinZ),
            new Vector3(boxMaxX, boxMaxY, boxMaxZ));
        var sphere = new BoundingSphere(
            new Vector3(sphereX, sphereY, sphereZ),
            0.5f);

        var result = box.Contains(ref sphere);

        if (boxMinX <= sphereX && sphereX <= boxMaxX &&
            boxMinY <= sphereY && sphereY <= boxMaxY &&
            boxMinZ <= sphereZ && sphereZ <= boxMaxZ)
        {
            // If sphere center is inside box, result should be Contains or Intersects
            Assert.True(result == ContainmentType.Contains || result == ContainmentType.Intersects);
        }
        else
        {
            // If sphere center is outside box, result should be Disjoint
            Assert.Equal(ContainmentType.Disjoint, result);
        }
    }

    [Fact]
    public void TestRayTriangleIntersectionDegenerate()
    {
        // Test degenerate triangle (line)
        var v1 = new Vector3(0, 0, 0);
        var v2 = new Vector3(1, 0, 0);
        var v3 = new Vector3(1, 0, 0); // Same as v2, making a degenerate triangle

        var ray = new Ray(new Vector3(0.5f, 1, 1), -Vector3.UnitY);
        float distance;
        Assert.False(ray.Intersects(ref v1, ref v2, ref v3, out distance));

        // Test degenerate triangle (point)
        v2 = v1; // All vertices at same point
        v3 = v1;
        Assert.False(ray.Intersects(ref v1, ref v2, ref v3, out distance));
    }

    [Fact]
    public void TestInvalidBoundingBox()
    {
        // Create a box with min > max
        var invalidBox = new BoundingBox(
            new Vector3(1, 1, 1),
            new Vector3(0, 0, 0));

        // Test that box reports correct min and max
        Assert.True(invalidBox.Minimum.X > invalidBox.Maximum.X);
        Assert.True(invalidBox.Minimum.Y > invalidBox.Maximum.Y);
        Assert.True(invalidBox.Minimum.Z > invalidBox.Maximum.Z);

        // Test that the invalid box can still be used in intersection tests
        var sphere = new BoundingSphere(new Vector3(0.5f, 0.5f, 0.5f), 2.0f);
        Assert.True(invalidBox.Intersects(ref sphere)); // Large sphere should still intersect
    }

    [Fact]
    public void TestZeroRadiusSphereIntersection()
    {
        var sphere = new BoundingSphere(Vector3.Zero, 0.0f);

        // Test ray that passes through sphere center
        var ray1 = new Ray(new Vector3(0, 0, -1), Vector3.UnitZ);
        float distance1;
        Assert.True(ray1.Intersects(ref sphere, out distance1));
        Assert.True(distance1 >= 0);

        // Test ray that misses sphere center
        var ray2 = new Ray(new Vector3(0.1f, 0, -1), Vector3.UnitZ);
        float distance2;
        Assert.False(ray2.Intersects(ref sphere, out distance2));
    }

    [Fact]
    public void TestSphereSphereIntersection()
    {
        var sphere1 = new BoundingSphere(Vector3.Zero, 1.0f);

        // Test intersecting spheres
        var sphere2 = new BoundingSphere(new Vector3(1.5f, 0, 0), 1.0f);
        Assert.True(sphere1.Intersects(ref sphere2));

        // Test touching spheres
        var sphere3 = new BoundingSphere(new Vector3(2, 0, 0), 1.0f);
        Assert.True(sphere1.Intersects(ref sphere3));

        // Test non-intersecting spheres
        var sphere4 = new BoundingSphere(new Vector3(3, 0, 0), 1.0f);
        Assert.False(sphere1.Intersects(ref sphere4));

        // Test contained sphere
        var sphere5 = new BoundingSphere(Vector3.Zero, 0.5f);
        Assert.True(sphere1.Intersects(ref sphere5));
    }

    [Fact]
    public void TestRayNearZeroDirection()
    {
        var ray = new Ray(Vector3.Zero, new Vector3(float.Epsilon));
        var sphere = new BoundingSphere(new Vector3(0, 0, 1), 1.0f);
        var plane = new Plane(Vector3.UnitZ, -1.0f);
        var v1 = new Vector3(0, 0, 1);
        var v2 = new Vector3(1, 0, 1);
        var v3 = new Vector3(0, 1, 1);

        float distance;

        // Ray with near-zero direction should still handle intersections correctly
        Assert.True(ray.Intersects(ref sphere, out distance));  // Should hit sphere since ray starts at center
        Assert.True(distance >= 0);

        // Test extreme cases
        var tinyRay = new Ray(Vector3.Zero, new Vector3(float.Epsilon, float.Epsilon, float.Epsilon));
        Assert.True(tinyRay.Intersects(ref sphere, out distance));
        Assert.True(distance >= 0);
    }
}