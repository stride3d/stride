// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.BepuPhysics.Soft.Tests;

public class TopologyTests
{
    [Fact]
    public static void VoxelizerFillsABox()
    {
        var box = Box(new Vector3(2f, 1f, 1f));
        var inside = Voxelizer.CellCentersInside(box.Positions, box.Indices, new Vector3(-1f, -0.5f, -0.5f), 0.25f, new Int3(8, 4, 4));
        Assert.All(inside, Assert.True);

        // A grid larger than the box only keeps the cells within it
        inside = Voxelizer.CellCentersInside(box.Positions, box.Indices, new Vector3(-2f, -1f, -1f), 0.25f, new Int3(16, 8, 8));
        Assert.Equal(8 * 4 * 4, inside.Count(cell => cell));
    }

    [Fact]
    public static void VoxelizerFillsASphere()
    {
        var sphere = Sphere(1f, 32);
        const int size = 40;
        var inside = Voxelizer.CellCentersInside(sphere.Positions, sphere.Indices, new Vector3(-1f), 2f / size, new Int3(size));
        var fraction = inside.Count(cell => cell) / (float)inside.Length;
        Assert.InRange(fraction, MathF.PI / 6f * 0.95f, MathF.PI / 6f * 1.02f);
    }

    [Fact]
    public static void VoxelizerToleratesAMissingTriangle()
    {
        var box = Box(Vector3.One);
        var indices = box.Indices.Skip(3).ToArray(); // A hole in one face
        var inside = Voxelizer.CellCentersInside(box.Positions, indices, new Vector3(-0.5f), 0.125f, new Int3(8));
        Assert.All(inside, Assert.True);
    }

    [Fact]
    public static void LatticeRestShapeReproducesTheMesh()
    {
        var mesh = Sphere(0.5f, 16);
        var topology = LatticeBuilder.Build(mesh, 6, 0.5f);

        Assert.NotEmpty(topology.Tetrahedra);
        Assert.Equal(8, topology.BindingStride);
        AssertRestShapeReproducesMesh(topology);

        // Every particle sits within the bounds of the mesh, the outermost ones half a cell in
        var bounds = mesh.ComputeBounds();
        foreach (var particle in topology.RestPositions)
            Assert.Equal(ContainmentType.Contains, bounds.Contains(in particle));
    }

    [Fact]
    public static void LatticeOfABoxIsRegular()
    {
        var topology = LatticeBuilder.Build(Box(Vector3.One), 4, 0.5f);

        Assert.Equal(5 * 5 * 5, topology.ParticleCount);
        Assert.Equal(5 * 4 * 4 * 4, topology.Tetrahedra.Length);
        // Edges of the cells along each axis, then the two diagonals of every face and the four of every cell
        int stretch = 3 * 4 * 5 * 5;
        int shear = 3 * 2 * 4 * 4 * 5 + 4 * 4 * 4 * 4;
        Assert.Equal(stretch, topology.Edges.Count(e => e.Kind == EdgeKind.Stretch));
        Assert.Equal(shear, topology.Edges.Count(e => e.Kind == EdgeKind.Shear));
        AssertRestShapeReproducesMesh(topology);
    }

    [Fact]
    public static void LatticeRejectsOpenSurfaces()
    {
        Assert.Throws<InvalidOperationException>(() => LatticeBuilder.Build(Grid(4, doubleSided: false), 4, 0.5f));
    }

    [Fact]
    public static void ClothWeldsBothFaces()
    {
        const int cells = 4;
        var topology = ClothBuilder.Build(Grid(cells, doubleSided: true));

        Assert.Equal((cells + 1) * (cells + 1), topology.ParticleCount);
        Assert.Equal(1, topology.BindingStride);
        // Rows, columns and one diagonal per quad
        Assert.Equal(2 * cells * (cells + 1) + cells * cells, topology.Edges.Count(e => e.Kind == EdgeKind.Stretch));
        // One per interior edge, the back faces do not add any
        Assert.Equal(2 * cells * (cells - 1) + cells * cells, topology.Edges.Count(e => e.Kind == EdgeKind.Bend));
        Assert.DoesNotContain(topology.Edges, e => e.A == e.B);
        AssertRestShapeReproducesMesh(topology);
    }

    [Fact]
    public static void LatticeFramesFollowRotations()
    {
        var mesh = Box(Vector3.One);
        var topology = LatticeBuilder.Build(mesh, 3, 0.5f);
        foreach (var rotation in new[] { Quaternion.RotationY(MathF.PI / 2f), Quaternion.RotationX(MathF.PI / 2f), Quaternion.RotationYawPitchRoll(0.3f, 1.2f, -0.7f) })
        {
            var particles = Array.ConvertAll(topology.RestPositions, p => Vector3.Transform(p, rotation) + new Vector3(4f, 1f, -2f));
            for (int v = 0; v < mesh.Positions.Length; v++)
            {
                topology.DeformFrame(v, particles, out var normal, out _);
                var expected = Vector3.Transform(mesh.Normals[v], rotation);
                Assert.True(Vector3.Distance(Vector3.Normalize(normal), expected) < 1e-3f, $"Vertex {v}: {Vector3.Normalize(normal)} != {expected}");
            }
        }
    }

