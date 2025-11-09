// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Xunit;

namespace Stride.Core.Mathematics.Tests;

public class TestPlane
{
    [Fact]
    public void TestPlaneConstruction()
    {
        // Normal and D
        var plane1 = new Plane(0, 1, 0, 5);
        Assert.Equal(0, plane1.Normal.X);
        Assert.Equal(1, plane1.Normal.Y);
        Assert.Equal(0, plane1.Normal.Z);
        Assert.Equal(5, plane1.D);

        // Vector3 normal and D
        var plane2 = new Plane(new Vector3(0, 1, 0), 5);
        Assert.Equal(plane1, plane2);

        // Three points (cross product of (1,0,0) and (0,0,1) is (0,-1,0))
        var p1 = new Vector3(0, 0, 0);
        var p2 = new Vector3(1, 0, 0);
        var p3 = new Vector3(0, 0, 1);
        var plane3 = new Plane(p1, p2, p3);
        Assert.Equal(0, plane3.Normal.X, 3);
        Assert.Equal(-1, plane3.Normal.Y, 3);
        Assert.Equal(0, plane3.Normal.Z, 3);
    }

    [Fact]
    public void TestPlaneNormalize()
    {
        var plane = new Plane(2, 0, 0, 4);
        var normalized = Plane.Normalize(plane);
        Assert.Equal(1, normalized.Normal.X, 3);
        Assert.Equal(0, normalized.Normal.Y, 3);
        Assert.Equal(0, normalized.Normal.Z, 3);
        Assert.Equal(2, normalized.D, 3);

        // Test instance method
        plane.Normalize();
        Assert.Equal(1, plane.Normal.X, 3);
        Assert.Equal(2, plane.D, 3);
    }

    [Fact]
    public void TestPlaneDot()
    {
        var plane = new Plane(0, 1, 0, -5);
        var point = new Vector4(1, 2, 3, 1);
        var result = Plane.Dot(plane, point);
        Assert.Equal(-3, result); // (0*1 + 1*2 + 0*3 + (-5)*1)
    }

    [Fact]
    public void TestPlaneDotCoordinate()
    {
        var plane = new Plane(0, 1, 0, -5);
        var point = new Vector3(1, 2, 3);
        var result = Plane.DotCoordinate(plane, point);
        Assert.Equal(-3, result); // (0*1 + 1*2 + 0*3 - 5)
    }

    [Fact]
    public void TestPlaneDotNormal()
    {
        var plane = new Plane(0, 1, 0, -5);
        var vector = new Vector3(1, 2, 3);
        var result = Plane.DotNormal(plane, vector);
        Assert.Equal(2, result); // (0*1 + 1*2 + 0*3)
    }

