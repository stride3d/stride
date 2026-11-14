// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
        // Columns of the jacobian of the trilinear interpolation over the cell's corners.
        // Fully unrolled: an eight-iteration loop with four reads each is slower than this.
        var t = LatticeCoordinates![vertex];
        float tx = t.X, ty = t.Y, tz = t.Z;
        float ux = 1f - tx, uy = 1f - ty, uz = 1f - tz;

        ref int bp = ref Unsafe.Add(ref MemoryMarshal.GetArrayDataReference(BindingParticles), (nint)vertex * 8);
        ref Vector3 pb = ref MemoryMarshal.GetReference(particles);
        var p0 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 0));
        var p1 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 1));
        var p2 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 2));
        var p3 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 3));
        var p4 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 4));
        var p5 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 5));
        var p6 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 6));
        var p7 = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 7));

        // d/dt_x, d/dt_y, d/dt_z of the trilinear blend, assembled as pair differences.
        var dx = (p1 - p0) * (uy * uz) + (p3 - p2) * (ty * uz) + (p5 - p4) * (uy * tz) + (p7 - p6) * (ty * tz);
        var dy = (p2 - p0) * (ux * uz) + (p3 - p1) * (tx * uz) + (p6 - p4) * (ux * tz) + (p7 - p5) * (tx * tz);
        var dz = (p4 - p0) * (ux * uy) + (p5 - p1) * (tx * uy) + (p6 - p2) * (ux * ty) + (p7 - p3) * (tx * ty);

        // Normals transform with the cofactor matrix, which is the inverse transpose up to a scale
        var n = Mesh.Normals[vertex];
        normal = Vector3.Cross(dy, dz) * n.X + Vector3.Cross(dz, dx) * n.Y + Vector3.Cross(dx, dy) * n.Z;
        var restTangent = Mesh.Tangents[vertex];
        tangent = new Vector4(dx * restTangent.X + dy * restTangent.Y + dz * restTangent.Z, restTangent.W);
    }

    /// <summary> Writes the position of every vertex of <see cref="Mesh"/> given the current position of each particle </summary>
    public void Deform(ReadOnlySpan<Vector3> particles, Span<Vector3> vertices)
    {
        // Lattice vertices bind to the eight corners of the cell they are in: inlined, no bounds checks.
        if (BindingStride == 8)
        {
            ref int bpBase = ref MemoryMarshal.GetArrayDataReference(BindingParticles);
            ref float bwBase = ref MemoryMarshal.GetArrayDataReference(BindingWeights);
            ref Vector3 pb = ref MemoryMarshal.GetReference(particles);
            ref Vector3 vb = ref MemoryMarshal.GetReference(vertices);
            int count = vertices.Length;
            for (int v = 0; v < count; v++)
            {
                nint off = (nint)v * 8;
                ref int bp = ref Unsafe.Add(ref bpBase, off);
                ref float bw = ref Unsafe.Add(ref bwBase, off);
                var p = Unsafe.Add(ref pb, Unsafe.Add(ref bp, 0)) * Unsafe.Add(ref bw, 0)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 1)) * Unsafe.Add(ref bw, 1)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 2)) * Unsafe.Add(ref bw, 2)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 3)) * Unsafe.Add(ref bw, 3)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 4)) * Unsafe.Add(ref bw, 4)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 5)) * Unsafe.Add(ref bw, 5)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 6)) * Unsafe.Add(ref bw, 6)
                      + Unsafe.Add(ref pb, Unsafe.Add(ref bp, 7)) * Unsafe.Add(ref bw, 7);
                Unsafe.Add(ref vb, v) = p;
            }
            return;
        }

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
