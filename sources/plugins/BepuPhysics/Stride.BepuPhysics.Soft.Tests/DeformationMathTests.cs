// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.BepuPhysics.Soft.Rendering;
using Stride.BepuPhysics.Soft.Topology;
using Stride.Core.Mathematics;
using Xunit;

namespace Stride.BepuPhysics.Soft.Tests;

/// <summary>
/// The frames the GPU deformation computes per vertex: the jacobian of a lattice cell applied to the rest normal and tangent,
/// and the triangles around a cloth vertex standing for the normals and tangents the CPU recomputes over the whole mesh.
/// </summary>
public class DeformationMathTests
{
    private const float Tolerance = 1e-4f;

    public static TheoryData<int> Deformations => new() { 0, 1, 2 };

    /// <summary> Shear, uneven scale, rotation and translation: what the jacobian of a cell under one linear map must reproduce </summary>
    private static Matrix Affine(int index) => index switch
    {
        0 => Matrix.Scaling(2f, 0.5f, 1f) * Matrix.Translation(1f, 2f, 3f),
        1 => new Matrix(1f, 0f, 0f, 0f, 0.8f, 1f, 0f, 0f, 0f, -0.3f, 1f, 0f, 0f, 0f, 0f, 1f) * Matrix.RotationYawPitchRoll(0.4f, -1.1f, 0.3f),
        _ => Matrix.Scaling(-1f, 1f, 1f) * Matrix.Scaling(0.7f, 1.6f, 1.2f) * Matrix.RotationAxis(Vector3.Normalize(new Vector3(1f, 2f, 3f)), 2f),
    };

    [Theory]
    [MemberData(nameof(Deformations))]
    public static void LatticeNormalsFollowTheInverseTransposeAndTangentsTheMap(int deformation)
    {
        var map = Affine(deformation);
        Matrix.Transpose(Matrix.Invert(map), out var normalMap);
        var topology = LatticeBuilder.Build(Grid(4, closedBox: true, clockwise: false), 3, 0.5f);
        var particles = Array.ConvertAll(topology.RestPositions, p => Vector3.TransformCoordinate(p, map));
        // The cofactors carry the sign of the determinant: a mirroring map turns the normals inside out, as it does the faces
        var flip = map.Determinant() < 0f ? -1f : 1f;

        for (int v = 0; v < topology.Mesh.Positions.Length; v++)
        {
            topology.DeformFrame(v, particles, out var normal, out var tangent);
            var expectedNormal = Vector3.Normalize(Vector3.TransformNormal(topology.Mesh.Normals[v], normalMap)) * flip;
            var expectedTangent = Vector3.Normalize(Vector3.TransformNormal((Vector3)topology.Mesh.Tangents[v], map));
            AssertNear(expectedNormal, Vector3.Normalize(normal), $"normal of vertex {v}");
            AssertNear(expectedTangent, Vector3.Normalize((Vector3)tangent), $"tangent of vertex {v}");
            Assert.Equal(topology.Mesh.Tangents[v].W, tangent.W);
        }
    }

    /// <summary> Under one linear map, the smooth deformation puts every vertex, normal and tangent where the map does </summary>
    [Theory]
    [MemberData(nameof(Deformations))]
    public static void SmoothDeformationReproducesALinearMap(int deformation)
    {
        var map = Affine(deformation);
        Matrix.Transpose(Matrix.Invert(map), out var normalMap);
        var topology = LatticeBuilder.Build(Grid(4, closedBox: true, clockwise: false), 3, 0.5f);
        var particles = Array.ConvertAll(topology.RestPositions, p => Vector3.TransformCoordinate(p, map));
        var frames = new Vector3[particles.Length * 3];
        topology.ComputeParticleFrames(particles, frames);
        var flip = map.Determinant() < 0f ? -1f : 1f;

        for (int v = 0; v < topology.Mesh.Positions.Length; v++)
        {
            topology.DeformSmooth(v, particles, frames, out var position, out var normal, out var tangent);
            AssertNear(Vector3.TransformCoordinate(topology.Mesh.Positions[v], map), position, $"position of vertex {v}");
            AssertNear(Vector3.Normalize(Vector3.TransformNormal(topology.Mesh.Normals[v], normalMap)) * flip, Vector3.Normalize(normal), $"normal of vertex {v}");
            AssertNear(Vector3.Normalize(Vector3.TransformNormal((Vector3)topology.Mesh.Tangents[v], map)), Vector3.Normalize((Vector3)tangent), $"tangent of vertex {v}");
            Assert.Equal(topology.Mesh.Tangents[v].W, tangent.W);
        }
    }