    [Fact]
    public void TestPlaneProject()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane
        var point = new Vector3(1, 5, 2);
        var projected = Plane.Project(plane, point);
        Assert.Equal(1, projected.X, 3);
        Assert.Equal(0, projected.Y, 3);
        Assert.Equal(2, projected.Z, 3);
    }

    [Fact]
    public void TestPlaneMultiply()
    {
        var plane = new Plane(1, 0, 0, 2);
        var result = plane * 3.0f;
        Assert.Equal(3, result.Normal.X);
        Assert.Equal(0, result.Normal.Y);
        Assert.Equal(0, result.Normal.Z);
        Assert.Equal(6, result.D);

        var result2 = 3.0f * plane;
        Assert.Equal(result, result2);

        var result3 = Plane.Multiply(plane, 3.0f);
        Assert.Equal(result, result3);
    }

    [Fact]
    public void TestPlaneNegate()
    {
        var plane = new Plane(2, 0, 0, 4);

        // Test static method
        var negated = Plane.Negate(plane);
        Assert.Equal(-2, negated.Normal.X);
        Assert.Equal(0, negated.Normal.Y);
        Assert.Equal(0, negated.Normal.Z);
        Assert.Equal(-4, negated.D);

        // Test unary operator
        var negated2 = -plane;
        Assert.Equal(negated, negated2);

        // Test instance method
        plane.Negate();
        Assert.Equal(-2, plane.Normal.X);
        Assert.Equal(-4, plane.D);
    }

    [Fact]
    public void TestPlaneTransformMatrix()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane
        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        var transformed = Plane.Transform(plane, matrix);

        // After 90° rotation around Z, Y-up becomes X-left
        Assert.InRange(transformed.Normal.X, -1.1f, -0.9f);
        Assert.InRange(transformed.Normal.Y, -0.1f, 0.1f);
    }

    [Fact]
    public void TestPlaneTransformQuaternion()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane
        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        var transformed = Plane.Transform(plane, rotation);

        // After 90° rotation around Z, Y-up becomes X-left
        Assert.InRange(transformed.Normal.X, -1.1f, -0.9f);
        Assert.InRange(transformed.Normal.Y, -0.1f, 0.1f);
    }

    [Fact]
    public void TestPlaneEquality()
    {
        var plane1 = new Plane(1, 0, 0, 2);
        var plane2 = new Plane(1, 0, 0, 2);
        var plane3 = new Plane(0, 1, 0, 2);

        Assert.True(plane1 == plane2);
        Assert.False(plane1 == plane3);
        Assert.False(plane1 != plane2);
        Assert.True(plane1 != plane3);

        Assert.True(plane1.Equals(plane2));
        Assert.False(plane1.Equals(plane3));
    }

    [Fact]
    public void TestPlaneHashCode()
    {
        var plane1 = new Plane(1, 0, 0, 2);
        var plane2 = new Plane(1, 0, 0, 2);
        var plane3 = new Plane(0, 1, 0, 2);

        Assert.Equal(plane1.GetHashCode(), plane2.GetHashCode());
        Assert.NotEqual(plane1.GetHashCode(), plane3.GetHashCode());
    }

    [Fact]
    public void TestPlaneToString()
    {
        var plane = new Plane(1, 0, 0, 2);
        var str = plane.ToString();
        Assert.NotEmpty(str);
    }

    [Fact]
    public void TestPlaneIntersectsPoint()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        var pointOn = new Vector3(5, 0, 5);
        Assert.Equal(PlaneIntersectionType.Intersecting, plane.Intersects(ref pointOn));

        var pointFront = new Vector3(5, 10, 5);
        Assert.Equal(PlaneIntersectionType.Front, plane.Intersects(ref pointFront));

        var pointBack = new Vector3(5, -10, 5);
        Assert.Equal(PlaneIntersectionType.Back, plane.Intersects(ref pointBack));
    }

    [Fact]
    public void TestPlaneIntersectsRay()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        // Ray pointing down at plane
        var ray1 = new Ray(new Vector3(0, 10, 0), new Vector3(0, -1, 0));
        Assert.True(plane.Intersects(ref ray1));

        // Ray pointing away from plane
        var ray2 = new Ray(new Vector3(0, 10, 0), new Vector3(0, 1, 0));
        Assert.False(plane.Intersects(ref ray2));

        // Ray parallel to plane
        var ray3 = new Ray(new Vector3(0, 10, 0), new Vector3(1, 0, 0));
        Assert.False(plane.Intersects(ref ray3));
    }

    [Fact]
    public void TestPlaneIntersectsRayDistance()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        var ray = new Ray(new Vector3(0, 10, 0), new Vector3(0, -1, 0));
        bool intersects = plane.Intersects(ref ray, out float distance);

        Assert.True(intersects);
        Assert.Equal(10.0f, distance, 3);
    }

    [Fact]
    public void TestPlaneIntersectsRayPoint()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        var ray = new Ray(new Vector3(5, 10, 3), new Vector3(0, -1, 0));
        bool intersects = plane.Intersects(ref ray, out Vector3 point);

        Assert.True(intersects);
        Assert.Equal(5.0f, point.X, 3);
        Assert.Equal(0.0f, point.Y, 3);
        Assert.Equal(3.0f, point.Z, 3);
    }

    [Fact]
    public void TestPlaneIntersectsPlane()
    {
        // Two planes that intersect
        var plane1 = new Plane(0, 1, 0, 0); // XZ plane
        var plane2 = new Plane(1, 0, 0, 0); // YZ plane

        Assert.True(plane1.Intersects(ref plane2));

        // Two parallel planes
        var plane3 = new Plane(0, 1, 0, 5);
        Assert.False(plane1.Intersects(ref plane3));
    }

    [Fact]
    public void TestPlaneIntersectsPlaneWithLine()
    {
        var plane1 = new Plane(0, 1, 0, 0); // XZ plane
        var plane2 = new Plane(1, 0, 0, 0); // YZ plane

        bool intersects = plane1.Intersects(ref plane2, out Ray line);

        Assert.True(intersects);
        // The intersection line should be along the Z axis
        Assert.Equal(0.0f, line.Position.X, 3);
        Assert.Equal(0.0f, line.Position.Y, 3);
        Assert.Equal(0.0f, line.Direction.X, 3);
        Assert.Equal(0.0f, line.Direction.Y, 3);
    }

    [Fact]
    public void TestPlaneIntersectsTriangle()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        // Triangle completely in front
        var v1 = new Vector3(0, 1, 0);
        var v2 = new Vector3(1, 1, 0);
        var v3 = new Vector3(0.5f, 1, 1);
        Assert.Equal(PlaneIntersectionType.Front, plane.Intersects(ref v1, ref v2, ref v3));

        // Triangle completely behind
        var v4 = new Vector3(0, -1, 0);
        var v5 = new Vector3(1, -1, 0);
        var v6 = new Vector3(0.5f, -1, 1);
        Assert.Equal(PlaneIntersectionType.Back, plane.Intersects(ref v4, ref v5, ref v6));

        // Triangle intersecting plane
        var v7 = new Vector3(0, -1, 0);
        var v8 = new Vector3(1, 1, 0);
        var v9 = new Vector3(0.5f, 0, 1);
        Assert.Equal(PlaneIntersectionType.Intersecting, plane.Intersects(ref v7, ref v8, ref v9));
    }

    [Fact]
    public void TestPlaneIntersectsBoundingBox()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        // Box completely in front
        var box1 = new BoundingBox(new Vector3(-10, 1, -10), new Vector3(10, 11, 10));
        Assert.Equal(PlaneIntersectionType.Front, plane.Intersects(ref box1));

        // Box completely behind
        var box2 = new BoundingBox(new Vector3(-10, -11, -10), new Vector3(10, -1, 10));
        Assert.Equal(PlaneIntersectionType.Back, plane.Intersects(ref box2));

        // Box intersecting plane
        var box3 = new BoundingBox(new Vector3(-10, -5, -10), new Vector3(10, 5, 10));
        Assert.Equal(PlaneIntersectionType.Intersecting, plane.Intersects(ref box3));
    }

    [Fact]
    public void TestPlaneIntersectsBoundingSphere()
    {
        var plane = new Plane(0, 1, 0, 0); // XZ plane at Y=0

        // Sphere completely in front
        var sphere1 = new BoundingSphere(new Vector3(0, 10, 0), 5);
        Assert.Equal(PlaneIntersectionType.Front, plane.Intersects(ref sphere1));

        // Sphere completely behind
        var sphere2 = new BoundingSphere(new Vector3(0, -10, 0), 5);
        Assert.Equal(PlaneIntersectionType.Back, plane.Intersects(ref sphere2));

        // Sphere intersecting plane
        var sphere3 = new BoundingSphere(new Vector3(0, 3, 0), 5);
        Assert.Equal(PlaneIntersectionType.Intersecting, plane.Intersects(ref sphere3));
    }

    [Fact]
    public void TestPlaneTransformArray()
    {
        var planes = new[]
        {
            new Plane(0, 1, 0, 0),
            new Plane(1, 0, 0, 0)
        };

        var rotation = Quaternion.RotationZ(MathUtil.PiOverTwo);
        Plane.Transform(planes, ref rotation);

        // Verify planes were transformed
        Assert.NotEqual(0f, planes[0].Normal.X);
        Assert.NotEqual(0f, planes[1].Normal.Y);
    }

    [Fact]
    public void TestPlaneTransformMatrixArray()
    {
        var planes = new[]
        {
            new Plane(0, 1, 0, 0),
            new Plane(1, 0, 0, 0)
        };

        var matrix = Matrix.RotationZ(MathUtil.PiOverTwo);
        Plane.Transform(planes, ref matrix);

        // Verify planes were transformed
        Assert.NotEqual(0f, planes[0].Normal.X);
        Assert.NotEqual(0f, planes[1].Normal.Y);
    }

    [Fact]
    public void TestPlaneNormalizeComponents()
    {
        Plane.Normalize(2f, 0f, 0f, 4f, out var normalized);

        Assert.Equal(1f, normalized.Normal.X, 3);
        Assert.Equal(0f, normalized.Normal.Y, 3);
        Assert.Equal(0f, normalized.Normal.Z, 3);
        Assert.Equal(2f, normalized.D, 3);
    }
}

