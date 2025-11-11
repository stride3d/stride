// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestBoundingBox
{
    [Fact]
    public void TestBoundingBoxFromPoints()
    {
        var points = new[]
        {
            new Vector3(1, 2, 3),
            new Vector3(-1, -2, -3),
            new Vector3(0, 5, 0),
            new Vector3(2, -1, 1)
        };

        var box = BoundingBox.FromPoints(points);
        Assert.Equal(new Vector3(-1, -2, -3), box.Minimum);
        Assert.Equal(new Vector3(2, 5, 3), box.Maximum);

        // Test ref version
        BoundingBox.FromPoints(points, out var box2);
        Assert.Equal(box, box2);
    }

    [Fact]
    public void TestBoundingBoxFromSphere()
    {
        var sphere = new BoundingSphere(new Vector3(1, 2, 3), 5);
        var box = BoundingBox.FromSphere(sphere);
        
        Assert.Equal(new Vector3(-4, -3, -2), box.Minimum);
        Assert.Equal(new Vector3(6, 7, 8), box.Maximum);

        // Test ref version
        BoundingBox.FromSphere(ref sphere, out var box2);
        Assert.Equal(box, box2);
    }

    [Fact]
    public void TestBoundingBoxTransform()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var transform = Matrix.Translation(5, 10, 15);
        
        BoundingBox.Transform(ref box, ref transform, out var transformed);
        
        Assert.Equal(new Vector3(4, 9, 14), transformed.Minimum);
        Assert.Equal(new Vector3(6, 11, 16), transformed.Maximum);
    }

    [Fact]
    public void TestBoundingBoxTransformRotation()
    {
        // Use asymmetric box so rotation changes the bounds
        var box = new BoundingBox(new Vector3(0, 0, 0), new Vector3(1, 1, 1));
        var transform = Matrix.RotationZ(MathUtil.PiOverTwo);
        
        BoundingBox.Transform(ref box, ref transform, out var transformed);
        
        // After 90° rotation around Z, the AABB should change
        Assert.NotEqual(box.Minimum, transformed.Minimum);
    }

    [Fact]
    public void TestBoundingBoxMergeWithPoint()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var point = new Vector3(5, 0, 0);
        
        BoundingBox.Merge(ref box, ref point, out var merged);
        
        Assert.Equal(new Vector3(-1, -1, -1), merged.Minimum);
        Assert.Equal(new Vector3(5, 1, 1), merged.Maximum);
    }

    [Fact]
    public void TestBoundingBoxContainsBox()
    {
        var box1 = new BoundingBox(new Vector3(-2, -2, -2), new Vector3(2, 2, 2));
        var box2 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box3 = new BoundingBox(new Vector3(3, 3, 3), new Vector3(4, 4, 4));
        var box4 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(3, 3, 3));

        Assert.Equal(ContainmentType.Contains, box1.Contains(ref box2));
        Assert.Equal(ContainmentType.Disjoint, box1.Contains(ref box3));
        Assert.Equal(ContainmentType.Intersects, box1.Contains(ref box4));
    }

    [Fact]
    public void TestBoundingBoxContainsSphere()
    {
        var box = new BoundingBox(new Vector3(-2, -2, -2), new Vector3(2, 2, 2));
        var sphere1 = new BoundingSphere(new Vector3(0, 0, 0), 1);
        var sphere2 = new BoundingSphere(new Vector3(10, 10, 10), 1);
        var sphere3 = new BoundingSphere(new Vector3(2, 2, 2), 1);

        Assert.Equal(ContainmentType.Contains, box.Contains(ref sphere1));
        Assert.Equal(ContainmentType.Disjoint, box.Contains(ref sphere2));
        Assert.Equal(ContainmentType.Intersects, box.Contains(ref sphere3));
    }

    [Fact]
    public void TestBoundingBoxIntersectsBox()
    {
        var box1 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
        var box2 = new BoundingBox(new Vector3(1, 1, 1), new Vector3(3, 3, 3));
        var box3 = new BoundingBox(new Vector3(5, 5, 5), new Vector3(6, 6, 6));

        Assert.True(box1.Intersects(ref box2));
        Assert.False(box1.Intersects(ref box3));
    }

    [Fact]
    public void TestBoundingBoxIntersectsSphere()
    {
        var box = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));
        var sphere1 = new BoundingSphere(new Vector3(1, 1, 1), 1);
        var sphere2 = new BoundingSphere(new Vector3(10, 10, 10), 1);

        Assert.True(box.Intersects(ref sphere1));
        Assert.False(box.Intersects(ref sphere2));
    }

    [Fact]
    public void TestBoundingBoxIntersectsRay()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        
        // Ray from outside pointing at box
        var ray1 = new Ray(new Vector3(-5, 0, 0), new Vector3(1, 0, 0));
        Assert.True(box.Intersects(ref ray1));
        
        // Ray from outside pointing away
        var ray2 = new Ray(new Vector3(-5, 0, 0), new Vector3(-1, 0, 0));
        Assert.False(box.Intersects(ref ray2));
        
        // Ray from inside
        var ray3 = new Ray(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
        Assert.True(box.Intersects(ref ray3));
    }

    [Fact]
    public void TestBoundingBoxIntersectsRayWithDistance()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var ray = new Ray(new Vector3(-5, 0, 0), new Vector3(1, 0, 0));
        
        var result = box.Intersects(ref ray, out float distance);
        
        Assert.True(result);
        Assert.Equal(4, distance, 3); // Distance to box surface
    }

    [Fact]
    public void TestBoundingBoxIntersectsPlane()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        
        // Plane through box
        var plane1 = new Plane(0, 1, 0, 0);
        Assert.Equal(PlaneIntersectionType.Intersecting, box.Intersects(ref plane1));
        
        // Plane above box (normal points up, positive D means plane is shifted down)
        var plane2 = new Plane(0, 1, 0, 5);
        Assert.Equal(PlaneIntersectionType.Front, box.Intersects(ref plane2));
        
        // Plane below box (negative D means plane is shifted up)
        var plane3 = new Plane(0, 1, 0, -5);
        Assert.Equal(PlaneIntersectionType.Back, box.Intersects(ref plane3));
    }

    [Fact]
    public void TestBoundingBoxEquality()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box3 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));

        Assert.True(box1 == box2);
        Assert.False(box1 == box3);
        Assert.False(box1 != box2);
        Assert.True(box1 != box3);

        Assert.True(box1.Equals(box2));
        Assert.False(box1.Equals(box3));
    }

    [Fact]
    public void TestBoundingBoxHashCode()
    {
        var box1 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box2 = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var box3 = new BoundingBox(new Vector3(0, 0, 0), new Vector3(2, 2, 2));

        Assert.Equal(box1.GetHashCode(), box2.GetHashCode());
        Assert.NotEqual(box1.GetHashCode(), box3.GetHashCode());
    }

    [Fact]
    public void TestBoundingBoxToString()
    {
        var box = new BoundingBox(new Vector3(-1, -1, -1), new Vector3(1, 1, 1));
        var str = box.ToString();
        Assert.NotEmpty(str);
        Assert.Contains("Minimum", str);
        Assert.Contains("Maximum", str);
    }

    [Fact]
    public void TestBoundingBoxEmpty()
    {
        var empty = BoundingBox.Empty;
        Assert.Equal(float.MaxValue, empty.Minimum.X);
        Assert.Equal(float.MinValue, empty.Maximum.X);
    }
}
