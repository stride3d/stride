// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestBoundingVolumes
{
    [Fact]
    public void TestBoundingBoxExtConstruction()
    {
        var min = new Vector3(-1, -2, -3);
        var max = new Vector3(1, 2, 3);
        var box = new BoundingBoxExt(min, max);

        Assert.Equal(new Vector3(0, 0, 0), box.Center);
        Assert.Equal(new Vector3(1, 2, 3), box.Extent);
    }

    [Fact]
    public void TestBoundingBoxExtEmpty()
    {
        var empty = BoundingBoxExt.Empty;
        Assert.Equal(Vector3.Zero, empty.Center);
        Assert.True(float.IsNegativeInfinity(empty.Extent.X));
        Assert.True(float.IsNegativeInfinity(empty.Extent.Y));
        Assert.True(float.IsNegativeInfinity(empty.Extent.Z));
    }

    [Theory]
    [InlineData(0, 0, 0, true)]
    [InlineData(0.5f, 1.0f, 1.5f, true)]
    [InlineData(2, 3, 4, false)]
    public void TestBoundingBoxExtContainsPoint(float x, float y, float z, bool shouldContain)
    {
        var box = new BoundingBoxExt(new Vector3(-1, -2, -3), new Vector3(1, 2, 3));
        var point = new Vector3(x, y, z);

        var min = box.Minimum;
        var max = box.Maximum;
        bool contains = point.X >= min.X && point.X <= max.X &&
                       point.Y >= min.Y && point.Y <= max.Y &&
                       point.Z >= min.Z && point.Z <= max.Z;

        Assert.Equal(shouldContain, contains);
    }

    [Fact]
    public void TestBoundingBoxExtMerge()
    {
        var box1 = new BoundingBoxExt(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBoxExt(new Vector3(0, 0, 0), new Vector3(2, 2, 2));

        BoundingBoxExt.Merge(ref box1, ref box2, out var merged);

        // Merged box should contain both boxes
        Assert.True(merged.Extent.X >= 1.5f);
        Assert.True(merged.Extent.Y >= 1.5f);
        Assert.True(merged.Extent.Z >= 1.5f);
    }

    [Fact]
    public void TestPlaneConstruction()
    {
        // Plane(normal, d) where plane equation is: normal · point + d = 0
        // For plane at y=5 with normal pointing up: d = -5
        var plane = new Plane(new Vector3(0, 1, 0), -5);
        Assert.Equal(0f, plane.Normal.X);
        Assert.Equal(1f, plane.Normal.Y);
        Assert.Equal(0f, plane.Normal.Z);
        Assert.Equal(-5f, plane.D);
    }

    [Fact]
    public void TestPlaneNormalize()
    {
        // Plane with non-normalized normal (0,2,0) and d=-10
        // After normalization: normal becomes (0,1,0) and d becomes -5
        var plane = new Plane(new Vector3(0, 2, 0), -10);
        plane.Normalize();

        Assert.Equal(0f, plane.Normal.X);
        Assert.Equal(1f, plane.Normal.Y, 5);
        Assert.Equal(0f, plane.Normal.Z);
        Assert.Equal(-5f, plane.D, 5);
    }

    [Theory]
    [InlineData(0, 5, 0, 0)]    // Point on plane (y=5)
    [InlineData(0, 6, 0, 1)]    // Point above plane (y>5)
    [InlineData(0, 4, 0, -1)]   // Point below plane (y<5)
    public void TestPlaneDotCoordinate(float x, float y, float z, float expectedSign)
    {
        // Create plane at y=5 with normal pointing up: normal·point + d = 0
        // At y=5: (0,1,0)·(x,5,z) + d = 0 → 5 + d = 0 → d = -5
        var plane = new Plane(new Vector3(0, 1, 0), -5);
        var point = new Vector3(x, y, z);
        var dot = Plane.DotCoordinate(plane, point);

        if (expectedSign > 0)
            Assert.True(dot > 0);
        else if (expectedSign < 0)
            Assert.True(dot < 0);
        else
            Assert.True(Math.Abs(dot) < 0.0001f);
    }

    [Fact]
    public void TestPlaneIntersection()
    {
        // XZ plane at y=0
        var plane = new Plane(new Vector3(0, 1, 0), 0);
        // Ray shooting down from above
        var ray = new Ray(new Vector3(0, 5, 0), new Vector3(0, -1, 0));

        float distance;
        bool intersects = ray.Intersects(ref plane, out distance);

        Assert.True(intersects);
        Assert.Equal(5f, distance, 5);
    }

    [Fact]
    public void TestBoundingSphereConstruction()
    {
        var center = new Vector3(1, 2, 3);
        var sphere = new BoundingSphere(center, 5.0f);

        Assert.Equal(center, sphere.Center);
        Assert.Equal(5.0f, sphere.Radius);
    }

    [Fact]
    public void TestBoundingSphereEmpty()
    {
        var empty = BoundingSphere.Empty;
        Assert.Equal(Vector3.Zero, empty.Center);
        Assert.Equal(0f, empty.Radius);
    }

    [Theory]
    [InlineData(1, 2, 3, ContainmentType.Contains)]
    [InlineData(1, 2, 8, ContainmentType.Contains)]
    [InlineData(1, 2, 9, ContainmentType.Disjoint)]
    [InlineData(6, 2, 3, ContainmentType.Contains)]
    public void TestBoundingSphereContainsPoint(float x, float y, float z, ContainmentType expected)
    {
        var sphere = new BoundingSphere(new Vector3(1, 2, 3), 5.0f);
        var point = new Vector3(x, y, z);
        var result = sphere.Contains(ref point);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestBoundingSphereMerge()
    {
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 1.0f);
        var sphere2 = new BoundingSphere(new Vector3(2, 0, 0), 1.0f);

        BoundingSphere.Merge(ref sphere1, ref sphere2, out var merged);

        // Merged sphere should contain both spheres
        Assert.True(merged.Radius >= 2.0f);
    }

    [Fact]
    public void TestBoundingBoxConstruction()
    {
        var min = new Vector3(-1, -2, -3);
        var max = new Vector3(1, 2, 3);
        var box = new BoundingBox(min, max);

        Assert.Equal(min, box.Minimum);
        Assert.Equal(max, box.Maximum);
    }

    [Fact]
    public void TestBoundingBoxCenter()
    {
        var box = new BoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3));
        var center = box.Center;

        Assert.Equal(0f, center.X);
        Assert.Equal(0f, center.Y);
        Assert.Equal(0f, center.Z);
    }

    [Fact]
    public void TestBoundingBoxExtent()
    {
        var box = new BoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3));
        var extent = box.Extent;

        Assert.Equal(1f, extent.X);
        Assert.Equal(2f, extent.Y);
        Assert.Equal(3f, extent.Z);
    }

    [Theory]
    [InlineData(0, 0, 0, ContainmentType.Contains)]
    [InlineData(-1, -2, -3, ContainmentType.Contains)]
    [InlineData(1, 2, 3, ContainmentType.Contains)]
    [InlineData(2, 3, 4, ContainmentType.Disjoint)]
    public void TestBoundingBoxContainsPoint(float x, float y, float z, ContainmentType expected)
    {
        var box = new BoundingBox(new Vector3(-1, -2, -3), new Vector3(1, 2, 3));
        var point = new Vector3(x, y, z);
        var result = box.Contains(ref point);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void TestBoundingBoxMerge()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));

        BoundingBox.Merge(ref box1, ref box2, out var merged);

        Assert.Equal(new Vector3(-1, -1, -1), merged.Minimum);
        Assert.Equal(new Vector3(2, 2, 2), merged.Maximum);
    }

    [Fact]
    public void TestBoundingBoxGetCorners()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var corners = box.GetCorners();

        Assert.Equal(8, corners.Length);
        Assert.Contains(new Vector3(-1, -1, -1), corners);
        Assert.Contains(new Vector3(1, 1, 1), corners);
    }

    [Fact]
    public void TestRayConstruction()
    {
        var position = new Vector3(1, 2, 3);
        var direction = new Vector3(0, 1, 0);
        var ray = new Ray(position, direction);

        Assert.Equal(position, ray.Position);
        Assert.Equal(direction, ray.Direction);
    }
}
