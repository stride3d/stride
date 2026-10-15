// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Soft.Topology;

internal enum EdgeKind : byte
{
    /// <summary> Resists stretching along the surface or the lattice axes </summary>
    Stretch,
    /// <summary> Diagonals of the lattice, resists shearing </summary>
    Shear,
    /// <summary> Spans two adjacent triangles, resists folding </summary>
    Bend,
}

internal readonly record struct Edge(int A, int B, EdgeKind Kind);

internal readonly record struct Tetrahedron(int A, int B, int C, int D);

/// <summary>
/// The particles and constraints of a soft body in the space of its source model, and how the model's vertices follow the particles.
/// </summary>
internal sealed class SoftBodyTopology
{
    public required Vector3[] RestPositions { get; init; }

    /// <summary> Per particle, two particles less than one step apart on every axis never collide with each other </summary>
    public required Int3[] SelfCollisionCells { get; init; }

    public required Edge[] Edges { get; init; }

    public required Tetrahedron[] Tetrahedra { get; init; }

    /// <summary> The usual rest distance between two neighboring particles </summary>
    public required float Spacing { get; init; }

    /// <summary> How many particles drive each vertex of <see cref="Mesh"/> </summary>
    public required int BindingStride { get; init; }

    /// <summary> <see cref="BindingStride"/> particle indices per vertex </summary>
    public required int[] BindingParticles { get; init; }

    /// <summary> <see cref="BindingStride"/> weights per vertex, summing to one but not necessarily within [0, 1] </summary>
    public required float[] BindingWeights { get; init; }

    /// <summary>
    /// For lattices, the position of each vertex inside the cell it follows, in cells; the particles of <see cref="BindingParticles"/> are its corners.
    /// </summary>
    public Vector3[]? LatticeCoordinates { get; init; }

    public required SourceMesh Mesh { get; init; }

    public int ParticleCount => RestPositions.Length;

    /// <summary> Carries the normal and tangent of a lattice vertex along the deformation of the cell it is in, unnormalized </summary>
    public void DeformFrame(int vertex, ReadOnlySpan<Vector3> particles, out Vector3 normal, out Vector4 tangent)
    {
        // Columns of the jacobian of the trilinear interpolation over the cell's corners
        var t = LatticeCoordinates![vertex];
        Vector3 dx = default, dy = default, dz = default;
        for (int corner = 0; corner < 8; corner++)
        {
            float sx = (corner & 1) != 0 ? 1f : -1f, wx = (corner & 1) != 0 ? t.X : 1f - t.X;
            float sy = (corner & 2) != 0 ? 1f : -1f, wy = (corner & 2) != 0 ? t.Y : 1f - t.Y;
            float sz = (corner & 4) != 0 ? 1f : -1f, wz = (corner & 4) != 0 ? t.Z : 1f - t.Z;
            var p = particles[BindingParticles[vertex * 8 + corner]];
            dx += p * (sx * wy * wz);
            dy += p * (wx * sy * wz);
            dz += p * (wx * wy * sz);
        }

        // Normals transform with the cofactor matrix, which is the inverse transpose up to a scale
        var n = Mesh.Normals[vertex];
        normal = Vector3.Cross(dy, dz) * n.X + Vector3.Cross(dz, dx) * n.Y + Vector3.Cross(dx, dy) * n.Z;
        var restTangent = Mesh.Tangents[vertex];
        tangent = new Vector4(dx * restTangent.X + dy * restTangent.Y + dz * restTangent.Z, restTangent.W);
    }

    /// <summary> Writes the position of every vertex of <see cref="Mesh"/> given the current position of each particle </summary>
    public void Deform(ReadOnlySpan<Vector3> particles, Span<Vector3> vertices)
    {
        int stride = BindingStride;
        for (int v = 0, b = 0; v < vertices.Length; v++, b += stride)
        {
            var position = Vector3.Zero;
            for (int i = 0; i < stride; i++)
                position += particles[BindingParticles[b + i]] * BindingWeights[b + i];
            vertices[v] = position;
        }
    }
}
