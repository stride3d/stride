// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Assets.VHACD;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The native V-HACD decomposition gets a triangle count, not an index count, and returns hulls made of triangles.
    /// </summary>
    public class TestConvexHullMesh
    {
        [Fact]
        public void DecompositionOfATorusReturnsHullsOfTriangles()
        {
            const int Rings = 32, Sides = 16;
            var vertices = new float[Rings * Sides * 3];
            var indices = new uint[Rings * Sides * 6];
            for (int i = 0, v = 0, t = 0; i < Rings; i++)
            {
                for (int j = 0; j < Sides; j++)
                {
                    var u = 2 * MathF.PI * i / Rings;
                    var w = 2 * MathF.PI * j / Sides;
                    var radius = 1 + 0.3f * MathF.Cos(w);
                    vertices[v++] = radius * MathF.Cos(u);
                    vertices[v++] = 0.3f * MathF.Sin(w);
                    vertices[v++] = radius * MathF.Sin(u);

                    uint a = (uint)(i * Sides + j), b = (uint)((i + 1) % Rings * Sides + j);
                    uint c = (uint)((i + 1) % Rings * Sides + (j + 1) % Sides), d = (uint)(i * Sides + (j + 1) % Sides);
                    indices[t++] = a; indices[t++] = b; indices[t++] = c;
                    indices[t++] = a; indices[t++] = c; indices[t++] = d;
                }
            }

            using var mesh = new ConvexHullMesh();
            mesh.Generate(new ConvexHullMesh.DecompositionDesc
            {
                VertexCount = (uint)vertices.Length / 3,
                TriangleCount = (uint)indices.Length / 3,
                Vertexes = vertices,
                Indices = indices,
                MaxConvexHulls = 4,
                Resolution = 100_000,
                MaxRecursionDepth = 10,
                MinimumVolumePercentErrorAllowed = 1.0,
                ShrinkWrap = true,
                FillMode = ConvexDecompositionFillMode.FloodFill,
                MaxNumVerticesPerCH = 16,
            });

            Assert.InRange(mesh.Count, 1u, 4u);
            for (uint h = 0; h < mesh.Count; h++)
            {
                mesh.CopyPoints(h, out var points);
                mesh.CopyIndices(h, out var hullIndices);
                Assert.NotEmpty(points);
                Assert.NotEmpty(hullIndices);
                Assert.Equal(0, hullIndices.Length % 3);
            }
        }
    }
}