    /// <summary>
    /// A point on the face between two cells of a bent lattice, seen from each of the two: the smooth deformation gives it one
    /// position and one normal, the fast one gives it one position and two normals, which is the fold its shading shows.
    /// </summary>
    [Fact]
    public static void SmoothDeformationHasOneNormalAcrossTheFaceOfACell()
    {
        var lattice = LatticeBuilder.Build(Grid(4, closedBox: true, clockwise: false), 4, 0.5f);
        var origin = lattice.RestPositions[0];
        foreach (var rest in lattice.RestPositions)
            origin = Vector3.Min(origin, rest);
        int ParticleAt(int x, int y, int z)
        {
            int particle = Array.FindIndex(lattice.RestPositions, rest => Vector3.Distance(rest, origin + new Vector3(x, y, z) * lattice.Spacing) < lattice.Spacing * 0.01f);
            Assert.True(particle >= 0, $"No particle at {x}, {y}, {z}");
            return particle;
        }

        // The same point as the +x face of the cell at 1, 1, 1 and as the -x face of the cell at 2, 1, 1
        var corners = new int[16];
        for (int corner = 0; corner < 8; corner++)
        {
            corners[corner] = ParticleAt(1 + (corner & 1), 1 + ((corner >> 1) & 1), 1 + (corner >> 2));
            corners[8 + corner] = ParticleAt(2 + (corner & 1), 1 + ((corner >> 1) & 1), 1 + (corner >> 2));
        }
        var normal = Vector3.Normalize(new Vector3(0.2f, 1f, 0.3f));
        var tangent = new Vector4(Vector3.Normalize(Vector3.Cross(normal, Vector3.UnitX)), 1f);
        var point = new SourceMesh
        {
            Positions = [default, default],
            Normals = [normal, normal],
            Tangents = [tangent, tangent],
            TexCoords = [default, default],
            Indices = [],
            Parts = [],
        };
        var topology = new SoftBodyTopology
        {
            RestPositions = lattice.RestPositions,
            SelfCollisionCells = lattice.SelfCollisionCells,
            Edges = lattice.Edges,
            Tetrahedra = lattice.Tetrahedra,
            Spacing = lattice.Spacing,
            BindingStride = 8,
            BindingParticles = corners,
            BindingWeights = new float[16],
            LatticeCoordinates = [new Vector3(1f, 0.3f, 0.6f), new Vector3(0f, 0.3f, 0.6f)],
            LatticeNeighbors = lattice.LatticeNeighbors,
            Mesh = point,
        };

        // Bent and twisted, nothing a single linear map would do
        var particles = Array.ConvertAll(lattice.RestPositions, rest =>
        {
            var local = (rest - origin) / lattice.Spacing;
            return rest + new Vector3(0.05f * local.Y * local.Y, 0.12f * local.X * local.X - 0.04f * local.Z * local.X, 0.03f * local.X * local.Y) * lattice.Spacing;
        });
        var frames = new Vector3[particles.Length * 3];
        topology.ComputeParticleFrames(particles, frames);

        topology.DeformSmooth(0, particles, frames, out var positionA, out var normalA, out var tangentA);
        topology.DeformSmooth(1, particles, frames, out var positionB, out var normalB, out var tangentB);
        AssertNear(positionA, positionB, "smooth position from both cells");
        AssertNear(Vector3.Normalize(normalA), Vector3.Normalize(normalB), "smooth normal from both cells");
        AssertNear(Vector3.Normalize((Vector3)tangentA), Vector3.Normalize((Vector3)tangentB), "smooth tangent from both cells");

        topology.DeformFrame(0, particles, out var fastNormalA, out _);
        topology.DeformFrame(1, particles, out var fastNormalB, out _);
        Assert.True(Vector3.Distance(Vector3.Normalize(fastNormalA), Vector3.Normalize(fastNormalB)) > 0.02f, "The fast deformation was expected to fold here");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public static void RingsGiveTheNormalsAndTangentsOfTheDeformedSurface(bool clockwise)
    {
        var topology = ClothBuilder.Build(Grid(8, closedBox: false, clockwise));
        var (ring, starts) = GpuDeformation.BuildRings(topology);
        var mesh = topology.Mesh;

        // A rolled and rippled sheet: no two triangles in the same plane
        var particles = Array.ConvertAll(topology.RestPositions, p => new Vector3(MathF.Sin(p.X * 2f), p.Y + 0.2f * MathF.Sin(p.Z * 9f), MathF.Cos(p.X * 2f) + p.Z));
        var positions = Array.ConvertAll(topology.BindingParticles, p => particles[p]);
        var normals = new Vector3[positions.Length];
        var tangents = new Vector4[positions.Length];
        SourceMesh.ComputeNormals(positions, mesh.Indices, normals, mesh.Clockwise);
        SourceMesh.ComputeTangents(positions, normals, mesh.TexCoords, mesh.Indices, tangents);

        for (int v = 0; v < positions.Length; v++)
        {
            // As SoftBodySurfaceDeformation sums them
            var position = particles[topology.BindingParticles[v]];
            Vector3 normal = default, tangent = default;
            for (int i = starts[v]; i < starts[v + 1]; i++)
            {
                var next = particles[ring[i].Next] - position;
                var previous = particles[ring[i].Previous] - position;
                normal += Vector3.Cross(next, previous);
                tangent += next * ring[i].NextWeight + previous * ring[i].PreviousWeight;
            }
            normal = Vector3.Normalize(normal);
            tangent = Vector3.Normalize(tangent - normal * Vector3.Dot(normal, tangent));

            AssertNear(normals[v], normal, $"normal of vertex {v}");
            AssertNear((Vector3)tangents[v], tangent, $"tangent of vertex {v}");
        }
    }

    [Fact]
    public static void RingsHoldEveryCornerOfEveryTriangle()
    {
        var topology = ClothBuilder.Build(Grid(3, closedBox: false, clockwise: true));
        var (ring, starts) = GpuDeformation.BuildRings(topology);

        Assert.Equal(topology.Mesh.Indices.Length, ring.Length);
        Assert.Equal(ring.Length, starts[^1]);
        // Of the two corners of the first row, the one on the diagonal of its cell is in both triangles of the cell
        Assert.Equal(1, starts[1] - starts[0]);
        Assert.Equal(2, starts[4] - starts[3]);
    }

    private static void AssertNear(Vector3 expected, Vector3 actual, string what)
    {
        Assert.True(MathUtil.WithinEpsilon(expected.X, actual.X, Tolerance) && MathUtil.WithinEpsilon(expected.Y, actual.Y, Tolerance) && MathUtil.WithinEpsilon(expected.Z, actual.Z, Tolerance),
            $"{what}: {actual} instead of {expected}");
    }

    /// <summary>
    /// A unit square on XZ split in cells, with texture coordinates turned a little so tangents are not along an axis;
    /// as a closed box, the six faces of a unit cube made of such squares.
    /// </summary>
    private static SourceMesh Grid(int cells, bool closedBox, bool clockwise)
    {
        var positions = new List<Vector3>();
        var texCoords = new List<Vector2>();
        var indices = new List<int>();
        var faces = closedBox
            ? new[] { Matrix.Translation(0f, 0.5f, 0f), Matrix.RotationX(MathF.PI) * Matrix.Translation(0f, -0.5f, 0f), Matrix.RotationX(MathF.PI / 2f) * Matrix.Translation(0f, 0f, 0.5f),
                Matrix.RotationX(-MathF.PI / 2f) * Matrix.Translation(0f, 0f, -0.5f), Matrix.RotationZ(-MathF.PI / 2f) * Matrix.Translation(0.5f, 0f, 0f), Matrix.RotationZ(MathF.PI / 2f) * Matrix.Translation(-0.5f, 0f, 0f) }
            : new[] { Matrix.Identity };
        foreach (var face in faces)
        {
            int first = positions.Count;
            for (int z = 0; z <= cells; z++)
            for (int x = 0; x <= cells; x++)
            {
                positions.Add(Vector3.TransformCoordinate(new Vector3((float)x / cells - 0.5f, 0f, (float)z / cells - 0.5f), face));
                texCoords.Add(new Vector2(x + 0.3f * z, z - 0.2f * x) / cells);
            }
            for (int z = 0; z < cells; z++)
            for (int x = 0; x < cells; x++)
            {
                int a = first + z * (cells + 1) + x, b = a + 1, c = a + cells + 1, d = c + 1;
                // Facing +Y, out of the box, either way
                indices.AddRange(clockwise ? [a, b, c, b, d, c] : [a, c, b, b, c, d]);
            }
        }

        var normals = new Vector3[positions.Count];
        var tangents = new Vector4[positions.Count];
        SourceMesh.ComputeNormals(positions.ToArray(), indices.ToArray(), normals, clockwise);
        SourceMesh.ComputeTangents(positions.ToArray(), normals, texCoords.ToArray(), indices.ToArray(), tangents);
        return new SourceMesh
        {
            Positions = positions.ToArray(),
            Normals = normals,
            Tangents = tangents,
            TexCoords = texCoords.ToArray(),
            Indices = indices.ToArray(),
            Parts = [new SourceMesh.Part(0, positions.Count, 0, indices.Count, 0)],
            Clockwise = clockwise,
        };
    }
}