    [Fact]
    public static void ShapeMatchingFindsTheRotation()
    {
        var random = new Random(4);
        var points = Enumerable.Range(0, 20).Select(_ => new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) - new Vector3(0.5f)).ToArray();
        var expected = Quaternion.RotationYawPitchRoll(1.1f, -0.4f, 2.3f);

        Vector3 column0 = default, column1 = default, column2 = default;
        foreach (var rest in points)
        {
            var current = Vector3.Transform(rest, expected) * 1.3f; // Scaling does not change the rotation
            column0 += current * rest.X;
            column1 += current * rest.Y;
            column2 += current * rest.Z;
        }

        var rotation = ShapeMatching.ExtractRotation(column0, column1, column2, Quaternion.Identity, iterations: 50);
        Assert.True(MathF.Abs(Quaternion.Dot(rotation, expected)) > 0.9999f, $"{rotation} != {expected}");
    }

    private static void AssertRestShapeReproducesMesh(SoftBodyTopology topology)
    {
        var deformed = new Vector3[topology.Mesh.Positions.Length];
        topology.Deform(topology.RestPositions, deformed);
        for (int i = 0; i < deformed.Length; i++)
            Assert.True(Vector3.Distance(deformed[i], topology.Mesh.Positions[i]) < 1e-4f, $"Vertex {i} moved to {deformed[i]} from {topology.Mesh.Positions[i]}");

        var weightSum = 0f;
        for (int i = 0; i < topology.BindingWeights.Length; i++)
        {
            weightSum += topology.BindingWeights[i];
            if ((i + 1) % topology.BindingStride == 0)
            {
                Assert.Equal(1f, weightSum, 4);
                weightSum = 0f;
            }
        }
    }

    private static SourceMesh FromTriangles(List<Vector3> positions, List<int> indices)
    {
        var normals = new Vector3[positions.Count];
        SourceMesh.ComputeNormals(positions.ToArray(), indices.ToArray(), normals);
        return new SourceMesh
        {
            Positions = positions.ToArray(),
            Normals = normals,
            Tangents = new Vector4[positions.Count],
            TexCoords = new Vector2[positions.Count],
            Indices = indices.ToArray(),
            Parts = [new SourceMesh.Part(0, positions.Count, 0, indices.Count, 0)],
        };
    }

    /// <summary> A box with split vertices on each face, as most models have </summary>
    private static SourceMesh Box(Vector3 size)
    {
        var positions = new List<Vector3>();
        var indices = new List<int>();
        for (int face = 0; face < 6; face++)
        {
            int axis = face / 2;
            var normal = Vector3.Zero;
            normal[axis] = face % 2 == 0 ? 1f : -1f;
            var u = Vector3.Zero;
            u[(axis + 1) % 3] = 1f;
            var v = Vector3.Cross(normal, u);
            int first = positions.Count;
            foreach (var (a, b) in new[] { (-1f, -1f), (1f, -1f), (1f, 1f), (-1f, 1f) })
                positions.Add((normal + u * a + v * b) * size * 0.5f);
            indices.AddRange([first, first + 1, first + 2, first, first + 2, first + 3]);
        }
        return FromTriangles(positions, indices);
    }

    private static SourceMesh Sphere(float radius, int segments)
    {
        var positions = new List<Vector3>();
        var indices = new List<int>();
        for (int ring = 0; ring <= segments; ring++)
        {
            var phi = MathF.PI * ring / segments;
            for (int slice = 0; slice <= segments; slice++)
            {
                var theta = 2f * MathF.PI * slice / segments;
                positions.Add(new Vector3(MathF.Sin(phi) * MathF.Cos(theta), MathF.Cos(phi), MathF.Sin(phi) * MathF.Sin(theta)) * radius);
            }
        }
        for (int ring = 0; ring < segments; ring++)
        for (int slice = 0; slice < segments; slice++)
        {
            int a = ring * (segments + 1) + slice, b = a + 1, c = a + segments + 1, d = c + 1;
            indices.AddRange([a, c, b, b, c, d]);
        }
        return FromTriangles(positions, indices);
    }

    /// <summary> A unit square on XZ split in cells, with its back face made of duplicated vertices when asked </summary>
    private static SourceMesh Grid(int cells, bool doubleSided)
    {
        var positions = new List<Vector3>();
        var indices = new List<int>();
        for (int side = 0; side < (doubleSided ? 2 : 1); side++)
        {
            int first = positions.Count;
            for (int z = 0; z <= cells; z++)
            for (int x = 0; x <= cells; x++)
                positions.Add(new Vector3((float)x / cells - 0.5f, 0f, (float)z / cells - 0.5f));
            for (int z = 0; z < cells; z++)
            for (int x = 0; x < cells; x++)
            {
                int a = first + z * (cells + 1) + x, b = a + 1, c = a + cells + 1, d = c + 1;
                if (side == 0)
                    indices.AddRange([a, c, b, b, c, d]);
                else
                    indices.AddRange([a, b, c, b, d, c]);
            }
        }
        return FromTriangles(positions, indices);
    }
}
