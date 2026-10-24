// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Stride.Core.Mathematics;
using Stride.Graphics.GeometricPrimitives;

namespace Stride.Graphics.Tests
{
    public class TestGeometricPrimitiveSubdivisions
    {
        private const float Epsilon = 1e-5f;

        [Fact]
        public void CapsuleWithoutLengthRingsMatchesOriginalOverload()
        {
            AssertSameMesh(GeometricPrimitive.Capsule.New(1.5f, 0.4f, 8, 2, 3, false), GeometricPrimitive.Capsule.New(1.5f, 0.4f, 8, 2, 3, false, lengthRings: 0));
            AssertSameMesh(GeometricPrimitive.Capsule.New(1.5f, 0.4f, 8, 2, 3, false), GeometricPrimitive.Capsule.New(1.5f, 0.4f, 8, 2, 3, false, lengthRings: -2));
        }

        [Fact]
        public void CapsuleLengthRingsAreEvenlySpacedOnTheSurface()
        {
            const float length = 1.5f, radius = 0.4f;
            const int tessellation = 8, lengthRings = 5;
            var mesh = GeometricPrimitive.Capsule.New(length, radius, tessellation, 1, 1, false, lengthRings);

            var ringSize = 4 * tessellation + 1;
            var ringCount = 2 * tessellation + lengthRings;
            Assert.Equal(ringCount * ringSize, mesh.Vertices.Length);
            Assert.Equal((ringCount - 1) * ringSize * 6, mesh.Indices.Length);

            for (int m = 1; m <= lengthRings; m++)
            {
                var expectedY = -length / 2 + length * m / (lengthRings + 1);
                for (int j = 0; j < ringSize; j++)
                {
                    var vertex = mesh.Vertices[(tessellation - 1 + m) * ringSize + j];
                    Assert.Equal(expectedY, vertex.Position.Y, Epsilon);
                    Assert.Equal(radius, new Vector2(vertex.Position.X, vertex.Position.Z).Length(), Epsilon);
                    Assert.Equal(0, vertex.Normal.Y, Epsilon);
                }
            }

            AssertUnitNormals(mesh);
            AssertSameWinding(GeometricPrimitive.Capsule.New(length, radius, tessellation, 1, 1, false), mesh);
        }

        [Fact]
        public void CylinderWithoutHeightRingsMatchesOriginalOverload()
        {
            AssertSameMesh(GeometricPrimitive.Cylinder.New(2, 0.3f, 12, 2, 3, false), GeometricPrimitive.Cylinder.New(2, 0.3f, 12, 2, 3, false, heightRings: 0));
            AssertSameMesh(GeometricPrimitive.Cylinder.New(2, 0.3f, 12, 2, 3, false), GeometricPrimitive.Cylinder.New(2, 0.3f, 12, 2, 3, false, heightRings: -1));
        }

        [Fact]
        public void CylinderHeightRingsAreEvenlySpacedOnTheSurface()
        {
            const float height = 2, radius = 0.3f;
            const int tessellation = 12, heightRings = 4;
            var mesh = GeometricPrimitive.Cylinder.New(height, radius, tessellation, 1, 1, false, heightRings);

            var rows = heightRings + 2;
            var sideVertexCount = (tessellation + 1) * rows;
            Assert.Equal(sideVertexCount + tessellation * 4, mesh.Vertices.Length);
            Assert.Equal((tessellation + 1) * (rows - 1) * 6 + (tessellation - 2) * 6, mesh.Indices.Length);

            for (int i = 0; i <= tessellation; i++)
            {
                for (int k = 0; k < rows; k++)
                {
                    var vertex = mesh.Vertices[i * rows + k];
                    Assert.Equal(height / 2 - height * k / (rows - 1), vertex.Position.Y, Epsilon);
                    Assert.Equal(radius, new Vector2(vertex.Position.X, vertex.Position.Z).Length(), Epsilon);
                    Assert.Equal(0, vertex.Normal.Y, Epsilon);
                }
            }

            // The caps fill only part of their reserved range; the remaining vertices are never indexed.
            Assert.True(mesh.Indices.Max() < sideVertexCount + tessellation * 2);
            AssertUnitNormals(mesh, mesh.Indices.Max() + 1);
            AssertSameWinding(GeometricPrimitive.Cylinder.New(height, radius, tessellation, 1, 1, false), mesh);
        }

        [Fact]
        public void CubeWithoutSubdivisionsMatchesOriginalOverload()
        {
            AssertSameMesh(GeometricPrimitive.Cube.New(new Vector3(1, 2, 3), 2, 3, false), GeometricPrimitive.Cube.New(new Vector3(1, 2, 3), 2, 3, false, subdivisions: 0));
            AssertSameMesh(GeometricPrimitive.Cube.New(1.5f, 2, 3, false), GeometricPrimitive.Cube.New(1.5f, 2, 3, false, subdivisions: 0));
            AssertSameMesh(GeometricPrimitive.Cube.New(1.5f, 2, 3, false), GeometricPrimitive.Cube.New(1.5f, 2, 3, false, subdivisions: -4));
        }

        [Fact]
        public void CubeSubdivisionsSplitEachFaceIntoAnEvenGrid()
        {
            var size = new Vector3(1, 2, 3);
            const int subdivisions = 3;
            var mesh = GeometricPrimitive.Cube.New(size, 1, 1, false, subdivisions);

            var gridSize = subdivisions + 2;
            Assert.Equal(6 * gridSize * gridSize, mesh.Vertices.Length);
            Assert.Equal(6 * (subdivisions + 1) * (subdivisions + 1) * 6, mesh.Indices.Length);

            foreach (var vertex in mesh.Vertices)
            {
                // On the face plane given by the normal, and inside the face.
                Assert.Equal(Vector3.Dot(size, Abs(vertex.Normal)) / 2, Vector3.Dot(vertex.Position, vertex.Normal), Epsilon);
                for (int axis = 0; axis < 3; axis++)
                    Assert.True(MathF.Abs(vertex.Position[axis]) <= size[axis] / 2 + Epsilon);
            }

            // Along each axis, the vertices lie on subdivisions + 2 evenly spaced planes.
            for (int axis = 0; axis < 3; axis++)
            {
                var step = size[axis] / (subdivisions + 1);
                var planes = mesh.Vertices.Select(v => (int)MathF.Round((v.Position[axis] + size[axis] / 2) / step)).Distinct().Count();
                Assert.Equal(gridSize, planes);
                Assert.All(mesh.Vertices, v =>
                {
                    var t = (v.Position[axis] + size[axis] / 2) / step;
                    Assert.Equal(MathF.Round(t), t, 1e-4f);
                });
            }

            AssertUnitNormals(mesh);
            AssertSameWinding(GeometricPrimitive.Cube.New(size, 1, 1, false), mesh);
        }

        private static Vector3 Abs(Vector3 v) => new(MathF.Abs(v.X), MathF.Abs(v.Y), MathF.Abs(v.Z));

        private static void AssertSameMesh(GeometricMeshData<VertexPositionNormalTexture> expected, GeometricMeshData<VertexPositionNormalTexture> actual)
        {
            Assert.Equal(expected.Indices, actual.Indices);
            Assert.Equal(expected.Vertices.Length, actual.Vertices.Length);
            for (int i = 0; i < expected.Vertices.Length; i++)
            {
                Assert.Equal(expected.Vertices[i].Position, actual.Vertices[i].Position);
                Assert.Equal(expected.Vertices[i].Normal, actual.Vertices[i].Normal);
                Assert.Equal(expected.Vertices[i].TextureCoordinate, actual.Vertices[i].TextureCoordinate);
            }
            Assert.Equal(expected.IsLeftHanded, actual.IsLeftHanded);
        }

        private static void AssertUnitNormals(GeometricMeshData<VertexPositionNormalTexture> mesh, int vertexCount = -1)
        {
            foreach (var vertex in mesh.Vertices.Take(vertexCount < 0 ? mesh.Vertices.Length : vertexCount))
                Assert.Equal(1, vertex.Normal.Length(), Epsilon);
        }

        // Every non-degenerate triangle faces the same side of its vertex normals as in the mesh without subdivision.
        private static void AssertSameWinding(GeometricMeshData<VertexPositionNormalTexture> reference, GeometricMeshData<VertexPositionNormalTexture> mesh)
        {
            var expected = WindingSigns(reference).Distinct().ToArray();
            Assert.Single(expected);
            Assert.All(WindingSigns(mesh), sign => Assert.Equal(expected[0], sign));
        }

        private static int[] WindingSigns(GeometricMeshData<VertexPositionNormalTexture> mesh)
        {
            var signs = new List<int>();
            for (int i = 0; i < mesh.Indices.Length; i += 3)
            {
                var a = mesh.Vertices[mesh.Indices[i]];
                var b = mesh.Vertices[mesh.Indices[i + 1]];
                var c = mesh.Vertices[mesh.Indices[i + 2]];
                var faceNormal = Vector3.Cross(b.Position - a.Position, c.Position - a.Position);
                if (faceNormal.Length() < 1e-6f)
                    continue;
                signs.Add(MathF.Sign(Vector3.Dot(faceNormal, a.Normal + b.Normal + c.Normal)));
            }
            return signs.ToArray();
        }
    }
}
